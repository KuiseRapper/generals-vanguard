# Why an AI expansion never gets built: the build timer

**Status: diagnosis only, nothing changed.** Four attempts to fix it are recorded below so the next
person does not re-run them. The defect is a **timing** defect, not a placement defect: the AI picks a
legal spot and then does not attempt to build it for tens of thousands of frames, by which time the
spot is no longer legal and the request is dropped.

Found while building the Russia faction: Russia is the first faction with no captured-derrick income
to paper over it, and its supply centre never completed, so its income stayed 0.

## The chain (fixed seed 0, 96 cells, 60,000-frame cap, China slot 0 vs USA)

| frame | what |
|---|---|
| 14,725 | `AI EXPAND player 2 builds 'ChinaSupplyCenter' at (186,125) by dock 1, 8500 in the bank` — the spot passes the picker's test, including the enemy check, at pick time |
| 45,862 | first dozer attempt: `BUILD 'ChinaSupplyCenter': enemy overlap at (186,125), giving up the spot` |
| 45,863 | the AI asks again at the same spot |
| 45,982 | refused again, given up |

**31,137 frames (8.6 minutes of game time) pass between the pick and the first attempt.** In that time
an enemy moved onto the spot. The dozer then refuses, the entry is dropped (`giveUpOnSpot`), and
nothing re-picks: the expansion is abandoned for the match.

America's `AmericaSupplyCenter` in the same match is picked at frame 0 at an uncontested dock and goes
`AI QUEUE 1809` -> `CREATE 1809` -> `AI BUILT 2738` -> two Chinooks, because nothing moved onto it in
the intervening 1,809 frames.

The wait comes from `AIPlayer::processBaseBuilding`, which builds **one structure per timer slot**
(`m_readyToBuildStructure` / `m_structureTimer`) and walks the build list in order. A priority entry
added by the expansion sits behind the rest of the base, so its spot ages while the AI builds barracks,
bunkers and defences.

## What each attempt proved, and why it failed

| attempt | what it proved | why it failed |
|---|---|---|
| Instrumentation only (`createSpawn`, the six named `buildStructureWithDozer` returns) | the supply centre is refused for **enemy overlap**, at the placement test, twice | this is what located the defect |
| Move the failed spot into the existing ground flood search | the flood is the wrong tool: it returns "unfinished" and sets `m_buildDelay = 1` to resume next frame | a spot that stays contested never finishes, `m_readyToBuildStructure` never advances and the rest of the base starves — measured: China **10 buildings -> 4**, earned 11,600 -> 6,800, peak 53 -> 31. Reverted |
| Bounded ring, single frame (8 directions, radii 2–10 cells), no flood, no delay | a bounded retry is safe: China stayed exactly at baseline | **no legal candidate in forty** around (186,125) at frame 45,862 — the site had no usable neighbour at that moment. The site was fine when picked; the world had moved |
| Make the picker use the dozer's exact flags (`CLEAR_PATH \| TERRAIN_RESTRICTIONS \| NO_OBJECT_OVERLAP \| NO_ENEMY_OBJECT_OVERLAP`), one helper instead of five copies | the pick at 14,725 **was** legal under those flags | it does not help: legality at pick time says nothing about legality 31k frames later. Reverted |
| Collapse the five placement tests into one helper | cleanup worth having when the real fix lands | not a fix on its own; reverted with the above |

## The two candidate fixes

**(a) Re-pick the spot at build time instead of dropping the entry.** When `buildStructureWithDozer`
finds the spot illegal, ask the picker (`buildBySupplies`/`buildAISupplyCenter` logic, which can now
test with the dozer's flags) for a fresh spot near the same dock; only `giveUpOnSpot` when the picker
also finds none. *This is the one I would take.* It is local to the dozer path, it uses machinery that
already exists, and it fixes the real damage: the request is not thrown away because the world moved.
*What could go wrong:* re-picking must be rate-limited (once per attempt, not per frame) or a contested
dock becomes the cost sink the flood attempt measured; and the re-pick must not wander — it should stay
within the dock's `SUPPLY_CENTER_CLOSE_DIST`, or a "supply centre" will be built at the far side of the
map.

**(b) Let expansion requests bypass the one-per-timer queue.** A priority build added by
`buildAISupplyCenter` could be attempted on the next build tick rather than behind the whole list, so
the gap between pick and build is small. *What could go wrong:* this changes the base-building rhythm
for **every** faction and every building, and the one-per-timer rule exists to spread placement cost
across frames (one `isLocationLegalToBuild` ring can cost 10ms). It needs its own performance
measurement, and it is a bigger behavioural change than (a).

Either way this is an **AI-expansion behaviour change for every faction**, not a Russia patch.

## Acceptance gate for either fix

1. **China's supply centre completes** — `AI BUILT 'ChinaSupplyCenter'` in the fixed-seed log, with no
   `giving up the spot` for it.
2. **Russia's income** — `earns > 0` on seed 0, 96 cells, Russia slot 0 vs USA, and a gatherer in the
   log.
3. **No regression** — China back to 10 buildings, peak ~53, a win; then 10+ seeds, both orderings, on
   the standing recipe, against the pre-fix peak army, matches finished and pathfinder telemetry.

## Hypothesis, not a finding

Any AI expansion can sit in this queue for tens of thousands of frames, for any faction, on any map.
America's centre did not lose the race here, but nothing guarantees that: a 30k-frame gap between pick
and build is a general exposure. It may explain other behaviour seen this session — late expansions,
supply centres that "never appear", income gaps that look like AI passivity — but none of those has
been measured against this defect yet, and this document is the instrument to do it with.
