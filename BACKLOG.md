# Backlog

Small and ordered. Each item says what it is, why it matters, and the first step. Move things up as
they unblock, delete what stops mattering. Nothing here is a commitment. The thinking behind the
bigger items is in [ROADMAP.md](ROADMAP.md); the non-negotiable constraints are in
[AGENTS.md](AGENTS.md).

The scope is the owner's: fix what is broken, rework the AI, then build our own factions and content
on top.

## Done

- **Alt-Tab** (`43a7ee7c`). Still needs the owner's Windows play-test; nothing here reproduces it.
- **Game data on this Mac.** The full Steam install is at
  `~/Games/Command & Conquer Generals - Zero Hour`, with `~/zh-data/zerohour` pointing at it. Headless
  matches run against it, no shims.
- **`ai-batch.sh`.** The POSIX twin of `ai-batch.ps1`, with `--parse-only` and `--extra`. This is what
  makes an AI claim measurable on this machine.
- **Credits, notices, rename and version.** 0.2.0 tagged; EA's required sentence is in `README.md`
  and `NOTICE.md`; the product rename is complete; `CHANGELOG.md` carries a separated Vanguard section.
- **The AI review and the ravine review**, in `docs/ai/skirmish-ai.md` and
  `docs/ai/twilight-ravine.md`.

- **AI expansion retry and the ravine merge (2026-10-23).** `buildBySupplies` asks the next dock
  when one has nowhere legal to stand (up to three), and the five `ai/lane-reachability` commits -
  unreachable lanes out of the vote, Easy/Medium going around them, the split wave off a bad road,
  and `findClosestPath` no longer aiming at a ravine rim - are on `develop` after the owner's
  Twilight Flame play-test. Details in the removed items' changelog entries.
## Now

### Campaign chooser (Default / ShockWave / RotR)
**Why it is not built yet: the data is missing, not the code.** The three story campaigns are not in
the tree or in the combined package: `!!VanguardCombined.big` carries one `Data\INI\Campaign.ini`
(ours, the retail USA/GLA/China missions) and `!Shw_challenge.big` carries ShockWave's Generals
Challenge, but **no RotR campaign missions exist anywhere in the package**. RotR's campaign is the
"separate pack" the combined plan already lists as open, and campaign maps are big files shipped with
the mods, not something the fork can author.
`CampaignManager::init()` also loads one fixed path (`Data\INI\Campaign.ini`) once at startup, so a
real chooser needs (a) a re-entrant init that can swap the campaign set, (b) the three data sets
present, (c) a shell screen that lists them, per faction.
*First step:* build the RotR campaign pack first (its `Campaign.ini` plus maps into the combined
mod), then a `-campaign <set>` switch that picks the INI before `CampaignManager::init`, then the
menu over it.
*Open question:* if "per faction" means choosing which mod's version of a faction the lobby offers
(Default USA / ShockWave USA / RotR USA) rather than a story campaign, that is `-modfactions` plus a
chooser UI instead, and it does not need the campaign pack. Say which and it is the first step.

### Mod manager in the launcher
**macOS launcher: done and published (0.5.11).** Mods sheet with **ShockWave 1.201** and **Rise of the
Reds 1.8** as checkboxes: "Update mods" installs a checked mod from the matching archive in
`~/Downloads` (or opens its ModDB page when there is none) and deletes an unchecked one, with a
progress bar; custom `.zip`/`.big` install and uninstall are there too. Mods live under
`~/Library/Application Support/GeneralsVanguard/Mods/<id>/`; Play passes `-mod "<abs path>/"`, which
the engine already supports. The launcher also replaces itself from `launcher-mac.zip`/`.dmg` instead
of asking the player to drag the app out of a disk image, and its manifest URL is
`releases/latest/download/launcher-mac.json` (the old pin to v0.4.0 was why 0.4.x/0.5.3 never saw an
update).

**Windows launcher: still to do.** Same UI in `Tools/VanguardLauncher/MainForm.cs`, mods under
`%USERPROFILE%\Documents\Generals Vanguard\Mods`, `GameLocator.Start` taking `-mod`.

### Performance: a frame cap and a Vulkan option (separate pass)
**VSync on by default is done** (0.5.12: `m_vsync = TRUE`, the debug line's ~118 fps on a 61 Hz panel
is what it fixes). What remains is deliberately its own pass, because the Vulkan backend is a large
piece of work: an explicit `-fpslimit <hz>` cap, then the SDL3 GPU backend selectable (D3D11 stays
the default on Windows; Vulkan is the experimental option the macOS Metal path proves the shape of),
measured for frame time and GPU power on the owner's Windows box.

### 2. The AI cannot close a game out (endgame spread)
**The rule is in (2026-10-23); needs the owner's play-test.** Once an AI has had a factory, barracks
or airfield and has none left, and the chance is not in its favour, it stops putting up anything new:
no power, no defenses, no factories, no expansions, no tunnels, no rebuild (`updateConsolidation`,
gated in `processBaseBuilding`, `doEconomy`, `doPower`, `doSuperweapons` and `doExpansion`). Its army
keeps fighting, and what still stands is a finite list to clear. The reveal toggle and the "enemy
structures: N" counter from [ROADMAP.md](ROADMAP.md) section 1 are still open.

### 3. Wave memory: the AI never adapts
**In (2026-10-23), behind the skirmish box "AI wave memory", on by default** (the design in
[docs/ai/skirmish-ai.md](docs/ai/skirmish-ai.md), E1). A wave leaves with the AI's win chance at the
time; when the next wave goes down the same lane, or ninety seconds pass, the chance is looked at
again. A lane that gained nothing twice is barred for three minutes and the vote takes the next
quietest road. `-nowavememory` is the A/B control for batches. The measured numbers are in the
changelog entry for 0.5.15.

## Next

### 5. More fun
Wave memory; per-match AI doctrines; random maps in skirmish (`Tools/RandomMapLab` is in-tree);
skirmish rule presets; a post-game stats screen from telemetry we already collect; a General's
Challenge ladder. See [ROADMAP.md](ROADMAP.md) section 3.

### 7. Performance (parked by the owner)
A "classic art" option (the upscaled overlay art is 1.07 GB of textures), the re-path storms, a
persisted BIG index, a shipped shader cache, job-system tuning. See [ROADMAP.md](ROADMAP.md) section
3b.

## Later

### 4. GLA is the strongest AI
*The owner will do this later.* Owner report. Needs numbers before any change. See
[ROADMAP.md](ROADMAP.md) section 2. `Tools/Balance` for unit-level numbers; then a decisive headless
setup for faction-vs-faction win rates. Also check the seeded army pick, which is probably
coincidence.

### 8. Balance tooling
`Tools/Balance` (`duel.py`, `siege.py`, `unitmatrix.py`, `compare.py`) exists to argue balance with
numbers. Run it for a baseline before touching any value.

### 9. Total conversion
The owner's long-term goal: new setting, factions, art and story. Keep the fork's name separate from
whatever the content project is eventually called.

### 10. Hygiene
The four known failing tests (`widechar_check`, `install_guard_check`, and the two POSIX path tests
that need a case-sensitive filesystem). A launcher - the README currently says "build it yourself".
Keep `docs/porting/windows-impact.md` honest about what was and was not verified.

## Found while measuring

### 11. A misspelled `-side` hangs the run
`parseSide` (CommandLine.cpp:2112) refuses a name that is not a player template, and
`startSkirmish` (GameEngine.cpp:690) then returns - but the process keeps spinning an idle loop for
ever instead of exiting. A batch waits out its entire timeout, and the same silent hang would hit
anyone who typo'd a faction name. Every other unattended guard in this tree exits with a reason on
stderr.
*First step:* exit with a status and a stderr line, the way the base-game guards at
`Win32BIGFileSystem.cpp:100` and `GameEngine.cpp:1024` already do.

### 12. The 0.5.18 faction load plates are never drawn
`ReforgedLoadScreens.ini` maps `SAFactionLogoPage_US`, `_China`, `_GLA`, `_EU` and
`ReforgedLoadRussia` to five 800x600 Vanguard plates, and `PlayerTemplate.ini` names them as
`LoadScreenImage` - but `PlayerTemplate::getLoadScreen()` has no live caller. The only two sites that
would use it, `LoadScreen.cpp:1359` and `:1648`, are EA's own comments from the initial commit, and
the screens that do load a match draw `MDSkirmishLoadB` (skirmish and LAN) or `MissionLoad_*` /
`LoadPageHuge` (campaign and save). So no screen in game shows the new plates.
*First step:* decide which screen they belong on. The multiplayer load layout puts its title bar
over the plate's wordmark and its player table over the faction name, so restoring the old line
there is not enough; the single-player layout is the one with room, and a skirmish-faction load page
would need a layout of its own. Then photograph a match's load before claiming it - no harness can
pause the load screen today.
