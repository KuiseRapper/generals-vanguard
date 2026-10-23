# Vanguard Combined: the plan

**Vanguard Combined** is this fork's own mod: **ShockWave 1.201 is the base**, and **Rise of the Reds'
Russian Federation and ECA** are added on top. It ships in the launcher's mod list next to
ShockWave, RotR and vanilla, and it is one merged data set, not two `-mod` layers.

ShockWave and Rise of the Reds are both by SWR Productions; this merge is unofficial and neither
team supports it. Credit both, and keep their licences intact.

## Why a merge, not two mods
`-mod` layers files: a later layer replaces an earlier one file by file, and INI blocks with the same
object name replace each other. Loading ShockWave and RotR together would give one mod's USA/China
and the other's leftovers, not both armies. The combined mod keeps ShockWave's data for the base
game and imports only RotR's Russia/ECA trees, so the two never share a file name on purpose.

## What comes from where
| Area | Source |
|:--|:--|
| USA, China, GLA, their generals, units, defences, powers, UI | ShockWave 1.201 |
| Russian Federation (units, buildings, powers, cameos, AI data) | RotR 1.8 |
| ECA (same) | RotR 1.8 |
| Engine fixes, performance, AI, balance | this fork |
| Campaign missions | RotR, as a separate pack loaded with the combined mod |

**Not taken:** RotR's reworked USA/China/GLA. Importing them would overwrite ShockWave's base game,
which is the whole point of using ShockWave as the base.

## The work, in order
1. **Remove Vanguard's own Russia faction** from the base game (data, science, powers, command sets,
   art, load-screen branch, AI data, scenario, docs). Vanilla Vanguard becomes plain Zero Hour plus
   the fork's fixes/AI/performance/balance. Keep the extracted RotR art: it moves into the combined
   mod rather than being deleted.
2. **Combined skeleton.** ShockWave's INIs untouched; add RotR's `PlayerTemplate` entries for Russia
   and ECA, plus one working unit and one building per faction, with their art and cameos. Test a
   skirmish. This proves the seam.
3. **Port the rest of Russia**, then **ECA**: objects, weapons, sciences, command sets/buttons, AI
   data, cameos, side icons, load screens, UI atlases.
4. **Resolve seams:** shared `Science.ini`, `CommandSet.ini`, `AIData.ini`, `MappedImages`, FX and
   object names; anything RotR's factions reference that ShockWave does not ship.
5. **Balance and AI pass**, then a play-test.
6. **Campaign pack:** RotR's missions and maps, loaded with the combined mod.
7. Ship it in the launcher as a bundled entry with a details sheet (what it includes, version,
   credits), plus a "report a problem" path.

## Rules
- The base game is ShockWave's; RotR only supplies the two new armies and the campaign.
- Every imported file is credited to SWR Productions; no re-licensing.
- Each stage ends with a headless scenario run and a screenshot before it is called done.

## Progress
- [x] Launcher lists the combined mod with a details sheet (in development).
- [x] Russia faction removed from Vanguard (template, data and art; the art and INIs are staged
  under `build-mac/combined_staging/` for the mod).
- [x] Combined skeleton: ShockWave base + `FactionVanguardRussia` and `FactionVanguardECA` start,
  with RotR units spawned in a skirmish; shipped as `VanguardCombined-0.1.zip` and listed in the launcher.
- [x] Merged string table: ShockWave's `Generals.str` plus every RotR-only label, shipped as
  `Data/Generals.str` in the mod.
- [ ] Full Russia port (production, AI, balance).
- [ ] Full ECA port (same).
- [ ] Campaign pack.

## Finding: how RotR 1.8 gets Russia and ECA into the game

RotR does **not** ship extra player templates. Its `PlayerTemplate.ini` has seven, the retail set,
and it reuses the two America general slots:

- `FactionAmericaLaserGeneral` has `BaseSide = Russia`, `DisplayName = INI:FactionRussia`,
  `StartingBuilding = RussiaCommandCenter`, Russia sciences, Russia shortcut bar.
- `FactionAmericaSuperWeaponGeneral` has `BaseSide = ECA`, the ECA base and sciences.

The comment in the file says it plainly: "named as the Laser general to make them work on unofficial
ZH Maps". A mod that only has those seven templates can be picked on stock maps because the map asks
for a template name it recognises.

**This is the central collision with ShockWave**, which uses those same slots for its own Laser and
Super Weapon generals (and has seventeen templates). A combined mod cannot give both mods the slot.

Our fork already proves the engine accepts *more* templates: the custom Russia faction was an extra
`PlayerTemplate FactionRussia` and appeared in the lobby. So the combined mod's route is:

1. Keep ShockWave's seventeen templates unchanged.
2. Add **two new templates** (`FactionVanguardRussia`, `FactionVanguardECA`) with their own
   `Side`/`BaseSide` values, pointing at RotR's `RussiaCommandCenter` and ECA starting building.
3. Port RotR's Russia/ECA sides, objects, sciences, command sets and art under those templates.
4. Check the map-start logic: skirmish start positions and any map scripts that name a side; the fork's
   extra template already worked for skirmish, so this is the same path.

That is the seam to build first, before importing a single unit: two new templates that show up next
to ShockWave's generals in the lobby.
