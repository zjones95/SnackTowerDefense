---
name: Create 3D Asset
description: Create a low-poly, colorful game asset for Snack TD — asks asset type + look, shows a multi-angle concept for approval, then builds it in Blender and exports to Unity.
---

## When to use
When the user wants a new 3D game asset for Snack Tower Defense (tower, mob, projectile, prop, or custom). Follow the steps in order — do not skip the concept approval gate.

## Workflow

### 1. Ask asset type (required first step)
Use the `question` tool with multiple-choice options + custom allowed:

- `Tower` — `Assets/Resources/Snack/Towers/<Name>.glb`. Must match a `TowerType` in `Assets/Scripts/TowerCatalog.cs` (`SingleShot, Splash, Slow, Sniper, Chain, Pierce, Poison, Gold, FondueFountain, IceCreamTruck, BobaBlaster, PizzaOven`).
- `Mob` — `Assets/Resources/Snack/Mobs/<id>.glb` where `<id>` matches `MobDef.id` in `MobCatalog.cs` (see `docs/MobRoster.md`; no code needed for new mobs).
- `Projectile` — `Assets/Resources/Snack/Projectiles/<TowerName>.glb`.
- `Room prop / decor` — goes through `TDRoom.cs` / `TDBoardBuilder.cs` instead of `Snack/`; confirm placement with user.
- Custom (user types own answer).

Record the chosen type + target filename before continuing.

### 2. Ask what it should look like
Ask via `question` or plain prompt (keep it short):
- What snack / fruit / object is it? Key shapes, colors, personality (kid-fun, toy-like)?
- Any gameplay constraint (e.g. needs a barrel/muzzle pointing forward, needs a fixed base)?
- If Tower with a fixed stand: base object must be named `pedestal` (see step 4).

Do not generate anything until you have this description.

### 3. Concept image, multi-angle + feedback gate
1. Generate a **GenAI concept image FIRST**, before any Blender work:
   - Preferred: Muse Image via Meta Model API (`POST https://api.meta.ai/v1/images/generations`,
     `model: muse-image-1.0`, key from `HKCU:\Environment\META_MODEL_API_KEY` read at call time —
     never echo it). Prompt a single 3/4 (45deg) view on a plain background, playful,
     no text, `output_format: png`, save to `%TEMP%\opencode\<name>_genai_concept.png`, then
     `read` it so it is shown in chat AND open it for the user (e.g. `Start-Process` the PNG
     so it opens in the OS image viewer) before asking for approval.
     Iterate with `/v1/images/edits` on change requests.
   - Fallback (no API key): ask the user to supply a concept image path, or build a quick
     low-detail Blender clay blockout, frame with `view_selected`, Material shading,
     `scene.view_settings.view_transform = 'Standard'`, and render a single 3/4 (45deg) view
     with `bpy.ops.render.render(write_still=True)` to a PNG under
     `%TEMP%\opencode\` — then `read` the PNG AND open it for the user
     (e.g. `Start-Process`) before asking for approval. Viewport screenshots alone are not enough.
2. Always open the concept image(s) in the OS viewer before the approval gate, then
   ask for feedback with the `question` tool: `Approve` / `Request changes` (custom text). Iterate on the concept until approved.
3. Never proceed to step 4 on a rejected concept.

### 4. Build the model in Blender (only after approval)
Drive Blender through `tools["blender"]`. Rules from `AGENTS.md`:

- `get_addon_status()` then `get_scene_info()` first.
- Look shader nodes up by type, never by name (`next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")`). Never hardcode enum identifiers — read valid values first.
- `execute_blender_code` runs at module scope — use `print`, no top-level `return`.
- **Low-poly (hard requirement):** primitives only with low segments (e.g. cylinder 12–20 verts, sphere 16×8, cone 16–24), no Subdivision Surface, join into few objects, `transform_apply` BEFORE re-centring vertices (or the model lands offset). Count and report `faces/vertices` at the end via mesh `polygons`/`vertices` lengths.
- **Colorful and fun:** `use_nodes = True`, set colors on the Principled BSDF `Base Color` input (saturated kid-snack palette), `Roughness` ~0.4–0.6, `Metallic` ~0 unless metal. `material.diffuse_color` alone does nothing for renders.
- **Snack TD conventions:**
  - ~1 unit tall total, base on `z = 0`, centred in X/Y. Scale about the origin so the base stays on 0.
  - Mob/tower forward is Blender `-Y` (→ Unity `+Z`). Barrel/muzzle points `-Y`.
  - Fixed-stand towers: name the base object `pedestal` (anchors `SnackModels.CenterOn`, excluded from head-split aim rotation). Most snack towers have no `pedestal`/`rim` so the whole model rotates.
  - Preview renders: `scene.view_settings.view_transform = 'Standard'` (AgX desaturates).
- After changes: `get_viewport_screenshot()` + `get_scene_info()` to confirm look and placement.
- Save the authoring script to `tools/blender/<name>.py` (see `tools/blender/soda_cup_concept.py` pattern), then export with `bpy.ops.export_scene.gltf(filepath=…, export_format='GLB', use_selection=True)` to BOTH mirrors:
  - `Assets/Resources/Snack/<Type>s/<Name>.glb` (runtime)
  - `Assets/Models/<Type>s/<Name>.glb` (source mirror)
- Report final `faces / vertices` counts (per object + total) — required by this skill.

### 5. Verify + hand off
- Tell the user the export paths + face/vert totals.
- Suggest verification: batch compile (`CICompileCheck.EnsureBootScene`, editor closed) and `SnackPreview.Render` (tower/mob grid PNGs to `%TEMP%\opencode\`). Do not claim in-game behaviour works from a compile alone.
- Behavioural check needs a human play-test (agent cannot run play mode). Never build WebGL unless explicitly asked (use the `webgl-build` skill).
