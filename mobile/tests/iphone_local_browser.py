#!/usr/bin/env python3
"""Exercise the actual standalone file in Chromium with frame-spanning touches.

Default mode has no HTTP server. An explicit loopback fallback verifies the same
HTML bytes but is not file:// verification. Neither mode uses request routing,
secure-context overrides or a Godot test bridge. Pixel observations are not
assertions about internal Station IDs/domain state.
"""
import argparse
import asyncio
import hashlib
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
from pathlib import Path
import shutil
import threading
from urllib.parse import urlparse

from PIL import Image, ImageChops
from playwright.async_api import async_playwright


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


async def exercise(args):
    args.output.mkdir(parents=True, exist_ok=True)
    html_hash = digest(args.html)
    checks, captures, page_errors, console_errors, requests = [], [], [], [], []
    diagnostic, browser_version, environment_block = {}, '', None
    screenshots = {}
    status = 'FAIL'
    server = None
    url = args.html.resolve().as_uri()
    if args.local_http:
        class LocalDocument(BaseHTTPRequestHandler):
            def do_GET(self):
                if self.path == '/favicon.ico':
                    self.send_response(204)
                    self.end_headers()
                    return
                if self.path != '/WarehouseStation-iPhone.html':
                    self.send_error(404)
                    return
                self.send_response(200)
                self.send_header('Content-Type', 'text/html; charset=utf-8')
                self.send_header('Content-Length', str(args.html.stat().st_size))
                self.send_header('Cache-Control', 'no-store')
                self.end_headers()
                with args.html.open('rb') as stream:
                    shutil.copyfileobj(stream, self.wfile)
            def log_message(self, *_args):
                pass
        server = ThreadingHTTPServer(('127.0.0.1', 0), LocalDocument)
        threading.Thread(target=server.serve_forever, daemon=True).start()
        url = 'http://127.0.0.1:%d/WarehouseStation-iPhone.html' % server.server_port
    def check(ok, description, detail=None):
        checks.append({'status': 'PASS' if ok else 'FAIL', 'description': description, **({'detail': detail} if detail is not None else {})})
        if not ok:
            raise AssertionError(description)
    try:
        async with async_playwright() as playwright:
            browser = await playwright.chromium.launch(executable_path=args.chromium, headless=True, args=['--no-sandbox', '--disable-dev-shm-usage', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'])
            browser_version = browser.version
            context = await browser.new_context(viewport={'width': 844, 'height': 390}, device_scale_factor=1, is_mobile=True, has_touch=True)
            page = await context.new_page()
            page.on('pageerror', lambda error: page_errors.append(str(error)))
            page.on('console', lambda message: console_errors.append(message.text) if message.type == 'error' else None)
            context.on('request', lambda request: requests.append({'scheme': urlparse(request.url).scheme, 'url': request.url if not request.url.startswith('data:') else 'data:[embedded]'}))
            cdp = await context.new_cdp_session(page)
            async def touch(kind, points):
                await cdp.send('Input.dispatchTouchEvent', {'type': kind, 'touchPoints': [{'x': float(x), 'y': float(y), 'id': i, 'radiusX': 1, 'radiusY': 1, 'force': 1} for i, x, y in points]})
            async def tap(x, y):
                await touch('touchStart', [(1, x, y)])
                await page.wait_for_timeout(180)
                await touch('touchEnd', [])
                await page.wait_for_timeout(700)
            async def drag(start, end, identity=1):
                await touch('touchStart', [(identity, *start)])
                await page.wait_for_timeout(180)
                for step in range(1, 7):
                    ratio = step / 6
                    point = (start[0] + (end[0] - start[0]) * ratio, start[1] + (end[1] - start[1]) * ratio)
                    await touch('touchMove', [(identity, *point)])
                    await page.wait_for_timeout(85)
                await touch('touchEnd', [])
                await page.wait_for_timeout(700)
            async def capture(name, viewport_expected=(844, 390)):
                path = args.output / (name + '.png')
                await page.screenshot(path=str(path))
                with Image.open(path) as raw:
                    image = raw.convert('RGB')
                    check(image.size == viewport_expected, name + ': actual display dimensions', list(image.size))
                    region = image.crop((0, 60, min(570, image.width), min(320, image.height)))
                    colors = len(region.getcolors(region.width * region.height) or [])
                    region_sha = hashlib.sha256(region.tobytes()).hexdigest()
                    screenshots[name] = image.copy()
                captures.append({'name': name, 'file': path.name, 'sha256': digest(path), 'width': image.width, 'height': image.height, 'world_region_sha256': region_sha, 'world_region_color_count': colors})
                return image
            def region_diff(left, right, box):
                return ImageChops.difference(screenshots[left].crop(box), screenshots[right].crop(box)).getbbox() is not None
            def selected_navigation(name):
                # Panels are translucent, so whole-strip bytes can change when
                # the 3D camera moves behind them. Read the green selection
                # indicator instead of treating that compositing as navigation.
                image = screenshots[name]
                scores = []
                for index in range(6):
                    left = int(5 + index * ((844 - 30) / 6 + 4))
                    region = image.crop((left, 337, int(left + (844 - 30) / 6), 384)).tobytes()
                    scores.append(sum(1 for r, g, b in zip(region[0::3], region[1::3], region[2::3]) if g - r > 25 and g - b > 3 and g > 60))
                return {'column': scores.index(max(scores)) if max(scores) > 100 else None, 'green_pixel_counts': scores}

            await page.goto(url, wait_until='domcontentloaded', timeout=120000)
            expected_protocol = 'http:' if args.local_http else 'file:'
            check(await page.evaluate('location.protocol') == expected_protocol, 'The unchanged HTML uses the recorded actual transport: ' + expected_protocol)
            check(await page.evaluate('isSecureContext') is True, 'The actual origin is a genuine secure context without overrides')
            check(await page.locator('#orientation').is_visible() is False, 'Landscape start is unobstructed')
            check(await page.locator('#start').is_enabled(), 'Start button is enabled before initialization')
            await page.locator('#start').click()
            try:
                await page.wait_for_function("window.warehouseLocal?.phase === 'running' || window.warehouseLocal?.phase === 'error'", timeout=180000)
            except Exception:
                await page.screenshot(path=str(args.output / 'startup-failure.png'))
                diagnostic = await page.evaluate('window.warehouseLocal')
                raise
            await page.wait_for_timeout(2000)
            diagnostic = await page.evaluate('window.warehouseLocal')
            check(diagnostic.get('phase') == 'running', 'Local embedded engine reaches running phase', diagnostic)
            check(diagnostic.get('missingFeatures') == [], 'Standard Godot feature checks find no missing requirements')
            check(diagnostic.get('secureContext') is True, 'Loader records the real secure-context result')
            check(diagnostic.get('externalRequests') == 0, 'Local loader attempted no external fetch')
            check(await page.locator('#wasm-payload, #pck-payload').count() == 0, 'Consumed Base64 payload nodes are removed from the DOM')
            check(await page.locator('#welcome').count() == 0, 'Start overlay is removed after initialization')
            graphics = await page.evaluate("""() => { const c=document.getElementById('canvas'), gl=c.getContext('webgl2'); return {width:c.width,height:c.height,webgl2:!!gl,version:gl?gl.getParameter(gl.VERSION):null}; }""")
            check(graphics['webgl2'] and graphics['width'] > 0 and graphics['height'] > 0, 'Actual WebGL2 canvas is initialized', graphics)
            await capture('overview')
            check(captures[-1]['world_region_color_count'] > 100, 'Overview world region has actual rendered detail; visual review still required')

            for name, x in [('inbound', 73), ('inspection', 213), ('export', 353), ('overview_return', 492), ('build', 632)]:
                await tap(x, 360)
                await capture(name)
                check(captures[-1]['world_region_color_count'] > 100, name + ': nonblank actual 3D display after frame-spanning touch')
            chosen = ['inbound', 'inspection', 'export', 'overview_return', 'build']
            values = {next(c['world_region_sha256'] for c in captures if c['name'] == name) for name in chosen}
            check(len(values) == len(chosen), 'Five navigation touches produce distinct 3D display regions; internal Station IDs are not exposed')

            await tap(213, 360)
            await capture('inspection_before_drag')
            await drag((250, 180), (350, 180))
            await capture('inspection_after_drag')
            check(region_diff('inspection_before_drag', 'inspection_after_drag', (80, 90, 550, 310)), 'Frame-spanning native browser drag changes inspection pixels; visual review needed for rotation')
            await tap(766, 286)
            await capture('inspection_after_rotate_button')
            check(region_diff('inspection_after_drag', 'inspection_after_rotate_button', (80, 90, 550, 310)), 'Inspection rotate button produces visible model-region change')
            await tap(644, 286)
            await capture('inspection_label_overlay')
            check(region_diff('inspection_after_rotate_button', 'inspection_label_overlay', (40, 70, 800, 310)), 'Label button produces an actual overlay display change')
            await tap(422, 342)
            await capture('inspection_label_closed')
            check(region_diff('inspection_label_overlay', 'inspection_label_closed', (40, 70, 800, 310)), 'Overlay close area changes the actual displayed image')

            await tap(632, 360)
            await capture('build_before_actions')
            await tap(705, 232)
            await capture('build_after_rotate')
            check(region_diff('build_before_actions', 'build_after_rotate', (0, 60, 836, 320)), 'Preview rotate touch changes the actual display')
            await tap(786, 232)
            await capture('build_after_flip')
            check(region_diff('build_after_rotate', 'build_after_flip', (0, 60, 836, 320)), 'Preview IN/OUT touch changes the actual display')
            await drag((300, 190), (400, 240))
            await capture('build_after_drag')
            check(region_diff('build_after_flip', 'build_after_drag', (0, 60, 836, 320)), 'Frame-spanning build drag changes the visible preview context')
            await tap(624, 286)
            await capture('build_after_cancel')
            check(region_diff('build_after_drag', 'build_after_cancel', (0, 60, 836, 320)), 'Preview cancel touch changes the actual display')

            await tap(492, 360)
            await capture('overview_before_cancel')
            before_nav = selected_navigation('overview_before_cancel')
            check(before_nav['column'] == 3, 'Overview navigation indicator is visibly selected before Cancel regression', before_nav)
            await touch('touchStart', [(10, 213, 360)])
            await page.wait_for_timeout(180)
            await touch('touchCancel', [])
            await page.wait_for_timeout(700)
            await capture('overview_after_touch_cancel')
            after_nav = selected_navigation('overview_after_touch_cancel')
            check(after_nav['column'] == before_nav['column'], 'Canceled native browser navigation touch preserves the selected navigation indicator', after_nav)
            await drag((250, 170), (320, 210))
            await capture('overview_after_pan')
            check(region_diff('overview_after_touch_cancel', 'overview_after_pan', (0, 60, 570, 320)), 'Actual overview drag changes the 3D region')

            await touch('touchStart', [(21, 250, 170), (22, 370, 170)])
            await page.wait_for_timeout(180)
            for step in range(1, 5):
                await touch('touchMove', [(21, 250 - step * 9, 170), (22, 370 + step * 9, 170)])
                await page.wait_for_timeout(85)
            await touch('touchEnd', [])
            await page.wait_for_timeout(700)
            await capture('overview_after_pinch')
            check(region_diff('overview_after_pan', 'overview_after_pinch', (0, 60, 570, 320)), 'Two simultaneous native browser touches change the overview 3D region')
            before_pinch_guard_nav = selected_navigation('overview_after_pinch')
            await touch('touchStart', [(31, 213, 360)])
            await page.wait_for_timeout(180)
            await touch('touchStart', [(31, 213, 360), (32, 250, 170), (33, 370, 170)])
            await page.wait_for_timeout(180)
            await touch('touchMove', [(31, 213, 360), (32, 235, 170), (33, 385, 170)])
            await page.wait_for_timeout(180)
            await touch('touchEnd', [(31, 213, 360)])
            await page.wait_for_timeout(180)
            await touch('touchEnd', [])
            await page.wait_for_timeout(700)
            await capture('overview_after_pinch_ui_guard')
            after_pinch_guard_nav = selected_navigation('overview_after_pinch_ui_guard')
            check(before_pinch_guard_nav['column'] == 3 and after_pinch_guard_nav['column'] == 3, 'A held navigation touch preserves the selected navigation indicator after an overlapping two-finger browser gesture ends', after_pinch_guard_nav)

            await page.set_viewport_size({'width': 390, 'height': 844})
            await page.wait_for_timeout(800)
            check(await page.locator('#orientation').is_visible(), 'Portrait viewport displays the rotation gate')
            await capture('portrait_gate', (390, 844))
            await page.set_viewport_size({'width': 844, 'height': 390})
            await page.wait_for_timeout(1000)
            check(not await page.locator('#orientation').is_visible(), 'Returning to landscape removes the rotation gate')
            await tap(73, 360)
            await capture('inbound_after_orientation_return')
            check(region_diff('overview_after_pan', 'inbound_after_orientation_return', (0, 60, 570, 320)), 'Fresh touch produces a new 3D display after orientation recovery')

            diagnostic = await page.evaluate('window.warehouseLocal')
            check(diagnostic.get('phase') == 'running' and diagnostic.get('externalRequests') == 0, 'Local engine remains running with no external fetches after all interactions')
            check(not diagnostic.get('errors'), 'Godot loader/printErr diagnostics contain no errors', diagnostic.get('errors'))
            check(not page_errors and not console_errors, 'Browser page and console contain no errors', {'page': page_errors, 'console': console_errors})
            network = [r for r in requests if r['scheme'] in ['http', 'https', 'ws', 'wss']]
            external = [r for r in network if not (args.local_http and urlparse(r['url']).netloc == urlparse(url).netloc and urlparse(r['url']).path in ['/WarehouseStation-iPhone.html', '/favicon.ico'])]
            check(not external, 'Actual browser made zero external HTTP/HTTPS/WebSocket requests; loopback document transport is recorded separately', external)
            if not args.local_http:
                check(not network, 'Actual file:// browser made zero network requests of any kind', network)
            check(digest(args.html) == html_hash, 'Standalone HTML bytes remain unchanged throughout the test')
            status = 'PASS'
            await browser.close()
    except Exception as error:
        if 'ERR_BLOCKED_BY_ADMINISTRATOR' in str(error):
            status = 'BLOCKED_ENVIRONMENT'
            environment_block = 'Managed Chromium refused file:// before executing the HTML. Browser policy was not changed.'
        else:
            raise
    finally:
        if server is not None:
            server.shutdown()
            server.server_close()
        external = [r for r in requests if r['scheme'] in ['http', 'https', 'ws', 'wss'] and not (args.local_http and urlparse(r['url']).netloc == urlparse(url).netloc and urlparse(r['url']).path in ['/WarehouseStation-iPhone.html', '/favicon.ico'])]
        report = {'status': status, 'scope': 'actual_desktop_chromium_webgl2_frame_spanning_touch_pixels_not_internal_state_or_iphone_acceptance', 'transport': 'actual_loopback_http_same_html_bytes_not_a_file_url_verification' if args.local_http else 'actual_file_url_no_server', 'environment_block': environment_block, 'html_sha256': html_hash, 'html_bytes': args.html.stat().st_size, 'browser_version': browser_version, 'checks': checks, 'captures': captures, 'diagnostic': diagnostic, 'page_errors': page_errors, 'console_errors': console_errors, 'requests': requests, 'network_request_count': sum(r['scheme'] in ['http', 'https', 'ws', 'wss'] for r in requests), 'external_network_request_count': len(external), 'loopback_server_stopped': server is not None, 'visual_review': 'REQUIRED', 'iPhone_Scriptable': 'NOT_RUN', 'iPhone_Safari': 'NOT_RUN', 'phone_fps': 'NOT_RUN', 'phone_thermal': 'NOT_RUN', 'public_hosting': 'NOT_PERFORMED'}
        (args.output / 'report.json').write_text(json.dumps(report, indent=2, ensure_ascii=False) + '\n')
        print('IPHONE LOCAL CHROMIUM', status, len(checks), 'checks,', len(captures), 'captures')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--html', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--chromium', default='/usr/bin/chromium')
    parser.add_argument('--local-http', action='store_true', help='Partial fallback: serve only the unchanged HTML on loopback when managed Chromium blocks file URLs. Does not verify file://.')
    asyncio.run(exercise(parser.parse_args()))
