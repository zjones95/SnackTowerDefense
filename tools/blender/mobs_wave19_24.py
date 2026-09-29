"""Waves 19-24 mobs: Plum, Durian (boss), Onion, Radish, Eggplant, Kiwi (no tuft).

Run through Blender MCP:
    exec(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_wave19_24.py').read())
    build_1924_all()

Loads shared helpers from mobs_first5.py (Builder, primitives, surfaces, feet,
rig, export, gallery, walk_sheet). Same conventions: one joined mesh named the
MobDef.id, base z=0, forward -Y, flat-shaded, rigged + 24-frame walk loop,
exported to BOTH Unity mirrors.
"""
from pathlib import Path

from mathutils import Quaternion

exec(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_first5.py').read())

SCENE_1924 = 'Mobs 19-24'
TARGET_HEIGHT.update({'Plum': 1.0, 'Durian': 1.4, 'Onion': 1.05,
                      'Radish': 1.05, 'Eggplant': 1.15, 'Kiwi': 0.95})
WALK.update({
    'Plum': dict(swing=26, lift=.045, bob=.022, roll=5.0, nod=2.0),
    'Durian': dict(swing=18, lift=.040, bob=.020, roll=4.5, nod=1.5),
    'Onion': dict(swing=26, lift=.045, bob=.022, roll=5.0, nod=2.0),
    'Radish': dict(swing=30, lift=.050, bob=.024, roll=4.5, nod=2.5),
    'Eggplant': dict(swing=20, lift=.040, bob=.026, roll=6.0, nod=2.0),
    'Kiwi': dict(swing=26, lift=.045, bob=.022, roll=5.0, nod=2.0),
})


# ---------------------------------------------------------------- builders
def build_plum():
    b = Builder('Plum')
    skin = b.mat('skin', '#854085', .5)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#3A1C3A', .5)
    brown = b.mat('stem', '#7B4B23', .7)
    foot = b.mat('foot', '#5A2A5A', .7)
    R, zc = .42, .55
    prof = [(0, zc - R)] + [(R * math.cos(math.radians(p)), zc + R * math.sin(math.radians(p)))
                            for p in (-60, -30, 0, 30, 60)] + [(0, zc + R)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), skin)
    b.add(frustum_g(5, .045, .032, .15), brown, X((0, 0, zc + R - .03), (0, -.18, 0)))
    feet(b, skin, foot, (-.13, .13))
    for sx in (-1, 1):
        disc(b, S, black, sx * .14, .60, .05, .03, n=8)
    deco_strip(b, S, mouth, [-.10 + .20 * k / 6 for k in range(7)],
               lambda x: .46 + 3.2 * x * x, lambda x: .026 * (1 - (x / .11) ** 2 * .7))
    return b


def build_durian():
    b = Builder('Durian')
    husk = b.mat('husk', '#A89E3D', .6)
    spike = b.mat('spike', '#7A7526', .65)
    black = b.mat('eye', '#262322', .4)
    brow = b.mat('brow', '#4A451A', .6)
    mouth = b.mat('mouth', '#3A3512', .5)
    foot = b.mat('foot', '#5A5520', .7)
    R, zc = .40, .58
    prof = [(0, .16), (.30, .16), (.38, .30), (.41, .50), (.38, .70), (.30, .86), (0, .94)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), husk)
    # chunky pyramid spikes everywhere except a face window (front, mid height)
    zup = Vector((0, 0, 1))
    for (z, n, tilt) in [(.32, 8, -.35), (.50, 10, 0), (.68, 10, 0), (.82, 8, .35)]:
        r = S.r(z)
        for k in range(n):
            a = (k + .5 / (n / 8)) * 2 * PI / n
            to_front = abs((a + PI / 2 + PI) % (2 * PI) - PI)
            if to_front < .60 and .42 < z < .76:
                continue
            outward = Vector((math.cos(a), math.sin(a), tilt)).normalized()
            q = zup.rotation_difference(outward)
            loc = Vector((r * math.cos(a), r * math.sin(a), z)) + outward * .02
            b.add(frustum_g(4, .085, 0, .22), spike,
                  Matrix.Translation(loc) @ q.to_matrix().to_4x4())
    b.add(frustum_g(4, .09, 0, .24), spike, X((0, 0, .92)))
    feet(b, husk, foot, (-.17, .17), leg_r=.065, leg_h=.20, leg_z=.02, foot_sc=(.15, .22, .06))
    for sx in (-1, 1):
        disc(b, S, black, sx * .13, .60, .05, .035, n=8, off=-.004)
        c = S.pt(sx * .13, .68, 0)
        b.add(box_g(), brow, X((c.x, c.y - .012, c.z), (0, 0, sx * -.42), (.14, .030, .04)))
    xs = [-.10 + .20 * k / 6 for k in range(7)]
    deco_strip(b, S, mouth, xs, lambda x: .47 - 2.6 * x * x, lambda x: .024, depth=.045, off=-.004)
    return b


def build_onion():
    b = Builder('Onion')
    skin = b.mat('skin', '#DBBD9E', .6)
    panel = b.mat('panel', '#C4A37E', .65)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#6B4A30', .5)
    green = b.mat('sprout', '#4E9C43', .6)
    foot = b.mat('foot', '#A08050', .7)
    prof = [(0, .14), (.30, .14), (.40, .30), (.42, .50), (.36, .70), (.22, .86), (.08, .96), (0, .98)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, seg = lathe_g(prof, N)
    # papery panels: alternate shades every 3 segments
    b.add((v, f), [skin if (s // 3) % 2 == 0 else panel for s in seg])
    b.add(frustum_g(4, .045, 0, .20), green, X((0, 0, .94), (0, .30, .3)))
    b.add(frustum_g(4, .040, 0, .17), green, X((0, 0, .94), (0, -.28, 2.6)))
    feet(b, skin, foot, (-.13, .13))
    for sx in (-1, 1):
        disc(b, S, black, sx * .14, .56, .05, .03, n=8)
    deco_strip(b, S, mouth, [-.10 + .20 * k / 6 for k in range(7)],
               lambda x: .43 + 3.2 * x * x, lambda x: .026 * (1 - (x / .11) ** 2 * .7))
    return b


def build_radish():
    b = Builder('Radish')
    red = b.mat('red', '#E03A56', .5)
    white = b.mat('white', '#EDE8DC', .55)
    leaf = b.mat('leaf', '#2E9E44', .6)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#7A1A28', .5)
    foot = b.mat('foot', '#A02030', .7)
    # red upper bulb + white tapering root
    red_prof = [(0, .30), (.34, .32), (.40, .46), (.38, .62), (.30, .76), (0, .84)]
    white_prof = [(.30, .52), (.32, .44), (.22, .30), (.12, .18), (0, .10)]
    N = 12
    S = LatheSurf(red_prof, N)
    v, f, _ = lathe_g(red_prof, N)
    b.add((v, f), red)
    v2, f2, _ = lathe_g(white_prof, N)
    b.add((v2, f2), white)
    for k in range(4):
        b.add(frustum_g(4, .055, 0, .26), leaf, X((0, 0, .82), (0, .38, k * 2 * PI / 4 + .4)))
    feet(b, red, foot, (-.12, .12), leg_r=.045, leg_h=.20, leg_z=.04, foot_sc=(.10, .18, .07))
    for sx in (-1, 1):
        disc(b, S, black, sx * .13, .60, .048, .03, n=8)
    deco_strip(b, S, mouth, [-.09 + .18 * k / 6 for k in range(7)],
               lambda x: .48 + 3.4 * x * x, lambda x: .024 * (1 - (x / .10) ** 2 * .7))
    return b


def build_eggplant():
    b = Builder('Eggplant')
    skin = b.mat('skin', '#663380', .45)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#2A1540', .5)
    green = b.mat('calyx', '#3F9E3E', .6)
    foot = b.mat('foot', '#4A2560', .7)
    prof = [(0, .14), (.30, .14), (.40, .28), (.42, .48), (.36, .68), (.26, .84), (.14, .94), (0, .96)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), skin)
    for k in range(7):
        a = k * 2 * PI / 7
        q = Quaternion((0, 0, 1), a) @ Quaternion((0, 1, 0), math.radians(22))
        loc = Vector((math.cos(a) * .04, math.sin(a) * .04, .93))
        b.add(lens_g(.24, .10, .04), green, Matrix.Translation(loc) @ q.to_matrix().to_4x4())
    b.add(frustum_g(5, .05, .035, .14), green, X((0, 0, .92), (0, -.12, 0)))
    feet(b, skin, foot, (-.14, .14), leg_r=.055, leg_h=.18, leg_z=.05, foot_sc=(.12, .19, .07))
    for sx in (-1, 1):
        disc(b, S, black, sx * .13, .58, .048, .03, n=8)
    deco_strip(b, S, mouth, [-.10 + .20 * k / 6 for k in range(7)],
               lambda x: .45 + 3.0 * x * x, lambda x: .026 * (1 - (x / .11) ** 2 * .65))
    return b


def build_kiwi():
    b = Builder('Kiwi')
    skin = b.mat('skin', '#A06A35', .65)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#5A3A18', .5)
    foot = b.mat('foot', '#7A5028', .7)
    R, zc = .38, .50
    prof = [(0, zc - R)] + [(R * math.cos(math.radians(p)), zc + R * math.sin(math.radians(p)))
                            for p in (-60, -30, 0, 30, 60)] + [(0, zc + R)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), skin)
    feet(b, skin, foot, (-.11, .11))
    for sx in (-1, 1):
        disc(b, S, black, sx * .13, .55, .048, .03, n=8)
    deco_strip(b, S, mouth, [-.09 + .18 * k / 6 for k in range(7)],
               lambda x: .42 + 3.2 * x * x, lambda x: .024 * (1 - (x / .10) ** 2 * .7))
    return b


BUILDERS_1924 = {'Plum': build_plum, 'Durian': build_durian, 'Onion': build_onion,
                 'Radish': build_radish, 'Eggplant': build_eggplant, 'Kiwi': build_kiwi}


def _is_1924(name):
    base = name.split('.')[0]
    if base in BUILDERS_1924:
        return True
    if base.endswith('_Rig') and base[:-4] in BUILDERS_1924:
        return True
    return False


def build_1924_all(do_export=True, do_gallery=True, export_only=None):
    for ob in list(bpy.data.objects):
        if _is_1924(ob.name):
            bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes):
        if _is_1924(me.name) and me.users == 0:
            bpy.data.meshes.remove(me)
    for ar in list(bpy.data.armatures):
        if (ar.name.startswith('MobLP ') and ar.name.split(' ')[1] in BUILDERS_1924) and ar.users == 0:
            bpy.data.armatures.remove(ar)
    for a in list(bpy.data.actions):
        if (a.name.startswith('MobLP ') and a.name.split(' ')[1] in BUILDERS_1924) and a.users == 0:
            bpy.data.actions.remove(a)
    old = bpy.data.scenes.get(SCENE_1924)
    if old:
        bpy.data.scenes.remove(old)
    win = bpy.context.window or bpy.context.window_manager.windows[0]
    bpy.ops.scene.new(type='NEW')
    sc = win.scene
    sc.name = SCENE_1924
    obs, arms, tf, tv = [], [], 0, 0
    for name, fn in BUILDERS_1924.items():
        ob, arm = finish(fn(), sc)
        obs.append(ob)
        arms.append(arm)
        if do_export and (export_only is None or name in export_only):
            export(ob, arm)
        f, v = report(ob)
        tf += f
        tv += v
    print(f'TOTAL1924 faces={tf} verts={tv}')
    if do_gallery:
        gallery(sc, arms, -25, 'mobs_1924_front.png')
        gallery(sc, arms, 155, 'mobs_1924_back.png')
        walk_sheet(sc, arms, frames=(6, 18), prefix='mobs_1924_walk')
    return obs
