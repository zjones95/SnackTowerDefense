"""Approved Caesar Salad / Starfruit / Seed / Artichoke / Asparagus concepts.

Run in Blender: exec(compile(open(path).read(), path, 'exec')); build_batch()
Armless, shared shipped feet + six-bone walk. Creates a fresh scene without
cleaning any other scene. Art only: wave/catalog integration is still pending.
"""
exec(compile(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_wave35_44.py',
                  encoding='utf-8').read(), 'mobs_wave35_44.py', 'exec'))
from mathutils.geometry import intersect_ray_tri
import json

SCENE_NAME = 'Mobs 45-49'
TARGET_HEIGHT.update(CaesarSalad=1.25, Starfruit=1.1, PomegranateSeed=1.0,
                     Artichoke=1.08, Asparagus=1.15)
for name in ('CaesarSalad', 'Starfruit', 'PomegranateSeed', 'Artichoke', 'Asparagus'):
    heavy = name in ('CaesarSalad', 'Artichoke')
    WALK[name] = dict(swing=18 if heavy else 26, lift=.035 if heavy else .045,
                      bob=.012 if heavy else .018, roll=2 if heavy else 3, nod=1.2)


class FacetSurface:
    """Project eyes/mouth onto the actual authored front-facing facets."""
    def __init__(self, b):
        self.tris = [(Vector(b.verts[f[0]]), Vector(b.verts[f[i]]), Vector(b.verts[f[i+1]]))
                     for f in b.faces for i in range(1, len(f)-1)]

    def pt(self, x, z, off=0):
        origin, direction = Vector((x, -5, z)), Vector((0, 1, 0))
        hits = [p for tri in self.tris
                if (p := intersect_ray_tri(*tri, direction, origin, True)) is not None]
        if not hits:
            raise ValueError(f'Face outside body at {x}, {z}')
        p = min(hits, key=lambda p: p.y)
        return Vector((x, p.y-off, z))


def blade(b, mat, loc, height, width, angle=0, lean=0):
    # Closed, faceted pointed leaf, upright; broad face toward -Y.
    verts = [(0,0,0),(-width*.48,0,height*.35),(-width*.38,0,height*.78),
             (0,0,height),(width*.38,0,height*.78),(width*.48,0,height*.35),
             (0,-.055,height*.48),(0,.025,height*.48)]
    faces = [(i,(i+1)%6,6) for i in range(6)]
    faces += [((i+1)%6,i,7) for i in range(6)]
    b.add((verts,faces),mat,X(loc,(lean,0,angle)))


def build_caesar():
    b=Builder('CaesarSalad')
    ceramic=b.mat('ceramic','#EBE5D0'); rim=b.mat('rim','#FFF4DA')
    leaf=b.mat('romaine','#64AE3E'); dark=b.mat('outer leaves','#377C36')
    light=b.mat('leaf highlights','#8DC952'); vein=b.mat('rib','#BEDB81')
    bread=b.mat('croutons','#C58A36'); toast=b.mat('toast edges','#A76B27')
    cheese=b.mat('parmesan','#FFF0B6'); dressing=b.mat('dressing','#F3E3BC')
    bowl=[(0,.19),(.22,.19),(.34,.24),(.46,.39),(.50,.51),
          (.50,.56),(.46,.56),(.44,.49),(.36,.32),(0,.28)]
    b.add(lathe_g(bowl,16),ceramic)
    b.add(lathe_g([(.502,.51),(.513,.525),(.513,.565),(.47,.565),(.47,.54)],16),rim)
    b.add(sphere_g(12,5),dark,X((0,.02,.65),sc=(.37,.30,.26)))
    # Rear leaves form a varied crown, without limb-like sideways extensions.
    for x,y,z,h,w,a in [(-.26,.13,.43,.53,.32,-.3),(.02,.20,.43,.66,.36,.1),
                        (.28,.13,.42,.51,.30,.4),(-.34,-.02,.43,.40,.29,-.45),
                        (.34,-.03,.43,.44,.28,.4)]:
        blade(b,leaf if x<0 else light,(x,y,z),h,w,a,-.13)
    for x,y,z,a in [(-.26,-.13,.73,.3),(.24,-.12,.77,-.2),(.08,.12,.94,.4),
                     (-.05,-.03,.69,-.3)]:
        b.add(box_g(),[bread,toast,bread,bread,toast,bread],X((x,y,z),(.15,a,.2),(.17,.14,.15)))
    for x,y,z,a in [(-.15,-.19,.89,.4),(.26,-.19,.65,-.2),(.17,.10,1.0,-.4)]:
        blade(b,cheese,(x,y,z),.16,.13,a,.4)
    # Foreground romaine leaf holds the face; dressing stays above the eyes.
    blade(b,leaf,(0,-.35,.49),.49,.45,0,0)
    surf=FacetSurface(b)
    deco_strip(b,surf,vein,[-.014,.014],lambda x:.83,lambda x:.15,depth=.012)
    deco_strip(b,surf,dressing,[-.12,-.07,0,.06,.12],
               lambda x:.89+.025*math.sin(x*25),lambda x:.018,depth=.012)
    visage(b,FacetSurface(b),.72,.095,False)
    two_feet(b,dark,dark,.22)
    return b


def build_starfruit():
    b=Builder('Starfruit'); gold=b.mat('gold','#F4C72E')
    pale=b.mat('gold facets','#FFE263'); edge=b.mat('green ridge','#B9CD55')
    # Five longitudinal ribs: each peak has a narrow coloured bevel.
    contour=[]
    for k in range(5):
        for da,r in ((-.045,1),(.045,1),(PI/5,.52)):
            a=-PI/2+2*PI*k/5+da
            contour.append((r*math.cos(a),r*math.sin(a)))
    profile=[(.07,.20),(.29,.28),(.38,.47),(.38,.79),(.28,1.0),(.06,1.09)]
    verts=[(x*r,y*r,z) for r,z in profile for x,y in contour]
    n=len(contour); faces=[]; mats=[]
    for j in range(len(profile)-1):
        for i in range(n):
            faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
            mats.append(edge if i%3==0 else pale if i%3==1 else gold)
    faces += [tuple(range(n-1,-1,-1)),tuple((len(profile)-1)*n+i for i in range(n))]
    mats += [gold,gold]
    b.add((verts,faces),mats)
    visage(b,FacetSurface(b),.67,.09)
    two_feet(b,gold,b.mat('feet','#B6AF42'),.13)
    return b


def build_seed():
    b=Builder('PomegranateSeed'); red=b.mat('ruby','#CA2843')
    pale=b.mat('ruby facets','#E44357'); dark=b.mat('garnet','#8C1733')
    p=[(0,.18),(.17,.21),(.29,.32),(.34,.52),(.28,.73),(.14,.88),(.045,1.0),(0,1.07)]
    v,f,s=lathe_g(p,10)
    b.add((v,f),[pale if i in (6,7) else dark if i in (1,2) else red for i in s])
    visage(b,LatheSurf(p,10),.57,.10)
    two_feet(b,dark,dark,.12)
    return b


def build_artichoke():
    b=Builder('Artichoke'); green=b.mat('heart','#97B45D')
    dark=b.mat('outer bracts','#387D40'); mid=b.mat('bracts','#629947')
    pale=b.mat('inner bracts','#A9C16E')
    p=[(0,.20),(.18,.23),(.26,.38),(.29,.57),(.21,.79),(.08,.97),(0,1.05)]
    b.add(lathe_g(p,12),green)
    for row,(z,r,h,w,n) in enumerate(((.28,.27,.51,.30,7),(.48,.24,.45,.26,7),(.73,.13,.32,.22,5))):
        for i in range(n):
            a=2*PI*i/n
            # Leave a natural face opening between front armour layers.
            if row<2 and math.sin(a)<-.5:
                continue
            blade(b,dark if row==0 else mid if row==1 else pale,
                  (r*math.cos(a),r*math.sin(a),z),h,w,a+PI/2,.24)
    blade(b,mid,(0,-.32,.27),.55,.40)
    visage(b,FacetSurface(b),.60,.095,False)
    two_feet(b,mid,dark,.17)
    return b


def build_asparagus():
    b=Builder('Asparagus'); stalk=b.mat('stalk','#86A846')
    pale=b.mat('stalk facets','#ABC15F'); dark=b.mat('bud','#39763A')
    mid=b.mat('bud facets','#5A953D')
    p=[(0,.20),(.16,.20),(.18,.34),(.16,.71),(.15,1.04),(.11,1.17),(0,1.24)]
    v,f,s=lathe_g(p,10)
    b.add((v,f),[pale if i%3==0 else stalk for i in s])
    for row,(z,r,h,w) in enumerate(((1.01,.11,.24,.15),(1.16,.07,.20,.13))):
        for i in range(5):
            a=2*PI*(i+.4*row)/5
            blade(b,dark if i%2 else mid,(r*math.cos(a),r*math.sin(a),z),h,w,a+PI/2,-.12)
    b.add(frustum_g(5,.067,0,.16),mid,X((0,0,1.27)))
    visage(b,LatheSurf(p,10),.84,.065)
    two_feet(b,stalk,dark,.105)
    return b


BUILDERS={'CaesarSalad':build_caesar,'Starfruit':build_starfruit,
          'PomegranateSeed':build_seed,'Artichoke':build_artichoke,'Asparagus':build_asparagus}


def validate(sc, obs):
    result=[]
    for ob in obs:
        arm=ob.parent
        assert set(b.name for b in arm.data.bones)=={'root','body','leg_a','leg_b','foot_a','foot_b'}
        assert len(ob.modifiers)==1 and all(len(v.groups)==1 for v in ob.data.vertices)
        minimum=100; start=None; loop_error=0
        for frame in range(25):
            sc.frame_set(frame)
            deps=bpy.context.evaluated_depsgraph_get()
            ev=ob.evaluated_get(deps); mesh=ev.to_mesh()
            co=[v.co.copy() for v in mesh.vertices]
            minimum=min(minimum,min(v.z for v in co))
            if frame==0: start=co
            if frame==24: loop_error=max((a-b).length for a,b in zip(start,co))
            ev.to_mesh_clear()
        assert minimum>-.005, (ob.name,'ground penetration',minimum)
        assert loop_error<1e-5, (ob.name,'loop seam',loop_error)
        f,v=report(ob)
        result.append(dict(name=ob.name,faces=f,vertices=v,min_walk_z=minimum,loop_error=loop_error))
    sc.frame_set(0)
    PREVIEW_DIR.mkdir(parents=True,exist_ok=True)
    (PREVIEW_DIR/'mobs_wave45_49_validation.json').write_text(json.dumps(result,indent=2))
    print('VALIDATED',json.dumps(result))


def build_batch():
    old=bpy.data.scenes.get(SCENE_NAME)
    if old:
        if not old.get('late_mob_authoring'):
            raise ValueError('Scene name already in use; preserve it before rebuilding')
        for ob in list(old.objects):
            if len(ob.users_scene)==1: bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.scenes.remove(old)
    sc=bpy.data.scenes.new(SCENE_NAME)
    sc['late_mob_authoring']=True
    bpy.context.window.scene=sc
    obs=[]
    for fn in BUILDERS.values():
        ob,arm=finish(fn(),sc)
        obs.append(ob)
    validate(sc,obs)
    for ob in obs: export(ob,ob.parent)
    print('TOTAL',sum(len(o.data.polygons) for o in obs),'faces',
          sum(len(o.data.vertices) for o in obs),'vertices')
    return obs


def preview(obs):
    sc=bpy.context.scene
    for i,ob in enumerate(obs):
        ob.parent.location=((i-2)*1.42,0,0)
        ob.parent.rotation_euler=(0,0,math.radians(-22))
    world=bpy.data.worlds.new('Late mobs studio'); world.use_nodes=True
    bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
    bg.inputs['Color'].default_value=(.72,.76,.82,1)
    bg.inputs['Strength'].default_value=.65
    sc.world=world
    sc.view_settings.exposure=-.6
    cd=bpy.data.cameras.new('Late mobs camera'); _set_enum(cd,'type','ORTHO')
    cam=bpy.data.objects.new('Late mobs camera',cd); sc.collection.objects.link(cam)
    sc.camera=cam; cam.location=(0,-9,3.6)
    cam.rotation_euler=(Vector((0,0,.60))-cam.location).to_track_quat('-Z','Y').to_euler()
    cd.ortho_scale=7.45
    for name,loc,energy,size in [('Key',(-3,-4,6),650,5),('Fill',(4,-1,4),350,4)]:
        ld=bpy.data.lights.new('Late mobs '+name,'AREA'); ld.energy=energy; ld.size=size
        lamp=bpy.data.objects.new(ld.name,ld); sc.collection.objects.link(lamp); lamp.location=loc
        lamp.rotation_euler=(Vector((0,0,.5))-lamp.location).to_track_quat('-Z','Y').to_euler()
    # A fresh staging floor, never included in selected exports.
    mesh=bpy.data.meshes.new('Late mobs floor')
    mesh.from_pydata([(-20,-20,-.008),(20,-20,-.008),(20,20,-.008),(-20,20,-.008)],[],[(0,1,2,3)])
    floor=bpy.data.objects.new('Late mobs floor',mesh); sc.collection.objects.link(floor)
    mat=bpy.data.materials.new('Late mobs floor'); mat.use_nodes=True
    bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    bsdf.inputs['Base Color'].default_value=(.64,.68,.72,1); bsdf.inputs['Roughness'].default_value=.85
    mesh.materials.append(mat)
    # View-transform is a dynamic enum in this Blender version.
    try: sc.view_settings.view_transform='Standard'
    except TypeError: pass
    sc.render.resolution_x=2200; sc.render.resolution_y=680; sc.render.resolution_percentage=100
    _set_enum(sc.render.image_settings,'file_format','PNG')
    sc.render.filepath=str(PREVIEW_DIR/'mobs_wave45_49_front.png')
    sc.frame_set(0)
    bpy.ops.render.render(write_still=True)
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=7.8
            area.spaces.active.region_3d.view_location=(0,0,.6)
            area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion()
            _set_enum(area.spaces.active.shading,'type','MATERIAL')
    bpy.ops.wm.save_as_mainfile(filepath=str(PREVIEW_DIR/'mobs_wave45_49.blend'))
