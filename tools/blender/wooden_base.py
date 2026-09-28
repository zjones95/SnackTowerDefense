# Wooden tower base — base-only prototype (from flat round platform concept).
#
# Flat round wooden pedestal platform, single level, toy-like, centered on
# X/Y with bottom at z=0. This is the reusable base; snack tops are added by
# future tower scripts which import build_base() then export full towers.
# Concept: %TEMP%/opencode/wooden_base_flat_genai_concept.png
# (plank top, dark wood side, orange bottom ring — ring made TALLER here so
# the tier color reads in-game).
#
# Textures: procedural IMAGE textures generated in Python (packed into the
# .blend). ONLY image textures survive glTF export to Unity — procedural
# node trees are dropped by the exporter. Pattern follows
# tools/blender/popcorn_cannon.py lines 72-101 (create image, img.pack(),
# link ShaderNodeTexImage Color -> Principled BSDF Base Color).
#   WoodTop  -> WoodTopPlanks image (512x512, 5 plank rows + grain + knots)
#   WoodSide -> WoodSideBand image (512x256, stave seams + wrap grain)
#   TierOrange (rim) stays a FLAT untextured color — code tints it per tier.
#
# Dimensions (Blender units = meters):
#   total height 0.18, max diameter 0.90 (rim), fits 2-unit tile at 1.66x:
#   0.90*1.66 = 1.5 < 2.0 OK.
#   rim:       h=0.07, r=0.45 (dia 0.90), z 0.00 -> 0.07, own object+material
#   side band: h=0.09, r=0.43 (dia 0.86), z 0.07 -> 0.16, part of `pedestal`
#   top disc:  h=0.02, r=0.43 (dia 0.86), z 0.16 -> 0.18, part of `pedestal`
# Objects: `pedestal` (side+top joined, 2 material slots), `rim` (own
# material so code can tint it per tower tier later). All cylinders 20
# segments, primitives only, no Subdivision Surface. Round base so aim is
# irrelevant; Blender -Y kept as forward for consistency.
# Run through the Blender MCP (`execute_blender_code`) or Blender's Scripting tab.

import bpy
import math
import os
import random

RES = "C:/Users/Desktop/SnackTowerDefense/Assets/Resources/Snack"
MOD = "C:/Users/Desktop/SnackTowerDefense/Assets/Models"
PREVIEW = "C:/Users/Desktop/AppData/Local/Temp/opencode/wooden_base_preview.glb"

# Dimensions (see header).
RIM_H, RIM_R = 0.07, 0.45
SIDE_H, SIDE_R = 0.09, 0.43
TOP_H, TOP_R = 0.02, 0.43
SEGMENTS = 20

# Colors (linear-ish picks; Standard view transform for previews).
TOP_RGB = (0.80, 0.52, 0.22)   # warm orange-brown plank top
SIDE_RGB = (0.30, 0.18, 0.10)  # dark wood side band
RIM_RGB = (0.95, 0.50, 0.12)   # tier-tint orange accent ring


def mat(name, rgb, metallic=0.0, rough=0.55):
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
    return m


def make_top_image():
    """Top planks texture: 512x512, warm orange-brown, 5 horizontal plank
    rows with dark seams, sine-warped grain along plank direction, 2 small
    darker knots, per-plank brightness variation. Kid-friendly toy look."""
    W = H = 512
    PLANKS = 5
    row_h = H / PLANKS
    rng = random.Random(42)
    row_var = [1.0, 0.94, 1.06, 0.97, 1.03]
    row_ph = [rng.uniform(0, 6.28) for _ in range(PLANKS)]
    base = (0.82, 0.55, 0.25)
    seam = (0.23, 0.13, 0.07)
    knots = [(150, 140, 13), (370, 390, 15)]  # x, y (from bottom), radius
    px = [0.0] * (W * H * 4)
    for y in range(H):
        row = min(int(y // row_h), PLANKS - 1)
        y_in = y - row * row_h
        for x in range(W):
            i = (y * W + x) * 4
            if y_in < 3:  # dark seam between planks
                px[i], px[i + 1], px[i + 2], px[i + 3] = seam[0], seam[1], seam[2], 1.0
                continue
            v = row_var[row]
            r, g, b = base[0] * v, base[1] * v, base[2] * v
            w = math.sin(y * 0.05 + row * 1.7) * 2.0 + math.sin(x * 0.008 + row) * 1.5
            gr = 0.5 + 0.5 * math.sin(x * 0.035 + w + row_ph[row])
            gr2 = 0.5 + 0.5 * math.sin(x * 0.09 + w * 1.4 + row_ph[row] * 2.0)
            dark = 1.0 - 0.13 * (gr * 0.6 + gr2 * 0.4)
            if gr2 > 0.88:  # thin dark grain lines
                dark *= 0.85
            r *= dark
            g *= dark
            b *= dark
            n = (rng.random() - 0.5) * 0.04
            r += n
            g += n
            b += n
            for (kx, ky, kr) in knots:
                dx, dy = x - kx, y - ky
                d = math.sqrt(dx * dx + dy * dy)
                if d < kr:
                    t = d / max(kr, 1e-5)
                    ring = 0.5 + 0.5 * math.sin(d * 1.2)
                    kd = 1.0 - 0.5 * (1.0 - t) * (0.6 + 0.4 * ring)
                    r = r * kd * 0.88 + 0.30 * (1 - kd)
                    g = g * kd * 0.82 + 0.18 * (1 - kd)
                    b = b * kd * 0.78 + 0.10 * (1 - kd)
            px[i], px[i + 1], px[i + 2], px[i + 3] = (
                max(0, min(1, r)), max(0, min(1, g)), max(0, min(1, b)), 1.0)
    img = bpy.data.images.get("WoodTopPlanks")
    if img is None:
        img = bpy.data.images.new("WoodTopPlanks", W, H, alpha=False)
    else:
        img.scale(W, H)
    img.colorspace_settings.name = 'sRGB'
    try:
        img.pixels.foreach_set(px)
    except Exception:
        img.pixels[:] = px
    img.pack()
    return img


def make_side_image():
    """Side band texture: 512x256, darker brown, horizontal grain streaks
    wrapping around (U wraps circumference), subtle vertical stave seams
    every 1/12 of width."""
    W2, H2 = 512, 256
    rng = random.Random(1234)
    base2 = (0.38, 0.235, 0.125)
    seam2 = (0.16, 0.09, 0.05)
    ph = rng.uniform(0, 6.28)
    px2 = [0.0] * (W2 * H2 * 4)
    for y in range(H2):
        for x in range(W2):
            i = (y * W2 + x) * 4
            if (x % (W2 // 12)) < 2:  # stave joint seams
                px2[i], px2[i + 1], px2[i + 2], px2[i + 3] = seam2[0], seam2[1], seam2[2], 1.0
                continue
            r, g, b = base2
            w = math.sin(x * 0.025 + ph) * 3.0 + math.sin(y * 0.05) * 1.5
            gr = 0.5 + 0.5 * math.sin(y * 0.30 + w + ph)
            gr2 = 0.5 + 0.5 * math.sin(y * 0.75 + w * 1.6)
            dark = 1.0 - 0.20 * (gr * 0.65 + gr2 * 0.35)
            if gr2 > 0.88:
                dark *= 0.80
            r *= dark
            g *= dark
            b *= dark
            n = (rng.random() - 0.5) * 0.045
            r += n
            g += n
            b += n
            px2[i], px2[i + 1], px2[i + 2], px2[i + 3] = (
                max(0, min(1, r)), max(0, min(1, g)), max(0, min(1, b)), 1.0)
    img = bpy.data.images.get("WoodSideBand")
    if img is None:
        img = bpy.data.images.new("WoodSideBand", W2, H2, alpha=False)
    else:
        img.scale(W2, H2)
    img.colorspace_settings.name = 'sRGB'
    try:
        img.pixels.foreach_set(px2)
    except Exception:
        img.pixels[:] = px2
    img.pack()
    return img


def link_image(mat_name, img, rough=0.55):
    """Link an image texture Color -> Principled BSDF Base Color (glTF-safe).
    Removes stale TEX_IMAGE nodes first; keeps Roughness/Metallic flat."""
    m = bpy.data.materials.get(mat_name)
    assert m is not None and img is not None, (mat_name, img)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    for n in [n for n in nt.nodes if n.type == 'TEX_IMAGE']:
        nt.nodes.remove(n)
    tex = nt.nodes.new('ShaderNodeTexImage')
    tex.image = img
    bc = None
    for inp in bsdf.inputs:
        if inp.type == 'RGBA' and 'color' in inp.name.lower():
            bc = inp
            break
    if bc is None:
        bc = bsdf.inputs.get('Base Color') or bsdf.inputs[0]
    nt.links.new(tex.outputs['Color'], bc)
    try:
        bsdf.inputs['Roughness'].default_value = rough
    except Exception:
        pass
    try:
        bsdf.inputs['Metallic'].default_value = 0.0
    except Exception:
        pass
    return m


def apply_uvs(pedestal):
    """Low-level UV assignment (no UI-context operators).
    Top disc caps: planar top-down (X/Y -> U/V). Everything else on the
    pedestal: cylindrical wrap so side grain runs around the band."""
    me = pedestal.data
    uvl = me.uv_layers.get("UVMap")
    if uvl is None:
        uvl = me.uv_layers.new(name="UVMap")
    R = TOP_R
    ZMIN, ZMAX = RIM_H, RIM_H + SIDE_H + TOP_H  # 0.07 .. 0.18
    mw = pedestal.matrix_world
    for poly in me.polygons:
        is_top_slot = (poly.material_index == 1)  # slot 0=WoodSide, 1=WoodTop
        zdom = abs(poly.normal.z) > 0.5
        for li in poly.loop_indices:
            co_l = me.vertices[me.loops[li].vertex_index].co
            co_w = mw @ co_l
            if is_top_slot and zdom:
                u = co_w.x / (2 * R) + 0.5
                v = co_w.y / (2 * R) + 0.5
            else:
                u = 0.5 + math.atan2(co_l.y, co_l.x) / (2 * math.pi)
                v = (co_w.z - ZMIN) / (ZMAX - ZMIN)
                v = max(0.0, min(1.0, v))
            uvl.data[li].uv = (u, v)
    return uvl


def _apply_transforms(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)


def build_base(clear=True):
    """Build the wooden pedestal base. Returns (pedestal, rim, materials)."""
    if clear:
        for o in list(bpy.data.objects):
            if o.type in ('MESH', 'EMPTY'):
                bpy.data.objects.remove(o, do_unlink=True)
    try:
        bpy.context.scene.view_settings.view_transform = 'Standard'
    except Exception as e:
        print("XFORM_FALLBACK", e)

    top_mat = mat('WoodTop', TOP_RGB, 0.0, 0.55)
    side_mat = mat('WoodSide', SIDE_RGB, 0.0, 0.55)
    rim_mat = mat('TierOrange', RIM_RGB, 0.0, 0.5)

    # Side band cylinder (dark wood).
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=SEGMENTS, radius=SIDE_R, depth=SIDE_H,
        location=(0, 0, RIM_H + SIDE_H / 2))
    side = bpy.context.active_object
    side.name = 'pedestal_side'
    side.data.materials.clear()
    side.data.materials.append(side_mat)
    _apply_transforms(side)

    # Top plank disc (warm orange-brown).
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=SEGMENTS, radius=TOP_R, depth=TOP_H,
        location=(0, 0, RIM_H + SIDE_H + TOP_H / 2))
    top = bpy.context.active_object
    top.name = 'pedestal_top'
    top.data.materials.clear()
    top.data.materials.append(top_mat)
    _apply_transforms(top)

    # Join side+top into single `pedestal` (keeps both material slots).
    bpy.ops.object.select_all(action='DESELECT')
    side.select_set(True)
    top.select_set(True)
    bpy.context.view_layer.objects.active = side
    bpy.ops.object.join()
    pedestal = bpy.context.active_object
    pedestal.name = 'pedestal'

    # Bottom accent ring (own object + own material for per-tier tinting).
    # Slightly wider radius than the side band so it reads as trim.
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=SEGMENTS, radius=RIM_R, depth=RIM_H,
        location=(0, 0, RIM_H / 2))
    rim = bpy.context.active_object
    rim.name = 'rim'
    rim.data.materials.clear()
    rim.data.materials.append(rim_mat)
    _apply_transforms(rim)

    # --- Wood-grain image textures (glTF-safe, packed) ---
    top_img = make_top_image()
    side_img = make_side_image()
    link_image('WoodTop', top_img, 0.55)
    link_image('WoodSide', side_img, 0.55)
    # Rim stays a flat untextured color for per-tier tinting: strip any
    # image nodes that may linger from earlier sessions.
    for n in [n for n in rim_mat.node_tree.nodes if n.type == 'TEX_IMAGE']:
        rim_mat.node_tree.nodes.remove(n)
    apply_uvs(pedestal)
    if len(rim.data.uv_layers) == 0:
        rim.data.uv_layers.new(name="UVMap")

    bpy.context.view_layer.update()
    return pedestal, rim, {'top': top_mat, 'side': side_mat, 'rim': rim_mat}


def select_base(pedestal, rim):
    bpy.ops.object.select_all(action='DESELECT')
    pedestal.select_set(True)
    rim.select_set(True)
    bpy.context.view_layer.objects.active = pedestal


def export_preview(pedestal, rim, filepath=PREVIEW):
    select_base(pedestal, rim)
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=filepath, export_format='GLB', use_selection=True)


def report(pedestal, rim):
    pf, pv = len(pedestal.data.polygons), len(pedestal.data.vertices)
    rf, rv = len(rim.data.polygons), len(rim.data.vertices)
    print("WOODEN_BASE pedestal faces=%d verts=%d | rim faces=%d verts=%d | "
          "TOTAL faces=%d verts=%d" % (pf, pv, rf, rv, pf + rf, pv + rv))
    print("WOODEN_BASE dims total_h=0.18 max_dia=0.90 "
          "(rim h=0.07 r=0.45 / side h=0.09 r=0.43 / top h=0.02 r=0.43)")


if __name__ == "__main__":
    _ped, _rim, _mats = build_base(clear=True)
    export_preview(_ped, _rim)
    report(_ped, _rim)
