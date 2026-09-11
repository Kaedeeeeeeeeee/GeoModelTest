"""Browser integration checks against loading-server.py and the actual Unity player."""
import argparse
import json
from pathlib import Path
from playwright.sync_api import sync_playwright

parser = argparse.ArgumentParser()
parser.add_argument('--url', default='http://127.0.0.1:55888')
parser.add_argument('--output', default='Logs/loading-improvements/browser')
parser.add_argument('--long', action='store_true', help='Also verify the real 90-second inactivity timeout')
parser.add_argument('--case', help='Run only one scenario')
args = parser.parse_args()
out = Path(args.output)
out.mkdir(parents=True, exist_ok=True)
results = []

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True, args=['--use-angle=metal', '--use-gl=angle', '--enable-gpu'])
    scenarios = ['normal', 'corrupt-cache', 'fail-ui', 'fail-loader', 'fail-data', 'invalid-data', 'fail-twice', 'disconnect', 'stall']
    if args.long: scenarios.append('timeout')
    if args.case: scenarios = [args.case]
    try:
        for scenario in scenarios:
            mobile = scenario in ('fail-data', 'stall')
            context = browser.new_context(viewport={'width':844 if mobile else 1366, 'height':390 if mobile else 768},
                is_mobile=mobile, has_touch=mobile, locale='ja-JP')
            context.route('**/*.supabase.co/**', lambda route: route.abort())
            context.add_init_script('window.__originalFetch = window.fetch;')
            page = context.new_page()
            errors, dialogs = [], []
            page.on('pageerror', lambda error: errors.append(str(error)))
            def on_dialog(dialog): dialogs.append(dialog.message); dialog.dismiss()
            page.on('dialog', on_dialog)
            page.goto(f'{args.url}/{scenario}/', wait_until='domcontentloaded')

            def state():
                return page.evaluate('''() => ({state:document.getElementById('unity-loading-bar').dataset.state,
                    status:document.getElementById('loading-status').textContent,
                    progress:document.getElementById('loading-progress-text').textContent,
                    ready:!!window.unityInstance, fetchRestored:window.fetch === window.__originalFetch})''')

            def ready():
                page.wait_for_function('!!window.unityInstance', timeout=60000)
                page.wait_for_load_state('networkidle')
                page.wait_for_timeout(1200)
                assert page.locator('#unity-loading-bar').is_hidden()
                assert page.evaluate('window.fetch === window.__originalFetch')
                assert not dialogs, dialogs

            if scenario == 'normal':
                ready()
                page.screenshot(path=str(out/'title.png'))
                page.reload(wait_until='domcontentloaded')
                ready()
                results.append({'case': 'normal-and-cached-reload', **state(), 'errors':errors})
            elif scenario == 'stall':
                page.wait_for_function("document.getElementById('loading-progress-text').textContent !== '0%'")
                page.screenshot(path=str(out/'downloading-mobile.png'))
                page.wait_for_function("document.getElementById('loading-status').textContent.includes('届くのを待って')", timeout=25000)
                during = state()
                assert during['state'] != 'failed'
                page.screenshot(path=str(out/'waiting-mobile.png'))
                ready()
                results.append({'case':scenario, 'during':during, 'afterResume':state(), 'errors':errors})
            else:
                if scenario == 'corrupt-cache':
                    ready()
                    # Simulate damaged cached bytes with unchanged HTTP validators.
                    page.evaluate('''async () => {
                        const cache=await caches.open('UnityCache_'+config.companyName+'_'+config.productName);
                        const url=new URL(config.dataUrl,document.baseURI).href;
                        const original=await cache.match(url);
                        if(!original) throw new Error('Expected real Unity data cache');
                        await cache.put(url,new Response('<html>Damaged cached data</html>',{headers:original.headers}));
                    }''')
                    page.reload(wait_until='domcontentloaded')
                page.locator('#loading-retry').wait_for(state='visible', timeout=100000 if scenario=='timeout' else 20000)
                if scenario == 'timeout':
                    page.wait_for_function("document.getElementById('unity-loading-bar').dataset.state === 'failed'", timeout=100000)
                if scenario != 'fail-ui':
                    assert state()['state'] == 'failed', state()
                before = state()
                assert page.locator('#unity-warning').is_hidden(), 'Technical banner obscures Japanese recovery UI'
                page.screenshot(path=str(out/(scenario+'.png')))
                # A retry must preserve identity and saves in both browser stores.
                page.evaluate('''async () => {
                    localStorage.setItem('qa-invitation-identity', 'local-synthetic-identity');
                    await new Promise((resolve,reject) => {
                        const r=indexedDB.open('qa-saved-progress',1);
                        r.onupgradeneeded=()=>r.result.createObjectStore('saves');
                        r.onerror=()=>reject(r.error);
                        r.onsuccess=()=>{const db=r.result,t=db.transaction('saves','readwrite');
                            t.objectStore('saves').put('synthetic-progress','current');
                            t.oncomplete=()=>{db.close();resolve();};};
                    });
                }''')
                for attempt in range(2 if scenario=='fail-twice' else 1):
                    with page.expect_navigation(wait_until='domcontentloaded'):
                        if mobile: page.locator('#loading-retry').tap()
                        else: page.get_by_role('button', name='もう一度読み込む', exact=True).click()
                    if scenario == 'fail-twice' and attempt == 0:
                        page.wait_for_function("document.getElementById('unity-loading-bar').dataset.state === 'failed'")
                ready()
                if scenario == 'corrupt-cache':
                    page.reload(wait_until='domcontentloaded')
                    ready()
                preserved = page.evaluate('''async () => {
                    const value=await new Promise(resolve=>{const r=indexedDB.open('qa-saved-progress',1);
                        r.onsuccess=()=>{const db=r.result,q=db.transaction('saves').objectStore('saves').get('current');
                            q.onsuccess=()=>{db.close();resolve(q.result);};};});
                    return value==='synthetic-progress' && localStorage.getItem('qa-invitation-identity')==='local-synthetic-identity';
                }''')
                assert preserved
                results.append({'case':scenario, 'failure':before, 'afterRetry':state(), 'savedStatePreserved':preserved, 'errors':errors})
            print(json.dumps(results[-1], ensure_ascii=False), flush=True)
            (out/'results.json').write_text(json.dumps(results, indent=2, ensure_ascii=False))
            context.close()
    finally:
        browser.close()
