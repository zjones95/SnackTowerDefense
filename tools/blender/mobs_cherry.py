"""Wave 6 Cherry mob (Fast, small and quick).

Run through Blender MCP:
    exec(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_cherry.py').read())
    build_cherry_all()

Loads the shared helpers from mobs_first5.py (Builder, primitives, surfaces,
feet, rig, export, gallery, walk_sheet), then builds one mob: Cherry. Round red
body, LONG GREEN STEM curving up (no leaf), small black disc eyes, smile, stubby
feet. Rigged + 24-frame walk loop, exported to BOTH Unity mirrors.
"""
from pathlib import Path

exec(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_first5.py').read())

CHERRY_SCENE = 'Mobs Cherry'
TARGET_HEIGHT['Cherry'] = 1.15
WALK['Cherry'] = dict(swing=32, lift=.050, bob=.024, roll=4.5, nod=2.5)


def build_cherry():
    b = Builder('Cherry')
    red = b.mat('red', '#D81F2A', .55)
    darkred = b.mat('leg', '#A0151F', .6)
    black = b.mat('eye', '#262322', .4)
    mouth = b.mat('mouth', '#6B1517', .5)
    brown = b.mat('foot', '#6E4A22', .7)
    green = b.mat('stem', '#3F9E3E', .6)
    prof = [(0, .14), (.24, .14), (.36, .22), (.42, .34), (.43, .48),
            (.38, .64), (.28, .76), (.14, .81), (0, .80)]
    N = 12
    S = LatheSurf(prof[:-1], N)
    v, f, _ = lathe_g(prof, N)
    b.add((v, f), red)
    # long green stem curving up and slightly back (+Y), no leaf
    stem_pts = [(0, 0, .78), (0, .03, .95), (0, .12, 1.08), (0, .26, 1.14)]
    stem_rad = [(.052, .052), (.046, .046), (.040, .040), (.028, .028)]
    b.add(tube_g(stem_pts, stem_rad, 6), green)
    feet(b, darkred, brown, (-.13, .13))
    for sx in (-1, 1):
        disc(b, S, black, sx * .14, .58, .05, .03, n=8)
    deco_strip(b, S, mouth, [-.10 + .20 * k / 6 for k in range(7)],
               lambda x: .47 + 3.4 * x * x, lambda x: .026 * (1 - (x / .11) ** 2 * .7))
    return b


def build_cherry_all(do_export=True, do_gallery=True):
    for ob in list(bpy.data.objects):
        if ob.name.startswith('Cherry') or ob.name.startswith('MobLP Cherry'):
            bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes):
        if me.name.startswith('Cherry') and me.users == 0:
            bpy.data.meshes.remove(me)
    for ar in list(bpy.data.armatures):
        if ar.name.startswith('MobLP Cherry') and ar.users == 0:
            bpy.data.armatures.remove(ar)
    for a in list(bpy.data.actions):
        if (a.name.startswith('Walk') or a.name.startswith('MobLP Cherry')) and a.users == 0:
            bpy.data.actions.remove(a)
    old = bpy.data.scenes.get(CHERRY_SCENE)
    if old:
        bpy.data.scenes.remove(old)
    win = bpy.context.window or bpy.context.window_manager.windows[0]
    bpy.ops.scene.new(type='NEW')
    sc = win.scene
    sc.name = CHERRY_SCENE
    ob, arm = finish(build_cherry(), sc)
    if do_export:
        export(ob, arm)
    f, v = report(ob)
    print(f'CHERRY faces={f} verts={v}')
    if do_gallery:
        gallery(sc, [arm], -28, 'mob_cherry_front.png')
        gallery(sc, [arm], 152, 'mob_cherry_back.png')
        walk_sheet(sc, [arm], frames=(0, 6, 12, 18), prefix='mob_cherry_walk')
    return ob, arm
