# Rise of the Reds 1.8 art, and how it got in here

The Russia faction's buildings are no longer China's. Their models and textures come from **Rise of
the Reds 1.8** (SWR Productions, December 2012), the Zero Hour mod that already has a Russian
Federation and an ECA faction.

## Provenance

- Download: `ROTRBeta_18_Noinstl.zip` from the ModDB file page (no-installer, "MAC compatible"),
  size 500,193,329 bytes, MD5 `c7f2da57cf655cf9c86959d74b91210e`.
- It holds the mod's `.big` archives; the ones used here are `!BetaRotrW3D.big` (models) and
  `!BetaRotrTextures.big` (textures), with the object INIs in `!BetaRotrINI.big`.
- The 1.85 installer (`ROTRBeta_185.exe`) is a Clickteam Install Creator package and does not unpack
  with 7-Zip; 1.8's no-installer zip is the same art and unpacks directly.

## What was taken

| Our object | RotR model | RotR source INI |
|:--|:--|:--|
| `RussiaCommandBunker` | `RBComBnkr` | `Object/Russia/Buildings/CommandBunker.ini` |
| `RussiaThermalPowerPlant` | `RBPwrPlnt` | `CoalPowerPlant.ini` |
| `RussiaRegimentalDepot` | `RBBarracks` | `Barracks.ini` |
| `RussiaMobilizationPlant` | `RBWarfct` | `Warfactory.ini` |
| `RussiaAirBase` | `RBAirfield` | `Airfield.ini` |
| `RussiaSupplyCenter` | `RBSuplyCntr` | `SupplyCenter.ini` |
| `RussiaFireDirectionCenter` | `RBRadar` | `RadarStation.ini` |
| `RussiaStrongpoint` | `RBWepnBnkr` | `WeaponBunker.ini` |
| `RussiaMissileBrigade` | `RBTremor` | `Tremor.ini` |
| `RussiaT72B` | `RVT64` | `Vehicles/Rhino.ini` (`W3DTankDraw`) |
| `Russia2S19` | `RVMSTAS` | `Vehicles/Msta.ini` |
| `RussiaTunguska` | `RVTngska` | `Vehicles/Tunguska.ini` |
| `RussiaSupplyTruck` | `RVSplyTrck` | `Vehicles/SupplyWarTruck.ini` |
| `RussiaMiG25` | `RVMig` | `Aircraft/MiGBomber.ini` (`W3DModelDraw`) |

Each model's `_D` (damaged), `_S` (snow), `_N` (night), `_SC` (under construction) and `_BF`
(build-up) variants were extracted with it, and every texture the W3D files name (`.tga` in the
model, `.dds` on disk — the engine substitutes the extension) was copied from
`!BetaRotrTextures.big`.

## How it is wired

`Data/INI/RussiaReforged.ini` adds one `ReplaceModule` per draw module on the base object:

```
Object RussiaCommandBunker
  ReplaceModule ModuleTag_01
    Draw = W3DModelDraw ModuleTag_RU_01
      ...
    End
  End
  ReplaceModule ModuleTag_02
    Draw = W3DModelDraw ModuleTag_RU_02
      DefaultConditionState
        Model = None
      End
    End
  End
  ...
End
```

ModuleTag_01 becomes the RotR model with its damage, snow, night and construction states; the base
object's other draw modules (doors, fences, China-specific bits) are replaced with `Model = None`.
**A unit's replacement must use the base's own draw module type** — `W3DTankDraw` for a tank,
`W3DTruckDraw` for a truck, `W3DModelDraw` for an aircraft. A different type is skipped with a log
line and the base model stays.

**The keyword matters:** a draw module is `Draw = W3DModelDraw tag`, not
`Behavior = W3DModelDraw tag`; the wrong one is an INI parse failure (the game exits at load).

## Looking at them without touching the mouse

`Data/Scenarios/russia_show.txt` spawns the imported units around the player's start and tallies
them; run it windowed and screenshot the window:

```
generals -win -quickstart -noshellmap -map "Maps\Alpine Assault\Alpine Assault.map"
  -autoskirmish 2 -side 0 FactionRussia -side 1 FactionAmerica -seed 1
  -maxframes 400 -scenario russia_show -noaudio
```

The scenario drives the whole match, so no mouse or keyboard is needed. `-headless` instead of
`-win` runs the same file for logic-only checks.

## Re-extracting

```
python3 GeneralsMD/Code/Tools/bigfile.py list build-mac/mods/rot18/'!BetaRotrW3D.big'
python3 GeneralsMD/Code/Tools/bigfile.py extract ... 'Art\W3D\RBComBnkr.W3D' -o /tmp/rot
```

The extraction script used for the nine buildings is in the commit that added this file; the
textures a model needs are the `.tga` names inside the W3D (`strings` finds them).

## Still placeholder

- Build-button cameos are still the base models' retail cameos.
- Aircraft other than the MiG-25, and the infantry, are still China/USA models.
- The superweapon effect is ours, not RotR's.

## Licensing

These files are SWR Productions' work and are **not** GPL. They are in this tree because the owner of
the fork directed that the Russia faction use them; see the third-party section in
[NOTICE.md](../../NOTICE.md). They can be removed without touching the code.
