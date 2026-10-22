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

## Now

### 0. AI expansion: choose another dock when the first one cannot take a centre
The **illegal spot** half is fixed (2026-10-21), and it was three defects, not one: `buildBySupplies`
offset a supply centre by a fixed three pathfind cells, inside the engine's `SupplyBuildBorder`;
`isLocationSafe` counted an inert Spy Satellite ping as an enemy; and it refused any centre whose site
was within 300 units of the enemy, which on a 96-cell map is the player's own base. China's centre now
completes on 3 of 4 measured seeds instead of never, and Russia's economy depends on it (see
[russia-build-plan.md](docs/factions/russia-build-plan.md)). The numbers and the seven earlier attempts
are in [ai-expansion-build-timer.md](docs/ai/ai-expansion-build-timer.md).
What remains: when a dock yields no legal spot the AI re-asks the same dock and never chooses another
(seed 0 asked at 14,725 after an earlier refusal).
*First step:* dock selection on failure, with the gate in the doc (China's centre completes on every
seed, then no regression).

### 1. Close out the ravine work
Five commits on `ai/lane-reachability`, cherry-picked to `ai/measure`. The mechanism is confirmed
(orders into unreachable goals: 16 -> 4,207 refusals over 20 matched seeds); the **outcome is
unproven** because 0 of 162 matches were decided.
*First step:* the owner's Twilight Flame play-test, then merge or drop.

*Then:* find a configuration where matches actually decide (smaller map, higher cap), so the next
change can be judged on outcome rather than mechanism.

### 2. The AI cannot close a game out (endgame spread)
Solo versus two GLA AI: cheap structures across the whole map, so the game cannot be closed by
sweeping. The owner's top report now. See [ROADMAP.md](ROADMAP.md) section 1.
*First step:* stop the proliferation - an AI with no production, or below a strength floor, stops
placing and rebuilding - and reveal remaining AI structures to the last human.

### 3. Wave memory: the AI never adapts
Designed, never written ([docs/ai/skirmish-ai.md](docs/ai/skirmish-ai.md), E1). The biggest AI variety
win available.
*First step:* write it behind a knob, then measure with `ai-batch.sh --extra`.

### 4. GLA is the strongest AI
Owner report. Needs numbers before any change. See [ROADMAP.md](ROADMAP.md) section 2.
*First step:* `Tools/Balance` for unit-level numbers; then a decisive headless setup for
faction-vs-faction win rates. Also check the seeded army pick, which is probably coincidence.

## Next

### 5. More fun
Wave memory; per-match AI doctrines; random maps in skirmish (`Tools/RandomMapLab` is in-tree);
skirmish rule presets; a post-game stats screen from telemetry we already collect; a General's
Challenge ladder. See [ROADMAP.md](ROADMAP.md) section 3.

### 6. Russia: the skeleton and its first systems play; finish the faction
**Owner's ruling (2026-10-17): go big immediately.** The skeleton is **done** (2026-10-21): a fourth
side with twelve buildings and twelve units on reused art, its own command sets, AI build list and
skirmish scripts, plus its systems - **Igla** (Motostrelki gain an anti-air missile), **ERA/Shtora**
(+400 health to every T-90), **Infantry Training**, **Aircraft Armour**, **Artillery Training**, the
**Command Doctrine**, three base defences (S-300, Trench Line, Strongpoint), the **Missile Brigade**
(superweapon), a small **air arm** (MiG-25, Mi-24) and **seven general's powers** (Artillery
Preparation, Buratino Salvo, Scorched Earth, Radio-Electronic Strike, Long-Range Aviation, Guards
Assault, Field Repair), all with AI scripts. What it took, the general AI defects it found and the measurements are in
[russia-build-plan.md](docs/factions/russia-build-plan.md).
*First step now:* **the art pass and the remaining design powers** - seven powers work and the AI uses
them, but Recon-Fire, Aerosol Screen and VDV are still design and the borrowed models are placeholders.
**Balance** is much closer: the random 24-match matrices went from 1v1 18-3 (85.7%) and 2v2 14-7
(66.7%) to 1v1 12-7 (63.2%) and 2v2 13-10 (56.5%), with the 1v1 losses spread across the field.  The
remaining balance work is a larger sample and the per-faction table -
the artillery tier (Russia needs its own howitzer weapon), the Mobilized-to-Guards conversion, and the
silhouette art pass. The windowed bar, portrait and Eva fallbacks still need the owner's play-test.

The **three generals** (USA "Vanguard" first) move to *Later*: the owner would rather build the real
faction directly than rehearse on a general.

### 7. Performance (parked by the owner)
A "classic art" option (the upscaled overlay art is 1.07 GB of textures), the re-path storms, a
persisted BIG index, a shipped shader cache, job-system tuning. See [ROADMAP.md](ROADMAP.md) section
3b.

## Later

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
