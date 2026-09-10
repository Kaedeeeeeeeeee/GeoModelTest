"""Review the published itch.io embed in a fresh, headless Chromium profile.

Explicit commands and results stay in ignored Logs; no game code is injected.
"""
import argparse
import json
from pathlib import Path
from playwright.sync_api import sync_playwright

parser = argparse.ArgumentParser()
parser.add_argument('--url', required=True)
parser.add_argument('--expect-version', required=True)
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
directory = root/'Logs/release-2026-09-11'
directory.mkdir(parents=True, exist_ok=True)
pending, result = directory/'command.json', directory/'result.json'
errors, messages, research = [], [], []
with sync_playwright() as p:
    context = p.chromium.launch_persistent_context(str(directory/'browser-profile'), headless=True,
        args=['--use-angle=metal','--use-gl=angle','--enable-gpu'],
        viewport={'width':1440,'height':1000}, locale='ja-JP', device_scale_factor=1)
    page = context.new_page()
    page.on('pageerror', lambda error: errors.append(str(error)))
    page.on('console', lambda message: messages.append(message.text))
    page.on('request', lambda request: research.append(request.url.split('/')[-1]) if '/auth/v1/' in request.url or '/functions/v1/' in request.url else None)

    def load():
        page.goto(args.url, wait_until='networkidle', timeout=60000)
        run = page.get_by_role('button', name='Run game')
        if run.count():
            run.click()
        iframe = page.wait_for_selector('iframe#game_drop', timeout=30000)
        frame = iframe.content_frame()
        frame.wait_for_function('Boolean(window.unityInstance)', timeout=180000)
        frame.wait_for_timeout(2500)
        version = frame.evaluate('window.config.productVersion')
        assert version == args.expect_version, (version, args.expect_version)
        return frame

    try:
        frame = load()
        frame.locator('canvas').screenshot(path=str(directory/'01-online-title.png'))
        page.screenshot(path=str(directory/'00-itch-page.png'), full_page=True)
        (directory/'ready.json').write_text(json.dumps({'version':args.expect_version,'frame':frame.url,'page':page.url,'errors':errors}, indent=2))
        print('Published game loaded with expected release version', flush=True)
        while True:
            if not pending.exists():
                page.wait_for_timeout(100)
                continue
            command = json.loads(pending.read_text())
            pending.unlink()
            try:
                op, value = command['op'], command.get('value')
                output = None
                if op == 'capture':
                    frame.locator('canvas').screenshot(path=str(directory/(value+'.png')))
                elif op == 'click':
                    canvas = frame.locator('canvas')
                    box = canvas.bounding_box()
                    canvas.click(position={'x':command['x']/1366*box['width'],'y':command['y']/768*box['height']})
                    page.wait_for_timeout(1000)
                elif op == 'key':
                    page.keyboard.press(value)
                    page.wait_for_timeout(800)
                elif op == 'hold':
                    page.keyboard.down(value)
                    page.wait_for_timeout(command.get('duration',1000))
                    page.keyboard.up(value)
                elif op == 'reload':
                    frame = load()
                elif op == 'evaluate':
                    output = frame.evaluate(value)
                elif op == 'state':
                    output = {'url':page.url,'frame':frame.url,'frame_detached':frame.is_detached(),'errors':errors,'research_requests':research,'console_tail':messages[-8:]}
                elif op == 'quit':
                    result.write_text(json.dumps({'ok':True,'op':op}))
                    break
                else:
                    raise ValueError('Unknown review command')
                result.write_text(json.dumps({'ok':True,'op':op,'result':output}, ensure_ascii=False))
            except Exception as error:
                result.write_text(json.dumps({'ok':False,'error':str(error)}))
    finally:
        (directory/'browser-console.log').write_text('\n'.join(messages))
        (directory/'browser-errors.json').write_text(json.dumps(errors, indent=2))
        context.close()
