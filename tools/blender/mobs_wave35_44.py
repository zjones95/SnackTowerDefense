"""Wave 35-44 mobs, matching the approved low-poly concept sheet.

Run inside Blender: exec(compile(open(__file__).read(), __file__, 'exec')); build_batch()
Each model is one rigid-skinned mesh with the shared six-bone looping walk.
"""
exec(compile(open(r'C:/Users/Desktop/SnackTowerDefense/tools/blender/mobs_first5.py', encoding='utf-8').read(),
             'mobs_first5.py', 'exec'))

SCENE_NAME = 'Mobs 35-44'
TARGET_HEIGHT.update(Blackberry=1.35, Lemon=1.05, Lettuce=1.05, Zucchini=1.17,
                     Chili=1.12, RotKing=1.43, Mushroom=1.04, Garlic=1.08,
                     Grapefruit=1.10, Kale=1.12)
for name in ('Blackberry', 'Lemon', 'Lettuce', 'Zucchini', 'Chili', 'RotKing',
             'Mushroom', 'Garlic', 'Grapefruit', 'Kale'):
    WALK[name] = dict(swing=17 if name in ('Blackberry', 'RotKing') else 25,
                      lift=.035 if name in ('Blackberry', 'RotKing') else .045,
                      bob=.012 if name in ('Blackberry', 'RotKing') else .019,
                      roll=2 if name in ('Blackberry', 'RotKing') else 3.5, nod=1.2)


class Front:
    """Flat face plate, set in front of body (-Y)."""
    def __init__(self, y): self.y = y
    def pt(self, x, z, off=0): return Vector((x, self.y - off, z))


class OffsetFront(Front):
    def __init__(self, y, x):
        super().__init__(y)
        self.x = x
    def pt(self, x, z, off=0): return Vector((x+self.x, self.y-off, z))


def visage(b, surface, z, gap=.12, smile=True):
    ink = b.mat('ink', '#221F21', .65)
    for x in (-gap, gap):
        disc(b, surface, ink, x, z, .032, .024, n=8)
    deco_strip(b, surface, ink, [-.065 + i*.13/6 for i in range(7)],
               lambda x: z-.105 + (3.3 if smile else -3.3)*x*x,
               lambda x: .015, depth=.025)


def two_feet(b, skin, foot, spread=.15):
    feet(b, skin, foot, (-spread, spread), leg_r=.042, leg_h=.14, leg_z=.06,
         foot_sc=(.095, .16, .065), foot_y=-.035)


def leaf_cluster(b, color, z, count=5, radius=.26, length=.30):
    for i in range(count):
        a = 2*PI*i/count
        b.add(lens_g(length, .13, .06), color,
              X((radius*.42*math.cos(a), radius*.42*math.sin(a), z),
                (0, -.5, a)))


def build_blackberry():
    b = Builder('Blackberry')
    dark=b.mat('berries','#302341'); light=b.mat('berry facets','#49315C')
    vine=b.mat('bramble','#75432E'); leaves=b.mat('leaf','#548C38')
    # Berries all feed one body bone, while feet alone walk.
    b.add(sphere_g(10,5),dark,X((0,0,.52),sc=(.36,.29,.35)))
    for row,z in enumerate((.39,.70)):
        for i in range(4):
            a=2*PI*(i+(.5 if row else 0))/4
            b.add(sphere_g(6,3), light if (i+row)%3 else dark,
                  X((.30*math.cos(a),.23*math.sin(a),z),sc=(.15,.14,.15)))
    # Crown of thorny branches / leaves, kept above rolling body.
    for i in range(4):
        a=2*PI*i/4
        b.add(tube_g([(.16*math.cos(a),.13*math.sin(a),.73),
                      (.34*math.cos(a),.29*math.sin(a),1.02)], [.045,.018]),vine)
        b.add(frustum_g(4,.065,0,.16),vine,
              X((.34*math.cos(a),.29*math.sin(a),.99),(0,.30,a)))
        b.add(lens_g(.19,.10,.025),leaves,
              X((.22*math.cos(a),.17*math.sin(a),.87),(0,-.5,a)))
    visage(b,Front(-.384),.48,.11,False)
    two_feet(b,dark,dark,.19)
    return b


def build_lemon():
    b=Builder('Lemon'); yellow=b.mat('peel','#F7D740'); pale=b.mat('highlight','#FFE865')
    stem=b.mat('stem','#557936'); leaf=b.mat('leaf','#4F9A3D')
    p=[(0,.18),(.11,.22),(.29,.30),(.39,.47),(.38,.68),(.25,.83),(.08,.93),(0,.99)]
    v,f,seg=lathe_g(p,12); b.add((v,f),[pale if i in (2,3,8) else yellow for i in seg])
    b.add(frustum_g(5,.035,.022,.08),stem,X((0,0,.94)))
    b.add(lens_g(.24,.12,.035),leaf,X((0,0,.97),(0,-.3,.4)))
    visage(b,LatheSurf(p,12),.51)
    two_feet(b,yellow,b.mat('feet','#CEA531'),.13)
    return b


def build_lettuce():
    b=Builder('Lettuce'); core=b.mat('heart','#A7CE70')
    pale=b.mat('leaf light','#94C868'); dark=b.mat('leaf shade','#558E43')
    p=[(0,.18),(.21,.21),(.32,.32),(.37,.49),(.31,.71),(.15,.80),(0,.82)]
    b.add(lathe_g(p,12),core)
    for i in range(9):
        a=2*PI*i/9
        # Large upright leaf blades make ruffled bowl silhouette.
        b.add(lens_g(.45,.26,.075), pale if i%3 else dark,
              X((.27*math.cos(a),.27*math.sin(a),.43),(.25,-.84,a+.22)))
    leaf_cluster(b,dark,.77,6,.17,.24)
    visage(b,Front(-.376),.48)
    two_feet(b,core,dark,.14)
    return b


def build_zucchini():
    b=Builder('Zucchini'); green=b.mat('green','#2E783C'); stripe=b.mat('stripe','#539D52')
    p=[(0,.18),(.19,.21),(.26,.32),(.28,.53),(.26,.77),(.20,1.02),(.13,1.10),(0,1.10)]
    v,f,s=lathe_g(p,12); b.add((v,f),[stripe if i%3==0 else green for i in s])
    b.add(frustum_g(6,.10,.075,.09),b.mat('stem','#537F3D'),X((0,0,1.08)))
    visage(b,LatheSurf(p,12),.36,.085)
    two_feet(b,green,green,.12)
    return b


def build_chili():
    b=Builder('Chili'); red=b.mat('red','#D9332D'); bright=b.mat('highlight','#F4523D')
    green=b.mat('calyx','#549344')
    # Ring profile bends into a pepper crescent while keeping every ring
    # horizontal. That gives a clean taper above the legs, without the
    # twisting cross-section the old tube made as it curled at the bottom.
    p=[(0,.20),(.07,.23),(.14,.31),(.20,.44),(.22,.60),(.18,.77),
       (.10,.90),(0,.96)]
    v,f,_=lathe_g(p,10)
    def bend(z):
        if z<.44: return .07 - .07*(z-.20)/.24
        if z<.70: return -.12*(z-.44)/.26
        return -.12 + .12*(z-.70)/.26
    b.add(([(x+bend(z),y,z) for x,y,z in v],f),red)
    b.add(frustum_g(7,.18,.035,.10),green,X((0,0,.93)))
    b.add(tube_g([(0,0,1.0),(.06,.01,1.13)],[.045,.025]),green)
    # Small red cheek on body, plus a flat face in front of curved spine.
    visage(b,OffsetFront(-.223,-.07),.60,.075)
    # Banana's long legs reach into the fruit instead of stopping just below
    # the narrow tip. Use that same hip/ankle spacing and forward foot offset.
    feet(b,red,b.mat('feet','#A92826'),(-.07,.07),leg_r=.05,
         leg_h=.24,leg_z=.07,foot_sc=(.10,.18,.07),foot_y=-.05,leg_y=-.02)
    return b


def build_rotking():
    b=Builder('RotKing'); pale=b.mat('cabbage','#859A59'); shadow=b.mat('wilt','#506B3D')
    edge=b.mat('tattered edge','#A4A16D'); crown=b.mat('crown','#9A7843')
    p=[(0,.17),(.28,.21),(.43,.34),(.45,.55),(.37,.76),(.19,.85),(0,.84)]
    b.add(lathe_g(p,12),pale)
    for i in range(8):
        a=2*PI*i/8
        b.add(lens_g(.36,.20,.05),shadow if i%2 else edge,
              X((.30*math.cos(a),.28*math.sin(a),.61),(.12,-.85,a)))
    # Crooked leaf crown with dark spikes, not royal metallic jewellery.
    for i in range(7):
        a=2*PI*i/7
        b.add(frustum_g(4,.085,0,.29 if i%2 else .38),crown,
              X((.26*math.cos(a),.26*math.sin(a),.78),(0,.12,a)))
        b.add(lens_g(.19,.08,.025),shadow,X((.21*math.cos(a),.21*math.sin(a),.93),(0,-.8,a)))
    visage(b,Front(-.455),.50,.16,False)
    two_feet(b,shadow,shadow,.20)
    return b


def build_mushroom():
    b=Builder('Mushroom'); stalk=b.mat('stalk','#E9D8B5')
    cap=b.mat('cap','#A98362'); caplite=b.mat('cap facet','#B99A78')
    gills=b.mat('gills','#D9C5A4')
    p=[(0,.17),(.13,.20),(.20,.33),(.20,.60),(.17,.69),(0,.71)]
    b.add(lathe_g(p,10),stalk)
    b.add(frustum_g(12,.42,.38,.07),gills,X((0,0,.70)))
    c=[(0,.69),(.42,.71),(.43,.78),(.35,.92),(.20,1.01),(0,1.03)]
    v,f,s=lathe_g(c,12); b.add((v,f),[caplite if i in (0,4,8) else cap for i in s])
    for x,y in ((-.16,-.19),(.21,-.11),(.08,.23)):
        b.add(sphere_g(8,4),gills,X((x,y,.88),sc=(.07,.04,.025)))
    visage(b,LatheSurf(p,10),.43,.085)
    two_feet(b,stalk,b.mat('feet','#BBA986'),.12)
    return b


def build_garlic():
    b=Builder('Garlic'); ivory=b.mat('ivory','#F0E7D4'); fold=b.mat('fold','#D4C7B2')
    stem=b.mat('sprout','#659042')
    p=[(0,.17),(.18,.20),(.32,.31),(.37,.48),(.30,.66),(.15,.85),(.07,.99),(0,1.0)]
    v,f,s=lathe_g(p,12); b.add((v,f),[fold if i%3==0 else ivory for i in s])
    for i in range(6):
        a=2*PI*i/6
        b.add(sphere_g(8,4), ivory,
              X((.18*math.cos(a),.18*math.sin(a),.38),sc=(.19,.18,.26)))
    for i in range(3):
        b.add(frustum_g(4,.035,0,.21),stem,X((.05*i-.05,0,.94),(0,.25,(i-1)*.3)))
    visage(b,Front(-.36),.46,.11)
    two_feet(b,ivory,fold,.14)
    return b


def build_grapefruit():
    b=Builder('Grapefruit'); pink=b.mat('pink','#E99691'); highlight=b.mat('facet','#F4AAA2')
    rind=b.mat('rind','#D6786D'); green=b.mat('leaf','#5F963F')
    p=[(0,.16),(.25,.19),(.39,.30),(.46,.48),(.43,.68),(.31,.85),(.12,.92),(0,.91)]
    v,f,s=lathe_g(p,14); b.add((v,f),[highlight if i%5==0 else pink for i in s])
    b.add(frustum_g(5,.06,.035,.09),rind,X((0,0,.89)))
    for a in (0,PI):
        b.add(lens_g(.25,.14,.03),green,X((0,0,.94),(0,-.45,a+.3)))
    visage(b,LatheSurf(p,14),.52,.16)
    two_feet(b,pink,rind,.17)
    return b


def build_kale():
    b=Builder('Kale'); core=b.mat('green','#5EAF55'); dark=b.mat('dark','#326C39')
    bright=b.mat('frills','#75BD60')
    p=[(0,.17),(.17,.21),(.25,.30),(.27,.45),(.23,.61),(.13,.68),(0,.69)]
    b.add(lathe_g(p,12),core)
    for z,n,r in ((.47,9,.22),(.67,11,.15),(.87,8,.08)):
        for i in range(n):
            a=2*PI*i/n + (0 if z==.47 else .22)
            b.add(lens_g(.30 if z<.8 else .24,.14,.035),bright if i%2 else dark,
                  X((r*math.cos(a),r*math.sin(a),z),(.25,-.70,a)))
    visage(b,Front(-.277),.42,.09)
    two_feet(b,core,dark,.12)
    return b


BUILDERS = {name: fn for name, fn in (
    ('Blackberry',build_blackberry), ('Lemon',build_lemon), ('Lettuce',build_lettuce),
    ('Zucchini',build_zucchini), ('Chili',build_chili), ('RotKing',build_rotking),
    ('Mushroom',build_mushroom), ('Garlic',build_garlic),
    ('Grapefruit',build_grapefruit), ('Kale',build_kale))}


def build_batch(do_gallery=True):
    # Base helper uses a new scene; existing Blender scenes stay intact.
    old=bpy.data.scenes.get(SCENE_NAME)
    if old: bpy.data.scenes.remove(old)
    obs=build_all(do_export=True,do_gallery=False)
    sc=bpy.context.scene
    sc.name=SCENE_NAME
    arms=[o.parent for o in obs]
    # Two independent five-mob gallery renders; preserves scale/readability.
    if do_gallery:
        for offset, group in ((0,arms[:5]),(5,arms[5:])):
            for o in obs: o.hide_render = o.parent not in group
            gallery(sc,group,-20, f'mobs_wave35_44_row{offset//5+1}.png')
        for o in obs: o.hide_render=False
    for i, arm in enumerate(arms):
        arm.location = (((i % 5) - 2)*1.55, (i // 5)*2.0, 0)
        arm.rotation_euler = (0, 0, math.radians(-20))
    sc.frame_set(0)
    # Store editable source beside preview files, not inside Unity project.
    bpy.ops.wm.save_as_mainfile(filepath=str(PREVIEW_DIR/'mobs_wave35_44.blend'))
    print('Mobs 35-44 exported to Resources and Models mirrors')


if __name__=='__main__': build_batch()
