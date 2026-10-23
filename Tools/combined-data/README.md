# Combined mod data overrides

The combined mod is assembled outside this tree; these are the files that keep its **AI and its
main menu** working, copied verbatim into the package at the paths below.  They are here so the fixes
are versioned, not lost in a build folder.

| File | Goes to | Why |
|:--|:--|:--|
| `PlayerTemplate.ini` | `Data/INI/PlayerTemplate.ini` | The two new factions use RotR's own skirmish-slot sides (`AmericaLaserGeneral`, `AmericaSuperWeaponGeneral`).  A stock map has no side for a faction it has never heard of, so with a side of its own the player found no skirmish scripts and `Player::initFromDict` quietly made the AI seat a **human** one.  ShockWave's own two generals for those slots are set `PlayableSide = No` so the slots are the new factions' alone. |
| `AIData.ini` | `Data/INI/Default/AIData.ini` | The RotR build lists (`SideInfo` / `SkirmishBuildList`) are named for those same sides, so the AI builds Russia's and ECA's structures and units instead of America's. |
| `GameData.ini` | `Data/INI/GameData.ini` | `ShellMapName` points at `ShellMapSHW`, a map inside the mod's own maps archive.  Maps are fork-owned, so a mod's map is never listed, the shell map map was not in the cache, and the main menu fell back to its static backdrop.  Pointing it at retail's `ShellMap1` gives the animated menu back. |
