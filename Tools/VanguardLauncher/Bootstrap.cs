using System.Diagnostics;

namespace VanguardLauncher;

/// <summary>
/// The downloaded launcher installs itself the first time it runs.  This keeps the release to the
/// one file a player downloads: it is copied into LocalAppData (a writable place, so self-update
/// keeps working), the shortcuts are created, and the installed copy starts the game download.
/// A build tree or a portable copy with its own game folder is left where it is.
/// </summary>
internal static class Bootstrap
{
    private const string AppName = "Generals Vanguard";
    private const string ExeName = "VanguardLauncher.exe";

    public static string InstallDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        AppName);

    /// <summary>True when the caller should stop: the installed copy has been started.</summary>
    public static bool TrySelfInstall(string[] args)
    {
        if (args.Any(argument =>
                argument.Equals("--portable", StringComparison.OrdinalIgnoreCase)
                || argument.Equals("--no-self-install", StringComparison.OrdinalIgnoreCase)))
            return false;

        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            return false;
        // A framework-dependent `dotnet run` points ProcessPath at dotnet itself; never copy that.
        if (!Path.GetFileName(exe).Equals(ExeName, StringComparison.OrdinalIgnoreCase))
            return false;

        var current = Path.GetFullPath(Path.GetDirectoryName(exe)!);
        var install = Path.GetFullPath(InstallDirectory);
        if (string.Equals(Trim(current), Trim(install), StringComparison.OrdinalIgnoreCase))
            return false;
        if (LooksLikeWorkingTree(current))
            return false;

        try
        {
            Directory.CreateDirectory(install);
            var target = Path.Combine(install, ExeName);
            if (!File.Exists(target) || IsNewer(exe, target))
                File.Copy(exe, target, true);

            CreateShortcuts(target, install);

            var info = new ProcessStartInfo(target)
            {
                WorkingDirectory = install,
                UseShellExecute = true,
                Arguments = "--install",
            };
            Process.Start(info);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Generals Vanguard could not install itself in:\n" + install + "\n\n"
                + ex.Message + "\n\nThe launcher will run from this folder instead.",
                "Could not install the launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    public static bool CreateShortcuts(string target, string workingDirectory)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
                return false;
            dynamic shell = Activator.CreateInstance(shellType)!;

            var desktop = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                AppName + ".lnk");
            WriteShortcut(shell, desktop, target, workingDirectory);

            var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            if (!string.IsNullOrWhiteSpace(programs))
            {
                Directory.CreateDirectory(programs);
                WriteShortcut(shell, Path.Combine(programs, AppName + ".lnk"),
                    target, workingDirectory);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteShortcut(dynamic shell, string path, string target, string workingDirectory)
    {
        dynamic shortcut = shell.CreateShortcut(path);
        shortcut.TargetPath = target;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.IconLocation = target;
        shortcut.Save();
    }

    /// <summary>A game folder beside the exe, or a source checkout above it, is a working copy.</summary>
    private static bool LooksLikeWorkingTree(string directory)
    {
        if (File.Exists(Path.Combine(directory, "game", "generals.exe")))
            return true;
        if (File.Exists(Path.Combine(directory, "..", "GeneralsMD", "Run", "generals.exe")))
            return true;
        return File.Exists(Path.Combine(directory, "..", "..", "GeneralsMD", "Run", "generals.exe"));
    }

    private static bool IsNewer(string candidate, string installed)
    {
        try
        {
            var candidateVersion = Version.Parse(
                FileVersionInfo.GetVersionInfo(candidate).FileVersion ?? "0.0.0");
            var installedVersion = Version.Parse(
                FileVersionInfo.GetVersionInfo(installed).FileVersion ?? "0.0.0");
            return candidateVersion > installedVersion;
        }
        catch
        {
            try
            {
                return File.GetLastWriteTimeUtc(candidate) > File.GetLastWriteTimeUtc(installed);
            }
            catch
            {
                return true;
            }
        }
    }

    private static string Trim(string path) => path.TrimEnd(
        Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
