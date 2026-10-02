# Batch: bake the approved wooden base into four of the v3 snack towers.
# Mirrors add_base_to_towers.py (which covers the original 12), reusing
# wooden_base.py via importlib. Per tower: fresh scene -> build_base(clear)
# -> import GLB from the Models mirror -> rename pedestal/rim collisions
# -> lift art +0.18 -> apply translations -> export to BOTH mirrors.
# CoffeeMug is NOT in this list: it also needs its cup body recentred and
# scaled, so fix_coffee_mug_base.py bakes its base instead (run that last).
# Module scope with print, no top-level return (Blender MCP rule).

import bpy
import importlib.util
import os

WB_PATH = "C:/Users/Desktop/SnackTowerDefense/tools/blender/wooden_base.py"
MODELS_DIR = "C:/Users/Desktop/SnackTowerDefense/Assets/Models/Towers"
RES_DIR = "C:/Users/Desktop/SnackTowerDefense/Assets/Resources/Snack/Towers"
LIFT = 0.18

TOWERS = ["HotSauce", "PopTartToaster", "CookieCrumbler", "SourFizz"]

_spec = importlib.util.spec_from_file_location("wooden_base", WB_PATH)
wb = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(wb)


def mesh_stats(objs):
    f = v = 0
    for o in objs:
        if o.type == "MESH" and o.data is not None:
            try:
                f += len(o.data.polygons)
                v += len(o.data.vertices)
            except Exception:
                pass
    return f, v


def bottom_z(objs):
    zmin = None
    for o in objs:
        if o.type != "MESH" or o.data is None:
            continue
        try:
            mw = o.matrix_world
            for vv in o.data.vertices:
                z = (mw @ vv.co).z
                if zmin is None or z < zmin:
                    zmin = z
        except Exception:
            continue
    return zmin


def clear_all_objects():
    for o in list(bpy.data.objects):
        try:
            bpy.data.objects.remove(o, do_unlink=True)
        except Exception:
            pass


def apply_location(obj):
    try:
        bpy.ops.object.select_all(action="DESELECT")
    except Exception:
        pass
    try:
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


def process_one(tower):
    clear_all_objects()
    ped, rim, _mats = wb.build_base(clear=True)
    before = set(list(bpy.data.objects))

    src = os.path.join(MODELS_DIR, tower + ".glb")
    bpy.ops.import_scene.gltf(filepath=src)
    imported = [o for o in bpy.data.objects if o not in before]
    imported_set = set(imported)

    imp_f, imp_v = mesh_stats(imported)

    renames = []
    had_pedestal = False
    for o in imported:
        low = o.name.lower()
        if ("pedestal" in low) or ("rim" in low):
            if "pedestal" in low:
                had_pedestal = True
            old = o.name
            o.name = "%s_%s" % (tower, old)
            renames.append("%s -> %s" % (old, o.name))

    bpy.context.view_layer.update()
    for o in imported:
        if o.parent is None or o.parent not in imported_set:
            try:
                o.location.z += LIFT
            except Exception as e:
                print("LIFT_WARN %s: %s" % (o.name, e))
    bpy.context.view_layer.update()
    for o in imported:
        apply_location(o)
    bpy.context.view_layer.update()

    bz = bottom_z(imported)
    fin_f, fin_v = mesh_stats(list(bpy.data.objects))

    try:
        bpy.context.scene.view_settings.view_transform = "Standard"
    except Exception as e:
        print("XFORM_FALLBACK %s %s" % (tower, e))

    bpy.ops.object.select_all(action="SELECT")
    dst_res = os.path.join(RES_DIR, tower + ".glb")
    dst_mod = os.path.join(MODELS_DIR, tower + ".glb")
    bpy.ops.export_scene.gltf(filepath=dst_res, export_format="GLB", use_selection=True)
    bpy.ops.export_scene.gltf(filepath=dst_mod, export_format="GLB", use_selection=True)

    print("TOWER %s imported faces=%d verts=%d | final faces=%d verts=%d | "
          "bottom_z=%.4f | had_pedestal=%s | renames=%s | n_imported=%d" % (
              tower, imp_f, imp_v, fin_f, fin_v,
              bz if bz is not None else -999.0, had_pedestal,
              renames if renames else [], len(imported)))


for _t in TOWERS:
    process_one(_t)
print("NEW base bake complete (CoffeeMug via fix_coffee_mug_base.py)")
