using System.Text.Json;
using OmiliaWebhook.Enrichment;
using OmiliaWebhook.Types;
using Xunit;

namespace OmiliaWebhook.Tests;

public class EnrichmentTests
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = false };

    // ── Helpers ─────────────────────────────────────────────────

    private static CdrEvent MakeDialogStart() => new()
    {
        App = new AppInfo { Name = "b49b79ed.SND_BG_VB_Auth.Flow.Optus_Demo@SND_BG_VB_Auth.Optus_Demo", Version = 498 },
        OriginUri = "undefined",
        Channel = "web-chat",
        SessionId = "f99adb4a-3ac8-527a-9093-6d18807b1378",
        MessageType = "dialog_start",
        DestinationUri = "undefined",
        Diamant = new DiamantInfo { Id = "us1-m-aws-flows-sandbox1", Region = "aws-us-east-1", Version = "10.39.0" },
        Time = 1772596513117,
        Id = "db6bdc4ca13ce3649233562e8af006472437b05a.1772596513117.0712df1eb744449ca6f8021236226e67",
        Flow = new FlowInfo
        {
            AccountId = "b49b79ed-6dbc-48c8-aa43-e980f341ee96", ParentStep = 0,
            Organization = "Optus_Demo", Name = "SND_BG_VB_Auth", Type = "Flow",
            Id = "b49b79ed.SND_BG_VB_Auth", Uuid = "bfd379", DiamantAppName = "Optus_Demo@SND_BG_VB_Auth",
        },
        Group = "db6bdc4ca13ce3649233562e8af006472437b05a.1772596513117.0712df1eb744449ca6f8021236226e67",
        Direction = "inbound",
    };

    private static CdrEvent MakeDialogStepWithNlu()
    {
        var json = """
        {
            "app": {"name": "b49b79ed.Auth_Collect_CAN.Alphanumeric.MiniApps.Optus_Demo", "version": 2039000},
            "original_utterance": "123456789t", "transcription": "123456789t",
            "input_modality": "text", "channel": "web-chat",
            "session_id": "f99adb4a-3ac8-527a-9093-6d18807b1378",
            "bio": {},
            "message_type": "dialog_step", "locale": "en-AU",
            "diamant": {"id": "us1-m-aws-flows-sandbox1", "region": "aws-us-east-1", "version": "10.39.0"},
            "nlu": {
                "result_type": "RECOGNITION", "result_count": 1,
                "semantic_interpretation": [{
                    "entities": [{"instances": [{"confidence": 100.0, "end": 0, "begin": 0, "value": "123456789t"}], "sensitivity_level": "None", "name": "Alphanumeric"}],
                    "confidence": 100.0,
                    "intent": {"confidence": 0.0, "name": "undefined", "status": "Undefined"},
                    "utterance": "123456789t"
                }]
            },
            "action": {"name": "ExitAction:GET_ALPHANUMERIC", "type": "EXIT", "timeout": 5000, "prompt_duration": 0},
            "time": 1772596519173,
            "id": "0294c4709c00982ea9aa07b13168fc9ce33217a9.1772596513122.d5a0dd89cca046faabc9537d83de4afd",
            "test_flag": false,
            "events": [
                {"log": {"event": "globalResult:success"}, "index": 1, "active_intent": "undefined"},
                {"log": {"exit_reason": "NEAR", "event": "HUP"}, "index": 2, "active_intent": "undefined"}
            ],
            "flow": {
                "parent_name": "Optus_Demo@SND_BG_VB_Auth", "parent_step": 2,
                "type": "Alphanumeric", "uuid": "Optus_Demo",
                "account_id": "b49b79ed-6dbc-48c8-aa43-e980f341ee96",
                "diamant_app_name": "MiniApps", "organization": "Optus_Demo",
                "name": "Auth_Collect_CAN",
                "id": "b49b79ed.Auth_Collect_CAN.Alphanumeric.MiniApps.Optus_Demo",
                "root_step": 2, "reportable": false
            },
            "group": "db6bdc4ca13ce3649233562e8af006472437b05a.1772596513117.0712df1eb744449ca6f8021236226e67"
        }
        """;
        return JsonSerializer.Deserialize<CdrEvent>(json)!;
    }

    private static CdrEvent MakeDialogStepWithPrompt()
    {
        var json = """
        {
            "app": {"name": "9f09bb6d.AT_UP_LongReasonDescYesNo.YesNo.MiniApps.Optus_Demo", "version": 2039000},
            "original_utterance": "no", "transcription": "no",
            "input_modality": "text", "channel": "web-chat",
            "session_id": "f99adb4a-3ac8-527a-9093-6d18807b1378",
            "bio": {"search_result": "NO_USER_FOUND/0"},
            "message_type": "dialog_step", "locale": "en-AU",
            "diamant": {"id": "us1-m-aws-flows-sandbox2", "region": "aws-us-east-1", "version": "10.39.0"},
            "nlu": {
                "result_type": "RECOGNITION", "result_count": 1,
                "semantic_interpretation": [{
                    "entities": [
                        {"instances": [{"confidence": 100.0, "end": 2, "begin": 0, "value": "REJECT"}], "sensitivity_level": "None", "name": "DIALOGACT"},
                        {"instances": [{"confidence": 100.0, "end": 2, "begin": 0, "value": "null"}], "sensitivity_level": "None", "name": "REJECT"},
                        {"instances": [{"confidence": 100.0, "end": 0, "begin": 0, "value": "NO"}], "sensitivity_level": "None", "name": "YesNo"}
                    ],
                    "confidence": 100.0,
                    "intent": {"confidence": 0.0, "name": "undefined", "status": "Undefined"},
                    "utterance": "no"
                }]
            },
            "action": {"name": "ExitAction:GET_YESNO", "type": "EXIT", "timeout": 5000, "prompt_duration": 0},
            "time": 1772596523922,
            "id": "008546820fad5e9429877581dbf217ee28802701",
            "test_flag": false,
            "events": [
                {"log": {"event": "globalResult:success"}, "index": 1, "active_intent": "undefined"},
                {"log": {"exit_reason": "NEAR", "event": "HUP"}, "index": 2, "active_intent": "undefined"}
            ],
            "flow": {
                "parent_step": 4, "type": "YesNo", "uuid": "Optus_Demo",
                "account_id": "9f09bb6d", "diamant_app_name": "MiniApps",
                "organization": "Optus_Demo", "name": "AT_UP_LongReasonDescYesNo",
                "id": "9f09bb6d.AT_UP_LongReasonDescYesNo", "root_step": 4, "reportable": false
            },
            "group": "db6bdc4ca13ce3649233562e8af006472437b05a.1772596513117.0712df1eb744449ca6f8021236226e67"
        }
        """;
        return JsonSerializer.Deserialize<CdrEvent>(json)!;
    }

    private static CdrEvent MakeDialogStepRootWithTasks()
    {
        var json = """
        {
            "app": {"name": "b49b79ed.SND_BG_VB_Auth.Flow.Optus_Demo@SND_BG_VB_Auth.Optus_Demo", "version": 498},
            "input_modality": "text", "channel": "web-chat",
            "session_id": "f99adb4a-3ac8-527a-9093-6d18807b1378",
            "bio": {},
            "message_type": "dialog_step", "locale": "en-AU",
            "diamant": {"id": "us1-m-aws-flows-sandbox1", "region": "aws-us-east-1", "version": "10.39.0"},
            "nlu": {"result_type": "RECOGNITION", "result_count": 0},
            "action": {
                "name": "Get_Alphanumeric", "type": "ASK",
                "prompt": "Please tell me your Customer Access Number. If you have one you may know this as your Customer Reference Number.",
                "timeout": 5000, "prompt_duration": 0
            },
            "time": 1772596513118,
            "id": "db6bdc4ca13ce3649233562e8af006472437b05a.1772596513117.0712df1eb744449ca6f8021236226e67",
            "test_flag": false,
            "events": [
                {"log": {"task": "auxiliary", "name": "b49b79ed.SND_BG_VB_Auth.Flow...", "status": "initiated"}, "index": 1, "active_intent": "undefined"},
                {"log": {"event": "DialogGroupID:db6bdc4ca13ce364..."}, "index": 2, "active_intent": "undefined"},
                {"log": {"event": "FlowName:MiniApps"}, "index": 3, "active_intent": "undefined"},
                {"log": {"event": "appId:b49b79ed..."}, "index": 4, "active_intent": "undefined"},
                {"log": {"event": "requestId:undefined"}, "index": 5, "active_intent": "undefined"},
                {"log": {"event": "testMode:false"}, "index": 6, "active_intent": "undefined"},
                {"log": {"event": "Locale:en-AU"}, "index": 7, "active_intent": "undefined"},
                {"log": {"event": "Get_Alphanumeric:InformationAsked"}, "index": 8, "active_intent": "undefined"}
            ],
            "flow": {
                "account_id": "b49b79ed", "parent_step": 0,
                "diamant_app_name": "Optus_Demo@SND_BG_VB_Auth",
                "organization": "Optus_Demo", "name": "SND_BG_VB_Auth",
                "id": "b49b79ed.SND_BG_VB_Auth", "root_step": 0,
                "type": "Flow", "uuid": "bfd379", "reportable": false
            },
            "group": "db6bdc4ca13ce3649233562e8af006472437b05a.1772596513117.0712df1eb744449ca6f8021236226e67"
        }
        """;
        return JsonSerializer.Deserialize<CdrEvent>(json)!;
    }

    private static CdrEvent MakeDialogEnd(string? endType = null, string? events = null, FlowInfo? flow = null, List<Kvp>? kvps = null)
    {
        var ev = new CdrEvent
        {
            App = new AppInfo { Name = "b49b79ed.SND_BG_VB_Auth.Flow.Optus_Demo@SND_BG_VB_Auth.Optus_Demo", Version = 498 },
            SessionId = "f99adb4a-3ac8-527a-9093-6d18807b1378",
            MessageType = "dialog_end",
            Channel = "web-chat",
            Diamant = new DiamantInfo { Id = "us1-m-aws-flows-sandbox1", Region = "aws-us-east-1", Version = "10.39.0" },
            Time = 1772596513117,
            Id = "db6bdc4ca13ce3649233562e8af006472437b05a.1772596513117.0712df1eb744449ca6f8021236226e67",
            Flow = flow ?? new FlowInfo
            {
                AccountId = "b49b79ed", ParentStep = 0, Organization = "Optus_Demo",
                Name = "SND_BG_VB_Auth", Type = "Flow", Id = "b49b79ed.SND_BG_VB_Auth",
                Uuid = "bfd379", DiamantAppName = "Optus_Demo@SND_BG_VB_Auth",
            },
            Group = "db6bdc4ca13ce3649233562e8af006472437b05a.1772596513117.0712df1eb744449ca6f8021236226e67",
            EndType = endType ?? "FAR_HUP",
            Duration = 20253,
            Events = events ?? "auxiliary:initiated:...,globalResult:success,HUP:NEAR,authentication:initiated:...,HUP:FAR,authentication:failed:...,auxiliary:failed:...",
            Kvps = kvps ?? new List<Kvp>
            {
                new() { Step = 1, Value = "b49b79ed...Auth_Collect_CAN...", Key = "appId" },
                new() { Step = 1, Value = "false", Key = "testMode" },
                new() { Step = 1, Value = "en-AU", Key = "Locale" },
                new() { Step = 2, Value = "text", Key = "CANInputMode" },
                new() { Step = 2, Value = "123456789t", Key = "CAN" },
                new() { Step = 2, Value = "123456789t", Key = "BG_Testing" },
                new() { Step = 4, Value = "NO", Key = "ConfirmEnrolment" },
            },
        };
        return ev;
    }

    // ── dialog_start ────────────────────────────────────────────

    [Fact]
    public void DialogStart_BaseFields()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogStart());
        var e = enriched.Enrichment;
        Assert.Equal("dialog_start", e.MessageType);
        Assert.True(e.IsRootFlow);
        Assert.Equal("Optus_Demo", e.Organization);
        Assert.Equal("SND_BG_VB_Auth", e.FlowName);
        Assert.NotEmpty(e.ReceivedAt);
    }

    [Fact]
    public void DialogStart_P1Metrics()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogStart());
        var e = enriched.Enrichment;
        Assert.Equal("undefined", e.CallerCli);
        Assert.Equal("undefined", e.Dnis);
        Assert.Equal("web-chat", e.IvrChannel);
    }

    // ── dialog_step NLU ─────────────────────────────────────────

    [Fact]
    public void DialogStep_NluBaseFields()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogStepWithNlu());
        var e = enriched.Enrichment;
        Assert.Equal("dialog_step", e.MessageType);
        Assert.False(e.IsRootFlow);
        Assert.True(e.HasUserInput);
        Assert.Equal("RECOGNITION", e.NluResultType);
        Assert.Equal("123456789t", e.ExtractedEntities!["Alphanumeric"]);
        Assert.Equal("NEAR", e.ExitReason);
        Assert.Equal("Auth_Collect_CAN", e.FlowName);
    }

    [Fact]
    public void DialogStep_P1StepMetrics()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogStepWithNlu());
        var e = enriched.Enrichment;
        Assert.Equal("123456789t", e.CallerUtterance);
        Assert.Equal("123456789t", e.OptionSelected);
        Assert.Null(e.SystemPrompt);
        Assert.Null(e.MessageName);
    }

    // ── dialog_step meta entities ───────────────────────────────

    [Fact]
    public void DialogStep_OptionSelectedSkipsMeta()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogStepWithPrompt());
        var e = enriched.Enrichment;
        Assert.Equal("no", e.CallerUtterance);
        Assert.Equal("NO", e.OptionSelected);
        Assert.Equal("REJECT", e.ExtractedEntities!["DIALOGACT"]);
        Assert.Equal("NO", e.ExtractedEntities!["YesNo"]);
        Assert.Equal("NO_USER_FOUND/0", e.BioStatus);
    }

    // ── dialog_step ASK action ──────────────────────────────────

    [Fact]
    public void DialogStep_AskActionPromptFields()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogStepRootWithTasks());
        var e = enriched.Enrichment;
        Assert.True(e.IsRootFlow);
        Assert.Equal(
            "Please tell me your Customer Access Number. If you have one you may know this as your Customer Reference Number.",
            e.SystemPrompt);
        Assert.Equal("Get_Alphanumeric", e.MessageName);
        Assert.Equal(1772596513118L, e.MessagePlayTime);
    }

    [Fact]
    public void DialogStep_TaskEvents()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogStepRootWithTasks());
        var e = enriched.Enrichment;
        Assert.Single(e.TaskEvents!);
        Assert.Equal("auxiliary", e.TaskEvents![0].Task);
        Assert.Equal("initiated", e.TaskEvents![0].Status);
    }

    // ── dialog_step KVPs ────────────────────────────────────────

    [Fact]
    public void DialogStep_SystemEventsFiltered()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogStepRootWithTasks());
        var kvps = enriched.Enrichment.StepKvps!;
        Assert.False(kvps.ContainsKey("DialogGroupID"));
        Assert.False(kvps.ContainsKey("FlowName"));
        Assert.False(kvps.ContainsKey("appId"));
        Assert.False(kvps.ContainsKey("testMode"));
        Assert.False(kvps.ContainsKey("Locale"));
        Assert.False(kvps.ContainsKey("requestId"));
    }

    [Fact]
    public void DialogStep_BusinessEventsCaptured()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogStepRootWithTasks());
        Assert.Equal("InformationAsked", enriched.Enrichment.StepKvps!["Get_Alphanumeric"]);
    }

    // ── dialog_step HUP ─────────────────────────────────────────

    [Fact]
    public void DialogStep_HupHandling()
    {
        var ev = MakeDialogStepWithNlu();
        ev.Transcription = "[hup]";
        ev.OriginalUtterance = "[hup]";
        ev.Nlu = new NluInfo { ResultType = "HUP", ResultCount = 0 };

        var enriched = Enricher.EnrichEvent(ev);
        var e = enriched.Enrichment;
        Assert.False(e.HasUserInput);
        Assert.Null(e.CallerUtterance);
        Assert.Equal("HUP", e.NluResultType);
    }

    // ── dialog_end ──────────────────────────────────────────────

    [Fact]
    public void DialogEnd_BaseAndKvps()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogEnd());
        var e = enriched.Enrichment;
        Assert.Equal("dialog_end", e.MessageType);
        Assert.True(e.IsRootFlow);
        Assert.Equal("FAR_HUP", e.EndTypeValue);
        Assert.Equal(1772596513117L + 20253L, e.ComputedEndTime);
        Assert.NotNull(e.ComputedEndTimeIso);
    }

    [Fact]
    public void DialogEnd_P3Containment()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogEnd());
        Assert.Equal("abandoned_auth_failed", enriched.Enrichment.ContainmentOutcome);
    }

    [Fact]
    public void DialogEnd_BusinessKvps()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogEnd());
        var e = enriched.Enrichment;
        Assert.Equal("123456789t", e.BusinessKvps!["CAN"]);
        Assert.Equal("123456789t", e.BusinessKvps!["BG_Testing"]);
        Assert.Equal("NO", e.BusinessKvps!["ConfirmEnrolment"]);
        Assert.Equal("text", e.BusinessKvps!["CANInputMode"]);
        Assert.False(e.BusinessKvps!.ContainsKey("appId"));
        Assert.False(e.BusinessKvps!.ContainsKey("testMode"));
        Assert.False(e.BusinessKvps!.ContainsKey("Locale"));
    }

    [Fact]
    public void DialogEnd_P2ReportingKvps()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogEnd());
        var e = enriched.Enrichment;
        Assert.Equal("123456789t", e.ReportingKvps!["can"]);
        Assert.Equal("123456789t", e.ReportingKvps!["bg_testing"]);
        Assert.Equal("NO", e.ReportingKvps!["consent_ivr_enrol"]);
        Assert.Equal("text", e.ReportingKvps!["can_input_mode"]);
        Assert.False(e.ReportingKvps!.ContainsKey("app_id"));
    }

    [Fact]
    public void DialogEnd_P4ExitCode()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogEnd());
        Assert.Equal("FAR_HUP_AUTH_FAILED", enriched.Enrichment.ExitCode);
    }

    [Fact]
    public void DialogEnd_P4CanNotCollected()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogEnd());
        Assert.False(enriched.Enrichment.CanNotCollectedFlag);
    }

    // ── dialog_end variants ─────────────────────────────────────

    [Fact]
    public void DialogEnd_NearHupAuthPass()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogEnd(
            endType: "NEAR_HUP",
            events: "auxiliary:initiated:...,authentication:initiated:...,authentication:completed:...,HUP:NEAR"));
        var e = enriched.Enrichment;
        Assert.Equal("completed", e.ContainmentOutcome);
        Assert.Equal("NEAR_HUP_AUTH_PASS", e.ExitCode);
    }

    [Fact]
    public void DialogEnd_Transfer()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogEnd(
            endType: "TRANSFER", events: "auxiliary:initiated:...,TRANSFER"));
        var e = enriched.Enrichment;
        Assert.Equal("transferred", e.ContainmentOutcome);
        Assert.Equal("TRANSFER", e.ExitCode);
    }

    [Fact]
    public void DialogEnd_ChildFlow()
    {
        var childFlow = new FlowInfo
        {
            AccountId = "b49b79ed", ParentStep = 2, Organization = "Optus_Demo",
            Name = "SND_BG_VB_Auth", Type = "Alphanumeric", Id = "b49b79ed.SND_BG_VB_Auth",
            Uuid = "bfd379", DiamantAppName = "Optus_Demo@SND_BG_VB_Auth",
        };
        var enriched = Enricher.EnrichEvent(MakeDialogEnd(
            flow: childFlow,
            kvps: new List<Kvp>
            {
                new() { Step = 0, Value = "b49b79ed...", Key = "appId" },
                new() { Step = 0, Value = "false", Key = "testMode" },
                new() { Step = 0, Value = "en-AU", Key = "Locale" },
            }));
        var e = enriched.Enrichment;
        Assert.False(e.IsRootFlow);
        Assert.Null(e.CanNotCollectedFlag);
        Assert.Null(e.BusinessKvps);
        Assert.Null(e.ReportingKvps);
    }

    [Fact]
    public void DialogEnd_CanNotCollected()
    {
        var enriched = Enricher.EnrichEvent(MakeDialogEnd(
            kvps: new List<Kvp>
            {
                new() { Step = 1, Value = "b49b79ed...", Key = "appId" },
                new() { Step = 1, Value = "false", Key = "testMode" },
                new() { Step = 1, Value = "en-AU", Key = "Locale" },
                new() { Step = 3, Value = "PASS", Key = "TransactionOutcome" },
            }));
        var e = enriched.Enrichment;
        Assert.True(e.CanNotCollectedFlag);
        Assert.Equal("PASS", e.ReportingKvps!["transaction_outcome"]);
    }

    // ── Partition key ───────────────────────────────────────────

    [Fact]
    public void PartitionKey_EqualsGroup()
    {
        var ev = MakeDialogStart();
        Assert.Equal(ev.Group, Enricher.GetPartitionKey(ev));
    }

    [Fact]
    public void PartitionKey_SameAcrossTypes()
    {
        var key1 = Enricher.GetPartitionKey(MakeDialogStart());
        var key2 = Enricher.GetPartitionKey(MakeDialogStepWithNlu());
        Assert.Equal(key1, key2);
    }
}
