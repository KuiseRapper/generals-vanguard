# Generals Vanguard roadmap

Ideas for where this fork could go, roughly in the order I would try them. Each entry says what it
fixes, what it costs, and how it would be checked. **Nothing here is a commitment** - it is a menu.
Ordered work lives in [BACKLOG.md](BACKLOG.md); this file explains the thinking.

Two rules the project already follows: no AI claim without a headless batch behind it
(`ai-batch.sh`), and no change to `LICENSE.md`, upstream's data names or EA's own game strings.

Performance is parked at the owner's request (2026-10-16) - it is item 7 in the backlog, and section
3b here is only the reasoning.

---

## 1. Closing out a game against a spreading AI

**What actually happens.** Solo against two GLA AI. GLA builds cheap structures all over the map, so
with two of them the map fills up, and the game cannot be closed by sweeping - you cannot patrol an
entire map while probing for the last few buildings. It is not really *stealth*: it is **structure
spam**, made worse by the fact that there are two of them.

There is no "build anywhere" flag in the engine - GLA spreads because its structures are cheap, its
workers are free, and the AI placement logic happily fills space. So this is a decision problem, not a
flag to turn off.

**Three separate causes:**

1. The AI keeps **expanding and rebuilding while it is losing**. Nothing tells it to stop.
2. Its structures are **scattered**, so finding the last ones is a search rather than a fight.
3. The player has **no idea how much is left**, so there is no moment where the game feels closeable.

**Fixes, in the order I would do them:**

| # | Idea | Why | Cost |
|---|---|---|---|
| 1 | **Stop the proliferation.** When an AI is beaten - no production buildings left, or below a strength floor - it stops placing new structures, stops rebuilding destroyed ones, and its workers stop roaming. | Cause 1, and it is the root: a losing AI should consolidate, not colonise. | One rule in the AI's build path (the economy-build list and `findBuildLocation`), plus a difficulty knob. |
| 2 | **Reveal what remains.** Once one human is left, remaining AI structures are revealed permanently. | Cause 2, directly. Structures are what you must kill; a hidden *unit* is a fight, a hidden *building* is a chore. Units can stay hidden. | Needs a spike on the per-player visibility path; script actions already reveal map areas, so the machinery exists. |
| 3 | **Count it.** "Enemy structures: N" once the enemy is reduced. | Cause 3, very cheaply. The scoreboard already tracks built/lost per player. | Small UI change. |
| 4 | *Optional*: **surrender rule** - no production and fewer than N units for M seconds, and the AI surrenders. | Ends the game without the sweep. | Small, but it can feel abrupt; take it only if 1+2 are not enough. |
| 5 | *Not recommended as the fix*: **disable generals for the AI**. | It does not fix a China AI doing the same thing, it removes a faction's identity, and it makes the AI worse. | Keep as an optional toggle only. |

**What not to do:** nerf stealth or GLA's cheap buildings in *normal* play. Spreading and hiding are
those factions' identities; the problem is only that a beaten AI keeps doing it forever.

## 2. GLA is the strongest AI

**Report.** GLA wins every AI game; "random" seemed to pick GLA four times in a row. Two separate
questions, both checkable:

- **Balance.** GLA's cheap structures, free workers and strong early units may simply be better in an
  AI's hands. The levers are data: build times, costs, structure HP, worker build rate, income. Use
  `Tools/Balance` first - numbers, not opinions - and record every change.
- **The random pick.** The skirmish menu seeds its generator from the match seed
  (`InitRandom(TheSkirmishGameInfo->getSeed())`), so the army choice is seeded rather than biased.
  Four GLA in a row has roughly a 4% chance for *some* faction to do that, so it is plausible
  coincidence - and a two-minute check of the selection code settles it.

**The measurement problem, honestly.** Our own 162-match run had **zero decided matches** at 60,000
frames on Twilight Flame, so win rate currently says nothing about anything. Before any balance claim:
**find a configuration where matches actually decide** (a smaller map, a higher cap, or fewer
players), then run faction against faction.

## 3. More fun

| Idea | Why it is fun | Cost |
|---|---|---|
| **Wave memory** - the AI remembers how its last waves went and changes lane, composition or target. | The "late game it repeats itself" complaint, and the biggest single AI win available. Already designed. | Moderate; gate and measure. |
| **Per-match AI doctrines** - aggressive, defensive, economic, air-heavy, chosen at match start and visible to the player. | Makes opponents recognisable, and turns "the AI" into "that AI again". | Moderate: a doctrine field plus build-list selection. |
| **Random maps in skirmish** - `Tools/RandomMapLab` already generates maps, and `-randommap` already works headless. | Infinite replayability for near-zero art cost. | Small-to-moderate: expose it in the menu. |
| **Skirmish rule presets** - starting cash, no superweapons, fast build, sudden death, unit caps. | Big variety from a small UI; every preset changes how the game feels. | Small. |
| **Post-game stats screen** from telemetry we already write to every log (kills, spent, pathfinder searches, blocked unit-frames). | Nobody else's C&C shows this, and it is nearly free. | Small. |
| **A General's Challenge ladder** - a run of AI opponents with escalating rosters and their own doctrines. | Campaign-lite replay value from data plus existing systems. | Moderate. |
| **Difficulty you can read** - the rung's character stated in the menu ("counters what it scouts, masses before attacking, cheats at nothing"). | Your best selling point is currently only visible in the source. | Small. |
| **QoL pass** - control-group persistence, stances, rally points, match speed, windowed and resolution options. | Small annoyances compound over hundreds of matches. | Small each. |
| **The endgame fixes** (section 1). | Removes the worst experience reported so far. | See above. |

## 3b. Performance (parked - backlog item 7)

For when it comes back: the upscaled overlay art is the dominant memory cost
(`ReforgedTextures.big` 1.07 GB, `ReforgedTerrain.big` 227 MB), so a "classic art" option is the best
win per hour. Then the re-path storms (a 60,000-frame match costs ~5-6 million pathfinder cell
expansions), persisting the BIG archive index, shipping a precompiled shader cache, and job-system
tuning.

## 4. New content: three generals, then one new faction

### The three generals (start with USA)

One new general per existing side, each a build list plus a couple of units and one signature power.
Cheap, because they reuse art, and they exercise the whole pathway.

1. **USA - "Vanguard"** (first): prototype technology - railgun, directed energy, drones. The mod's
   own identity, and the most natural fit for the name.
2. **China**: an attrition general - conscripts, heavy artillery, slow and relentless.
3. **GLA**: an insurgent general - ambush, sabotage, salvage; the opposite pole to Vanguard.

### The new faction

Owner's candidates: **Russia, Germany, Japan**. My recommendation: **Russia first**, then Germany as
part of a broader European or coalition army, and Japan last (most art-heavy and the biggest risk of
drifting into science fiction).

Why Russia first:

- The strongest identity of the three, and the clearest contrast with China. China is already the
  cheap-swarm side; Russia can be the opposite - heavy, slow, durable, artillery-led, no air force.
- The most natural model reuse: tank hulls, artillery, SAMs and heavy infantry can be adapted from
  China and USA shapes with new silhouettes, which matches the "reuse and change them" plan.
- It gives the AI a genuinely different playstyle to learn, which is worth as much as the player
  content.

Suggested mechanical identities - differentiation must be **mechanical**, not just stats, or the
faction feels like a reskin:

| | Identity | Signature mechanics |
|---|---|---|
| Russia | Ground war, no air force | Slow high-HP armour; long-range artillery with a creeping-barrage power; cheap large conscript squads; strong static AA; slower but cheaper economy. |
| Germany / EU | Precision and recovery | Fast mobile armour; recon drones that spot (an anti-hiding faction by design); repair and reclaim of wrecks; strong defensive turrets. |
| Japan | Networks and drones | Unmanned units, ECM that disables enemy units briefly, strong stealth; expensive, fragile, high-tech. |

**What a faction actually costs.** Art is the real bill: models, textures, animations, voices, UI
palette and side icon. Technically it is data - `PlayerTemplate` (with `m_side`, `m_sideIconImage`), a
`CommandSet`, units and powers, strings - plus teams in the compiled `SkirmishScripts.scb` so the AI
can play it, which the in-tree `Tools/ScbTool` can edit.

**Read this first:** upstream has a `feature/turkey-faction` branch - a working reference for adding a
whole side to this engine. Study it before designing anything of our own.

**Spike before promising it:** a fourth *side* touches the skirmish UI layout, side icons, colours and
any three-side assumptions in menus and score screens. A new *general* avoids all of that, which is
why the generals come first.

## 5. What I would do first

1. **Close out the ravine work**: the play-test decides, then merge or drop it.
2. **The endgame fixes** (section 1): stop the proliferation, reveal what remains, count it.
3. **Find a decisive measurement setup** so balance and AI claims can be settled on outcome.
4. **GLA balance** (section 2), with `Tools/Balance` and a faction-vs-faction batch.
5. **Wave memory** - the AI variety win.
6. **The USA general** - the first real content, and the rehearsal for the new faction.
7. **Fun batch** (section 3): doctrines, random maps, rule presets, post-game stats.
8. **Russia** (section 4), after the generals have proven the pipeline.
