using System.Text.Json.Serialization;

namespace OmiliaWebhook.Types;

// ============================================================
// Common types shared across all message types
// ============================================================

public class AppInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("version")] public int Version { get; set; }
}

public class DiamantInfo
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("region")] public string Region { get; set; } = "";
    [JsonPropertyName("version")] public string Version { get; set; } = "";
}

public class FlowInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("account_id")] public string AccountId { get; set; } = "";
    [JsonPropertyName("organization")] public string Organization { get; set; } = "";
    [JsonPropertyName("uuid")] public string Uuid { get; set; } = "";
    [JsonPropertyName("diamant_app_name")] public string DiamantAppName { get; set; } = "";
    [JsonPropertyName("reportable")] public bool Reportable { get; set; }
    [JsonPropertyName("parent_name")] public string? ParentName { get; set; }
    [JsonPropertyName("parent_id")] public string? ParentId { get; set; }
    [JsonPropertyName("parent_step")] public int ParentStep { get; set; }
    [JsonPropertyName("root_step")] public int RootStep { get; set; }
    [JsonPropertyName("path")] public List<string>? Path { get; set; }
    [JsonPropertyName("path_app_ids")] public List<string>? PathAppIds { get; set; }
    [JsonPropertyName("ocp_group_name")] public string? OcpGroupName { get; set; }
    [JsonPropertyName("ocp_organization_id")] public string? OcpOrganizationId { get; set; }
}

// ============================================================
// NLU types (dialog_step)
// ============================================================

public class EntityInstance
{
    [JsonPropertyName("value")] public string Value { get; set; } = "";
    [JsonPropertyName("confidence")] public double Confidence { get; set; }
    [JsonPropertyName("begin")] public int Begin { get; set; }
    [JsonPropertyName("end")] public int End { get; set; }
}

public class NluEntity
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("sensitivity_level")] public string SensitivityLevel { get; set; } = "";
    [JsonPropertyName("instances")] public List<EntityInstance> Instances { get; set; } = new();
}

public class NluIntent
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("confidence")] public double Confidence { get; set; }
}

public class SemanticInterpretation
{
    [JsonPropertyName("utterance")] public string Utterance { get; set; } = "";
    [JsonPropertyName("confidence")] public double Confidence { get; set; }
    [JsonPropertyName("intent")] public NluIntent Intent { get; set; } = new();
    [JsonPropertyName("entities")] public List<NluEntity> Entities { get; set; } = new();
}

public class NluInfo
{
    [JsonPropertyName("result_type")] public string ResultType { get; set; } = "";
    [JsonPropertyName("result_count")] public int ResultCount { get; set; }
    [JsonPropertyName("work_flow_id")] public string? WorkFlowId { get; set; }
    [JsonPropertyName("nlu_app")] public string? NluApp { get; set; }
    [JsonPropertyName("semantic_interpretation")] public List<SemanticInterpretation>? SemanticInterpretation { get; set; }
}

public class ActionInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("subtype")] public string? Subtype { get; set; }
    [JsonPropertyName("prompt")] public string? Prompt { get; set; }
    [JsonPropertyName("tag")] public string? Tag { get; set; }
    [JsonPropertyName("timeout")] public int? Timeout { get; set; }
    [JsonPropertyName("prompt_duration")] public int? PromptDuration { get; set; }
}

public class BioInfo
{
    [JsonPropertyName("search_result")] public string? SearchResult { get; set; }
}

public class StepEventLog
{
    [JsonPropertyName("event")] public string? Event { get; set; }
    [JsonPropertyName("exit_reason")] public string? ExitReason { get; set; }
    [JsonPropertyName("task")] public string? Task { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
}

public class StepEvent
{
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("active_intent")] public string ActiveIntent { get; set; } = "";
    [JsonPropertyName("log")] public StepEventLog Log { get; set; } = new();
    [JsonPropertyName("task_id")] public string? TaskId { get; set; }
}

public class Kvp
{
    [JsonPropertyName("key")] public string Key { get; set; } = "";
    [JsonPropertyName("value")] public string Value { get; set; } = "";
    [JsonPropertyName("step")] public int Step { get; set; }
}

// ============================================================
// CDR Event (unified with JsonExtensionData for flexible parsing)
// ============================================================

public class CdrEvent
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("group")] public string Group { get; set; } = "";
    [JsonPropertyName("session_id")] public string SessionId { get; set; } = "";
    [JsonPropertyName("message_type")] public string MessageType { get; set; } = "";
    [JsonPropertyName("time")] public long Time { get; set; }
    [JsonPropertyName("test_flag")] public bool TestFlag { get; set; }
    [JsonPropertyName("channel")] public string? Channel { get; set; }
    [JsonPropertyName("app")] public AppInfo App { get; set; } = new();
    [JsonPropertyName("diamant")] public DiamantInfo Diamant { get; set; } = new();
    [JsonPropertyName("flow")] public FlowInfo Flow { get; set; } = new();

    // dialog_start fields
    [JsonPropertyName("origin_uri")] public string? OriginUri { get; set; }
    [JsonPropertyName("destination_uri")] public string? DestinationUri { get; set; }
    [JsonPropertyName("direction")] public string? Direction { get; set; }

    // dialog_step fields
    [JsonPropertyName("transcription")] public string? Transcription { get; set; }
    [JsonPropertyName("original_utterance")] public string? OriginalUtterance { get; set; }
    [JsonPropertyName("nlu")] public NluInfo? Nlu { get; set; }
    [JsonPropertyName("action")] public ActionInfo? Action { get; set; }
    [JsonPropertyName("bio")] public BioInfo? Bio { get; set; }
    [JsonPropertyName("events")] public object? Events { get; set; } // List<StepEvent> for step, string for end

    // dialog_end fields
    [JsonPropertyName("end_type")] public string? EndType { get; set; }
    [JsonPropertyName("duration")] public long Duration { get; set; }
    [JsonPropertyName("kvps")] public List<Kvp>? Kvps { get; set; }
}

// ============================================================
// Enriched event (what we send to Kinesis)
// ============================================================

public class TaskEventEntry
{
    [JsonPropertyName("task")] public string Task { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("flow_name")] public string FlowName { get; set; } = "";
}

public class CdrEnrichment
{
    [JsonPropertyName("received_at")] public string ReceivedAt { get; set; } = "";
    [JsonPropertyName("webhook_version")] public string WebhookVersion { get; set; } = "";
    [JsonPropertyName("organization")] public string Organization { get; set; } = "";
    [JsonPropertyName("session_group")] public string SessionGroup { get; set; } = "";
    [JsonPropertyName("message_type")] public string MessageType { get; set; } = "";
    [JsonPropertyName("flow_name")] public string FlowName { get; set; } = "";
    [JsonPropertyName("flow_type")] public string FlowType { get; set; } = "";
    [JsonPropertyName("is_root_flow")] public bool IsRootFlow { get; set; }

    // dialog_start
    [JsonPropertyName("caller_cli")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? CallerCli { get; set; }
    [JsonPropertyName("dnis")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Dnis { get; set; }
    [JsonPropertyName("ivr_channel")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? IvrChannel { get; set; }

    // dialog_step
    [JsonPropertyName("has_user_input")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? HasUserInput { get; set; }
    [JsonPropertyName("caller_utterance")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? CallerUtterance { get; set; }
    [JsonPropertyName("system_prompt")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? SystemPrompt { get; set; }
    [JsonPropertyName("message_name")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? MessageName { get; set; }
    [JsonPropertyName("message_play_time")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public long? MessagePlayTime { get; set; }
    [JsonPropertyName("application_tag")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ApplicationTag { get; set; }
    [JsonPropertyName("nlu_result_type")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? NluResultType { get; set; }
    [JsonPropertyName("nlu_intent")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? NluIntent { get; set; }
    [JsonPropertyName("option_selected")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? OptionSelected { get; set; }
    [JsonPropertyName("extracted_entities")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Dictionary<string, string>? ExtractedEntities { get; set; }
    [JsonPropertyName("bio_status")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? BioStatus { get; set; }
    [JsonPropertyName("exit_reason")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ExitReason { get; set; }
    [JsonPropertyName("task_events")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<TaskEventEntry>? TaskEvents { get; set; }
    [JsonPropertyName("step_kvps")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Dictionary<string, string>? StepKvps { get; set; }

    // dialog_end
    [JsonPropertyName("end_type")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? EndTypeValue { get; set; }
    [JsonPropertyName("computed_end_time")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public long? ComputedEndTime { get; set; }
    [JsonPropertyName("computed_end_time_iso")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ComputedEndTimeIso { get; set; }
    [JsonPropertyName("business_kvps")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Dictionary<string, string>? BusinessKvps { get; set; }
    [JsonPropertyName("reporting_kvps")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Dictionary<string, string>? ReportingKvps { get; set; }
    [JsonPropertyName("containment_outcome")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ContainmentOutcome { get; set; }
    [JsonPropertyName("exit_code")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ExitCode { get; set; }
    [JsonPropertyName("can_not_collected_flag")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? CanNotCollectedFlag { get; set; }
}

public class EnrichedCdrEvent
{
    [JsonPropertyName("raw")] public CdrEvent Raw { get; set; } = new();
    [JsonPropertyName("enrichment")] public CdrEnrichment Enrichment { get; set; } = new();
}
