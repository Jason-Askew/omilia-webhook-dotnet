using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace OmiliaWebhook.Dev;

/// <summary>
/// Replays real export data against the dev server.
/// Run with: dotnet run --project Dev -- [url] [--batch]
/// </summary>
public static class SendTestEvents
{
    private static readonly string DefaultUrl = "http://127.0.0.1:3000/webhook";

    public static async Task Run(string[] args)
    {
        var batchMode = args.Contains("--batch");
        var url = args.FirstOrDefault(a => a.StartsWith("http")) ?? DefaultUrl;
        var secret = Environment.GetEnvironmentVariable("WEBHOOK_SECRET");

        var exportsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "exports"));
        if (!Directory.Exists(exportsDir))
        {
            Console.Error.WriteLine($"\nExports directory not found: {exportsDir}");
            Console.Error.WriteLine("\nPlace Omilia CDR export .json files in the exports/ directory at the repo root.");
            Environment.Exit(1);
        }

        Console.WriteLine($"Loading records from {exportsDir}");
        var records = LoadRecords(exportsDir);
        Console.WriteLine($"Loaded {records.Count} records\n");

        records.Sort((a, b) =>
        {
            var ta = a.TryGetProperty("time", out var pa) && pa.ValueKind == JsonValueKind.Number ? pa.GetInt64() : 0L;
            var tb = b.TryGetProperty("time", out var pb) && pb.ValueKind == JsonValueKind.Number ? pb.GetInt64() : 0L;
            return ta.CompareTo(tb);
        });

        using var client = new HttpClient(new SocketsHttpHandler
        {
            ConnectCallback = async (ctx, ct) =>
            {
                // Force IPv4 to avoid 2s IPv6 timeout on Windows
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.NoDelay = true;
                var uri = ctx.DnsEndPoint;
                await socket.ConnectAsync(IPAddress.Loopback, uri.Port, ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
        });

        if (secret is not null)
            client.DefaultRequestHeaders.Add("x-webhook-secret", secret);

        if (batchMode)
        {
            Console.WriteLine($"Sending {records.Count} records as batch...");
            var ndjson = string.Join("\n", records.Select(r => r.GetRawText()));
            var sw = Stopwatch.StartNew();
            var resp = await client.PostAsync(url, new StringContent(ndjson, Encoding.UTF8, "application/x-ndjson"));
            Console.WriteLine($"  -> {(int)resp.StatusCode} ({sw.Elapsed.TotalSeconds:F3}s)");
        }
        else
        {
            Console.WriteLine($"Sending {records.Count} records individually to {url}\n");
            var totalSw = Stopwatch.StartNew();
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var msgType = record.TryGetProperty("message_type", out var mt) ? mt.GetString() : "?";
                var flowName = record.TryGetProperty("flow", out var flow) && flow.TryGetProperty("name", out var fn)
                    ? fn.GetString() : "?";

                var sw = Stopwatch.StartNew();
                var resp = await client.PostAsync(url,
                    new StringContent(record.GetRawText(), Encoding.UTF8, "application/json"));
                var elapsed = sw.Elapsed.TotalSeconds;

                Console.WriteLine($"  [{i + 1}/{records.Count}] {msgType,-14} | {flowName,-30} -> {(int)resp.StatusCode} ({elapsed:F3}s)");
            }
            Console.WriteLine($"\nTotal send time: {totalSw.Elapsed.TotalSeconds:F3}s");
        }

        Console.WriteLine("Done.");
    }

    private static List<JsonElement> LoadRecords(string dir)
    {
        var records = new List<JsonElement>();
        var files = Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories).OrderBy(f => f).ToList();

        if (files.Count == 0)
        {
            Console.Error.WriteLine($"No .json files found in {dir}");
            Environment.Exit(1);
        }

        foreach (var file in files)
        {
            Console.WriteLine($"  Loading {Path.GetRelativePath(dir, file)}");
            foreach (var line in File.ReadLines(file))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(trimmed);
                    records.Add(doc.RootElement.Clone());
                }
                catch (JsonException ex)
                {
                    Console.Error.WriteLine($"    Line parse error: {ex.Message}");
                }
            }
        }

        return records;
    }
}
