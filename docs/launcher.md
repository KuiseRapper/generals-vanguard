# Launcher

`VanguardLauncher.exe` is the Windows front door: it installs the current Vanguard game build, starts
it, and updates both the game and itself from GitHub releases. It is a .NET 8 WinForms application,
published self-contained for Windows x64; the source is in `Tools/VanguardLauncher`.

It does **not** ship EA's game files. The game build it installs is the fork's `generals.exe`, Data and
scripts; Zero Hour itself must already be installed, because the game reads its `*.big` files from
that installation. A Steam copy still starts through Steam.

It looks for **Zero Hour**, not the base game: the `InstallPath` under
`HKCU`/`HKLM\SOFTWARE\Electronic Arts\EA Games\Command and Conquer Generals Zero Hour` (both
registry views), then Steam's `libraryfolders.vdf` and the EA app's default folders. A folder counts
only if it holds `INIZH.big`. If the registry key is missing or points at a folder without that file
and a real Zero Hour folder is found elsewhere, the launcher writes the per-user key so the game finds
it. The launcher window shows which Zero Hour folder it is using.

It starts the game with `-rk7`, the marker `WinMain.cpp` requires; starting `generals.exe` directly
shows *"Please start Generals Vanguard from its launcher."*

## Using it

1. Download `VanguardLauncher.exe` from the
   [latest release](https://github.com/KuiseRapper/generals-vanguard/releases/latest). Put it anywhere
   writable; it keeps its game in a `game` folder beside itself.
2. Run it. **Check for updates** downloads the game package from the same release when the game folder
   is empty or older; **Play** starts `game\generals.exe`.
3. To use a build you compiled yourself, **Set game folder** points the launcher at the folder holding
   `generals.exe` (normally `GeneralsMD\Run`). Updates still install into the launcher's own `game`
   folder, so they cannot overwrite your build.

The launcher refuses to update while `generals.exe` is running, because Windows would keep the old
files locked.

## Updates

The launcher reads this asset from the newest release:

```
https://github.com/KuiseRapper/generals-vanguard/releases/latest/download/launcher.json
```

The manifest names the two files and their SHA-256 hashes:

```json
{
  "launcher": {
    "version": "0.3.2",
    "url": "https://github.com/KuiseRapper/generals-vanguard/releases/download/v0.3.2/VanguardLauncher.exe",
    "sha256": "..."
  },
  "game": {
    "version": "0.3.2",
    "url": "https://github.com/KuiseRapper/generals-vanguard/releases/download/v0.3.2/VanguardGame.zip",
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
for the launcher to exit, moves the new file into place and restarts it. Put the launcher somewhere
writable (not `Program Files`) or self-update will fail and the launcher will say so.

The exe is not code-signed, so Windows SmartScreen may ask before the first run: **More info** →
**Run anyway**. The download is hashed against the release manifest, but the manifest itself is served
over HTTPS from the same release, so the release account is the trust root.

## Building it

The launcher builds with the .NET 8 SDK:

```
dotnet publish Tools/VanguardLauncher/VanguardLauncher.csproj ^
  -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:DebugType=None ^
  -o dist
```

The result is a single `VanguardLauncher.exe` (about 150 MB; the .NET runtime is inside it). It can be
cross-published from macOS or Linux with the same command; the project sets
`EnableWindowsTargeting` for that.

The update core is platform-neutral and has a self-contained test:

```
dotnet run --project Tools/VanguardLauncher.Tests/VanguardLauncher.Tests.csproj -c Release
```

It serves a manifest and a zip from a local `HttpListener` and checks the manifest parse, the SHA-256
refusal, the zip layout and the version comparison. The WinForms window itself can only be run on
Windows.

## Cutting a release

`.github/workflows/release.yml` builds the game and the launcher on `windows-latest` and puts three
assets on a GitHub release:

| Asset | What it is |
|---|---|
| `VanguardLauncher.exe` | the launcher; the download users want |
| `VanguardGame.zip` | the same game build for a manual install |
| `launcher.json` | the update manifest the launcher reads |

Run it by pushing a tag:

```
git tag v0.3.2
git push origin v0.3.2
```

or from the Actions tab (**Run workflow**) with the version, or:

```
gh workflow run release.yml --ref develop -f version=v0.3.2
```

The workflow needs **Read and write permissions** for `GITHUB_TOKEN` in the repository's
**Settings → Actions → General**; it uploads the same three files as a workflow artifact as well, so a
failed release step does not lose the build.

The launcher's assembly version is in `Tools/VanguardLauncher/VanguardLauncher.csproj`; bump it with
the tag so the self-update check has something to compare.

## Limits

- Windows x64 only. Windows on ARM64 can run it under emulation; there is no native ARM64 publish yet.
- No delta patches: an update downloads the whole `VanguardGame.zip`.
- The launcher has no code signing certificate, so SmartScreen is expected to warn.
- It does not install Zero Hour, and it cannot update EA's `*.big` files.
- The game folder it manages is `game` beside the exe; a custom **Set game folder** build is played
  but never updated in place.
