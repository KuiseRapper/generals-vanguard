# Russia assets — plan

The current release has Russia gameplay, a unique 2D icon atlas, menu/side/general/score/load art and
its own OCLs. The remaining gap is the **spawned 3D models**, which are still reskins. This is the plan
to replace them properly.

## Licensing first

- **Rise of the Reds** and other mods are a **reference**, not an asset source. Their meshes and textures
  are not ours to redistribute, and this tree is GPL-3.0 + EA section 7. Use them to study silhouettes,
  proportions, camo and UI language only.
- Real vehicles (T-72B, T-90, BMP-3, 2S19, Tunguska, MiG-25, Mi-24) are the primary reference; build our
  own meshes and textures from them.
- Every mesh, texture, icon and sound we add is ours, with the fork's copyright header on source files.

## 3D pipeline

1. **Blockout** in Blender: 1 unit = 1 m, Z up, matching the W3D scale of the China/USA placeholders.
2. **Mesh** low-poly, bevelled hard-surface, one material per texture page.
3. **UV** + bake AO/curvature to aid hand-painted diffuse and specular.
4. **Textures**: 1024² (vehicles), 2048² (buildings), DDS DXT5; a shared Russia camo palette
   (olive/grey-green, red-star decals, white tactical numbers).
5. **Rig** a minimal skeleton: hull, turret, barrel, wheels/tracks, rotor/landing gear; Idle/Move/
   Attack/Die sequences.
6. **Export** W3D with the W3D exporter; ship `<name>.w3d` + textures + the `W3DModelDraw` block.
7. **LODs**: high/medium/low; the game already draws far batches.
8. **Test**: spawn each unit on a flat map, all camera distances, shadows, team colour, Turret/Barrel
   bones, death animations, and a 1v1 against the AI.

## Model list (priority order)

1. **T-72B / T-90** — the two Russian tanks; the most-seen models.
2. **BMP-3** — IFV.
3. **2S19 Msta** — self-propelled gun.
4. **Tunguska** — AA vehicle.
5. **MiG-25** — interceptor.
6. **Mi-24** — attack helicopter.
7. **Motostrelki / Spetsnaz / Kornet** — infantry (3 poses + death).
8. **Buildings** — Command Bunker, Thermal Plant, Supply Center, Regimental Depot, Mobilization Plant,
   Fire Direction Centre, SAM, Air Base, Missile Brigade, Trench, Strongpoint.
9. **Superweapon** — the Siberian Front weather station.

## 2D and UI

- Re-render each finished mesh into its **build button**, **SelectPortrait** and **score-screen** cameo;
  the current icons stay until then.
- Loading/score/general images: Russia flag and red star only; no China flag, no USA general. The current
  `SAFactionLogoPage_RU`, `Russia_ScoreScreen`, `Russia_Logo`, `GameinfoRUSSIA` are the placeholders to
  refine.
- Check every screen a Russia player sees: lobby, loading, score, end screen, replay, observer.

## Balance and correctness

- A batch of 1v1 and 2v2 seeds with the AI doctrine restored.
- Verify every Russia command button has an object/upgrade/science, every science is purchasable, every
  special power has a timer and a valid OCL, and the radar/options/cursor work.
- Only then tune numbers.

## Patch order

1. Options/mouse/cursor, building order, loading/spectator labels.
2. 3D models in the priority order above.
3. Icons/cameos from the new renders.
4. Balance batch.
