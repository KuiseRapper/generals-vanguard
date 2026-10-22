using System.Diagnostics;
using System.Text;

namespace VanguardLauncher.Core;

/// <summary>
/// Swap the running launcher exe for a downloaded one.  Windows will not let the process overwrite its
/// own image, so a small cmd helper waits for this process to exit, moves the new file into place and
/// restarts it.  The launcher calls this and then exits.
/// </summary>
public static class SelfUpdater
{
    public static void Launch(string newExe, string currentExe)
    {
        var image = Path.GetFileNameWithoutExtension(currentExe);
        var script = Path.Combine(Path.GetTempPath(), $"vanguard-update-{Guid.NewGuid():N}.cmd");
        var text = $"""
@echo off
:wait
tasklist /fi "imagename eq {image}.exe" 2>nul | find /i "{image}.exe" >nul
if not errorlevel 1 (
  timeout /t 1 /nobreak >nul
  goto wait
)
:replace
move /y "{newExe}" "{currentExe}" >nul 2>&1
if exist "{newExe}" (
  timeout /t 1 /nobreak >nul
  goto replace
)
start "" "{currentExe}"
del "%~f0" >nul 2>&1
""";
        File.WriteAllText(script, text.Replace("\n", "\r\n"), new UTF8Encoding(false));
        var info = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        info.ArgumentList.Add("/c");
        info.ArgumentList.Add(script);
        Process.Start(info);
    }
}
