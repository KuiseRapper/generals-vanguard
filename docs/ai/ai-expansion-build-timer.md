# Why an AI expansion never gets built: the build timer

**Status: diagnosis only, nothing changed.** Five attempts to fix it are recorded below so the next
person does not re-run them.

Found while building the Russia faction: Russia is the first faction with no captured-derrick income
to paper over it, and its supply centre never completed, so its income stayed 0.

## The starting point for the next attempt

**The AI's supply-centre picker validates a spot, finds it invalid, and queues the request anyway - at
the dock's own cell, which is water or otherwise unsuitable - so the request is doomed at pick time.
And when one dock yields no legal spot, the AI never tries a different dock: it re-asks the same dock
until the match ends.**

Everything below is how that was established. The 31,137-frame wait between pick and build is real and
worth fixing in its own right, but it is **secondary**: even with no wait at all, the queued position
was not a legal build spot.

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
bunkers and defences. **The wait is real but secondary** (see the starting point above): the queued
position was already illegal when it was queued, so fixing the wait alone would not build the centre.

## What each attempt proved, and why it failed

| attempt | what it proved | why it failed |
|---|---|---|
| Instrumentation only (`createSpawn`, the six named `buildStructureWithDozer` returns) | the supply centre is refused for **enemy overlap**, at the placement test, twice | this is what located the defect |
| Move the failed spot into the existing ground flood search | the flood is the wrong tool: it returns "unfinished" and sets `m_buildDelay = 1` to resume next frame | a spot that stays contested never finishes, `m_readyToBuildStructure` never advances and the rest of the base starves — measured: China **10 buildings -> 4**, earned 11,600 -> 6,800, peak 53 -> 31. Reverted |
| Bounded ring, single frame (8 directions, radii 2–10 cells), no flood, no delay | a bounded retry is safe: China stayed exactly at baseline | **no legal candidate in forty** around (186,125) at frame 45,862 — the site had no usable neighbour at that moment. The site was fine when picked; the world had moved |
| Make the picker use the dozer's exact flags (`CLEAR_PATH \| TERRAIN_RESTRICTIONS \| NO_OBJECT_OVERLAP \| NO_ENEMY_OBJECT_OVERLAP`), one helper instead of five copies | the pick at 14,725 **was** legal under those flags | it does not help: legality at pick time says nothing about legality 31k frames later. Reverted |
| Collapse the five placement tests into one helper | cleanup worth having when the real fix lands | not a fix on its own; reverted with the above |
| Re-pick the spot at build time (fix (a), the previous recommendation): bounded, rate-limited, nearest-first rings out to `SUPPLY_CENTER_CLOSE_DIST` = 20 cells around the requested spot *and* around the dock, with the dozer's flags | **no legal candidate in the whole 20-cell square** at the moment of the attempt - the spot had been invalid since it was picked | the request was queued at the dock's own cell and fails the picker's own test; a retry cannot rescue a spot that never existed. Reverted |
| Companion: refuse to queue an invalid expansion spot | China then **never asks for that centre at all** - no request, no build | avoids the wasted queue entry but produces no building, and loses scale: China **10 buildings -> 7**, peak 53 -> 49, earned 11,600 -> 9,800 (still a win). Reverted |

## Where the fix now belongs: dock selection

The dozer, the re-pick and the queue are all downstream of a choice that was wrong when it was made.
The fix is in **dock selection** - `buildBySupplies` / `findBestSupplyWarehouse` / `buildAISupplyCenter`
- and it is two rules:

1. **Never queue a spot that failed the picker's own test.** If no position beside the chosen dock is
   legal, the dock is not usable for a supply centre this time.
2. **Then choose another dock** instead of re-asking the same one. The need (the AI wants a supply
   centre) must survive even when one dock cannot take it; today the need is either queued at an
   impossible position or dropped, and neither is right.

*What could go wrong:* dock selection is shared by every faction's expansion, and the search must stay
bounded (the map has tens of docks); a dock that fails today may be usable later (an enemy walks away),
so a "skip" should not be permanent. Measure the dock-scan cost with the pathfinder telemetry.

**Secondary, still worth taking:** expansion requests bypass the one-per-timer queue, so the gap
between pick and build cannot grow to 31k frames. It changes base-building rhythm for every faction and
must be measured on its own (one `isLocationLegalToBuild` ring can cost 10ms).

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
