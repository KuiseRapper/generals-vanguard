using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace VanguardLauncher;

/// <summary>What the launcher found of the player's own Zero Hour install.</summary>
public sealed record ZeroHourInstall(string? Path, bool Found, bool BaseGeneralsOnly, bool Repaired);

/// <summary>
/// Find the Zero Hour folder, not the base game's.  The game itself reads
/// HKCU/HKLM\SOFTWARE\Electronic Arts\EA Games\Command and Conquer Generals Zero Hour InstallPath
/// (with the 32-bit view), so that is the first place to look; Steam's library folders and the EA
/// app's default folders are the fallbacks.  A found folder is written to the per-user key when the
/// key is missing or points at something without INIZH.big, because the game reads HKCU first.
/// </summary>
public static class ZeroHourLocator
{
    private const string ZeroHourKey =
        @"SOFTWARE\Electronic Arts\EA Games\Command and Conquer Generals Zero Hour";
    private const string GeneralsKey =
        @"SOFTWARE\Electronic Arts\EA Games\Generals";

    public static ZeroHourInstall Detect()
    {
        var candidates = new List<string?>();
        candidates.AddRange(RegistryCandidates());
        candidates.AddRange(SteamCandidates());
        candidates.AddRange(DefaultCandidates());

        string? found = null;
        foreach (var candidate in candidates)
        {
            var path = Normalize(candidate);
            if (path is not null && File.Exists(Path.Combine(path, "INIZH.big")))
            {
                found = path;
                break;
            }
        }

        if (found is not null)
            return new ZeroHourInstall(found, true, false, Repair(found));

        var basePath = BaseGeneralsPath();
        return new ZeroHourInstall(null, false, basePath is not null, false);
    }

    private static IEnumerable<string?> RegistryCandidates()
    {
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                string? value = null;
                try
                {
                    using var root = RegistryKey.OpenBaseKey(hive, view);
                    using var key = root.OpenSubKey(ZeroHourKey);
                    value = key?.GetValue("InstallPath") as string;
                }
                catch
                {
                    // A missing hive or a denied key is one fewer candidate.
                }
                yield return value;
            }
        }
    }

    private static IEnumerable<string?> SteamCandidates()
    {
        var roots = new List<string?>();
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            roots.Add(key?.GetValue("SteamPath") as string);
        }
        catch
        {
            // No Steam key.
        }
        roots.Add(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));

        var libraries = new List<string>();
        foreach (var root in roots)
        {
            var steam = Normalize(root);
            if (steam is null)
                continue;
            libraries.Add(steam);
            var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            try
            {
                if (!File.Exists(vdf))
                    continue;
                foreach (var line in File.ReadLines(vdf))
                {
                    var match = Regex.Match(line, "\"path\"\\s+\"([^\"]+)\"");
                    if (match.Success)
                        libraries.Add(match.Groups[1].Value.Replace("\\\\", "\\"));
                }
            }
            catch
            {
                // A broken library file just means the default root is all we have.
            }
        }

        var names = new[]
        {
            "Command and Conquer Generals - Zero Hour",
            "Command & Conquer Generals - Zero Hour",
            "Command and Conquer Generals Zero Hour",
        };
        foreach (var library in libraries.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var name in names)
                yield return Path.Combine(library, "steamapps", "common", name);
        }
    }

    private static IEnumerable<string?> DefaultCandidates()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        foreach (var root in new[] { programFiles, programFilesX86 })
        {
            yield return Path.Combine(root, "EA Games", "Command and Conquer Generals Zero Hour");
            yield return Path.Combine(root, "Electronic Arts", "Command and Conquer Generals Zero Hour");
            yield return Path.Combine(root, "EA Games", "Command and Conquer Generals - Zero Hour");
        }
    }

    private static bool Repair(string found)
    {
        // If any readable Zero Hour key already points at a valid install, leave the registry alone.
        foreach (var candidate in RegistryCandidates())
        {
            var path = Normalize(candidate);
            if (path is not null && File.Exists(Path.Combine(path, "INIZH.big")))
                return false;
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(ZeroHourKey, true);
            if (key is null)
                return false;
            key.SetValue("InstallPath", found);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? BaseGeneralsPath()
    {
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using var root = RegistryKey.OpenBaseKey(hive, view);
                    using var key = root.OpenSubKey(GeneralsKey);
                    var path = Normalize(key?.GetValue("InstallPath") as string);
                    if (path is not null && File.Exists(Path.Combine(path, "INIGEN.big")))
                        return path;
                }
                catch
                {
                    // Try the next view.
                }
            }
        }
        return null;
    }

    private static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        try
        {
            return Path.GetFullPath(path.Trim().Trim('"'));
        }
        catch
        {
            return null;
        }
    }
}
