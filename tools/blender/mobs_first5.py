"""First five mobs: Apple, Carrot, Pear, Banana, Watermelon (boss).

Run through Blender MCP:  exec(open(<this file>).read()); build_all()

Low-poly, flat-shaded, faceted look (matches the approved concept art). Each mob
is ONE mesh object named after its MobDef.id, ~1 unit tall, base on z = 0,
centred in X/Y, forward = Blender -Y (-> Unity +Z). Everything is built from
low-segment lathes / spheres / tubes and written with from_pydata, so it is fully
reproducible. Builds in its own scene, never touching other scenes/objects; only
datablocks prefixed 'MobLP ' are ever cleaned up. Exports BOTH Unity mirrors.
"""
import bpy
import bmesh
import math
from pathlib import Path
from mathutils import Vector, Matrix, Euler

ROOT = Path(r'C:/Users/Desktop/SnackTowerDefense')
OUT_DIRS = [ROOT / 'Assets/Resources/Snack/Mobs', ROOT / 'Assets/Models/Mobs']
PREVIEW_DIR = Path.home() / 'AppData/Local/Temp/opencode'
PI = math.pi
SCENE_NAME = 'Mobs First 5'
TARGET_HEIGHT = {'Apple': 1.0, 'Carrot': 1.1, 'Pear': 1.05, 'Banana': 1.15, 'Watermelon': 1.2}


# ---------------------------------------------------------------- basics
def _lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hexrgb(h):
    h = h.lstrip('#')
    return tuple(_lin(int(h[i:i + 2], 16) / 255) for i in (0, 2, 4))


def X(loc=(0, 0, 0), rot=(0, 0, 0), sc=(1, 1, 1)):
    return (Matrix.Translation(Vector(loc)) @ Euler(rot, 'XYZ').to_matrix().to_4x4()
            @ Matrix.Diagonal((sc[0], sc[1], sc[2], 1)))


class Builder:
    def __init__(self, name):
        self.name = name
        self.verts, self.faces, self.fmat = [], [], []
        self.mats, self.matmap = [], {}
        self.cur_bone = 'body'   # bone that newly added geometry is rigidly skinned to
        self.vbone = []          # bone name per vertex (parallel to self.verts)
        self.legs = []           # (x, y, z) hip pivots, pre-normalisation
        self.ankles = []         # (x, y, z) foot pivots (feet counter-rotate to stay flat)

    def mat(self, key, hexcol, rough=0.6):
        if key not in self.matmap:
            m = bpy.data.materials.new(f'MobLP {self.name} {key}')
            m.use_nodes = True
            bsdf = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
            rgb = hexrgb(hexcol)
            bsdf.inputs['Base Color'].default_value = (*rgb, 1)
            bsdf.inputs['Roughness'].default_value = rough
            bsdf.inputs['Metallic'].default_value = 0.0
            m.diffuse_color = (*rgb, 1)
            self.matmap[key] = len(self.mats)
            self.mats.append(m)
        return self.matmap[key]

    def add(self, geo, mat, xf=None):
        verts, faces = geo[0], geo[1]
        base = len(self.verts)
        if xf is not None:
            verts = [xf @ Vector(v) for v in verts]
        self.verts += [tuple(v) for v in verts]
        self.vbone += [self.cur_bone] * len(verts)
        for k, f in enumerate(faces):
            self.faces.append(tuple(i + base for i in f))
            self.fmat.append(mat[k] if isinstance(mat, list) else mat)


# ---------------------------------------------------------------- primitives
def sphere_g(u, v):
    verts = [(0, 0, 1)]
    for i in range(1, v):
        th = PI * i / v
        for j in range(u):
            a = 2 * PI * j / u
            verts.append((math.sin(th) * math.cos(a), math.sin(th) * math.sin(a), math.cos(th)))
    verts.append((0, 0, -1))
    ring = lambda i, j: 1 + (i - 1) * u + (j % u)
    faces = [(0, ring(1, j), ring(1, j + 1)) for j in range(u)]
    for i in range(1, v - 1):
        for j in range(u):
            faces.append((ring(i, j), ring(i + 1, j), ring(i + 1, j + 1), ring(i, j + 1)))
    bot = len(verts) - 1
    faces += [(bot, ring(v - 1, j + 1), ring(v - 1, j)) for j in range(u)]
    return verts, faces


def frustum_g(n, r1, r2, h):
    verts = [(r1 * math.cos(2 * PI * i / n), r1 * math.sin(2 * PI * i / n), 0) for i in range(n)]
    faces = []
    if r2 > 1e-6:
        verts += [(r2 * math.cos(2 * PI * i / n), r2 * math.sin(2 * PI * i / n), h) for i in range(n)]
        for i in range(n):
            faces.append((i, (i + 1) % n, n + (i + 1) % n, n + i))
        faces.append(tuple(range(n - 1, -1, -1)))
        faces.append(tuple(range(n, 2 * n)))
    else:
        verts.append((0, 0, h))
        for i in range(n):
            faces.append((i, (i + 1) % n, n))
        faces.append(tuple(range(n - 1, -1, -1)))
    return verts, faces


def box_g():
    v = [(x, y, z) for x in (-.5, .5) for y in (-.5, .5) for z in (-.5, .5)]
    idx = lambda x, y, z: (x > 0) * 4 + (y > 0) * 2 + (z > 0)
    f = [(idx(-1, -1, -1), idx(-1, 1, -1), idx(1, 1, -1), idx(1, -1, -1)),
         (idx(-1, -1, 1), idx(1, -1, 1), idx(1, 1, 1), idx(-1, 1, 1)),
         (idx(-1, -1, -1), idx(1, -1, -1), idx(1, -1, 1), idx(-1, -1, 1)),
         (idx(-1, 1, -1), idx(-1, 1, 1), idx(1, 1, 1), idx(1, 1, -1)),
         (idx(-1, -1, -1), idx(-1, -1, 1), idx(-1, 1, 1), idx(-1, 1, -1)),
         (idx(1, -1, -1), idx(1, 1, -1), idx(1, 1, 1), idx(1, -1, 1))]
    return v, f


def lens_g(length, width, thick):
    """Closed leaf: 6-vertex bipyramid along +X."""
    v = [(0, 0, 0), (length, 0, thick * .6), (length * .42, width / 2, thick * .5),
         (length * .42, -width / 2, thick * .5), (length * .42, 0, thick), (length * .42, 0, -thick * .1)]
    f = [(0, 2, 4), (0, 4, 3), (0, 5, 2), (0, 3, 5), (1, 4, 2), (1, 3, 4), (1, 2, 5), (1, 5, 3)]
    return v, f


def lathe_g(profile, n):
    """profile = [(radius, z)...] bottom to top; radius 0 -> pole. Returns verts, faces, seg index per face."""
    verts, rings, faces, seg = [], [], [], []
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
            if isinstance(A, int):
                faces.append((A, B[i], B[i2]))
            elif isinstance(B, int):
                faces.append((A[i], A[i2], B))
            else:
                faces.append((A[i], A[i2], B[i2], B[i]))
            seg.append(i)
    if not isinstance(rings[0], int):
        faces.append(tuple(rings[0][::-1])); seg.append(0)
    if not isinstance(rings[-1], int):
        faces.append(tuple(rings[-1])); seg.append(0)
    return verts, faces, seg


def strip_g(top, bot, depth):
    """Closed thin prism through two polylines (front) extruded +Y by depth (into the body)."""
    n = len(top)
    verts = []
    for k in range(n):
        verts += [tuple(top[k]), tuple(bot[k])]
    for k in range(n):
        verts += [(top[k][0], top[k][1] + depth, top[k][2]), (bot[k][0], bot[k][1] + depth, bot[k][2])]
    B = 2 * n
    faces = []
    for k in range(n - 1):
        t0, b0, t1, b1 = 2 * k, 2 * k + 1, 2 * k + 2, 2 * k + 3
        faces.append((t0, b0, b1, t1))
        faces.append((B + t1, B + b1, B + b0, B + t0))
        faces.append((t0, t1, B + t1, B + t0))
        faces.append((b0, B + b0, B + b1, b1))
    faces.append((0, B, B + 1, 1))
    faces.append((2 * n - 2, 2 * n - 1, B + 2 * n - 1, B + 2 * n - 2))
    return verts, faces


def tube_g(points, radii, sides=6, frames=None, start=0.0):
    """Tapered tube along a polyline. radii: float or (rx, rf) per ring."""
    pts = [Vector(p) for p in points]
    verts, faces = [], []
    for k, c in enumerate(pts):
        T = (pts[min(k + 1, len(pts) - 1)] - pts[max(k - 1, 0)]).normalized()
        if frames:
            S, F = frames[k]
        else:
            ref = Vector((1, 0, 0)) if abs(T.x) < .9 else Vector((0, 1, 0))
            S = T.cross(ref).normalized()
            F = T.cross(S).normalized()
        rx, rf = radii[k] if isinstance(radii[k], (tuple, list)) else (radii[k], radii[k])
        for i in range(sides):
            a = 2 * PI * i / sides + start
            verts.append(tuple(c + S * (rx * math.cos(a)) + F * (rf * math.sin(a))))
    for k in range(len(pts) - 1):
        for i in range(sides):
            i2 = (i + 1) % sides
            faces.append((k * sides + i, k * sides + i2, (k + 1) * sides + i2, (k + 1) * sides + i))
    faces.append(tuple(range(sides - 1, -1, -1)))
    last = (len(pts) - 1) * sides
    faces.append(tuple(range(last, last + sides)))
    return verts, faces


# ---------------------------------------------------------------- surfaces (decals sit exactly on facets)
class LatheSurf:
    def __init__(self, profile, n):
        self.p, self.n = profile, n

    def r(self, z):
        for (r0, z0), (r1, z1) in zip(self.p, self.p[1:]):
            if abs(z1 - z0) < 1e-9:
                continue
            if min(z0, z1) - 1e-9 <= z <= max(z0, z1) + 1e-9:
                return r0 + (r1 - r0) * (z - z0) / (z1 - z0)
        return 0.0

    def pt(self, x, z, off=0.0):
        r = self.r(z)
        ys = []
        for i in range(self.n):
            a0, a1 = 2 * PI * i / self.n, 2 * PI * (i + 1) / self.n
            x0, y0, x1, y1 = r * math.cos(a0), r * math.sin(a0), r * math.cos(a1), r * math.sin(a1)
            if min(x0, x1) - 1e-9 <= x <= max(x0, x1) + 1e-9 and abs(x1 - x0) > 1e-9:
                ys.append(y0 + (y1 - y0) * (x - x0) / (x1 - x0))
        y = min(ys) if ys else -r
        return Vector((x, y - off, z))


class BananaSurf:
    SIDES = 6

    @staticmethod
    def center(t):
        return Vector((0, .10 - .30 * math.sin(PI * t), .20 + .78 * t))

    @staticmethod
    def radius(t):
        rx = .05 + .135 * math.sin(PI * t) ** .7
        return rx, rx * .92

    @classmethod
    def frame(cls, t):
        e = 1e-3
        T = (cls.center(min(t + e, 1)) - cls.center(max(t - e, 0))).normalized()
        S = Vector((1, 0, 0))
        F = S.cross(T).normalized()
        return S, F

    def pt(self, x, z, off=0.0):
        t = min(max((z - .20) / .78, 0), 1)
        c = self.center(t)
        S, F = self.frame(t)
        rx, rf = self.radius(t)
        poly = [(rx * math.cos(2 * PI * i / self.SIDES), rf * math.sin(2 * PI * i / self.SIDES))
                for i in range(self.SIDES + 1)]
        f = 0.0
        for (x0, f0), (x1, f1) in zip(poly, poly[1:]):
            if min(x0, x1) - 1e-9 <= x <= max(x0, x1) + 1e-9 and abs(x1 - x0) > 1e-9:
                f = max(f, f0 + (f1 - f0) * (x - x0) / (x1 - x0))
        return c + S * x + F * (f + off)


# ---------------------------------------------------------------- shared decal helpers
def deco_strip(b, S, mat, xs, zfn, thfn, off=0.006, depth=0.03):
    top, bot = [], []
    for x in xs:
        z, th = zfn(x), thfn(x)
        top.append(S.pt(x, z, off))
        bot.append(S.pt(x, z - th, off))
    b.add(strip_g(top, bot, depth), mat)


def eye_ball(b, S, mat_white, mat_pupil, cx, cz, sc, look=(0, 0), off=0.004, pupil_sc=None):
    c = S.pt(cx, cz, -off)
    b.add(sphere_g(8, 5), mat_white, X(c, (0, 0, 0), sc))
    p = pupil_sc or (sc[0] * .42, sc[1] * .7, sc[2] * .42)
    b.add(sphere_g(6, 4), mat_pupil, X((c.x + look[0], c.y - sc[1] * .75, c.z + look[1]), (0, 0, 0), p))


def disc(b, S, mat, cx, cz, r, h, n=8, off=0.004):
    c = S.pt(cx, cz, -off)
    b.add(frustum_g(n, r, r, h), mat, X(c, (PI / 2, 0, 0)))


def feet(b, mat_leg, mat_foot, xs, leg_r=.048, leg_h=.16, leg_z=.07, foot_sc=(.11, .18, .07), foot_y=-.06, leg_y=-.02):
    for x in xs:
        side = 'a' if x < 0 else 'b'
        b.cur_bone = 'leg_' + side
        b.add(frustum_g(6, leg_r, leg_r, leg_h), mat_leg, X((x, leg_y, leg_z)))
        b.cur_bone = 'foot_' + side
        b.add(box_g(), mat_foot, X((x, foot_y, foot_sc[2] / 2), (0, 0, 0), foot_sc))
        b.legs.append((x, leg_y, leg_z + leg_h))
        b.ankles.append((x, foot_y, foot_sc[2] / 2))
    b.cur_bone = 'body'


# ---------------------------------------------------------------- mobs
def build_apple():
    b = Builder('Apple')
    red = b.mat('red', '#D3302F', .55)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#6B1517', .5)
    brow = b.mat('brow', '#7E1D20', .5)
    brown = b.mat('stem', '#7B4B23', .7)
    leaf = b.mat('leaf', '#4E9C43', .6)
    foot = b.mat('foot', '#8B6A3C', .7)
    prof = [(0, .15), (.20, .15), (.34, .22), (.43, .36), (.45, .52), (.40, .70), (.28, .84), (.13, .88), (0, .83)]
    N = 12
    S = LatheSurf(prof[:-1], N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), red)
    b.add(frustum_g(5, .05, .035, .17), brown, X((0, 0, .80), (0, -.22, 0)))
    b.add(lens_g(.26, .13, .035), leaf, X((.03, 0, .90), (0, -.35, .5)))
    feet(b, foot, foot, (-.15, .15))
    for sx in (-1, 1):
        cx = sx * .15
        disc(b, S, black, cx, .60, .05, .03, n=8)
        c = S.pt(cx, .72, 0)
        b.add(box_g(), brow, X((c.x, c.y - .012, c.z), (0, 0, sx * .38), (.13, .028, .035)))
    deco_strip(b, S, mouth, [-.11 + .22 * k / 6 for k in range(7)],
               lambda x: .40 + 3.2 * x * x, lambda x: .026 * (1 - (x / .12) ** 2 * .7))
    return b


def build_carrot():
    b = Builder('Carrot')
    orange = b.mat('orange', '#F28C28', .55)
    orange2 = b.mat('orange_dark', '#D9701C', .6)
    green = b.mat('leaf', '#3F9E3E', .6)
    green2 = b.mat('leaf_dark', '#25692C', .6)
    black = b.mat('face', '#262322', .4)
    prof = [(0, .14), (.09, .14), (.15, .22), (.20, .33), (.245, .45), (.275, .56), (.295, .66),
            (.27, .73), (.16, .78), (.06, .79), (0, .79)]
    N = 10
    S = LatheSurf(prof[:-1], N)
    v, f, seg = lathe_g(prof, N)
    b.add((v, f), orange)
    for k in range(5):
        b.add(frustum_g(4, .055, 0, .27 + .05 * (k % 2)), green if k % 2 == 0 else green2,
              X((0, 0, .77), (0, .30 + .18 * (k % 2), k * 2 * PI / 5 + .3)))
    feet(b, orange2, orange2, (-.09, .09), leg_r=.045, leg_h=.16, leg_z=.07, foot_sc=(.10, .17, .07))
    for k, (x, z, w) in enumerate([(-.17, .36, .10), (.18, .30, .09), (-.10, .28, .07), (.13, .43, .10), (-.20, .49, .08)]):
        c = S.pt(x, z, -.004)
        b.add(box_g(), orange2, X((c.x, c.y - .004, c.z), (0, 0, (-.35 if x < 0 else .35)), (w, .02, .022)))
    for sx in (-1, 1):
        disc(b, S, black, sx * .10, .655, .05, .03, n=8)
    deco_strip(b, S, black, [-.09 + .18 * k / 6 for k in range(7)],
               lambda x: .555 + 3.4 * x * x, lambda x: .026 * (1 - (x / .10) ** 2 * .7), depth=.04)
    return b


def build_pear():
    b = Builder('Pear')
    body = b.mat('body', '#D2D24A', .6)
    freckle = b.mat('freckle', '#6B4A22', .7)
    brown = b.mat('stem', '#7B4B23', .7)
    ring = b.mat('eye_ring', '#8A5A2B', .6)
    pupil = b.mat('pupil', '#231A12', .4)
    leaf = b.mat('leaf', '#3F7A2E', .6)
    foot = b.mat('foot', '#4F5F26', .7)
    prof = [(0, .14), (.24, .14), (.36, .22), (.41, .34), (.38, .46), (.30, .58), (.22, .70),
            (.18, .80), (.13, .88), (0, .88)]
    N = 12
    S = LatheSurf(prof[:-1], N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), body)
    b.add(frustum_g(5, .04, .03, .17), brown, X((0, 0, .85), (0, -.20, 0)))
    b.add(lens_g(.22, .11, .03), leaf, X((.02, 0, .93), (0, -.30, .45)))
    feet(b, foot, foot, (-.13, .13), leg_r=.045, leg_h=.16, leg_z=.07, foot_sc=(.11, .17, .07))
    for sx in (-1, 1):
        disc(b, S, ring, sx * .11, .42, .065, .03, n=8)
        disc(b, S, pupil, sx * .11, .42, .028, .045, n=6)
    deco_strip(b, S, ring, [-.10 + .20 * k / 6 for k in range(7)],
               lambda x: .30 + 3.4 * x * x, lambda x: .024 * (1 - (x / .11) ** 2 * .7), depth=.04)
    for (x, z) in [(-.24, .28), (.22, .30), (.03, .20), (-.14, .57), (.16, .60), (.0, .72)]:
        disc(b, S, freckle, x, z, .022, .015, n=5)
    return b


def build_banana(top_stem_color=None):
    b = Builder('Banana')
    yellow = b.mat('yellow', '#F6D62F', .55)
    brown = b.mat('brown', '#5A3A18', .7)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#5A2A14', .5)
    S = BananaSurf()
    ts = [k / 8 for k in range(9)]
    pts = [S.center(t) for t in ts]
    rad = [S.radius(t) for t in ts]
    frames = [S.frame(t) for t in ts]
    b.add(tube_g(pts, rad, BananaSurf.SIDES, frames, start=0.0), yellow)
    # stem (top, up-back) and tip (bottom)
    Tt = (S.center(1) - S.center(.97)).normalized()
    stem = b.mat('top_stem', top_stem_color) if top_stem_color else brown
    b.add(frustum_g(4, .07, .055, .13), stem, X(S.center(1) - Tt * .01, Vector((0, 0, 1)).rotation_difference(Tt).to_euler()))
    Tb = (S.center(.03) - S.center(0)).normalized()
    b.add(frustum_g(5, .055, .035, .06), brown, X(S.center(0) + Tb * .01, Vector((0, 0, 1)).rotation_difference(-Tb).to_euler()))
    # static standing legs + flat feet, no arms (matches the other mobs)
    feet(b, yellow, brown, (-.07, .07), leg_r=.05, leg_h=.24, leg_z=.07, foot_sc=(.10, .18, .07), foot_y=-.05, leg_y=-.02)
    # face
    for sx in (-1, 1):
        disc(b, S, black, sx * .075, .69, .038, .03, n=8)
        c = S.pt(sx * .075, .765, 0)
        b.add(box_g(), brown, X((c.x, c.y - .01, c.z), (0, 0, sx * -.32), (.11, .028, .03)))
    deco_strip(b, S, mouth, [-.075 + .15 * k / 6 for k in range(7)],
               lambda x: .585 + 5.0 * x * x, lambda x: .024 * (1 - (x / .085) ** 2 * .65), depth=.04)
    return b


def build_watermelon():
    b = Builder('Watermelon')
    dark = b.mat('stripe_dark', '#1F7A4A', .55)
    light = b.mat('stripe_light', '#86D07A', .55)
    black = b.mat('eye', '#262322', .4)
    lid = b.mat('lid', '#7ACB6C', .55)
    flesh = b.mat('flesh', '#E8484B', .45)
    rim = b.mat('rim', '#8A1B1E', .5)
    seed = b.mat('seed', '#161616', .4)
    brown = b.mat('stem', '#6E4A22', .7)
    R, Rz, zc = .50, .50, .62
    prof = [(0, zc - Rz)] + [(R * math.cos(math.radians(p)), zc + Rz * math.sin(math.radians(p)))
                             for p in (-67.5, -45, -22.5, 0, 22.5, 45, 67.5)] + [(0, zc + Rz)]
    N = 16
    S = LatheSurf(prof, N)
    v, f, seg = lathe_g(prof, N)
    b.add((v, f), [dark if s % 2 == 0 else light for s in seg])
    b.add(frustum_g(5, .06, .04, .12), brown, X((0, 0, 1.08), (0, -.15, 0)))
    for k in range(2):
        b.add(frustum_g(4, .022, 0, .2), light, X((0, 0, 1.10), (0, .9, k * PI + .4)))
    feet(b, light, light, (-.22, .22), leg_r=.07, leg_h=.22, leg_z=0, foot_sc=(.17, .24, .06), foot_y=-.06, leg_y=-.02)
    for sx in (-1, 1):
        cx = sx * .20
        disc(b, S, black, cx, .745, .058, .03, n=8)
        c = S.pt(cx, .76, 0)
        b.add(sphere_g(8, 4), lid, X((c.x, c.y - .012, c.z + .058), (0, 0, 0), (.13, .066, .062)))
    xs = [-.26 + .0433 * k for k in range(13)]
    zt = lambda x: .48 + .08 * (x / .26) ** 2
    zb = lambda x: .30 + .15 * (x / .26) ** 2
    deco_strip(b, S, rim, [x * 1.07 for x in xs], lambda x: zt(x / 1.07) + .025,
               lambda x: zt(x / 1.07) + .025 - (zb(x / 1.07) - .025), off=.002, depth=.04)
    deco_strip(b, S, flesh, xs, zt, lambda x: zt(x) - zb(x), off=.007, depth=.04)
    for x, z in [(-.15, .375), (-.02, .335), (.12, .385), (.0, .445)]:
        c = S.pt(x, z, .012)
        b.add(sphere_g(6, 4), seed, X(c, (0, 0, 0), (.020, .010, .030)))
    return b


BUILDERS = {'Apple': build_apple, 'Carrot': build_carrot, 'Pear': build_pear,
            'Banana': build_banana, 'Watermelon': build_watermelon}


# ---------------------------------------------------------------- finish / export / preview
def cleanup():
    for ob in list(bpy.data.objects):
        if ob.name in BUILDERS or ob.name.startswith('MobLP') or ob.get('moblp_rig'):
            bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes):
        if me.name in BUILDERS and me.users == 0:
            bpy.data.meshes.remove(me)
    for ar in list(bpy.data.armatures):
        if ar.name.startswith('MobLP ') and ar.users == 0:
            bpy.data.armatures.remove(ar)
    for a in list(bpy.data.actions):
        if a.get('moblp_walk') and a.users == 0:
            bpy.data.actions.remove(a)
    for m in list(bpy.data.materials):
        if m.name.startswith('MobLP ') and m.users == 0:
            bpy.data.materials.remove(m)


# Walk-cycle tuning per mob. swing = leg swing (deg), lift = foot lift, bob = body bob,
# roll = side-to-side waddle (deg), nod = pitch nod (deg). One 24-frame loop @ 24 fps.
WALK = {
    'Apple':      dict(swing=26, lift=.045, bob=.022, roll=5.0, nod=2.0),
    'Carrot':     dict(swing=30, lift=.050, bob=.026, roll=6.0, nod=2.5),
    'Pear':       dict(swing=24, lift=.045, bob=.022, roll=6.5, nod=2.0),
    'Banana':     dict(swing=32, lift=.050, bob=.022, roll=4.0, nod=3.0),
    'Watermelon': dict(swing=18, lift=.040, bob=.020, roll=4.5, nod=1.5),
}
WALK_FRAMES = 24
FPS = 24


def rig(ob, sc, hips, ankles, name):
    """Rigid-skin `ob` to root/body/leg_a/leg_b (+ foot_a/foot_b under the legs) and key a
    looping walk. All bones point +Y with roll 0 so their local axes equal world axes
    (X swing, Z bob, Y waddle). Feet counter-rotate the leg swing so they stay flat."""
    ad = bpy.data.armatures.new(f'MobLP {name} Rig')
    arm = bpy.data.objects.new(f'{name}_Rig', ad)
    arm['moblp_rig'] = True
    sc.collection.objects.link(arm)
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    hip_z = sum(h[2] for h in hips) / len(hips)

    def bone(n, head, parent=None):
        eb = ad.edit_bones.new(n)
        eb.head = head
        eb.tail = (head[0], head[1] + .1, head[2])
        eb.roll = 0.0
        if parent:
            eb.parent = ad.edit_bones[parent]
        return eb

    bone('root', (0, 0, 0))
    bone('body', (0, 0, hip_z), 'root')
    for n, h in zip(('leg_a', 'leg_b'), sorted(hips, key=lambda p: p[0])):
        bone(n, h, 'root')
    for n, a, parent in zip(('foot_a', 'foot_b'), sorted(ankles, key=lambda p: p[0]), ('leg_a', 'leg_b')):
        bone(n, a, parent)
    bpy.ops.object.mode_set(mode='OBJECT')

    # weights: every vertex belongs to exactly one bone (rigid skinning)
    groups = {n: ob.vertex_groups.new(name=n) for n in ('body', 'leg_a', 'leg_b', 'foot_a', 'foot_b')}
    per = {}
    for i, n in enumerate(ob['vbone']):
        per.setdefault(n, []).append(i)
    for n, idx in per.items():
        groups[n].add(idx, 1.0, 'REPLACE')
    ob.parent = arm
    mod = ob.modifiers.new('Armature', 'ARMATURE')
    mod.object = arm

    # walk cycle: frames 0..24, frame 24 == frame 0 so it loops seamlessly
    p = WALK[name]
    sc.render.fps = FPS
    sc.frame_start, sc.frame_end = 0, WALK_FRAMES
    pb = arm.pose.bones
    for b_ in pb:
        b_.rotation_mode = 'XYZ'
    sw = math.radians(p['swing'])
    for f in range(WALK_FRAMES + 1):
        ph = 2 * PI * f / WALK_FRAMES
        # +X rotation swings a hanging leg backward (forward is -Y)
        for n, sign in (('leg_a', 1), ('leg_b', -1)):
            swing = sign * sw * math.sin(ph)
            pb[n].rotation_euler = (swing, 0, 0)
            pb[n].location = (0, 0, p['lift'] * max(0.0, -sign * math.cos(ph)))
            pb[n].keyframe_insert('rotation_euler', frame=f)
            pb[n].keyframe_insert('location', frame=f)
            foot = pb['foot_' + n[-1]]
            foot.rotation_euler = (-swing, 0, 0)
            foot.keyframe_insert('rotation_euler', frame=f)
        pb['body'].location = (0, 0, p['bob'] * math.cos(2 * ph))
        pb['body'].rotation_euler = (math.radians(p['nod']) * math.cos(2 * ph), math.radians(p['roll']) * math.sin(ph), 0)
        pb['body'].keyframe_insert('location', frame=f)
        pb['body'].keyframe_insert('rotation_euler', frame=f)
    act = arm.animation_data.action
    act.name = f'MobLP {name} Walk'
    act['moblp_walk'] = True
    sc.frame_set(0)
    return arm


def finish(b, sc):
    me = bpy.data.meshes.new(b.name)
    me.from_pydata(b.verts, [], b.faces)
    me.update()
    for m in b.mats:
        me.materials.append(m)
    for p, mi in zip(me.polygons, b.fmat):
        p.material_index = mi
        p.use_smooth = False
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(me)
    bm.free()
    # bake nothing to re-centre: mesh already in object space with identity transform
    co = [v.co for v in me.vertices]
    mn = Vector((min(c.x for c in co), min(c.y for c in co), min(c.z for c in co)))
    mx = Vector((max(c.x for c in co), max(c.y for c in co), max(c.z for c in co)))
    s = TARGET_HEIGHT[b.name] / (mx.z - mn.z)
    me.transform(Matrix.Scale(s, 4))
    mn, mx = mn * s, mx * s
    T = Vector((-(mn.x + mx.x) / 2, -(mn.y + mx.y) / 2, -mn.z))
    me.transform(Matrix.Translation(T))
    me.update()
    ob = bpy.data.objects.new(b.name, me)
    ob['vbone'] = list(b.vbone)
    sc.collection.objects.link(ob)
    hips = [tuple(Vector(h) * s + T) for h in b.legs]
    ankles = [tuple(Vector(a) * s + T) for a in b.ankles]
    arm = rig(ob, sc, hips, ankles, b.name)
    return ob, arm


def export(ob, arm):
    # glTF names the animation after its action; make ours exactly "Walk" for this export
    act = arm.animation_data.action
    act.name = 'Walk'
    # The exporter unions selection across ALL view layers, and older build
    # sessions leave objects selected in other scenes (e.g. the last mob of a
    # previous batch). Clear selection + active object in every view layer or
    # those strays get exported into this mob's GLB.
    for s in bpy.data.scenes:
        for vl in s.view_layers:
            for o in vl.objects:
                o.select_set(False, view_layer=vl)
            vl.objects.active = None
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    for d in OUT_DIRS:
        d.mkdir(parents=True, exist_ok=True)
        bpy.ops.export_scene.gltf(filepath=str(d / f'{ob.name}.glb'), export_format='GLB', use_selection=True,
                                  export_animations=True, export_skins=True,
                                  export_animation_mode='ACTIVE_ACTIONS')
    act.name = f'MobLP {ob.name} Walk'


def report(ob):
    me = ob.data
    me.calc_loop_triangles()
    bb = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
    size = [max(v[i] for v in bb) - min(v[i] for v in bb) for i in range(3)]
    print(f'{ob.name:11s} faces={len(me.polygons):4d} tris={len(me.loop_triangles):4d} verts={len(me.vertices):4d} '
          f'size(x,y,z)=({size[0]:.2f},{size[1]:.2f},{size[2]:.2f}) minz={min(v[2] for v in bb):.3f}')
    return len(me.polygons), len(me.vertices)


def _set_enum(obj, prop, value):
    valid = [i.identifier for i in obj.bl_rna.properties[prop].enum_items]
    if value in valid:
        setattr(obj, prop, value)
    else:
        print('enum', prop, 'has no', value, valid)


def gallery(sc, obs, yaw_deg, filename):
    for k, ob in enumerate(obs):
        ob.location = ((k - 2) * 1.55, 0, 0)
        ob.rotation_euler = (0, 0, math.radians(yaw_deg))
    cam = sc.camera
    if cam is None:
        cd = bpy.data.cameras.new('MobLP cam')
        cd.lens = 34
        cam = bpy.data.objects.new('MobLP cam', cd)
        sc.collection.objects.link(cam)
        sc.camera = cam
        tgt = bpy.data.objects.new('MobLP target', None)
        tgt.location = (0, 0, .55)
        sc.collection.objects.link(tgt)
        con = cam.constraints.new('TRACK_TO')
        con.target = tgt
        con.track_axis = 'TRACK_NEGATIVE_Z'
        con.up_axis = 'UP_Y'
        sun = bpy.data.objects.new('MobLP sun', bpy.data.lights.new('MobLP sun', 'SUN'))
        sun.data.energy = 3.5
        sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
        sc.collection.objects.link(sun)
        w = bpy.data.worlds.new('MobLP world')
        w.use_nodes = True
        bg = next(n for n in w.node_tree.nodes if n.type == 'BACKGROUND')
        bg.inputs['Color'].default_value = (*hexrgb('#DCDCDC'), 1)
        bg.inputs['Strength'].default_value = 1.0
        sc.world = w
    cam.data.lens = 38
    cam.location = (0, -10.2, 2.7)
    sc.world.node_tree.nodes[[n.name for n in sc.world.node_tree.nodes if n.type == 'BACKGROUND'][0]].inputs['Strength'].default_value = .55
    for o in sc.objects:
        if o.type == 'LIGHT':
            o.data.energy = 2.6
    r = sc.render
    r.resolution_x, r.resolution_y = 2000, 560
    _set_enum(sc.view_settings, 'view_transform', 'Standard')
    if r.engine == 'CYCLES':
        sc.cycles.samples = 24
    r.filepath = str(PREVIEW_DIR / filename)
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    bpy.ops.render.render(write_still=True)
    print('rendered', r.filepath)


def build_all(do_export=True, do_gallery=True, export_only=None):
    cleanup()
    old = bpy.data.scenes.get(SCENE_NAME)
    if old:
        bpy.data.scenes.remove(old)
    win = bpy.context.window or bpy.context.window_manager.windows[0]
    prev = win.scene
    bpy.ops.scene.new(type='NEW')
    sc = win.scene
    sc.name = SCENE_NAME
    obs, arms, tf, tv = [], [], 0, 0
    for name, fn in BUILDERS.items():
        ob, arm = finish(fn(), sc)
        obs.append(ob)
        arms.append(arm)
        if do_export and (export_only is None or name in export_only):
            export(ob, arm)
        f, v = report(ob)
        tf += f
        tv += v
    print(f'TOTAL faces={tf} verts={tv}')
    if do_gallery:
        gallery(sc, arms, -28, 'mobs_first5_front.png')
        gallery(sc, arms, 152, 'mobs_first5_back.png')
    return obs


def walk_sheet(sc, arms, frames=(0, 6, 12, 18), prefix='mobs_walk'):
    """Side-profile renders (mobs face +X) at several phases of the walk cycle."""
    for f in frames:
        sc.frame_set(f)
        gallery(sc, arms, 90, f'{prefix}_f{f:02d}.png')
