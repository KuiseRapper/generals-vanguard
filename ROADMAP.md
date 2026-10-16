# Generals Vanguard roadmap

Ideas for where this fork could go, roughly in the order I would try them. Each entry says what it
fixes, what it costs, and how it would be checked. **Nothing here is a commitment** - it is a menu.
Ordered work lives in [BACKLOG.md](BACKLOG.md); this file explains the thinking behind the options.

Everything below assumes the two rules this project already follows: no AI claim without a headless
batch behind it (`ai-batch.sh`), and no change to `LICENSE.md`, upstream's data names or EA's own
game strings.

---

## 1. The endgame hunt (the stealth-general problem)

**The complaint.** Against a reduced GLA Stealth opponent, the game stops being a fight and becomes a
search. You are the last player alive, the AI has a handful of camouflaged units, tunnels or workers
scattered somewhere, and you sweep the map to find them. It is tedious, and it is the single most
annoying thing reported so far.

**It has two separate causes**, which is why there is no single fix:

1. The AI has no *reason* to show itself. Its surviving teams sit in the hold-for-wave logic
   (`doWaves`, `holdTeamForWave`, `WAVE_POWER`) and its units idle where they stand. Nothing tells it
   that hiding is now pointless.
2. The player has no *information*. Nothing says how much is left or roughly where.

**Options.**

| # | Idea | What it fixes | Cost | Check |
|---|---|---|---|---|
| 1 | **Last-stand aggression.** When an AI has no production buildings left (or is the last opponent and below a strength floor), stop holding: release every team at once, ignore the wave threshold, and attack the nearest enemy structure. | Cause 1, and turns the hunt into a final battle - the fun version. | One rule in the team-release path plus a difficulty knob. The mechanism is the one the lane work already uses. | Headless: teams released, waves sent, match length. Feel: play-test. |
| 2 | **Endgame reveal.** Once one human player is left, AI structures are revealed permanently and AI units periodically (a sweep, not permanent). Guarantees the hunt is finite. | Cause 2, generically - it also covers a China AI with one hidden building. | Needs a spike: find the per-player visibility path and whether objects can be revealed, not just map cells. Script actions already reveal map areas, so the machinery exists. | Headless: reveal events fire only in the remnant state. |
| 3 | **"Enemy forces remaining" readout.** When enemy strength drops below a threshold, show units and buildings left - the scoreboard already tracks built/lost/killed per player. | Cause 2, cheaply: you know *whether* to keep hunting. | Small UI change; it leaks information, so gate it to the remnant state or make it an option. | Visual. |
| 4 | **Restrict generals for the AI.** The skirmish menu builds your and the AI's choices from `PlayerTemplate`s, so a "fair-play pool" for the AI is a data/UI change. | Nothing, really. | Moderate. | - |

**What I would do: 1 + 2, with 3 as a cheap bonus.**

Why not 4 first: it does not fix hiding by *other* armies (any faction can leave one building in a
corner), it removes a faction's identity, and it makes the AI worse at the game. Keep it as an
optional toggle if you want it for your own games - and note that in a skirmish you already choose
the AI's general by hand, so if the annoyance only appears when you or a random pick hands it GLA
Stealth, the smallest fix is a filter on the *random* pool, not a ban.

**What not to do:** make the AI stop using stealth in normal play. Camouflage and tunnels are that
faction's identity, and this fork's selling point is an AI that does not cheat. Restrict the
behaviour to the remnant state, where it is a mercy rather than a strategy.

---

## 2. More fun

| Idea | Why it is worth it | Cost |
|---|---|---|
| **Wave memory (E1)** - the AI remembers how its last few waves went and changes lane, composition or target instead of repeating itself. | The "late game it does the same thing forever" complaint. Designed already in [docs/ai/skirmish-ai.md](docs/ai/skirmish-ai.md); never written. The biggest single fun win available. | Moderate. Gate it, then measure with `ai-batch.sh`. |
| **A post-game stats screen** built from telemetry the engine already collects (`searches`, `expansions`, `nopath`, `blocked` unit-frames, kills, spent). | Nobody else's C&C shows this. It is nearly free - the numbers are already written to every headless log. | Small. |
| **Difficulty you can read.** The rungs differ across 33 knobs; players cannot see how. | "Hard counters what it scouts and masses before attacking, and cheats at nothing" is a selling point you currently have to read the source to discover. | Small: menu or loading-screen text. |
| **Opening variety.** A few distinct build orders per faction, chosen per match, so game one does not look like game five. | Cheap variety without new systems. | Small-to-moderate, data plus AI. |
| **The endgame hunt** (section 1). | Removes the worst experience reported. | See above. |

## 2b. More performance

Measured facts first, because these decide the order: a 60,000-frame 1v1 on Twilight Flame costs
**~5-6 million pathfinder cell expansions per match**, and the AI's own thinking is the top CPU
consumer. The overlay's upscaled art is the top memory consumer: `ReforgedTextures.big` is **1.07 GB**
(5,539 files) and `ReforgedTerrain.big` **227 MB**.

| Idea | Effect | Cost |
|---|---|---|
| **A "classic art" option** that skips the upscaled overlay art. | Up to ~1.3 GB less texture data: faster loads, far less VRAM, better on modest hardware and integrated GPUs. | Small - packaging and INI, no engine change. The single best performance-per-effort item. |
| **Pathfinding: cut the re-path storms.** Attack-move retries 5 times, 3 s apart, per unit; Hard re-decides every 1.5 s. | Our `-strictgoal` work already cut expansions ~11% by refusing orders that cannot succeed. More is available: path reuse, and not re-planning a route that has not changed. | Moderate; measured with the existing telemetry. |
| **Persist the BIG archive index.** 22 archives, ~4 GB, re-indexed on every launch. | Faster startup. | Moderate. |
| **Ship a precompiled shader cache** (`dx11shaders.cache`) instead of compiling on first run. | Removes first-run hitches. | Small. |
| **Job system tuning** - 13 worker threads; check that pathfinding actually parallelises and that the count suits the machine. | Free FPS on multicore. | Small, but measure - it is easy to make this worse. |

## 2c. Better (quality and hygiene)

- **A launcher.** The README currently says "build it yourself". A small launcher is the difference
  between a repository and something a friend can install. Upstream's exists but installs *their*
  build; ours would be ours.
- **The four known failing tests** (`widechar_check`, `install_guard_check`, and the two POSIX path
  tests that need a case-sensitive filesystem) plus the remaining lint. Not glamorous; it is the
  floor that keeps the green signal meaningful.
- **Windows play-test items still unverified**: the Alt-Tab focus fix and the D3D11 device path.
- **QoL pass**: control-group and rally persistence, build-queue behaviour, unit stances, match
  speed, windowed/borderless and resolution options.
- **A map validator** (we already have `Tools/PathLab`): flag any map whose attack lanes run into
  impassable terrain. This is the ravine bug as a *check*, applicable to every custom map, and the
  logic is already written - the far-end reachability test in the lane work.

---

## 3. New factions

**What a faction actually is here.** A `PlayerTemplate` in INI (side and general), a build list
(`CommandSet`), unit and building definitions, `Science` and special powers, strings, UI art - and
for skirmish, teams in the compiled `SkirmishScripts.scb`, which the in-tree `Tools/ScbTool` can
edit.

Three facts that shape the plan:

1. **Our INI defines no `PlayerTemplate`s at all** - they come from the game's data. The fork's
   overlay is searched *before* the install, so adding one is a data change, not an engine change.
2. **The skirmish menu builds its choices from the player templates** (it includes
   `PlayerTemplate.h`), so a new template shows up as a new army or general on its own.
3. **The expensive part is art**, not code: models, textures, animations, voices and UI. The second
   expensive part is the AI actually playing it well.

**A caution to check before promising a fourth *side*:** `MAX_PLAYER_COUNT` is 16 (players, not
sides) so there is no obvious hard cap, but the skirmish UI layout, flag/logo art and any
three-side assumptions in menus and score screens need a spike. A new *general inside an existing
side* sidesteps all of that - which is why it is stage 1.

### Stage 1 - a general (days to weeks, no new side)

Take an existing side and add a general to it: **"Vanguard" as a USA general.** Existing art, a
distinct build list, one or two units reusing existing models with new stats and weapons, one
signature power.

This exercises the whole pathway - template, command set, science tree, strings, AI build lists - at
a fraction of the cost, gives the mod its own identity, and is testable headless. It is also the
honest way to find out how much work stage 2 really is before committing to it.

### Stage 2 - one full army (months)

New art and new AI teams. Concepts, in the order I would pick them:

1. **Coalition / Task Force** - a European or UN-flavoured army built on reconnaissance and
   precision: drones, shared vision between units, long-range fire, defensive. Its identity is also
   a *counter to the hiding problem*: an army that finds things.
2. **Salvage / PMC** - cheap conscripts and captured hardware, a salvage economy, ambush tactics.
   Asymmetric like GLA without copying it: no tunnels, instead a mobile repair-and-reclaim loop that
   rewards fighting over wrecks.
3. **Prototype-tech USA** - railguns, directed energy, stealth drones: stage 1 grown into a full
   army, and the most natural fit for the name.

### Stage 3 - the second faction

Only after the first one ships, is balanced, and the AI plays it properly.

**What I need from you to start stage 1:** which fantasy or identity you want, and whether the new
units may reuse existing models (fast) or need new art (slow). Everything else I can stage.

---

## 4. What I would do first

1. Finish the ravine work: the play-test decides it, then merge it or drop it. It is already written
   and its mechanism is confirmed; only its outcome is unproven.
2. **The endgame hunt** (section 1) - the worst experience reported, and cheap.
3. **Wave memory** - the AI variety win, already designed.
4. **The map validator** - turns a whole class of bug into a check, using logic that already exists.
5. **The "classic art" option** - best performance per hour of work.
6. The four failing tests and a launcher - hygiene and shippability.
7. **Stage 1 faction** - the first real content, and the rehearsal for stage 2.

Two things worth remembering while reading this list: every AI change gets *measured* before it is
believed, and nothing here is worth doing if the answer is already decided by a play-test you can
run in ten minutes.
