# Faction balance: a first baseline (2026-10-16)

The first measurement of faction strength in AI-vs-AI headless matches. **Small, provisional, and
honest about what it cannot say.** No game data was changed to produce it.

## The instrument

`./ai-batch.sh --data <dir> --runs N --players P --maxframes 200000 --mapcells "96 128 160"`, every
seat on the hard rung (`-aidiff brutal` - the code name for the UI's Hard), with factions pinned by
`--extra "-side 0 FactionAmerica ..."` and teams by `-team <slot> <n>`. A match costs roughly 5-15
seconds of wall time, and about **60% of them decide** at this cap; the rest hit it.

## Pinned 1v1 - the cleanest experiment (10 seeds per arm, both orderings)

| pairing | decided | winner |
|---|---|---|
| GLA vs USA | 7/10 | USA 7-0 |
| USA vs GLA | 6/10 | USA 1, GLA 5 |
| GLA vs China | 6/10 | GLA 5, China 1 |
| China vs GLA | 6/10 | China 2, GLA 4 |
| USA vs China | 5/10 | USA 3, China 2 |
| China vs USA | 6/10 | China 3, USA 3 |

**Aggregate: USA 8-5 over GLA, GLA 9-3 over China, USA 6-5 over China.** 36 of 60 matches decided.

**The caveat that outranks all of it:** in the GLA/USA pair the **slot-1 player won 12 of 13** decided
matches. Start position dominates, so the faction edges above are provisional. The orderings are
consistent across arms, which is the only reason to take them seriously at all.

## Pinned 2v2 (6 seeds per arm, teams 1+2 vs 3+4)

| teams | decided | winner |
|---|---|---|
| USA+GLA vs China+China | 2/6 | 1-1 |
| China+China vs USA+GLA | 4/6 | China+China 3, USA+GLA 1 |
| USA+China vs GLA+GLA | 3/6 | USA+China 1, GLA+GLA 2 |
| GLA+GLA vs USA+China | 3/6 | GLA+GLA 2, USA+China 1 |

**Single-faction teams beat mixed teams 4-2 in both pairings** - a hint that coordination between two
different factions is worse than two of the same, or simply noise at 2-4 decided matches per arm.

## Mirror controls - how big the positional bias is

| mirror | decided | winner |
|---|---|---|
| USA vs USA (4 seats) | **0/6** | none - every match hit the cap |
| China vs China (4 seats) | 4/6 | team 1 won 3, team 2 won 1 |

Two useful facts come out of this. A four-way USA mirror **does not resolve at all**, so a USA-heavy
meta may be a stalemate rather than a winner. And with identical factions, team 1 won 3 of 4 - so
positional bias is real and must be cancelled by swapping, always.

## Random factions (no `-side`, so each seat is drawn from the seed)

1v1: 17 of 20 decided. USA beat GLA 5-2, GLA beat China 3-2, China beat USA 2-1 - the same ordering as
the pinned arms, on 3-7 matches per pairing, so it is corroboration rather than evidence.
2v2: 17 of 20 decided, spread across every composition; no composition won more than 3.

## What this does *not* say

- **Nothing about human-versus-AI.** The owner's "GLA wins every time" comes from playing it by hand.
  GLA's cheap spread and harassment may be far stronger against a human than against another AI - which
  is a different, and perfectly real, balance problem from the one measured here.
- **Nothing that justifies a data change yet.** No value was touched.
- **Nothing at 40 seeds.** 36 decided 1v1 matches is a hint, not a result.

## Next

1. **Raise the cap to 400,000** and run 40 seeds both directions per pairing - about 60% resolve at
   200k, and the unresolved ones are exactly the stalemates worth understanding.
2. **Model the owner's complaint directly:** three seats, two GLA on team 1 and one USA on team 2, so
   the 1v2 that started this can be measured.
3. Only then touch GLA's numbers - and prefer structure cost or HP, which the owner suggested, over any
   AI-side handicap.

---

# Round 2 (2026-10-17) - 70 mixed matches, then 50 strict 2v2

The owner asked for volume, variety and teams that never share a faction. Every match got **its own
seed** (so its own map) and a map size cycling 96/128/160, `--maxframes 200000`, the hard rung, and a
deterministically drawn combination that never repeats. Generals are included, not just base factions -
all twelve playable templates.

## 1v1 with generals (20 matches, 17 decided)

By base faction: **China 8-5, USA 5-7, GLA 4-5.**

By general (samples are 1-5 each, so read the ordering as a hint):

| general | W-L | | general | W-L |
|---|---|---|---|---|
| **AmericaSuperWeaponGeneral** | **5-3** | | ChinaInfantryGeneral | 2-2 |
| **ChinaTankGeneral** | **4-0** | | GLAStealthGeneral | 2-2 |
| China (base) | 2-1 | | GLA (base) | 0-2 |
| GLADemolitionGeneral | 2-1 | | ChinaNukeGeneral | 0-2 |
| AmericaAirForceGeneral | 0-2 | | America / AmericaLaser | 0-1 / 0-1 |

## 2v2 - two batches, two rules

**Batch A (50 matches, 43 decided): different generals within a team, same faction allowed.**
Winning seats: **GLA 36, China 29, USA 21** of 86 (even would be ~29).

**Batch B (50 matches, 35 decided): different factions within a team** - the rule the owner actually
asked for. Winning seats: **GLA 28, China 22, USA 20** of 70 (even ~23).

| team faction pair | W-L | win% |
|---|---|---|
| **China + GLA** | 15-8 | **65%** |
| GLA + USA | 13-13 | 50% |
| **China + USA** | 7-14 | **33%** |

Teams with 3+ decisions, by template: GLA (base) 10-6, AmericaAirForce 9-10, ChinaNuke 7-6,
AmericaLaser 6-7, GLADemolition 7-6, ChinaTank 6-6, GLAStealth 5-6, China (base) 7-4,
GLAToxin 6-3, **AmericaSuperWeapon 2-8**, ChinaInfantry 2-6, America (base) 3-2.

## What this says

1. **USA is the weak faction in AI hands, in both formats** - 5-7 in 1v1 and 41 of 156 winning 2v2 seats
   (26%, against 33% expected). This is the opposite of the owner's impression of GLA, and it is the
   most consistent signal in the data.
2. **GLA is over-represented in 2v2 wins** - 36/86 (42%) and 28/70 (40%) across two independent
   batches, both above the 33% share. Having GLA on your team helps; **China+GLA is the best pairing at
   65%**, **China+USA the worst at 33%.**
3. **No single ordering holds across formats.** GLA was average in 1v1 (4-5) and strong in 2v2; China
   was the best 1v1 faction and mid in 2v2. Faction strength is not one number.
4. **Per-general swings are bigger than per-faction ones, and they do not carry across formats.**
   AmericaSuperWeapon went 5-3 in 1v1 and 2-8 in strict 2v2; ChinaTank went 4-0 in 1v1 and 6-6 in 2v2;
   GLA base went 0-2 in 1v1 and 10-6 in 2v2. That is either a real interaction or noise at these
   sample sizes - it is not settled.
5. **Resolution is good enough to build on**: 85% of 1v1 decided, 86% and 70% of the two 2v2 batches.

## Limits

- 17 decided 1v1 matches and 78 decided 2v2 matches in total. No confidence intervals were computed;
  a 40%-vs-33% seat share over 70 seats is suggestive, not conclusive.
- Start-position bias is real and was not cancelled here (it was ~12/13 in the earlier GLA/USA pair).
  Round 3 should swap team positions on every pairing.
- Nothing here is a human-vs-AI measurement, which is where "GLA feels oppressive" comes from.

## Round 3, if it is worth it

1. Swap positions per pairing (run each match twice, teams mirrored) and pool.
2. 100+ matches per format so per-faction shares get intervals.
3. Model the owner's own scenario: three seats, GLA+GLA against a single USA.
4. Then, and only then, decide whether GLA's structure cost or HP needs touching - with USA's weakness
   as the more likely finding to act on.
