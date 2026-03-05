using System.Net;
using System.Text;
using System.Text.Json;
using OmiliaWebhook.Enrichment;
using OmiliaWebhook.Types;

namespace OmiliaWebhook.Dev;

/// <summary>
/// Local dev server — writes enriched events to Dev/kinesis-output.log
/// instead of Kinesis.
/// </summary>
public static class Server
{
    private static readonly HashSet<string> ValidMessageTypes = new() { "dialog_start", "dialog_step", "dialog_end" };

    public static async Task Run(string[] args)
    {
        var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
        var webhookSecret = Environment.GetEnvironmentVariable("WEBHOOK_SECRET");
        var logPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Dev", "kinesis-output.log");
        logPath = Path.GetFullPath(logPath);

        // Clear log on start
        await File.WriteAllTextAsync(logPath, "");
        using var logWriter = new StreamWriter(logPath, append: true) { AutoFlush = true };

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        Console.WriteLine("Omilia Webhook Dev Server (C#)");
        Console.WriteLine($"  Listening on http://127.0.0.1:{port}/webhook");
        Console.WriteLine($"  Log file:   {logPath}");
        if (webhookSecret is not null)
            Console.WriteLine("  Auth:       x-webhook-secret header required");
        Console.WriteLine();

        while (true)
        {
            var ctx = await listener.GetContextAsync();
            await HandleRequest(ctx, webhookSecret, logWriter);
        }
    }

    private static async Task HandleRequest(HttpListenerContext ctx, string? webhookSecret, StreamWriter logWriter)
    {
        var req = ctx.Request;
        var resp = ctx.Response;

        if (req.HttpMethod == "GET" && req.Url?.AbsolutePath == "/health")
        {
            await Respond(resp, 200, new { status = "ok" });
            return;
        }

        if (req.HttpMethod != "POST" || req.Url?.AbsolutePath != "/webhook")
        {
            await Respond(resp, 404, new { error = "Not found" });
            return;
        }

        // Auth
        if (webhookSecret is not null)
        {
            var secret = req.Headers["x-webhook-secret"] ?? req.Headers["X-Webhook-Secret"];
            if (secret != webhookSecret)
            {
                await Respond(resp, 401, new { error = "Unauthorized" });
                return;
            }
        }

        // Read body
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            await Respond(resp, 400, new { error = "Empty body" });
            return;
        }

        var records = ParseBody(body);
        if (records.Count == 0)
        {
            await Respond(resp, 400, new { error = "No valid CDR records in body" });
            return;
        }

        var accepted = 0;
        foreach (var raw in records)
        {
            if (!IsValid(raw))
            {
                Console.WriteLine($"  ! Skipping invalid record: {raw.MessageType} / {raw.Id}");
                continue;
            }

            var enriched = Enricher.EnrichEvent(raw);
            var json = JsonSerializer.Serialize(enriched);
            await logWriter.WriteLineAsync(json);
            accepted++;

            Console.WriteLine($"  + {enriched.Enrichment.MessageType} | {enriched.Enrichment.FlowName}");
        }

        await Respond(resp, 200, new { accepted, message = "OK" });
    }

    private static List<CdrEvent> ParseBody(string body)
    {
        var trimmed = body.Trim();
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return JsonSerializer.Deserialize<List<CdrEvent>>(trimmed) ?? new();
            var single = JsonSerializer.Deserialize<CdrEvent>(trimmed);
            return single is not null ? new List<CdrEvent> { single } : new();
        }
        catch (JsonException) { }

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
            catch (JsonException) { Console.Error.WriteLine($"  ! Failed to parse NDJSON line: {l[..Math.Min(l.Length, 100)]}"); }
        }
        return records;
    }

    private static bool IsValid(CdrEvent ev) =>
        !string.IsNullOrEmpty(ev.Id) && !string.IsNullOrEmpty(ev.Group) &&
        !string.IsNullOrEmpty(ev.SessionId) && ValidMessageTypes.Contains(ev.MessageType) &&
        !string.IsNullOrEmpty(ev.Flow.Name) && !string.IsNullOrEmpty(ev.App.Name);

    private static async Task Respond(HttpListenerResponse resp, int status, object body)
    {
        var json = JsonSerializer.Serialize(body);
        var bytes = Encoding.UTF8.GetBytes(json);
        resp.StatusCode = status;
        resp.ContentType = "application/json";
        resp.ContentLength64 = bytes.Length;
        await resp.OutputStream.WriteAsync(bytes);
        resp.Close();
    }
}
