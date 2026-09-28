# Soda Cup tower — in-game model author (Splash tower).
#
# Builds the Soda Cup as a white paper cup with a teal brush-splat + magenta/purple
# slash (a painted, packed texture), a domed lid, and a BENT STRAW used as a forward
# cannon barrel. Exports Soda.glb, replacing the Splash tower model, into both:
#   Assets/Resources/Snack/Towers/Soda.glb   (runtime)
#   Assets/Models/Towers/Soda.glb            (source mirror)
#
# Conventions: base on z = 0, centred in X/Y, scaled to 1.0 unit tall so it matches
# the other authored towers. The base object is named "pedestal" so SnackModels.CenterOn
# anchors on it (the cup stays on the tile centre while the barrel points forward), and
# TowerVisual's head-split keeps it fixed while the rest rotates to aim.
#
# Mob-facing is -Y in Blender (-> +Z forward in Unity), so the barrel points along -Y.
# Run through the Blender MCP (`execute_blender_code`) or Blender's Scripting tab.

import bpy, mathutils, math, os

RES = "C:/Users/Desktop/SnackTowerDefense/Assets/Resources/Snack"
MOD = "C:/Users/Desktop/SnackTowerDefense/Assets/Models"

for o in list(bpy.data.objects):
    if o.type == 'MESH':
        bpy.data.objects.remove(o, do_unlink=True)


def mat(name, rgb, metallic=0.0, rough=0.5):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if b:
        b.inputs['Base Color'].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
        b.inputs['Metallic'].default_value = metallic
        b.inputs['Roughness'].default_value = rough
    return m


def smooth(o):
    for p in o.data.polygons:
        p.use_smooth = True


def cyl(r, d, x, y, z, m, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=r, depth=d, location=(x, y, z))
    o = bpy.context.active_object
    o.rotation_euler = rot
    o.data.materials.append(m)
    smooth(o)
    return o


def cone(r1, r2, d, x, y, z, m):
    bpy.ops.mesh.primitive_cone_add(vertices=64, radius1=r1, radius2=r2, depth=d, location=(x, y, z))
    o = bpy.context.active_object
    o.data.materials.append(m)
    smooth(o)
    return o


def sph(r, x, y, z, m, sz=1.0):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=r, location=(x, y, z))
    o = bpy.context.active_object
    o.scale = (1, 1, sz)
    o.data.materials.append(m)
    smooth(o)
    return o


# ---- painted cup texture: white paper + solid teal splat + magenta/purple slash ----
W = H = 256
px = [0.0] * (W * H * 4)


def put(x, y, r, g, b):
    if 0 <= x < W and 0 <= y < H:
        i = (y * W + x) * 4
        px[i] = r
        px[i + 1] = g
        px[i + 2] = b
        px[i + 3] = 1.0


for y in range(H):
    for x in range(W):
        put(x, y, 0.96, 0.96, 0.94)

cx, cy = W * 0.5, H * 0.52


def star_poly(n, rout, rin, rot, ox=0.0, oy=0.0):
    pts = []
    for i in range(2 * n):
        a = rot + i * math.pi / n
        rad = rout if i % 2 == 0 else rin
        pts.append((cx + ox + rad * math.cos(a), cy + oy + rad * math.sin(a)))
    return pts


def pip(x, y, poly):
    inside = False
    j = len(poly) - 1
    for i in range(len(poly)):
        xi, yi = poly[i]
        xj, yj = poly[j]
        if ((yi > y) != (yj > y)) and (x < (xj - xi) * (y - yi) / (yj - yi) + xi):
            inside = not inside
        j = i
    return inside


teal = (0.02, 0.66, 0.80)
for (ox, oy, rout, rin, n, rot) in [(0.0, 0.0, 0.40, 0.17, 7, 0.35), (-0.06, -0.05, 0.30, 0.13, 6, 1.05)]:
    poly = star_poly(n, rout * W, rin * W, rot, ox * W, oy * H)
    for y in range(H):
        for x in range(W):
            if pip(x, y, poly):
                put(x, y, teal[0], teal[1], teal[2])


def slash(cx0, cy0, dx, dy, halfw, col):
    L = math.hypot(dx, dy)
    ux, uy = dx / L, dy / L
    nx, ny = -uy, ux
    for y in range(H):
        for x in range(W):
            t = (x - cx0) * ux + (y - cy0) * uy
            if abs(t) > L * 0.5:
                continue
            if abs((x - cx0) * nx + (y - cy0) * ny) < halfw:
                put(x, y, col[0], col[1], col[2])


slash(cx, cy, 0.66 * W, -0.50 * H, 0.060 * W, (0.74, 0.06, 0.58))
slash(cx, cy, 0.60 * W, -0.44 * H, 0.030 * W, (0.34, 0.14, 0.62))

img = bpy.data.images.new("CupPaint", W, H, alpha=False)
img.colorspace_settings.name = 'sRGB'
try:
    img.pixels.foreach_set(px)
except Exception:
    img.pixels[:] = px
img.pack()

# ---- model ----
white = mat("CupTrim", (0.95, 0.95, 0.93), 0.0, 0.42)
lidm = mat("Lid", (0.16, 0.17, 0.20), 0.0, 0.62)
straw = mat("Straw", (0.96, 0.78, 0.16), 0.0, 0.36)
straw2 = mat("StrawTip", (0.90, 0.66, 0.10), 0.0, 0.34)
base = mat("Base", (0.12, 0.12, 0.14), 0.0, 0.70)

objs = []
p0 = cyl(0.42, 0.14, 0, 0, 0.07, base)
p0.name = "pedestal"   # CenterOn anchor + fixed head-split part
objs.append(p0)
objs.append(cyl(0.44, 0.03, 0, 0, 0.155, base))
objs.append(cyl(0.30, 0.10, 0, 0, 0.22, base))

CUP_D = 0.62
cup = cone(0.255, 0.345, CUP_D, 0, 0, 0.60, white)
objs.append(cup)
objs.append(cone(0.345, 0.348, 0.05, 0, 0, 0.885, white))
objs.append(cone(0.258, 0.262, 0.05, 0, 0, 0.31, white))
objs.append(cyl(0.365, 0.05, 0, 0, 0.93, lidm))
objs.append(sph(0.345, 0, 0, 0.945, lidm, sz=0.45))

# bent-straw cannon, aimed forward (-Y)
RISE_Z = 1.06
objs.append(cyl(0.062, 0.24, 0.0, 0.10, RISE_Z, straw))
objs.append(sph(0.062, 0.0, 0.10, RISE_Z + 0.12, straw))
objs.append(cyl(0.062, 0.62, 0.0, -0.21, RISE_Z + 0.12, straw2, rot=(1.5708, 0, 0)))
objs.append(cyl(0.078, 0.07, 0.0, -0.50, RISE_Z + 0.12, straw2, rot=(1.5708, 0, 0)))
objs.append(cyl(0.045, 0.03, 0.0, -0.535, RISE_Z + 0.12, base, rot=(1.5708, 0, 0)))

for o in objs:
    smooth(o)

# cylindrical UVs + painted material on the cup body
me = cup.data
uvl = me.uv_layers.new(name="UVMap")
zmin, zmax = -CUP_D / 2, CUP_D / 2
for poly in me.polygons:
    for li in poly.loop_indices:
        co = me.vertices[me.loops[li].vertex_index].co
        u = 0.5 + math.atan2(co.y, co.x) / (2 * math.pi) + 0.25   # design faces -Y
        v = (co.z - zmin) / (zmax - zmin)
        uvl.data[li].uv = (u, v)

paint = bpy.data.materials.new("CupPaintMat")
paint.use_nodes = True
nt = paint.node_tree
bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
tex = nt.nodes.new('ShaderNodeTexImage')
tex.image = img
nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
bsdf.inputs['Roughness'].default_value = 0.42
me.materials.clear()
me.materials.append(paint)

# scale about the origin so total height = 1.0 (base stays on z = 0)
maxz = 0.0
for o in objs:
    for c in o.bound_box:
        w = o.matrix_world @ mathutils.Vector(c)
        if w.z > maxz:
            maxz = w.z
s = 1.0 / maxz
for o in objs:
    o.location = o.location * s
    o.scale = o.scale * s
bpy.context.view_layer.update()

# export Soda.glb, replacing the Splash tower model
bpy.ops.object.select_all(action='DESELECT')
for o in objs:
    o.select_set(True)
bpy.context.view_layer.objects.active = objs[0]
for d in [os.path.join(RES, "Towers"), os.path.join(MOD, "Towers")]:
    os.makedirs(d, exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=os.path.join(d, "Soda.glb"), export_format='GLB', use_selection=True)

print("SODA_REPLACED")
