# One-off: shell tuning for two v3 towers after the base bake.
#   HotSauce      - shift the bottle body onto the base centre (nozzle skews the bbox)
#   PopTartToaster- recentre on X and scale the art to 70% about the base top
# Operates on the base-baked GLBs in Assets/Models/Towers (art node only, so the
# shared pedestal/rim are untouched) and re-exports both mirrors.
# Already applied; re-running would double-apply. Module scope, no top-level return.

import bpy

MOD = "C:/Users/Desktop/SnackTowerDefense/Assets/Models/Towers"
RES = "C:/Users/Desktop/SnackTowerDefense/Assets/Resources/Snack/Towers"


def load(name):
    for o in list(bpy.data.objects):
        try:
            bpy.data.objects.remove(o, do_unlink=True)
        except Exception:
            pass
    bpy.ops.import_scene.gltf(filepath=MOD + "/" + name + ".glb")
    return bpy.data.objects.get(name)


def export(name):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.gltf(filepath=RES + "/" + name + ".glb", export_format="GLB", use_selection=True)
    bpy.ops.export_scene.gltf(filepath=MOD + "/" + name + ".glb", export_format="GLB", use_selection=True)


art = load("HotSauce")
for v in art.data.vertices:
    v.co.y -= 0.095
art.data.update()
export("HotSauce")
print("HotSauce recentred -0.095 Y")

art = load("PopTartToaster")
BASE_TOP = 0.18
S = 0.7
for v in art.data.vertices:
    v.co.x = (v.co.x + 0.050) * S
    v.co.y = (v.co.y + 0.013) * S
    v.co.z = BASE_TOP + (v.co.z - BASE_TOP) * S
art.data.update()
export("PopTartToaster")
print("Toaster recentred + scaled 0.7")
