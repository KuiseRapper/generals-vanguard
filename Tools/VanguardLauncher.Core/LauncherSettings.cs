using System.Text.Json;

namespace VanguardLauncher.Core;

/// <summary>What the launcher remembers between runs. It lives in LocalApplicationData, not the game folder.</summary>
public sealed class LauncherSettings
{
    public string? GamePath { get; set; }
    public string? ZeroHourPath { get; set; }
    public string? ActiveMod { get; set; }
    public bool ShortcutCreated { get; set; }

    /// <summary>The release asset the launcher asks for the manifest; the latest release by default.</summary>
    public string ManifestUrl { get; set; } =
        "https://github.com/KuiseRapper/generals-vanguard/releases/latest/download/launcher.json";

    public bool CheckOnStartup { get; set; } = true;
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string DirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GeneralsVanguard");

    public static string FilePath => Path.Combine(DirectoryPath, "launcher.json");

    public static LauncherSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(FilePath), Options)
                       ?? new LauncherSettings();
        }
        catch
        {
            // A corrupt settings file must not stop the launcher; the defaults are safe.
        }
        return new LauncherSettings();
    }

    public static void Save(LauncherSettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));
    }
}
