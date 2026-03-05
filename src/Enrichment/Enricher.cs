using System.Text.Json;
using System.Text.RegularExpressions;
using OmiliaWebhook.Types;

namespace OmiliaWebhook.Enrichment;

public static partial class Enricher
{
    private const string WebhookVersion = "1.0.0";

    // System KVP keys to exclude from business_kvps
    private static readonly HashSet<string> SystemKvpKeys = new() { "appId", "testMode", "Locale" };

    // System event prefixes to exclude when extracting step KVPs (P0)
    private static readonly string[] SystemEventPrefixes =
    {
        "DialogGroupID:", "FlowName:", "appId:", "requestId:",
        "envMode:", "testMode:", "Locale:"
    };

    // Meta NLU entity names to skip when picking option_selected (P1)
    private static readonly HashSet<string> MetaEntityNames = new() { "DIALOGACT", "REJECT" };

    // ── P2: Raw KVP key → standardised reporting key ────────────
    private static readonly Dictionary<string, string> KvpReportingKeyMap = new()
    {
        ["CAN"] = "can",
        ["CRN"] = "crn",
        ["CANInputMode"] = "can_input_mode",
        ["ConfirmEnrolment"] = "consent_ivr_enrol",
        ["BG_Testing"] = "bg_testing",
        ["AuthLevel"] = "auth_level",
        ["AuthMode"] = "auth_mode",
        ["ManualBypass"] = "agent_manual_bypass",
        ["BlockListCLI"] = "blocklist_cli",
        ["BlockListCount"] = "blocklist_count",
        ["BlockListCRN"] = "blocklist_crn_claimed",
        ["BlockListDateTime"] = "blocklist_datetime",
        ["BlockListDetail"] = "blocklist_detail",
        ["BlockListDNIS"] = "blocklist_dnis",
        ["BlockListRef"] = "blocklist_identity_ref",
        ["BlockListName"] = "blocklist_name",
        ["CardNumber"] = "card_number",
        ["ConsentStatus"] = "consent_status",
        ["CSPConsentOutcome"] = "consent_csp_outcome",
        ["C2CTokenOutcome"] = "c2c_token_outcome",
        ["C2CTransOutcome"] = "c2c_auth_outcome",
        ["CredentialOutcome"] = "credential_collection_outcome",
        ["CredentialStatus"] = "credential_status",
        ["CredentialStatusReason"] = "credential_status_reason",
        ["LockedReason"] = "credential_locked_reason",
        ["PreferredCredential"] = "credential_vpti_flag",
        ["RegLevel"] = "registration_level",
        ["SuspensionActive"] = "credential_suspension_active",
        ["SuspensionDateTime"] = "credential_suspension_datetime",
        ["Identification"] = "caller_identification",
        ["InvalidCAN"] = "error_invalid_can",
        ["TransmissionError"] = "error_transmission",
        ["ExpressPlusEnrolOutcome"] = "express_plus_enrol_outcome",
        ["IVRFlowOutcome"] = "ivr_flow_outcome",
        ["EnrolEligibility"] = "vb_enrol_eligibility",
        ["EnrolOutcome"] = "vb_enrol_outcome",
        ["StepUpOutcome"] = "vb_stepup_outcome",
        ["AudioAttempt1"] = "vb_audio_first_attempt",
        ["AudioRetry"] = "vb_audio_retry",
        ["AudioQuality"] = "vb_fail_poor_audio",
        ["VoiceID"] = "vb_speaker_id",
        ["SpeakerID"] = "vb_speaker_id",
        ["VPType"] = "vb_voiceprint_type",
        ["VerificationResult"] = "vb_verification_pass",
        ["SearchLocation"] = "office_search_location",
        ["OfficeLocation"] = "office_result_location",
        ["SMSSent"] = "sms_sent_flag",
        ["SensitivityType"] = "dva_sensitivity_type",
        ["TransactionOutcome"] = "transaction_outcome",
    };

    [GeneratedRegex(@"([a-z0-9])([A-Z])")]
    private static partial Regex CamelPattern1();

    [GeneratedRegex(@"([A-Z])([A-Z][a-z])")]
    private static partial Regex CamelPattern2();

    public static string ToReportingKey(string rawKey)
    {
        if (KvpReportingKeyMap.TryGetValue(rawKey, out var mapped))
            return mapped;

        // Fallback: PascalCase/camelCase → snake_case
        var s = CamelPattern1().Replace(rawKey, "$1_$2");
        s = CamelPattern2().Replace(s, "$1_$2");
        return s.ToLowerInvariant();
    }

    // ============================================================
    // Main enrichment entry point
    // ============================================================

    public static EnrichedCdrEvent EnrichEvent(CdrEvent raw)
    {
        var isRootFlow = raw.Flow.ParentStep == 0 && raw.Flow.Type == "Flow";

        var enrichment = new CdrEnrichment
        {
            ReceivedAt = DateTimeOffset.UtcNow.ToString("o"),
            WebhookVersion = WebhookVersion,
            Organization = raw.Flow.Organization,
            SessionGroup = raw.Group,
            MessageType = raw.MessageType,
            FlowName = raw.Flow.Name,
            FlowType = raw.Flow.Type,
            IsRootFlow = isRootFlow,
        };

        switch (raw.MessageType)
        {
            case "dialog_start":
                EnrichDialogStart(raw, enrichment);
                break;
            case "dialog_step":
                EnrichDialogStep(raw, enrichment);
                break;
            case "dialog_end":
                EnrichDialogEnd(raw, enrichment);
                break;
        }

        return new EnrichedCdrEvent { Raw = raw, Enrichment = enrichment };
    }

    // ============================================================
    // dialog_start enrichment (P1)
    // ============================================================

    private static void EnrichDialogStart(CdrEvent ev, CdrEnrichment e)
    {
        e.CallerCli = ev.OriginUri;
        e.Dnis = ev.DestinationUri;
        e.IvrChannel = ev.Channel;
    }

    // ============================================================
    // dialog_step enrichment (P0, P1)
    // ============================================================

    private static void EnrichDialogStep(CdrEvent ev, CdrEnrichment e)
    {
        // Has user input?
        e.HasUserInput = ev.Transcription is not null && ev.Transcription != "[hup]";

        // Caller utterance
        if (ev.OriginalUtterance is not null && ev.OriginalUtterance != "[hup]")
            e.CallerUtterance = ev.OriginalUtterance;

        // System prompt and message name (ASK/ANNOUNCEMENT only)
        if (ev.Action?.Type is "ASK" or "ANNOUNCEMENT")
        {
            e.SystemPrompt = ev.Action.Prompt;
            e.MessageName = ev.Action.Name;
            e.MessagePlayTime = ev.Time;
        }

        // Application tag
        if (ev.Action?.Tag is not null)
            e.ApplicationTag = ev.Action.Tag;

        // NLU result type
        if (ev.Nlu is not null)
            e.NluResultType = ev.Nlu.ResultType;

        // NLU intent and entities
        if (ev.Nlu?.SemanticInterpretation is { Count: > 0 } interps)
        {
            var entities = new Dictionary<string, string>();
            string? firstOptionSelected = null;
            string? intentName = null;

            foreach (var interp in interps)
            {
                // Intent
                if (interp.Intent.Name is not null && interp.Intent.Name != "undefined")
                    intentName = interp.Intent.Name;

                // Entities
                foreach (var entity in interp.Entities)
                {
                    if (entity.Instances.Count > 0)
                    {
                        entities[entity.Name] = entity.Instances[0].Value;

                        if (firstOptionSelected is null && !MetaEntityNames.Contains(entity.Name))
                            firstOptionSelected = entity.Instances[0].Value;
                    }
                }
            }

            if (entities.Count > 0) e.ExtractedEntities = entities;
            if (intentName is not null) e.NluIntent = intentName;
            if (firstOptionSelected is not null) e.OptionSelected = firstOptionSelected;
        }

        // Bio status
        if (ev.Bio?.SearchResult is not null)
            e.BioStatus = ev.Bio.SearchResult;

        // ── P0: Forward-compatible KVP extraction from events ────
        var stepKvps = new Dictionary<string, string>();
        var taskEvents = new List<TaskEventEntry>();
        string? exitReason = null;

        var stepEvents = ParseStepEvents(ev.Events);
        foreach (var evt in stepEvents)
        {
            var log = evt.Log;

            if (log.ExitReason is not null)
                exitReason = log.ExitReason;

            if (log.Task is not null && log.Status is not null)
            {
                taskEvents.Add(new TaskEventEntry
                {
                    Task = log.Task,
                    Status = log.Status,
                    FlowName = log.Name ?? "unknown",
                });
            }

            if (log.Event is not null && log.Event.Contains(':'))
            {
                var isSystem = SystemEventPrefixes.Any(p => log.Event.StartsWith(p));
                if (!isSystem)
                {
                    var colonIdx = log.Event.IndexOf(':');
                    var key = log.Event[..colonIdx];
                    var value = log.Event[(colonIdx + 1)..];
                    if (key.Length > 0 && value.Length > 0 && !key.Contains(' '))
                        stepKvps[key] = value;
                }
            }
        }

        if (exitReason is not null) e.ExitReason = exitReason;
        if (taskEvents.Count > 0) e.TaskEvents = taskEvents;
        if (stepKvps.Count > 0) e.StepKvps = stepKvps;
    }

    // ============================================================
    // dialog_end enrichment (P2, P3, P4)
    // ============================================================

    private static void EnrichDialogEnd(CdrEvent ev, CdrEnrichment e)
    {
        var isRootFlow = ev.Flow.ParentStep == 0 && ev.Flow.Type == "Flow";

        var endTimeMs = ev.Time + ev.Duration;
        e.ComputedEndTime = endTimeMs;
        e.ComputedEndTimeIso = DateTimeOffset.FromUnixTimeMilliseconds(endTimeMs).ToString("o");
        e.EndTypeValue = ev.EndType;

        // Flatten business KVPs
        if (ev.Kvps is { Count: > 0 })
        {
            var businessKvps = new Dictionary<string, string>();
            var reportingKvps = new Dictionary<string, string>();

            foreach (var kvp in ev.Kvps)
            {
                if (!SystemKvpKeys.Contains(kvp.Key))
                {
                    businessKvps[kvp.Key] = kvp.Value;
                    reportingKvps[ToReportingKey(kvp.Key)] = kvp.Value;
                }
            }

            if (businessKvps.Count > 0)
            {
                e.BusinessKvps = businessKvps;
                e.ReportingKvps = reportingKvps;
            }
        }

        // P4: can_not_collected_flag (root flow only)
        if (isRootFlow)
        {
            var hasCan = ev.Kvps?.Any(k => k.Key == "CAN") ?? false;
            e.CanNotCollectedFlag = !hasCan;
        }

        // P3: Containment outcome
        var eventsString = GetEventsString(ev.Events);
        e.ContainmentOutcome = ClassifyOutcome(ev.EndType ?? "", eventsString);

        // P4: Exit code
        e.ExitCode = DeriveExitCode(ev.EndType ?? "", eventsString);
    }

    // ============================================================
    // P3: Containment outcome classification
    // ============================================================

    private static string ClassifyOutcome(string endType, string events)
    {
        return endType switch
        {
            "NEAR_HUP" or "NORMAL" => "completed",
            "FAR_HUP" when events.Contains("authentication:failed") => "abandoned_auth_failed",
            "FAR_HUP" when events.Contains("auxiliary:failed") => "abandoned_auxiliary_failed",
            "FAR_HUP" => "abandoned",
            "TRANSFER" => "transferred",
            _ => "unknown",
        };
    }

    // ============================================================
    // P4: Exit code derivation
    // ============================================================

    private static string DeriveExitCode(string endType, string events)
    {
        return endType switch
        {
            "TRANSFER" => "TRANSFER",
            "NORMAL" => "NORMAL",
            "NEAR_HUP" when events.Contains("authentication:completed") => "NEAR_HUP_AUTH_PASS",
            "NEAR_HUP" => "NEAR_HUP",
            "FAR_HUP" when events.Contains("authentication:failed") => "FAR_HUP_AUTH_FAILED",
            "FAR_HUP" when events.Contains("auxiliary:failed") => "FAR_HUP_AUX_FAILED",
            "FAR_HUP" => "FAR_HUP",
            _ => string.IsNullOrEmpty(endType) ? "UNKNOWN" : endType,
        };
    }

    // ============================================================
    // Helpers for the Events field (array for step, string for end)
    // ============================================================

    private static List<StepEvent> ParseStepEvents(object? events)
    {
        if (events is null) return new List<StepEvent>();
        if (events is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.Deserialize<List<StepEvent>>() ?? new List<StepEvent>();
        return new List<StepEvent>();
    }

    private static string GetEventsString(object? events)
    {
        if (events is null) return "";
        if (events is JsonElement je && je.ValueKind == JsonValueKind.String)
            return je.GetString() ?? "";
        if (events is string s) return s;
        return "";
    }

    // ============================================================
    // Partition key
    // ============================================================

    public static string GetPartitionKey(CdrEvent ev) => ev.Group;
}
