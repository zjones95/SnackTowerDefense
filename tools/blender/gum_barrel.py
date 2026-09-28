# Slow tower (gumball machine) — restyled Gum.glb art.
# Concept: %TEMP%/opencode/gum_restyle_genai_concept.png
# (classic store gumball machine: round CLEAR glass head with a white tint,
# red TAPERED CYLINDRICAL body wider at the base, red/blue/green/purple/orange
# gumballs, chrome lid, and a VERY SHORT dark barrel at the lower-front
# dispenser chute. No coin slot, no accents.)
#
# Model machine only — base via shared script tools/blender/wooden_base.py
# (build_base via importlib; base occupies z 0->0.18, objects `pedestal`+`rim`).
#
# Layout (Blender units = meters, -Y = forward):
#   base:  z 0 -> 0.18 (pedestal + rim from wooden_base)
#   Body:  red tapered cylinder (frustum), bottom r=0.30 -> top r=0.20,
#          height 0.35, bottom z=0.18 (top z=0.53)
#   Globe: clear glass sphere r=0.30, white tint (alpha 0.18), center z=0.70
#   Gumballs: 14 x r=0.075, colors red/blue/green/purple/orange
#   Lid:   chrome disc + knob on globe top (~z 1.00)
#   Barrel: SHORT dark-steel cylinder, len 0.16, r 0.07, pointing FORWARD (-Y)
#           from the NECK of the body (upper body, z~0.47, just under the glass),
#           open muzzle (near-black recessed bore disc, no gumball).
# Run via the Blender MCP (execute_blender_code) or headless:
#   "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" \
#       --background --python tools\blender\gum_barrel.py

import bpy
import math
import os
import random
import importlib.util

BASE_SCRIPT = "C:/Users/zach-laptop/SnackTowerDefense/tools/blender/wooden_base.py"
OUT_RES = "C:/Users/zach-laptop/SnackTowerDefense/Assets/Resources/Snack/Towers/Gum.glb"
OUT_MOD = "C:/Users/zach-laptop/SnackTowerDefense/Assets/Models/Towers/Gum.glb"

UP = "Build the Slow tower (gumball machine) in Blender for SnackTowerDefense (Windows), replacing Gum.glb art."


def mat_flat(name, rgb, metallic=0.0, rough=0.5, alpha=None):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if b is not None:
        try:
            b.inputs['Base Color'].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
        except Exception:
            pass
        for k, v in (('Roughness', rough), ('Metallic', metallic)):
            try:
                b.inputs[k].default_value = v
            except Exception:
                pass
        if alpha is not None:
            try:
                b.inputs['Alpha'].default_value = float(alpha)
            except Exception:
                pass
    if alpha is not None and alpha < 1.0:
        # Unity built-in pipeline: glTF exports Alpha < 1 as transparent.
        # Do NOT rely on Transmission — it will not survive to Unity.
        try:
            m.blend_method = 'BLEND'
        except Exception:
            pass
        try:
            m.show_transparent_back = False
        except Exception:
            pass
    else:
        try:
            m.blend_method = 'OPAQUE'
        except Exception:
            pass
    return m


def apply_scale(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)


def build_art():
    try:
        bpy.context.scene.view_settings.view_transform = 'Standard'
    except Exception as e:
        print("XFORM_FALLBACK", e)

    # --- materials ---
    red_mat = mat_flat('GumRed', (0.78, 0.06, 0.08), metallic=0.10, rough=0.35)
    steel_mat = mat_flat('GumSteel', (0.16, 0.16, 0.18), metallic=0.6, rough=0.45)
    chrome_mat = mat_flat('GumChrome', (0.85, 0.86, 0.88), metallic=0.85, rough=0.3)
    bore_mat = mat_flat('GumBore', (0.02, 0.02, 0.02), metallic=0.0, rough=0.9)
    # Clear glass, soft white tint, NOT frosted: low roughness, modest alpha.
    glass_mat = mat_flat('GumGlass', (0.93, 0.96, 1.0), metallic=0.0, rough=0.05, alpha=0.18)
    gum_mats = [
        mat_flat('GumBall_Red', (0.90, 0.08, 0.10), metallic=0.0, rough=0.25),
        mat_flat('GumBall_Blue', (0.10, 0.45, 0.95), metallic=0.0, rough=0.25),
        mat_flat('GumBall_Green', (0.10, 0.75, 0.15), metallic=0.0, rough=0.25),
        mat_flat('GumBall_Purple', (0.55, 0.20, 0.80), metallic=0.0, rough=0.25),
        mat_flat('GumBall_Orange', (0.95, 0.55, 0.10), metallic=0.0, rough=0.25),
    ]

    art = []

    # --- Body: tapered red cylinder (frustum), wider at the base ---
    BOT_R, TOP_R, BH = 0.30, 0.20, 0.35
    Z0 = 0.18
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=24, radius=BOT_R, depth=BH, location=(0, 0, Z0 + BH / 2))
    body = bpy.context.active_object
    body.name = 'Body'
    apply_scale(body)
    # taper: interpolate radius from BOT_R (bottom, t=0) to TOP_R (top, t=1)
    me = body.data
    half = BH / 2.0
    for v in me.vertices:
        t = (v.co.z + half) / BH
        s = 1.0 - t + t * (TOP_R / BOT_R)
        v.co.x *= s
        v.co.y *= s
    body.data.materials.clear()
    body.data.materials.append(red_mat)
    if len(body.data.uv_layers) == 0:
        body.data.uv_layers.new(name="UVMap")
    art.append(body)

    # --- Globe: clear glass sphere r=0.30, white tint, center z=0.70 ---
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=16, ring_count=10, radius=0.30, location=(0, 0, 0.70))
    globe = bpy.context.active_object
    globe.name = 'Globe'
    apply_scale(globe)
    globe.data.materials.clear()
    globe.data.materials.append(glass_mat)
    if len(globe.data.uv_layers) == 0:
        globe.data.uv_layers.new(name="UVMap")
    art.append(globe)

    # --- Gumballs: 14 x r=0.075, red/blue/green/purple/orange, packed inside ---
    rng = random.Random(7)
    palette = [0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3]
    placed = []
    R_IN = 0.30 - 0.075 - 0.01  # keep fully inside glass
    tries = 0
    while len(placed) < 14 and tries < 2000:
        tries += 1
        x = rng.uniform(-R_IN, R_IN)
        y = rng.uniform(-R_IN, R_IN)
        z = rng.uniform(-R_IN, R_IN)
        if math.sqrt(x * x + y * y + z * z) > R_IN:
            continue
        ok = all(math.sqrt((x - px) ** 2 + (y - py) ** 2 + (z - pz) ** 2) > 0.145
                 for (px, py, pz) in placed)
        if ok:
            placed.append((x, y, z))
    # fallback: fill remaining on a grid if packing failed
    while len(placed) < 14:
        placed.append((0.0, 0.0, 0.0))
    for i, (px, py, pz) in enumerate(placed):
        bpy.ops.mesh.primitive_uv_sphere_add(
            segments=8, ring_count=5, radius=0.075,
            location=(px, py, 0.70 + pz))
        g = bpy.context.active_object
        g.name = 'Gumball_%02d' % (i + 1)
        apply_scale(g)
        g.data.materials.clear()
        g.data.materials.append(gum_mats[palette[i] % 5])
        if len(g.data.uv_layers) == 0:
            g.data.uv_layers.new(name="UVMap")
        art.append(g)

    # --- Lid: chrome disc + knob on globe top ---
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=20, radius=0.19, depth=0.045, location=(0, 0, 1.005))
    lid = bpy.context.active_object
    lid.name = 'Lid'
    apply_scale(lid)
    lid.data.materials.clear()
    lid.data.materials.append(chrome_mat)
    if len(lid.data.uv_layers) == 0:
        lid.data.uv_layers.new(name="UVMap")
    art.append(lid)

    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=12, ring_count=6, radius=0.05, location=(0, 0, 1.055))
    kn = bpy.context.active_object
    kn.name = 'Knob'
    kn.scale = (1.0, 1.0, 0.7)
    apply_scale(kn)
    kn.data.materials.clear()
    kn.data.materials.append(chrome_mat)
    if len(kn.data.uv_layers) == 0:
        kn.data.uv_layers.new(name="UVMap")
    art.append(kn)

    # --- Barrel: SHORT dark-steel cylinder at the neck (upper body) ---
    # tube: len 0.16, r 0.07, center y=-0.27, z=0.47 ; muzzle ring joined in
    BLEN, BR = 0.16, 0.07
    BC_Y, BC_Z = -0.27, 0.47
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=16, radius=BR, depth=BLEN, location=(0, BC_Y, BC_Z))
    tube = bpy.context.active_object
    tube.name = 'Barrel_tube'
    tube.rotation_euler = (math.pi / 2, 0, 0)  # +Z -> -Y (forward)
    apply_scale(tube)
    # muzzle ring flange at the muzzle end
    MUZ_Y = BC_Y - BLEN / 2
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=16, radius=0.086, depth=0.03, location=(0, MUZ_Y + 0.004, BC_Z))
    ring = bpy.context.active_object
    ring.name = 'Barrel_ring'
    ring.rotation_euler = (math.pi / 2, 0, 0)
    apply_scale(ring)
    bpy.ops.object.select_all(action='DESELECT')
    tube.select_set(True)
    ring.select_set(True)
    bpy.context.view_layer.objects.active = tube
    bpy.ops.object.join()
    barrel = bpy.context.active_object
    barrel.name = 'Barrel'
    barrel.data.materials.clear()
    barrel.data.materials.append(steel_mat)
    if len(barrel.data.uv_layers) == 0:
        barrel.data.uv_layers.new(name="UVMap")
    art.append(barrel)

    # --- MuzzleBore: recessed near-black disc, empty opening, NO gumball ---
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=16, radius=0.056, depth=0.012,
        location=(0, MUZ_Y - 0.004, BC_Z))
    bore = bpy.context.active_object
    bore.name = 'MuzzleBore'
    bore.rotation_euler = (math.pi / 2, 0, 0)
    apply_scale(bore)
    bore.data.materials.clear()
    bore.data.materials.append(bore_mat)
    if len(bore.data.uv_layers) == 0:
        bore.data.uv_layers.new(name="UVMap")
    art.append(bore)

    bpy.context.view_layer.update()
    return art


def select_all():
    bpy.ops.object.select_all(action='SELECT')


def export_all(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    select_all()
    bpy.ops.export_scene.gltf(
        filepath=path, export_format='GLB', use_selection=True)


def report(art):
    tf = tv = 0
    for o in art:
        f, v = len(o.data.polygons), len(o.data.vertices)
        tf += f
        tv += v
        print("GUM %s faces=%d verts=%d" % (o.name, f, v))
    print("GUM ART faces=%d verts=%d (excl. base pedestal/rim)" % (tf, tv))
    for o in list(bpy.data.objects):
        if o.type == 'MESH' and o.name in ('pedestal', 'rim'):
            print("GUM BASE %s faces=%d verts=%d" % (
                o.name, len(o.data.polygons), len(o.data.vertices)))
    print("GUM glass: Principled Alpha=0.18 white tint, clear (not frosted), blend_method=BLEND")
    print("GUM muzzle: -Y forward (Blender), at the neck (upper body), empty bore, NO gumball, NO coin slot")


# Execute at module scope (Blender MCP / headless --python convention: no top-level return).
_spec = importlib.util.spec_from_file_location("wooden_base", BASE_SCRIPT)
_wb = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_wb)
_ped, _rim, _mats = _wb.build_base(clear=True)
_art = build_art()
export_all(OUT_RES)
export_all(OUT_MOD)
_wb.report(_ped, _rim)
report(_art)
