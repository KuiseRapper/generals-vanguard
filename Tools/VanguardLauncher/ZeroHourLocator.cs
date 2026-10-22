using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace VanguardLauncher;

/// <summary>What the launcher found of the player's own Zero Hour install and its shared base assets.</summary>
public sealed record ZeroHourInstall(
    string? Path,
    bool Found,
    bool BaseGeneralsOnly,
    bool Repaired,
    string? BaseSource,
    bool BaseReady);

public enum BaseAssetResult
{
    Ready,
    Linked,
    Copied,
    Missing,
    Failed,
}

/// <summary>
/// Find Zero Hour — not the base game — and connect the base game's .big files that Zero Hour shares.
/// The game looks for them in ZH_Generals beside generals.exe (or the registered base install), so a
/// portable launcher folder has to link that folder in from the player's own install.
/// </summary>
public static class ZeroHourLocator
{
    private const string ZeroHourKey =
        @"SOFTWARE\Electronic Arts\EA Games\Command and Conquer Generals Zero Hour";
    private const string GeneralsKey =
        @"SOFTWARE\Electronic Arts\EA Games\Generals";
    private const string BaseArchive = "Textures.big";

    public static ZeroHourInstall Detect(string? manualZeroHour = null, string? gameDirectory = null)
    {
        var candidates = new List<string?>();
        var manual = Normalize(manualZeroHour);
        if (manual is not null && File.Exists(Path.Combine(manual, "INIZH.big")))
            candidates.Add(manual);
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

        var baseSource = FindBaseSource(found, manual);
        var baseReady = gameDirectory is not null
                        && HasBase(Path.Combine(gameDirectory, "ZH_Generals"));

        if (found is not null)
            return new ZeroHourInstall(found, true, false, Repair(found), baseSource, baseReady);

        var baseOnly = BaseGeneralsPath() is not null;
        return new ZeroHourInstall(null, false, baseOnly, false, baseSource, baseReady);
    }

    /// <summary>
    /// Make gameDirectory\ZH_Generals hold the base game's .big files.  A junction to the Zero Hour
    /// install's own ZH_Generals is instant and copies nothing; if Windows refuses the junction, the
    /// .big files are copied instead.
    /// </summary>
    public static BaseAssetResult PrepareBaseAssets(
        string gameDirectory, string? manualZeroHour, out string message)
    {
        var target = Path.Combine(gameDirectory, "ZH_Generals");
        if (HasBase(target))
        {
            message = "Base game files are linked.";
            return BaseAssetResult.Ready;
        }

        var zeroHour = Detect(manualZeroHour);
        var source = zeroHour.BaseSource;
        if (source is null)
        {
            message = "Zero Hour's shared base-game files (Textures.big) were not found in the Zero Hour "
                      + "install, the registered Generals install, or a Steam sibling folder.";
            return BaseAssetResult.Missing;
        }

        try
        {
            RemoveDirectory(target);

            // A clean base folder (ZH_Generals, or a separate Generals install) can be a junction:
            // the game reads through it and nothing is copied.  A Zero Hour folder that carries the
            // base .big files itself would expose INIZH.big through the link, so that case copies only
            // the base archives.
            if (!File.Exists(Path.Combine(source, "INIZH.big")) && TryJunction(target, source))
            {
                message = "Base game files linked from " + source;
                return BaseAssetResult.Linked;
            }

            Directory.CreateDirectory(target);
            var copied = 0;
            foreach (var file in Directory.GetFiles(source, "*.big"))
            {
                var name = Path.GetFileName(file);
                if (name.Contains("ZH", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("INIZH.big", StringComparison.OrdinalIgnoreCase))
                    continue;
                File.Copy(file, Path.Combine(target, name), true);
                copied++;
            }
            if (copied == 0)
            {
                message = "The base game folder " + source + " has no .big files to copy.";
                return BaseAssetResult.Missing;
            }
            message = $"Base game files copied from {source} ({copied} archives).";
            return BaseAssetResult.Copied;
        }
        catch (Exception ex)
        {
            message = "Could not link the base game files: " + ex.Message;
            return BaseAssetResult.Failed;
        }
    }

    public static bool HasBase(string directory)
        => File.Exists(Path.Combine(directory, BaseArchive));

    /// <summary>
    /// The engine looks for Zero Hour's .big files beside the exe.  Reforged installs into the Zero
    /// Hour folder; the portable launcher instead links every top-level .big from the Zero Hour
    /// install into its game folder (hard links where Windows allows them, copies otherwise), so
    /// INIZH.big and its Data\INI\Default\Weather.ini are found without touching the Steam install.
    /// </summary>
    public static (int Linked, int Copied, string Message) PrepareZeroHourArchives(
        string gameDirectory, string? zeroHour)
    {
        if (zeroHour is null || !Directory.Exists(zeroHour))
            return (0, 0, "Zero Hour folder not found.");
        Directory.CreateDirectory(gameDirectory);
        var linked = 0;
        var copied = 0;
        foreach (var source in Directory.GetFiles(zeroHour, "*.big"))
        {
            var target = Path.Combine(gameDirectory, Path.GetFileName(source));
            if (File.Exists(target))
                continue;
            if (TryHardLink(target, source))
            {
                linked++;
                continue;
            }
            try
            {
                File.Copy(source, target, false);
                copied++;
            }
            catch
            {
                // A locked or unreadable archive is reported by the Weather check below.
            }
        }
        var message = linked + copied == 0
            ? "No Zero Hour .big files were found in " + zeroHour
            : $"Zero Hour data: {linked} linked, {copied} copied.";
        return (linked, copied, message);
    }

    /// <summary>
    /// The game's pointers are the retail .ANI files under Data\Cursors, loaded by Win32Mouse with
    /// LoadCursorFromFile from the game folder.  A portable install ships none of them (they are EA's),
    /// so the launcher links the player's own copies in beside the rest of the game data.  Without this
    /// there is no pointer in the menu or in the match: the buttons cannot be clicked.
    /// </summary>
    public static (int Linked, int Copied, string Message) PrepareCursorAssets(
        string gameDirectory, string? zeroHour)
    {
        if (zeroHour is null || !Directory.Exists(zeroHour))
            return (0, 0, "Zero Hour folder not found, so its pointers were not linked.");
        var source = Path.Combine(zeroHour, "Data", "Cursors");
        if (!Directory.Exists(source))
            return (0, 0, "The Zero Hour install has no Data\\Cursors folder, so its pointers were not linked.");
        var target = Path.Combine(gameDirectory, "Data", "Cursors");
        Directory.CreateDirectory(target);
        var linked = 0;
        var copied = 0;
        foreach (var file in Directory.GetFiles(source))
        {
            var destination = Path.Combine(target, Path.GetFileName(file));
            if (File.Exists(destination))
                continue;
            if (TryHardLink(destination, file))
            {
                linked++;
                continue;
            }
            try
            {
                File.Copy(file, destination, false);
                copied++;
            }
            catch
            {
                // A missing pointer falls back to the system arrow in Win32Mouse.
            }
        }
        var message = linked + copied == 0
            ? "Zero Hour pointers already linked."
            : $"Zero Hour pointers: {linked} linked, {copied} copied.";
        return (linked, copied, message);
    }

    private static bool TryHardLink(string link, string target)
    {
        try
        {
            var info = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            info.ArgumentList.Add("/c");
            info.ArgumentList.Add($"mklink /H \"{link}\" \"{target}\"");
            using var process = Process.Start(info);
            if (process is null)
                return false;
            process.WaitForExit(15000);
            return process.ExitCode == 0 && File.Exists(link);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Some Zero Hour SKUs ship a second INIZH.big in Data\INI.  The retail game deletes it on
    /// startup (relative to its own working directory), but Vanguard runs from its own game folder,
    /// so the duplicate survives and its GameData.ini wins — that is where a Reforged 3.x
    /// UserDataLeafName comes from even after a Steam verify.  Move it aside once.
    /// </summary>
    public static string? MoveDuplicateInizh(string? zeroHour)
    {
        if (zeroHour is null)
            return null;
        var path = Path.Combine(zeroHour, "Data", "INI", "INIZH.big");
        if (!File.Exists(path))
            return null;
        var backup = path + ".vanguard-backup";
        var index = 1;
        while (File.Exists(backup))
            backup = path + ".vanguard-backup" + index++;
        try
        {
            File.Move(path, backup);
            return backup;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// The expansion's INIZH.big carries the shared INIs, including Data\INI\Default\Weather.ini.
    /// An install where the game cannot open that file is incomplete; the launcher says so before
    /// starting rather than letting the engine throw.
    /// </summary>
    public static bool HasInizhFile(string? zeroHour, string fileName)
    {
        if (zeroHour is null)
            return false;
        var path = Path.Combine(zeroHour, "INIZH.big");
        if (!File.Exists(path))
            return false;
        try
        {
            var needle = Encoding.ASCII.GetBytes(fileName);
            using var stream = File.OpenRead(path);
            var buffer = new byte[(1 << 20) + needle.Length];
            var carry = 0;
            while (true)
            {
                var read = stream.Read(buffer, carry, buffer.Length - carry);
                if (read <= 0)
                    return false;
                var total = carry + read;
                if (buffer.AsSpan(0, total).IndexOf(needle) >= 0)
                    return true;
                carry = Math.Min(needle.Length - 1, total);
                Buffer.BlockCopy(buffer, total - carry, buffer, 0, carry);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Upstream 3.2.0 added UserDataLeafName to GameData.ini.  This fork is based on 3.0.1 and
    /// stops on that field, so a Zero Hour install carrying the Reforged 3.x data needs to be
    /// restored to the 3.0.1 files before Vanguard can parse it.  The field is readable in the
    /// unofficial INIZH.big; a vanilla one does not contain it.
    /// </summary>
    public static bool HasReforgedGameData(string? zeroHour)
    {
        if (zeroHour is null)
            return false;
        var path = Path.Combine(zeroHour, "INIZH.big");
        if (!File.Exists(path))
            return false;
        try
        {
            var needle = Encoding.ASCII.GetBytes("UserDataLeafName");
            using var stream = File.OpenRead(path);
            var buffer = new byte[(1 << 20) + needle.Length];
            var carry = 0;
            while (true)
            {
                var read = stream.Read(buffer, carry, buffer.Length - carry);
                if (read <= 0)
                    return false;
                var total = carry + read;
                if (buffer.AsSpan(0, total).IndexOf(needle) >= 0)
                    return true;
                carry = Math.Min(needle.Length - 1, total);
                Buffer.BlockCopy(buffer, total - carry, buffer, 0, carry);
            }
        }
        catch
        {
            return false;
        }
    }

    private static string? FindBaseSource(string? zeroHour, string? manual)
    {
        var candidates = new List<string?>();
        if (manual is not null)
        {
            candidates.Add(Path.Combine(manual, "ZH_Generals"));
            candidates.Add(manual);
        }
        if (zeroHour is not null)
        {
            candidates.Add(Path.Combine(zeroHour, "ZH_Generals"));
            candidates.Add(zeroHour);
            var parent = Path.GetDirectoryName(zeroHour);
            if (parent is not null)
            {
                candidates.Add(Path.Combine(parent, "Command & Conquer Generals"));
                candidates.Add(Path.Combine(parent, "Command & Conquer(tm) Generals"));
                candidates.Add(Path.Combine(parent, "Command and Conquer Generals"));
                candidates.Add(Path.Combine(parent, "Command and Conquer The First Decade",
                    "Command & Conquer(tm) Generals"));
            }
        }
        candidates.Add(BaseGeneralsPath());
        candidates.AddRange(SteamBaseCandidates());
        candidates.AddRange(DefaultBaseCandidates());

        foreach (var candidate in candidates)
        {
            var path = Normalize(candidate);
            if (path is not null && HasBase(path))
                return path;
        }
        return null;
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
        foreach (var root in SteamRoots())
        {
            foreach (var library in SteamLibraries(root))
            {
                yield return Path.Combine(library, "steamapps", "common", "Command and Conquer Generals - Zero Hour");
                yield return Path.Combine(library, "steamapps", "common", "Command & Conquer Generals - Zero Hour");
                yield return Path.Combine(library, "steamapps", "common", "Command and Conquer Generals Zero Hour");
            }
        }
    }

    private static IEnumerable<string?> SteamBaseCandidates()
    {
        foreach (var root in SteamRoots())
        {
            foreach (var library in SteamLibraries(root))
            {
                yield return Path.Combine(library, "steamapps", "common", "Command and Conquer Generals");
                yield return Path.Combine(library, "steamapps", "common", "Command & Conquer Generals");
                yield return Path.Combine(library, "steamapps", "common", "Command and Conquer(tm) Generals");
            }
        }
    }

    private static IEnumerable<string?> SteamRoots()
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
        return roots;
    }

    private static IEnumerable<string> SteamLibraries(string? root)
    {
        var libraries = new List<string>();
        var steam = Normalize(root);
        if (steam is null)
            return libraries;
        libraries.Add(steam);
        var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        try
        {
            if (File.Exists(vdf))
            {
                foreach (var line in File.ReadLines(vdf))
                {
                    var match = Regex.Match(line, "\"path\"\\s+\"([^\"]+)\"");
                    if (match.Success)
                        libraries.Add(match.Groups[1].Value.Replace("\\\\", "\\"));
                }
            }
        }
        catch
        {
            // A broken library file just means the default root is all we have.
        }
        return libraries;
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

    private static IEnumerable<string?> DefaultBaseCandidates()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        foreach (var root in new[] { programFiles, programFilesX86 })
        {
            yield return Path.Combine(root, "EA Games", "Command and Conquer Generals");
            yield return Path.Combine(root, "Electronic Arts", "Command and Conquer Generals");
            yield return Path.Combine(root, "EA Games", "Command and Conquer(tm) Generals");
        }
    }

    private static bool Repair(string found)
    {
        // Always point the per-user key at the install the launcher found.  A stale HKLM key after
        // an uninstall/reinstall otherwise sends the game at a folder whose INIZH.big is gone.
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
                    if (path is not null && HasBase(path))
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

    private static bool TryJunction(string link, string target)
    {
        try
        {
            var info = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            info.ArgumentList.Add("/c");
            info.ArgumentList.Add($"mklink /J \"{link}\" \"{target}\"");
            using var process = Process.Start(info);
            if (process is null)
                return false;
            process.WaitForExit(15000);
            return process.ExitCode == 0 && HasBase(link);
        }
        catch
        {
            return false;
        }
    }

    private static void RemoveDirectory(string path)
    {
        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(path);
        }
        catch
        {
            return;
        }
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            var info = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            info.ArgumentList.Add("/c");
            info.ArgumentList.Add($"rmdir \"{path}\"");
            using var process = Process.Start(info);
            process?.WaitForExit(15000);
            return;
        }
        Directory.Delete(path, true);
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
