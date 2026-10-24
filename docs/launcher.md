# Launcher

`VanguardLauncher.exe` is the Windows front door: it installs the current Vanguard game build, starts
it, lists the optional mods, and updates both the game and itself from GitHub releases. It is a .NET 8
WinForms application, published self-contained for Windows x64; the source is in
`Tools/VanguardLauncher`.

It does **not** ship EA's game files. The game build it installs is the fork's `generals.exe`, Data and
scripts; Zero Hour itself must already be installed, because the game reads its `*.big` files from
that installation. A Steam copy still starts through Steam.

Zero Hour shares most of its art, sound and music with the base game. It keeps those shared files in a
`ZH_Generals` folder beside its `generals.exe` (Steam does this); the launcher links that folder into
its own `game` folder as `ZH_Generals` before Play, from the Zero Hour install, a registered Generals
install, or a Steam sibling. If Windows refuses the junction, the `.big` files are copied. A separate
base-game install is **not** required.

It looks for **Zero Hour**, not the base game: the `InstallPath` under
`HKCU`/`HKLM\SOFTWARE\Electronic Arts\EA Games\Command and Conquer Generals Zero Hour` (both
registry views), then Steam's `libraryfolders.vdf` and the EA app's default folders. A folder counts
only if it holds `INIZH.big`. If the registry key is missing or points at a folder without that file
and a real Zero Hour folder is found elsewhere, the launcher writes the per-user key so the game finds
it. The launcher window shows which Zero Hour folder it is using.

It starts the game with `-rk7`, the marker `WinMain.cpp` requires; starting `generals.exe` directly
shows *"Please start Generals Vanguard from its launcher."*

## Installing it

The recommended download is **`VanguardSetup.exe`**, built by `Tools/VanguardInstaller/VanguardSetup.iss`
with Inno Setup 6. It:

1. installs the launcher in `%LOCALAPPDATA%\Programs\Generals Vanguard` - a writable folder, so the
   launcher can replace its own exe when it updates;
2. puts a Start Menu entry and, unless you untick it, a desktop shortcut in place;
3. starts `VanguardLauncher.exe --install`, which downloads the current game, checks its SHA-256,
   connects the Zero Hour base files and cursors, repairs the per-user Zero Hour path, and leaves the
   launcher ready on its main screen.

The installer installs **no game data and no mod**. It uses `PrivilegesRequired=lowest`, so it never
asks for administrator rights, and the uninstaller removes the game folder it installed but leaves
your downloaded mods in Documents alone.

`VanguardLauncher.exe` is still published on every release for anyone who would rather not run an
installer. Put it anywhere writable; it keeps its game in a `game` folder beside itself.

## The main screen

The window is a single screen: the hero art on the left, the game and launcher versions, **Play**,
**Check for updates**, **Mods**, and the Zero Hour/base-file status on the right. Check for updates
opens a review screen with the release notes and an Update/Cancel choice; nothing is written until
Update is pressed.

Play starts `game\generals.exe`. To use a build you compiled yourself, **Set game folder** points the
launcher at the folder holding `generals.exe` (normally `GeneralsMD\Run`). Updates still install into
the launcher's own `game` folder, so they cannot overwrite your build.

The launcher refuses to update while `generals.exe` is running, because Windows would keep the old
files locked.

## Mods

**Mods** opens a screen with the three packages the launcher offers:

| Mod | What it is | Package |
|---|---|---|
| Vanguard Combined | ShockWave's armies and generals plus Rise of the Reds' Russia and ECA in one data set | 775 MB |
| ShockWave 1.201 | ShockWave on its own: USA, China and GLA, extra generals, units and balance | 288 MB |
| Rise of the Reds 1.8 | RotR's Russian Federation and ECA, with its own take on the classic armies | 500 MB |

All three are listed **before anything is downloaded**. **Download** fetches the package from the
release that first carried it (~v0.5.11, so a later game release does not carry another 1.5 GB), shows
progress, and unpacks it into:

```
%USERPROFILE%\Documents\Generals Vanguard\Mods\<id>\
```

The folder gets the archives and loose `Data` the package ships, plus a `mod.json` that names it and
its version. Play then passes `-mod "<absolute path>"`, which the engine accepts; the mod's files load
over the game's. Tick the checkbox on one row to play that mod; tick none to play vanilla Vanguard.
**Remove** deletes only that folder.

Because mods live in Documents rather than the launcher's `game` folder, **Check for updates** cannot
delete one. **Install from file...** accepts a `.zip` or a `.big`, and **Open mods folder** shows where
they are.

The three packages are third-party or community work, are not bundled, and keep their own credits -
ShockWave and Rise of the Reds are by SWR Productions. The combined mod is an unofficial merge.

## Updates

The launcher reads this asset from the newest release:

```
https://github.com/KuiseRapper/generals-vanguard/releases/latest/download/launcher.json
```

The manifest names the two files and their SHA-256 hashes:

```json
{
  "launcher": {
    "version": "0.6.0",
    "url": "https://github.com/KuiseRapper/generals-vanguard/releases/download/v0.6.0/VanguardLauncher.exe",
    "sha256": "..."
  },
  "game": {
    "version": "0.6.0",
    "url": "https://github.com/KuiseRapper/generals-vanguard/releases/download/v0.6.0/VanguardGame.zip",
    "sha256": "...",
    "size": 12345678
  }
}
```

The game package is downloaded to a temporary zip, its SHA-256 is checked, it is extracted to
`game.new`, and only then is `game` moved aside and the new folder moved into its place. A corrupt
download is refused before anything is replaced. The version is written into `game\version.txt` for
the next check.

A newer launcher follows the same hash check. Windows will not let the launcher overwrite its own
running image, so it downloads `VanguardLauncher.new.exe` and starts a small `cmd` helper that waits
for the launcher to exit, moves the new file into place and restarts it. The installer's LocalAppData
folder is writable for exactly this; a launcher copied into `Program Files` cannot update itself and
says so.

The exe and the installer are not code-signed, so Windows SmartScreen may ask before the first run:
**More info** → **Run anyway**. The download is hashed against the release manifest, but the manifest
itself is served over HTTPS from the same release, so the release account is the trust root.

## Building it

The launcher builds with the .NET 8 SDK:

```
dotnet publish Tools/VanguardLauncher/VanguardLauncher.csproj ^
  -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:DebugType=None ^
  -o dist/launcher
```

The result is a single `VanguardLauncher.exe` (about 150 MB; the .NET runtime is inside it). It can be
cross-published from macOS or Linux with the same command; the project sets
`EnableWindowsTargeting` for that.

The installer is built from that publish output with Inno Setup 6:

```
Tools\VanguardInstaller\build.ps1 -Version 0.6.0
```

It writes `dist\VanguardSetup.exe`. The release workflow installs Inno Setup and runs the same script.

The update core is platform-neutral and has a self-contained test:

```
dotnet run --project Tools/VanguardLauncher.Tests/VanguardLauncher.Tests.csproj -c Release
```

It serves a manifest and a zip from a local `HttpListener` and checks the manifest parse, the SHA-256
refusal, the zip layout, the version comparison, and the mod store (a packaged `.big` installs,
lists, removes, and a package without one is refused). The WinForms window itself can only be run on
Windows.

## Cutting a release

`.github/workflows/release.yml` builds the game, runs the launcher tests, publishes the launcher and
the installer on `windows-2022`, and puts four assets on a GitHub release:

| Asset | What it is |
|---|---|
| `VanguardSetup.exe` | the one-click Windows installer; the download users want |
| `VanguardLauncher.exe` | the launcher alone, for a manual install |
| `VanguardGame.zip` | the game build for a manual install |
| `launcher.json` | the update manifest the launcher reads |

Run it by pushing a tag:

```
git tag v0.6.0
git push origin v0.6.0
```

or from the Actions tab (**Run workflow**) with the version, or:

```
gh workflow run release.yml --ref develop -f version=v0.6.0
```

The workflow needs **Read and write permissions** for `GITHUB_TOKEN` in the repository's
**Settings → Actions → General**; it uploads the same four files as a workflow artifact as well, so a
failed release step does not lose the build.

The launcher's assembly version is in `Tools/VanguardLauncher/VanguardLauncher.csproj`; bump it with
the tag so the self-update check has something to compare.

## Limits

- Windows x64 only. Windows on ARM64 can run it under emulation; there is no native ARM64 publish yet.
- No delta patches: an update downloads the whole `VanguardGame.zip`.
- Neither the launcher nor the installer is code-signed, so SmartScreen is expected to warn.
- It does not install Zero Hour, and it cannot update EA's `*.big` files.
- The game folder it manages is `game` beside the exe; a custom **Set game folder** build is played
  but never updated in place.
- A mod is one active choice at a time. Loading two mods together would need a merged package, which
  is what Vanguard Combined is.
