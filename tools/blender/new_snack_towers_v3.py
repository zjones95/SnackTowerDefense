"""Five approved v3 tower concepts. Run in Blender; exports use MCP export_scene.
All geometry is low-segment primitive construction, flat shaded, -Y forward.
Cookie disks are large and nested side-by-side INSIDE the hopper (not particles).
"""
import bpy, math, json, random
from pathlib import Path
from mathutils import Vector

ROOT = Path(r'C:\Users\Desktop\SnackTowerDefense')
OUT = Path(r'C:\Users\Desktop\AppData\Local\Temp\opencode')
SOURCE = ROOT / 'tools/blender/source/new_snack_towers_v3.blend'
NAMES = ['HotSauce','CoffeeMug','PopTartToaster','CookieCrumbler','SourFizz']

def enum(obj, key, value):
    valid = [i.identifier for i in obj.bl_rna.properties[key].enum_items]
    if value in valid: setattr(obj,key,value)
    else:
        try: setattr(obj,key,value)  # dynamic color-management enums under-report
        except TypeError: print('Unavailable enum',key,value,valid)

def material(name, color, metal=0, rough=.48):
    m=bpy.data.materials.new('V3_'+name); m.use_nodes=True
    m.diffuse_color=(*color,1)
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough; p.inputs['Metallic'].default_value=metal
    return m

def finish(o,name,mat):
    o.name=name; o.data.materials.append(mat); parts.append(o); return o

def box(name, loc, size, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Single-segment soft edges','BEVEL'); mod.width=bevel; mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o,name,mat)

def cyl(name,loc,r,depth,mat,axis=(0,0,1),n=16,r2=None):
    if r2 is None: bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=depth,location=loc)
    else: bpy.ops.mesh.primitive_cone_add(vertices=n,radius1=r,radius2=r2,depth=depth,location=loc)
    o=bpy.context.object; o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    return finish(o,name,mat)

def ring(name,loc,outer,inner,depth,mat,axis=(0,0,1),n=16):
    # Closed annular cylinder primitive; inner wall gives genuinely recessed bores.
    vs=[]; fs=[]
    for z,r in [(-depth/2,outer),(depth/2,outer),(depth/2,inner),(-depth/2,inner)]:
        vs.extend((r*math.cos(2*math.pi*i/n),r*math.sin(2*math.pi*i/n),z) for i in range(n))
    for row in range(4):
        for i in range(n): fs.append((row*n+i,row*n+(i+1)%n,((row+1)%4)*n+(i+1)%n,((row+1)%4)*n+i))
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(vs,[],fs); mesh.update()
    o=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(o); o.location=loc
    o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler(); return finish(o,name,mat)

def ball(name,loc,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=1,location=loc)
    o=bpy.context.object; o.scale=scale; return finish(o,name,mat)

def join_asset(name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts: o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.ops.object.join(); o=bpy.context.object; o.name=name
    lo=Vector(tuple(min(v.co[i] for v in o.data.vertices) for i in range(3)))
    hi=Vector(tuple(max(v.co[i] for v in o.data.vertices) for i in range(3)))
    center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z)); height=hi.z-lo.z
    for v in o.data.vertices: v.co=(v.co-center)/height
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.smart_project(island_margin=.025); bpy.ops.object.mode_set(mode='OBJECT')
    for p in o.data.polygons: p.use_smooth=False
    o.data.calc_loop_triangles()
    stats[name]={'vertices':len(o.data.vertices),'faces':len(o.data.polygons),'triangles':len(o.data.loop_triangles),'mesh_objects':1,'height_m':1.0,'uv_layers':len(o.data.uv_layers)}
    assets.append(o); return o

def build():
    global parts,stats,assets
    # Preserve the user's existing scene; work in a new, dedicated authoring scene.
    scene=bpy.data.scenes.new('SnackTowers_ApprovedV3'); bpy.context.window.scene=scene
    scene.unit_settings.scale_length=1
    stats={}; assets=[]; parts=[]
    black=material('Charcoal',(.018,.023,.033)); dark=material('Bore',(.004,.003,.002))
    silver=material('Silver',(.53,.60,.67),.65); gold=material('WarmGold',(.93,.57,.13),.15)
    orange=material('HotOrange',(.94,.095,.012)); navy=material('Navy',(.025,.04,.095))
    cream=material('Cream',(.96,.85,.57)); brown=material('BrownCeramic',(.29,.105,.038))
    coffee=material('Coffee',(.024,.009,.004),0,.27); biscuit=material('Cookie',(.68,.36,.105))
    crust=material('GoldenCrust',(.88,.53,.19)); icing=material('VanillaFrosting',(.97,.92,.82))
    pink=material('PinkCap',(.91,.045,.25)); powder=material('PinkPowder',(.94,.53,.66))
    red=material('CandyRed',(.78,.012,.035),0,.28); yellow=material('ChipperYellow',(1,.62,.015))
    blue=material('LabelBlue',(.035,.35,.75)); white=material('PaperWhite',(.92,.94,.93))
    sprinkles=[pink,blue,yellow,red]
    # Hot sauce: a bottle, NOT a horizontal sauce cannon. Black elbow at neck.
    cyl('BottleHeel',(0,0,.035),.185,.07,gold)
    cyl('SauceBody',(0,0,.30),.18,.53,orange)
    cyl('BottleShoulder',(0,0,.64),.18,.15,orange,r2=.078)
    cyl('BottleNeck',(0,0,.755),.078,.13,orange)
    cyl('NavyPaperWrap',(0,0,.31),.183,.29,navy)
    for z in [.157,.463]: cyl('GoldLabelEdge',(0,0,z),.186,.014,cream)
    box('LabelGoldFrame',(0,-.181,.31),(.205,.016,.23),cream,.015)
    box('LabelNavyInset',(0,-.194,.31),(.181,.012,.206),navy,.014)
    ball('SauceDrop',(0,-.205,.31),(.023,.007,.034),orange)
    cyl('ElbowUpright',(0,0,.83),.092,.13,black)
    box('ElbowCorner',(0,-.025,.889),(.183,.19,.16),black,.035)
    ring('BlackNeckBarrel',(0,-.18,.89),.086,.059,.32,black,(0,-1,0),12)
    ring('MuzzleLip',(0,-.351,.89),.095,.06,.045,black,(0,-1,0),12)
    cyl('BoreBack',(0,-.07,.89),.06,.01,dark,(0,-1,0),12)
    join_asset('HotSauce'); parts=[]
    # Mug hollow rim, coffee inset, ceramic handle and central front barrel.
    cyl('MugFoot',(0,0,.07),.23,.14,brown,r2=.285)
    o=ring('MugBody',(0,0,.43),.34,.296,.60,brown)
    for v in o.data.vertices:
        if v.co.z<0: v.co.x*=.838; v.co.y*=.838
    ring('CeramicRim',(0,0,.75),.351,.296,.065,gold)
    ring('InsideWall',(0,0,.706),.33,.295,.05,brown)
    cyl('CoffeeSurface',(0,0,.689),.296,.015,coffee)
    # Polygonal D-shaped handle from six stout segments; hole remains open.
    path=[(.29,.66),(.47,.69),(.59,.60),(.63,.43),(.58,.24),(.43,.14),(.27,.15)]
    for i,(a,b) in enumerate(zip(path,path[1:])):
        p=Vector((a[0],0,a[1])); q=Vector((b[0],0,b[1])); d=q-p
        cyl('HandleSegment'+str(i),(p+q)/2,.063,d.length+.027,brown,d,8)
    ring('BarrelMount',(0,-.316,.42),.128,.065,.065,brown,(0,-1,0),12)
    ring('CentralCoffeeBarrel',(0,-.445,.42),.081,.055,.235,brown,(0,-1,0),12)
    ring('CoffeeMuzzleLip',(0,-.574,.42),.088,.057,.028,gold,(0,-1,0),12)
    cyl('CoffeeBoreBack',(0,-.333,.42),.057,.01,dark,(0,-1,0))
    for x,z in [(-.099,.42),(.099,.42),(0,.52),(0,.32)]: box('MountBolt',(x,-.357,z),(.027,.019,.027),silver,.003)
    join_asset('CoffeeMug'); parts=[]
    # Wide two-slot toaster with separate pastry slots and side lever.
    for x in [-.4,.4]:
        for y in [-.22,.22]: box('RubberFoot',(x,y,.04),(.10,.11,.08),black,.018)
    box('BlackBase',(0,0,.105),(1.02,.65,.09),black,.025)
    box('SilverShell',(0,0,.39),(.98,.62,.52),silver,.085)
    for y in [-.15,.15]:
        box('SlotShadow',(0,y,.647),(.72,.12,.025),black,.026)
        box('HeatingInterior',(0,y,.655),(.66,.075,.012),orange,.016)
        box('Pastry',(0,y,.77),(.57,.073,.40),crust,.036)
        box('FrostedFront',(0,y-.042,.805),(.49,.022,.275),icing,.025)
        box('FrostedBack',(0,y+.042,.805),(.49,.022,.275),icing,.025)
        for k in range(7):
            for side in [-1,1]:
                x=-.207+k*.069
                box('Crimp',(x,y+side*.036,.945),(.027,.018,.03),biscuit,.004)
        for k in range(12):
            x=-.19+(k%4)*.124; z=.723+(k//4)*.073+(k%2)*.017
            o=box('Sprinkle',(x,y-.057,z),(.025,.009,.009),sprinkles[k%4],.002); o.rotation_euler.y=(k%3-1)*.55
    box('LeverTrack',(.493,0,.43),(.012,.038,.31),black,.006)
    box('Lever',(.545,0,.49),(.12,.12,.045),black,.012)
    cyl('Dial',(.515,-.15,.245),.061,.034,black,(1,0,0),12)
    box('DialMarker',(.537,-.15,.266),(.008,.013,.025),white)
    join_asset('PopTartToaster'); parts=[]
    # Wood chipper chassis and front rectangular open crumb chute.
    box('Chassis',(0,0,.12),(.50,.56,.11),black,.018)
    for x in [-.24,.24]:
        cyl('Wheel',(x,.14,.12),.12,.072,black,(1,0,0),12)
        cyl('WheelHub',(x*1.17,.14,.12),.055,.01,silver,(1,0,0),12)
    box('MotorBody',(0,.035,.30),(.46,.39,.28),yellow,.025)
    cyl('RearMotor',(0,.27,.32),.17,.22,black,(0,1,0),12)
    for x in [-.11,0,.11]: box('MotorCoolingFin',(x,.373,.32),(.025,.022,.23),silver,.004)
    box('ChuteDarkBack',(0,-.218,.265),(.26,.025,.17),dark)
    box('ChuteFloor',(0,-.30,.174),(.29,.26,.025),yellow)
    box('ChuteRoof',(0,-.30,.359),(.29,.26,.025),yellow)
    for x in [-.145,.145]: box('ChuteWall',(x,-.30,.268),(.025,.26,.19),yellow)
    for x in [-.15,.15]:
        for z in [.177,.359]: box('ChuteCorner',(x,-.436,z),(.032,.014,.035),black)
    cyl('GrinderCollar',(0,0,.475),.24,.095,black)
    ring('CollarRim',(0,0,.522),.248,.178,.025,silver)
    # Four tapered hopper walls, built as closed trapezoid prisms.
    def hopperwall(rot):
        vs=[(-.17,-.17,.52),(.17,-.17,.52),(.43,-.43,.90),(-.43,-.43,.90),(-.145,-.145,.52),(.145,-.145,.52),(.40,-.40,.90),(-.40,-.40,.90)]
        if rot: vs=[(y*rot,-x*rot,z) for x,y,z in vs]
        elif rot==0: pass
        mesh=bpy.data.meshes.new('HopperWall'); mesh.from_pydata(vs,[],[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)]); mesh.update()
        o=bpy.data.objects.new('HopperWall',mesh); bpy.context.collection.objects.link(o); finish(o,'HopperWall',yellow)
    # Rectangular hopper uses same square mouth dimensions on each wall.
    # Set the outer mouth to square .86 for comfortable side-by-side disks.
    for a in range(4):
        hopperwall(0); parts[-1].rotation_euler.z=a*math.pi/2
    for y in [-.43,.43]: box('HopperTopLip',(0,y,.907),(.90,.04,.043),black,.006)
    for x in [-.43,.43]: box('HopperTopLip',(x,0,.907),(.04,.82,.043),black,.006)
    cyl('HopperThroat',(0,0,.535),.17,.015,dark)
    # Two LARGE cookies in adjacent hopper pockets, plus a rear nested cookie.
    # bottoms at .635, below .907 rim; centers inside mouth; no floating cookies.
    for idx,(x,y,z,r) in enumerate([(-.172,-.025,.855,.18),(.172,-.015,.855,.18),(0,.145,.875,.18)]):
        cyl('NestedCookie'+str(idx),(x,y,z),r,.075,crust,(0,-1,0),16)
        cyl('CookieBakedFace',(x,y-.04,z),r*.88,.012,biscuit,(0,-1,0),16)
        for k in range(7):
            angle=k*2.4; rr=.105 if k else 0
            ball('ChocolateChip',(x+rr*math.cos(angle),y-.051,z+rr*math.sin(angle)),(.021,.013,.019),coffee)
    # Crumbs rest IN the chute, not floating VFX outside the export.
    for k in range(9):
        box('ChuteCrumb',(-.10+(k%3)*.095,-.40+(k//3)*.065,.198),(.031,.028,.027),biscuit,.003)
    join_asset('CookieCrumbler'); parts=[]
    # Baby-bottle-pop powder jar and red hard-candy nipple.
    cyl('JarFoot',(0,0,.025),.205,.05,silver)
    cyl('PowderJar',(0,0,.24),.218,.40,powder)
    cyl('JarShoulder',(0,0,.452),.218,.045,powder,r2=.20)
    cyl('WhiteWrap',(0,0,.245),.220,.22,white)
    for z in [.137,.351]: cyl('BlueLabelTrim',(0,0,z),.222,.015,blue)
    # Geometric stars sit on front facets, a readable candy-package motif.
    for x,z,mat in [(-.09,.265,blue),(.105,.215,yellow)]:
        for a in [0,math.pi/3,-math.pi/3]:
            o=box('LabelStar',(x,-.219,z),(.135,.008,.025),mat); o.rotation_euler.y=a
    cyl('PinkCap',(0,0,.50),.23,.10,pink)
    for k in range(24):
        a=k*math.tau/24
        o=box('CapGrip',(.229*math.cos(a),.229*math.sin(a),.50),(.014,.025,.09),pink,.002); o.rotation_euler.z=a
    ring('CapUpperRim',(0,0,.554),.23,.174,.018,pink)
    cyl('CandyFlare',(0,0,.576),.10,.06,red,r2=.069)
    cyl('CandyNippleStem',(0,0,.718),.069,.24,red,r2=.043)
    ball('CandyNippleTip',(0,0,.86),(.061,.061,.072),red)
    join_asset('SourFizz')
    # Neutral studio preview environment, excluded from all asset exports.
    parts=[]
    ground=material('StudioGround',(.72,.77,.81))
    box('StudioFloor',(0,0,-.027),(200,200,.05),ground)
    bpy.ops.object.camera_add(location=(1.65,-2.75,1.65)); cam=bpy.context.object
    cam.name='PreviewCamera'; cam.rotation_euler=(Vector((0,0,.48))-cam.location).to_track_quat('-Z','Y').to_euler()
    enum(cam.data,'type','ORTHO'); cam.data.ortho_scale=1.70; scene.camera=cam
    for loc,power,size in [((-3,-4,6),450,4),((4,-1,3),230,3),((0,4,5),400,3)]:
        bpy.ops.object.light_add(type='AREA',location=loc); light=bpy.context.object
        light.data.energy=power; light.data.shape='DISK'; light.data.size=size
        light.rotation_euler=(Vector((0,0,.4))-light.location).to_track_quat('-Z','Y').to_euler()
    scene.world=bpy.data.worlds.new('V3StudioWorld'); scene.world.use_nodes=True
    bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND'); bg.inputs['Color'].default_value=(.72,.77,.84,1); bg.inputs['Strength'].default_value=.65
    enum(scene.view_settings,'view_transform','Standard')
    enum(scene.render.image_settings,'file_format','PNG')
    scene.render.resolution_x=800; scene.render.resolution_y=800; scene.render.resolution_percentage=100
    SOURCE.parent.mkdir(parents=True,exist_ok=True)
    (OUT/'new_towers_v3_mesh_counts.json').write_text(json.dumps(stats,indent=2))
    for i,o in enumerate(assets): o.location.x=(i-2)*1.6
    bpy.ops.object.select_all(action='DESELECT')
    for o in assets: o.select_set(True)
    bpy.context.view_layer.objects.active=assets[0]
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            region=next(r for r in area.regions if r.type=='WINDOW')
            with bpy.context.temp_override(area=area,region=region): bpy.ops.view3d.view_selected()
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
    print(json.dumps(stats,indent=2))

def render_one(name):
    for o in assets:
        o.hide_render=o.name!=name
        o.location=(0,0,0)
    scene=bpy.context.scene; scene.render.filepath=str(OUT/('tower_'+name+'_model_v3.png'))
    bpy.ops.render.render(write_still=True)

if __name__=='__main__':
    build()
