# Mob Modelling

How the fruit & vegetable mobs are modelled, rigged, animated and shipped. Written
after building the first five (Apple, Carrot, Pear, Banana, Watermelon boss), which
are the reference implementation. See `MobRoster.md` for *what* each mob is and
`ARCHITECTURE.md` for the wider codebase.

> **Status:** all 45 mobs have Blender-authored, rigged, walk-animated GLBs.
> Waves 31–34 plus Granola Mom (now wave 45) are authored in
> `tools/blender/mobs_wave31_35.py`. Waves 35–44 are authored in
> `tools/blender/mobs_wave35_44.py`. Granola Mom has a dedicated humanoid rig;
> the other mobs use the shared six-bone rig. Movement-speed playback still
> needs play-testing.

---

## Files

| Path | Purpose |
|---|---|
| `tools/blender/mobs_first5.py` | Authoring script for the first five: geometry, rig, walk cycle, export |
| `tools/blender/mobs_wave35_44.py` | Authoring script for ten late-game mobs, six-bone walk, export |
| `Assets/Resources/Snack/Mobs/<id>.glb` | Runtime model, loaded by `Snack/Mobs/<MobDef.id>` |
| `Assets/Models/Mobs/<id>.glb` | Source mirror. Always export to **both** |
| `Assets/Scripts/MobWalkAnimation.cs` | Plays the baked clip at a speed matched to real movement |
| `Assets/Scripts/SnackVisuals.cs` (`MobVisual.Build`) | Instantiates the model, calls `MobWalkAnimation.Attach` |
| `Assets/Editor/MobWalkCheck.cs` | Batch-mode verification of the rigged GLBs |

Adding a mob needs **no gameplay code**: drop `<id>.glb` into both folders, where
`<id>` matches `MobDef.id` in `MobCatalog.cs`. Missing models fall back to the
procedural `ChildModel`.

## Model conventions (hard requirements)

- **~1 unit tall**, base on **z = 0**, centred in X/Y. The game scales it by
  `def.scale * 1.5` (`MobVisual.Build`), so author at 1.0-1.2 tall.
- **Forward is Blender −Y** (→ Unity +Z). Faces and toes point −Y.
- **Low-poly, flat shaded**: primitives with few segments (lathes of 10-16 sides,
  6-sided limbs), **no subdivision**, `use_smooth = False`. The five shipped mobs are
  149-453 faces each (1,283 total).
- **Colourful, toy-like**: saturated Principled BSDF `Base Color`, roughness ~0.55,
  metallic 0. Setting `material.diffuse_color` alone does nothing for renders.
- **Faces are simple**: small flat black eyes (a short 8-sided disc) and a curved
  mouth strip, plus optional brows/lids. No white eyeballs or pupils. That was an
  explicit art-direction call: white "human" eyes read as generic AI-looking.
- **One mesh object per mob**, named exactly the `MobDef.id`, with one material per
  colour. Everything is joined, so a mob is a single `SkinnedMeshRenderer`.
- Bake transforms **before** re-centring, or the model lands offset. The script builds
  vertices directly in object space (identity transform) and normalises afterwards:
  scale to target height, then translate so min-Z is 0 and X/Y are centred.

## Art direction: approval workflow

1. Ask what the mob looks like, then generate a **concept image first** with Muse
   Image (`POST https://api.meta.ai/v1/images/generations`, `model: muse-image-1.0`,
   key read from `HKCU:\Environment\META_MODEL_API_KEY` at call time, never echoed).
   Save to `%TEMP%\opencode\`, show it, and open it in the OS viewer.
2. **Get approval before building.** Do not skip this gate.
3. The first prompt gave a glossy, generic "AI" look and was rejected. What worked:
   ask explicitly for **"low-poly stylized 3D game character, chunky simplified
   geometry with visible flat-shaded facets, 150-300 polygons, solid flat colours,
   NO glossy reflections, simple face from a few basic shapes, tiny stubby feet,
   3/4 view on plain light grey"**.
4. Match the new towers: chunky, toy-like, cute faces, consistent with the latest
   tower art.

## Building the geometry

`mobs_first5.py` uses a tiny `Builder` that accumulates vertices/faces/materials and a
handful of primitive generators (all return `(verts, faces)`):

- `sphere_g`, `frustum_g` (cylinder/cone), `box_g`, `lens_g` (leaf), `lathe_g`
  (revolved profile, bottom to top), `tube_g` (tapered tube along a polyline),
  `strip_g` (thin prism through two polylines, used for mouths).
- **Decals sit exactly on facets.** Faces are placed with a surface object
  (`LatheSurf.pt(x, z, off)` / `BananaSurf.pt`) that returns the point on the
  *faceted* low-poly surface, not the ideal curve. Placing a disc or strip at the
  smooth radius makes it float or sink into the flats.
- The body is a **lathe** for round mobs (Apple, Carrot, Pear, Watermelon). The Banana
  is a tapered 6-sided tube along a curved centre-line.
- Watermelon stripes come from per-face materials (alternate by lathe segment).

Gotchas hit along the way:

- The Carrot's first lathe profile produced three stacked "pancake" discs. Use a
  smooth monotonic taper plus small **ridge decals** instead.
- The Watermelon's flesh/seed mouth was too dense. Fewer, larger seeds (squashed
  spheres) read better than many tiny ones and cut the face count.
- Blender's `export_format` enum reads back **empty** at call time. Don't validate it
  up front; just pass `'GLB'`.
- `render.view_transform` has only `NONE` in this build. Set enums defensively by
  reading the valid list first (`_set_enum` in the script).

## Rig

Every mob shares the same skeleton, **rigid-skinned** (each vertex belongs to exactly
one bone, weight 1.0):

```
root
├─ body            (everything that is not a leg or foot)
├─ leg_a ─ foot_a  (x < 0)
└─ leg_b ─ foot_b  (x > 0)
```

- All bones point +Y with **roll 0**, so a bone's local axes equal world axes:
  X = swing, Z = bob, Y = waddle. This keeps the keyed values easy to reason about.
- The `Builder` tags each added part with its bone (`cur_bone`), and `feet()` records
  the **hip** (top of the leg) and **ankle** (foot centre) pivots. Those pivots are
  transformed by the same scale/translation as the mesh before the bones are placed.
- The mesh is parented to the armature with an Armature modifier.
- **Feet must counter-rotate.** With feet rigidly on the leg bone, the toe tilted
  into the ground by ~3 cm during the swing (caught by `MobWalkCheck`). The foot bones
  key the *negative* of the leg swing so feet stay flat.
- **Wide, low body parts are levers.** Body roll rotates about the forward axis, so
  anything sticking far out in ±X swings down by `x · sin(roll)` (the Cauliflower's
  low leaves dipped 5 cm underground with roll 6.5°). Keep wide attachments high,
  shorten them, or lower that mob's roll.

## Walk cycle

### Granola Mom humanoid extension

The final-batch script wraps the shared rig with seven extra bones: `head`,
`arm_a/b`, `forearm_a/b`, and `shin_a/b` (13 total including root).
`leg_a/b` are thighs; each foot is parented to its shin. Arms counter-swing
against the corresponding thighs, knees bend during recovery, and foot rotation
cancels the combined thigh/shin angle. Head and long hair share rigid weights.
The shortened tunic leaves knees clear. This is a custom skeleton exported with
a baked Legacy clip, not a Unity Humanoid/Avatar retargeting setup.
`MobWalkCheck` additionally checks required bones, opposing arm swing, and knee motion.
Run the final script in a fresh Blender background process; it replaces its active scene.

- **24 frames at 24 fps**, keyed 0-24. **Frame 24 equals frame 0** so the loop has no
  pop when it wraps (do not drop the duplicate end key).
- Legs swing in opposite phase; the lifting leg gets a small Z translation; the body
  bobs at twice the step rate and rolls/nods slightly.
- Per-mob tuning lives in the `WALK` dict:

| Mob | swing (deg) | lift | bob | roll (deg) | nod (deg) |
|---|---:|---:|---:|---:|---:|
| Apple | 26 | .045 | .022 | 5.0 | 2.0 |
| Carrot | 30 | .050 | .026 | 6.0 | 2.5 |
| Pear | 24 | .045 | .022 | 6.5 | 2.0 |
| Banana | 32 | .050 | .022 | 4.0 | 3.0 |
| Watermelon | 18 | .040 | .020 | 4.5 | 1.5 |

- Static poses only for the Banana: it was reworked from a mid-sprint pose with arms
  to a **standing pose with no arms**, matching the others, before rigging.

## Exporting from Blender

- Select the mesh **and** its armature, then `export_scene.gltf(...,
  export_format='GLB', use_selection=True, export_animations=True,
  export_skins=True, export_animation_mode='ACTIVE_ACTIONS')`.
- Use **`ACTIVE_ACTIONS`**. The default `ACTIONS` can drag in other actions in the
  file. Verified: each GLB contains exactly one clip.
- **Clear selection in every view layer before exporting.** The exporter unions
  selection across *all* view layers, and old build sessions leave strays selected
  in other scenes (we shipped a Cherry GLB containing the Watermelon this way).
  `export()` now deselects every object in every scene's every view layer and
  clears all active objects first.
- The exporter names the clip `Animation` regardless of the action name. That is fine,
  because `MobWalkAnimation` plays the first clip.
- The exporter's size optimisation drops constant channels to 2 keys. The moving
  channels keep all 25.
- Export to **both** `Assets/Resources/Snack/Mobs/` and `Assets/Models/Mobs/`.
- Sizes: ~40-95 KB per mob with the rig and clip.

## Unity side

- **Importer setting:** each rigged mob's `.glb.meta` must have
  `animationMethod: 1` (**Legacy**). The project default is `2` (Mecanim), which
  produces no playable `Animation` component. Both mirrors' metas were changed.
  glTFast sets the legacy clip's wrap mode to Loop.
- `MobVisual.Build` instantiates the model, calls `SnackModels.CenterOn`, then
  `MobWalkAnimation.Attach(m)`. Models without an `Animation` component are ignored.
- `MobWalkAnimation`:
  - measures the mob's **actual horizontal movement** each frame (position delta over
    `Time.deltaTime`), smooths it, and sets `state.speed = min(speed *
    CyclesPerUnit, MaxCyclesPerSecond)`;
  - so **slowed mobs walk slower, stunned mobs freeze mid-step**, and dashes/enrage
    are reflected automatically without reading `MobDef` stats;
  - starts each mob at a **random normalised time** so a wave does not march in lockstep;
  - `CyclesPerUnit = 1.2`, `MaxCyclesPerSecond = 4` are untuned starting values.
- `CenterOn` and the health-bar height use `Renderer.bounds`, which works with the
  `SkinnedMeshRenderer` (verified feet land on the ground at y = 0 and y = -400).

## Verification

Editor must be **closed** for batch mode.

```powershell
# compile
Unity.exe -batchmode -projectPath <project> -executeMethod CICompileCheck.EnsureBootScene -quit -logFile <log>

# rigged-mob check -> %TEMP%\opencode\mob_walk_check.txt
Unity.exe -batchmode -projectPath <project> -executeMethod MobWalkCheck.Verify -quit -logFile <log>

# Windows player (force the target; the active target may be WebGL)
$env:CI_BUILD_TARGET = "StandaloneWindows64"
Unity.exe -batchmode -projectPath <project> -executeMethod CICompileCheck.Build -quit -logFile <log>
```

`MobWalkCheck` builds each mob through `MobVisual.Build` at y = 0 and y = -400 and fails
if: there is no `Animation`, the clip is not legacy or does not loop,
`MobWalkAnimation` is missing, there is no `SkinnedMeshRenderer`, a leg bone is
missing, the legs barely move (< 8 deg) or swing the same way, the feet sink more than
2 cm below the ground, or the mob drifts more than 0.12 off centre. It bakes the skinned
mesh at 12 points through the cycle to measure real vertex positions.

**What this does not prove:** the playback speed logic (`Update` only runs in play
mode), how it looks in the arena, and remote-board behaviour. Those need a human
play-test.

## Blender MCP notes

- Each `execute_blender_code` call runs in a **fresh namespace**: re-`exec` the script
  before calling helpers such as `gallery` or `walk_sheet`.
- Code runs at module scope: use `print`, no top-level `return`.
- Look shader nodes up by **type**, never by name. Never hardcode enum identifiers.
- `render(write_still=True)` may return before the file is readable. Check the
  timestamp/size, and render to a **new filename** to avoid looking at a stale image.
- The MCP addon reported protocol 7 vs expected 11 (`uvx mcp-for-blender
  install-addon` to update). Everything used here worked on the old protocol.

## Doing the next mobs

1. Pick the mob from `MobRoster.md`; get a concept and **approval** (see above).
2. Add a `build_<mob>()` to a script modelled on `mobs_first5.py` (or extend it),
   using `feet()` so the leg/foot bones and pivots are recorded, and add a `WALK` entry.
3. Preview with `walk_sheet` (side profile at frames 6 and 18) before exporting.
4. Export to both folders, set both `.meta` files to `animationMethod: 1`, add the id
   to `MobWalkCheck.Mobs`, and run the verification above.
5. Commit **before** building: the player's version stamp is `HEAD`'s sha.

### Open items

- Gait speed (`CyclesPerUnit`) and per-mob stride are untuned; play-test them.
- The 29 other built mobs are still static. Migrating them means re-authoring or
  rigging each one.
- Boss gimmicks (Pumpkin regen, Durian enrage, Dragonfruit dash) have no animation
  hooks yet; `MobWalkAnimation` already speeds up with enrage/dash via movement.
- Only the leg-based walk is rigged: no idle, hit or death animations.
