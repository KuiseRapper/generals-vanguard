using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using VanguardLauncher.Core;

// A local server stands in for the GitHub release asset.  The test covers the part that can break a
// real update: manifest parsing, the hash check, the zip layout and the version comparison.
var root = Path.Combine(Path.GetTempPath(), "vanguard-launcher-test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var package = Path.Combine(root, "package");
    Directory.CreateDirectory(package);
    File.WriteAllText(Path.Combine(package, "generals.exe"), "not a real exe");
    Directory.CreateDirectory(Path.Combine(package, "Data"));
    File.WriteAllText(Path.Combine(package, "Data", "test.txt"), "data");
    var zip = Path.Combine(root, "VanguardGame.zip");
    ZipFile.CreateFromDirectory(package, zip);
    var hash = UpdateService.Sha256File(zip);

    var listener = new HttpListener();
    var port = FreePort();
    listener.Prefixes.Add($"http://127.0.0.1:{port}/");
    listener.Start();
    var manifest = new LauncherManifest
    {
        Launcher = new LauncherRelease
        {
            Version = "9.9.9", Url = $"http://127.0.0.1:{port}/VanguardLauncher.exe", Sha256 = "00",
        },
        Game = new GameRelease
        {
            Version = "9.9.9", Url = $"http://127.0.0.1:{port}/VanguardGame.zip", Sha256 = hash,
            Size = new FileInfo(zip).Length,
        },
    };
    var manifestJson = JsonSerializer.Serialize(manifest);
    var server = Task.Run(async () =>
    {
        while (listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch
            {
                return;
            }
            byte[] body;
            if (context.Request.Url!.AbsolutePath == "/launcher.json")
            {
                body = Encoding.UTF8.GetBytes(manifestJson);
                context.Response.ContentType = "application/json";
            }
            else
            {
                body = await File.ReadAllBytesAsync(zip);
                context.Response.ContentType = "application/zip";
            }
            context.Response.ContentLength64 = body.Length;
            await context.Response.OutputStream.WriteAsync(body);
            context.Response.Close();
        }
    });

    var fetched = await UpdateService.FetchManifestAsync($"http://127.0.0.1:{port}/launcher.json");
    Assert(fetched.Game?.Version == "9.9.9", "manifest version");
    Assert(UpdateService.IsNewer("0.3.0", "0.2.0"), "newer version");
    Assert(!UpdateService.IsNewer("0.2.0", "0.2.0"), "same version");
    Assert(!UpdateService.IsNewer("0.2.0", "0.3.0"), "older version");

    var game = Path.Combine(root, "game");
    await UpdateService.InstallGameAsync(fetched.Game!, game, null);
    Assert(File.Exists(Path.Combine(game, "generals.exe")), "game exe installed");
    Assert(File.Exists(Path.Combine(game, "Data", "test.txt")), "game data installed");
    Assert(File.ReadAllText(Path.Combine(game, "version.txt")) == "9.9.9", "version recorded");

    var bad = new GameRelease
    {
        Version = "9.9.9", Url = fetched.Game!.Url, Sha256 = new string('0', 64),
    };
    var failed = false;
    try
    {
        await UpdateService.InstallGameAsync(bad, Path.Combine(root, "bad"), null);
    }
    catch (InvalidDataException)
    {
        failed = true;
    }
    Assert(failed, "a bad hash is refused");

    listener.Stop();
    await server;
    Console.WriteLine("launcher selftest ok");

    // --live installs the real latest release through the same code path.  It is optional because
    // it needs the network and a 50 MB download; the local test above always runs.
    if (args.Contains("--live"))
    {
        var live = await UpdateService.FetchManifestAsync(
            "https://github.com/KuiseRapper/generals-vanguard/releases/latest/download/launcher.json");
        var liveDir = Path.Combine(root, "live-game");
        await UpdateService.InstallGameAsync(live.Game!, liveDir, null);
        Assert(File.Exists(Path.Combine(liveDir, "generals.exe")), "live game exe installed");
        Assert(File.Exists(Path.Combine(liveDir, "Data", "INI", "RussiaReforged.ini")),
            "live game carries the Russia data");
        Assert(File.ReadAllText(Path.Combine(liveDir, "version.txt")) == live.Game!.Version,
            "live version recorded");
        Console.WriteLine("live launcher update ok: " + live.Game.Version);
    }
    return 0;
}
finally
{
    try
    {
        Directory.Delete(root, true);
    }
    catch
    {
        // A leftover temp directory is not a test failure.
    }
}

static int FreePort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

static void Assert(bool condition, string what)
{
    if (!condition)
        throw new InvalidOperationException("selftest failed: " + what);
}
