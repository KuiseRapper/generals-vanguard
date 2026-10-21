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
