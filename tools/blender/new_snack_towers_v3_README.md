# Approved v3 snack towers

Issue: #61. Art-only deliverable; no catalog, combat, projectile, or gameplay changes.

Authoring: `new_snack_towers_v3.py` (Blender 5.2.2); source:
`source/new_snack_towers_v3.blend`. Existing startup scene preserved separately in
the blend. The authoring scene displays models in a spaced lineup; **exports were
made with each object's location reset to zero**. Do not export the lineup offsets.
Build in a fresh scene/session without objects bearing these exact asset names.

All five are flat-shaded, unrigged static meshes, 1 meter tall, grounded at z=0,
centered in X/Y, with baked rotation/scale and a UV map. Forward is Blender -Y.
Material colors are Principled PBR inputs, not viewport-only colors. There are no
external texture dependencies. No faces, steam particles or floating crumbs.
Cookie Crumbler has three large cookies nested inside its open hopper, two beside
each other and a third behind, not hovering above it. Front chute crumbs are static
and supported by its floor.

| Asset / sole mesh object | Vertices | Faces | Triangles | Material primitives |
|---|---:|---:|---:|---:|
| HotSauce | 510 | 404 | 968 | 6 |
| CoffeeMug | 624 | 554 | 1196 | 5 |
| PopTartToaster | 1712 | 1840 | 3136 | 11 |
| CookieCrumbler | 2278 | 2336 | 4296 | 7 |
| SourFizz | 1038 | 958 | 1916 | 7 |
| Total | 6162 | 6092 | 11512 | 36 |

The vertex/face counts are Blender authoring counts. GLB splits vertices for flat
normals and UV seams; material primitives become submeshes/draw calls on import.

Exports: identical `<Asset>.glb` files in both `Assets/Resources/Snack/Towers/` and
`Assets/Models/Towers/`, made with Blender MCP `export_scene` (named object only).

Verified:
- Blender renders and viewport screenshot, scene object inventory.
- No loose vertices, identity mesh scale/rotation.
- `python tools/blender/verify_new_snack_towers_v3.py`: GLB 2.0 structure,
  byte-identical mirrors, one mesh/node per file, normals/UVs, triangle counts,
  centered geometry and Y-up bounds [0,1], origin translation, no scene props.
- `git diff --check` passed (an unrelated AGENTS.md line-ending warning remains).

Previews were opened individually and as a contact sheet. Reassemble/open using
`preview_new_snack_towers_v3.ps1`. PNGs are in
`C:\Users\Desktop\AppData\Local\Temp\opencode\`:
`tower_<Asset>_model_v3.png`, `new_towers_model_contact_sheet_v3.png`.

Not verified: Unity import/render, gameplay or player build. Use the Unity CLI for
that next stage. No gameplay claims follow from the Blender/GLB checks.
