# AGENTS.md

Working notes for agents and humans on **Generals Vanguard**. This is a living document: when you
learn something the next person would otherwise have to rediscover, add a line here. Plans go in
[BACKLOG.md](BACKLOG.md), not here.

> Upstream ignores `AGENTS.md` in `.gitignore` (as a per-machine file). We un-ignore it on purpose so
> these notes travel with the fork. See "Deliberate deviations" below.

## What this is

Generals Vanguard is a fan mod for **Command & Conquer: Generals — Zero Hour**: a fork of
[olcayseygan/CnCGeneralsZH-Reforged](https://github.com/olcayseygan/CnCGeneralsZH-Reforged)
("Zero Hour Reforged", [zerohour.gg](https://zerohour.gg)), which is itself a GitHub fork of
[electronicarts/CnC_Generals_Zero_Hour](https://github.com/electronicarts/CnC_Generals_Zero_Hour).

```
EA CnC_Generals_Zero_Hour (commit 49843ea4, 2025-02-27, GPL-3.0 + EA section 7 terms)
  -> olcayseygan/CnCGeneralsZH-Reforged   x64, D3D11/Metal/Vulkan, ~60 EA bugs fixed, AI work
  -> KuiseRapper/generals-vanguard        this fork, forked at e81b9f6a (upstream v3.0.1)
```

**Do not confuse this with a different, unrelated project called "Generals Reforged"**
(`asaprockky/generals-reforged`). That is a 32-bit data mod on a different engine and shares no code
with this tree. We forked from it by mistake once and re-based; the abandoned work is archived
outside the repository (see "Archive"). Mentioning it in credits would be wrong.

### Remotes

| Remote | Points at | Use |
|---|---|---|
| `origin` | `KuiseRapper/generals-vanguard` | our fork |
| `upstream` | `olcayseygan/CnCGeneralsZH-Reforged` | pull his future work with `git fetch upstream` |

`main` mirrors upstream. **All our work goes on `develop`**, never straight onto `main`, so that
`main` stays a clean copy of upstream and `git fetch upstream && git merge upstream/main` keeps working.

## Conventions that are enforced, not preferences

**Commit messages are checked by CI** (`.github/workflows/commit-messages.yml`, on pull requests):

```
type(scope): summary      # type is feat|fix|perf|build|refactor|test|docs
                          # scope is lowercase a-z0-9._- ; docs may omit it
                          # first line under 72 characters
```

Imperative, lowercase, no trailing full stop, one change per commit. **No merge commits** — rebase
onto `main` instead. `CONTRIBUTING.md` has the long version.

**File headers are checked** by `GeneralsMD/Code/Tools/license-headers.py --check`, and
[NOTICE.md](NOTICE.md) is the contract they implement:

- A file EA released that we changed keeps EA's header exactly, plus a line directly under it:
  `Modified <year> by <name> for <what>`.
- A file we add carries our own copyright line and the GPL-3.0-or-later notice.
- A file we add that holds code taken from an EA file keeps EA's header and names the EA file.

**The build fingerprint tracks paths.** `GeneralsMD/Code/BuildFingerprint.manifest` lists every
tracked file under `GeneralsMD/Code` that the fingerprint hashes (currently 4018 entries; files at
the repository root are not in it). **Adding, removing or renaming a file under `GeneralsMD/Code`
means re-running** `GeneralsMD/Code/Tools/fingerprint-manifest.sh`, or the
`build_fingerprint_manifest` test fails. Editing an existing file's contents does not change the list
— but it does change the fingerprint value, which is what LAN and replay compatibility key off.

**Never commit game data.** No retail `.big`, art, audio or models. Upstream already agrees: its
`.gitignore` drops `**/Tools/wnd/` as "game data [that] does not belong in the repo". What the repo
does legitimately contain is the art EA released inside the source tree
(`Libraries/Source/WWVegas/WW3D2/RequiredAssets/`) and art upstream authored
(`GeneralsMD/Code/Data/Art/Textures/Reforged*.tga`, `.dds`).

## Naming and licensing

Decided 2026-10-15. Read the "Naming, credits and licensing" entry in [CHANGELOG.md](CHANGELOG.md)
before changing any of it.

- **Display name: `Generals Vanguard`.** Artifacts use `Vanguard` (`Vanguard.exe`, `Vanguard.big`).
  Our own version line **starts at 0.1.0**, independent of upstream's 3.x.
- The name carries an EA mark, which is the project owner's accepted decision. The trade-off is that
  the disclaimers must stay: never remove them.
- **EA's required sentence is currently missing from this tree** and must be added and kept in
  `README.md` and `NOTICE.md`:
  `EA has not endorsed and does not support this product.`
  EA's [Franchise Modding Guidelines](https://www.ea.com/games/command-and-conquer/news/modding-faq)
  require it in at least one root-level licence file. They also forbid using EA marks, logos or
  artwork *to promote* the mod; stating in plain text what the mod is for is allowed, so the
  descriptive line `for Command & Conquer: Generals Zero Hour` stays.
- Credits, in order: **Electronic Arts** (original games, and the source release) → **Olcay Seygan**
  (Zero Hour Reforged, our base) → **İlyas Akın** (the macOS, Linux and Windows ARM64 port) →
  **TheSuperHackers** (fixes credited where they landed) → **Vanguard**.
  Our line: `Vanguard modifications © 2026 KuiseRapper`. Never claim theirs.
- `LICENSE.md` is this fork's copy and stays **verbatim** (637 lines: EA's, minus the two
  `SPDX-License-Identifier` lines). Never edit, trim, reorder or relicense it.
- **Do not rename upstream-authored data names**: `GeneralsMD/Code/Data/INI/*Reforged.ini`,
  `Reforged*.tga`/`.dds`, and the labels that reference them. They are functional keys upstream
  authored; renaming them is churn with breakage risk and no user-visible gain.
- No donation restriction applies to this base. (That came from TheSuperHackers' *data-patch* repo,
  which this tree does not use. EA's guidelines allow donations, with conditions.)

**The rename is done (2026-10-15).** The display name, the README title, the macOS app
(`Generals Vanguard.app`, `io.github.kuiserapper.GeneralsVanguard`), the Linux application id and the
two package files under `GeneralsMD/Code/Tools/linux-packages/`, the Linux package name and installed
paths (`generals-vanguard`), the window title and the Chroma metadata all say `Generals Vanguard`, with
the descriptive line `for Command & Conquer: Generals Zero Hour` in user-facing text. Deliberately
*not* renamed, and the reasons: the executable target stays `generals` (about a third of the tree's
scripts, tests and harnesses name it, and upstream's Windows binary is `generals.exe`); the version
numbers stay where they are; upstream's data names (`*Reforged.ini`, `Reforged*.tga`/`.dds`, the
`Reforged*.big`/`ReforgedArt` art folders) stay; and every EA identity string (the registry key, the
`Command and Conquer Generals Zero Hour Data` user folder, the `.csf` labels, the save and replay
paths) stays, because that is how the game finds its data. Renaming the executable or the fingerprint
identity is a separate, riskier decision.

## Layout

| Path | What |
|---|---|
| `Generals/`, `GeneralsMD/` | The engine source, in EA's original layout (`Code/GameEngine`, `Code/GameEngineDevice`, `Code/Libraries`, `Code/Tools`, `Code/Tests`, `Code/Data`) |
| `GeneralsMD/Code/Data/` | Upstream's own data and art (`*Reforged.ini`, `Reforged*.tga`, `.dds`) |
| `GeneralsMD/Code/Tests/` | Test sources and the shell harnesses (`run_replay_check.sh`, `run_generals_smoke.sh`, ...) |
| `GeneralsMD/Code/Tools/` | Build, packaging, fingerprint and verification scripts |
| `Tools/` | Standalone tools: `PathLab` (pathfinding), `Balance`, `Chroma`, `ScbTool`, `RandomMapLab` |
| `docs/porting/` | `windows-impact.md` is the verification ledger; `sanitizers.md` |
| `*.ps1` at the root | Windows harnesses: `ai-batch.ps1`, `replay-check.ps1`, `windows-ci.ps1`, `tree-check.ps1`, `dx11-check.ps1`, `shadow-check.ps1`, `lan-play.ps1` |

## Build and test

This checkout lives on **macOS arm64**; the play-tests happen on the owner's **Windows** machine.
Upstream supports both, so code can be built and much of it verified here.

```
brew install cmake                  # the only missing tool here
./build-macos.sh                    # macOS app; also RelWithDebInfo|Debug, "test", "clean"
./build-macos.sh Release test       # build and run ctest
build.bat                           # Windows: build;  build.bat Release test  also runs ctest
ctest --test-dir build-mac -C Release --output-on-failure
```

**Deep verification needs a Zero Hour install.** Tests that need data are guarded by
`if(ZH_GAME_DATA)` and are **Skipped (77), not Passed**, without it:

```
cmake -DZH_GAME_DATA="$HOME/zh-data" ...    # a folder holding zerohour/
#   $HOME/zh-data/zerohour/               the Zero Hour folder (INIZH.big, WindowZH.big, ...)
#   $HOME/zh-data/zerohour/ZH_Generals/   the base game's files
```

That unlocks `replay_check` (four 12000-frame matches; recording, playback and a second run must
agree on the same HEADLESS CRC), `locomotor_check`, `net_check`, `data_gone_check` and the rest.
Copying the folder from the owner's own Windows install is legitimate and is the highest-leverage
setup step for this project.

**Headless is a first-class mode** (`Common/GlobalData.h`):

- `-headless` — no window, no frame drawn, the logic tick unpaced, quits when the match ends. A
  23-minute skirmish plays out in 38 seconds, identically every run.
- `-maxframes <n>` bounds a match the AI cannot close out; `-randommap` makes the seed the map, so
  comparing two builds needs nothing installed; `-logPrefix` keeps each run's log separate;
  `-nodevice` runs with no graphics device at all.
- `ai-batch.ps1` drives batches of headless matches over seeds and prints the win rate per seat.
  **No AI change is claimed without a number behind it.** `ai-batch.sh` at the root is its POSIX twin
  (same flags, same table, `--parse-only` on a fixture needs no game data); the Windows script still
  needs PowerShell, `brew install powershell` on macOS.
- `ScenarioDrill` writes one `HEADLESS ARRIVE` line per arrive action: how many watched units got
  within the goal. That is the instrument for "units ordered somewhere they never reach".

Record what a change was verified with, where. `docs/porting/windows-impact.md` is the ledger for it
(build, ctest N/N, replay check, pixel diffs, and on which platform).

## Verify before saying done

```
git log --oneline -1                        # does the subject read type(scope): summary, under 72?
GeneralsMD/Code/Tools/license-headers.py --check
GeneralsMD/Code/Tools/fingerprint-manifest.sh --check      # if you touched files under GeneralsMD/Code
ctest --test-dir build-mac -C Release --output-on-failure  # note any test that reports Skipped
grep -rn "EA has not endorsed" README.md NOTICE.md         # must be present once we add it
diff <(git show upstream/main:LICENSE.md) LICENSE.md       # only the SPDX lines may differ
```

Gameplay changes still need a play-test on Windows. Static checks and headless matches catch a lot,
but the Alt-Tab class of bug only exists on Windows, and no test can tell you whether the game is
*fun* to play.

## Deliberate deviations from upstream

- `AGENTS.md` is committed here although upstream's `.gitignore` excludes it.
- Our version line starts at 0.1.0 rather than continuing upstream's 3.x.
- Branding (display name, credits, disclaimers) differs; keep the diff to those surfaces and leave
  upstream's data names and internal identifiers alone.

## Archive

The abandoned first attempt (forked from `asaprockky/generals-reforged` by mistake) is kept outside
this repository at `../generals-vanguard.old/`: a full git bundle of both commits under `.archive/`,
plus the same commits as patches in `.archive/patches/`. Nothing in it applies to this tree, but it
is the record of the naming and licensing review, including the text of the credits and disclaimers.
