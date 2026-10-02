"""Dependency-free GLB structure/mirror validation (not Unity gameplay testing)."""
from pathlib import Path
import json,struct,hashlib
root=Path(__file__).resolve().parents[2]
names=['HotSauce','CoffeeMug','PopTartToaster','CookieCrumbler','SourFizz']
report=[]
for name in names:
    runtime=root/'Assets/Resources/Snack/Towers'/f'{name}.glb'
    mirror=root/'Assets/Models/Towers'/f'{name}.glb'
    data=runtime.read_bytes()
    assert data==mirror.read_bytes(),f'{name}: mirror differs'
    magic,version,size=struct.unpack_from('<4sII',data)
    assert magic==b'glTF' and version==2 and size==len(data)
    length,kind=struct.unpack_from('<II',data,12)
    assert kind==0x4e4f534a
    doc=json.loads(data[20:20+length])
    assert len(doc['meshes'])==1 and len(doc['nodes'])==1
    node=doc['nodes'][0]
    assert node['name']==name
    assert node.get('translation',[0,0,0])==[0,0,0]
    assert node.get('scale',[1,1,1])==[1,1,1]
    tris=0; bounds=[]
    for p in doc['meshes'][0]['primitives']:
        assert 'NORMAL' in p['attributes'] and 'TEXCOORD_0' in p['attributes']
        assert p.get('mode',4)==4
        tris+=doc['accessors'][p['indices']]['count']//3
        bounds.append(doc['accessors'][p['attributes']['POSITION']])
    low=[min(a['min'][i] for a in bounds) for i in range(3)]
    high=[max(a['max'][i] for a in bounds) for i in range(3)]
    assert abs(low[1])<1e-5 and abs(high[1]-1)<1e-5
    for i in [0,2]: assert abs(low[i]+high[i])<1e-5
    row={'name':name,'bytes':len(data),'triangles':tris,'material_primitives':len(bounds),'bounds_y_up':[low,high],'sha256':hashlib.sha256(data).hexdigest()}
    report.append(row)
print(json.dumps(report,indent=2))
