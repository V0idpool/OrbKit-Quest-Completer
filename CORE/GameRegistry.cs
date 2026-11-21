using System.Net.Http.Json;
using System.Text.Json;

namespace OrbKit_Quest_Completer.CORE;

public sealed class GameRegistry
{
    private const string DetectableUrl = "https://discord.com/api/v9/applications/detectable";
    private readonly string _cachePath;
    private readonly HttpClient _http;

    public GameRegistry(string cacheDirectory)
    {
        Directory.CreateDirectory(cacheDirectory);
        _cachePath = Path.Combine(cacheDirectory, "detectable_games.json");
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<List<DetectableGame>> GetGames(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (!forceRefresh && File.Exists(_cachePath) &&
            (DateTime.UtcNow - File.GetLastWriteTimeUtc(_cachePath)) < TimeSpan.FromHours(12))
        {
            await using var fs = File.OpenRead(_cachePath);
            var cached = await JsonSerializer.DeserializeAsync<List<DetectableGame>>(fs, cancellationToken: ct);
            if (cached is { Count: > 0 })
                return cached;
        }

        var games = await _http.GetFromJsonAsync<List<DetectableGame>>(DetectableUrl, ct) ?? [];
        await using var outFs = File.Create(_cachePath);
        await JsonSerializer.SerializeAsync(outFs, games, cancellationToken: ct);
        return games;
    }

    public async Task<DetectableGame?> FetchSingleApp(string appId, CancellationToken ct = default)
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<DetectableGame>>(
                $"https://discord.com/api/v9/applications/public?application_ids={Uri.EscapeDataString(appId)}", ct);
            return list?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}