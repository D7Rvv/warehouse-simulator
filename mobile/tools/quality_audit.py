#!/usr/bin/env python3
"""Audit real GLB payloads and authored texture budgets; not visual/phone sign-off."""
from __future__ import annotations

import argparse
from collections import Counter
from hashlib import sha256
from io import BytesIO
import json
from pathlib import Path
import struct
import sys

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
EXPECTED = {'warehouse_shell', 'truck', 'inspection_bench', 'conveyor', 'export_gate', 'rack', 'pallet', 'parcel'}


def require(ok, message):
    if not ok:
        raise ValueError(message)


def read_glb(path):
    data = path.read_bytes()
    require(len(data) >= 20, 'Truncated GLB: ' + path.name)
    magic, version, length = struct.unpack_from('<III', data)
    require(magic == 0x46546C67 and version == 2 and length == len(data), 'Invalid GLB header: ' + path.name)
    cursor, document, binary = 12, None, b''
    while cursor < len(data):
        chunk_len, kind = struct.unpack_from('<II', data, cursor)
        cursor += 8
        payload = data[cursor:cursor + chunk_len]
        require(len(payload) == chunk_len, 'Truncated GLB chunk')
        cursor += chunk_len
        if kind == 0x4E4F534A:
            require(document is None, 'Duplicate JSON chunk')
            document = json.loads(payload.rstrip(b' \x00'))
        elif kind == 0x004E4942:
            binary = payload
    require(document is not None and cursor == len(data), 'GLB JSON missing')
    require(document.get('asset', {}).get('version') == '2.0', 'Not glTF2')
    for buffer in document.get('buffers', []):
        require('uri' not in buffer, 'External GLB buffer is not self-contained: ' + path.name)
        require(buffer['byteLength'] <= len(binary), 'GLB buffer length exceeds payload')
    for image in document.get('images', []):
        require('uri' not in image, 'External GLB image is not self-contained: ' + path.name)
    return data, document, binary


def matrix_product(left, right):
    return [[sum(left[row][k] * right[k][col] for k in range(4)) for col in range(4)] for row in range(4)]


def node_matrix(node):
    if 'matrix' in node:
        return [[node['matrix'][col * 4 + row] for col in range(4)] for row in range(4)]
    x, y, z, w = node.get('rotation', [0, 0, 0, 1])
    scale = node.get('scale', [1, 1, 1])
    position = node.get('translation', [0, 0, 0])
    rotation = [[1 - 2*y*y - 2*z*z, 2*x*y - 2*z*w, 2*x*z + 2*y*w],
                [2*x*y + 2*z*w, 1 - 2*x*x - 2*z*z, 2*y*z - 2*x*w],
                [2*x*z - 2*y*w, 2*y*z + 2*x*w, 1 - 2*x*x - 2*y*y]]
    return [[rotation[row][col] * scale[col] for col in range(3)] + [position[row]] for row in range(3)] + [[0, 0, 0, 1]]


def scene_bounds(gltf):
    identity = [[1 if row == col else 0 for col in range(4)] for row in range(4)]
    minimum, maximum = [float('inf')] * 3, [-float('inf')] * 3
    def visit(index, parent):
        node = gltf['nodes'][index]
        transform = matrix_product(parent, node_matrix(node))
        if 'mesh' in node:
            for primitive in gltf['meshes'][node['mesh']]['primitives']:
                accessor = gltf['accessors'][primitive['attributes']['POSITION']]
                for mask in range(8):
                    corner = [accessor['max' if mask & (1 << axis) else 'min'][axis] for axis in range(3)] + [1]
                    point = [sum(transform[row][col] * corner[col] for col in range(4)) for row in range(3)]
                    for axis in range(3):
                        minimum[axis] = min(minimum[axis], point[axis])
                        maximum[axis] = max(maximum[axis], point[axis])
        for child in node.get('children', []):
            visit(child, transform)
    for index in gltf['scenes'][gltf.get('scene', 0)]['nodes']:
        visit(index, identity)
    return minimum, maximum


def audit_model(path, metadata, texture_edge):
    raw, gltf, binary = read_glb(path)
    require(sha256(raw).hexdigest() == metadata['sha256'] and len(raw) == metadata['bytes'], 'Manifest mismatch: ' + path.name)
    accessors = gltf.get('accessors', [])
    triangles, surfaces, vertices = 0, 0, 0
    position_min, position_max = [float('inf')] * 3, [-float('inf')] * 3
    for mesh in gltf.get('meshes', []):
        for primitive in mesh.get('primitives', []):
            require(primitive.get('mode', 4) == 4, 'Non-triangle draw primitive: ' + path.name)
            positions = accessors[primitive['attributes']['POSITION']]
            count = accessors[primitive['indices']]['count'] if 'indices' in primitive else positions['count']
            require(count % 3 == 0 and count > 0, 'Invalid triangle count: ' + path.name)
            triangles += count // 3
            vertices += positions['count']
            surfaces += 1
            require('min' in positions and 'max' in positions, 'Position bounds missing')
            for axis in range(3):
                position_min[axis] = min(position_min[axis], positions['min'][axis])
                position_max[axis] = max(position_max[axis], positions['max'][axis])
    require(triangles == metadata['triangles'] and triangles > 0, 'Triangle inventory differs from Blender manifest: ' + path.name)
    materials = []
    for material in gltf.get('materials', []):
        pbr = material.get('pbrMetallicRoughness', {})
        roughness = pbr.get('roughnessFactor', 1.0)
        metallic = pbr.get('metallicFactor', 1.0)
        require(0 <= roughness <= 1 and 0 <= metallic <= 1, 'Invalid PBR factors')
        require(material.get('alphaMode', 'OPAQUE') == 'OPAQUE', 'Authored world props should not add transparent overdraw')
        materials.append({'name': material.get('name'), 'roughness_factor': roughness, 'metallic_factor': metallic,
                          'has_albedo_texture': 'baseColorTexture' in pbr, 'has_normal_texture': 'normalTexture' in material})
    images = []
    for entry in gltf.get('images', []):
        view = gltf['bufferViews'][entry['bufferView']]
        start = view.get('byteOffset', 0)
        payload = binary[start:start + view['byteLength']]
        with Image.open(BytesIO(payload)) as image:
            image.verify()
        with Image.open(BytesIO(payload)) as image:
            width, height = image.size
            require(0 < width <= texture_edge and 0 < height <= texture_edge, 'Texture exceeds mobile provisional edge budget')
            images.append({'width': width, 'height': height, 'encoded_bytes': len(payload),
                           'sha256': sha256(payload).hexdigest(),
                           'rgba8_mip_chain_bytes_estimate': int(width * height * 4 * 4 / 3)})
    actual_minimum, actual_maximum = scene_bounds(gltf)
    if path.stem == 'conveyor':
        for axis in (0, 2):
            require(actual_minimum[axis] >= -0.501 and actual_maximum[axis] <= 0.501, 'Conveyor geometry exceeds its visual one-cell preview footprint')
    return {'file': str(path.relative_to(ROOT)), 'bytes': len(raw), 'sha256': sha256(raw).hexdigest(),
            'triangles': triangles, 'vertices': vertices, 'material_surfaces': surfaces,
            'mesh_count': len(gltf.get('meshes', [])), 'node_count': len(gltf.get('nodes', [])),
            'accessor_local_bounds_min': position_min, 'accessor_local_bounds_max': position_max,
            'scene_bounds_min': actual_minimum, 'scene_bounds_max': actual_maximum,
            'materials': materials, 'embedded_images': images}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=ROOT.parent / 'builds/godot-mobile/asset-audit.json')
    parser.add_argument('--max-triangles', type=int, default=80000, help='Provisional unique-asset budget, not a phone measurement')
    parser.add_argument('--max-texture-edge', type=int, default=1024)
    args = parser.parse_args()
    manifest_path = ROOT / 'assets/ART_MANIFEST.json'
    manifest = json.loads(manifest_path.read_text())
    require(set(manifest['models']) == EXPECTED, 'Required eight authored models are missing or unexpected')
    require(manifest['reference_match_verified'] is False, 'Reference match cannot be asserted without actual comparison')
    require(manifest['source_status'] == 'P_ORIGINAL_ART', 'Original provisional art provenance is required')
    models = {}
    for name, metadata in manifest['models'].items():
        path = ROOT / metadata['file']
        require(path.is_file() and not path.is_symlink(), 'Missing or linked model: ' + name)
        models[name] = audit_model(path, metadata, args.max_texture_edge)
    triangles = sum(model['triangles'] for model in models.values())
    require(triangles <= args.max_triangles, 'Unique asset triangles exceed provisional budget')
    require(triangles == manifest['unique_model_triangles'], 'Total Blender triangle inventory mismatch')
    encoded_texture_hashes = Counter(image['sha256'] for model in models.values() for image in model['embedded_images'])
    texture_files = {}
    for name, metadata in manifest['textures'].items():
        path = ROOT / name if name.startswith('assets/') else ROOT / 'assets/textures' / name
        require(path.is_file() and not path.is_symlink(), 'Texture file missing: ' + name)
        require(path.stat().st_size == metadata['bytes'] and sha256(path.read_bytes()).hexdigest() == metadata['sha256'], 'Texture manifest mismatch')
        with Image.open(path) as image:
            require(max(image.size) <= args.max_texture_edge, 'Authored texture edge exceeds budget')
            texture_files[name] = {'width': image.width, 'height': image.height, 'bytes': path.stat().st_size}
    report = {'status': 'PASS', 'scope': 'static_real_glb_payload_and_texture_audit',
              'budget_status': 'P_TECHNICAL_PROVISIONAL_NOT_PHONE_VERIFIED',
              'unique_asset_triangles': triangles, 'unique_model_bytes': sum(m['bytes'] for m in models.values()),
              'provisional_unique_triangle_limit': args.max_triangles, 'texture_edge_limit': args.max_texture_edge,
              'texture_files': texture_files, 'models': models,
              'embedded_images_repeated_across_models': {key: count for key, count in encoded_texture_hashes.items() if count > 1},
              'embedded_rgba8_mip_memory_upper_estimate_bytes': sum(i['rgba8_mip_chain_bytes_estimate'] for m in models.values() for i in m['embedded_images']),
              'memory_estimate_limitations': 'Assumes duplicate embedded images remain distinct RGBA8 resources; actual GPU compression, sharing and mip allocation require runtime/device measurements.',
              'draw_call_limitations': 'Asset surfaces are not runtime draw calls. Instancing, section culling, UI, shadows and render passes affect actual counters.',
              'reference_parity': 'NOT_RUN_REFERENCE_UNAVAILABLE', 'actual_phone_fps': 'NOT_RUN', 'phone_thermal_30_minutes': 'NOT_RUN',
              'overall_product_acceptance': 'NOT_RUN', 'manifest_sha256': sha256(manifest_path.read_bytes()).hexdigest()}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    print(f'ASSET AUDIT PASS: {len(models)} actual GLBs; {triangles:,} unique triangles; texture bounds and manifest hashes verified. Reference/phone/thermal NOT_RUN.')


if __name__ == '__main__':
    try:
        main()
    except (OSError, ValueError, KeyError, struct.error) as error:
        print('ASSET AUDIT FAIL: ' + str(error), file=sys.stderr)
        sys.exit(1)
