#!/usr/bin/env python3
"""Regenerate the OFL Japanese UI subset; requires fontTools and original Noto TTC."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import fontTools
from fontTools import subset
from fontTools.ttLib import TTFont

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--source', type=Path, required=True)
args = parser.parse_args()
characters = set(chr(i) for i in range(32, 127))
inputs = [ROOT/'main.gd', *sorted((ROOT/'scripts').glob('*.gd')), ROOT/'data/prototype_station_fixture_v0_1.json']
for path in inputs:
    text = path.read_text()
    characters.update(text)
    characters.update(chr(int(value, 16)) for value in re.findall(r'\\u([0-9a-fA-F]{4})', text))
characters = {c for c in characters if ord(c) >= 32}
font = TTFont(args.source, fontNumber=0, recalcTimestamp=False)
missing = sorted(ord(c) for c in characters if ord(c) not in font.getBestCmap())
options = subset.Options()
options.layout_features = ['*']
options.name_IDs = ['*']
options.name_legacy = True
options.name_languages = ['*']
builder = subset.Subsetter(options=options)
builder.populate(unicodes=sorted(ord(c) for c in characters))
builder.subset(font)
for entry in font['name'].names:
    if entry.nameID in (1,4,6,16):
        value = 'WarehouseStationJP-Regular' if entry.nameID == 6 else 'Warehouse Station JP'
        entry.string = value.encode(entry.getEncoding())
target = ROOT/'assets/fonts/warehouse_jp.otf'
font.save(target)
record = {'source_family':'Noto Sans CJK JP','source_face':0,'license':'SIL OFL1.1; LICENSE.txt','source_sha256':hashlib.sha256(args.source.read_bytes()).hexdigest(),'fonttools':fontTools.__version__,'requested_codepoints':sorted(ord(c) for c in characters),'missing_codepoints':missing,'output_bytes':target.stat().st_size,'output_sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
(target.parent/'FONT_MANIFEST.json').write_text(json.dumps(record,indent=2)+'\n')
assert not missing,missing
print('Japanese font:',target.stat().st_size,'bytes;',len(characters),'requested characters, no missing glyphs.')
