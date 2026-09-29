"""Waves 7-12 mobs: Potato, Orange, Grapes, Pumpkin (boss), Corn, Tomato.

Run through Blender MCP:
    exec(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_wave7_12.py').read())
    build_712_all()

Loads shared helpers from mobs_first5.py (Builder, primitives, surfaces, feet,
rig, export, gallery, walk_sheet). Same conventions: one joined mesh named the
MobDef.id, ~1 unit tall, base z=0, forward -Y, flat-shaded, rigged + 24-frame
walk loop, exported to BOTH Unity mirrors.
"""
from pathlib import Path

from mathutils import Quaternion

exec(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_first5.py').read())

SCENE_712 = 'Mobs 7-12'
TARGET_HEIGHT.update({'Potato': 1.1, 'Orange': 1.0, 'Grapes': 1.1,
                      'Pumpkin': 1.4, 'Corn': 1.1, 'Tomato': 1.0})
WALK.update({
    'Potato': dict(swing=20, lift=.040, bob=.026, roll=7.0, nod=2.0),
    'Orange': dict(swing=26, lift=.045, bob=.022, roll=5.0, nod=2.0),
    'Grapes': dict(swing=32, lift=.050, bob=.024, roll=4.5, nod=2.5),
    'Pumpkin': dict(swing=18, lift=.040, bob=.020, roll=4.5, nod=1.5),
    'Corn': dict(swing=22, lift=.045, bob=.024, roll=6.0, nod=2.0),
    'Tomato': dict(swing=26, lift=.045, bob=.022, roll=5.0, nod=2.0),
})


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


def ribbed_lathe_g(profile, n, ribs, amp):
    """Lathe with radial ribs. Returns verts, faces, seg index per face."""
    def rr(r, i):
        return r * (1 + amp * math.cos(ribs * 2 * PI * i / n))
    verts, rings, faces, seg = [], [], [], []
    for r, z in profile:
        if r <= 1e-6:
            verts.append((0, 0, z))
            rings.append(len(verts) - 1)
        else:
            idx = []
            for i in range(n):
                a = 2 * PI * i / n
                q = rr(r, i)
                verts.append((q * math.cos(a), q * math.sin(a), z))
                idx.append(len(verts) - 1)
            rings.append(idx)
    for k in range(len(profile) - 1):
        A, B = rings[k], rings[k + 1]
        if isinstance(A, int) and isinstance(B, int):
            continue
        for i in range(n):
            i2 = (i + 1) % n
            if isinstance(A, int):
                faces.append((A, B[i], B[i2]))
            elif isinstance(B, int):
                faces.append((A[i], A[i2], B))
            else:
                faces.append((A[i], A[i2], B[i2], B[i]))
            seg.append(i)
    return verts, faces, seg


class RibSurf:
    """Facet-exact front point on a ribbed lathe (front is -Y)."""

    def __init__(self, profile, n, ribs, amp):
        self.p, self.n, self.ribs, self.amp = profile, n, ribs, amp

    def r(self, z, i):
        for (r0, z0), (r1, z1) in zip(self.p, self.p[1:]):
            if abs(z1 - z0) < 1e-9:
                continue
            if min(z0, z1) - 1e-9 <= z <= max(z0, z1) + 1e-9:
                r = r0 + (r1 - r0) * (z - z0) / (z1 - z0)
                return r * (1 + self.amp * math.cos(self.ribs * 2 * PI * i / self.n))
        return 0.0

    def pt(self, x, z, off=0.0):
        ys = []
        for i in range(self.n):
            a0, a1 = 2 * PI * i / self.n, 2 * PI * (i + 1) / self.n
            r0, r1 = self.r(z, i), self.r(z, i + 1 if i + 1 < self.n else 0)
            x0, y0, x1, y1 = r0 * math.cos(a0), r0 * math.sin(a0), r1 * math.cos(a1), r1 * math.sin(a1)
            if min(x0, x1) - 1e-9 <= x <= max(x0, x1) + 1e-9 and abs(x1 - x0) > 1e-9:
                ys.append(y0 + (y1 - y0) * (x - x0) / (x1 - x0))
        y = min(ys) if ys else 0
        return Vector((x, y - off, z))


# ---------------------------------------------------------------- builders
def build_potato():
    b = Builder('Potato')
    skin = b.mat('skin', '#C49A63', .65)
    spot = b.mat('spot', '#8A6238', .7)
    black = b.mat('eye', '#262322', .4)
    lid = b.mat('lid', '#C49A63', .65)
    mouth = b.mat('mouth', '#5A3A1E', .5)
    foot = b.mat('foot', '#7A5636', .7)
    prof = [(0, .14), (.26, .14), (.38, .24), (.43, .40), (.40, .58), (.32, .74), (.18, .86), (0, .88)]
    N = 10
    S = LatheSurf(prof[:-1], N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), skin)
    # lumps: squashed low-poly spheres intersecting the body
    for (x, y, z, sx, sy, sz) in [(.30, .10, .45, .16, .12, .18), (-.30, .05, .55, .14, .12, .20),
                                  (.10, .28, .70, .15, .10, .14), (-.15, -.28, .40, .16, .10, .16),
                                  (.05, -.30, .60, .13, .10, .15)]:
        b.add(sphere_g(7, 5), skin, X((x, y, z), (0, 0, 0), (sx, sy, sz)))
    feet(b, skin, foot, (-.13, .13))
    for sx in (-1, 1):
        disc(b, S, black, sx * .13, .62, .048, .03, n=8)
        c = S.pt(sx * .13, .62, 0)
        b.add(sphere_g(7, 4), lid, X((c.x, c.y - .010, c.z + .055), (0, 0, 0), (.085, .045, .05)))
    deco_strip(b, S, mouth, [-.08 + .16 * k / 6 for k in range(7)],
               lambda x: .48 + 3.0 * x * x, lambda x: .022 * (1 - (x / .09) ** 2 * .7))
    for (x, z) in [(-.22, .40), (.24, .44), (.02, .30), (-.10, .72)]:
        disc(b, S, spot, x, z, .030, .015, n=5)
    return b


def build_orange():
    b = Builder('Orange')
    skin = b.mat('skin', '#F59A1F', .55)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#7A3A10', .5)
    green = b.mat('nub', '#4E9C43', .6)
    foot = b.mat('foot', '#B25A12', .7)
    R, zc = .42, .57
    prof = [(0, zc - R)] + [(R * math.cos(math.radians(p)), zc + R * math.sin(math.radians(p)))
                            for p in (-60, -30, 0, 30, 60)] + [(0, zc + R)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), skin)
    b.add(frustum_g(5, .05, .035, .10), green, X((0, 0, zc + R - .02), (0, -.15, 0)))
    feet(b, skin, foot, (-.13, .13))
    for sx in (-1, 1):
        disc(b, S, black, sx * .14, .62, .05, .03, n=8)
    deco_strip(b, S, mouth, [-.10 + .20 * k / 6 for k in range(7)],
               lambda x: .48 + 3.2 * x * x, lambda x: .026 * (1 - (x / .11) ** 2 * .7))
    return b


def build_grapes():
    b = Builder('Grapes')
    skin = b.mat('skin', '#8A4FC9', .5)
    dark = b.mat('shade', '#5F3391', .55)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#3A1C55', .5)
    brown = b.mat('stem', '#7B4B23', .7)
    foot = b.mat('foot', '#4A2568', .7)
    R = .15
    # grapevine bunch: distinct round bulbs with real gaps between them, wide
    # top narrowing to a single tip bulb. Spacing ~ diameter so each bulb reads.
    spots = [(-.28, .06, .74), (0, -.08, .76), (.28, .06, .74),
             (-.26, -.04, .48), (0, -.12, .48), (.26, -.04, .48),
             (-.12, -.06, .26), (.12, .08, .26),
             (0, 0, .16)]
    for k, c in enumerate(spots):
        b.add(sphere_g(6, 4), skin if k % 2 == 0 else dark, X(c, (0, 0, 0), (R, R, R)))
    # woody vine stem from the top of the bunch
    b.add(tube_g([(0, -.02, .86), (0, .02, .98), (0, .08, 1.08)], [(.042, .042), (.034, .034), (.024, .024)], 5), brown)
    S = GrapeSurf((0, -.12, .48), R)
    feet(b, dark, foot, (-.11, .11), leg_r=.045, leg_h=.18, leg_z=.05, foot_sc=(.10, .17, .07))
    for sx in (-1, 1):
        disc(b, S, black, sx * .065, .51, .030, .035, n=8, off=-.006)
    deco_strip(b, S, mouth, [-.05 + .10 * k / 6 for k in range(7)],
               lambda x: .44 + 4.0 * x * x, lambda x: .018 * (1 - (x / .06) ** 2 * .65), depth=.04)
    return b


def build_pumpkin():
    b = Builder('Pumpkin')
    skin = b.mat('skin', '#EB7A1A', .55)
    black = b.mat('eye', '#262322', .4)
    brow = b.mat('brow', '#8A4A16', .6)
    mouth = b.mat('mouth', '#5A2A0E', .5)
    brown = b.mat('stem', '#7B4B23', .7)
    foot = b.mat('foot', '#8A4A16', .7)
    R, Rz, zc = .48, .44, .58
    prof = [(0, zc - Rz)] + [(R * math.cos(math.radians(p)), zc + Rz * math.sin(math.radians(p)))
                             for p in (-67.5, -45, -22.5, 0, 22.5, 45, 67.5)] + [(0, zc + Rz)]
    N, RIBS, AMP = 20, 10, .045
    S = RibSurf(prof, N, RIBS, AMP)
    v, f, _ = ribbed_lathe_g(prof, N, RIBS, AMP)
    b.add((v, f), skin)
    b.add(tube_g([(0, 0, 1.0), (0, .03, 1.12), (0, .08, 1.20)], [(.095, .095), (.080, .080), (.065, .065)], 6), brown)
    feet(b, skin, foot, (-.20, .20), leg_r=.07, leg_h=.22, leg_z=0, foot_sc=(.16, .23, .06), foot_y=-.06, leg_y=-.02)
    for sx in (-1, 1):
        disc(b, S, black, sx * .17, .66, .055, .03, n=8)
        c = S.pt(sx * .17, .72, 0)
        b.add(box_g(), brow, X((c.x, c.y - .012, c.z), (0, 0, sx * -.42), (.15, .030, .04)))
    xs = [-.12 + .24 * k / 6 for k in range(7)]
    deco_strip(b, S, mouth, xs, lambda x: .50 - 2.2 * x * x, lambda x: .024, depth=.04)
    return b


def build_corn():
    b = Builder('Corn')
    kernel = b.mat('kernel', '#F5D62F', .55)
    husk = b.mat('husk', '#3F9E3E', .6)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#7A5A10', .5)
    footm = b.mat('foot', '#8A6414', .7)
    N = 12
    base = [(0, .16), (.20, .16), (.26, .24), (.28, .40), (.26, .60), (.22, .78), (.15, .90), (0, .94)]

    def base_r(z):
        for (r0, z0), (r1, z1) in zip(base, base[1:]):
            if abs(z1 - z0) < 1e-9:
                continue
            if min(z0, z1) - 1e-9 <= z <= max(z0, z1) + 1e-9:
                return r0 + (r1 - r0) * (z - z0) / (z1 - z0)
        return 0.0

    # kernel grid: checkerboard radius bumps (reads as kernels, costs no faces)
    A = .055
    zrings = [.16, .25, .34, .43, .52, .61, .70, .79, .88, .94]
    verts, faces = [], []
    rings = []
    for k, z in enumerate(zrings):
        s2 = 1 if k % 2 == 0 else -1
        if k == len(zrings) - 1:
            verts.append((0, 0, z))
            rings.append(len(verts) - 1)
            continue
        idx = []
        for i in range(N):
            s1 = 1 if (i // 2) % 2 == 0 else -1
            r = base_r(z) * (1 + A * s1 * s2)
            a = 2 * PI * i / N
            verts.append((r * math.cos(a), r * math.sin(a), z))
            idx.append(len(verts) - 1)
        rings.append(idx)
    for k in range(len(zrings) - 1):
        RA, RB = rings[k], rings[k + 1]
        if isinstance(RA, int):
            continue
        for i in range(N):
            i2 = (i + 1) % N
            if isinstance(RB, int):
                faces.append((RA[i], RA[i2], RB))
            else:
                faces.append((RA[i], RA[i2], RB[i2], RB[i]))
    faces.append(tuple(rings[0][::-1]))
    b.add((verts, faces), kernel)
    S = LatheSurf(base, N)
    # top silk tuft only (side husks removed)
    for k in range(3):
        b.add(frustum_g(4, .05, 0, .22), husk,
              X((0, 0, .92), (0, .35, k * 2 * PI / 3 + .3)))
    feet(b, kernel, footm, (-.10, .10), leg_r=.05, leg_h=.18, leg_z=.05, foot_sc=(.11, .18, .07))
    for sx in (-1, 1):
        disc(b, S, black, sx * .11, .64, .045, .04, n=8, off=0)
    deco_strip(b, S, mouth, [-.09 + .18 * k / 6 for k in range(7)],
               lambda x: .54 + 3.2 * x * x, lambda x: .024 * (1 - (x / .10) ** 2 * .7), off=-.005, depth=.05)
    return b


def build_tomato():
    b = Builder('Tomato')
    skin = b.mat('skin', '#E02A20', .5)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#7A1512', .5)
    green = b.mat('calyx', '#3F9E3E', .6)
    foot = b.mat('foot', '#8A1B16', .7)
    R, zc = .42, .55
    prof = [(0, zc - R)] + [(R * math.cos(math.radians(p)), zc + R * math.sin(math.radians(p)))
                            for p in (-60, -30, 0, 30, 60)] + [(0, zc + R)]
    N = 12
    S = LatheSurf(prof, N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), skin)
    for k in range(8):
        a = k * 2 * PI / 8
        # base sits on top of the head (no clipping), tips tilt down to graze it
        q = Quaternion((0, 0, 1), a) @ Quaternion((0, 1, 0), math.radians(18))
        loc = Vector((math.cos(a) * .03, math.sin(a) * .03, zc + R - .01))
        b.add(lens_g(.26, .11, .05), green, Matrix.Translation(loc) @ q.to_matrix().to_4x4())
    feet(b, skin, foot, (-.13, .13))
    for sx in (-1, 1):
        disc(b, S, black, sx * .14, .60, .05, .03, n=8)
    deco_strip(b, S, mouth, [-.10 + .20 * k / 6 for k in range(7)],
               lambda x: .46 + 3.2 * x * x, lambda x: .026 * (1 - (x / .11) ** 2 * .7))
    return b


BUILDERS_712 = {'Potato': build_potato, 'Orange': build_orange, 'Grapes': build_grapes,
                'Pumpkin': build_pumpkin, 'Corn': build_corn, 'Tomato': build_tomato}


def _is_712(name):
    base = name.split('.')[0]
    if base in BUILDERS_712:
        return True
    if base.endswith('_Rig') and base[:-4] in BUILDERS_712:
        return True
    return base.startswith('MobLP712')


def build_712_all(do_export=True, do_gallery=True, export_only=None):
    for ob in list(bpy.data.objects):
        if _is_712(ob.name):
            bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes):
        if _is_712(me.name) and me.users == 0:
            bpy.data.meshes.remove(me)
    for ar in list(bpy.data.armatures):
        if (ar.name.startswith('MobLP ') and ar.name.split(' ')[1] in BUILDERS_712) and ar.users == 0:
            bpy.data.armatures.remove(ar)
    for a in list(bpy.data.actions):
        if (a.name.startswith('MobLP ') and a.name.split(' ')[1] in BUILDERS_712) and a.users == 0:
            bpy.data.actions.remove(a)
    old = bpy.data.scenes.get(SCENE_712)
    if old:
        bpy.data.scenes.remove(old)
    win = bpy.context.window or bpy.context.window_manager.windows[0]
    bpy.ops.scene.new(type='NEW')
    sc = win.scene
    sc.name = SCENE_712
    obs, arms, tf, tv = [], [], 0, 0
    for name, fn in BUILDERS_712.items():
        ob, arm = finish(fn(), sc)
        obs.append(ob)
        arms.append(arm)
        if do_export and (export_only is None or name in export_only):
            export(ob, arm)
        f, v = report(ob)
        tf += f
        tv += v
    print(f'TOTAL712 faces={tf} verts={tv}')
    if do_gallery:
        gallery(sc, arms, -25, 'mobs_712_front.png')
        gallery(sc, arms, 155, 'mobs_712_back.png')
        walk_sheet(sc, arms, frames=(6, 18), prefix='mobs_712_walk')
    return obs
