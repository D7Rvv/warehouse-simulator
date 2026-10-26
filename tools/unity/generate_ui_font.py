#!/usr/bin/env python3
"""Subset the licensed Noto JP face for the current prototype UI (optional fontTools)."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import fontTools
from fontTools import subset
from fontTools.ttLib import TTFont

ROOT = Path(__file__).resolve().parents[2]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True, help='Original NotoSansCJK-Regular.ttc, JP face index0')
    args = parser.parse_args()
    characters = set(chr(i) for i in range(32, 127))
    inputs = sorted((ROOT / 'Assets/Warehouse').rglob('*.cs'))
    inputs.append(ROOT / 'docs/data/prototype_station_fixture_v0_1.json')
    for path in inputs:
        text = path.read_text()
        characters.update(text)
        characters.update(chr(int(value, 16)) for value in re.findall(r'\\u([0-9a-fA-F]{4})', text))
    characters = {c for c in characters if ord(c) >= 32}
    font = TTFont(args.source, fontNumber=0, recalcTimestamp=False)
    available = font.getBestCmap()
    missing = sorted(ord(c) for c in characters if ord(c) not in available)
    options = subset.Options()
    options.layout_features = ['*']
    options.name_IDs = ['*']
    options.name_legacy = True
    options.name_languages = ['*']
    sub = subset.Subsetter(options=options)
    sub.populate(unicodes=sorted(ord(c) for c in characters))
    sub.subset(font)
    for record in font['name'].names:
        if record.nameID in (1, 4, 6, 16):
            name = 'WarehousePrototypeJP-Regular' if record.nameID == 6 else 'Warehouse Prototype JP'
            record.string = name.encode(record.getEncoding())
    target = ROOT / 'Assets/Resources/WarehouseJapanese.otf'
    font.save(target)
    report = {
        'description': 'S0/S1 Japanese UI font subset; regenerate when adding characters.',
        'source_family': 'Noto Sans CJK JP', 'source_face': 0,
        'license': 'SIL Open Font License 1.1; WarehouseJapanese-LICENSE.txt',
        'source_sha256': hashlib.sha256(args.source.read_bytes()).hexdigest(),
        'fonttools_version': fontTools.__version__, 'characters_requested': len(characters),
        'missing_codepoints': ['U+%04X' % code for code in missing],
        'output_bytes': target.stat().st_size,
        'output_sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
    }
    (target.parent / 'WarehouseJapanese-FONT.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    print(json.dumps(report, ensure_ascii=False))


if __name__ == '__main__':
    main()
