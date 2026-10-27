#!/usr/bin/env python3
"""Exercise an exported Linux executable via X11, with no Godot editor or source mount.

Requires an X11 display, libX11 and libXtst. Captures genuine display pixels; no
GDScript injection is available in the official release template. This tests
release startup/resources and mouse navigation, not phone or thermal acceptance.
"""
import argparse
import ctypes as c
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import time
import zlib


class XImage(c.Structure):
    _fields_ = [(name, kind) for name, kind in [
        ('width', c.c_int), ('height', c.c_int), ('xoffset', c.c_int),
        ('format', c.c_int), ('data', c.c_void_p), ('byte_order', c.c_int),
        ('bitmap_unit', c.c_int), ('bitmap_bit_order', c.c_int),
        ('bitmap_pad', c.c_int), ('depth', c.c_int), ('bytes_per_line', c.c_int),
        ('bits_per_pixel', c.c_int), ('red_mask', c.c_ulong),
        ('green_mask', c.c_ulong), ('blue_mask', c.c_ulong)]]


def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--executable', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    executable = args.executable.resolve(strict=True)
    x11, xtst = c.CDLL('libX11.so.6'), c.CDLL('libXtst.so.6')
    x11.XOpenDisplay.argtypes, x11.XOpenDisplay.restype = [c.c_char_p], c.c_void_p
    x11.XDefaultRootWindow.argtypes, x11.XDefaultRootWindow.restype = [c.c_void_p], c.c_ulong
    x11.XGetImage.argtypes = [c.c_void_p, c.c_ulong, c.c_int, c.c_int, c.c_uint, c.c_uint, c.c_ulong, c.c_int]
    x11.XGetImage.restype = c.POINTER(XImage)
    x11.XDestroyImage.argtypes = [c.POINTER(XImage)]
    x11.XFlush.argtypes = [c.c_void_p]
    x11.XCloseDisplay.argtypes = [c.c_void_p]
    x11.XQueryTree.argtypes = [c.c_void_p, c.c_ulong, c.POINTER(c.c_ulong), c.POINTER(c.c_ulong), c.POINTER(c.POINTER(c.c_ulong)), c.POINTER(c.c_uint)]
    x11.XFetchName.argtypes = [c.c_void_p, c.c_ulong, c.POINTER(c.c_char_p)]
    x11.XFree.argtypes = [c.c_void_p]
    x11.XSetInputFocus.argtypes = [c.c_void_p, c.c_ulong, c.c_int, c.c_ulong]
    x11.XRaiseWindow.argtypes = [c.c_void_p, c.c_ulong]
    xtst.XTestFakeMotionEvent.argtypes = [c.c_void_p, c.c_int, c.c_int, c.c_int, c.c_ulong]
    xtst.XTestFakeButtonEvent.argtypes = [c.c_void_p, c.c_uint, c.c_int, c.c_ulong]
    display = x11.XOpenDisplay(None)
    if not display:
        raise RuntimeError('NOT_RUN: an actual X11 display is required.')
    root = x11.XDefaultRootWindow(display)
    checks, captures = [], []

    def check(ok, description):
        checks.append({'status': 'PASS' if ok else 'FAIL', 'description': description})
        if not ok:
            raise RuntimeError(description)

    def click(x, y):
        xtst.XTestFakeMotionEvent(display, -1, x, y, 0)
        xtst.XTestFakeButtonEvent(display, 1, 1, 0)
        x11.XFlush(display)
        time.sleep(0.5)
        xtst.XTestFakeButtonEvent(display, 1, 0, 0)
        x11.XFlush(display)
        time.sleep(2)

    def capture(name):
        image = x11.XGetImage(display, root, 0, 0, 1280, 720, c.c_ulong(-1).value, 2)
        if not image:
            raise RuntimeError('X11 screenshot failed')
        try:
            info = image.contents
            if (info.bits_per_pixel, info.byte_order, info.red_mask, info.green_mask, info.blue_mask) != (32, 0, 0xff0000, 0xff00, 0xff):
                raise RuntimeError('Unsupported X11 pixel format; refusing to fabricate colors')
            raw = c.string_at(info.data, info.bytes_per_line * info.height)
            rgb = bytearray(info.width * info.height * 3)
            for y in range(info.height):
                row = raw[y * info.bytes_per_line:y * info.bytes_per_line + info.width * 4]
                start = y * info.width * 3
                rgb[start:start + info.width * 3:3] = row[2::4]
                rgb[start + 1:start + info.width * 3:3] = row[1::4]
                rgb[start + 2:start + info.width * 3:3] = row[0::4]
            def chunk(kind, value):
                return struct.pack('>I', len(value)) + kind + value + struct.pack('>I', zlib.crc32(kind + value))
            filtered = b''.join(b'\0' + rgb[y * 3840:(y + 1) * 3840] for y in range(720))
            path = args.output / (name + '.png')
            path.write_bytes(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 1280, 720, 8, 2, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(filtered, 6)) + chunk(b'IEND', b''))
            colors = len({bytes(rgb[i:i + 3]) for i in range(0, len(rgb), 51)})
            world_pixels = b''.join(rgb[y * 3840:y * 3840 + 980 * 3] for y in range(60, 650))
            captures.append({'file': path.name, 'width': 1280, 'height': 720, 'sha256': sha(path), 'world_region_sha256': hashlib.sha256(world_pixels).hexdigest(), 'sampled_color_count': colors})
            check(colors > 100, name + ': display contains a rendered scene, pending visual review')
        finally:
            x11.XDestroyImage(image)

    process = None
    status = 'FAIL'
    try:
        with (args.output / 'stdout.log').open('w') as log:
            process = subprocess.Popen([str(executable), '--resolution', '1280x720', '--position', '0,0', '--rendering-method', 'gl_compatibility', '--audio-driver', 'Dummy', '--verbose', '--log-file', str((args.output / 'application.log').resolve())], cwd='/tmp', stdout=log, stderr=subprocess.STDOUT)
            time.sleep(10)
            check(process.poll() is None, 'Exported application remains running after startup')
            root_return, parent_return, children, count = c.c_ulong(), c.c_ulong(), c.POINTER(c.c_ulong)(), c.c_uint()
            check(bool(x11.XQueryTree(display, root, c.byref(root_return), c.byref(parent_return), c.byref(children), c.byref(count))), 'X11 application window query succeeds')
            focused = False
            try:
                for i in range(count.value):
                    title = c.c_char_p()
                    if x11.XFetchName(display, children[i], c.byref(title)) and title.value:
                        if b'Warehouse Simulator' in title.value:
                            x11.XRaiseWindow(display, children[i])
                            x11.XSetInputFocus(display, children[i], 2, 0)
                            focused = True
                        x11.XFree(title)
            finally:
                if children:
                    x11.XFree(children)
            check(focused, 'Release window explicitly focused before input assertions')
            x11.XFlush(display)
            time.sleep(1)
            capture('overview')
            for name, x in [('inbound', 109), ('inspection', 321), ('build', 958)]:
                click(x, 690)
                check(process.poll() is None, name + ': native mouse navigation does not terminate the application')
                capture(name)
            check(len({item['world_region_sha256'] for item in captures}) == 4, 'External navigation changes the actual 3D region in all four views, excluding elapsed-clock text')
            status = 'PASS'
    finally:
        if process is not None and process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
        x11.XCloseDisplay(display)
        report = {'status': status, 'scope': 'exported_linux_process_x11_mouse_and_pixels_not_internal_state_or_phone', 'checks': checks, 'captures': captures, 'executable_sha256': sha(executable), 'no_project_mount_observed': not Path('/project').exists(), 'development_source_available': Path('/workspace/warehouse-simulator/mobile/project.godot').exists(), 'visual_review': 'REQUIRED', 'phone_fps': 'NOT_RUN', 'phone_thermal': 'NOT_RUN', 'windows_os_execution': 'NOT_RUN'}
        (args.output / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
        print('ISOLATED RELEASE', status, len(checks), 'checks; visual review required')


if __name__ == '__main__':
    main()
