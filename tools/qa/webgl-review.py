"""Local headless WebGL review session. Commands are explicit and stored in ignored Logs."""
import json
import time
from pathlib import Path
from playwright.sync_api import sync_playwright

root=Path(__file__).resolve().parents[2]
directory=root/'Logs/remediation'
command_path=directory/'browser-command.json'
result_path=directory/'browser-result.json'
shots=root/'Docs/reports/2026-09-10/screenshots'
messages=[]
errors=[]
research_requests=[]
with sync_playwright() as p:
    browser=p.chromium.launch(headless=True,args=['--enable-unsafe-swiftshader','--use-angle=swiftshader'])
    context=browser.new_context(viewport={'width':1366,'height':768},locale='ja-JP',device_scale_factor=1)
    context.route('https://itch.io/**',lambda route:route.fulfill(content_type='text/html',body='<h1>Local QA: exit destination reached</h1>'))
    page=context.new_page()
    page.on('console',lambda msg:messages.append(msg.text))
    page.on('pageerror',lambda error:errors.append(str(error)))
    page.on('request',lambda req:research_requests.append(req.url.split('/')[-1]) if ('/auth/' in req.url or '/functions/v1/' in req.url) else None)
    def ready():
        page.wait_for_function('Boolean(window.unityInstance)',timeout=180000)
        page.wait_for_timeout(2000)
    page.goto('http://127.0.0.1:55882',wait_until='networkidle',timeout=180000)
    ready()
    page.screenshot(path=str(shots/'webgl-title.png'))
    (directory/'browser-ready.json').write_text(json.dumps({'ready':True,'url':page.url,'errors':errors,'researchRequests':research_requests}))
    print('WebGL loaded; browser review ready',flush=True)
    while True:
        if not command_path.exists():
            page.wait_for_timeout(100)
            continue
        data=json.loads(command_path.read_text());command_path.unlink()
        try:
            op=data['op'];value=data.get('value');result=None
            if op=='capture': page.screenshot(path=str(shots/(value+'.png')))
            elif op=='click': page.mouse.click(data['x'],data['y']);page.wait_for_timeout(800)
            elif op=='key': page.keyboard.press(value);page.wait_for_timeout(500)
            elif op=='move': page.mouse.move(data['x'],data['y']);page.wait_for_timeout(300)
            elif op=='reload':page.reload(wait_until='networkidle');ready()
            elif op=='size': page.set_viewport_size({'width':data['width'],'height':data['height']});page.wait_for_timeout(800)
            elif op=='evaluate': result=page.evaluate(value)
            elif op=='state': result={'url':page.url,'title':page.title(),'canvas':page.locator('canvas').bounding_box() if page.locator('canvas').count() else None,'errors':errors,'researchRequests':research_requests,'consoleTail':messages[-12:]}
            elif op=='quit':break
            (directory/'webgl-console.log').write_text('\n'.join(messages))
            result_path.write_text(json.dumps({'ok':True,'op':op,'result':result},ensure_ascii=False))
        except Exception as error:
            result_path.write_text(json.dumps({'ok':False,'error':str(error)}))
    (directory/'webgl-console.log').write_text('\n'.join(messages))
    browser.close()
