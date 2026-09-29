"""Waves 13-18 mobs: Broccoli, Strawberry, Pineapple (boss), Peach, Blueberry, Cucumber.

Run through Blender MCP:
    exec(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_wave13_18.py').read())
    build_1318_all()

Loads shared helpers from mobs_first5.py (Builder, primitives, surfaces, feet,
rig, export, gallery, walk_sheet). Same conventions: one joined mesh named the
MobDef.id, base z=0, forward -Y, flat-shaded, rigged + 24-frame walk loop,
exported to BOTH Unity mirrors.
"""
from pathlib import Path

from mathutils import Quaternion

exec(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_first5.py').read())

SCENE_1318 = 'Mobs 13-18'
TARGET_HEIGHT.update({'Broccoli': 1.15, 'Strawberry': 1.05, 'Pineapple': 1.4,
                      'Peach': 1.0, 'Blueberry': 0.9, 'Cucumber': 1.15})
WALK.update({
    'Broccoli': dict(swing=20, lift=.040, bob=.026, roll=7.0, nod=2.0),
    'Strawberry': dict(swing=30, lift=.050, bob=.024, roll=4.5, nod=2.5),
    'Pineapple': dict(swing=18, lift=.040, bob=.020, roll=4.5, nod=1.5),
    'Peach': dict(swing=26, lift=.045, bob=.022, roll=5.0, nod=2.0),
    'Blueberry': dict(swing=32, lift=.050, bob=.022, roll=4.0, nod=3.0),
    'Cucumber': dict(swing=20, lift=.040, bob=.024, roll=6.5, nod=2.0),
})


def diamond_lathe(profile, n):
    """Lathe with crisscross two-tone faces. Returns verts, faces, tone per face."""
    verts, rings, faces, tone = [], [], [], []
    for r, z in profile:
        if r <= 1e-6:
            verts.append((0, 0, z))
            rings.append(len(verts) - 1)
        else:
            idx = []
            for i in range(n):
                a = 2 * PI * i / n
                verts.append((r * math.cos(a), r * math.sin(a), z))
                idx.append(len(verts) - 1)
            rings.append(idx)
    for k in range(len(profile) - 1):
        A, B = rings[k], rings[k + 1]
        if isinstance(A, int) and isinstance(B, int):
            continue
        for i in range(n):
            i2 = (i + 1) % n
            t = (i + k) % 2
            if isinstance(A, int):
                faces.append((A, B[i], B[i2]))
            elif isinstance(B, int):
                faces.append((A[i], A[i2], B))
            else:
                faces.append((A[i], A[i2], B[i2], B[i]))
            tone.append(t)
    if not isinstance(rings[0], int):
        faces.append(tuple(rings[0][::-1]))
        tone.append(0)
    if not isinstance(rings[-1], int):
        faces.append(tuple(rings[-1]))
        tone.append(0)
    return verts, faces, tone


# ---------------------------------------------------------------- builders
def build_broccoli():
    b = Builder('Broccoli')
    stalk = b.mat('stalk', '#B9C98A', .65)
    floret = b.mat('floret', '#2E6B2E', .6)
    floret2 = b.mat('floret_dark', '#1F4A24', .6)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#3A2A18', .5)
    foot = b.mat('foot', '#7A8A4A', .7)
    prof = [(0, .14), (.24, .14), (.27, .30), (.24, .48), (.20, .62), (0, .64)]
    N = 10
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), stalk)
    # floret crown: one big center bud + rings of smaller buds (scaled spheres!)
    buds = [(0, 0, .88, .30)] + [(math.cos(a) * .26, math.sin(a) * .26, .80, .20)
                                 for a in [k * 2 * PI / 6 for k in range(6)]]
    buds += [(math.cos(a) * .38, math.sin(a) * .38, .68, .15) for a in
             [(k + .5) * 2 * PI / 6 for k in range(6)]]
    buds += [(0, 0, 1.02, .18)]
    for k, (x, y, z, r) in enumerate(buds):
        b.add(sphere_g(6, 4), floret if k % 2 == 0 else floret2, X((x, y, z), (0, 0, 0), (r, r, r)))
    feet(b, stalk, foot, (-.12, .12))
    for sx in (-1, 1):
        disc(b, S, black, sx * .11, .44, .045, .03, n=8)
    deco_strip(b, S, mouth, [-.07 + .14 * k / 6 for k in range(7)],
               lambda x: .33 + 3.0 * x * x, lambda x: .022 * (1 - (x / .08) ** 2 * .7))
    return b


def build_strawberry():
    b = Builder('Strawberry')
    skin = b.mat('skin', '#E8353F', .5)
    seed = b.mat('seed', '#F5E6A8', .6)
    leaf = b.mat('leaf', '#3F9E3E', .6)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#7A1512', .5)
    foot = b.mat('foot', '#A02028', .7)
    R, zc = .40, .52
    prof = [(0, zc - R - .04)] + [(R * math.cos(math.radians(p)), zc + R * math.sin(math.radians(p)))
                                  for p in (-60, -30, 0, 30, 60)] + [(0, zc + R)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), skin)
    for k in range(6):
        a = k * 2 * PI / 6
        q = Quaternion((0, 0, 1), a) @ Quaternion((0, 1, 0), math.radians(20))
        loc = Vector((math.cos(a) * .03, math.sin(a) * .03, zc + R - .02))
        b.add(lens_g(.24, .10, .04), leaf, Matrix.Translation(loc) @ q.to_matrix().to_4x4())
    for (x, z) in [(-.2, .45), (.2, .45), (-.26, .60), (.26, .60), (-.12, .30),
                   (.12, .30), (0, .68), (-.3, .38), (.3, .38)]:
        disc(b, S, seed, x, z, .020, .015, n=5)
    feet(b, skin, foot, (-.12, .12))
    for sx in (-1, 1):
        disc(b, S, black, sx * .13, .55, .048, .03, n=8)
    deco_strip(b, S, mouth, [-.09 + .18 * k / 6 for k in range(7)],
               lambda x: .43 + 3.2 * x * x, lambda x: .024 * (1 - (x / .10) ** 2 * .7))
    return b


def build_pineapple():
    b = Builder('Pineapple')
    gold = b.mat('gold', '#F2C230', .55)
    gold2 = b.mat('gold_dark', '#C8901C', .6)
    leaf = b.mat('leaf', '#3F9E3E', .6)
    black = b.mat('eye', '#262322', .4)
    brow = b.mat('brow', '#7A5A10', .6)
    mouth = b.mat('mouth', '#6B4A10', .5)
    foot = b.mat('foot', '#8A6414', .7)
    R, zc, H = .38, .62, 1.02
    prof = [(0, .14), (.30, .14), (.37, .30), (.38, .55), (.35, .80), (.28, .98), (0, 1.04)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, tone = diamond_lathe(prof, N)
    b.add((v, f), [gold if t == 0 else gold2 for t in tone])
    # spiky crown: ring of tapering spikes tilted outward + taller center spikes
    for k in range(8):
        a = k * 2 * PI / 8
        tilt = .45
        q = Quaternion((0, 0, 1), a) @ Quaternion((0, 1, 0), math.pi / 2 - tilt)
        loc = Vector((math.cos(a) * .12, math.sin(a) * .12, 1.00))
        b.add(frustum_g(4, .055, 0, .42), leaf, Matrix.Translation(loc) @ q.to_matrix().to_4x4())
    for k in range(3):
        b.add(frustum_g(4, .05, 0, .5), leaf, X((0, 0, 1.02), (0, .25, k * 2 * PI / 3)))
    feet(b, gold, foot, (-.16, .16), leg_r=.06, leg_h=.20, leg_z=.02, foot_sc=(.14, .20, .06))
    for sx in (-1, 1):
        disc(b, S, black, sx * .14, .66, .05, .03, n=8)
        c = S.pt(sx * .14, .72, 0)
        b.add(box_g(), brow, X((c.x, c.y - .012, c.z), (0, 0, sx * -.42), (.14, .030, .04)))
    xs = [-.11 + .22 * k / 6 for k in range(7)]
    deco_strip(b, S, mouth, xs, lambda x: .52 - 2.4 * x * x, lambda x: .024, depth=.04)
    return b


def build_peach():
    b = Builder('Peach')
    skin = b.mat('skin', '#F59A6E', .55)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#8A4A30', .5)
    brown = b.mat('stem', '#7B4B23', .7)
    leaf = b.mat('leaf', '#5AA04A', .6)
    foot = b.mat('foot', '#C97A50', .7)
    R, zc = .42, .55
    prof = [(0, zc - R)] + [(R * math.cos(math.radians(p)), zc + R * math.sin(math.radians(p)))
                            for p in (-60, -30, 0, 30, 60)] + [(0, zc + R)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), skin)
    b.add(frustum_g(5, .045, .032, .16), brown, X((0, 0, zc + R - .03), (0, -.18, 0)))
    b.add(lens_g(.22, .11, .03), leaf, X((.03, 0, zc + R + .06), (0, -.30, .5)))
    feet(b, skin, foot, (-.13, .13))
    for sx in (-1, 1):
        disc(b, S, black, sx * .14, .60, .05, .03, n=8)
    deco_strip(b, S, mouth, [-.10 + .20 * k / 6 for k in range(7)],
               lambda x: .46 + 3.2 * x * x, lambda x: .026 * (1 - (x / .11) ** 2 * .7))
    return b


def build_blueberry():
    b = Builder('Blueberry')
    skin = b.mat('skin', '#5A6EC9', .5)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#2A2A4A', .5)
    star = b.mat('star', '#C89040', .6)
    foot = b.mat('foot', '#3A468A', .7)
    R, zc = .34, .50
    prof = [(0, zc - R)] + [(R * math.cos(math.radians(p)), zc + R * math.sin(math.radians(p)))
                            for p in (-60, -30, 0, 30, 60)] + [(0, zc + R)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), skin)
    # tiny 5-point star crown
    for k in range(5):
        a = k * 2 * PI / 5
        q = Quaternion((0, 0, 1), a) @ Quaternion((0, 1, 0), math.radians(25))
        loc = Vector((math.cos(a) * .02, math.sin(a) * .02, zc + R - .01))
        b.add(lens_g(.13, .055, .025), star, Matrix.Translation(loc) @ q.to_matrix().to_4x4())
    b.add(frustum_g(5, .035, .035, .03), star, X((0, 0, zc + R - .02)))
    feet(b, skin, foot, (-.10, .10), leg_r=.04, leg_h=.15, leg_z=.06, foot_sc=(.09, .15, .06))
    for sx in (-1, 1):
        disc(b, S, black, sx * .11, .54, .042, .03, n=8)
    deco_strip(b, S, mouth, [-.08 + .16 * k / 6 for k in range(7)],
               lambda x: .43 + 3.4 * x * x, lambda x: .022 * (1 - (x / .09) ** 2 * .7))
    return b


def build_cucumber():
    b = Builder('Cucumber')
    skin = b.mat('skin', '#2E7A32', .55)
    stripe = b.mat('stripe', '#7ACB6C', .55)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#1A3A1E', .5)
    nub = b.mat('nub', '#5AA04A', .6)
    foot = b.mat('foot', '#1F5A26', .7)
    R, zc = .26, .62
    prof = [(0, .14), (.18, .14), (.25, .26), (.26, .45), (.25, .65), (.22, .85), (.15, 1.00), (0, 1.04)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, seg = lathe_g(prof, N)
    b.add((v, f), [skin if (s // 3) % 2 == 0 else stripe for s in seg])
    b.add(frustum_g(5, .04, .03, .09), nub, X((0, 0, 1.02), (0, -.12, 0)))
    for (x, z) in [(-.16, .45), (.17, .55), (-.10, .75), (.12, .80), (0, .35), (-.18, .60)]:
        disc(b, S, nub, x, z, .022, .015, n=5)
    feet(b, skin, foot, (-.09, .09), leg_r=.05, leg_h=.18, leg_z=.05, foot_sc=(.10, .17, .07))
    for sx in (-1, 1):
        disc(b, S, black, sx * .10, .78, .042, .03, n=8)
    deco_strip(b, S, mouth, [-.08 + .16 * k / 6 for k in range(7)],
               lambda x: .68 + 3.4 * x * x, lambda x: .022 * (1 - (x / .09) ** 2 * .7))
    return b


BUILDERS_1318 = {'Broccoli': build_broccoli, 'Strawberry': build_strawberry,
                 'Pineapple': build_pineapple, 'Peach': build_peach,
                 'Blueberry': build_blueberry, 'Cucumber': build_cucumber}


def _is_1318(name):
    base = name.split('.')[0]
    if base in BUILDERS_1318:
        return True
    if base.endswith('_Rig') and base[:-4] in BUILDERS_1318:
        return True
    return False


def build_1318_all(do_export=True, do_gallery=True, export_only=None):
    for ob in list(bpy.data.objects):
        if _is_1318(ob.name):
            bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes):
        if _is_1318(me.name) and me.users == 0:
            bpy.data.meshes.remove(me)
    for ar in list(bpy.data.armatures):
        if (ar.name.startswith('MobLP ') and ar.name.split(' ')[1] in BUILDERS_1318) and ar.users == 0:
            bpy.data.armatures.remove(ar)
    for a in list(bpy.data.actions):
        if (a.name.startswith('MobLP ') and a.name.split(' ')[1] in BUILDERS_1318) and a.users == 0:
            bpy.data.actions.remove(a)
    old = bpy.data.scenes.get(SCENE_1318)
    if old:
        bpy.data.scenes.remove(old)
    win = bpy.context.window or bpy.context.window_manager.windows[0]
    bpy.ops.scene.new(type='NEW')
    sc = win.scene
    sc.name = SCENE_1318
    obs, arms, tf, tv = [], [], 0, 0
    for name, fn in BUILDERS_1318.items():
        ob, arm = finish(fn(), sc)
        obs.append(ob)
        arms.append(arm)
        if do_export and (export_only is None or name in export_only):
            export(ob, arm)
        f, v = report(ob)
        tf += f
        tv += v
    print(f'TOTAL1318 faces={tf} verts={tv}')
    if do_gallery:
        gallery(sc, arms, -25, 'mobs_1318_front.png')
        gallery(sc, arms, 155, 'mobs_1318_back.png')
        walk_sheet(sc, arms, frames=(6, 18), prefix='mobs_1318_walk')
    return obs
