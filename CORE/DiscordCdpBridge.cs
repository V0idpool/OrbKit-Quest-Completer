using OrbKit_Quest_Completer.CDPENGINE;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace OrbKit_Quest_Completer.CORE;

public sealed class DiscordCdpBridge
{
    private readonly int _port;
    private readonly CdpScriptProvider _scripts;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private int _cmdId = 1;

    public DiscordCdpBridge(CdpScriptProvider scripts, int port = 9222)
    {
        _scripts = scripts;
        _port = port;
    }

    public async Task<string?> FindDiscordWebSocketUrl(CancellationToken ct = default)
    {
        try
        {
            string json = await _http.GetStringAsync($"http://127.0.0.1:{_port}/json", ct);
            using var doc = JsonDocument.Parse(json);
            foreach (var target in doc.RootElement.EnumerateArray())
            {
                string? type = target.TryGetProperty("type", out var t) ? t.GetString() : null;
                string? url = target.TryGetProperty("url", out var u) ? u.GetString() : null;
                string? ws = target.TryGetProperty("webSocketDebuggerUrl", out var w) ? w.GetString() : null;

                if (type == "page" && !string.IsNullOrEmpty(ws) &&
                    url != null && url.Contains("discord.com", StringComparison.OrdinalIgnoreCase))
                {
                    return ws;
                }
            }
        }
        catch { }
        return null;
    }

    private async Task<JsonDocument?> SendCdpCommand(string method, object parameters, CancellationToken ct)
    {
        string wsUrl = await FindDiscordWebSocketUrl(ct)
            ?? throw new InvalidOperationException($"Discord CDP not found on 127.0.0.1:{_port}. Launch Discord with --remote-debugging-port={_port}.");

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(wsUrl), ct);

        int id = Interlocked.Increment(ref _cmdId);
        string payload = JsonSerializer.Serialize(new { id, method, @params = parameters });

        await ws.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, ct);

        var buffer = new byte[16384];
        while (true)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult res;
            do
            {
                res = await ws.ReceiveAsync(buffer, ct);
                if (res.MessageType == WebSocketMessageType.Close)
                    return null;
                ms.Write(buffer, 0, res.Count);
            } while (!res.EndOfMessage);

            var doc = JsonDocument.Parse(ms.ToArray());
            if (!doc.RootElement.TryGetProperty("id", out var respId) || respId.GetInt32() != id)
            {
                doc.Dispose();
                continue;
            }
            return doc;
        }
    }

    private async Task<string?> EvaluateJson(string jsExpression, CancellationToken ct)
    {
        using var doc = await SendCdpCommand("Runtime.evaluate", new
        {
            expression = jsExpression,
            awaitPromise = true,
            returnByValue = true
        }, ct);

        if (doc == null) return null;

        if (doc.RootElement.TryGetProperty("result", out var result) &&
            result.TryGetProperty("result", out var inner))
        {
            if (inner.TryGetProperty("value", out var val))
                return val.ValueKind == JsonValueKind.String ? val.GetString() : val.GetRawText();

            if (inner.TryGetProperty("description", out var desc))
                throw new InvalidOperationException($"Discord JS error: {desc.GetString()}");
        }
        return null;
    }

    public async Task<(bool Ok, string? Error)> CompileCheck(string scriptContent, CancellationToken ct = default)
    {
        string wrapped = $"(() => {{\n{scriptContent}\n}})";
        using var doc = await SendCdpCommand("Runtime.compileScript", new
        {
            expression = wrapped,
            sourceURL = "cdp_patch.js",
            persistScript = false
        }, ct);

        if (doc == null)
            return (false, "No response from Discord compiler.");

        if (doc.RootElement.TryGetProperty("result", out var result) &&
            result.TryGetProperty("exceptionDetails", out var ex))
        {
            int line = ex.TryGetProperty("lineNumber", out var ln) ? ln.GetInt32() : 0;
            int col = ex.TryGetProperty("columnNumber", out var cn) ? cn.GetInt32() : 0;
            string detail = ex.TryGetProperty("exception", out var exc) && exc.TryGetProperty("description", out var d)
                ? (d.GetString() ?? "Syntax error")
                : (ex.TryGetProperty("text", out var txt) ? txt.GetString() ?? "Syntax error" : "Syntax error");

            return (false, $"Syntax error at line {line}, col {col}: {detail}");
        }

        return (true, null);
    }

    private Task<string?> InvokePatch(string methodName, IProgress<string>? log, CancellationToken ct, params object?[] args)
    {
        string patchCode = _scripts.GetActiveScript(out string? warning);
        if (!string.IsNullOrEmpty(warning))
            log?.Report(warning);

        string methodJson = JsonSerializer.Serialize(methodName);
        string serializedArgs = string.Join(", ", args.Select(a => JsonSerializer.Serialize(a)));

        string js = $$"""
        (async () => {
            try {
                {{patchCode}}
                const __adapter = globalThis.__DQC_ADAPTER;
                if (!__adapter || typeof __adapter[{{methodJson}}] !== 'function') {
                    throw new Error('CDP patch is missing required method: ' + {{methodJson}});
                }
                const __res = await __adapter[{{methodJson}}]({{serializedArgs}});
                return typeof __res === 'string' ? __res : JSON.stringify(__res ?? null);
            } finally {
                delete globalThis.__DQC_ADAPTER;
            }
        })()
        """;

        return EvaluateJson(js, ct);
    }

    public async Task<DiscordQuestSnapshot> FetchQuestSnapshot(IProgress<string>? log = null, CancellationToken ct = default)
    {
        string rawJson = await InvokePatch("fetchSnapshot", log, ct)
            ?? throw new InvalidOperationException("Empty response from Discord CDP.");

        return JsonSerializer.Deserialize<DiscordQuestSnapshot>(rawJson) ?? new DiscordQuestSnapshot();
    }

    public async Task EnrollQuestAsync(DiscordQuestInfo quest, IProgress<string>? log = null, CancellationToken ct = default)
    {
        await InvokePatch("enrollQuest", log, ct, quest.Id, quest.TrafficMetadataSealed);
    }

    public async Task RunVideoQuest(
        DiscordQuestInfo quest,
        IProgress<string>? log = null,
        IProgress<SessionProgress>? progress = null,
        CancellationToken ct = default,
        double minPaceSeconds = 7.0,
        double maxPaceSeconds = 9.5,
        bool autoEnroll = true)
    {
        if (!quest.Enrolled)
        {
            if (!autoEnroll)
                throw new InvalidOperationException($"Quest \"{quest.Name}\" is not enrolled and Auto-Enroll is OFF. Accept it in Discord first.");

            log?.Report($"Enrolling in video quest \"{quest.Name}\"...");
            await EnrollQuestAsync(quest, log, ct);
            await Task.Delay(Random.Shared.Next(900, 1600), ct);
        }

        int minMs = (int)Math.Round(Math.Clamp(minPaceSeconds, 2.0, 20.0) * 1000);
        int maxMs = (int)Math.Round(Math.Clamp(Math.Max(minPaceSeconds, maxPaceSeconds), 2.0, 25.0) * 1000);

        double cur = quest.Progress;
        double target = Math.Max(1, quest.Target);

        log?.Report($"Running video quest: \"{quest.Name}\" ({cur:F0}s / {target:F0}s) | sending progress every {minMs / 1000.0:F1}-{maxMs / 1000.0:F1}s...");

        while (cur < target)
        {
            ct.ThrowIfCancellationRequested();

            int delayMs = Random.Shared.Next(minMs, maxMs + 1);
            await Task.Delay(delayMs, ct);

            double jitter = (Random.Shared.NextDouble() * 0.02) - 0.01;
            cur = Math.Min(target, cur + (delayMs / 1000.0) + jitter);
            double ts = Math.Round(cur, 6);

            string? reply = await InvokePatch("sendVideoProgress", log, ct, quest.Id, quest.TaskKey, ts);
            bool completed = false;
            if (!string.IsNullOrEmpty(reply))
            {
                using var doc = JsonDocument.Parse(reply);
                completed = doc.RootElement.TryGetProperty("completed", out var c) && c.GetBoolean();
                if (doc.RootElement.TryGetProperty("serverVal", out var sv) && sv.ValueKind == JsonValueKind.Number)
                    cur = Math.Max(cur, Math.Min(target, sv.GetDouble()));
            }

            progress?.Report(new SessionProgress(cur, target, 0, "Video Quest"));
            if (completed)
                break;
        }

        progress?.Report(new SessionProgress(target, target, 0, "Complete"));
        log?.Report($"Video quest \"{quest.Name}\" completed. Claim your reward manually inside Discord.");
    }

    public static bool TryRelaunchDiscordWithCdp(out string message, int port = 9222)
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string[] flavors = ["Discord", "DiscordCanary", "DiscordPTB"];

        foreach (string flavor in flavors)
        {
            string updater = Path.Combine(localAppData, flavor, "Update.exe");
            if (!File.Exists(updater)) continue;

            foreach (var p in Process.GetProcessesByName(flavor))
            {
                try { p.Kill(); } catch { }
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = updater,
                Arguments = $"--processStart {flavor}.exe --process-start-args=\"--remote-debugging-port={port}\"",
                UseShellExecute = false
            });

            message = $"Relaunched {flavor} with CDP on 127.0.0.1:{port}. Wait roughly 5 seconds, then click Connect CDP.";
            return true;
        }

        message = "Could not locate Discord's Update.exe in %LocalAppData%.";
        return false;
    }
}