# Soda Bendy tower — Splash tower art rebuild (cup+lid+straw ONLY, no wooden base).
#
# Tapered aqua paper cup with lightning-bolt + bubble image texture, white
# plastic lid, and a red-and-white bendy straw aimed FORWARD (Blender -Y, which
# is Unity +Z) as the muzzle. Composite onto the shared wooden base via
# tools/blender/wooden_base.py build_base(clear=True), then export the full
# tower (base + art) to Soda.glb in both:
#   Assets/Resources/Snack/Towers/Soda.glb   (runtime)
#   Assets/Models/Towers/Soda.glb            (source mirror)
#
# Object names: Cup, Lid, Straw, Bend, Muzzle (+ LidWall merged into Lid).
# NEVER `pedestal` or `rim` here — the base owns those (aim anchoring).
# Only image textures (packed) survive glTF: CupBolt + StrawStripes follow the
# popcorn_cannon.py stripe pattern (create image, img.pack(), ShaderNodeTexImage
# Color -> Principled BSDF Base Color). Primitives only, no Subsurface.
# Run through the Blender MCP (`execute_blender_code`) or Scripting tab.

import bpy
import math
import mathutils
import os
import sys

TOOLS = r"C:\Users\Desktop\SnackTowerDefense\tools\blender"
if TOOLS not in sys.path:
    sys.path.insert(0, TOOLS)
import importlib
import wooden_base

importlib.reload(wooden_base)

RES = "C:/Users/Desktop/SnackTowerDefense/Assets/Resources/Snack"
MOD = "C:/Users/Desktop/SnackTowerDefense/Assets/Models"

try:
    bpy.context.scene.view_settings.view_transform = 'Standard'
except Exception as e:
    print("XFORM_FALLBACK", e)

# ---- shared wooden base first (clear=True wipes the scene) ----
pedestal, rim, _bmats = wooden_base.build_base(clear=True)

BASE_TOP = 0.18  # base occupies z 0 -> 0.18, top disc dia 0.86


def mat_flat(name, rgb, metallic=0.0, rough=0.5):
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
    # strip any stale image nodes so flat mats stay flat
    for n in [n for n in m.node_tree.nodes if n.type == 'TEX_IMAGE']:
        m.node_tree.nodes.remove(n)
    return m


def link_image_mat(name, img, rough=0.5):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    for n in [n for n in nt.nodes if n.type == 'TEX_IMAGE']:
        nt.nodes.remove(n)
    tex = nt.nodes.new('ShaderNodeTexImage')
    tex.image = img
    bc = None
    for inp in bsdf.inputs:
        try:
            if inp.type == 'RGBA' and 'color' in inp.name.lower():
                bc = inp
                break
        except Exception:
            pass
    if bc is None:
        try:
            bc = bsdf.inputs['Base Color']
        except Exception:
            bc = bsdf.inputs[0]
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


def smooth(o):
    for p in o.data.polygons:
        p.use_smooth = True


def apply_scale(o):
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)


def cyl_wrap_uv(o, zmin, zmax, u_offset=0.0):
    """Cylindrical wrap UVs from LOCAL coords (object rotation preserved).

    Seam fix (Splash bolt squeeze): raw u=0.5+atan2(y,x)/2pi+offset wraps
    with %1.0, so the single quad/ngon straddling the u=0/1 boundary used
    to interpolate backwards across the whole texture (bolt squeezed into
    one face). Second pass unwraps each face: if its U span >0.5 it straddles
    the seam, so shift its low (<0.5) loops +1.0. UVs slightly above 1.0 are
    fine (Repeat wrap) and sample the same plain-aqua edge texels.
    With u_offset=0.25 the seam (u=0/1) sits at azimuth +Y (Blender back,
    away from muzzle) and bolt centre (u=0.5) faces -Y (muzzle side).
    """
    me = o.data
    uvl = me.uv_layers.get("UVMap")
    if uvl is None:
        uvl = me.uv_layers.new(name="UVMap")
    raw = {}
    for poly in me.polygons:
        for li in poly.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            u = 0.5 + math.atan2(co.y, co.x) / (2 * math.pi) + u_offset
            v = (co.z - zmin) / max(zmax - zmin, 1e-6)
            raw[li] = (u % 1.0, max(0.0, min(1.0, v)))
    for poly in me.polygons:
        us = [raw[li][0] for li in poly.loop_indices]
        if max(us) - min(us) > 0.5:  # straddles the seam -> unwrap
            if len(poly.loop_indices) > 4:
                # end-cap ngon fan (cup top/bottom, straw ends): its rim
                # spans the full circle, so unwrap can't help; pin it to a
                # plain-background texel instead (cup caps are hidden under
                # base/lid, straw caps under lid/bore). u=0.02 is verified
                # plain aqua in CupBolt / solid stripe in StrawStripes.
                for li in poly.loop_indices:
                    uvl.data[li].uv = (0.02, 0.02)
            else:
                for li in poly.loop_indices:
                    u, v = raw[li]
                    if u < 0.5:
                        u += 1.0
                    uvl.data[li].uv = (u, v)
        else:
            for li in poly.loop_indices:
                uvl.data[li].uv = raw[li]


# ============ CUP GRAPHIC IMAGE (512x512) ============
# Aqua background, big yellow lightning bolt (orange outline), white-outlined
# fizzy bubbles. Bolt centred at U=0.5 so it faces Blender -Y (muzzle side).
W = H = 512
AQUA = (0.16, 0.68, 0.84)
BOLT_Y = (1.00, 0.84, 0.12)
BOLT_O = (1.00, 0.58, 0.05)
BUB_F = (0.55, 0.88, 0.95)
WHITE = (1.0, 1.0, 1.0)
px = [0.0] * (W * H * 4)


def put(x, y, c):
    if 0 <= x < W and 0 <= y < H:
        i = (y * W + x) * 4
        px[i], px[i + 1], px[i + 2], px[i + 3] = c[0], c[1], c[2], 1.0


for y in range(H):
    for x in range(W):
        put(x, y, AQUA)


def pip(x, y, poly):
    inside = False
    j = len(poly) - 1
    for i in range(len(poly)):
        xi, yi = poly[i]
        xj, yj = poly[j]
        if ((yi > y) != (yj > y)) and (x < (xj - xi) * (y - yi) / (yj - yi + 1e-9) + xi):
            inside = not inside
        j = i
    return inside


def fill_poly(poly, c):
    xs = [p[0] for p in poly]
    ys = [p[1] for p in poly]
    for y in range(max(0, int(min(ys))), min(H, int(max(ys)) + 1)):
        for x in range(max(0, int(min(xs))), min(W, int(max(xs)) + 1)):
            if pip(x, y, poly):
                put(x, y, c)


bolt_outer = [(312, 420), (176, 256), (226, 256), (196, 80),
              (346, 244), (280, 244)]
bolt_inner = [(296, 394), (197, 256), (236, 256), (213, 112),
              (324, 239), (274, 239)]
fill_poly(bolt_outer, BOLT_O)
fill_poly(bolt_inner, BOLT_Y)
# bolt highlight sliver
fill_poly([(288, 380), (228, 268), (244, 268), (288, 380)], (1.0, 0.93, 0.45))


def bubble(bx, by, r):
    rr = int(r + 4)
    for y in range(max(0, by - rr), min(H, by + rr + 1)):
        for x in range(max(0, bx - rr), min(W, bx + rr + 1)):
            d = math.hypot(x - bx, y - by)
            if d <= r + 2.5:
                put(x, y, WHITE)
            if d <= r:
                put(x, y, BUB_F)
    # white specular dot
    hx, hy, hr = int(bx - r * 0.32), int(by + r * 0.32), max(2, int(r * 0.22))
    for y in range(max(0, hy - hr), min(H, hy + hr + 1)):
        for x in range(max(0, hx - hr), min(W, hx + hr + 1)):
            if math.hypot(x - hx, y - hy) <= hr:
                put(x, y, WHITE)


for (bx, by, br) in [(150, 300, 28), (200, 268, 19), (118, 340, 14),
                     (352, 318, 26), (382, 278, 15), (332, 182, 30),
                     (298, 148, 13), (162, 182, 21), (138, 138, 12),
                     (402, 200, 11), (108, 250, 10), (372, 382, 14),
                     (250, 120, 10), (420, 320, 9)]:
    bubble(bx, by, br)

cup_img = bpy.data.images.get("CupBolt")
if cup_img is None:
    cup_img = bpy.data.images.new("CupBolt", W, H, alpha=False)
else:
    cup_img.scale(W, H)
cup_img.colorspace_settings.name = 'sRGB'
try:
    cup_img.pixels.foreach_set(px)
except Exception:
    cup_img.pixels[:] = px
cup_img.pack()

# ============ STRAW STRIPE IMAGE (256x256, vertical red/white) ============
SW = SH = 256
NSTR = 8
spx = [0.0] * (SW * SH * 4)
for y in range(SH):
    for x in range(SW):
        s = (x * NSTR // SW) % 2
        c = (0.90, 0.14, 0.14) if s == 0 else (0.97, 0.97, 0.97)
        i = (y * SW + x) * 4
        spx[i], spx[i + 1], spx[i + 2], spx[i + 3] = c[0], c[1], c[2], 1.0
straw_img = bpy.data.images.get("StrawStripes")
if straw_img is None:
    straw_img = bpy.data.images.new("StrawStripes", SW, SH, alpha=False)
else:
    straw_img.scale(SW, SH)
straw_img.colorspace_settings.name = 'sRGB'
try:
    straw_img.pixels.foreach_set(spx)
except Exception:
    straw_img.pixels[:] = spx
straw_img.pack()

cup_mat = link_image_mat("CupBoltMat", cup_img, 0.5)
straw_mat = link_image_mat("StrawStripeMat", straw_img, 0.5)
lid_mat = mat_flat("LidWhite", (0.93, 0.93, 0.94), 0.0, 0.5)
bore_mat = mat_flat("MuzzleBore", (0.15, 0.10, 0.10), 0.0, 0.7)

# ============ CUP: tapered cone, bottom dia 0.44 / top 0.58 / h 0.55 ============
CUP_RB, CUP_RT, CUP_H = 0.22, 0.29, 0.55
CUP_CZ = BASE_TOP + CUP_H / 2  # 0.455, bottom sits at z=0.18
bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=CUP_RB, radius2=CUP_RT,
                                depth=CUP_H, location=(0, 0, CUP_CZ))
cup = bpy.context.active_object
cup.name = "Cup"
cup.data.materials.clear()
cup.data.materials.append(cup_mat)
smooth(cup)
cyl_wrap_uv(cup, -CUP_H / 2, CUP_H / 2, u_offset=0.25)  # bolt U=0.5 faces -Y
apply_scale(cup)

# ============ LID: flat disc + short rim wall, joined into one object ============
CUP_TOP = BASE_TOP + CUP_H  # 0.73
bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.31, depth=0.025,
                                    location=(0, 0, CUP_TOP + 0.015))
lid_disc = bpy.context.active_object
bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.315, depth=0.05,
                                    location=(0, 0, CUP_TOP + 0.0))
lid_wall = bpy.context.active_object
bpy.ops.object.select_all(action='DESELECT')
lid_disc.select_set(True)
lid_wall.select_set(True)
bpy.context.view_layer.objects.active = lid_disc
bpy.ops.object.join()
lid = bpy.context.active_object
lid.name = "Lid"
lid.data.materials.clear()
lid.data.materials.append(lid_mat)
smooth(lid)
apply_scale(lid)

# ============ STRAW: vertical striped tube through the lid ============
STRAW_R = 0.045
STRAW_Z0, STRAW_Z1 = 0.60, 0.92
STRAW_SX, STRAW_SY = 0.0, 0.05
bpy.ops.mesh.primitive_cylinder_add(
    vertices=20, radius=STRAW_R, depth=(STRAW_Z1 - STRAW_Z0),
    location=(STRAW_SX, STRAW_SY, (STRAW_Z0 + STRAW_Z1) / 2))
straw = bpy.context.active_object
straw.name = "Straw"
straw.data.materials.clear()
straw.data.materials.append(straw_mat)
smooth(straw)
cyl_wrap_uv(straw, -(STRAW_Z1 - STRAW_Z0) / 2, (STRAW_Z1 - STRAW_Z0) / 2)
apply_scale(straw)

# ============ BEND: 4 corrugated rings leaning toward -Y, joined ============
BEND_RB = 0.05
bend_parts = []
for i, deg in enumerate((0.0, 22.5, 45.0, 67.5)):
    t = math.radians(deg)
    by = STRAW_SY - BEND_RB * (1 - math.cos(t))
    bz = STRAW_Z1 + BEND_RB * math.sin(t)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=20, radius=0.052, depth=0.022, location=(0, by, bz))
    p = bpy.context.active_object
    p.rotation_euler = (t, 0, 0)  # +X tilt leans the top toward -Y
    p.data.materials.clear()
    p.data.materials.append(straw_mat)
    smooth(p)
    cyl_wrap_uv(p, -0.011, 0.011)
    bend_parts.append(p)
bpy.ops.object.select_all(action='DESELECT')
for p in bend_parts:
    p.select_set(True)
bpy.context.view_layer.objects.active = bend_parts[0]
bpy.ops.object.join()
bend = bpy.context.active_object
bend.name = "Bend"
smooth(bend)
apply_scale(bend)
t_end = math.radians(67.5)
bend_end_y = STRAW_SY - BEND_RB * (1 - math.cos(t_end))
bend_end_z = STRAW_Z1 + BEND_RB * math.sin(t_end)

# ============ MUZZLE: horizontal striped segment toward -Y + dark bore cap ====
MUZ_R, MUZ_L = 0.045, 0.28
MUZ_Z = bend_end_z + 0.012
MUZ_CY = bend_end_y - MUZ_L / 2 + 0.01
bpy.ops.mesh.primitive_cylinder_add(
    vertices=20, radius=MUZ_R, depth=MUZ_L, location=(0, MUZ_CY, MUZ_Z))
muz = bpy.context.active_object
muz.rotation_euler = (math.radians(90), 0, 0)  # axis Z -> Y, tip toward -Y
muz.data.materials.clear()
muz.data.materials.append(straw_mat)
smooth(muz)
cyl_wrap_uv(muz, -MUZ_L / 2, MUZ_L / 2)
TIP_Y = MUZ_CY - MUZ_L / 2
bpy.ops.mesh.primitive_cylinder_add(
    vertices=20, radius=0.036, depth=0.006,
    location=(0, TIP_Y + 0.004, MUZ_Z))
bore = bpy.context.active_object
bore.rotation_euler = (math.radians(90), 0, 0)
bore.data.materials.clear()
bore.data.materials.append(bore_mat)
bpy.ops.object.select_all(action='DESELECT')
muz.select_set(True)
bore.select_set(True)
bpy.context.view_layer.objects.active = muz
bpy.ops.object.join()
muzzle = bpy.context.active_object
muzzle.name = "Muzzle"
smooth(muzzle)
apply_scale(muzzle)

bpy.context.view_layer.update()

# ============ EXPORT full tower (base + art) to both mirrors ============
art = [cup, lid, straw, bend, muzzle]
all_objs = [pedestal, rim] + art
bpy.ops.object.select_all(action='DESELECT')
for o in all_objs:
    o.select_set(True)
bpy.context.view_layer.objects.active = pedestal
for d in [os.path.join(RES, "Towers"), os.path.join(MOD, "Towers")]:
    os.makedirs(d, exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=os.path.join(d, "Soda.glb"),
                              export_format='GLB', use_selection=True)

# ============ REPORT ============
tot_f = sum(len(o.data.polygons) for o in all_objs)
tot_v = sum(len(o.data.vertices) for o in all_objs)
for o in all_objs:
    print("SODA_BENDY %s faces=%d verts=%d loc=%s" %
          (o.name, len(o.data.polygons), len(o.data.vertices),
           tuple(round(v, 3) for v in o.location)))
print("SODA_BENDY TOTAL faces=%d verts=%d" % (tot_f, tot_v))
print("SODA_BENDY cup bottom_dia=0.44 top_dia=0.58 h=0.55 bottom_z=0.18 top_z=0.73")
print("SODA_BENDY straw r=0.045 vertical_z=0.60->0.92 bend_rb=0.05 "
      "muzzle_len=0.28 tip_y=%.3f tip_z=%.3f" % (TIP_Y, MUZ_Z))
print("SODA_BENDY muzzle_direction=Blender_-Y(Unity_+Z) top_art_z=%.3f" % (MUZ_Z + MUZ_R,))
print("SODA_BENDY_EXPORTED")
