using System.Text.Json.Serialization;

namespace OrbKit_Quest_Completer.CORE;
public sealed class DetectableGame
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("executables")]
    public List<GameExecutable>? Executables { get; set; }
}

public sealed class GameExecutable
{
    [JsonPropertyName("os")]
    public string Os { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_launcher")]
    public bool IsLauncher { get; set; }
}

public sealed record ResolvedExecutable(string ExeName, string RelativePath)
{
    public override string ToString() => RelativePath;
}

public sealed record SessionProgress(
    double ElapsedSeconds,
    double TargetSeconds,
    double TailSeconds,
    string Phase);

public sealed class DiscordQuestSnapshot
{
    [JsonPropertyName("suspendedUntil")]
    public string? SuspendedUntil { get; set; }

    [JsonPropertyName("enrollmentBlockedUntil")]
    public string? EnrollmentBlockedUntil { get; set; }

    [JsonPropertyName("orbBalance")]
    public int? OrbBalance { get; set; }

    [JsonPropertyName("quests")]
    public List<DiscordQuestInfo> Quests { get; set; } = [];
}

public sealed class DiscordQuestInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("taskType")]
    public string TaskType { get; set; } = string.Empty;

    [JsonPropertyName("taskKey")]
    public string TaskKey { get; set; } = string.Empty;

    [JsonPropertyName("appId")]
    public string? AppId { get; set; }

    [JsonPropertyName("target")]
    public double Target { get; set; }

    [JsonPropertyName("progress")]
    public double Progress { get; set; }

    [JsonPropertyName("enrolled")]
    public bool Enrolled { get; set; }

    [JsonPropertyName("orbs")]
    public int Orbs { get; set; }

    [JsonPropertyName("rewardLabel")]
    public string RewardLabel { get; set; } = string.Empty;

    [JsonPropertyName("sealed")]
    public string? TrafficMetadataSealed { get; set; }
}