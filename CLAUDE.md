# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
dotnet build                    # Build all projects
dotnet test                     # Run all xUnit tests (Tests/EnrichmentTests.cs)
dotnet test --filter "FullyQualifiedName~DialogEnd_P3Containment"  # Run a single test
dotnet publish src -c Release   # Publish Lambda bundle for deployment
cdk synth                       # Synthesize CloudFormation template
cdk deploy                      # Deploy to AWS (requires credentials)
```

### Local Development

```bash
dotnet run --project Dev                    # Start local dev server on port 3000
dotnet run --project Dev -- send            # Replay export JSONs from exports/ dir
dotnet run --project Dev -- send --batch    # Replay as NDJSON batch
PORT=4000 WEBHOOK_SECRET=mysecret dotnet run --project Dev  # Custom port/secret
```

## Architecture

**Pipeline:** Omilia OCP → POST /webhook → API Gateway HTTP API → Lambda (.NET 8, ARM64) → Kinesis Data Stream → downstream consumers

### Solution Projects

- **src/ (OmiliaWebhook)** — Lambda function. Receives CDR events via API Gateway, enriches them, writes to Kinesis.
- **Infra/ (OmiliaWebhook.Infra)** — AWS CDK stack (C#). Defines API Gateway HTTP API, Lambda, and Kinesis resources.
- **Dev/ (OmiliaWebhook.Dev)** — Local dev server (HttpListener) and test event replay client. Not deployed.
- **Tests/ (OmiliaWebhook.Tests)** — xUnit tests covering enrichment logic only.

### Core Data Flow (src/)

1. **WebhookHandler.cs** — Lambda entry point. Handles auth (`x-webhook-secret` header), parses body (single JSON, JSON array, or NDJSON), validates records, enriches via `Enricher`, writes to Kinesis in batches of 500. Returns 200/207/400/401/500.
2. **Types/CdrTypes.cs** — Omilia CDR schema (`CdrEvent`) and enriched output schema (`EnrichedCdrEvent`). The `Events` field is `object?` because it's a `List<StepEvent>` for `dialog_step` but a comma-separated string for `dialog_end`.
3. **Enrichment/Enricher.cs** — Stateless enrichment. Wraps raw CDR in `EnrichedCdrEvent` (raw preserved as `raw`, derived fields in `enrichment`). Handles three message types: `dialog_start`, `dialog_step`, `dialog_end`.

### Key Design Decisions

- **Partition key is `group`** — ensures Kinesis ordering per Omilia session.
- **Root flow detection:** `flow.parent_step == 0 && flow.type == "Flow"` identifies the master session record.
- **KVP normalization:** `KvpReportingKeyMap` has 50+ explicit mappings from raw Omilia keys to snake_case. Unmapped keys fall back to regex-based PascalCase→snake_case conversion.
- **System KVPs filtered out:** `appId`, `testMode`, `Locale` are excluded from business/reporting KVPs.
- **System event prefixes filtered:** `DialogGroupID:`, `FlowName:`, `appId:`, etc. are excluded from step KVPs.
- **Meta entity names skipped:** `DIALOGACT` and `REJECT` are excluded when picking `option_selected`.
- **JSON serialization uses `SnakeCaseLower`** naming policy for Kinesis output; input parsing uses explicit `[JsonPropertyName]` attributes.
- **CDK infra** runs from the `Infra/` directory via `cdk.json` → `dotnet run --project Infra`.
