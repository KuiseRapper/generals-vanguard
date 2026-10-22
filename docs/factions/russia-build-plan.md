# Russia — build plan

Working plan for the skeleton stage. The design is in [russia.md](russia.md); this file says which
files to add, what each object is, and what is data versus art. Nothing here is measured yet.

## The three mechanisms this rests on

**Overlay INIs.** The engine loads the fork's data files by name, one `ini.load(...)` call each, in
`GameEngine::init` (`GeneralsMD/Code/GameEngine/Source/Common/GameEngine.cpp`, around the other
`*Reforged.ini` loads): `MULTIFILE` adds a name or edits one after EA's file, `OVERWRITE` replaces a
list whole. A new file is a new call there and a new entry in `BuildFingerprint.manifest`
(`fingerprint-manifest.sh`).

**Names and descriptions.** Unit and building names are `OBJECT:<TemplateName>` labels in the fork's
string overlay, `GeneralsMD/Code/Data/Patch.str` (already loaded and tracked); buttons use
`CONTROLBAR:Construct<X>` / `CONTROLBAR:ToolTip<X>`, the side uses `SIDE:Russia`, the player template
`INI:FactionRussia`, tooltips `TOOLTIP:...`. Nothing goes into EA's `generals.csf`. German and Turkish
translations stay untranslated until a later pass (`Tools/gametext.py`).

**Art boundary.** No retail model or texture is copied into the repository. A unit is an
`ObjectReskin <RussiaName> <ExistingName>`: the engine copies the existing template whole (model,
draw, voices) and we patch `Side`, cost, health, weapons, command set and modules. Our own textures
(`Data/Art/Textures/*.tga|.dds`) may be added later for silhouettes. So the skeleton is **data on
existing models**; only the silhouette pass is authored art.

## Stage 1 — the fourth-side spike (proves it is a data job or a UI project)

Files:
- `Data/INI/PlayerTemplateReforged.ini` (new, `MULTIFILE` after `PlayerTemplate.ini`): one
  `PlayerTemplate FactionRussia` with `Side = Russia`, `BaseSide = China`, `PlayableSide = Yes`,
  `StartingBuilding = RussiaCommandBunker`, `StartingUnit0 = RussiaSapery`, `SideIconImage`,
  `PreferredColor`, `IntrinsicSciences`, the shortcut command set.
- `Data/INI/RussiaReforged.ini` (new, `MULTIFILE` after `FixesReforged.ini`): `ObjectReskin
  RussiaCommandBunker ChinaCommandCenter` + `Object RussiaCommandBunker` (`Side = Russia`,
  `DisplayName`, `CommandSet`), same for `RussiaSapery` on `ChinaDozer`, and the starting units.
- `Data/INI/AIDataReforged.ini` (new, `MULTIFILE` after `AIData.ini`): `SideInfo Russia` and
  `SkirmishBuildList Russia`.
- `Data/Patch.str`: `SIDE:Russia`, `INI:FactionRussia`, `OBJECT:`/`CONTROLBAR:` for the two objects.
- `GameEngine.cpp`: four `ini.load` calls (player template, roster, AIData) in the right order.
- `BuildFingerprint.manifest`: regenerated.

Proof: `./ai-batch.sh --data "$HOME/zh-data" --mapcells 96 --runs 1 --seedstart 0 --maxframes 200000
--extra "-side 0 FactionRussia -side 1 FactionAmerica"` starts **and finishes** (decided or capped,
not crashed, not idle). Question answered here: does a fourth side need engine/UI work beyond data?
If the skirmish side list, side icons or score screen refuse it, stop and report before the roster.

## Stage 2 — the skeleton roster (8 units, 8 buildings, upgrades, powers)

Every unit is an `ObjectReskin` of the named template with the patch block that makes it Russian.
Numbers are starting hypotheses, to be measured.

| Unit | Source model | Cost | HP | Speed | Weapon | Ability |
|---|---|---|---|---|---|---|
| Motostrelki | `ChinaInfantryRedguard` | 300 | 120 | 15 | AK-74 (rifle, 5-man) | garrison; **Igla** upgrade adds AA |
| Sapery | `ChinaDozer` | 500 | 200 | 20 | none (dozer) | the only builder; repairs; mine clear |
| Spetsnaz | `AmericaInfantryRanger` | 600 | 150 | 18 | SVD + AT | stealth detection; **designates** a target (+25% artillery damage, 30 s) |
| BMP-3 | `ChinaVehicleTroopCrawler` | 900 | 600 | 22 | 100 mm HE + ATGM | carries 5; the aggressive transport |
| T-72B (Mobilized) | `ChinaTankBattleMaster` | 700 | 900 | 18 | 125 mm (no ATGM) | none; no upgrade slots |
| T-90 | `ChinaTankOverlord` | 1600 | 1800 | 16 | 125 mm + gun ATGM | ERA/Shtora upgrade; slow |
| 2S19 Msta | `ChinaVehicleNukeCannon` | 1400 | 500 | 14 | 152 mm howitzer, min range | must deploy to fire; needs a spotter |
| Tunguska | `ChinaTankGattling` | 1100 | 800 | 20 | quad 30 mm + SAM | aircraft only; useless against tanks |

Deferred to the next stage: BTR-90, BM-30 Smerch, Mi-24 Hind, Su-25 Frogfoot (air needs the air base
and rearm work).

| Building | Source | Role | Delivers |
|---|---|---|---|
| Command Bunker | `ChinaCommandCenter` | HQ, radar, powers | starting building |
| Thermal Power Plant | `ChinaPowerPlant` | power | — (death explosion later) |
| Regimental Depot | `ChinaBarracks` | infantry | Motostrelki, Sapery, Spetsnaz; **Igla**, **Guards Conversion** |
| Mobilization Plant | `ChinaWarFactory` | cheap vehicles | T-72B (Mobilized), BMP-3 |
| Tank Works | `AmericaWarFactory` | real vehicles | T-90, 2S19, Tunguska; **ERA/Shtora** |
| Fire Direction Center | `AmericaStrategyCenter` | tech + artillery school | **artillery accuracy**, **range**, **reload**; the powers |
| Regimental Air Base | `ChinaAirfield` | air + SAM umbrella | aircraft (next stage); **SAM range** |
| Missile Brigade | `ChinaNuclearMissileLauncher` | superweapon | Missile Brigade (later) |

Upgrades are `Upgrade` blocks in `UpgradeReforged.ini` (new names), bought from the building that
delivers them: Depot (`Igla`, `GuardsConversion`), Tank Works (`ERAShtora`), Fire Direction Center
(`ArtilleryAccuracy`, `ArtilleryRange`, `ArtilleryReload`), Air Base (`SAMRange`).

Powers (`SpecialPowerReforged.ini` + `ScienceReforged.ini`, on the Command Bunker):
**Artillery Preparation** (rank 1, three scheduled waves), **Recon-Fire Complex** (rank 3, reveal +
mark), **Aerosol Screen** (rank 5, area stealth, reuses the GPS Scrambler mechanism), **Buratino
Salvo** (rank 5), **VDV Airborne Assault** (rank 8), **Missile Brigade** (superweapon).

AI wiring: `AIDataReforged.ini` `SideInfo Russia` (2 gatherers, base defence = Tunguska, SkillSet1/2
with the Russian sciences) and `SkirmishBuildList Russia` (the 8 buildings in a China-shaped base
layout, `AutomaticallyBuild` on the essentials); the AI's attack teams need `SkirmishRussia` in
`Data/Scripts/SkirmishScripts.scb`, added with `Tools/ScbTool/scbtool.py`. If the tool cannot add a
side cleanly, the fallback is `SkirmishChina`'s teams until it can — and then the AI builds but does
not attack, which is **not done**.

Command sets: `CommandSetReforged.ini` (one per building, one shortcut set), the same file the
engine already loads.

## Stage 3 — only after the roster plays

The Mobilized → Guards conversion (in-place object swap + veterancy + upgrade slots) as its own
spike, then one silhouette art pass (our textures), then balance.

## Measurement

`./ai-batch.sh --data "$HOME/zh-data" --runs 5 --maxframes 200000 --mapcells "96 128 160" --seedstart
0 --extra "-side 0 FactionRussia -side 1 FactionAmerica"` and the swapped ordering. Report decided,
idle/crash, average length and the per-seat table; a faction that starts but cannot be played by the
AI is not done.

## Result, 2026-10-21: the skeleton plays

The fourth side **builds, earns, trains the roster and attacks**, and a headless batch decides every
match. Two batches of six, generated maps at 96, 128 and 160 cells, brutal, 60,000-frame cap:

| Ordering | Decided | Russia wins | Avg length | Russia peak / kills / lost / spent | America peak / kills / lost / spent |
|---|---|---|---|---|---|
| Russia slot 0 | 6/6 | 6 | 24,797 (6.9 min) | 52.2 / 59.8 / 32.7 / 12,417 | 27.5 / 31.3 / 59.8 / 13,417 |
| Russia slot 1 | 6/6 | 4 | 30,922 (8.6 min) | 52.3 / 51.3 / 69.5 / 12,967 | 32.3 / 68.5 / 51.8 / 24,917 |

**The first twelve matches gave Russia ten wins.** Later clean-build batches (eight, sixteen, twelve,
eight and eight matches, 96-160 cells, both orders) kept the pattern. Across all **88 measured matches,
78 were decided and Russia won 69**; the ten frame-limited ones were mostly 128- and 160-cell games. That is a first
number, not a verdict: it says the faction is playable and that it sits above the shipped sides on these
maps, so **balance is now the open work**, not function. The full base goes up on the 96-cell seed-0 match at frames 3,939 (Supply Centre), 5,986
(Mobilization Plant), 7,099 (Tank Works) and 9,579 (Fire Direction Center); the first supply trucks
are bought at 3,964, T-72Bs and Tunguskas follow, and the match is decided at 13,927. No match crashed,
hung or ran to the cap.

### Breadth against the shipped sides (2026-10-21)

Russia is a complete core faction now, not just a couple of buildings:

| Category | Russia | An original side |
|---|---|---|
| Buildings | 12 (Command Bunker, Thermal Power Plant, Supply Centre, Regimental Depot, Mobilization Plant, Tank Works, Fire Direction Center, S-300 Battery, Missile Brigade, Regimental Air Base, Trench Line, Strongpoint) | 10-12 |
| Units | 12 (Sapery, Supply Truck, Motostrelki, Spetsnaz, Kornet Team, BMP-3, T-72B, T-90, 2S19, Tunguska, MiG-25, Mi-24) | 11-15 |
| Base defences | 3 (S-300 Battery, Trench Line, Strongpoint) | 1-2 |
| Superweapon | 1 (Missile Brigade) | 1 |
| Upgrades | 7 (capture, Igla, ERA/Shtora, Infantry Training, Aircraft Armour, Artillery Training, Command Doctrine) | 5-8 |
| General's powers | 7 (Artillery Preparation, Buratino Salvo, Scorched Earth, Radio-Electronic Strike, Long-Range Aviation, Guards Assault, Field Repair) | several |
| Aircraft | 2 (MiG-25 interceptor, Mi-24 attack helicopter) | 5-7 |

So the hole that is left is the **powers system** (one power works; the science tree, powers bar and the
other five powers are design), plus the art pass. The air arm is deliberately small - three airframes,
no strategic bomber - but it is no longer absent; the design's original "no air-superiority fighter"
line was overruled by the owner, who asked for a MiG-25.

The **S-300 Battery** is a reskin of the Patriot Battery: long-range AA, built by the dozer, and now the
`SideInfo Russia` base defense, so the AI's existing "build a defense" scripts finally have something
to build. It is the static half of the design's air denial.

The **Missile Brigade** is a reskin of China's Nuke Silo and reuses `SuperweaponNeutronMissile` as the
placeholder round. A human can build it after the Fire Direction Center with no defence count; a
computer player is held by the fork's twelve-defences-per-superweapon rule, and on the measured maps
the AI built 3-7 S-300s, so **the AI does not field the brigade yet**. Its wiring was verified with the
rule temporarily at one: the AI built it at frame 15,220 and aimed and fired the neutron missile. The
build-order scripts needed three fixes: China's Nuke Silo scripts are `once=1` with a condition false at
frame 0 (so they never ran), their Hard branch gifts 7,000 credits and repeated every pass once made
repeating (the AI's bank ran to 126 million), and the escalation gate of 75 is twelve and a half minutes,
longer than a measured match. Russia's copies run until the gate, ask at escalation 30 instead of 75, and
the gift is gone.

The **breadth pass** added the rest of a shipped side's categories: **Trench Line** (Fire Base howitzer,
800, anti-ground) and **Strongpoint** (Bunker, 500, garrison) beside the S-300, giving three base
defences; **Kornet Team** (Missile Defender, 400, anti-tank guided-missile infantry) as a unit; and
three upgrades - **Infantry Training** (+100 health to
Motostrelki and Spetsnaz), **Aircraft Armour** (+300 to the MiG-25 and Mi-24) and **Artillery Training**
(a new conventional `Russia2S19Gun`: 420 range, 8.5 s reload, no radiation field). The new units and
defences are human-buildable; the AI does not use them yet.

### Faction matrix, 2026-10-21

`Tools/Balance/faction_matrix.py` draws the non-Russian seats at random from the twelve lobby
templates each match and puts Russia on a random seat.  Two runs: 24 1v1 matches (96/128 cells, 60,000
frames; the opponent deck is shuffled so each template appears twice) and 24 2v2 matches (128/160
cells, 80,000 frames; Russia plus one random ally against two random enemies).  A frame-limited match
counts as a draw.  The per-seat rows are in `build-mac/faction_matrix_1v1.csv`,
`faction_matrix_2v2.csv` and `faction_matrix_combined.csv`.

**1v1** (Russia 18-3, 85.7% of decided):

| Faction | Played | Decided | Wins | Losses | Draws | Win% |
|---|---|---|---|---|---|---|
| China | 2 | 2 | 2 | 0 | 0 | 100.0 |
| **Russia** | **24** | **21** | **18** | **3** | **3** | **85.7** |
| China Infantry General | 2 | 2 | 1 | 1 | 0 | 50.0 |
| America | 2 | 1 | 0 | 1 | 1 | 0.0 |
| America Air Force General | 2 | 2 | 0 | 2 | 0 | 0.0 |
| America Laser General | 2 | 2 | 0 | 2 | 0 | 0.0 |
| America Superweapon General | 2 | 1 | 0 | 1 | 1 | 0.0 |
| China Nuke General | 2 | 1 | 0 | 1 | 1 | 0.0 |
| China Tank General | 2 | 2 | 0 | 2 | 0 | 0.0 |
| GLA | 2 | 2 | 0 | 2 | 0 | 0.0 |
| GLA Demolition General | 2 | 2 | 0 | 2 | 0 | 0.0 |
| GLA Stealth General | 2 | 2 | 0 | 2 | 0 | 0.0 |
| GLA Toxin General | 2 | 2 | 0 | 2 | 0 | 0.0 |

All three Russian losses were to China: twice to the base faction and once to the Infantry General.
Every American and GLA template lost both of its matches.

**2v2** (Russia 14-7, 66.7% of decided; the rate is the team result of every seat the faction held):

| Faction | Played | Decided | Wins | Losses | Draws | Win% |
|---|---|---|---|---|---|---|
| GLA | 4 | 3 | 3 | 0 | 1 | 100.0 |
| GLA Toxin General | 6 | 5 | 5 | 0 | 1 | 100.0 |
| GLA Demolition General | 10 | 9 | 6 | 3 | 1 | 66.7 |
| **Russia** | **24** | **21** | **14** | **7** | **3** | **66.7** |
| GLA Stealth General | 9 | 9 | 5 | 4 | 0 | 55.6 |
| China Infantry General | 8 | 7 | 3 | 4 | 1 | 42.9 |
| America Air Force General | 7 | 5 | 2 | 3 | 2 | 40.0 |
| America Laser General | 5 | 4 | 1 | 3 | 1 | 25.0 |
| America | 5 | 5 | 1 | 4 | 0 | 20.0 |
| America Superweapon General | 6 | 5 | 1 | 4 | 1 | 20.0 |
| China Nuke General | 5 | 5 | 1 | 4 | 0 | 20.0 |
| China | 3 | 2 | 0 | 2 | 1 | 0.0 |
| China Tank General | 4 | 4 | 0 | 4 | 0 | 0.0 |

**Combined** (48 matches, 42 decided, 32-10, 76.2%):

| Faction | Played | Decided | Wins | Losses | Draws | Win% |
|---|---|---|---|---|---|---|
| **Russia** | **48** | **42** | **32** | **10** | **6** | **76.2** |
| GLA Toxin General | 8 | 7 | 5 | 2 | 1 | 71.4 |
| GLA | 6 | 5 | 3 | 2 | 1 | 60.0 |
| GLA Demolition General | 12 | 11 | 6 | 5 | 1 | 54.5 |
| China | 5 | 4 | 2 | 2 | 1 | 50.0 |
| GLA Stealth General | 11 | 11 | 5 | 6 | 0 | 45.5 |
| China Infantry General | 10 | 9 | 4 | 5 | 1 | 44.4 |
| America Air Force General | 9 | 7 | 2 | 5 | 2 | 28.6 |
| America | 7 | 6 | 1 | 5 | 1 | 16.7 |
| America Laser General | 7 | 6 | 1 | 5 | 1 | 16.7 |
| America Superweapon General | 8 | 6 | 1 | 5 | 2 | 16.7 |
| China Nuke General | 7 | 6 | 1 | 5 | 1 | 16.7 |
| China Tank General | 6 | 6 | 0 | 6 | 0 | 0.0 |

The 2v2 table is not a faction-strength table on its own: every seat's result is its team's result, so
a strong ally lifts a faction and a strong enemy lowers it.  It does say where Russia's 2v2 losses
came from: all seven came against a team with at least one GLA general, and two were Russia plus an
American ally against two GLA.  Russia remains above the field in 1v1 and in the
middle of the 2v2 spread, with China the only side that beat it one-on-one.

### Powers, AI coverage and balance pass, 2026-10-21

The Command Bunker inherits seven of China's Command Centre power modules.  All seven now carry
Russian names and buttons, and the Fire Direction Center's **Command Doctrine** unlock gates them:
the player buys the (free) doctrine at the centre, and the AI's own script grants the sciences the
moment its centre stands, so the AI uses the same powers without a science-purchase screen.

| Power | Effect | AI |
|---|---|---|
| Artillery Preparation | three-wave barrage | existing fire script |
| Buratino Salvo | thermobaric/napalm run | added always-on fire script |
| Scorched Earth | cluster minefield | existing fire script |
| Radio-Electronic Strike | EMP pulse | existing EMP script (no longer remapped to the missile) |
| Long-Range Aviation | carpet bombing run | existing carpet script, aimed at the short carpet power the bunker has |
| Guards Assault | frenzy buff on our own troops | added script, targets our base/damaged vehicle |
| Field Repair | repairs our vehicles | added script, targets our most damaged vehicle |

The **AI now uses the whole roster**, not a human-only subset: the Trench Line is an automatic
structure in the AI build list, the Strongpoint comes from the mapped Bunker build actions, the
Kornet Team from the mapped Tank Hunter teams, the Air Base/MiG-25/Mi-24 from the air teams, and all
seven powers from the scripts above.  The AI buys the six ordinary upgrades; the doctrine is granted
by its centre script.

**Balance changes** (from the 85.7% / 66.7% matrices):

- Borrowed building shells to their source costs: Thermal Power Plant 600 -> 1,000, Supply Centre
  1,000 -> 1,500, Mobilization Plant 1,200 -> 2,000, Fire Direction Center 1,500 -> 2,000, Trench Line
  800 -> 1,000.
- T-72B 700 -> 800, T-90 1,600 -> 2,000, 2S19 1,400 -> 1,600.
- The S-300 was a multi-role gun: the Patriot's copied set is ground-primary, air-tertiary.  It now
  keeps only the anti-air slot, so it is a SAM and the Trench Line is the ground defence.
- The seven powers are gated behind the Fire Direction Center's Command Doctrine instead of being
  free from the opening Command Bunker.
- The AI can place a superweapon outside the base edge when its footprint no longer fits inside.

**After** (same random-faction method, fresh draws):

| | 1v1 | 2v2 |
|---|---|---|
| Russia before | 18-3, 85.7% | 14-7, 66.7% |
| **Russia after** | **12-7, 63.2%** | **13-10, 56.5%** |
| Draws | 5 of 24 | 1 of 24 |

The 1v1 losses are spread across the field (America Air Force twice, GLA Demolition twice, China Tank,
GLA and GLA Toxin once each), not concentrated on one counter.  The 2v2 rate is a team result; the
sample is 24 matches, so the intervals are wide, but the direction is consistent with the changes.

### Model audit, 2026-10-21

Every Russian object is an `ObjectReskin`; there is no Russian art in the tree. The audit covered all
24 reskins and checked four things: the model loads (no missing-model or missing-texture line in a
match's log), the reskin keeps every behaviour its source has (`ReskinCheck`: 0 of 260 lacking one),
no two Russian objects use the same model, and whether the model takes the faction colour.

| Russia object | Source | Base model | Colour |
|---|---|---|---|
| Command Bunker | ChinaCommandCenter | NBConYard | yes |
| Thermal Power Plant | ChinaPowerPlant | NBPwrPlant | yes |
| Supply Centre | ChinaSupplyCenter | NBSupCent | yes |
| Regimental Depot | ChinaBarracks | NBBarracks | yes |
| Mobilization Plant | ChinaWarFactory | NBWarFact | yes |
| Tank Works | AmericaWarFactory | ABWarFact | yes |
| Fire Direction Center | ChinaPropagandaCenter | NBPCenter | yes |
| S-300 Battery | AmericaPatriotBattery | ABPatriot | yes |
| Trench Line | AmericaFireBase | ABFIREBASE | yes |
| Strongpoint | ChinaBunker | NBBunker | yes |
| Missile Brigade | ChinaNuclearMissileLauncher | NBNMissle | yes |
| Regimental Air Base | ChinaAirfield | NBAirfield | yes |
| Sapery | ChinaVehicleDozer | NVCONSTDOZ | **no** |
| Motostrelki | ChinaInfantryRedguard | NICNSC | yes |
| Spetsnaz | AmericaInfantryRanger | AIRngr | yes |
| BMP-3 | ChinaVehicleTroopCrawler | NVTCrawler | yes |
| T-72B | ChinaTankBattleMaster | NVBtMstr | yes |
| T-90 | ChinaTankOverlord | NVOvrlrd | yes |
| 2S19 | ChinaVehicleNukeLauncher | NVNukeCn | yes |
| Tunguska | ChinaTankGattling | NVGattTank | **no** |
| Supply Truck | ChinaVehicleSupplyTruck | NVSSUPPLYTK | yes |
| MiG-25 | ChinaJetMIG | NVMIG | yes |
| Mi-24 | ChinaVehicleHelix | NVHELIX | yes |
| Kornet Team | AmericaInfantryMissileDefender | NITHNT | yes |

Nineteen sources are Chinese and five American. No Russia object shares a model with another Russia
object, and every model loads. The Sapery and Tunguska do not accept the faction colour, so they keep
the source's colour while the other twenty-two turn red with the player.

**What the audit cannot fix:** every model is still shared with the faction it came from, and the
Chinese building set is the largest visual mismatch - the base reads as Chinese architecture with red
paint, not as a Soviet base. The MiG-25 was the worst unit case (it wore the American Raptor); it now
wears the MiG model with an air-to-air weapon set, at the cost of sharing the MiG's silhouette with
China. A true Russian look needs authored models and textures; that is the art pass, not a data fix.

The **first general's power** is **Artillery Preparation** on the Command Bunker. The Command Centre's
artillery-barrage `OCLSpecialPower` is inherited by the Russian bunker; the PlayerTemplate grants the
three barrage sciences intrinsically, so the power needs no purchase yet. It fires - the AI aimed it at
frame 19,826 in one measured match - and it is the template the rest of the powers (Recon-Fire,
Aerosol Screen, Buratino, VDV) will follow.

The **air arm** is the owner's revision of the design's original "no air-superiority fighter" line: a
Regimental Air Base (1,500, Fire Direction Center) producing a **MiG-25** (1,200, interceptor) and a
**Mi-24** (1,500, attack helicopter and troop carrier). The MiG reskins the shipped MiG model - the
only Russian-looking jet in the data - and a patch weapon set with the same conditions replaces that
model's napalm with a new air-to-air `RussiaMiG25Missile`; the Mi-24 reskins the Helix. Both are
placeholders until the art pass, and the Su-25 remains the designed but unbuilt CAS airframe. The AI builds the air
base on the longer maps (frames 12,449 and 21,756 in two batch matches), fields both aircraft, and they
fight: a MiG-25 killed a Humvee and a Ranger in one match, a Mi-24 damaged a barracks in another.

### What it took (and the general defects it exposed)

The skeleton was not a data-only job. Each of these was measured, and each fix is general - it is not a
Russia special case:

1. **Patch blocks appended prerequisites instead of replacing them.** `ThingTemplate::parsePrerequisites`
   cleared its list for `INI_LOAD_CREATE_OVERRIDES` only, so `Object RussiaX` under `INI_LOAD_MULTIFILE`
   kept the source's China prerequisite beside the Russian one. No Russian building that needed another
   Russian building could ever be built (`Mobilization Plant` reported `NO_PREREQ` for 60,000 frames
   while 6,300 sat in the bank). Fixed in `ThingTemplate.cpp`; the base tree loads objects with
   `INI_LOAD_OVERWRITE`, so no shipped object changes.
2. **Russia had no gatherer.** Supply centres do not earn by themselves; the AI builds any template with
   `KINDOF_HARVESTER`. Added `RussiaSupplyTruck` (reskin of `ChinaVehicleSupplyTruck`), its button and
   its set on the supply centre. Income starts the frame the first truck exists.
3. **The AI placed supply centres inside the engine's supply build border.** `buildBySupplies` offset a
   cash generator by a fixed three pathfind cells; `isLocationLegalToBuild` refuses a supply centre
   within `SupplyBuildBorder` of the source, so the dozer gave up every spot and never tried another
   dock. The offset now clears the border (`AIPlayer.cpp`).
4. **An inert Spy Satellite ping read as an enemy.** `isLocationSafe` counted any enemy object inside
   `SupplyCenterSafeRadius`; the ping is `NO_COLLIDE IMMOBILE UNATTACKABLE INERT` and sits still for the
   match. It is filtered out now. This is the same class of defect the seven expansion attempts in
   [ai-expansion-build-timer.md](../ai/ai-expansion-build-timer.md) were circling.
5. **A supply centre in the home base was called "unsafe".** On a 96-cell map the enemy's own power
   plant stands inside the 300-unit safe radius, so every candidate was refused. Home is not an
   expansion: a cash generator inside the base radius skips the enemy-proximity check.
6. **Two reskins of one model were indistinguishable to a command set.** `isEquivalentTo` calls two
   reskins of `ChinaWarFactory` equivalent; `isPossibleToMakeUnit` kept the *last* matching button, so
   asking whether the dozer could build a Mobilization Plant answered for the Tank Works (which needs
   it) and Russia never built a vehicle. An exact template match now wins over an equivalent one, and
   the Tank Works reskins `AmericaWarFactory` as the plan always said, so the two buildings no longer
   collide.
7. **The skirmish script copy did not rename with its own references.** Scripts are found by name
   across the whole file, so renaming a Russia script in `SkirmishScripts.scb` without moving its
   `ENABLE_SCRIPT`, `CALL_SUBROUTINE` and team-hook references pointed Russia at the China side's
   scripts. `Tools/ScbTool/russia.py` now remaps all of them, and is idempotent so the file can be
   regenerated in place.

### The first upgrade tier (2026-10-21)

Two player upgrades, both bought from the building the design gives them:

- **Igla** (`Upgrade_RussiaIgla`, Regimental Depot, 1,000): a `WeaponSetUpgrade` adds a
  `PLAYER_UPGRADE` weapon set to Motostrelki with the Stinger missile in the secondary slot. The
  Stinger is a placeholder for the Igla launcher until the weapon pass. Verified applied: a match
  that bought it logged the set being set on twelve Motostrelki already on the field.
- **ERA/Shtora** (`Upgrade_RussiaERAShtora`, Tank Works, 1,500): a `MaxHealthUpgrade` adds 400 health
  to every T-90. A health pool stands in for a separate armour type until the stat pass.

Both modules are added with `AddModule`, not as bare `Behavior` lines: a restated module clears every
copied module sharing its interface (port defect 34), and the reskin check then reports the T-90
losing the Overlord's upgrade modules. `ReskinCheck` now reports 0 of 252 reskins lacking a source
module.

The AI buys both from the skirmish scripts: `russia.py` maps the relevant China upgrade purchases to
Russia's (Nationalism and Neutron Shells to Igla, Uranium Shells and Nuclear Tanks to ERA/Shtora) and
drops the air upgrades it has no aircraft for. Measured on 16 matches, 96-160 cells, brutal, both
orders: Igla was queued in 5 and ERA/Shtora in 8; 13 matches were decided and Russia won all 13.

### Still open

- **Balance.** The running total is 88 measured matches, 78 decided, Russia 69. That is a faction that
  plays, not a balanced one; a faction-vs-faction table is the next measurement.
- **The rest of the powers.** Artillery Preparation works from the Command Bunker. The science tree and
  the powers bar are not built (the three barrage sciences are granted outright), and Recon-Fire,
  Aerosol Screen, Buratino Salvo and VDV are still design. This is the next structural gap.
- **Windowed play-test.** The bar, load-screen portrait and Eva fall back to China's; headless draws
  none of them, so the owner's Windows (or windowed macOS) check is still the one that proves them.
- **The artillery tier.** Capture, Igla and ERA/Shtora are wired. The artillery-school upgrades need
  Russia's own howitzer weapon (the 2S19 still fires the Nuke Cannon's, and a bonus on that template
  would buff China too).
- **Art.** Every unit and building is borrowed geometry. The silhouette pass is Stage 3.
- **Two unit types are not yet fielded in a short match.** T-90, 2S19 and BMP-3 appear in the harder
  teams but a 3-9 minute match ends before those waves; longer batches must confirm their factories and
  prerequisites.
