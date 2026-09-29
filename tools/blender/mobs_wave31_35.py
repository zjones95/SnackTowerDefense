"""Approved final five. Run Blender --background --python this_file.

Fruit use the shared six-bone rig. Granola Mom adds head, upper/lower arms,
and knees, with rigid low-poly skinning and a grounded baked humanoid walk.
"""
exec(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_first5.py').read())

TARGET_HEIGHT.update(Avocado=1.15, Lychee=.95, Turnip=1.10, Papaya=1.2, GranolaMom=1.65)
for name in ('Avocado', 'Lychee', 'Turnip', 'Papaya', 'GranolaMom'):
    WALK[name] = dict(swing=24 if name == 'Lychee' else 18, lift=.04, bob=.015, roll=3, nod=1)


class Plane:
    def __init__(self, y): self.y = y
    def pt(self, x, z, off=0): return Vector((x, self.y + off, z))


def face(b, surface, z, spacing=.12, smile=False):
    eye = b.mat('eyes', '#201E1C')
    for x in (-spacing, spacing):
        disc(b, surface, eye, x, z, .032, .025, n=8)
    deco_strip(b, surface, eye, [-.07+i*.14/6 for i in range(7)],
               lambda x: z-.12+(3 if smile else -3)*x*x, lambda x: .017, depth=.025)


def fruit(name, profile, color):
    b = Builder(name)
    m = b.mat('skin', color)
    b.add(lathe_g(profile, 12), m)
    return b, m, LatheSurf(profile, 12)


def build_avocado():
    p = [(0,.16),(.26,.18),(.40,.37),(.39,.59),(.28,.83),(.22,1.02),(0,1.10)]
    b, skin, _ = fruit('Avocado', p, '#356B30')
    b.verts = [(x,max(y,-.24),z) for x,y,z in b.verts]
    flesh = b.mat('flesh', '#CEE780')
    # Flat cut face, inset from the dark rind. A shallow extruded polygon.
    outline = [(-.19,.98),(-.24,.79),(-.34,.57),(-.33,.34),(-.20,.23),
               (.20,.23),(.33,.34),(.34,.57),(.24,.79),(.19,.98),(0,1.04)]
    v = [(x,y,z) for y in (-.295,-.25) for x,z in outline]
    n = len(outline)
    f = [tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]
    f += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    b.add((v,f),flesh)
    b.add(sphere_g(10,6), b.mat('pit','#835029'), X((0,-.31,.45),sc=(.155,.095,.18)))
    face(b,Plane(-.31),.82,.10)
    feet(b,skin,skin,(-.14,.14))
    return b


def build_lychee():
    p = [(0,.17),(.24,.22),(.38,.40),(.39,.62),(.25,.81),(0,.87)]
    b, skin, s = fruit('Lychee',p,'#D95570')
    pink = b.mat('shell facets','#EC7890')
    for z,r,n in ((.32,.30,12),(.48,.39,14),(.66,.35,12),(.79,.23,9)):
        for i in range(n):
            a = 2*PI*(i+.3)/n
            if math.sin(a)<-.65 and .4<z<.7: continue
            loc = Vector((r*math.cos(a),r*math.sin(a),z))
            q = Vector((0,0,1)).rotation_difference(Vector((math.cos(a),math.sin(a),.3)).normalized())
            b.add(frustum_g(4,.065,0,.065),pink,Matrix.Translation(loc)@q.to_matrix().to_4x4())
    face(b,s,.60,.12,True)
    feet(b,skin,b.mat('feet','#982D49'),(-.12,.12))
    return b


def build_turnip():
    p = [(0,.17),(.26,.23),(.40,.40),(.41,.59),(.32,.76),(0,.82)]
    b = Builder('Turnip')
    white = b.mat('ivory','#E6E3D8'); purple = b.mat('purple','#894DB1')
    v,f,_ = lathe_g(p,12)
    b.add((v,f),[purple if sum(v[i][2] for i in face_)/len(face_)>.62 else white for face_ in f])
    leaf = b.mat('leaf','#388C42')
    for k in range(4):
        b.add(lens_g(.30,.15,.05),leaf,X((0,0,.79),(0,-.8,k*PI/2)))
    face(b,LatheSurf(p,12),.56)
    feet(b,purple,purple,(-.14,.14))
    return b


def build_papaya():
    p = [(0,.16),(.24,.22),(.34,.44),(.32,.68),(.23,.91),(.15,1.12),(0,1.17)]
    b = Builder('Papaya')
    orange=b.mat('gold','#F4AD2D'); green=b.mat('green','#77963D')
    v,f,seg=lathe_g(p,12)
    b.add((v,f),[green if sum(v[i][2] for i in face_)/len(face_)>.9 and k%3==0 else orange for k,face_ in zip(seg,f)])
    face(b,LatheSurf(p,12),.73,.11,True)
    feet(b,orange,b.mat('feet','#C98221'),(-.12,.12))
    return b


def build_mom():
    b=Builder('GranolaMom')
    skin=b.mat('skin','#E3BC8B'); dress=b.mat('dress','#63834D')
    coat=b.mat('cardigan','#BA995C'); hair=b.mat('hair','#794323'); boot=b.mat('boots','#573B28')
    # Short flared tunic leaves knees free to bend.
    b.add(frustum_g(8,.30,.23,.42),dress,X((0,0,.53),sc=(1,.72,1)))
    b.add(frustum_g(8,.23,.29,.25),dress,X((0,0,.93),sc=(1,.72,1)))
    for sign in (-1,1):
        b.add(box_g(),coat,X((sign*.215,-.02,.94),sc=(.12,.35,.47)))
    b.add(frustum_g(8,.09,.09,.13),skin,X((0,0,1.15)))
    b.cur_bone='head'
    b.add(sphere_g(10,6),skin,X((0,-.02,1.43),sc=(.255,.215,.30)))
    # Hair cap excludes front-lower faces; long side locks frame the face.
    v,f=sphere_g(10,6)
    f=[p for p in f if sum(v[i][1] for i in p)/len(p)>-.25 or sum(v[i][2] for i in p)/len(p)>.55]
    b.add((v,f),hair,X((0,.035,1.48),sc=(.29,.25,.32)))
    b.add(tube_g([(-.24,-.12,1.57),(-.12,-.18,1.68),(.06,-.16,1.73),(.22,-.10,1.62)],
                 [.065,.075,.06,.05]),hair)
    for sign in (-1,1):
        b.add(tube_g([(sign*.23,-.06,1.57),(sign*.27,-.05,1.31),(sign*.26,-.03,1.05),(sign*.29,-.02,.87)], [.095,.085,.075,.045]),hair)
    b.add(box_g(),hair,X((0,.19,1.16),sc=(.44,.12,.48)))
    face(b,Plane(-.223),1.46,.095)
    b.add(sphere_g(4,3),skin,X((0,-.237,1.38),sc=(.04,.05,.06)))
    b.cur_bone='body'
    b.add(lens_g(.12,.06,.015),b.mat('emblem','#335C2C'),X((-.03,-.185,1.04),(PI/2,-.7,0)))
    for suffix,sign in (('a',-1),('b',1)):
        x=sign*.14
        b.legs.append((x,0,.64)); b.ankles.append((x,0,.10))
        b.cur_bone='leg_'+suffix
        b.add(tube_g([(x,0,.64),(x,0,.37)],[.085,.07]),skin)
        b.cur_bone='shin_'+suffix
        b.add(tube_g([(x,0,.37),(x,0,.10)],[.07,.065]),skin)
        b.cur_bone='foot_'+suffix
        b.add(box_g(),boot,X((x,-.055,.09),sc=(.19,.29,.18)))
        b.cur_bone='arm_'+suffix
        b.add(tube_g([(sign*.30,0,1.13),(sign*.35,0,.89)],[.09,.075]),coat)
        b.cur_bone='forearm_'+suffix
        b.add(tube_g([(sign*.35,0,.89),(sign*.37,-.015,.70)],[.075,.055]),coat)
        b.add(sphere_g(6,4),skin,X((sign*.37,-.015,.66),sc=(.065,.07,.09)))
    return b


simple_rig=rig


def rig(ob,sc,hips,ankles,name):
    if name!='GranolaMom': return simple_rig(ob,sc,hips,ankles,name)
    tags=list(ob['vbone'])
    basic={'body','leg_a','leg_b','foot_a','foot_b'}
    ob['vbone']=[n if n in basic else 'body' for n in tags]
    arm=simple_rig(ob,sc,hips,ankles,name)
    scale=(hips[1][0]-hips[0][0])/.28
    shift=Vector(hips[0])-Vector((-.14,0,.64))*scale
    bpy.context.view_layer.objects.active=arm
    bpy.ops.object.mode_set(mode='EDIT')
    def bone(n,p,parent):
        eb=arm.data.edit_bones.new(n); eb.head=Vector(p)*scale+shift
        eb.tail=eb.head+Vector((0,.1,0)); eb.parent=arm.data.edit_bones[parent]
    bone('head',(0,0,1.24),'body')
    for suffix,sign in (('a',-1),('b',1)):
        bone('shin_'+suffix,(sign*.14,0,.37),'leg_'+suffix)
        arm.data.edit_bones['foot_'+suffix].parent=arm.data.edit_bones['shin_'+suffix]
        bone('arm_'+suffix,(sign*.30,0,1.13),'body')
        bone('forearm_'+suffix,(sign*.35,0,.89),'arm_'+suffix)
    bpy.ops.object.mode_set(mode='OBJECT')
    for n in set(tags)-basic:
        indices=[i for i,t in enumerate(tags) if t==n]
        ob.vertex_groups['body'].remove(indices)
        ob.vertex_groups.new(name=n).add(indices,1,'REPLACE')
    ob['vbone']=tags
    pb=arm.pose.bones
    for p in pb: p.rotation_mode='XYZ'
    for f in range(25):
        ph=2*PI*f/24
        for suffix,sign in (('a',1),('b',-1)):
            swing=math.radians(18)*sign*math.sin(ph)
            knee=math.radians(25)*max(0,-sign*math.cos(ph))
            pb['shin_'+suffix].rotation_euler=(-knee,0,0)
            pb['foot_'+suffix].rotation_euler=(-swing+knee,0,0)
            pb['arm_'+suffix].rotation_euler=(-swing*.8,0,0)
            pb['forearm_'+suffix].rotation_euler=(-.12-.10*max(0,sign*math.sin(ph)),0,0)
            for n in ('shin_','foot_','arm_','forearm_'):
                pb[n+suffix].keyframe_insert('rotation_euler',frame=f)
        pb['head'].rotation_euler=(0,0,math.radians(2)*math.sin(ph))
        pb['head'].keyframe_insert('rotation_euler',frame=f)
    sc.frame_set(0)
    return arm


def build_final():
    sc=bpy.context.scene
    sc.name='Mobs 31-35'
    for ob in list(sc.objects): bpy.data.objects.remove(ob,do_unlink=True)
    arms=[]
    for fn in (build_avocado,build_lychee,build_turnip,build_papaya,build_mom):
        ob,arm=finish(fn(),sc)
        export(ob,arm); report(ob); arms.append(arm)
    gallery(sc,arms,-20,'mobs_final5_front.png')
    walk_sheet(sc,arms,frames=(6,18),prefix='mobs_final5_walk')
    gallery(sc,arms,-20,'mobs_final5_front.png')
    bpy.ops.wm.save_as_mainfile(filepath=str(PREVIEW_DIR/'mobs_final5.blend'))


if __name__=='__main__': build_final()
