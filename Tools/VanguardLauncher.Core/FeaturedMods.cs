namespace VanguardLauncher.Core;

/// <summary>A mod the launcher offers before anything is downloaded.  The files are never bundled.</summary>
public sealed class FeaturedMod
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Version { get; init; } = "1.0";
    public string Detail { get; init; } = "";
    public string DownloadUrl { get; init; } = "";
    public string PageUrl { get; init; } = "";
    public string SizeText { get; init; } = "";
    public bool Preview { get; init; }
    public IReadOnlyList<string> Includes { get; init; } = Array.Empty<string>();
}

/// <summary>
/// The three mods the launcher lists out of the box.  They stay on the release that first carried
/// them: the assets are large and every later release does not need to carry another 1.5 GB copy.
/// Downloading one is always an explicit choice; nothing is fetched until the player asks.
/// </summary>
public static class FeaturedMods
{
    private const string Release = "https://github.com/KuiseRapper/generals-vanguard/releases/download/v0.5.11";
    private const string Project = "https://github.com/KuiseRapper/generals-vanguard";

    public static IReadOnlyList<FeaturedMod> All { get; } = new[]
    {
        new FeaturedMod
        {
            Id = "vanguardcombined",
            Title = "Vanguard Combined",
            Version = "0.1",
            Detail = "The full Vanguard mod: ShockWave's armies and generals plus Rise of the Reds' "
                     + "Russian Federation and ECA in one data set.",
            DownloadUrl = Release + "/VanguardCombined-0.1.zip",
            PageUrl = Project,
            SizeText = "775 MB",
            Preview = true,
            Includes = new[]
            {
                "ShockWave 1.201 as the base: USA, China and GLA, their generals, units and balance",
                "Rise of the Reds' Russian Federation and ECA as two more armies",
                "Vanguard's engine fixes, AI, performance and balance underneath both",
                "Credits: SWR Productions for ShockWave and Rise of the Reds",
            },
        },
        new FeaturedMod
        {
            Id = "shockwave",
            Title = "ShockWave 1.201",
            Version = "1.201",
            Detail = "ShockWave on its own: USA, China and GLA with its extra generals, units and balance.",
            DownloadUrl = Release + "/ShockWave-1.1-noinstaller.zip",
            PageUrl = "https://www.moddb.com/mods/cc-shockwave/downloads/shockwave-version-12",
            SizeText = "288 MB",
            Includes = new[]
            {
                "ShockWave's USA, China and GLA: extra generals, units, structures and powers",
                "ShockWave's UI, cameos and balance",
                "Its challenge maps and skirmish AI",
            },
        },
        new FeaturedMod
        {
            Id = "rotr",
            Title = "Rise of the Reds 1.8",
            Version = "1.8",
            Detail = "Rise of the Reds on its own: its Russian Federation and ECA, with its own take "
                     + "on the classic armies.",
            DownloadUrl = Release + "/RiseOfTheReds-1.8-noinstaller.zip",
            PageUrl = "https://www.moddb.com/mods/rise-of-the-reds/downloads/rotr-eca-beta-18-no-installer-mac-compatible",
            SizeText = "500 MB",
            Includes = new[]
            {
                "Rise of the Reds' Russian Federation and European Continental Alliance",
                "Their buildings, vehicles, aircraft, infantry, powers and cameos",
                "RotR's reworked USA, China and GLA",
            },
        },
    };

    public static FeaturedMod? Find(string id) =>
        All.FirstOrDefault(mod => mod.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
