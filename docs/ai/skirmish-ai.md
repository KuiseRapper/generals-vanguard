# The skirmish AI: how it decides, and why the late game repeats

Findings from reading the engine (2026-10-15), for backlog items 2 and 4. Read this before proposing
any AI change: most of the obvious ideas have already been tried upstream and **measured worse**, and
the list of those is at the bottom.

## First, whose AI this is

The *engine* is EA's. **The AI in this tree is not.** `AIPlayer.cpp` is 15,109 lines where EA's was
roughly 4,000; every behaviour below is upstream Zero Hour Reforged's work, and this fork has changed
**zero AI code**. Any proposal has to be checked against upstream's own measurement notes, because
upstream gates every AI change on a head-to-head win rate (see `AI.cpp:1190-1195`, `:1232-1236`).

`GeneralsMD/Code/Data/Scripts/SkirmishScripts.scb` (2.2 MB) is **the strategy**: which teams exist,
when they are built, which approach path they take, when they guard or hunt. The C++ executes around
it. Some of it is neither: `aiTeamAttacks` (`AIPlayer.cpp:8170`) exists so the C++ can ask the compiled
script what it intends.

## How it decides

`AIPlayer::update` (`AIPlayer.cpp:4909`) runs every logic frame as a fixed list of independent jobs,
each rate-limited by its own frame modulo: base building, ready teams, queued teams, team building,
upgrades and skills, then for skirmish only — scouting, retreats, expansion, capture, hijack, economy,
spending, power, superweapons, particle beams, enemies, `answerArmy`, `doMass`, `doWaves`, tactics,
transports, shuttles, helixes, ambulances (`:4928-4946`).

There is **no single "think" step** and no plan.

- **What to build:** a fixed plan seeded from the side's `SkirmishBuildList` (`AISkirmishPlayer::newMap`,
  `AISkirmishPlayer.cpp:1347`), plus additions past a cash hoard (`doEconomy`, `:6039`).
- **What army:** `selectTeamToBuild` (`:3109`) scores every buildable team —
  `m_productionPriority + counterCompositionWeight * 20 * aiCounterScore(...)` — and takes the best,
  ties by `GameLogicRandomValue`. The hoard raises how many copies of one team may exist
  (`aiHoardAllowedTeamInstances`, `AI.cpp:1525`, up to +3).
- **When to attack:** waves gather in `doWaves` (`:8515`); `updatePressure` (`:8439`) sets
  `m_pressure` from `aiWinChance(myPower, m_knownEnemyPower)` — the only "am I winning" input, with
  hysteresis. It changes volume (wave size, guard release), not intent.
- **Where:** `chooseApproachLabel` (`:6821`) votes among `Center`/`Flank`/`Backdoor` by enemy
  firepower along the road. See `docs/ai/twilight-ravine.md` — this vote is the ravine bug's root.
- **Difficulty:** two axes by design. `GameDifficulty` picks scripts; `AISkillLevel`
  (Easy/Medium/Brutal) is *how well it plays*, 33 fields in `AIDifficultyProfile`
  (`AI.h:186-237`), all decision switches. **No rung cheats** (`AI.cpp:1162-1165`).

## Why the late game repeats

**There is no plan-level state at all.** No variable holds "what I am doing", no plan identity, no
outcome counter. What persists is the skill level, a role, the base centre, a 180 s ledger of seen
enemy units, the influence map, the fixed build list, the held-team staging list, timers, and two
pressure scalars. Nothing else.

The sharpest evidence, and the answer to the complaint:

- **The code already computes whether an attack worked, and throws it away.** `logEngage`
  (`AIPlayer.cpp:2867`) and `onUnitLost` (`:2896`) derive power sent against the remembered army and
  classify "AI LOSS … straggling / grouped / last" — as `DEBUG_LOG` only. Neither is stored.
- **No failed-attack detection.** Nothing compares this wave with the last, or notices that N waves
  went down one road and the front did not move. `doRetreats` (`:11675`) is fight-level, not strategy.
- **No re-plan.** Nothing re-reads the build list; `doEconomy` only appends to it.
- **One ratchet pushes toward repetition.** `TeamPrototype::increaseAIPriorityForSuccess`
  (`Team.cpp:965`) raises `m_productionPriority`, which is the **base of the team score** — and it
  has no cap, decay or reset. Its only callers are the script actions `TEAM_INCREASE_PRIORITY` /
  `TEAM_DECREASE_PRIORITY` (`ScriptActions.cpp:2452`, `:2474`); no AI code calls it. Both names are
  present in `SkirmishScripts.scb`. If the shipped scripts use them, success compounds into
  repetition. **Confirming that is the highest-value open question below.**
- **Everything adaptive is one-shot or saturated.** `m_role` is rolled once and frozen by design
  (`AISkirmishPlayer.cpp:97-104`); the science skill set is chosen once (`:4756-4791`); the enemy is
  re-checked every 5 s but `acquireEnemy` returns early while it has units and a factory
  (`AISkirmishPlayer.cpp:602`); `m_firstPushSeconds`, `m_upgradeStartSeconds`, `m_maxProduction` and
  the defence allowance are all capped. After ~10–15 minutes nothing is a function of match time any
  more, and the inputs it reacts to are near-stationary. **A stationary greedy policy over stationary
  inputs produces the same attack forever.**

The only anti-repetition memory in the whole AI is four slots for where the last superweapons were
aimed (`AIPlayer.h:708`).

Upstream knows: `AI.cpp:1234-1235` records that "a Hard that cannot close out Medium is still open",
and `CHANGELOG.md:231` ends the pressure work with "The other eleven still ran to the stop".

## Ranked changes

| # | Change | Effect | Cost / risk |
|---|---|---|---|
| **E1** | Wave-outcome memory in `doWaves` (`:8515`): remember the last K waves (lane, power sent, frame, both armies' power). When one lane has been used K times with no improvement in `aiWinChance`, bar it and take `secondApproachLabel` (`:6857`) or the vote's runner-up | the AI stops walking the same road into the same guns | ~60-100 lines, all state already exists, deterministic. Low risk — worst case it alternates lanes; it cannot break production or economy |
| **E2** | Reachability test in the lane vote (`:6821`) | the ravine, and any map shaped like it | medium; same code path as E1, so ship together |
| E3 | Re-evaluate the enemy while it is still alive (`AISkirmishPlayer.cpp:597-605`) | multi-AI games | medium; `aiEnemyCost`'s anti-ganging term exists to balance this and the sign was wrong once |
| E4 | Feed `AIPRESSURE_DEFEND` into *what is built*, instead of the frozen `m_role` | defensive posture | medium; team selection is where every measured column lands |
| E5 | Cap or decay the `productionPriority` ratchet in the engine | de-compounds repetition | very small, but silently changes team selection for every AI |
| D2 | Set the per-team `teamProductionPrioritySuccessIncrease` to 0 in the script data | same as E5 without an engine change | needs the script toolchain; changes the EXE CRC. Only worth it if the ratchet is real |
| E6/E7 | Un-freeze `m_role`; re-plan the build list late | | **not worth it** — against upstream's stated design and the owner's "do not change what I enjoy" |

**Shipped 2026-10-23: E1 and E2, both.** E2 is the lane reachability mask (the ravine work, merged);
E1 is the wave-outcome memory behind the skirmish box "AI wave memory" (on by default, `-nowavememory`
to A/B).  E1's rule as shipped: one non-improving wave (less than three points of chance) bars that
lane for two minutes.  This paragraph stays for the record of the plan.

**Recommended first move: E1 and E2 together, opt-in per rung so they can be A/B'd**, because they are
the only changes that address "it never switches" without touching what already works. Everything here
is simulation-affecting: it changes the HEADLESS CRC and invalidates replays, so each needs
`replay_check` plus an `ai-batch` run. This repo's rule is that no AI change is claimed without a
number behind it.

## Already tried and measured worse — do not re-propose

- **`MassUnit`, the "fist"** (hoard one unit type the enemy cannot answer, then push): off on all three
  rungs, measured `0.70 against 0.64` — "stays off until it earns its place" (`AI.cpp:1194-1195`).
- **Aiming attacks at the influence map's most valuable cell** instead of a lane: "7-9 over 32 matches
  against itself, twice the losses for half the kills, because aiming at the money means walking past
  the army" (`AI.cpp:1176-1180`).
- **Retreat at ttk 0.5-0.85:** "turning retreat off entirely scored better than 0.85 did"
  (`AI.cpp:1171-1174`).
- **Holding a finished team in the ready queue to force massing:** "20 measured brutal-vs-brutal
  matches went from nine decided to none, with zero kills and a quarter of the spending"
  (`AI.cpp:4582-4586`).
- **One gun every 90 s unconditionally:** 241 guns over twelve matches, down to 60 kills a match from
  73 (`CHANGELOG.md:237`).
- **The group-movement rework:** reverted after "eighty-odd matches … never produced a win rate the
  change could be argued with" (`ad2c878f`).

`shouldHoldForMassing` (`:4469`) and `aiShouldMass` (`AI.cpp:1360`) are dead code kept for a future
attempt — do not mistake them for live behaviour.

## Data schema, briefly

- `AIData` (`Data\INI\Default\AIData.ini` then `Data\INI\AIData.ini`, both `INI_LOAD_OVERWRITE`, in the
  INI CRC) holds `SideInfo <side>`, `SkillLevel Easy|Medium|Brutal`, `SkirmishBuildList <side>`, plus
  the grouping and guard fields (`AI.cpp:178-248`). **There is no `AITrigger` block in this engine** —
  that is a SAGE (C&C3/RA3) concept; zero hits.
- `SkillLevel` fields include `MassBeforeAttacking`, `MassUnit`, `EngageGate`,
  `UseInfluenceMapForAttackLane`, `CounterCompositionWeight`, `MinAttackPower`, `FirstPushSeconds`,
  `CashHoardThreshold`, `MaxProduction`, and more (`AI.cpp:1872-1928`).
- **Teams are not INI.** They come from the map's `SidesList` dict, and for stock skirmish from
  `SkirmishScripts.scb`.
- A loose `Data\INI\AIData.ini` beats `INIZH.big` (loose files are tried first), and it is in the INI
  CRC, so it refuses a match against an unmodified build.

## What only a run can settle

1. **Does `SkirmishScripts.scb` actually call `TEAM_INCREASE_PRIORITY` / `TEAM_DECREASE_PRIORITY`, and
   with what values?** This decides whether the ratchet is the mechanism or a red herring, and whether
   E5/D2 are worth anything. Highest-value single unknown here.
2. Does the shipped distribution override `SkillLevel` in a data `AIData.ini`? The table above is the
   compiled default; data could set `MassUnit = Yes` for Brutal and make that the status quo.
3. How often is one lane reused late, and does the front move? Needs `AI LANE` / `AI WAVE` / `AI COUNTER`
   lines from real Hard matches on Twilight Flame — that is the baseline E1 must beat.
4. Whether any of it is more *fun*. No headless metric answers that; the owner's constraint is explicit.
