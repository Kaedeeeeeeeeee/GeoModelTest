"""Test the actual WebGL survey bridge in an embed without popup permission."""
import json
from pathlib import Path
from playwright.sync_api import sync_playwright

root = Path(__file__).resolve().parents[2]
source = (root/'Assets/Plugins/WebGL/IndexedDBSync.jslib').read_text()
with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page()
    page.route('http://localhost:55880/**', lambda r: r.fulfill(content_type='text/html', body='<iframe src="http://127.0.0.1:55880/game" sandbox="allow-scripts allow-same-origin allow-top-navigation-by-user-activation"></iframe>'))
    page.route('http://127.0.0.1:55880/**', lambda r: r.fulfill(content_type='text/html', body='<div>Game</div>'))
    page.route('https://survey.example.test/**', lambda r: r.fulfill(content_type='text/html', body='<h1>Survey</h1>'))
    page.goto('http://localhost:55880')
    page.wait_for_load_state('networkidle')
    frame = page.frame(url='http://127.0.0.1:55880/game')
    frame.evaluate('window.LibraryManager={library:{}};window.mergeInto=(a,b)=>Object.assign(a,b);window.UTF8ToString=x=>x;'+source)
    frame.evaluate("LibraryManager.library.GeoModelTest_OpenSurvey('javascript:alert(1)','Answer survey')")
    assert frame.locator('a').count()==0
    # Playwright evaluate grants transient activation; let it expire as asynchronous saves can.
    frame.evaluate("setTimeout(()=>LibraryManager.library.GeoModelTest_OpenSurvey('https://survey.example.test/#ticket='+'a'.repeat(64),'アンケートに答える'),6500)")
    page.wait_for_timeout(7000)
    assert page.url.startswith('http://localhost:55880')
    link = frame.get_by_role('link',name='アンケートに答える')
    assert link.get_attribute('target')=='_top'
    link.click()
    page.wait_for_url('https://survey.example.test/**')
    assert page.locator('h1').inner_text()=='Survey'
    browser.close()
(root/'Logs/remediation/survey/bridge-results.json').write_text(json.dumps(['Reject non-HTTP destinations','Sandboxed automatic navigation retains a visible link','User click navigates without popup permission']))
print('3 actual WebGL survey bridge checks passed')
