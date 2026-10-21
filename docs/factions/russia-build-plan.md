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

**The first twelve matches gave Russia ten wins.** Two later clean-build batches (eight and sixteen
matches, 96-160 cells, both orders) decided 22 of 24 and Russia took 18. Across all **36 measured
matches, 34 were decided and Russia won 28**; the two frame-limited ones were 160-cell games. That is a
first number, not a verdict: it says the faction is playable and that the skeleton sits above the shipped
sides on these maps, so **balance is now the open work**, not function. The full base goes up on the 96-cell seed-0 match at frames 3,939 (Supply Centre), 5,986
(Mobilization Plant), 7,099 (Tank Works) and 9,579 (Fire Direction Center); the first supply trucks
are bought at 3,964, T-72Bs and Tunguskas follow, and the match is decided at 13,927. No match crashed,
hung or ran to the cap.

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

- **Balance.** The running total is 52 measured matches, 47 decided, Russia 41. That is a faction that
  plays, not a balanced one; a faction-vs-faction table is the next measurement.
- **Windowed play-test.** The bar, load-screen portrait and Eva fall back to China's; headless draws
  none of them, so the owner's Windows (or windowed macOS) check is still the one that proves them.
- **Powers and the artillery tier.** Capture, Igla and ERA/Shtora are wired. The artillery-school
  upgrades need Russia's own howitzer weapon (the 2S19 still fires the Nuke Cannon's, and a bonus on
  that template would buff China too); the six general's powers, the science tree and the powers bar
  are still design.
- **Art.** Every unit and building is borrowed geometry. The silhouette pass is Stage 3.
- **Two unit types are not yet fielded in a short match.** T-90, 2S19 and BMP-3 appear in the harder
  teams but a 3-9 minute match ends before those waves; longer batches must confirm their factories and
  prerequisites.
