# macOS launcher

Download **`VanguardLauncher-mac.zip`** from
[v0.5.18](https://github.com/KuiseRapper/generals-vanguard/releases/tag/v0.5.18) (0.6.0 is the
Windows release), unzip it and drag **Generals Vanguard** to Applications.

The launcher:

- installs the macOS game app under `~/Library/Application Support/GeneralsVanguard/game`;
- looks for your Zero Hour folder at `~/zh-data/zerohour`, and otherwise asks you to pick the folder
  that holds `INIZH.big`;
- launches the app as `Generals Vanguard.app/Contents/MacOS/generals -root <your Zero Hour folder>`;
- offers **Vanguard Combined**, **ShockWave 1.201** and **Rise of the Reds 1.8** in its **Mods** sheet,
  with nothing downloaded until you ask, and passes the selected one as `-mod`;
- checks `launcher-mac.json` for a newer game and installs it after a SHA-256 check; a newer launcher
  itself is downloaded as a DMG and opened for the usual drag-to-Applications step.

The macOS game app is built from the same tree as the Windows build; it contains the fork's data
overlay and reads your own Zero Hour data, so no EA game files are redistributed.
