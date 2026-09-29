"""Guardian consent form in a real browser, with the Unity build loading behind it.

Serve the checked-in template over the current build first:
    python3 tools/qa/loading-server.py --preview --port 55888
"""
import argparse
import json
from playwright.sync_api import sync_playwright, expect

parser = argparse.ArgumentParser()
parser.add_argument('--url', default='http://127.0.0.1:55888/ok/index.html')
args = parser.parse_args()
results = []


def state(page):
    return page.evaluate("""() => ({
        pending: GeoModelGuardianConsent.isPending(),
        stored: sessionStorage.getItem('geomodel-guardian-consent'),
        focused: document.activeElement && (document.activeElement.name || document.activeElement.id),
        disabled: document.querySelector('#guardian-consent-form button[type=submit]').disabled,
        hint: !document.getElementById('guardian-consent-hint').hidden,
        idError: document.querySelector('[data-error-for=respondentId]').hidden ? '' :
            document.querySelector('[data-error-for=respondentId]').textContent
    })""")


with sync_playwright() as p:
    browser = p.chromium.launch(headless=True, args=['--use-angle=metal', '--use-gl=angle', '--enable-gpu'])
    page = browser.new_context(viewport={'width': 1024, 'height': 768}).new_page()
    errors = []
    page.on('pageerror', lambda error: errors.append(str(error)))
    page.goto(args.url)
    form = page.locator('#guardian-consent-form')
    expect(form).to_be_visible()
    # Unity captures keys on the whole window once it runs; type only after it is ready.
    page.wait_for_function('() => !!window.unityInstance', timeout=180000)
    s = state(page)
    assert s['pending'] and s['disabled'] and s['hint'], s
    results.append('Before any answer the button is disabled and the hint explains why')

    form.locator('input[name=guardianAgreed][value=yes]').check()
    form.locator('input[name=guardianConfirmed]').check()
    name = form.locator('input[name=guardianName]')
    name.click()
    page.keyboard.type('yamada hanako')
    page.keyboard.press('Enter')
    s = state(page)
    assert name.input_value() == 'yamada hanako', 'typing must reach the form while Unity runs'
    assert s['pending'] and s['stored'] is None and s['focused'] == 'respondentId' and s['disabled'], s
    results.append('Enter in the name field moves to the ID field; nothing is sent without an ID')

    respondent = form.locator('input[name=respondentId]')
    page.keyboard.type('p123')
    page.keyboard.press('Enter')
    s = state(page)
    assert s['focused'] != 'respondentId' and 'p' in s['idError'] and s['disabled'] and s['pending'], s
    results.append('Enter in the ID field closes the keyboard and explains a mistyped ID at once')

    respondent.fill('Ｐ１２３４５６７')
    s = state(page)
    assert not s['disabled'] and not s['hint'] and s['idError'] == '', s
    respondent.press('Enter')
    s = state(page)
    assert respondent.input_value() == 'p1234567' and s['pending'] and s['stored'] is None, s
    results.append('A full-width ID enables the button and shows as p1234567; Enter still does not submit')

    # An Enter that confirms an IME conversion belongs to the IME, not to field navigation.
    name.focus()
    page.evaluate("""() => document.querySelector('input[name=guardianName]').dispatchEvent(
        new KeyboardEvent('keydown', {key: 'Enter', keyCode: 229, isComposing: true, bubbles: true}))""")
    assert state(page)['focused'] == 'guardianName'
    results.append('An IME-confirming Enter leaves focus where it is')

    name.fill('')
    assert state(page)['disabled']
    name.fill('山田　花子')
    form.locator('button[type=submit]').click()
    s = state(page)
    stored = json.loads(s['stored'])
    assert not s['pending'] and stored['respondentId'] == 'p1234567' and stored['guardianName'] == '山田　花子', s
    results.append('Only the enabled button submits; the stored answer has the normalized ID')

    declined = browser.new_context(viewport={'width': 390, 'height': 844}, is_mobile=True, has_touch=True).new_page()
    declined.on('pageerror', lambda error: errors.append(str(error)))
    declined.goto(args.url)
    declined.locator('input[name=guardianAgreed][value=no]').check()
    assert not state(declined)['disabled']
    declined.locator('#guardian-consent-form button[type=submit]').click()
    expect(declined.locator('#guardian-consent-declined')).to_be_visible()
    assert state(declined)['pending'] and state(declined)['stored'] is None
    results.append('Declining needs no other answers and sends nothing')

    assert not errors, errors
    browser.close()

for line in results:
    print('PASS', line)
print(f'{len(results)} guardian consent browser checks passed')
