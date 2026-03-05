using System.Text;
using System.Text.Json;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using OmiliaWebhook.Enrichment;
using OmiliaWebhook.Types;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OmiliaWebhook.Handlers;

public class WebhookHandler
{
    private static readonly string KinesisStreamName = Environment.GetEnvironmentVariable("KINESIS_STREAM_NAME") ?? "";
    private static readonly string? WebhookSecret = Environment.GetEnvironmentVariable("WEBHOOK_SECRET");
    private static readonly HashSet<string> ValidMessageTypes = new() { "dialog_start", "dialog_step", "dialog_end" };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly IAmazonKinesis _kinesis;

    public WebhookHandler() : this(new AmazonKinesisClient()) { }

    public WebhookHandler(IAmazonKinesis kinesis)
    {
        _kinesis = kinesis;
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> Handler(
        APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        var requestId = request.RequestContext?.RequestId ?? "unknown";

        try
        {
            // ── Auth ─────────────────────────────────────────
            if (WebhookSecret is not null)
            {
                string? secret = null;
                request.Headers?.TryGetValue("x-webhook-secret", out secret);
                if (secret != WebhookSecret)
                {
                    context.Logger.LogWarning($"[{requestId}] Auth failed");
                    return Respond(401, new { error = "Unauthorized" });
                }
            }

            // ── Parse body ───────────────────────────────────
            if (string.IsNullOrEmpty(request.Body))
                return Respond(400, new { error = "Empty body" });

            var body = request.IsBase64Encoded
                ? Encoding.UTF8.GetString(Convert.FromBase64String(request.Body))
                : request.Body;

            var records = ParseBody(body);
            if (records.Count == 0)
                return Respond(400, new { error = "No valid CDR records in body" });

            // ── Validate & Enrich ────────────────────────────
            var kinesisRecords = new List<PutRecordsRequestEntry>();

            foreach (var raw in records)
            {
                if (!IsValidCdrEvent(raw))
                {
                    context.Logger.LogWarning(
                        $"[{requestId}] Skipping invalid record: {raw.MessageType} / {raw.Id}");
                    continue;
                }

                var enriched = Enricher.EnrichEvent(raw);
                var json = JsonSerializer.Serialize(enriched, JsonOptions);

                kinesisRecords.Add(new PutRecordsRequestEntry
                {
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(json)),
                    PartitionKey = Enricher.GetPartitionKey(raw),
                });
            }

            if (kinesisRecords.Count == 0)
                return Respond(400, new { error = "No valid records after validation" });

            // ── Send to Kinesis ──────────────────────────────
            var (successCount, failedCount) = await SendToKinesis(kinesisRecords, requestId, context);

            context.Logger.LogInformation(
                $"[{requestId}] Processed: received={records.Count} sent={kinesisRecords.Count} failed={failedCount}");

            if (failedCount > 0)
            {
                return Respond(207, new
                {
                    accepted = successCount,
                    failed = failedCount,
                    message = "Partial delivery — some records failed to write to stream",
                });
            }

            return Respond(200, new { accepted = kinesisRecords.Count, message = "OK" });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"[{requestId}] Unhandled error: {ex}");
            return Respond(500, new { error = "Internal server error" });
        }
    }

    // ============================================================
    // Body parsing
    // ============================================================

    private static List<CdrEvent> ParseBody(string body)
    {
        var trimmed = body.Trim();

        // Try single JSON object or array
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return JsonSerializer.Deserialize<List<CdrEvent>>(trimmed) ?? new();
            var single = JsonSerializer.Deserialize<CdrEvent>(trimmed);
            return single is not null ? new List<CdrEvent> { single } : new();
        }
        catch (JsonException) { }

        // NDJSON
        var records = new List<CdrEvent>();
        foreach (var line in trimmed.Split('\n'))
        {
            var l = line.Trim();
            if (string.IsNullOrEmpty(l)) continue;
            try
            {
                var record = JsonSerializer.Deserialize<CdrEvent>(l);
                if (record is not null) records.Add(record);
            }
            catch (JsonException)
            {
                Console.Error.WriteLine($"Failed to parse NDJSON line: {l[..Math.Min(l.Length, 100)]}");
            }
        }
        return records;
    }

    // ============================================================
    // Validation
    // ============================================================

    private static bool IsValidCdrEvent(CdrEvent ev) =>
        !string.IsNullOrEmpty(ev.Id) &&
        !string.IsNullOrEmpty(ev.Group) &&
        !string.IsNullOrEmpty(ev.SessionId) &&
        ValidMessageTypes.Contains(ev.MessageType) &&
        !string.IsNullOrEmpty(ev.Flow.Name) &&
        !string.IsNullOrEmpty(ev.App.Name);

    // ============================================================
    // Kinesis delivery
    // ============================================================

    private async Task<(int successCount, int failedCount)> SendToKinesis(
        List<PutRecordsRequestEntry> records, string requestId, ILambdaContext context)
    {
        const int batchSize = 500;
        int successCount = 0, failedCount = 0;

        for (var i = 0; i < records.Count; i += batchSize)
        {
            var batch = records.GetRange(i, Math.Min(batchSize, records.Count - i));

            var result = await _kinesis.PutRecordsAsync(new PutRecordsRequest
            {
                StreamName = KinesisStreamName,
                Records = batch,
            });

            var batchFailed = result.FailedRecordCount;
            successCount += batch.Count - batchFailed;
            failedCount += batchFailed;

            if (batchFailed > 0)
            {
                for (var idx = 0; idx < result.Records.Count; idx++)
                {
                    var r = result.Records[idx];
                    if (r.ErrorCode is not null)
                    {
                        context.Logger.LogError(
                            $"[{requestId}] Kinesis put failed: index={i + idx} error={r.ErrorCode} msg={r.ErrorMessage}");
                    }
                }
            }
        }

        return (successCount, failedCount);
    }

    // ============================================================
    // HTTP response helper
    // ============================================================

    private static APIGatewayHttpApiV2ProxyResponse Respond(int statusCode, object body) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(body),
    };
}
