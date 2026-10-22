using System.Text.Json.Serialization;

namespace VanguardLauncher.Core;

/// <summary>The launcher.json asset on a GitHub release: what to download and what it should hash to.</summary>
public sealed class LauncherManifest
{
    [JsonPropertyName("launcher")]
    public LauncherRelease? Launcher { get; set; }

    [JsonPropertyName("game")]
    public GameRelease? Game { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public sealed class LauncherRelease
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = "";
}

public sealed class GameRelease
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
