# Slow tower (gumball machine) — replaces Gum.glb art.
# Concept: %TEMP%/opencode/slow_new_genai_concept_v4.png
# (red body, glass globe of colorful gumballs, chrome lid, SHORT smooth
# dark-steel open barrel with empty muzzle, NO muzzle gumball, NO gauge).
# Model machine only — base via shared script tools/blender/wooden_base.py
# (build_base via importlib; base occupies z 0->0.18).
#
# Layout (Blender units = meters, -Y = forward):
#   base: z 0 -> 0.18 (pedestal + rim from wooden_base)
#   Body: red tapered box, ~0.45 wide, ~0.35 tall, bottom z=0.18
#   Globe: glass sphere r=0.30, center z=0.70
#   Gumballs: 14 x r=0.075 spheres packed inside globe
#   Lid: chrome disc + knob on globe top (~z 1.00)
#   Barrel: SHORT smooth dark-steel cylinder, len 0.22, r 0.075,
#           pointing FORWARD (-Y) from body front at z~0.45, open muzzle
#           (muzzle ring joined into Barrel, near-black recessed bore disc).
# Run through the Blender MCP (execute_blender_code) or Scripting tab.

import bpy
import math
import os
import random
import importlib.util

BASE_SCRIPT = "C:/Users/Desktop/SnackTowerDefense/tools/blender/wooden_base.py"
OUT_RES = "C:/Users/Desktop/SnackTowerDefense/Assets/Resources/Snack/Towers/Gum.glb"
OUT_MOD = "C:/Users/Desktop/SnackTowerDefense/Assets/Models/Towers/Gum.glb"

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
    red_mat = mat_flat('GumRed', (0.80, 0.10, 0.12), metallic=0.15, rough=0.4)
    dark_mat = mat_flat('GumDark', (0.08, 0.08, 0.09), metallic=0.3, rough=0.6)
    steel_mat = mat_flat('GumSteel', (0.25, 0.25, 0.28), metallic=0.6, rough=0.45)
    chrome_mat = mat_flat('GumChrome', (0.85, 0.86, 0.88), metallic=0.85, rough=0.3)
    bore_mat = mat_flat('GumBore', (0.02, 0.02, 0.02), metallic=0.0, rough=0.9)
    glass_mat = mat_flat('GumGlass', (0.85, 0.95, 1.0), metallic=0.0, rough=0.05, alpha=0.22)
    gum_mats = [
        mat_flat('GumBall_Red', (0.90, 0.08, 0.10), metallic=0.0, rough=0.25),
        mat_flat('GumBall_Yellow', (0.95, 0.80, 0.08), metallic=0.0, rough=0.25),
        mat_flat('GumBall_Green', (0.10, 0.75, 0.15), metallic=0.0, rough=0.25),
        mat_flat('GumBall_Blue', (0.10, 0.45, 0.95), metallic=0.0, rough=0.25),
    ]

    art = []

    # --- Body: tapered red box, bottom z=0.18, ~0.45 wide, ~0.35 tall ---
    W, H = 0.45, 0.35
    Z0 = 0.18
    bpy.ops.mesh.primitive_cube_add(size=W, location=(0, 0, Z0 + H / 2))
    body = bpy.context.active_object
    body.name = 'Body'
    # squash Z: default cube is W x W x W -> scale Z to H
    body.scale = (1.0, 1.0, H / W)
    apply_scale(body)
    # taper: pull top verts inward (toy-like bevel look), then re-apply
    me = body.data
    for v in me.vertices:
        if v.co.z > 0:
            v.co.x *= 0.88
            v.co.y *= 0.88
    body.data.materials.clear()
    body.data.materials.append(red_mat)
    if len(body.data.uv_layers) == 0:
        body.data.uv_layers.new(name="UVMap")
    art.append(body)

    # --- CoinSlot: small dark box + tiny knob on front (-Y), joined ---
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -W / 2 - 0.008, 0.47))
    slot = bpy.context.active_object
    slot.name = 'CoinSlot_box'
    slot.scale = (0.12, 0.02, 0.07)
    apply_scale(slot)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=12, radius=0.018, depth=0.03, location=(0.09, -W / 2 - 0.02, 0.50))
    knob = bpy.context.active_object
    knob.name = 'CoinSlot_knob'
    knob.rotation_euler = (math.pi / 2, 0, 0)
    apply_scale(knob)
    bpy.ops.object.select_all(action='DESELECT')
    slot.select_set(True)
    knob.select_set(True)
    bpy.context.view_layer.objects.active = slot
    bpy.ops.object.join()
    coinslot = bpy.context.active_object
    coinslot.name = 'CoinSlot'
    coinslot.data.materials.clear()
    coinslot.data.materials.append(dark_mat)
    if len(coinslot.data.uv_layers) == 0:
        coinslot.data.uv_layers.new(name="UVMap")
    art.append(coinslot)

    # --- Globe: clear glass sphere r=0.30, center z=0.70 ---
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

    # --- Gumballs: 14 x r=0.075 packed inside globe ---
    rng = random.Random(7)
    palette = [0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3, 0, 1]
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
        g.data.materials.append(gum_mats[palette[i] % 4])
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

    # --- Barrel: SHORT smooth dark-steel cylinder, -Y forward, z~0.45 ---
    # tube: len 0.22, r 0.075, center y=-0.335 ; muzzle ring flange joined in
    BLEN, BR = 0.22, 0.075
    BC_Y, BC_Z = -(W / 2) - BLEN / 2 + 0.02, 0.45
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=16, radius=BR, depth=BLEN, location=(0, BC_Y, BC_Z))
    tube = bpy.context.active_object
    tube.name = 'Barrel_tube'
    tube.rotation_euler = (math.pi / 2, 0, 0)  # +Z -> -Y (forward)
    apply_scale(tube)
    # muzzle ring flange at the muzzle end
    MUZ_Y = BC_Y - BLEN / 2
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=16, radius=0.092, depth=0.035, location=(0, MUZ_Y + 0.005, BC_Z))
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
        vertices=16, radius=0.058, depth=0.012,
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
        print("GUM %s faces=%d verts=%d loc=%s" % (o.name, f, v, list(o.location)))
    print("GUM ART faces=%d verts=%d (excl. base pedestal/rim)" % (tf, tv))
    for o in list(bpy.data.objects):
        if o.type == 'MESH' and o.name in ('pedestal', 'rim'):
            print("GUM BASE %s faces=%d verts=%d" % (
                o.name, len(o.data.polygons), len(o.data.vertices)))
    print("GUM glass: Principled Alpha=0.22 white-blue tint, blend_method=BLEND")
    print("GUM muzzle: -Y forward (Blender), empty bore, NO gumball, NO gauge")


if __name__ == "__main__":
    spec = importlib.util.spec_from_file_location("wooden_base", BASE_SCRIPT)
    wb = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(wb)
    ped, rim, _mats = wb.build_base(clear=True)
    art = build_art()
    export_all(OUT_RES)
    export_all(OUT_MOD)
    wb.report(ped, rim)
    report(art)
