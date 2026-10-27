#!/usr/bin/env python3
"""Export license-free native review builds and an isolated source ZIP, never publish."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tarfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT.parent/'builds/godot-mobile/packages'
PRIVATE = {'.p12','.pfx','.jks','.keystore','.key','.ulf','.pem'}


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream,'sha256').hexdigest()


def sources():
    values={}
    for p in sorted(ROOT.rglob('*')):
        relative=p.relative_to(ROOT)
        if any(part in {'.godot','builds','__pycache__'} for part in relative.parts):continue
        if p.suffix in {'.blend','.blend1','.pyc'}:continue
        if p.is_symlink():raise RuntimeError('Source symlink rejected: '+str(relative))
        if not p.is_file():continue
        if p.suffix.lower() in PRIVATE or p.name.startswith('.env'):raise RuntimeError('Private filename rejected: '+str(relative))
        values[relative.as_posix()]={'bytes':p.stat().st_size,'sha256':digest(p)}
    return values


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-only',action='store_true')
    args=parser.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    warmup=subprocess.run([str(ROOT/'tools/godot.sh'),'--headless','--editor','--quit'],cwd=ROOT,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    (OUT/'import.log').write_text(warmup.stdout)
    if warmup.returncode or 'SCRIPT ERROR:' in warmup.stdout or 'ERROR:' in warmup.stdout:
        raise RuntimeError('Godot import failed before export freeze.')
    original=sources()
    (OUT/'source-before-export.json').write_text(json.dumps(original,indent=2)+'\n')
    records=[]
    binary_sources=original if not args.source_only else None
    # Refreshing documentation/source must not discard already verified binary
    # provenance. Retain only unchanged local artifacts and their original status.
    if args.source_only and (OUT/'manifest.json').is_file():
        previous=json.loads((OUT/'manifest.json').read_text())
        binary_sources=previous.get('binary_export_source_inputs',previous.get('source_inputs'))
        for record in previous.get('artifacts',[]):
            if record.get('file') == 'WarehouseStation-Godot-Source.zip':continue
            artifact=OUT/record['file']
            if artifact.is_file() and digest(artifact)==record.get('sha256'):
                records.append(record)
    if not args.source_only:
        for preset,name,platform in [('Linux Review','WarehouseStation.x86_64','linux_x86_64'),('Windows Review','WarehouseStation.exe','windows_x86_64')]:
            binary=OUT/name
            command=[str(ROOT/'tools/godot.sh'),'--headless','--export-release',preset,str(binary)]
            result=subprocess.run(command,cwd=ROOT,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
            log=OUT/(platform+'-export.log');log.write_text(result.stdout)
            if result.returncode or 'SCRIPT ERROR:' in result.stdout or 'ERROR:' in result.stdout or not binary.is_file():
                raise RuntimeError('Native export failed; inspect '+str(log))
            expected=b'MZ' if platform.startswith('windows') else b'\x7fELF'
            with binary.open('rb') as stream:assert stream.read(len(expected))==expected
            if platform.startswith('linux'):binary.chmod(0o755)
            record={'platform':platform,'file':name,'bytes':binary.stat().st_size,'sha256':digest(binary),'execution':'NOT_RUN','signature':'unsigned_review_build'}
            records.append(record)
            print('EXPORTED',platform,record['bytes'],'bytes; execution NOT_RUN')
        staging=OUT/'review-notices'
        staging.mkdir(exist_ok=True)
        for p in (ROOT/'licenses').glob('*.txt'):shutil.copy2(p,staging/p.name)
        shutil.copy2(ROOT/'assets/fonts/LICENSE.txt',staging/'FONT-OFL-LICENSE.txt')
        (staging/'README.txt').write_text('Warehouse Simulator — Godot station prototype\n\nNo Unity, Unity license, .NET or Blender is needed to run the native executable.\nLandscape mouse/touch station controls. This S0/S1 fixture does not perform logistics, purchases or export revenue. Default cap30fps, quality can be lowered from the menu.\n\nWindows binary export is not Windows OS execution verification. iPhone/Android native performance and 30-minute heat tests remain NOT_RUN.\nGodot engine and font notices are included; original game/art ownership follows the repository policy.\n')
        win=OUT/'WarehouseStation-Windows.zip'
        with zipfile.ZipFile(win,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
            z.write(OUT/'WarehouseStation.exe','WarehouseStation/WarehouseStation.exe')
            for p in sorted(staging.iterdir()):z.write(p,'WarehouseStation/'+p.name)
        with zipfile.ZipFile(win) as z:assert z.testzip() is None
        linux=OUT/'WarehouseStation-Linux.tar.gz'
        with tarfile.open(linux,'w:gz') as t:
            t.add(OUT/'WarehouseStation.x86_64',arcname='WarehouseStation/WarehouseStation.x86_64')
            for p in sorted(staging.iterdir()):t.add(p,arcname='WarehouseStation/'+p.name)
    current=sources()
    changed=[name for name in sorted(set(original)|set(current)) if original.get(name)!=current.get(name)]
    assert not changed,'Project changed during export; freeze and retry: '+str(changed)
    source=OUT/'WarehouseStation-Godot-Source.zip'
    with zipfile.ZipFile(source,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
        for name in original:z.write(ROOT/name,'WarehouseStation-Godot/'+name)
    with zipfile.ZipFile(source) as z:
        assert z.testzip() is None
        for name,metadata in original.items():assert hashlib.sha256(z.read('WarehouseStation-Godot/'+name)).hexdigest()==metadata['sha256']
    for p in [source,*([OUT/'WarehouseStation-Windows.zip',OUT/'WarehouseStation-Linux.tar.gz'] if not args.source_only else [])]:
        records.append({'file':p.name,'bytes':p.stat().st_size,'sha256':digest(p)})
        p.with_suffix(p.suffix+'.sha256').write_text(digest(p)+'  '+p.name+'\n')
    report={'engine':'Godot4.6.3standard','status':'exported_source_verified_not_published','artifacts':records,'source_inputs':original,'binary_export_source_inputs':binary_sources,'source_only_refresh':args.source_only,'source_zip_crc':'PASS','source_zip_every_entry_sha256':'PASS','native_phone_performance':'NOT_RUN','thermal_30_minutes':'NOT_RUN'}
    (OUT/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
    print('SOURCE ZIP VERIFIED',len(original),'files; no publication performed.')

if __name__=='__main__':main()
