# Russia — faction design

**Status: design only.** Nothing here is built, and nothing here is measured. Treat every number as a
starting hypothesis. One spike (the in-place vehicle conversion) has to prove itself before any of this
is promised.

## The identity

**Russia is the army that arrives after the bombardment.**

Soviet deep operation run by a 2003 professional: a fire plan laid on the objective before a vehicle
moves, an air-defence net thick enough that enemy aircraft become a liability, and a small, expensive,
heavily armoured fist that walks into whatever the guns left standing. It is not a horde. Its infantry
and its cheapest tanks exist to hold ground and be spent; its real strength is few-and-good and must
never be traded away.

> The sentence a player uses: **"Russia doesn't out-fight you — it makes the ground you're standing on
> unlivable, and then drives a wall of armour through what's left."**

It is also, usefully, **the faction that finds things** — Spetsnaz detection, spotting, and a
reconnaissance-fire power. That is a deliberate counterweight to this project's endgame problem: an
army whose whole identity is "get seen, then get shelled" is the natural answer to opponents who hide
and spread.

## Why this identity and not the arcade one

Games present two different Russias:

- **The arcade reading** (Red Alert 1/2/3, Company of Heroes 2): cheap mass, one wonder-weapon, tesla
  and camp.
- **The serious reading** (World in Conflict, Wargame, EndWar): **expensive, concentrated, artillery-led,
  air-denying.** In World in Conflict's Soviet campaign the guides hand you `2S7 Pion` batteries and
  smoke rather than troops. Wargame players' summary of the USSR is "an answer to everything, but you
  cannot trade points and you collapse if you lose a few high-value machines." EndWar describes Russia's
  kit as "the oldest of the three, but rugged and heavily armed — very Russian."

**We take the serious reading and keep one legible cheap floor** (conscripts, Mobilized tanks) for
recognisability. That choice is a judgement call, not something the sources settle — but it is the
reading that produces a faction with a *decision* in it rather than a damage type.

## Strengths and weaknesses

| Strengths | Weaknesses |
|---|---|
| The best and most varied indirect fire in the game — sustained area denial, not one-off strikes | Slowest army; cannot answer a raid on the far side of the map |
| The densest ground-based air denial; makes the opponent's air force a cost centre | Cannot trade: losing one T-90 costs more than three enemy tanks |
| Armour is efficient per unit — few models, high HP and damage, strong in set-piece fights | Blind: artillery needs spotters, and spotters cost money and die |
| Very hard base to raid — layered fires, SAMs, fortifiable approaches | No stealth, no early harassment, no fast pressure; the first ten minutes are dangerous |
| Strong scaling — cheap early vehicles can be *promoted* into the good ones | Committed: a push that stalls is a push that dies |
| | Power-hungry base, so the base is a large, obvious, fragile target |

## The mechanic: Cadre & Mobilization

Russia buys **the same vehicle class in two grades, from two buildings, at the same time.**

| | **Mobilization Plant** | **Tank Works** |
|---|---|---|
| Cost | cheap, low power | expensive, heavy power |
| Prerequisite | none | Fire Direction Center |
| Speed | fast | slow |
| Output | "Mobilized" pattern: ~55–65% cost, no veterancy, no upgrade slots | full pattern: full price, full upgrades, full veterancy |

**The hook: Guards Conversion.** An upgrade at the Regimental Depot converts a *surviving* Mobilized
vehicle **in place** into its Guards pattern — object swap, a veterancy step, upgrade slots unlocked —
for a per-unit fee. Cheap early junk does not become obsolete; it becomes your mid-game army, but only
if you kept it alive. The whole early game becomes one question: **how much of my army is expendable
this match?**

How that differs from the base game:

- **USA** — one quality track behind tech and power. You never choose cheap-or-good, you just wait.
- **China** — cheap from the start and it *stays* cheap. The horde is the plan; the cost is a power deficit.
- **GLA** — no power at all, salvage, free units from tunnels. Cheapness is a lifestyle, not a decision.
- **Russia** — cheapness is a *loan*, repaid by keeping the loan alive.

This is grounded in history: Soviet industry ran three near-identical tanks in parallel (T-64 high-end,
T-72 "mobilization", T-80 gas turbine). The mechanic is that accident turned into a player decision.

## Units (12)

Naming: real designations for vehicles (recognition is the point), role-words for infantry.

| Name | What it does | The decision it changes | Closest ZH starting point |
|---|---|---|---|
| **Motostrelki** | 5-man rifle squad; garrisons; **Igla** upgrade | cheap line, and the cheapest air denial | China Red Guard |
| **Sapery** | 3-man engineers: Russia's **only builder**, repair, mine-clear, can emplace a forward Trench Line | you build with infantry, so your economy walks | GLA Worker |
| **Spetsnaz** | 2-man recon: stealth detection, long-range AT, **designates** a target so artillery hits it harder | fires need eyes, and the eyes are fragile | USA Ranger |
| **BTR-90** | fast 8×8 APC, 5 passengers, 30mm that hurts infantry and can plink aircraft | speed without protection | USA Humvee |
| **BMP-3** | tracked IFV, 5 passengers, 100mm HE + ATGM, loses to tanks | the aggressive transport | China Troop Crawler |
| **T-72B (Mobilized)** | cheap MBT, slow, no upgrades; fine against light vehicles, dies to real tanks | numbers now vs quality later | China Battlemaster |
| **T-90** | main MBT: gun-launched ATGM for first-shot reach, ERA/Shtora; slow, poor against close infantry | the fist | China Overlord, scaled ~0.85 |
| **Tunguska** | tracked SPAAG: quad gun + SAM; shreds aircraft, useless against tanks | escorts armour, denies the sky locally | Gattling Tank turret + Avenger box |
| **2S19 Msta** | tracked SP howitzer; long-range indirect, must deploy, needs a spotter | the gun that opens the position | China Nuke Cannon silhouette, no nuke |
| **BM-30 Smerch** | heavy MLRS; saturation salvo, wide spread, long reload, very fragile | area denial, not precision | USA MLRS |
| **Mi-24 Hind** | armoured attack helicopter carrying 4 infantry; rockets + chin gun, no dogfight | a flying IFV | China Helix |
| **Su-25 Frogfoot** | armoured CAS jet: slow, tough, short range, long rearm | the only way to break a dug-in position *before* the guns are ready | China MiG |

Optional 13th: a **Command variant** upgrade on the BTR-90/BMP-3 giving a small radius buff, for a
concentration-of-force feel.

## General's powers

| Power | Effect | Feel |
|---|---|---|
| **Artillery Preparation** | three scheduled waves walking across the target over ~20 s — area denial, not one instant hit | cheap, ~90 s, available early |
| **Recon-Fire Complex** | reveals a large area **and** marks targets in it to take extra artillery damage for ~20 s | 1 point, ~4 min |
| **Aerosol Screen (Maskirovka)** | wide smoke granting area stealth to ground units until they fire | 1 point, ~3 min, reuses ZH's area-stealth mechanism |
| **Buratino Salvo** | TOS-1 thermobaric strike; short range, so you must be able to reach the target | 2 points, ~3 min |
| **VDV Airborne Assault** | drops Guards infantry plus a BMD behind the lines | 2 points, ~5 min |
| *superweapon* **Missile Brigade** | four-missile salvo over a wide area, not a single beam | building-gated, area denial rather than a match-ender |

## Buildings

1. **Command Bunker** — HQ, commander, radar.
2. **Thermal Power Plant** — cheap, dirty, high output; cooks off when destroyed, so a greedy power base is a liability.
3. **Regimental Depot** — infantry, the Igla upgrade, and Guards Conversion.
4. **Mobilization Plant** — cheap vehicles, no tech.
5. **Tank Works** — the real vehicles; needs the Fire Direction Center and serious power.
6. **Fire Direction Center** — tech; unlocks artillery upgrades, spotting accuracy, the powers, and the Missile Brigade.
7. **Regimental Air Base** — Su-25 and Mi-24 production, rearm, **and the SAM umbrella**. One building is both Russia's air power and its air denial, which is the cleanest statement of the air doctrine.
8. **Missile Brigade** — superweapon.

Defences, not counted above: **Trench Line** (cheap, forward, infantry-only, built by Sapery), the
**Tunguska** as a mobile hardpoint, and an **S-300 site** — very long range, cannot hit ground, big
minimum-range dead zone, so it denies the sky over your base and nothing else.

## Air

The answer to "no air force" is not more aircraft: it is **aircraft with a job ground units cannot do**,
under a SAM umbrella.

- **Su-25 Frogfoot** — slow, armoured, short-ranged CAS. The only reliable way to break a position before the artillery is in place; it trades speed for surviving one pass.
- **Mi-24 Hind** — a flying BMP that puts infantry on top of a position without the slow ground approach.
- **VDV drop** — where the strategic-lift fantasy lives, as a power rather than a transport fleet.

**Deliberately absent:** an air-superiority fighter, stealth aircraft, and a strategic bomber. Enemy air
is answered by **Tunguska, the S-300 and Igla-armed infantry** — Russia does not contest the sky, it
*denies* it from the ground, which makes the opponent's air force expensive rather than absent.

**Why air does not take over:** both airframes are slow, short-ranged and long to rearm, so sorties are
events; both lose to dedicated AA, so they exploit suppressed defences rather than bypass them; and air
cannot hold ground, so every gain must be occupied by the slow army.

## What to avoid

- **Human wave as identity** — it is the arcade reading, GLA already owns swarm here, and it contradicts what the serious games do.
- **Cartoon-Soviet** — tesla, lightning, Iron Curtain, Apocalypse tanks, airships.
- **Nuclear everything** — China owns nukes. **Chemical/biological** — GLA owns WMD.
- **Stealth units** — maskirovka as a *power* is fine; a stealth tank is GLA's mechanic in a different hat.
- **Comedy accents** — Zero Hour plays its factions straight.
- **The stat-swap reskin** — the failure mode this whole document exists to avoid. Test each unit with *what decision does this change?*, never *what number is different?*
- **"Slow, so give it more HP"** — the compensation for slowness must be **reach** (artillery range, SAM range, ATGM first-shot range), not toughness. A slow army with more HP is just a slower version of a fast army.
- **A second resource, or fuel/ammo micro** — not native to the engine, and it hides bad design behind bookkeeping.
- **Any claim about balance.** Nothing is measured.

## Contrast with the EU / coalition faction

| Axis | **Russia** | **EU / coalition** |
|---|---|---|
| Tempo | slow, prepared, fires first | fast, reactive, decides first |
| Signature arm | artillery and ground-based air denial | air power, precision, reconnaissance |
| Air | battlefield aviation under a SAM umbrella; no air superiority | owns the sky: superiority plus strike packages |
| Armour | few, heavy, efficient per unit, echeloned | lighter, more numerous, modular, wheeled |
| Infantry | cheap conscript floor plus a small elite | small, professional, expensive, high-utility |
| Economy | two-track procurement, lumpy | steady, upgrade-driven, no bad units and no cheap ones |
| Weakness | blind, immobile, cannot trade | fragile, air- and tech-dependent, brittle when blinded |
| Sentence | "the ground you're standing on is already dead" | "we saw you set up, and we hit the guns before they fired" |

They answer each other cleanly: Russia punishes an EU player who lets a fire plan mature; the EU
punishes a Russian player who is still setting up. Same logic as USA/China/GLA, expressed through
tempo rather than damage types.

## Implementation plan

**Data only** (no engine work): the `PlayerTemplate` (a new side, `m_side`, `m_sideIconImage`), command
sets, all units and weapons, the power list, the aura/command variant, and the area-stealth power
(reusing the mechanism behind GLA's GPS Scrambler).

**Needs a spike:**
- the **in-place Mobilized → Guards conversion** (object swap plus veterancy plus upgrade slots) — the
  mechanic stands or falls on this;
- the **"cannot repair above 50%"** distinction (not native; consider simply "no repair button" instead);
- **Spetsnaz designation** (a targetable damage bonus for artillery) — check whether the engine's
  weapon bonuses can express it before inventing a module;
- the **Thermal Power Plant death explosion** (a death-weapon field if the template has one).

**Art reuse:** every unit above starts from an existing model (see the table). Step one is re-use with a
recolour; step two is a silhouette pass — turret, barrel, stance, antenna — because shared silhouettes
are what make a faction read as a reskin.

**AI support (the hidden cost):** the AI must be able to *play* it — build lists in `AIData`, and teams
in the compiled `SkirmishScripts.scb`, which the in-tree `Tools/ScbTool` can edit. Russia also fits the
planned per-match **AI doctrines** better than any other faction: an "artillery" doctrine is basically
this faction's playbook.

**Staging:**
1. **Skeleton** — template, command sets, six units on reused art, powers off or simplified, playable in skirmish. This is where the real cost estimate comes from.
2. **Silhouettes** — one art pass over the roster so it stops looking borrowed.
3. **Doctrine** — artillery guidance, spotting and the two-track decision for the AI.
4. **Balance** — only after it can be played and measured.

**The side spike, before promising any of it:** a fourth *side* touches the skirmish UI layout, side
icons, colours, and any three-side assumptions in menus and score screens. `PlayerTemplate` carries
`m_sideIconImage`, so an icon is data — but the menu layout is `.wnd` art and has to be checked.

**How it will be measured:** the standing recipe — `./ai-batch.sh --data <dir> --runs 20 --maxframes
200000 --mapcells 96`, with `--extra "-side 0 FactionRussia -side 1 FactionAmerica"` to pin the matchup,
and the sides swapped to cancel the start-position bias that pinned matches showed.

## Evidence notes

The design leans on how Russia is played in *World in Conflict*, *Wargame: Red Dragon* and *EndWar*
(expensive, concentrated, artillery-led, air-denying), and deliberately not on *Red Alert* or *Company
of Heroes 2* (cheap horde). Real-doctrine anchors: the T-64/T-72/T-80 parallel-tank lineage, and Soviet
deep-battle terminology. Two caveats worth keeping: the Wargame material is community discussion about a
deck-builder rather than developer intent, and *Act of War* has no official playable Russian faction — its
Consortium is a PMC, retconned to Russia by a mod — so it is evidence about depiction, not design.
