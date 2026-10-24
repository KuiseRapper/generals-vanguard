using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace VanguardLauncher.Core;

/// <summary>
/// The update path: read the release manifest, download the game package, check its hash and put it in
/// place.  Everything here is platform-neutral so the test project can exercise it on the build host.
/// </summary>
public static class UpdateService
{
    public static readonly string ReleasePage =
        "https://github.com/KuiseRapper/generals-vanguard/releases/latest";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("VanguardLauncher/0.3");
        return client;
    }

    public static async Task<LauncherManifest> FetchManifestAsync(string url, CancellationToken ct = default)
    {
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var manifest = await JsonSerializer.DeserializeAsync<LauncherManifest>(stream, JsonOptions, ct);
        if (manifest?.Game is null || string.IsNullOrWhiteSpace(manifest.Game.Url))
            throw new InvalidDataException("The launcher manifest has no game entry.");
        return manifest;
    }

    /// <summary>True when <paramref name="available"/> is a higher dotted version than <paramref name="installed"/>.</summary>
    public static bool IsNewer(string available, string installed)
    {
        return Version.TryParse(available.TrimStart('v'), out var a)
               && Version.TryParse(installed.TrimStart('v'), out var i)
               && a > i;
    }

    public static async Task DownloadFileAsync(
        string url, string destination, IProgress<double>? progress, CancellationToken ct = default)
    {
        var part = destination + ".part";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        try
        {
            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? -1;
            await using var input = await response.Content.ReadAsStreamAsync(ct);
            await using var output = new FileStream(part, FileMode.Create, FileAccess.Write, FileShare.None,
                81920, useAsync: true);
            var buffer = new byte[81920];
            long read = 0;
            int n;
            while ((n = await input.ReadAsync(buffer, ct)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, n), ct);
                read += n;
                if (total > 0)
                    progress?.Report((double)read / total);
            }
        }
        catch
        {
            TryDelete(part);
            throw;
        }
        File.Move(part, destination, true);
    }

    public static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    public static void VerifySha256(string path, string expected)
    {
        var actual = Sha256File(path);
        if (!actual.Equals(expected.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"The download is corrupt (sha256 {actual}, expected {expected}).");
    }

    /// <summary>
    /// Download and install the game zip next to the launcher.  The old folder is only removed after
    /// the new one is complete, and the manifest version is written for the next update check.
    /// </summary>
    public static async Task InstallGameAsync(
        GameRelease release, string gameDirectory, IProgress<double>? progress, CancellationToken ct = default)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(gameDirectory))!;
        Directory.CreateDirectory(parent);
        var zip = Path.Combine(parent, "VanguardGame.download.zip");
        try
        {
            await DownloadFileAsync(release.Url, zip, progress, ct);
            VerifySha256(zip, release.Sha256);

            var staging = gameDirectory + ".new";
            if (Directory.Exists(staging))
                DeleteTree(staging);
            ZipFile.ExtractToDirectory(zip, staging);

            // A zip made with "GeneralsMD/Run/*" has generals.exe at the root; tolerate one wrapping folder too.
            var exe = Path.Combine(staging, "generals.exe");
            if (!File.Exists(exe))
            {
                var subdirectories = Directory.GetDirectories(staging);
                if (subdirectories.Length == 1 && File.Exists(Path.Combine(subdirectories[0], "generals.exe")))
                {
                    var wrapped = subdirectories[0];
                    foreach (var entry in Directory.GetFileSystemEntries(wrapped))
                    {
                        var target = Path.Combine(staging, Path.GetFileName(entry));
                        if (Directory.Exists(entry))
                            Directory.Move(entry, target);
                        else
                            File.Move(entry, target);
                    }
                    Directory.Delete(wrapped);
                }
            }
            if (!File.Exists(Path.Combine(staging, "generals.exe")))
                throw new InvalidDataException("The game package has no generals.exe.");

            File.WriteAllText(Path.Combine(staging, "version.txt"), release.Version);

            var old = gameDirectory + ".old";
            if (Directory.Exists(old))
                DeleteTree(old);
            if (Directory.Exists(gameDirectory))
                Directory.Move(gameDirectory, old);
            Directory.Move(staging, gameDirectory);
            if (Directory.Exists(old))
                DeleteTree(old);
        }
        finally
        {
            TryDelete(zip);
        }
    }

    /// <summary>Download and verify a newer launcher exe; returns its path.</summary>
    public static async Task<string> DownloadLauncherAsync(
        LauncherRelease release, IProgress<double>? progress, CancellationToken ct = default)
    {
        var target = Path.Combine(Path.GetTempPath(), "VanguardLauncher.new.exe");
        await DownloadFileAsync(release.Url, target, progress, ct);
        VerifySha256(target, release.Sha256);
        return target;
    }

    /// <summary>
    /// Delete a tree without following a directory reparse point.  The game folder can hold
    /// ZH_Generals as a junction to the player's own Zero Hour install; a recursive delete would walk
    /// into it and remove their game files, so a link is removed as a link.
    /// </summary>
    private static void DeleteTree(string path)
    {
        if (!Directory.Exists(path))
            return;
        foreach (var entry in Directory.GetFileSystemEntries(path))
        {
            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.Directory) != 0)
            {
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    Directory.Delete(entry, false);
                else
                    DeleteTree(entry);
            }
            else
            {
                File.Delete(entry);
            }
        }
        Directory.Delete(path, false);
    }

    public static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // A locked temp file is not worth failing the install over.
        }
    }
}
