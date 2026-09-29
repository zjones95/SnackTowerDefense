"""Waves 25-30 mobs: Coconut (boss), Mango, Raspberry, Cauliflower, Beetroot
(no taproot), Dragonfruit (boss).

Run through Blender MCP:
    exec(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_wave25_30.py').read())
    build_2530_all()

Loads shared helpers from mobs_first5.py (Builder, primitives, surfaces, feet,
rig, export, gallery, walk_sheet). Same conventions: one joined mesh named the
MobDef.id, base z=0, forward -Y, flat-shaded, rigged + 24-frame walk loop,
exported to BOTH Unity mirrors.
"""
from pathlib import Path

from mathutils import Quaternion

exec(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_first5.py').read())


class GrapeSurf:
    """Front point on a sphere (front is -Y)."""

    def __init__(self, c, r):
        self.c = Vector(c)
        self.r = r

    def pt(self, x, z, off=0.0):
        dx, dz = x - self.c.x, z - self.c.z
        d2 = dx * dx + dz * dz
        y = self.c.y - math.sqrt(max(self.r * self.r - d2, 1e-6)) - off
        return Vector((x, y, z))


SCENE_2530 = 'Mobs 25-30'
TARGET_HEIGHT.update({'Coconut': 1.4, 'Mango': 1.05, 'Raspberry': 1.0,
                      'Cauliflower': 1.2, 'Beetroot': 1.1, 'Dragonfruit': 1.4})
WALK.update({
    'Coconut': dict(swing=16, lift=.035, bob=.020, roll=4.0, nod=1.5),
    'Mango': dict(swing=26, lift=.045, bob=.022, roll=5.0, nod=2.0),
    'Raspberry': dict(swing=32, lift=.050, bob=.024, roll=4.5, nod=2.5),
    'Cauliflower': dict(swing=20, lift=.040, bob=.026, roll=4.0, nod=2.0),
    'Beetroot': dict(swing=22, lift=.045, bob=.024, roll=6.0, nod=2.0),
    'Dragonfruit': dict(swing=22, lift=.045, bob=.022, roll=4.0, nod=2.5),
})


# ---------------------------------------------------------------- builders
def build_coconut():
    b = Builder('Coconut')
    husk = b.mat('husk', '#8A5A2E', .65)
    hair = b.mat('hair', '#5A3A1A', .7)
    black = b.mat('eye', '#262322', .4)
    brow = b.mat('brow', '#3A2612', .6)
    mouth = b.mat('mouth', '#2A1A0E', .5)
    foot = b.mat('foot', '#6B4423', .7)
    R, zc = .48, .62
    prof = [(0, zc - R)] + [(R * math.cos(math.radians(p)), zc + R * math.sin(math.radians(p)))
                            for p in (-60, -30, 0, 30, 60)] + [(0, zc + R)]
    N = 14
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), husk)
    # shaggy hair tufts on the upper hemisphere (skip the face window)
    zup = Vector((0, 0, 1))
    for (z, n) in [(.85, 10), (1.00, 8), (1.10, 5)]:
        r = S.r(z)
        for k in range(n):
            a = (k + .3 * n / 8) * 2 * PI / n
            to_front = abs((a + PI / 2 + PI) % (2 * PI) - PI)
            if to_front < .55 and z < 1.02:
                continue
            outward = Vector((math.cos(a), math.sin(a), .5)).normalized()
            q = zup.rotation_difference(outward)
            loc = Vector((r * math.cos(a), r * math.sin(a), z)) + outward * .01
            b.add(frustum_g(4, .06, 0, .16), hair,
                  Matrix.Translation(loc) @ q.to_matrix().to_4x4())
    # three germ pores: two as stern eyes' surround + one center pore
    feet(b, husk, foot, (-.20, .20), leg_r=.07, leg_h=.22, leg_z=0, foot_sc=(.16, .23, .06), foot_y=-.06, leg_y=-.02)
    for sx in (-1, 1):
        disc(b, S, black, sx * .17, .68, .052, .03, n=8)
        c = S.pt(sx * .17, .76, 0)
        b.add(box_g(), brow, X((c.x, c.y - .012, c.z), (0, 0, sx * -.35), (.14, .030, .04)))
    disc(b, S, black, 0, .80, .045, .025, n=8)
    xs = [-.11 + .22 * k / 6 for k in range(7)]
    deco_strip(b, S, mouth, xs, lambda x: .50 - 2.4 * x * x, lambda x: .024, depth=.04)
    return b


def build_mango():
    b = Builder('Mango')
    skin = b.mat('skin', '#F59A28', .55)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#8A4A18', .5)
    brown = b.mat('stem', '#7B4B23', .7)
    leaf = b.mat('leaf', '#4E9C43', .6)
    foot = b.mat('foot', '#C06A20', .7)
    R, zc = .40, .50
    prof = [(0, .14), (.30, .14), (.40, .28), (.42, .48), (.37, .68), (.27, .84), (.13, .92), (0, .94)]
    N = 12
    S0 = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)

    # mango lean: squash x, stretch z, shear top toward +X (kidney tilt).
    # facets stay exact: the face surface applies the inverse map first.
    def T(p):
        return Vector((p.x * .92 + (p.z - zc) * .15, p.y * .92, zc + (p.z - zc) * 1.08))

    class MangoSurf:
        def pt(self, x, z, off=0.0):
            zb = zc + (z - zc) / 1.08
            xb = (x - (z - zc) * .15) / .92
            return T(S0.pt(xb, zb, off))

    S = MangoSurf()
    v = [tuple(T(Vector(p))) for p in v]
    b.add((v, f), skin)
    top = T(Vector((0, 0, .94)))
    b.add(lens_g(.20, .10, .03), leaf, X((top.x + .06, top.y, top.z - .01), (0, -.12, .5)))
    feet(b, skin, foot, (-.12, .12))
    for sx in (-1, 1):
        disc(b, S, black, sx * .13, .60, .048, .03, n=8)
    deco_strip(b, S, mouth, [-.09 + .18 * k / 6 for k in range(7)],
               lambda x: .47 + 3.2 * x * x, lambda x: .024 * (1 - (x / .10) ** 2 * .7))
    return b


def build_raspberry():
    b = Builder('Raspberry')
    skin = b.mat('skin', '#E8355A', .5)
    dark = b.mat('shade', '#B02040', .55)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#6B1028', .5)
    green = b.mat('calyx', '#3F9E3E', .6)
    foot = b.mat('foot', '#A02038', .7)
    R = .13
    # drupelet cap: rings of small bulbs, widest at middle
    spots = [(0, -.06, .78), (-.20, .05, .74), (.20, .05, .74),
             (-.30, 0, .58), (-.10, -.08, .56), (.10, .08, .56), (.30, 0, .58),
             (-.22, -.02, .38), (0, -.10, .36), (.22, -.02, .38),
             (-.10, 0, .22), (.10, 0, .22)]
    for k, c in enumerate(spots):
        b.add(sphere_g(6, 4), skin if k % 2 == 0 else dark, X(c, (0, 0, 0), (R, R, R)))
    for k in range(5):
        a = k * 2 * PI / 5
        q = Quaternion((0, 0, 1), a) @ Quaternion((0, 1, 0), math.radians(30))
        loc = Vector((math.cos(a) * .05, math.sin(a) * .05, .86))
        b.add(lens_g(.20, .09, .035), green, Matrix.Translation(loc) @ q.to_matrix().to_4x4())
    S = GrapeSurf((0, -.10, .36), R)
    feet(b, dark, foot, (-.10, .10), leg_r=.045, leg_h=.18, leg_z=.05, foot_sc=(.10, .17, .07))
    for sx in (-1, 1):
        disc(b, S, black, sx * .055, .39, .028, .032, n=8, off=-.006)
    deco_strip(b, S, mouth, [-.045 + .09 * k / 6 for k in range(7)],
               lambda x: .33 + 4.0 * x * x, lambda x: .016 * (1 - (x / .055) ** 2 * .65), depth=.04)
    return b


def build_cauliflower():
    b = Builder('Cauliflower')
    curd = b.mat('curd', '#EDE6CC', .6)
    curd2 = b.mat('curd_dark', '#D4C8A4', .65)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#6B5A3A', .5)
    leaf = b.mat('leaf', '#5AA04A', .6)
    foot = b.mat('foot', '#8A8A5A', .7)
    # bumpy curd head: cream bud cluster on a short stalk
    buds = [(0, 0, .80, .30)] + [(math.cos(a) * .28, math.sin(a) * .28, .72, .21)
                                 for a in [k * 2 * PI / 7 for k in range(7)]]
    buds += [(math.cos(a) * .38, math.sin(a) * .38, .55, .16) for a in
             [(k + .5) * 2 * PI / 7 for k in range(7)]]
    buds += [(0, 0, .98, .17)]
    for k, (x, y, z, r) in enumerate(buds):
        b.add(sphere_g(6, 4), curd if k % 2 == 0 else curd2, X((x, y, z), (0, 0, 0), (r, r, r)))
    b.add(frustum_g(8, .22, .28, .30), curd2, X((0, 0, .14)))
    # leaves cupping the head (kept high: far-low leaves get dragged underground by body roll)
    for k in range(5):
        a = k * 2 * PI / 5 + .3
        q = Quaternion((0, 0, 1), a) @ Quaternion((0, 1, 0), math.radians(55))
        loc = Vector((math.cos(a) * .32, math.sin(a) * .32, .44))
        b.add(lens_g(.30, .15, .04), leaf, Matrix.Translation(loc) @ q.to_matrix().to_4x4())
    # face lives on the FRONT ring bud: the ring sticks out past the center bud
    S = GrapeSurf((0, -.28, .72), .21)
    feet(b, curd2, foot, (-.13, .13), leg_r=.05, leg_h=.18, leg_z=.05, foot_sc=(.11, .18, .07))
    # coarse bud facets sit ~.028 inside the ideal sphere: protrude well clear
    for sx in (-1, 1):
        disc(b, S, black, sx * .08, .74, .040, .035, n=8, off=-.015)
    xs = [-.08 + .16 * k / 6 for k in range(7)]
    deco_strip(b, S, mouth, xs, lambda x: .66 - 2.6 * x * x, lambda x: .022, depth=.045, off=-.015)
    return b


def build_beetroot():
    b = Builder('Beetroot')
    skin = b.mat('skin', '#A02050', .5)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#4A0E28', .5)
    green = b.mat('leaf', '#2E9E44', .6)
    foot = b.mat('foot', '#701838', .7)
    R, zc = .40, .52
    prof = [(0, zc - R)] + [(R * math.cos(math.radians(p)), zc + R * math.sin(math.radians(p)))
                            for p in (-60, -30, 0, 30, 60)] + [(0, zc + R)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), skin)
    # leafy greens on top only — no taproot
    for k in range(5):
        b.add(frustum_g(4, .055, 0, .30), green, X((0, 0, zc + R - .04), (0, .35, k * 2 * PI / 5 + .2)))
    feet(b, skin, foot, (-.12, .12))
    for sx in (-1, 1):
        disc(b, S, black, sx * .13, .58, .048, .03, n=8)
    xs = [-.09 + .18 * k / 6 for k in range(7)]
    deco_strip(b, S, mouth, xs, lambda x: .44 - 2.6 * x * x, lambda x: .024, depth=.04)
    return b


def build_dragonfruit():
    b = Builder('Dragonfruit')
    skin = b.mat('skin', '#E8357A', .5)
    fin = b.mat('fin', '#8ACB3A', .55)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#7A1040', .5)
    foot = b.mat('foot', '#A02050', .7)
    prof = [(0, .14), (.32, .14), (.42, .32), (.45, .55), (.40, .78), (.30, .96), (0, 1.04)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), skin)
    # swept-back fins: rings of spikes angled toward the back (+Y)
    zup = Vector((0, 0, 1))
    back = Vector((0, 1, -.55)).normalized()
    for (z, n, size) in [(.35, 7, .8), (.58, 8, .9), (.80, 7, .8), (.95, 5, .65)]:
        r = S.r(z)
        for k in range(n):
            a = k * 2 * PI / n + (z * 3.1)
            to_front = abs((a + PI / 2 + PI) % (2 * PI) - PI)
            if to_front < .50 and .45 < z < .75:
                continue
            outward = (Vector((math.cos(a), math.sin(a), 0)) * .35 + back).normalized()
            q = zup.rotation_difference(outward)
            loc = Vector((r * math.cos(a), r * math.sin(a), z)) + outward * .01
            b.add(frustum_g(4, .06 * size, 0, .20 * size), fin,
                  Matrix.Translation(loc) @ q.to_matrix().to_4x4())
    feet(b, skin, foot, (-.16, .16), leg_r=.06, leg_h=.20, leg_z=.02, foot_sc=(.14, .20, .06))
    for sx in (-1, 1):
        disc(b, S, black, sx * .13, .62, .048, .03, n=8)
    deco_strip(b, S, mouth, [-.09 + .18 * k / 6 for k in range(7)],
               lambda x: .50 + 3.0 * x * x, lambda x: .024 * (1 - (x / .10) ** 2 * .7))
    return b


BUILDERS_2530 = {'Coconut': build_coconut, 'Mango': build_mango, 'Raspberry': build_raspberry,
                 'Cauliflower': build_cauliflower, 'Beetroot': build_beetroot,
                 'Dragonfruit': build_dragonfruit}


def _is_2530(name):
    base = name.split('.')[0]
    if base in BUILDERS_2530:
        return True
    if base.endswith('_Rig') and base[:-4] in BUILDERS_2530:
        return True
    return False


def build_2530_all(do_export=True, do_gallery=True, export_only=None):
    for ob in list(bpy.data.objects):
        if _is_2530(ob.name):
            bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes):
        if _is_2530(me.name) and me.users == 0:
            bpy.data.meshes.remove(me)
    for ar in list(bpy.data.armatures):
        if (ar.name.startswith('MobLP ') and ar.name.split(' ')[1] in BUILDERS_2530) and ar.users == 0:
            bpy.data.armatures.remove(ar)
    for a in list(bpy.data.actions):
        if (a.name.startswith('MobLP ') and a.name.split(' ')[1] in BUILDERS_2530) and a.users == 0:
            bpy.data.actions.remove(a)
    old = bpy.data.scenes.get(SCENE_2530)
    if old:
        bpy.data.scenes.remove(old)
    win = bpy.context.window or bpy.context.window_manager.windows[0]
    bpy.ops.scene.new(type='NEW')
    sc = win.scene
    sc.name = SCENE_2530
    obs, arms, tf, tv = [], [], 0, 0
    for name, fn in BUILDERS_2530.items():
        ob, arm = finish(fn(), sc)
        obs.append(ob)
        arms.append(arm)
        if do_export and (export_only is None or name in export_only):
            export(ob, arm)
        f, v = report(ob)
        tf += f
        tv += v
    print(f'TOTAL2530 faces={tf} verts={tv}')
    if do_gallery:
        gallery(sc, arms, -25, 'mobs_2530_front.png')
        gallery(sc, arms, 155, 'mobs_2530_back.png')
        walk_sheet(sc, arms, frames=(6, 18), prefix='mobs_2530_walk')
    return obs
