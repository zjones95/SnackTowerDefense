# One-off: recentre the Coffee Mug on its cup body, scale it to 80%, bake the
# wooden base, and re-export both mirrors. The handle is built on +X and the
# front barrel on -Y, so join_asset() centred the silhouette instead of the
# cup; this shifts the body axis back onto the slot and trims the footprint.
# Module scope with print, no top-level return (Blender MCP rule).

import bpy
import importlib.util
from mathutils import Vector

ROOT = "C:/Users/Desktop/SnackTowerDefense"
WB_PATH = ROOT + "/tools/blender/wooden_base.py"
MODELS_DIR = ROOT + "/Assets/Models/Towers"
RES_DIR = ROOT + "/Assets/Resources/Snack/Towers"
LIFT = 0.18
SCALE = 0.8

_spec = importlib.util.spec_from_file_location("wooden_base", WB_PATH)
wb = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(wb)

for o in list(bpy.data.objects):
    try:
        bpy.data.objects.remove(o, do_unlink=True)
    except Exception:
        pass

ped, rim, _mats = wb.build_base(clear=True)
before = set(list(bpy.data.objects))
bpy.ops.import_scene.gltf(filepath=MODELS_DIR + "/CoffeeMug.glb")
arts = [o for o in bpy.data.objects if o not in before]

for o in arts:
    vs = o.data.vertices
    zs = [v.co.z for v in vs]
    zmin = min(zs); zmax = max(zs); h = zmax - zmin
    top = [v.co for v in vs if v.co.z > zmin + 0.85 * h]
    cx = sum(v.x for v in top) / len(top)
    cy = sum(v.y for v in top) / len(top)
    for v in vs:
        v.co.x = (v.co.x - cx) * SCALE
        v.co.y = (v.co.y - cy) * SCALE
        v.co.z = (v.co.z - zmin) * SCALE
    o.data.update()
    print("recentred body axis (%.4f, %.4f), scaled %.2f" % (cx, cy, SCALE))

for o in arts:
    o.location.z += LIFT
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

for o in arts:
    mw = o.matrix_world
    vs = [mw @ v.co for v in o.data.vertices]
    print("after bounds x %.4f..%.4f y %.4f..%.4f z %.4f..%.4f" % (
        min(v.x for v in vs), max(v.x for v in vs),
        min(v.y for v in vs), max(v.y for v in vs),
        min(v.z for v in vs), max(v.z for v in vs)))

bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.gltf(filepath=RES_DIR + "/CoffeeMug.glb", export_format="GLB", use_selection=True)
bpy.ops.export_scene.gltf(filepath=MODELS_DIR + "/CoffeeMug.glb", export_format="GLB", use_selection=True)
print("coffee mug base bake complete")
