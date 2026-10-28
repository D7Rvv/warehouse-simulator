#!/usr/bin/env python3
"""Create a self-contained local HTML for Scriptable; never host or publish."""
import argparse
import base64
import hashlib
import html
import json
from pathlib import Path
import re
import shutil
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT.parent / 'builds/godot-mobile/iphone-local'


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def runtime_sources():
    paths = [ROOT / n for n in ['main.gd', 'main.gd.uid', 'main.tscn', 'project.godot', 'export_presets.cfg', 'icon.svg', 'icon.svg.import', 'assets/ART_MANIFEST.json', 'tools/package_iphone_local.py']]
    for directory, pattern in [('scripts', '*.gd'), ('scripts', '*.uid'), ('data', '*.json'), ('assets/models', '*'), ('assets/fonts', '*'), ('licenses', '*.txt'), ('iphone-local', '*')]:
        paths.extend(p for p in (ROOT / directory).glob(pattern) if p.is_file())
    return {p.relative_to(ROOT).as_posix(): sha(p) for p in sorted(paths)}


def export_hashes(web):
    return {p.name: sha(p) for p in sorted(web.iterdir()) if p.is_file()}


def run_godot(arguments, log_path):
    result = subprocess.run([str(ROOT / 'tools/godot.sh'), *arguments], cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    log_path.write_text(result.stdout)
    if result.returncode or 'ERROR:' in result.stdout or 'SCRIPT ERROR:' in result.stdout:
        raise RuntimeError('Godot command failed; inspect ' + str(log_path))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--reuse-export', action='store_true')
    args = parser.parse_args()
    web = OUT / 'web'
    web.mkdir(parents=True, exist_ok=True)
    version = subprocess.check_output([str(ROOT / 'tools/godot.sh'), '--version'], text=True).strip()
    if not version.startswith('4.6.3.'):
        raise RuntimeError('Expected Godot4.6.3; found ' + version)
    cohort_path = OUT / 'export-source-cohort.json'
    if not args.reuse_export:
        # Finish import before freezing UID/import metadata, then prove that the
        # source cohort remained fixed throughout the actual export.
        run_godot(['--headless', '--editor', '--quit'], OUT / 'import.log')
        original = runtime_sources()
        run_godot(['--headless', '--export-release', 'iPhone Local Review', str(web / 'index.html')], OUT / 'export.log')
        if original != runtime_sources():
            raise RuntimeError('Runtime source changed during export; freeze and retry')
        cohort = {'engine_version': version, 'runtime_source_sha256': original, 'exported_web_sha256': export_hashes(web)}
        cohort_path.write_text(json.dumps(cohort, indent=2) + '\n')
    else:
        if not cohort_path.is_file():
            raise RuntimeError('No verified export cohort; run without --reuse-export first')
        cohort = json.loads(cohort_path.read_text())
        original = runtime_sources()
        if cohort.get('runtime_source_sha256') != original or cohort.get('engine_version') != version:
            raise RuntimeError('Source or engine differs from verified export cohort; rebuild')
        if cohort.get('exported_web_sha256') != export_hashes(web):
            raise RuntimeError('Export bytes differ from verified export cohort; rebuild')
    engine = (web / 'index.js').read_text()
    wasm, pack = (web / 'index.wasm').read_bytes(), (web / 'index.pck').read_bytes()
    assert wasm[:4] == b'\0asm' and pack[:4] == b'GDPC'
    assert 'const GODOT_THREADS_ENABLED = false;' in (web / 'index.html').read_text()
    shell = (ROOT / 'iphone-local/shell.html').read_text()
    notices = {p.name: p.read_text() for p in (ROOT / 'licenses').glob('*.txt')}
    notices['FONT-OFL-LICENSE.txt'] = (ROOT / 'assets/fonts/LICENSE.txt').read_text()
    text = shell.replace('@@WASM_BASE64@@', base64.b64encode(wasm).decode('ascii')).replace('@@PCK_BASE64@@', base64.b64encode(pack).decode('ascii'))
    text = text.replace('@@ENGINE_JS@@', re.sub('</script', r'<\\/script', engine, flags=re.I))
    text = text.replace('@@LICENSE_NOTICES_HTML@@', html.escape('\n\n'.join(name + '\n' + value for name, value in notices.items())))
    assert not re.search(r'@@[A-Z_]+@@', text)
    standalone = OUT / 'WarehouseStation-iPhone.html'
    standalone.write_text(text)
    for label, expected in [('wasm', wasm), ('pck', pack)]:
        encoded = re.search(r'<script id="' + label + r'-payload"[^>]*>(.*?)</script>', text, re.S).group(1)
        assert hashlib.sha256(base64.b64decode(encoded, validate=True)).hexdigest() == hashlib.sha256(expected).hexdigest()
    for name in ['Open-Warehouse.js', 'README-ja.txt']:
        shutil.copy2(ROOT / 'iphone-local' / name, OUT / name)
    files = {name: {'bytes': (OUT / name).stat().st_size, 'sha256': sha(OUT / name)} for name in ['WarehouseStation-iPhone.html', 'Open-Warehouse.js', 'README-ja.txt']}
    manifest = {'engine': 'Godot 4.6.3 standard', 'delivery': 'private_local_file_for_Scriptable_WebView_on_iPhone', 'scope': 'S0_S1_readonly_station_controls_not_full_logistics_game', 'runtime_source_sha256': original, 'exported_web_sha256': cohort['exported_web_sha256'], 'export_source_cohort_verified': True, 'files': files, 'embedded_payload_sha256_verified': True, 'network_required_for_game': False, 'requires_PC': False, 'requires_public_hosting': False, 'requires_Unity': False, 'container_app': 'Scriptable; installation from App Store required', 'minimum_engine_safari_version': '15.2', 'minimum_Scriptable_app_iOS': 'CHECK_APP_STORE_REQUIREMENT', 'iPhone_physical_execution': 'NOT_RUN', 'phone_fps_and_heat': 'NOT_RUN', 'audio': 'Dummy_muted_for_local_review', 'save_and_IDBFS': 'disabled_readonly_prototype'}
    manifest_path = OUT / 'manifest.json'
    manifest_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + '\n')
    archive = OUT.parent / 'WarehouseStation-iPhone-Offline.zip'
    with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for name in [*files, 'manifest.json']:
            z.write(OUT / name, 'WarehouseStation-iPhone/' + name)
    all_entry_hashes = {name: sha(OUT / name) for name in [*files, 'manifest.json']}
    with zipfile.ZipFile(archive) as z:
        assert z.testzip() is None
        assert set(z.namelist()) == {'WarehouseStation-iPhone/' + name for name in all_entry_hashes}
        for name, expected in all_entry_hashes.items():
            assert hashlib.sha256(z.read('WarehouseStation-iPhone/' + name)).hexdigest() == expected
    assert runtime_sources() == original, 'Runtime changed during packaging'
    archive.with_suffix('.zip.sha256').write_text(sha(archive) + '  ' + archive.name + '\n')
    delivery = {'status': 'PASS', 'scope': 'build_payload_and_archive_integrity_not_iPhone_acceptance', 'file': archive.name, 'bytes': archive.stat().st_size, 'sha256': sha(archive), 'archive_entry_sha256': all_entry_hashes, 'zip_crc_and_all_entry_sha256': 'PASS', 'manifest': manifest}
    (OUT / 'delivery.json').write_text(json.dumps(delivery, indent=2, ensure_ascii=False) + '\n')
    print('IPHONE LOCAL PACKAGE', archive, archive.stat().st_size, 'bytes; not published')


if __name__ == '__main__':
    main()
