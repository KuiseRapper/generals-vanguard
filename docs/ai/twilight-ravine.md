# Twilight Flame: the AI and the ravine

Findings from reading the engine (2026-10-15), for backlog item 3. Two symptoms, two real causes, and
neither is where it looks. The third candidate turned out to be dead code - see (b).

## Status (2026-10-15): implemented, not measured

Five commits on `ai/lane-reachability`. None of it has been played or measured; each needs a batch
before it can be called an improvement.

| commit | what it does |
|---|---|
| `b25e8a28` | an unreachable lane is kept out of the attack lane vote (`aiLeastDefendedLane` gains a mask) |
| `dda6c909` | a lane is judged by the waypoint at its **far** end - the near one sits beside our own base and is reachable whatever the map does, so asking about it excluded nothing |
| `9893a984` | Easy and Medium, which do not vote, take a reachable lane when the script's lane is out of reach |
| `cb733b0d` | the same check for the split wave's "second road" (`secondApproachLabel`) |
| `f7466a08` | `-strictgoal`, default **off**: an order to a goal the army cannot reach reports no path instead of a hand-out route to the rim |

Failure mode accepted in the far-end heuristic, from the agent that wrote it: the mask judges a whole
lane by one waypoint, so a wrong "unreachable" drops a good lane - the farthest *labelled* waypoint
need not be the lane's true terminus, and it is Euclidean farthest, not path-order farthest. It is
bounded, because the mask can only remove lanes from a vote and never add or reorder them, and with
none reachable the old behaviour stands.

## (a0) The root cause: the lane vote never asks whether the road can be walked

Before anything paths, the AI picks which lane to attack down. `AIPlayer::chooseApproachLabel`
(`AIPlayer.cpp:6821`) scores the map's `Center` / `Flank` / `Backdoor` waypoint paths **purely by
enemy firepower along them** (`knownFirepowerAlongPath`, `:6883`; `aiLeastDefendedLane`,
`AI.cpp:1676`). It never asks whether the AI's ground units can traverse the path at all.

So a lane whose waypoints exist but run into a ravine scores **zero** enemy firepower, and therefore
looks like the quietest road on the map. It wins the vote. Everything below then compounds that
choice. This is the most likely explanation of "the AI tries too much to get through that ravine",
and the fix belongs beside the choice, not in the pathfinder.

The vote itself also has no memory: a lane chosen that goes nowhere can be chosen again next wave
(see `docs/ai/skirmish-ai.md`, item E1/E2).

## (a) Units driven into the ravine and left there

The direct search refuses an unreachable goal. The AI's **fallback does not**, and it rewrites the
destination on the way out.

- `Pathfinder::findPath` returns NULL up front when the start and goal cells are in different zones
  for this locomotor set — `AIPathfind.cpp:8191-8193` via `clientSafeQuickDoesPathExist`, whose
  compare ends `return FALSE; // no path exists` at `AIPathfind.cpp:10064-10068`; the same test runs
  again inside the search at `AIPathfind.cpp:8397-8402`.
- `AIUpdateInterface::computePath` then falls back, `AIUpdate.cpp:2223-2228`:
  `findClosestPath(...)` with `m_retryPath = true`.
- `findClosestPath` **discards every region restriction** — `m_zoneManager.setAllPassable()` at
  `AIPathfind.cpp:10786-10797` — keeps the reachable cell nearest the goal, and **writes the new goal
  back into the caller's destination**, `AIPathfind.cpp:10953-10961`. A goal on the far plateau
  therefore yields a route to the near rim of the ravine. That is the reported symptom.
- It returns NULL only when there is nowhere on the whole map to stand, so
  `m_pathfindFoundNothing` is almost never set, and `AIInternalMoveToState::update` keeps going —
  `AIStates.cpp:1826-1846`.
- What burns the time is attack move: `AIAttackMoveToState::update` re-paths **5 times, 3 s apart**
  — `AIStates.cpp:4213-4228` with `ATTACK_RETRY_COUNT = 5` (`AIStateMachine.h:596-601`), set at
  `AIStates.cpp:3714`.
- Nothing anywhere remembers that a goal is unreachable: there is no unreachable-set or blacklist.
  `bumpNoPath` and `didPathfindFindNothing` count it; nothing acts on it.

The goal is never repaired either: `adjustDestination` does use the zone screen and so fails for a
far-plateau goal (`AIPathfind.cpp:6877`, `:6748-6756`), and the `snapClosestGoalPosition` that
follows only tests the 3x3 cells around the goal for passability, with no reachability test —
`AIStates.cpp:1693-1700`, `AIPathfind.cpp:6639-6700`.

For waypoint waves there is a second keep-trying loop: because a closest-path move ends in
`STATE_SUCCESS` at the rim, `AIFollowPathState::update` advances to the next waypoint
(`AIStates.cpp:3438-3446`), each one further across the ravine, while the unit stays put. Those
intermediate waypoints are ordered with `setAdjustsDestination(false)` (`AIStates.cpp:3397-3399`), so
nothing re-checks them. The waves are ordered along the map's own waypoint paths —
`AIPlayer::sendWave`, `AIPlayer.cpp:9363-9405`, using `<lane><enemyStart+1>` labels resolved from the
`.map`'s waypoint objects.

## (b) The pile at the AI's own plateau edge - and why it is not the "fist"

The first reading of this blamed `AIPlayer::doMass`, which stages a "fist" one base radius out from
the base centre straight at the enemy - on this map, the plateau edge facing the ravine. **That code
does not run.** `doMass` returns at its first gate, `getSkillProfile()->m_massUnit`
(`AIPlayer.cpp:7919`), because:

- `m_massUnit` is **false on all three rungs** of the compiled ladder (`s_defaultSkillLadder`,
  `AI.cpp:1245-1250`); and
- the shipped data does not override it - there is no `SkillLevel` block anywhere under
  `GeneralsMD/Code/Data/`, and no `MassUnit` in any of its INI files.

Upstream measured it and left it off deliberately (`AI.cpp:1192-1195`): *"massU on top shortened the
matches and lost more for what it killed (0.70 against 0.64), so it stays off until it earns its
place."*

So the pile is not the fist. It is the **deliberate massing before an attack** -
`m_massBeforeAttacking` is true on Brutal - which parks finished teams until a wave is worth enough
(`doWaves`, `holdTeamForWave`, `m_heldTeam[]`; `WAVE_POWER` 6000, or a 90 s cap). They idle at their
rally point, and where that is comes from the team prototypes inside the compiled
`SkirmishScripts.scb`, not from C++. That is a design and data question rather than a pathfinding
bug, and upstream has already tried holding finished teams for massing and reverted it - twenty
measured brutal-vs-brutal matches went from nine decided to none (`AIPlayer.cpp:4582-4594`). It is
left alone.

## What a mod cannot do about it

- **Terrain passability is map data, not INI.** Cliffs are a per-cell bit in the `.map`
  (`WorldHeightMapData::getCliffState`, `WorldHeightMapData.cpp:123-130`, read at `:444`), classified
  by `Pathfinder::classifyMapCell` (`AIPathfind.cpp:5959-5993`). A map's `map.ini` may not even hold a
  `Terrain`, `Bridge` or `Road` block: `isMapINIBlock`'s whitelist is `INI.cpp:335-351`. And
  `TerrainType` has no impassability field at all (`TerrainTypes.cpp:42-50`).
- **A per-map `map.ini` changes the map's CRC**, which the lobby compares — `MapUtil.cpp:106-111`,
  `calcCRC` at `:139-156`. Fine between two modded installs, a mismatch against a stock one, and
  replays key off it.
- **`AIData` in a `map.ini` is never undone.** `AI::newOverride` exists for exactly that and has
  **no caller** (`AI.cpp:464-486`), so the override mutates `TheAI->m_aiData` for the rest of the
  process and leaks into later matches. That contradicts `INI.cpp:326-333`. A separate bug.
- **`Tools/PathLab` cannot reproduce this.** It is a reimplementation from the engine's constants
  (`CELL = 10`, `COST_ORTHOGONAL = 10`, `COST_DIAGONAL = 14`), with maps from `make_map()` over
  `("choke", "open", "twogaps")`: no `.map` reader, no cliffs, no zones, no AI layer. It can argue the
  A* and nothing about this. It also needs `pygame`, which is not installed here.

## Ranked fixes

Already implemented on `ai/lane-reachability` (see Status): E2 in its far-end form, the Easy/Medium
fallback, the second road, and E behind `-strictgoal`. H is withdrawn - (b) is a false lead. What is
left is the measurement, and confirming the far-end heuristic on a real map.

| # | Change | Fixes | Cost / risk |
|---|---|---|---|
| **E2** | **A reachability test in the lane vote** — `chooseApproachLabel`, `AIPlayer.cpp:6821`, so a road the ground locomotor cannot walk stops winning the vote | **(a) at its root**, and every future map with the same shape | medium: a pathfinder query per lane per wave, must stay deterministic and cheap. Low risk, and the same code path as E1 |
| E | Guard the closest-path fallback so an unreachable destination is reported as no-path — `AIUpdate.cpp:2223-2228` (narrow, AI move only) or the zone screen at `AIPathfind.cpp:10719-10731` | (a) at the source | ~10 lines. `findClosestPath` is also what makes "move onto a blocked cell" work (`AIPathfind.h:859-863`), so the narrow AI-side variant is the one to take. Simulation-affecting |
| F | Do not burn 5 x 3 s on a provably unreachable attack-move goal — `AIStates.cpp:4213-4227` | the 15 s of grinding | very low; attack move only. Replay-affecting |
| H | Move the massing staging point off the enemy-facing edge — `AIPlayer.cpp:8061-8075` | (b) precisely | low lines, real regression risk: the offset keeps the fist clear of the factory doors |
| G | Skip an unreachable waypoint instead of advancing on a closest-path success — `AIStates.cpp:3438-3446` | waves walking waypoint after waypoint into the wall | ~15 lines, must be measured; a wrong failure test strands a wave early |
| I | Remember unreachable goals for a while on `AIUpdateInterface` | the "repeatedly", for all orders | largest; the class is `xfer()`'d, so save/replay compatibility must be handled |
| A/B | `map.ini` with `AIData`/`SkillLevel` `MassUnit = No` (optionally `UseInfluenceMapForAttackLane`, `EngageGate`) | (b), as data | zero engine risk, but changes the map CRC and leaks (above), and massing is a designed behaviour the owner may like |
| D | Re-author Twilight Flame's approach waypoints so each lane is drivable | (a) if the lanes are the cause | game data; cannot ship here, needs World Builder |

Not worth doing: re-enabling the massing gate in `checkReadyTeams` (documented in-tree as a
20-match regression, `AIPlayer.cpp:4582-4594`), or re-introducing the group-movement rework
(reverted on measurement, `ad2c878f`).

## How to settle it

Every engine option above is simulation-affecting: it changes the HEADLESS CRC and invalidates
replays, so each needs `replay_check` plus an `ai-batch` run. This repo's rule is that no AI change
is claimed without a number behind it, and none of this can be measured without a Zero Hour install
(`-DZH_GAME_DATA`).

The instrument is `ScenarioDrill`'s `arrive`, which reports what actually happened to units sent to a
point:

```
-headless -map "Twilight Flame" -seed <n> -scenario twilight -maxframes 24000 -logPrefix tl-a
# Run/Scenarios/twilight.txt:
#   180 attackmove 1 * <far-side x> <far-side y>
#   200 arrive     1 * <far-side x> <far-side y> 200
```

Read `HEADLESS ARRIVE`, `HEADLESS STRAGGLER`, `HEADLESS STALL` and `HEADLESS PATHFIND` out of the
log. A straggler with `path 1` on the near rim is case (a) exactly — it holds a route and it is the
wrong one; `waiting 0 path 0` there would instead mean no route was found at all. Stragglers sitting
idle on the AI's own plateau edge are case (b), and their coordinates against
`m_baseCenter + m_baseRadius * toward(enemy)` say which of (b)'s contributors it is.

Still unknown without a run: whether Twilight Flame's Centre/Flank/Backdoor waypoints for the
owner's seat pair actually lead into the ravine, and which lane `chooseApproachLabel` picks — it logs
`AI LANE frame %d player %d takes %s ...` at `AIPlayer.cpp:6853-6855`.
