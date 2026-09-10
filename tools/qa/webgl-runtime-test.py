"""Review the actual local WebGL player using observed canvas controls; no player test hooks."""
import io
import json
import re
import os
from pathlib import Path
from PIL import Image, ImageChops
from playwright.sync_api import sync_playwright

root=Path(__file__).resolve().parents[2]
shots=root/'Docs/reports/2026-09-10/screenshots'
logs=root/'Logs/remediation'
checks=[]
messages=[]
errors=[]
requests=[]
with sync_playwright() as p:
    browser=p.chromium.launch(headless=True,args=['--use-angle=metal','--use-gl=angle','--enable-gpu'])
    context=browser.new_context(viewport={'width':1366,'height':768},locale='ja-JP',device_scale_factor=1)
    context.route('https://itch.io/**',lambda route:route.fulfill(content_type='text/html',body='<h1 id="destination">Local QA: exit destination reached</h1>'))
    if os.environ.get('QA_SYNC_DEBUG'):
        def instrument(route):
            response=route.fetch()
            body=response.text().replace('FS.staticInit();', "FS.staticInit();window.qaFS=FS;window.qaIDBFS=IDBFS;var originalSync=FS.syncfs;FS.syncfs=function(populate,callback){console.log('QA sync start',populate,performance.now());return originalSync(populate,function(error){console.log('QA sync done',String(error),performance.now());callback(error);});};",1)
            route.fulfill(response=response,body=body)
        context.route('**/RemediationWebGL.framework.js',instrument)
        context.add_init_script("let status=0;Object.defineProperty(window,'geoModelSyncStatus',{get(){return status},set(value){status=value;console.log('QA status',value,performance.now());}})")
    page=context.new_page()
    page.on('console',lambda msg:messages.append(msg.text))
    page.on('pageerror',lambda error:errors.append(str(error)))
    page.on('request',lambda req:requests.append(req.url.split('/')[-1]) if ('/auth/' in req.url or '/functions/v1/' in req.url) else None)
    def capture(name):
        data=page.screenshot(path=str(shots/(name+'.png')))
        return Image.open(io.BytesIO(data)).convert('RGB')
    def ready():
        page.wait_for_function('Boolean(window.unityInstance)',timeout=180000)
        page.wait_for_timeout(2500)
    def click(x,y):page.mouse.click(x,y);page.wait_for_timeout(800)
    def save_return():
        click(990,676)
        capture('webgl-save-return-confirm')
        click(850,516)
        page.wait_for_function('window.geoModelSyncStatus === 2',timeout=20000)
        page.wait_for_timeout(2000)
        checks.append('Save completed before returning to title')
    try:
        page.goto('http://127.0.0.1:55882/',wait_until='networkidle',timeout=180000)
        ready()
        assert page.locator('canvas').count()==1
        renderer=page.evaluate("() => {const gl=document.querySelector('canvas').getContext('webgl2');const ext=gl.getExtension('WEBGL_debug_renderer_info');return ext?gl.getParameter(ext.UNMASKED_RENDERER_WEBGL):gl.getParameter(gl.RENDERER);}")
        fresh=capture('webgl-title')
        page.keyboard.press('Escape');page.wait_for_timeout(800);capture('webgl-settings-esc')
        assert page.evaluate('document.pointerLockElement === null')
        page.keyboard.press('Escape');page.wait_for_timeout(800)
        checks.append('Escape opens and closes title settings with a free cursor')
        click(300,423) # New game, observed title layout.
        page.wait_for_timeout(9000)
        capture('webgl-game-start')
        page.keyboard.press('Escape');page.wait_for_timeout(1200)
        before=capture('webgl-paused-game')
        assert page.evaluate('document.pointerLockElement === null')
        page.keyboard.down('w');page.mouse.move(1250,300);page.wait_for_timeout(1200)
        after=capture('webgl-paused-input-check');page.keyboard.up('w')
        # Left edge lies outside all settings controls and includes the visible game world.
        assert ImageChops.difference(before.crop((0,150,180,760)),after.crop((0,150,180,760))).getbbox() is None, 'World moved while settings were open'
        checks.append('Movement and look input leave the paused game world unchanged')
        save_return()
        page.reload(wait_until='networkidle',timeout=180000);ready()
        restored=capture('webgl-saved-after-refresh')
        assert restored.getpixel((150,340))[1] > fresh.getpixel((150,340))[1] + 30, 'Continue did not become available after reload'
        checks.append('IndexedDB reload restores an enabled Continue button')
        click(300,345);page.wait_for_timeout(9000);capture('webgl-continued-scene')
        page.keyboard.press('Escape');page.wait_for_timeout(800)
        save_return()
        click(425,503)
        page.wait_for_url('https://itch.io/',timeout=20000)
        assert page.locator('#destination').count()==1
        checks.append('Quit navigates to the destination after saving (destination intercepted locally)')
        assert not requests, requests
        checks.append('Ordinary play makes zero research authentication/upload requests')
        assert not errors, errors
        runtime_failures=[m for m in messages if re.search(r'(?:ArgumentNull|NullReference|InvalidOperation)Exception|CharacterController.Move called on inactive|创建图鉴UI失败|SampleIconGenerator 实例不存在',m)]
        assert not runtime_failures, runtime_failures
        checks.append('No shader/null-reference/controller-startup failures in the reviewed path')
        result={'passed':True,'checks':checks,'pageErrors':errors,'researchRequests':requests,'browser':browser.version,'renderer':renderer}
    except Exception as error:
        if os.environ.get('QA_SYNC_DEBUG'):
            print(page.evaluate('({status:window.geoModelSyncStatus,again:window.geoModelSyncAgain,mounts:window.qaFS?.getMounts(window.qaFS.root.mount).map(m=>({path:m.mountpoint,state:m.idbPersistState})),requests:window.qaFS?.syncFSRequests})'),flush=True)
        result={'passed':False,'checks':checks,'error':str(error),'pageErrors':errors,'researchRequests':requests}
        capture('webgl-runtime-failure')
        raise
    finally:
        (logs/'webgl-runtime-results.json').write_text(json.dumps(result,ensure_ascii=False,indent=2))
        (logs/'webgl-runtime-console.log').write_text('\n'.join(messages))
        browser.close()
print(f'{len(checks)} actual WebGL runtime checks passed')
