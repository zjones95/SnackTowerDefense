"""Remaining snack towers. Run through Blender MCP with build_all().

Creates isolated scenes, preserves existing work, exports both Unity mirrors,
and saves one self-contained editable source per tower. Forward is Blender -Y.
Gum reuses the approved Desktop source on first run; subsequent runs reuse its
checked-in source. All other art is reproducible from this script.
"""
import bpy
import math
import random
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / 'tools/blender/sources'
PREVIEW = Path.home() / 'AppData/Local/Temp/opencode/remaining_towers.png'
PI = math.pi
SCENES = {}


def enum(obj, prop, value):
    valid = [i.identifier for i in obj.bl_rna.properties[prop].enum_items]
    if value not in valid:
        raise ValueError((prop, value, valid))
    setattr(obj, prop, value)


def material(name, rgb, metal=0, rough=.35, alpha=1, emission=0):
    m = bpy.data.materials.new('Snack2 ' + name)
    m.use_nodes = True
    m.diffuse_color = (*rgb, alpha)
    n = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    n.inputs['Base Color'].default_value = (*rgb, 1)
    n.inputs['Metallic'].default_value = metal
    n.inputs['Roughness'].default_value = rough
    n.inputs['Alpha'].default_value = alpha
    if emission:
        n.inputs['Emission Color'].default_value = (*rgb, 1)
        n.inputs['Emission Strength'].default_value = emission
    return m


def mesh(name, vertices, faces, mat, smooth=True):
    me = bpy.data.meshes.new(name)
    me.from_pydata(vertices, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    me.materials.append(mat)
    for p in me.polygons:
        p.use_smooth = smooth
    return ob


def finish(ob, name, mat):
    ob.name = name
    ob.data.materials.append(mat)
    for p in ob.data.polygons:
        p.use_smooth = True
    return ob


def sphere(name, loc, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=16, radius=1, location=loc)
    ob = finish(bpy.context.object, name, mat)
    ob.scale = scale if hasattr(scale, '__len__') else (scale,) * 3
    return ob


def box(name, loc, size, mat, bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = finish(bpy.context.object, name, mat)
    ob.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = ob.modifiers.new('Soft toy edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 3
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in ob.data.polygons:
        p.use_smooth = False
    return ob


def lathe(name, profile, mat, loc=(0, 0, 0), segments=48):
    verts = [(r * math.cos(2*PI*i/segments), r * math.sin(2*PI*i/segments), z)
             for r, z in profile for i in range(segments)]
    faces = []
    for j in range(len(profile)-1):
        for i in range(segments):
            a = j*segments+i
            b = j*segments+(i+1)%segments
            faces.append((a, b, b+segments, a+segments))
    ob = mesh(name, verts, faces, mat)
    ob.location = loc
    return ob


def cyl(name, loc, radius, depth, mat, axis=(0, 0, 1)):
    ob = lathe(name, [(0,-depth/2),(radius,-depth/2),(radius,depth/2),(0,depth/2)], mat, loc)
    ob.rotation_euler = Vector(axis).to_track_quat('Z','Y').to_euler()
    return ob


def torus(name, loc, radius, thickness, mat, axis=(0,0,1)):
    profile = [(radius+thickness*math.cos(2*PI*j/12), thickness*math.sin(2*PI*j/12)) for j in range(13)]
    ob = lathe(name, profile, mat, loc)
    ob.rotation_euler = Vector(axis).to_track_quat('Z','Y').to_euler()
    return ob


def tube(name, points, radius, wall, mat):
    # True hollow bore, with end annuli. No opaque disc across the muzzle.
    points = [Vector(p) for p in points]
    count, sides = len(points), 32
    verts = []
    for r in (radius, radius-wall):
        for j, p in enumerate(points):
            direction = points[min(j+1,count-1)]-points[max(0,j-1)]
            direction.normalize()
            reference=Vector((1,0,0)) if abs(direction.x)<.95 else Vector((0,1,0))
            u=(reference-direction*reference.dot(direction)).normalized()
            v=direction.cross(u)
            verts += [tuple(p+r*(u*math.cos(i*2*PI/sides)+v*math.sin(i*2*PI/sides))) for i in range(sides)]
    faces = []
    for layer in range(2):
        for j in range(count-1):
            for i in range(sides):
                a = layer*count*sides+j*sides+i
                b = layer*count*sides+j*sides+(i+1)%sides
                f = (a,b,b+sides,a+sides)
                faces.append(f if layer==0 else f[::-1])
    for j in (0,count-1):
        for i in range(sides):
            a=j*sides+i; b=j*sides+(i+1)%sides
            faces.append((a,a+count*sides,b+count*sides,b))
    return mesh(name, verts, faces, mat)


def stroke(name, points, radius, mat):
    # A solid round stroke, useful for frosting, embossing and waffle lines.
    ob = tube(name, points, radius, radius*.98, mat)
    for p in (points[0],points[-1]):
        sphere(name+' tip',p,radius,mat)
    return ob


def new_scene(name):
    scene = bpy.data.scenes.new(name+' Tower Studio')
    bpy.context.window.scene = scene
    # Reuse the actual shipped base, including packed plank image textures.
    before = set(scene.objects)
    bpy.ops.import_scene.gltf(filepath=str(ROOT/'Assets/Models/Towers/Gold.glb'))
    imported = set(scene.objects)-before
    bases = []
    for ob in imported:
        if ob.type=='MESH' and (ob.name.split('.')[0] in ('pedestal','rim')):
            ob['export_name'] = ob.name.split('.')[0]
            bases.append(ob)
        else:
            bpy.data.objects.remove(ob, do_unlink=True)
    assert len(bases)==2
    SCENES[name] = scene
    return scene


def build_truck():
    new_scene('IceCreamTruck')
    mint=material('mint enamel',(.18,.72,.64)); cream=material('vanilla',(.98,.85,.57))
    pink=material('strawberry',(.95,.21,.39)); dark=material('rubber',(.025,.035,.055))
    window=material('blue windows',(.055,.22,.32),.25,.15)
    chrome=material('satin chrome',(.7,.8,.83),.7)
    box('Mint truck body',(0,.02,.45),(.53,.69,.30),mint,.065)
    box('Freezer cabin',(0,.15,.66),(.53,.43,.37),cream,.055)
    box('Driver cab',(0,-.245,.585),(.51,.24,.30),mint,.045)
    box('Front windshield',(0,-.372,.64),(.41,.018,.145),window,.022)
    box('Front bumper',(0,-.39,.345),(.55,.055,.07),chrome,.02)
    for x in (-.267,.267):
        for y in (-.235,.235):
            cyl('Chocolate tire',(x,y,.285),.105,.062,dark,(1,0,0))
            cyl('Candy wheel hub',(x+(.034 if x>0 else -.034),y,.285),.06,.012,pink,(1,0,0))
        box('Serving window',(x,.12,.68),(.018,.27,.15),window,.012)
        box('Serving shelf',(x*1.08,.12,.58),(.08,.30,.025),chrome,.008)
        for i in range(5):
            box('Striped awning',(x*1.05,.005+i*.052,.79),(.11,.05,.028),pink if i%2==0 else cream,.004)
        box('Pink side stripe',(x,0,.445),(.012,.57,.047),pink,.003)
    for x in (-.18,.18):
        sphere('Warm headlight',(x,-.375,.49),(.048,.015,.038),cream)
    # Waffle cone-shaped roof launcher, discharging scoops forwards.
    cone=lathe('Roof waffle launcher',[(0,-.12),(.07,-.12),(.115,.12),(.087,.12),(.045,-.10)],cream,(0,.02,.94))
    cone.rotation_euler=(PI/2,0,0)
    for y,r in [(-.04,.103),(.035,.09),(.105,.077)]:
        torus('Waffle band',(0,y,.94),r,.009,pink,(0,1,0))
    sphere('Ready strawberry scoop',(0,-.145,.94),.11,pink)
    for a in range(7):
        theta=a*2*PI/7
        sphere('Scalloped scoop',(math.cos(theta)*.08,-.16,.94+math.sin(theta)*.08),.04,pink)


def build_fondue():
    new_scene('Fondue')
    ceramic=material('Fondue ivory',(.97,.79,.48),.15)
    choc=material('Glossy chocolate',(.19,.052,.021),0,.19)
    gold=material('Fondue brass',(.8,.42,.09),.65,.24)
    red=material('Strawberry dipper',(.85,.035,.055))
    lathe('Fountain basin',[(0,.18),(.23,.18),(.32,.23),(.34,.30),(.34,.34),(.30,.34),(.27,.28),(0,.28)],ceramic)
    cyl('Chocolate pool',(0,0,.302),.298,.028,choc)
    cyl('Fountain column',(0,0,.62),.048,.65,gold)
    for tier,(r,z) in enumerate(((.25,.49),(.18,.71),(.105,.90))):
        lathe('Chocolate cascade',[(0,z+.055),(r*.35,z+.05),(r*.72,z+.02),(r,z),(r*.98,z-.03),(r*.8,z-.037),(r*.3,z+.012),(0,z+.02)],choc)
        torus('Brass tier edge',(0,0,z-.018),r*.96,.012,gold)
        for k in range(7):
            a=2*PI*k/7+tier*.5
            x,y=math.cos(a)*r*.88,math.sin(a)*r*.88
            drop=.075+.03*((k+2*tier)%3)
            stroke('Chocolate ribbon',[(x,y,z),(x*.88,y*.88,z-drop*.5),(x*.79,y*.79,z-drop)],.018,choc)
    sphere('Chocolate crown',(0,0,.95),(.065,.065,.065),choc)
    tube('Chocolate pouring spout',[(0,0,.86),(0,-.09,.88),(0,-.20,.88),(0,-.30,.86)],.043,.012,gold)
    for x,y in ((-.24,-.07),(.20,.13)):
        stroke('Dipping skewer',[(x,y,.26),(x*1.2,y,.62)],.012,ceramic)
        sphere('Chocolate dipped berry',(x*1.2,y,.61),(.05,.045,.065),red)
        sphere('Dipped berry chocolate',(x*1.2,y,.574),(.052,.046,.036),choc)


def coin(name, loc, radius, thick, gold, edge, upright=False):
    axis=(0,-1,0) if upright else (0,0,1)
    cyl(name,loc,radius,thick,gold,axis)
    x,y,z=loc
    if upright:
        torus('Embossed coin border',(x,y-thick/2-.003,z),radius*.83,.009,edge,axis)
        # Raised dollar mark, geometry rather than font dependency.
        pts=[(x+.05,y-thick/2-.015,z+.09),(x-.03,y-thick/2-.015,z+.10),(x-.065,y-thick/2-.015,z+.055),(x+.052,y-thick/2-.015,z-.035),(x+.035,y-thick/2-.015,z-.085),(x-.05,y-thick/2-.015,z-.09)]
        stroke('Raised dollar S',pts,.014,edge)
        stroke('Raised dollar stem',[(x,y-thick/2-.016,z-.13),(x,y-thick/2-.016,z+.13)],.009,edge)
    else:
        torus('Coin milled border',(x,y,z+thick/2),radius*.85,.007,edge)
    for k in range(24):
        a=k*2*PI/24
        if upright:
            p=(x+radius*math.cos(a),y,z+radius*math.sin(a))
            s=(.006,thick*.8,.012)
        else:
            p=(x+radius*math.cos(a),y+radius*math.sin(a),z)
            s=(.008,.008,thick*.8)
        box('Coin edge notch',p,s,edge,0)


def build_gold():
    new_scene('Gold')
    gold=material('Gold coin face',(.95,.50,.045),.72,.24)
    edge=material('Pale gold embossing',(1,.79,.23),.65,.26)
    dark=material('Coin launcher bronze',(.24,.11,.025),.65,.32)
    for x,y,n in ((-.19,.06,5),(.17,.10,7),(0,-.13,3)):
        for j in range(n):
            coin('Stacked gold coin',(x+.008*math.sin(j),y,.202+j*.043),.125,.041,gold,edge)
    coin('Hero upright coin',(0,.055,.735),.265,.07,gold,edge,True)
    box('Coin feeder',(0,-.10,.525),(.22,.26,.12),dark,.025)
    # Broad narrow slot reads as a coin dispenser rather than a gun barrel.
    box('Coin slot housing',(0,-.275,.53),(.25,.15,.09),gold,.018)
    black=material('Coin slot shadow',(.022,.012,.004))
    box('Coin ejection slot',(0,-.352,.53),(.20,.006,.033),black,.007)
    coin('Coin being dispensed',(0,-.32,.525),.082,.016,gold,edge)


def build_boba():
    new_scene('Boba')
    tea=material('Honey milk tea',(.70,.40,.17),0,.4)
    purple=material('Taro lid',(.44,.16,.67),.08,.26)
    cream=material('Boba cream',(.99,.81,.50))
    pearl=material('Tapioca pearls',(.045,.015,.012),0,.16)
    teal=material('Mint straw',(.045,.70,.57),.12,.26)
    # Opaque game-safe cup with exposed pearl-window styling: no sorting issues.
    lathe('Milk tea cup',[(0,.18),(.19,.18),(.205,.22),(.265,.79),(.255,.81),(0,.81)],tea)
    for row in range(3):
        for k in range(11):
            a=2*PI*(k+row*.45)/11
            r=.198+row*.012
            sphere('Visible tapioca pearl',(r*math.cos(a),r*math.sin(a),.245+row*.068),(.039,.039,.038),pearl)
    lathe('Taro sleeve',[(.24,.48),(.263,.67),(.267,.67),(.244,.48),(.24,.48)],purple)
    sphere('Cream label medallion',(0,-.25,.575),(.105,.015,.075),cream)
    for x in (-.039,.039):
        sphere('Boba mascot eye',(x,-.267,.59),.012,pearl)
    stroke('Boba mascot smile',[(-.025,-.266,.561),(0,-.27,.55),(.025,-.266,.561)],.007,pearl)
    lathe('Sealed drink lid',[(0,.795),(.279,.795),(.283,.825),(.27,.85),(0,.85)],purple)
    torus('Lid seal',(0,0,.805),.274,.012,cream)
    tube('Wide bent boba straw',[(.085,.05,.80),(.085,.05,.96),(.085,.035,1.005),(.085,-.015,1.04),(.085,-.12,1.04),(.085,-.39,1.04)],.055,.013,teal)
    torus('Straw mouth edge',(.085,-.39,1.04),.049,.009,cream,(0,1,0))


def build_pizza():
    new_scene('PizzaOven')
    brick=material('Terracotta',(.65,.16,.065),0,.7)
    light=material('Warm bricks',(.86,.32,.12),0,.68)
    mortar=material('Warm mortar',(.8,.65,.43),0,.8)
    dark=material('Oven interior',(.027,.012,.007),0,.9)
    cheese=material('Melted mozzarella',(1,.70,.13),0,.33)
    crust=material('Baked pizza crust',(.78,.37,.09),0,.55)
    red=material('Pepperoni',(.57,.025,.015),0,.4)
    fire=material('Oven embers',(1,.19,.008),0,.4,emission=2)
    box('Oven stone hearth',(0,0,.235),(.64,.64,.11),mortar,.045)
    # Dome with an actual arched entrance removed from its front surface.
    verts=[]; faces=[]; rings=16; seg=48
    for j in range(rings+1):
        phi=(PI/2)*j/rings
        for i in range(seg):
            a=i*2*PI/seg
            verts.append((.31*math.cos(phi)*math.cos(a),.31*math.cos(phi)*math.sin(a),.29+.45*math.sin(phi)))
    for j in range(rings):
        for i in range(seg):
            ids=(j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i)
            p=sum((Vector(verts[k]) for k in ids),Vector())/4
            inside=p.y<0 and abs(p.x)<.185 and p.z<.43+math.sqrt(max(0,.185**2-p.x**2))
            if not inside: faces.append(ids)
    dome=mesh('Brick oven dome',verts,faces,brick)
    dome.data.materials.append(light)
    for p in dome.data.polygons:
        p.material_index=1 if p.index%9 in (0,1) else 0
    box('Deep oven shadow',(0,.09,.43),(.42,.045,.29),dark,.055)
    box('Hot oven floor',(0,-.06,.30),(.36,.43,.015),dark,.006)
    # Separate radial arch voussoirs make the opening readable at game scale.
    for i in range(11):
        a=PI*i/10
        o=box('Arch brick',(.213*math.cos(a),-.253,.425+.213*math.sin(a)),(.070,.105,.075),light if i%2 else mortar,.008)
        o.rotation_euler.y=PI/2-a
    for x in (-.213,.213):
        for z in (.328,.398):
            box('Door jamb brick',(x,-.253,z),(.075,.105,.064),light,.007)
    box('Chimney',(0,.115,.79),(.15,.16,.32),brick,.018)
    box('Chimney crown',(0,.115,.948),(.195,.20,.052),mortar,.012)
    box('Chimney soot',(0,.115,.977),(.125,.13,.009),dark,.006)
    for z in (.70,.78,.86):
        box('Chimney mortar course',(0,.027,z),(.145,.006,.01),mortar,.002)
    for x in (-.125,0,.125):
        sphere('Glowing coal',(x,.065,.33),(.055,.06,.025),fire)
        sphere('Golden flame',(x,.09,.405),(.024,.025,.095),fire)
    cyl('Pizza dough',(0,-.255,.325),.165,.025,crust)
    cyl('Pizza cheese',(0,-.255,.34),.145,.012,cheese)
    torus('Raised pizza crust',(0,-.255,.34),.155,.015,crust)
    for x,y in ((-.06,-.28),(.055,-.31),(0,-.19),(.07,-.22),(-.06,-.20)):
        cyl('Pepperoni topping',(x,y,.35),.025,.006,red)


def build_gum():
    cached = SOURCES/'Gum.blend'
    source=Path.home()/'Desktop/gumball_machine.blend'
    if cached.exists():
        with bpy.data.libraries.load(str(cached),link=False) as (src,dst):
            dst.scenes=[n for n in src.scenes if n.startswith('Gum Tower Studio')]
        scene=max(dst.scenes,key=lambda s:len(s.objects))
        bpy.context.window.scene=scene
        SCENES['Gum']=scene
        return
    scene=new_scene('Gum')
    with bpy.data.libraries.load(str(source),link=False) as (src,dst):
        dst.objects=[n for n in src.objects if n.startswith(('Mesh_','Gumball_'))]
    imported=dst.objects
    for ob in imported:
        scene.collection.objects.link(ob)
    bpy.context.view_layer.update()
    points=[o.matrix_world@Vector(c) for o in imported for c in o.bound_box]
    lo=min(v.z for v in points); hi=max(v.z for v in points)
    factor=.93/(hi-lo)
    for ob in imported:
        # Bake world coordinates so no origin or parent ambiguity survives export.
        mw=ob.matrix_world.copy()
        ob.data=ob.data.copy()
        for v in ob.data.vertices:
            p=mw@v.co
            v.co=(p.x*factor,p.y*factor,(p.z-lo)*factor+.18)
        ob.matrix_world.identity()
        if ob.name.startswith('Mesh_'):
            ob.name='Approved gumball machine'


def export_tower(name,scene):
    bpy.context.window.scene=scene
    bpy.context.view_layer.update()
    art=[o for o in scene.objects if o.type=='MESH']
    # Blender names are global across scenes; ensure exported base names are exact.
    renamed=[]
    for ob in art:
        target=ob.get('export_name')
        if target:
            collision=bpy.data.objects.get(target)
            if collision and collision!=ob:
                renamed.append((collision,collision.name))
                collision.name='Saved other scene '+target
            ob.name=target
    bpy.ops.object.select_all(action='DESELECT')
    for ob in art:
        if ob.data.users>1: ob.data=ob.data.copy()
        ob.select_set(True)
    bpy.context.view_layer.objects.active=art[0]
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    pts=[o.matrix_world@Vector(c) for o in art for c in o.bound_box]
    bottom=min(p.z for p in pts)
    assert abs(bottom)<.0001,(name,bottom)
    assert any(o.name=='rim' for o in art)
    assert any(o.name=='pedestal' for o in art)
    SOURCES.mkdir(parents=True,exist_ok=True)
    bpy.data.libraries.write(str(SOURCES/(name+'.blend')),{scene},fake_user=True,compress=True)
    # Consolidate only export copies; editable sources retain individual pieces.
    bpy.ops.object.select_all(action='DESELECT')
    export_copies=[]
    for ob in art:
        if ob.get('export_name'):
            continue
        clone=ob.copy(); clone.data=ob.data.copy()
        scene.collection.objects.link(clone)
        clone.select_set(True)
        export_copies.append(clone)
    bpy.context.view_layer.objects.active=export_copies[0]
    bpy.ops.object.join()
    head=bpy.context.object
    head.name=name+' Head'
    for ob in art:
        if ob.get('export_name'): ob.select_set(True)
    for folder in ('Assets/Models/Towers','Assets/Resources/Snack/Towers'):
        bpy.ops.export_scene.gltf(filepath=str(ROOT/folder/(name+'.glb')),use_selection=True,use_active_scene=True)
    bpy.data.objects.remove(head,do_unlink=True)
    print('TOWER_EXPORT',name,'meshes',len(art),'bounds',tuple(round(min(p[i] for p in pts),4) for i in range(3)),tuple(round(max(p[i] for p in pts),4) for i in range(3)))
    for ob,old in renamed:
        held=bpy.data.objects.get(old)
        if held: held.name=name+' '+old
        ob.name=old


def build_gallery():
    scene=bpy.data.scenes.new('Remaining Towers Gallery')
    bpy.context.window.scene=scene
    for idx,(name,source) in enumerate(SCENES.items()):
        offset=Vector(((idx%3-1)*1.65,(idx//3)*1.8,0))
        for ob in source.objects:
            if ob.type!='MESH': continue
            clone=ob.copy()
            scene.collection.objects.link(clone)
            clone.location+=offset
    floor=material('Studio floor',(.055,.08,.12),0,.8)
    box('Gallery floor',(0,.85,-.10),(6,5,.18),floor,.04)
    world=bpy.data.worlds.new('Snack gallery world'); world.use_nodes=True
    bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
    bg.inputs[0].default_value=(.16,.20,.29,1); bg.inputs[1].default_value=.45
    scene.world=world
    for name,loc,energy,size in [('Key',(-3,-4,6),550,5),('Fill',(4,-1,4),400,4),('Rim',(0,5,5),600,3)]:
        data=bpy.data.lights.new(name,'AREA'); data.energy=energy; enum(data,'shape','DISK'); data.size=size
        ob=bpy.data.objects.new(name,data); scene.collection.objects.link(ob); ob.location=loc
        ob.rotation_euler=(Vector((0,.7,.4))-ob.location).to_track_quat('-Z','Y').to_euler()
    data=bpy.data.cameras.new('Gallery camera'); enum(data,'type','ORTHO'); data.ortho_scale=5.7
    camera=bpy.data.objects.new('Gallery camera',data); scene.collection.objects.link(camera)
    camera.location=(2.6,-6,5.3); camera.rotation_euler=(Vector((0,.85,.45))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.camera=camera
    scene.render.resolution_x=1600; scene.render.resolution_y=1100; scene.render.resolution_percentage=100
    enum(scene.render.image_settings,'file_format','PNG')
    scene.render.filepath=str(PREVIEW)
    try:
        scene.render.engine='CYCLES'
    except TypeError as error:
        raise RuntimeError('Cycles unavailable: '+str(error))
    scene.cycles.samples=32
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_location=(0,.8,.45)
            area.spaces.active.region_3d.view_distance=6.5
            area.spaces.active.region_3d.view_rotation=camera.rotation_euler.to_quaternion()
            enum(area.spaces.active.shading,'type','MATERIAL')
    bpy.data.libraries.write(str(Path.home()/'Desktop/remaining_towers_gallery.blend'),{scene},fake_user=True,compress=True)
    print('GALLERY_READY',str(PREVIEW))


def build_all():
    for fn in (build_gum,build_truck,build_fondue,build_gold,build_boba,build_pizza):
        fn()
    for name,scene in SCENES.items(): export_tower(name,scene)
    build_gallery()


if __name__=='__main__':
    build_all()
