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
- [ ] Russia faction removed from Vanguard.
- [ ] Combined skeleton (Russia + ECA templates, one unit/building each).
- [ ] Full Russia port.
- [ ] Full ECA port.
- [ ] Campaign pack.
