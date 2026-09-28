# Rebake the 6 newly-authored tower models (Popcorn, Soda, SourStraw, SourBelt,
# Skewer, SpicyChips) onto the shared wooden pedestal base, and export GLB to
# BOTH mirrors (runtime Resources + source Models).
#
# The art lives in the .blend studio scenes (each .blend contains several
# accumulated scenes, but the correct studio scene is marked active). This
# script opens each .blend, keeps the art meshes, builds the wooden base in
# place (wooden_base.build_base, z 0 -> 0.18), lifts the art +0.18 so it sits
# on the base, applies locations, then exports every mesh in the active scene.
#
# Mirrors the logic of tools/blender/add_base_to_towers.py, but sources the art
# straight from the saved .blend (no intermediate art-only GLB, no double-base).
#
# Run headless:
#   "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" \
#       --background --python tools/blender/rebake_new_towers.py

import bpy
import importlib.util
import os

WB = "C:/Users/zach-laptop/SnackTowerDefense/tools/blender/wooden_base.py"
MOD = "C:/Users/zach-laptop/SnackTowerDefense/Assets/Models/Towers"
RES = "C:/Users/zach-laptop/SnackTowerDefense/Assets/Resources/Snack/Towers"
LIFT = 0.18

BLENDS = {
    "Popcorn": "C:/Users/zach-laptop/Desktop/popcorn_bucket_tower.blend",
    "Soda": "C:/Users/zach-laptop/Desktop/soda_cup_tower.blend",
    "SourStraw": "C:/Users/zach-laptop/Desktop/sour_straw_tower.blend",
    "SourBelt": "C:/Users/zach-laptop/Desktop/sour_belt_tower.blend",
    "Skewer": "C:/Users/zach-laptop/Desktop/skewer_tower.blend",
    "SpicyChips": "C:/Users/zach-laptop/Desktop/spicy_chips_tower.blend",
}

_spec = importlib.util.spec_from_file_location("wooden_base", WB)
wb = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(wb)


def apply_location(obj):
    try:
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
    except Exception as e:
        print("APPLY_WARN %s: %s" % (obj.name, e))
    finally:
        try:
            obj.select_set(False)
        except Exception:
            pass


def bbox(objects):
    from mathutils import Vector
    mn = [1e18, 1e18, 1e18]
    mx = [-1e18, -1e18, -1e18]
    for o in objects:
        mw = o.matrix_world
        for c in o.bound_box:
            w = mw @ Vector(c)
            for i in range(3):
                mn[i] = min(mn[i], w[i])
                mx[i] = max(mx[i], w[i])
    return mn, mx


def process(tower, blend):
    bpy.ops.wm.open_mainfile(filepath=blend)
    scene = bpy.context.scene
    if scene.name not in ("Popcorn Studio", "Soda Cup Studio", "Sour Straw Studio",
                          "Sour Belt Studio", "Skewer Studio", "Spicy Chips Studio"):
        print("WARN %s: unexpected active scene %r" % (tower, scene.name))

    # Art = every MESH in the active scene (studios have no base, no pedestal/rim).
    art = [o for o in scene.objects if o.type == "MESH"]
    art_set = set(art)
    mn0, mx0 = bbox(art)

    # Build the wooden base in-place (does not clear -> keeps art).
    ped, rim, _mats = wb.build_base(clear=False)
    bpy.context.view_layer.update()

    # Lift root art objects so the art sits on the base top (0 -> 0.18).
    for o in art:
        if o.parent is None or o.parent not in art_set:
            o.location.z += LIFT
    bpy.context.view_layer.update()
    for o in art:
        apply_location(o)
    bpy.context.view_layer.update()

    # Select every mesh in the ACTIVE scene (art + base) and export to both mirrors.
    bpy.ops.object.select_all(action="DESELECT")
    all_mesh = [o for o in scene.objects if o.type == "MESH"]
    for o in all_mesh:
        o.select_set(True)
    bpy.context.view_layer.update()

    mn, mx = bbox(all_mesh)
    os.makedirs(RES, exist_ok=True)
    os.makedirs(MOD, exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=os.path.join(RES, tower + ".glb"),
                              export_format="GLB", use_selection=True)
    bpy.ops.export_scene.gltf(filepath=os.path.join(MOD, tower + ".glb"),
                              export_format="GLB", use_selection=True)

    print("TOWER %-11s art_bottom=%.4f art_top=%.4f | final_bottom=%.4f final_top=%.4f "
          "n_mesh=%d" % (tower, mn0[2], mx0[2], mn[2], mx[2], len(all_mesh)))


def main():
    for tower, blend in BLENDS.items():
        process(tower, blend)


main()
