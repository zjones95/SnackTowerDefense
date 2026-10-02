"""Dependency-free GLB structure/mirror validation (not Unity gameplay testing).

Each v3 snack tower GLB now contains three nodes: the joined art mesh (named
after the tower) plus the shared `pedestal` and `rim` wooden base pieces baked
on by add_base_new_towers.py / fix_coffee_mug_base.py. The art is lifted onto
the base top (z=0.18); the base is centred on X/Z and grounded at z=0.
"""
from pathlib import Path
import json, struct, hashlib

root = Path(__file__).resolve().parents[2]
names = ['HotSauce', 'CoffeeMug', 'PopTartToaster', 'CookieCrumbler', 'SourFizz']
report = []

for name in names:
    runtime = root / 'Assets/Resources/Snack/Towers' / f'{name}.glb'
    mirror = root / 'Assets/Models/Towers' / f'{name}.glb'
    data = runtime.read_bytes()
    assert data == mirror.read_bytes(), f'{name}: mirror differs'
    magic, version, size = struct.unpack_from('<4sII', data)
    assert magic == b'glTF' and version == 2 and size == len(data)
    length, kind = struct.unpack_from('<II', data, 12)
    assert kind == 0x4e4f534a
    doc = json.loads(data[20:20 + length])

    node_names = [n.get('name') or '' for n in doc['nodes']]
    assert name in node_names, f'{name}: art node missing'
    assert any('pedestal' in n.lower() for n in node_names), f'{name}: pedestal base missing'
    assert any(n.lower() == 'rim' for n in node_names), f'{name}: rim base missing'

    tris = 0
    lo = [1e9, 1e9, 1e9]
    hi = [-1e9, -1e9, -1e9]
    for i, node in enumerate(doc['nodes']):
        nm = (node.get('name') or '').lower()
        # The wooden base is round and must sit centred on the cell.
        if nm in ('pedestal', 'rim'):
            t = node.get('translation', [0, 0, 0])
            assert abs(t[0]) < 1e-5 and abs(t[2]) < 1e-5, f'{name}: {nm} base not centred'
        if 'mesh' not in node:
            continue
        t = node.get('translation', [0, 0, 0])
        s = node.get('scale', [1, 1, 1])
        for p in doc['meshes'][node['mesh']]['primitives']:
            assert 'NORMAL' in p['attributes'] and 'TEXCOORD_0' in p['attributes'], f'{name}: missing NORMAL/UV'
            assert p.get('mode', 4) == 4, f'{name}: non-triangle primitive'
            tris += doc['accessors'][p['indices']]['count'] // 3
            a = doc['accessors'][p['attributes']['POSITION']]
            for k in range(3):
                lo[k] = min(lo[k], a['min'][k] * s[k] + t[k])
                hi[k] = max(hi[k], a['max'][k] * s[k] + t[k])

    assert abs(lo[1]) < 1e-4, f'{name}: not grounded (min y {lo[1]})'
    # Model totals: base top 0.18 + art. The mug art is deliberately shorter.
    assert 0.9 < hi[1] < 1.25, f'{name}: unexpected height {hi[1]}'

    row = {'name': name, 'bytes': len(data), 'triangles': tris,
           'bounds_y_up': [[round(v, 4) for v in lo], [round(v, 4) for v in hi]],
           'sha256': hashlib.sha256(data).hexdigest()}
    report.append(row)

print(json.dumps(report, indent=2))
