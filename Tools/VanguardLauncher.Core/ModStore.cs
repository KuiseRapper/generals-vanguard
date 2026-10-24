using System.IO.Compression;
using System.Text.Json;

namespace VanguardLauncher.Core;

/// <summary>A mod folder the launcher found under <see cref="ModStore.Root"/>.</summary>
public sealed record InstalledMod(string Id, string Name, string Version, string Directory);

/// <summary>
/// The player's mod folder.  Each mod is one directory holding the archives and loose Data its
/// package ships, plus a mod.json the engine reads for the version line.  Nothing here touches the
/// game folder, so a game update cannot remove a downloaded mod.
/// </summary>
public sealed class ModStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ModStore(string? root = null)
    {
        Root = Path.GetFullPath(root ?? DefaultRoot);
    }

    /// <summary>On Windows this is the real Documents folder, including a OneDrive redirect.</summary>
    public static string DefaultRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Generals Vanguard",
        "Mods");

    public string Root { get; }

    public void Ensure() => Directory.CreateDirectory(Root);

    public IReadOnlyList<InstalledMod> Scan()
    {
        if (!Directory.Exists(Root))
            return Array.Empty<InstalledMod>();

        var found = new List<InstalledMod>();
        foreach (var directory in Directory.GetDirectories(Root))
        {
            var id = Path.GetFileName(directory);
            if (string.IsNullOrWhiteSpace(id) || id.EndsWith(".new", StringComparison.OrdinalIgnoreCase)
                || id.EndsWith(".old", StringComparison.OrdinalIgnoreCase)
                || !HasBig(directory))
                continue;

            var name = id;
            var version = "1.0";
            var manifest = Path.Combine(directory, "mod.json");
            if (File.Exists(manifest))
            {
                try
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(manifest));
                    if (document.RootElement.TryGetProperty("name", out var nameValue)
                        && nameValue.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(nameValue.GetString()))
                        name = nameValue.GetString()!;
                    if (document.RootElement.TryGetProperty("version", out var versionValue)
                        && versionValue.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(versionValue.GetString()))
                        version = versionValue.GetString()!;
                }
                catch
                {
                    // A malformed manifest leaves the folder and the engine's own default name.
                }
            }
            found.Add(new InstalledMod(id, name, version, directory));
        }

        return found
            .OrderBy(mod => mod.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public bool IsInstalled(string id) => HasBig(DirectoryFor(id));

    public string DirectoryFor(string id) => Path.Combine(Root, SafeId(id));

    /// <summary>
    /// Install a .zip or a single .big.  The archive is unpacked beside the live folder, the single
    /// wrapping directory retail mod zips usually carry is flattened, and only a package that really
    /// holds a .big is allowed to replace what is there.
    /// </summary>
    public InstalledMod Install(string archivePath, string id, string name, string version)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("The mod archive was not found.", archivePath);

        var isBig = archivePath.EndsWith(".big", StringComparison.OrdinalIgnoreCase);
        if (!isBig && !LooksLikeZip(archivePath))
            throw new InvalidDataException(
                "The download is not a zip file. Check the connection and try Download again.");

        var destination = DirectoryFor(id);
        var staging = destination + ".new";
        DeleteTree(staging);
        Directory.CreateDirectory(staging);
        try
        {
            if (isBig)
            {
                File.Copy(archivePath, Path.Combine(staging, Path.GetFileName(archivePath)), false);
            }
            else
            {
                ZipFile.ExtractToDirectory(archivePath, staging);
                FlattenSingleDirectory(staging);
            }

            if (!HasBig(staging))
                throw new InvalidDataException(
                    "This download has no .big files, so it is not a playable mod package. Try downloading it again.");

            WriteManifest(staging, name, version);
            Replace(destination, staging);
            return new InstalledMod(SafeId(id), name, version, destination);
        }
        catch
        {
            DeleteTree(staging);
            throw;
        }
    }

    public void Remove(string id) => DeleteTree(DirectoryFor(id));

    public static string SafeId(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value
            .Trim()
            .Select(character => invalid.Contains(character) || character == '/' || character == '\\'
                ? '-'
                : character)
            .ToArray());
        cleaned = cleaned.Trim('.', ' ');
        return string.IsNullOrWhiteSpace(cleaned) ? "mod" : cleaned;
    }

    private static bool HasBig(string directory)
    {
        if (!Directory.Exists(directory))
            return false;
        return Directory.GetFiles(directory, "*.big", SearchOption.TopDirectoryOnly).Length > 0;
    }

    private static bool LooksLikeZip(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> magic = stackalloc byte[4];
            return stream.Read(magic) == 4 && magic[0] == 0x50 && magic[1] == 0x4b
                   && magic[2] == 0x03 && magic[3] == 0x04;
        }
        catch
        {
            return false;
        }
    }

    private static void FlattenSingleDirectory(string root)
    {
        // macOS adds a metadata folder to some zips; it is not part of the mod.
        var metadata = Path.Combine(root, "__MACOSX");
        DeleteTree(metadata);

        var visible = Directory.GetFileSystemEntries(root)
            .Where(entry => !Path.GetFileName(entry).StartsWith(".", StringComparison.Ordinal)
                            && !Path.GetFileName(entry).StartsWith("._", StringComparison.Ordinal))
            .ToArray();
        if (visible.Length != 1 || !Directory.Exists(visible[0]))
            return;

        var wrapper = visible[0];
        foreach (var entry in Directory.GetFileSystemEntries(wrapper))
        {
            var target = Path.Combine(root, Path.GetFileName(entry));
            if (Directory.Exists(entry))
                Directory.Move(entry, target);
            else
                File.Move(entry, target);
        }
        Directory.Delete(wrapper, false);
    }

    private static void WriteManifest(string directory, string name, string version)
    {
        var manifest = new Dictionary<string, string>
        {
            ["name"] = name,
            ["version"] = version,
        };
        File.WriteAllText(Path.Combine(directory, "mod.json"),
            JsonSerializer.Serialize(manifest, JsonOptions));
    }

    private static void Replace(string destination, string staging)
    {
        var old = destination + ".old";
        DeleteTree(old);
        var moved = false;
        try
        {
            if (Directory.Exists(destination))
            {
                Directory.Move(destination, old);
                moved = true;
            }
            Directory.Move(staging, destination);
            DeleteTree(old);
        }
        catch
        {
            if (moved && !Directory.Exists(destination) && Directory.Exists(old))
                Directory.Move(old, destination);
            throw;
        }
    }

    /// <summary>Delete a tree without following a directory link out of the mods folder.</summary>
    private static void DeleteTree(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                if (File.Exists(path))
                    File.Delete(path);
                return;
            }
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
        catch
        {
            // A leftover folder is better than a launcher that refuses to continue.
        }
    }
}
