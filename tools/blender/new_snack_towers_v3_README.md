# Approved v3 snack towers

Issue: #61. Originally an art-only deliverable; this folder now also carries the
follow-up integration fixes — the shared wooden tower base and the Coffee Mug's
size/centring.

Authoring: `new_snack_towers_v3.py` (Blender 5.2.2); source:
`source/new_snack_towers_v3.blend`. Existing startup scene preserved separately in
the blend. The authoring scene displays models in a spaced lineup; **exports were
made with each object's location reset to zero**. Do not export the lineup offsets.
Build in a fresh scene/session without objects bearing these exact asset names.

The art is flat-shaded, unrigged static meshes with baked rotation/scale and a UV
map. Forward is Blender -Y. Material colors are Principled PBR inputs, not
viewport-only colors. There are no external texture dependencies. No faces, steam
particles or floating crumbs. Cookie Crumbler has three large cookies nested inside
its open hopper, two beside each other and a third behind, not hovering above it.
Front chute crumbs are static and supported by its floor.

## Wooden base + Coffee Mug fix

Each shipped GLB carries the same `pedestal` + `rim` wooden base as the original 12
towers, baked on with `add_base_new_towers.py` (reusing `wooden_base.py`). The art
is lifted onto the base top (z = 0.18), so a full tower is ~1.18 units tall.
Pipeline from art-only GLBs:

1. `new_snack_towers_v3.py` — art-only GLBs.
2. `add_base_new_towers.py` — base for HotSauce, PopTartToaster, CookieCrumbler, SourFizz.
3. `fix_coffee_mug_base.py` — the mug is built with its handle on +X and front
   barrel on -Y, so `join_asset()` had centred the silhouette rather than the cup.
   This recentres the cup body on the slot, scales the mug to 80%, and bakes its base.

Follow-up shell tuning (edited the based GLBs in place, keeping the shared base):

- **HotSauce** — the black elbow/nozzle extends forward, so `join_asset()` left the
  bottle body off the base centre. The body axis (bottom-slice centroid) was shifted
  back by 0.095 on Y.
- **PopTartToaster** — the art is scaled to 70% about the base top (0.18) and recentred
  0.05 on X, so the toaster no longer overhangs its base. Full height is now ~0.88.

Art authoring counts (before the base bake; the base adds ~66 faces per tower):

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
`Assets/Models/Towers/`, made with Blender MCP exports (base + art selected).

Verified:
- Blender renders and viewport screenshot, scene object inventory.
- No loose vertices, identity mesh scale/rotation.
- `python tools/blender/verify_new_snack_towers_v3.py`: GLB 2.0 structure, byte-identical
  mirrors, art + `pedestal` + `rim` nodes, normals/UVs, triangle counts, base centred on
  X/Z, grounded at y = 0, total height 0.88 (toaster) / 0.98 (mug) / 1.18 (rest).
- `git diff --check` passed.

Not verified by these Blender/GLB checks alone: Unity import/render and gameplay. The
Unity CLI check `NewSnackTowerCheck.Verify` imports the models and renders them
(`NewSnackTowerCheck.RenderNewTowers`); run it for that stage.
