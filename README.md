# Omilia OCP CDR Webhook (.NET 8)

Receives streaming export events from Omilia OCP, enriches them with derived fields, and forwards to a Kinesis Data Stream for downstream consumers.

## Architecture

```
Omilia OCP → POST /webhook → API Gateway HTTP API → Lambda (.NET 8) → Kinesis Data Stream → consumers
```

## Project Structure

```
omilia-webhook-dotnet/
├── src/
│   ├── Handlers/WebhookHandler.cs     # Lambda handler (AWSSDK.Kinesis)
│   ├── Types/CdrTypes.cs              # CDR schema types (System.Text.Json)
│   └── Enrichment/Enricher.cs         # Stateless enrichment logic
├── Infra/
│   ├── Program.cs                     # CDK app entry point
│   └── WebhookStack.cs               # CDK stack — API GW + Lambda + Kinesis
├── Dev/
│   ├── Program.cs                     # Dev dispatcher (server or test client)
│   ├── Server.cs                      # Local dev server (HttpListener)
│   └── SendTestEvents.cs             # Replay export data against dev server
├── Tests/EnrichmentTests.cs           # 22 xUnit tests
├── exports/                           # Real Omilia CDR exports for testing (not committed)
├── OmiliaWebhook.sln
└── cdk.json
```

## Quick Start

```bash
dotnet build
dotnet test
```

## Local Development

**Terminal 1 — start the server:**

```bash
dotnet run --project Dev
```

**Terminal 2 — replay real export data:**

```bash
dotnet run --project Dev -- send                # sends each record individually
dotnet run --project Dev -- send --batch        # sends as NDJSON batch
```

Place Omilia CDR export `.json` files in the `exports/` directory at the repo root.

### Custom port or webhook secret

```bash
PORT=4000 WEBHOOK_SECRET=mysecret dotnet run --project Dev
dotnet run --project Dev -- send http://localhost:4000/webhook
```

### Monitoring output

```bash
tail -f Dev/kinesis-output.log
```

## Deploying to AWS

Requires AWS credentials and a bootstrapped CDK environment.

```bash
dotnet publish src -c Release
cdk deploy

# With webhook secret
cdk deploy -c webhookSecret=your-shared-secret-here

# Or via environment variable
OMILIA_WEBHOOK_SECRET=your-secret cdk deploy
```

### Infrastructure

- **API Gateway** — HTTP API with `POST /webhook` and `GET /health` routes
- **Lambda** — .NET 8, ARM64, 256MB, 15s timeout, X-Ray tracing
- **Kinesis** — 2 shards, 48h retention, AWS-managed encryption

## Enrichment

Each raw CDR event is wrapped in an `EnrichedCdrEvent`. The original event is preserved as `raw`; derived fields go in `enrichment`.

Three CDR message types are handled: `dialog_start`, `dialog_step`, `dialog_end`.

### Key enrichment fields

| Message Type | Fields |
|---|---|
| `dialog_start` | `caller_cli`, `dnis`, `ivr_channel`, `is_root_flow` |
| `dialog_step` | `has_user_input`, `system_prompt`, `nlu_intent`, `option_selected`, `task_events`, `step_kvps` |
| `dialog_end` | `business_kvps`, `reporting_kvps`, `containment_outcome`, `exit_code`, `can_not_collected_flag` |

- **Root flow detection:** `flow.parent_step === 0 && flow.type === "Flow"` identifies the master session record
- **Partition key is `group`** ensuring Kinesis ordering per session
- **Enrichment wraps, never mutates:** raw CDR is preserved as `raw`; derived fields go in `enrichment`
- **Reporting KVPs:** 50+ explicit key mappings normalise raw Omilia keys to snake_case (e.g. `CANInputMode` → `can_input_mode`)

### Containment outcome

| Outcome | Condition |
|---|---|
| `completed` | `NEAR_HUP` or `NORMAL` end type |
| `abandoned` | `FAR_HUP` — generic caller hangup |
| `abandoned_auth_failed` | `FAR_HUP` with `authentication:failed` in events |
| `abandoned_auxiliary_failed` | `FAR_HUP` with `auxiliary:failed` in events |
| `transferred` | `TRANSFER` end type |

### Exit codes

| Exit Code | Condition |
|---|---|
| `NEAR_HUP` | Clean completion, no auth |
| `NEAR_HUP_AUTH_PASS` | Clean completion with `authentication:completed` |
| `FAR_HUP` | Generic caller abandoned |
| `FAR_HUP_AUTH_FAILED` | Abandoned with `authentication:failed` |
| `FAR_HUP_AUX_FAILED` | Abandoned with `auxiliary:failed` |
| `TRANSFER` | Transferred to agent |
| `NORMAL` | Normal end type |

## Body Formats

The webhook accepts three formats:

- **Single JSON object** — one CDR event per POST (typical Omilia behavior)
- **JSON array** — multiple events in one POST
- **NDJSON** — newline-delimited JSON, one event per line

## Error Handling

| Status | Meaning |
|---|---|
| **200** | All records accepted |
| **207** | Partial failure — some records failed Kinesis write |
| **400** | Empty body or no valid CDR records |
| **401** | Invalid or missing webhook secret |
| **500** | Unhandled error — Omilia retries within 24-hour window |

## Scripts

| Command | Description |
|---|---|
| `dotnet build` | Build all projects |
| `dotnet test` | Run enrichment unit tests (22 xUnit tests) |
| `dotnet run --project Dev` | Start local dev server (port 3000) |
| `dotnet run --project Dev -- send` | Replay export data against dev server |
| `dotnet publish src -c Release` | Publish Lambda bundle |
| `cdk synth` | CDK synth |
| `cdk deploy` | CDK deploy |
