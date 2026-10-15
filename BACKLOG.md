# Backlog

Small and ordered. Each item says what it is, why it matters, and the first step. Move things up as
they unblock, delete what stops mattering. Nothing here is a commitment.

Constraints that are not negotiable live in [AGENTS.md](AGENTS.md). The scope is the owner's: fix what
is broken, rework the AI, then build our own factions and content on top.

The three items at the top are bugs the owner hit while playing upstream's build, in the order he
raised them. They are the reason this fork exists.

---

## Now — the three reported bugs

### 1. Alt-Tab does not come back to the desktop
Fullscreen, the game window stays in front and the player cannot switch to another window.
**Windows-only** — nothing here can reproduce it, and no headless test covers it.

*Why it matters:* it is the first thing anyone hits, and it makes the game feel broken outside a match.
*First step:* reproduce on the Windows machine, then read the window and display handling in
`GeneralsMD/Code/GameEngineDevice` — the Win32 message loop, where fullscreen mode is taken and
released, `ClipCursor`, `WM_ACTIVATE` / `WM_ACTIVATEAPP` / `WM_KILLFOCUS`, and whether the window is
ever minimised. Also check whether upstream has since fixed it in a later commit than our fork point.

### 2. The AI repeats itself and never adapts to close a game out
The opening is good. By the late game it runs the same strategy over and over and does not react to
the fact that it is not winning.

*Why it matters:* it is what makes a long solo match boring, and the AI is the owner's main interest.
*First step:* **measure before changing anything.** Run `ai-batch.ps1` over a spread of seeds, record
win rate per seat and match length, and look for matches that end by hitting `-maxframes` — those are
literally the games the AI cannot close out. Then read the skirmish AI's decision loop
(`AISkirmishPlayer`) and find where a plan is chosen, and whether anything ever re-evaluates it.

*Analysis done (2026-10-15):* the decision loop is mapped, and the answer is that **nothing**
re-evaluates it — there is no plan state and no memory of an attack at all. The mechanism, the ranked
fixes (E1/E2 first) and the ideas upstream already measured worse are written up in
[docs/ai/skirmish-ai.md](docs/ai/skirmish-ai.md). Only the measurement is left, and it needs the game
data from item 5.

### 3. Twilight Flame: units pile into a dead-end ravine
On the stock Twilight Flame map the AI sends units into the ravine between the two plateaus, toward a
base it cannot reach that way, instead of taking the way around. It also stockpiles units at the edge
of its own plateau before the ravine.

*Why it matters:* a visible, repeatable defect on a stock map, and the owner gave a precise
description of it.
*First step:* reproduce it headlessly. `ai-batch.ps1`'s own examples use `-Map "Twilight Flame"`, and
`-maxframes` bounds the run. `ScenarioDrill`'s `HEADLESS ARRIVE` lines are the proof: units ordered to
a point that never get within it. `Tools/PathLab` tests the pathfinder in isolation, so the map data,
the pathfinder and the AI's target choice can be separated before patching any of them.

*Analysis done (2026-10-15):* three causes, in a chain, and the first is not in the pathfinder — the
lane vote scores roads by enemy firepower only, so an untraversable ravine looks like the quietest road
on the map and wins. Then the closest-path fallback hands out a route to the rim, and attack move
grinds at it for 5 x 3 s. See [docs/ai/twilight-ravine.md](docs/ai/twilight-ravine.md). Note that
`Tools/PathLab` cannot reproduce this — it is a reimplementation with no map reader, no cliffs and no
AI layer — so the measurement is the headless one.

### 4. An honest review of the AI's decision-making
The owner asked for an assessment rather than a change: is the AI's strategy and decision-making
actually good, and where is it weak?

*Why it matters:* it decides what items 2 and 3 should really be, and the owner explicitly does not
want drastic changes to an AI he already enjoys.
*First step:* write it from evidence — the match logs from item 2's baseline, the building list and
attack behaviour, and what the AI never does (re-evaluate, switch, expand, tech). Rank the fixes and
say which are not worth making.

*Partly done (2026-10-15):* [docs/ai/skirmish-ai.md](docs/ai/skirmish-ai.md) is the written review —
how it decides, why the late game repeats, the ranked fixes, and the list of changes upstream already
tried and measured worse. What is missing is the numbers behind the late-game claim.

---

## Next — the setup that makes verification possible

### 5. Build here, and get game data onto this Mac
`brew install cmake`, then `./build-macos.sh` and a ctest baseline. Then copy the Zero Hour folder
from the Windows machine to `~/zh-data/zerohour/` (with `ZH_Generals/` beside it inside) and configure
with `-DZH_GAME_DATA`.

*Why it matters:* **this is the enabler for items 2, 3 and 4.** Without game data every match-level
test reports Skipped (77), so nothing about the AI or pathing can be verified here and every change
would need a hand play-test.
*First step:* `brew install cmake` and get a clean ctest run, which also tells us how many tests skip.

### 6. A portable driver for headless AI batches
`ai-batch.ps1` is exactly the right instrument — same seeds, win rate, match length — but it is
PowerShell, there is no `pwsh` here, and CI cannot run it.
*Why it matters:* the win-rate argument is our only honest way to claim an AI change helped.
*First step:* read what it does (seed loop, `-maxframes`, `-logPrefix`, log parsing, win-rate table)
and reproduce that output in a portable script, without changing the numbers.

---

## Then — the licensing work already agreed

### 7. Credits, disclaimers, version and changelog
Add EA's required sentence (it is genuinely absent from this tree), the credits chain, the version line
at 0.1.0, and a separated "Vanguard changes" section at the top of the existing 2146-line
`CHANGELOG.md`. `LICENSE.md` and `NOTICE.md` stay intact and the header checker must keep passing.

*Why it matters:* it is a compliance gap today, and it was the owner's original ask.
*First step:* one commit that adds the disclaimer and credits to `README.md` and `NOTICE.md`; the deep
rename (Linux package IDs, macOS `.app`, executable names, fingerprint) comes later and deliberately,
because version identity is what LAN compatibility keys off.

---

## Later

### 8. Factions
Upstream has `feature/turkey-faction` and `feature/apocalypse-mode` branches as working references for
adding a faction to this engine.
*First step:* read those branches before designing anything of our own.

### 9. Balance
`Tools/Balance` (`duel.py`, `siege.py`, `unitmatrix.py`, `compare.py`) exists to argue balance with
numbers rather than opinions.
*First step:* run it on the current build to get a baseline before touching any value.

### 10. Total conversion
The owner's long-term goal: new setting, factions, art and story. Keep this fork's name separate from
whatever the content project is eventually called.

---

## Hygiene

### 11. Keep the verification ledger honest
`docs/porting/windows-impact.md` is where upstream records what a change was verified with — build,
ctest N/N, replay check, pixel diffs, and which platform — including what was *not* verified. Our
changes should be recorded the same way, in the same place.

### 12. Skim the sanitizer and check tooling
`GeneralsMD/Code/Tools/asan-check.sh`, `docs/porting/sanitizers.md`, `tree-check.ps1`, `net-check.sh`
and `replay-check.sh` already exist. Read them before writing new harnesses.
