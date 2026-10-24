using System.Diagnostics;

namespace VanguardLauncher.Core;

/// <summary>Where the game lives and how it is started.</summary>
public static class GameLocator
{
    public static string LauncherDirectory =>
        Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;

    public static string DefaultGameDirectory => Path.Combine(LauncherDirectory, "game");

    public static string? Find(string? configured)
    {
        var candidates = new[]
        {
            configured,
            DefaultGameDirectory,
            Path.Combine(LauncherDirectory, "..", "GeneralsMD", "Run"),
            Path.Combine(LauncherDirectory, "..", "..", "GeneralsMD", "Run"),
        };
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;
            try
            {
                var full = Path.GetFullPath(candidate);
                if (File.Exists(Path.Combine(full, "generals.exe")))
                    return full;
            }
            catch
            {
                // A path the user typed badly is just one more miss.
            }
        }
        return null;
    }

    public static string? InstalledVersion(string? gameDirectory)
    {
        if (gameDirectory is null)
            return null;
        var file = Path.Combine(gameDirectory, "version.txt");
        return File.Exists(file) ? File.ReadAllText(file).Trim() : null;
    }

    /// <summary>
    /// Start the game from its own folder.  Windows finds the installed Zero Hour through the registry;
    /// there is no -root switch on this platform (that is the POSIX install root).  A mod is mounted
    /// with -mod and an absolute path, which the engine accepts as-is and remounts over retail.
    /// </summary>
    public static Process Start(string gameDirectory, string? modDirectory = null)
    {
        var exe = Path.Combine(gameDirectory, "generals.exe");
        if (!File.Exists(exe))
            throw new FileNotFoundException("generals.exe was not found in the game folder.", exe);

        var arguments = "-rk7";
        if (!string.IsNullOrWhiteSpace(modDirectory))
        {
            if (!Directory.Exists(modDirectory))
                throw new DirectoryNotFoundException("The selected mod folder is gone: " + modDirectory);
            arguments += $" -mod \"{modDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}\"";
        }

        var info = new ProcessStartInfo(exe)
        {
            WorkingDirectory = gameDirectory,
            UseShellExecute = true,
            // WinMain refuses a direct start with "Please start Generals Vanguard from its launcher."
            // unless this marker is present; it is the handshake the launcher's game expects.
            Arguments = arguments,
        };
        return Process.Start(info) ?? throw new InvalidOperationException("The game did not start.");
    }
}
