"""Exercise the actual WebGL bridge in Chromium, including sandboxed iframe navigation."""
import json
from pathlib import Path
from playwright.sync_api import sync_playwright

root = Path(__file__).resolve().parents[2]
source = (root / 'Assets/Plugins/WebGL/IndexedDBSync.jslib').read_text()
bootstrap = 'window.LibraryManager={library:{}}; window.mergeInto=(a,b)=>Object.assign(a,b);window.UTF8ToString=x=>x;'
results = []
with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page(viewport={'width':1366,'height':768})
    page.route('http://localhost:55880/**', lambda r: r.fulfill(content_type='text/html', body='<iframe title="game" src="http://127.0.0.1:55880/game" sandbox="allow-scripts allow-same-origin allow-top-navigation-by-user-activation" style="width:100%;height:90vh;border:0"></iframe>'))
    page.route('http://127.0.0.1:55880/**', lambda r: r.fulfill(content_type='text/html', body='<div id="game">Local bridge test</div>'))
    page.route('https://itch.io/**', lambda r: r.fulfill(content_type='text/html', body='<h1 id="destination">Test destination</h1>'))
    page.goto('http://localhost:55880/host')
    page.wait_for_load_state('networkidle')
    frame = page.frame(url='http://127.0.0.1:55880/game')
    assert frame.locator('#game').inner_text() == 'Local bridge test'
    frame.evaluate(bootstrap + source)
    status = frame.evaluate('''() => {
      window.callbacks=[];window.FS={syncfs:(populate,callback)=>callbacks.push(callback)};
      let api=LibraryManager.library;
      api.GeoModelTest_SyncFsToIDB();api.GeoModelTest_SyncFsToIDB();
      if(callbacks.length!==1||api.GeoModelTest_SyncStatus()!==1)throw Error('Concurrent flush');
      callbacks.shift()(null);
      if(callbacks.length!==1||api.GeoModelTest_SyncStatus()!==1)throw Error('Missing coalesced flush');
      callbacks.shift()(null);
      if(api.GeoModelTest_SyncStatus()!==2)throw Error('Missing success');
      api.GeoModelTest_SyncFsToIDB();callbacks.shift()(Error('simulated storage failure'));
      if(api.GeoModelTest_SyncStatus()!==-1)throw Error('Missing failure');
      api.GeoModelTest_SyncFsToIDB();callbacks.shift()(null);
      return api.GeoModelTest_SyncStatus();
    }''')
    assert status == 2
    results.extend(['Overlapping saves are serialized', 'Completion is observable', 'Failure is observable and retry succeeds'])
    frame.evaluate("setTimeout(() => LibraryManager.library.GeoModelTest_ReturnToGamePage('', 'ゲームページへ戻る'), 6500)")
    page.wait_for_timeout(7000)
    assert page.url == 'http://localhost:55880/host', 'Automatic navigation should be blocked by iframe sandbox'
    link = frame.get_by_role('link', name='ゲームページへ戻る')
    assert link.get_attribute('href') == 'https://itch.io'
    assert link.get_attribute('target') == '_top'
    page.screenshot(path=str(root/'Docs/reports/2026-09-10/screenshots/01-webgl-exit-fallback.png'))
    results.append('Sandboxed iframe retains a visible user-click fallback')
    link.click()
    page.wait_for_url('https://itch.io/')
    assert page.locator('#destination').inner_text() == 'Test destination'
    results.append('A real fallback click navigates the top-level page')
    browser.close()
(root/'Logs/remediation/webgl-bridge-results.json').write_text(json.dumps(results, indent=2))
print(f'{len(results)} WebGL bridge browser checks passed')
