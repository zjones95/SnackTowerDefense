# Batch: bake approved wooden base into all 12 catalog tower models.
# Reuses wooden_base.py via importlib (no duplicated texture code).
# Per tower: fresh scene -> build_base(clear) -> import GLB from Models mirror
# -> rename pedestal/rim collisions -> lift art +0.18 -> apply translations
# -> export (selection) to BOTH Resources + Models mirrors.
# Module scope with print, no top-level return (Blender MCP rule).

import bpy
import importlib.util
import os

WB_PATH = "C:/Users/Desktop/SnackTowerDefense/tools/blender/wooden_base.py"
MODELS_DIR = "C:/Users/Desktop/SnackTowerDefense/Assets/Models/Towers"
RES_DIR = "C:/Users/Desktop/SnackTowerDefense/Assets/Resources/Snack/Towers"
LIFT = 0.18

TOWERS = ["Popcorn", "Soda", "Gum", "SourStraw", "SourBelt", "Skewer",
          "SpicyChips", "Gold", "Fondue", "IceCreamTruck", "Boba", "PizzaOven"]

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
    # Fresh scene, then base (clear=True also wipes MESH/EMPTY; we pre-wipe ALL types).
    clear_all_objects()
    ped, rim, _mats = wb.build_base(clear=True)
    base_ids = set((ped, rim))
    before = set(list(bpy.data.objects))

    src = os.path.join(MODELS_DIR, tower + ".glb")
    bpy.ops.import_scene.gltf(filepath=src)
    imported = [o for o in bpy.data.objects if o not in before]
    imported_set = set(imported)

    imp_f, imp_v = mesh_stats(imported)

    # Collision renames: base owns `pedestal`+`rim`; imported hits get tower prefix.
    renames = []
    had_pedestal = False
    for o in imported:
        low = o.name.lower()
        hit = ("pedestal" in low) or ("rim" in low)
        if hit:
            if "pedestal" in low:
                had_pedestal = True
            old = o.name
            o.name = "%s_%s" % (tower, old)
            renames.append("%s -> %s" % (old, o.name))
    # Recompute lows after rename for safety (no further action needed).

    # Lift art so it sits on base top (base occupies 0 -> 0.18). Move only roots
    # to avoid double-offsetting parented children; children ride along.
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
    return {"tower": tower, "imp_f": imp_f, "imp_v": imp_v,
            "fin_f": fin_f, "fin_v": fin_v, "bottom": bz,
            "renames": renames, "had_pedestal": had_pedestal}


def main():
    total_imp_f = total_imp_v = total_fin_f = total_fin_v = 0
    for t in TOWERS:
        r = process_one(t)
        total_imp_f += r["imp_f"]
        total_imp_v += r["imp_v"]
        total_fin_f += r["fin_f"]
        total_fin_v += r["fin_v"]
    print("TOTAL12 imported faces=%d verts=%d | final faces=%d verts=%d" % (
        total_imp_f, total_imp_v, total_fin_f, total_fin_v))


main()
