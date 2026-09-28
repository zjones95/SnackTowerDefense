# Popcorn cannon tower — SingleShot re-imagining (from Muse Image concept).
#
# Red/cream vertically-striped popcorn bucket (painted texture) with a popcorn
# mound, a tapered steel cannon on trunnion brackets with brass pivots, barrel
# bands with brass rivets, brass breech, and a lobed butter kernel loaded in
# the muzzle. Concept: %TEMP%/opencode/popcorn_genai_concept.png
# Exports Popcorn.glb (replacing the SingleShot tower model) into both:
#   Assets/Resources/Snack/Towers/Popcorn.glb   (runtime)
#   Assets/Models/Towers/Popcorn.glb            (source mirror)
#
# Conventions: base on z = 0, centred in X/Y, scaled to 1.0 unit tall. No
# `pedestal` node, so the whole model rotates to aim (TowerVisual head-split).
# Muzzle faces -Y in Blender (-> +Z forward in Unity).
# Run through the Blender MCP (`execute_blender_code`) or Blender's Scripting tab.

import bpy, math, mathutils, os

RES = "C:/Users/Desktop/SnackTowerDefense/Assets/Resources/Snack"
MOD = "C:/Users/Desktop/SnackTowerDefense/Assets/Models"

for o in list(bpy.data.objects):
    if o.type in ('MESH', 'EMPTY'):
        bpy.data.objects.remove(o, do_unlink=True)

try:
    bpy.context.scene.view_settings.view_transform = 'Standard'
except Exception as e:
    print("XFORM_FALLBACK", e)


def mat(name, rgb, metallic=0.0, rough=0.5):
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


def finish(o, m, nm):
    o.name = nm
    o.data.materials.clear()
    o.data.materials.append(m)
    for p in o.data.polygons:
        p.use_smooth = True
    return o


cream = mat('PcCream', (0.97, 0.93, 0.82), 0.0, 0.5)
steel = mat('PcSteel', (0.22, 0.23, 0.27), 0.55, 0.45)
brass = mat('PcBrass', (0.90, 0.62, 0.15), 0.7, 0.35)
butter = mat('PcButter', (1.0, 0.85, 0.35), 0.0, 0.45)
puffm = mat('PcPuff', (1.0, 0.95, 0.72), 0.0, 0.6)
dark = mat('PcDark', (0.07, 0.07, 0.09), 0.2, 0.7)

objs = []


def add(o):
    objs.append(o)
    return o


# ---- striped bucket texture: vertical red/cream ----
W = H = 256
px = [0.0] * (W * H * 4)
NSTR = 12
for y in range(H):
    for x in range(W):
        s = (x * NSTR // W) % 2
        c = (0.85, 0.13, 0.14) if s == 0 else (0.96, 0.92, 0.80)
        i = (y * W + x) * 4
        px[i], px[i + 1], px[i + 2], px[i + 3] = c[0], c[1], c[2], 1.0
img = bpy.data.images.get("BucketStripes")
if img is None:
    img = bpy.data.images.new("BucketStripes", W, H, alpha=False)
img.colorspace_settings.name = 'sRGB'
try:
    img.pixels.foreach_set(px)
except Exception:
    img.pixels[:] = px
img.pack()
stripemat = bpy.data.materials.get("BucketStripeMat") or bpy.data.materials.new("BucketStripeMat")
stripemat.use_nodes = True
nt = stripemat.node_tree
bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
tex = nt.nodes.new('ShaderNodeTexImage')
tex.image = img
nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
try:
    bsdf.inputs['Roughness'].default_value = 0.5
except Exception:
    pass

# ---- bucket + rim ----
CUP_D = 0.55
bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.30, radius2=0.40, depth=CUP_D, location=(0, 0, 0.275))
cup = bpy.context.active_object
me = cup.data
uvl = me.uv_layers.new(name="UVMap")
zmin, zmax = -CUP_D / 2, CUP_D / 2
for poly in me.polygons:
    for li in poly.loop_indices:
        co = me.vertices[me.loops[li].vertex_index].co
        u = 0.5 + math.atan2(co.y, co.x) / (2 * math.pi) + 0.25
        v = (co.z - zmin) / (zmax - zmin)
        uvl.data[li].uv = (u, v)
add(finish(cup, stripemat, 'Bucket'))
bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.415, depth=0.07, location=(0, 0, 0.565))
add(finish(bpy.context.active_object, cream, 'Rim'))
# ---- popcorn mound ----
for (x, y, z, r) in ((-0.20, 0.02, 0.60, 0.105), (0.19, 0.03, 0.60, 0.105), (0.0, -0.17, 0.60, 0.10),
                     (0.02, 0.19, 0.60, 0.10), (-0.11, -0.10, 0.63, 0.095), (0.12, -0.09, 0.63, 0.095),
                     (-0.10, 0.11, 0.64, 0.09), (0.10, 0.10, 0.64, 0.09), (0.0, 0.0, 0.67, 0.10)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, radius=r, location=(x, y, z))
    add(finish(bpy.context.active_object, puffm, 'Puff'))
# ---- trunnion: crossbar + side brackets + brass pivots ----
bpy.ops.mesh.primitive_cube_add(size=1, location=(0, -0.06, 0.70))
cb = bpy.context.active_object
cb.scale = (0.40, 0.12, 0.06)
add(finish(cb, steel, 'Crossbar'))
for x in (-0.17, 0.17):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(x, -0.06, 0.76))
    br = bpy.context.active_object
    br.scale = (0.055, 0.14, 0.22)
    add(finish(br, steel, 'Bracket'))
for x in (-0.205, 0.205):
    bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=0.035, depth=0.05, location=(x, -0.06, 0.83))
    pv = bpy.context.active_object
    pv.rotation_euler = (0, math.radians(90), 0)
    add(finish(pv, brass, 'Pivot'))
# ---- tapered barrel along Y (breech +Y, muzzle -Y) ----
bpy.ops.mesh.primitive_cone_add(vertices=20, radius1=0.13, radius2=0.105, depth=0.66, location=(0, -0.06, 0.86))
bar = bpy.context.active_object
bar.rotation_euler = (math.radians(90), 0, 0)
add(finish(bar, steel, 'Barrel'))
for (y, r) in ((-0.20, 0.118), (0.10, 0.130)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=r, depth=0.06, location=(0, y, 0.86))
    bd = bpy.context.active_object
    bd.rotation_euler = (math.radians(90), 0, 0)
    add(finish(bd, steel, 'Band'))
    for k in range(6):
        a = math.radians(k * 60)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=6, ring_count=4, radius=0.018,
                                             location=(r * math.cos(a), y, 0.86 + r * math.sin(a)))
        add(finish(bpy.context.active_object, brass, 'Rivet'))
bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.13, depth=0.10, location=(0, -0.385, 0.86))
mz = bpy.context.active_object
mz.rotation_euler = (math.radians(90), 0, 0)
add(finish(mz, steel, 'MuzzleRing'))
bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.075, depth=0.02, location=(0, -0.44, 0.86))
md = bpy.context.active_object
md.rotation_euler = (math.radians(90), 0, 0)
add(finish(md, dark, 'MuzzleBore'))
bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.115, location=(0, 0.27, 0.86))
add(finish(bpy.context.active_object, brass, 'Breech'))
# ---- loaded kernel: lobed butter kernel at muzzle ----
bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=7, radius=0.062, location=(0, -0.50, 0.86))
ker = bpy.context.active_object
ker.scale = (1, 1.3, 1)
add(finish(ker, butter, 'Kernel'))
for (x, z) in ((0.045, 0.885), (-0.045, 0.885), (0.0, 0.825)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=5, radius=0.035, location=(x, -0.50, z))
    add(finish(bpy.context.active_object, butter, 'Lobe'))

bpy.context.view_layer.update()
maxz = 0.0
for o in objs:
    for c in o.bound_box:
        w = o.matrix_world @ mathutils.Vector(c)
        if w.z > maxz:
            maxz = w.z
s = 1.0 / maxz
for o in objs:
    o.location = (o.location[0] * s, o.location[1] * s, o.location[2] * s)
    o.scale = (o.scale[0] * s, o.scale[1] * s, o.scale[2] * s)
bpy.context.view_layer.update()

bpy.ops.object.select_all(action='DESELECT')
for o in objs:
    o.select_set(True)
bpy.context.view_layer.objects.active = bpy.data.objects.get('Bucket')
for d in [os.path.join(RES, "Towers"), os.path.join(MOD, "Towers")]:
    os.makedirs(d, exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=os.path.join(d, "Popcorn.glb"), export_format='GLB', use_selection=True)

tot_f = sum(len(o.data.polygons) for o in objs)
tot_v = sum(len(o.data.vertices) for o in objs)
print("POPCORN_CANNON faces=%d verts=%d objs=%d" % (tot_f, tot_v, len(objs)))
