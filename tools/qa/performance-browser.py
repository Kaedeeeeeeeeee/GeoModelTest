"""Verify the shipping WebGL build's real canvas, quality controls and touch mapping.

Run loading-server.py --port 55889 first. Uses isolated browser contexts and blocks
research backend traffic. Coordinates are canvas fractions observed in the game UI.
"""
import argparse
import io
import json
from pathlib import Path
from PIL import Image, ImageChops
from playwright.sync_api import sync_playwright

parser = argparse.ArgumentParser()
parser.add_argument('--url', default='http://127.0.0.1:55889/normal/')
parser.add_argument('--output', default='Logs/performance-2026-09-11/browser')
args = parser.parse_args()
output = Path(args.output)
output.mkdir(parents=True, exist_ok=True)
results = []

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True, args=['--use-angle=metal', '--use-gl=angle', '--enable-gpu'])
    try:
        for tablet in (False, True):
            options = dict(viewport={'width': 1024 if tablet else 1200, 'height': 768 if tablet else 800},
                device_scale_factor=2, has_touch=tablet, is_mobile=tablet, locale='ja-JP')
            if tablet:
                options['user_agent'] = 'Mozilla/5.0 (iPad; CPU OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1'
            context = browser.new_context(**options)
            context.route('**/*.supabase.co/**', lambda route: route.abort())
            page = context.new_page()
            errors, requests = [], []
            page.on('pageerror', lambda error: errors.append(str(error)))
            page.on('request', lambda request: requests.append(request.url) if '/auth/' in request.url or '/functions/v1/' in request.url else None)
            name = 'tablet' if tablet else 'retina-desktop'
            checks = []

            def ready():
                page.wait_for_function('!!window.unityInstance', timeout=120000)
                page.wait_for_timeout(1800)

            def state():
                return page.evaluate('''() => {
                    const c=document.querySelector('canvas'), r=c.getBoundingClientRect(), gl=c.getContext('webgl2');
                    return {version:config.productVersion, quality:GeoModelPerformance.quality,
                        manual:GeoModelPerformance.manual, canvas:[c.width,c.height],
                        buffer:[gl.drawingBufferWidth,gl.drawingBufferHeight], css:[r.width,r.height],
                        dpr:devicePixelRatio};
                }''')

            def check_quality(level, manual, size):
                page.wait_for_function('([q,m,w,h])=>GeoModelPerformance.quality===q && GeoModelPerformance.manual===m && canvas.width===w && canvas.height===h', arg=[level, manual, *size])
                page.wait_for_timeout(300)
                actual = state()
                assert actual['buffer'] == size, actual
                assert actual['dpr'] == 2, actual
                checks.append(actual)

            def click_canvas(x, y):
                r = page.locator('canvas').bounding_box()
                point = (r['x'] + x*r['width'], r['y'] + y*r['height'])
                if tablet: page.touchscreen.tap(*point)
                else: page.mouse.click(*point)
                page.wait_for_timeout(650)

            try:
                page.goto(args.url, wait_until='domcontentloaded')
                ready()
                check_quality(1, False, [1280, 720])
                if tablet: click_canvas(0.142, 0.653)
                else: page.keyboard.press('Escape'); page.wait_for_timeout(500)
                page.screenshot(path=str(output/(name+'-settings-low.png')))
                click_canvas(0.435, 0.799)  # Left end of quality slider: Very Low.
                check_quality(0, True, [960, 540])
                page.screenshot(path=str(output/(name+'-settings-very-low.png')))
                click_canvas(0.666, 0.799)  # High, explicitly selected by the player.
                check_quality(3, True, [1280, 720])
                page.wait_for_timeout(1500)
                page.reload(wait_until='domcontentloaded')
                ready()
                check_quality(3, True, [1280, 720])
                if tablet: click_canvas(0.142, 0.653)
                else: page.keyboard.press('Escape'); page.wait_for_timeout(500)
                click_canvas(0.286, 0.880)  # Restore automatic quality.
                check_quality(1, False, [1280, 720])
                click_canvas(0.51, 0.880)  # Close settings.

                if tablet:
                    page.set_viewport_size({'width': 768, 'height': 1024})
                    page.wait_for_timeout(600)
                    assert page.locator('#orientation-overlay').evaluate("e=>e.classList.contains('is-visible')")
                    assert state()['buffer'] == [1280, 720]
                    page.screenshot(path=str(output/'tablet-portrait.png'))
                    page.set_viewport_size({'width': 1024, 'height': 768})
                    page.wait_for_timeout(600)
                    assert not page.locator('#orientation-overlay').evaluate("e=>e.classList.contains('is-visible')")

                click_canvas(0.225, 0.55)  # Start an ordinary game, outside research mode.
                page.wait_for_timeout(6500)
                page.screenshot(path=str(output/(name+'-gameplay.png')))
                if not tablet:
                    page.keyboard.press('Escape')
                    page.wait_for_timeout(800)
                    before = Image.open(io.BytesIO(page.screenshot())).convert('RGB')
                    page.keyboard.down('w')
                    page.mouse.move(1180, 200)
                    page.wait_for_timeout(1200)
                    page.keyboard.up('w')
                    after = Image.open(io.BytesIO(page.screenshot())).convert('RGB')
                    crop = (0, 220, 230, 1250)
                    assert ImageChops.difference(before.crop(crop), after.crop(crop)).getbbox() is None, 'Paused game moved'
                    page.screenshot(path=str(output/'retina-desktop-paused.png'))
                assert not errors, errors
                assert not requests, requests
                results.append({'device':name, 'passed':True, 'checks':checks, 'pageErrors':errors, 'researchRequests':requests})
            except Exception as error:
                page.screenshot(path=str(output/(name+'-failure.png')))
                results.append({'device':name, 'passed':False, 'error':str(error), 'checks':checks, 'pageErrors':errors})
                raise
            finally:
                (output/'results.json').write_text(json.dumps(results, ensure_ascii=False, indent=2))
                context.close()
    finally:
        browser.close()
print(json.dumps(results, ensure_ascii=False, indent=2))
