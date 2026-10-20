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
