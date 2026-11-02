const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

const rootPath = path.resolve(__dirname, '../..');
const source = fs.readFileSync(path.join(rootPath,
  'Assets/WebGLTemplates/FixedAspect/TemplateData/guardian-consent.js'), 'utf8');
const STORAGE_KEY = 'geomodel-guardian-consent';
const VERSION = 'guardian-ja-2026-09-29b';

function fixture(options = {}) {
  const storage = new Map();
  if (options.stored !== undefined) {
    storage.set(STORAGE_KEY, typeof options.stored === 'string' ? options.stored : JSON.stringify(options.stored));
  }
  const classes = new Set();
  const windowListeners = [];
  const window = {
    sessionStorage: options.storageThrows ? {
      getItem() { throw new Error('blocked'); }, setItem() { throw new Error('blocked'); }, removeItem() {}
    } : {
      getItem: key => (storage.has(key) ? storage.get(key) : null),
      setItem: (key, value) => storage.set(key, String(value)),
      removeItem: key => storage.delete(key)
    },
    location: { search: '' },
    fetch: options.fetch,
    addEventListener(type, callback, capture) { windowListeners.push({ type, callback, capture }); }
  };
  const document = {
    documentElement: { classList: { add: name => classes.add(name), remove: name => classes.delete(name) } },
    readyState: 'complete',
    getElementById: () => null,
    addEventListener() {}
  };
  const context = { window, document, URLSearchParams, setTimeout, clearTimeout, AbortController };
  vm.createContext(context);
  vm.runInContext(source, context);
  return { api: window.GeoModelGuardianConsent, classes, storage, windowListeners };
}

function answer(overrides = {}) {
  return Object.assign({ consentVersion: VERSION, guardianAgreed: true, guardianConfirmed: true,
    guardianName: '山田　花子', respondentId: '1234567', guardianConsentedAt: '2026-09-29T01:02:03.456Z' }, overrides);
}

function keyEvent() {
  const event = { stopped: false, stopImmediatePropagation() { this.stopped = true; } };
  return event;
}

test('a first visit shows the guardian form before anything else', () => {
  const { api, classes } = fixture();
  assert.equal(api.isPending(), true);
  assert.ok(classes.has('guardian-consent-pending'));
  assert.equal(api.payloadJson(), '');
});

test('a reload in the same tab keeps the guardian answer but asks the student again', () => {
  const { api, classes } = fixture({ stored: answer() });
  assert.equal(api.isGuardianPending(), false);
  assert.equal(api.isPending(), true, 'the student confirms on every page load');
  assert.ok(classes.has('guardian-consent-pending'));
  assert.equal(api.payloadJson(), '', 'nothing reaches the game before the student confirms');
});

test('the student must confirm all three statements', () => {
  const { api } = fixture();
  assert.equal(api.studentReady([true, true, true]), true);
  for (const checks of [[], [true, true], [true, false, true], [true, true, 'true']]) {
    assert.equal(api.studentReady(checks), false, JSON.stringify(checks));
  }
});

test('the student sheet shows the approved wording word for word', () => {
  const html = fs.readFileSync(path.join(rootPath, 'Assets/WebGLTemplates/FixedAspect/index.html'), 'utf8');
  const texts = Object.fromEntries(JSON.parse(fs.readFileSync(path.join(rootPath,
    'Assets/Resources/Localization/Data/ja-JP.json'), 'utf8')).texts.map(row => [row.key, row.value]));
  const sheet = html.slice(html.indexOf('id="student-consent-view"'), html.indexOf('id="student-consent-declined"'));
  const plain = sheet.replace(/<br>/g, '\n').replace(/<\/p>/g, '\n\n').replace(/<[^>]+>/g, '');
  assert.ok(plain.includes(texts['ui.consent.title']));
  for (const block of texts['ui.consent.body'].split('\n\n')) assert.ok(plain.includes(block), block);
  for (const n of [1, 2, 3]) assert.ok(plain.includes(texts['ui.consent.agreement.' + n]), n);
  assert.ok(plain.includes(texts['ui.consent.continue']));
});

test('an answer to older wording, a declined answer or corrupt storage asks again', () => {
  assert.equal(fixture({ stored: answer({ consentVersion: 'guardian-ja-old' }) }).api.isPending(), true);
  assert.equal(fixture({ stored: answer({ guardianAgreed: false }) }).api.isPending(), true);
  assert.equal(fixture({ stored: '{broken' }).api.isPending(), true);
  assert.equal(fixture({ storageThrows: true }).api.isPending(), true);
});

test('choosing is required, and declining needs no other fields', () => {
  const { api } = fixture();
  const now = new Date('2026-09-29T10:00:00Z');
  assert.ok(api.evaluate({}, now).errors.choice);
  const declined = api.evaluate({ choice: 'no' }, now);
  assert.deepEqual(Object.keys(declined.errors), []);
  assert.equal(declined.declined, true);
  assert.equal(declined.payload, null);
});

test('agreeing requires the guardian confirmation, name and respondent ID', () => {
  const { api } = fixture();
  const result = api.evaluate({ choice: 'yes', guardianConfirmed: false, guardianName: '　 ', respondentId: '' }, new Date());
  assert.deepEqual(Object.keys(result.errors).sort(), ['guardianConfirmed', 'guardianName', 'respondentId']);
  assert.equal(result.payload, null);
  // Testee IDs are digits only, 1 to 10 of them (the old "p" + 7 digit form is gone).
  for (const typed of ['テスト01', 'p1234567', 'P1234567', '12345678901', '１２３４５６７８９０１', '12.34', 'T-0001', '-']) {
    assert.ok(api.evaluate({ choice: 'yes', guardianConfirmed: true, guardianName: '山田', respondentId: typed }, new Date()).errors.respondentId, typed);
  }
  for (const typed of ['1', '0', '0012345', '1234567890']) {
    assert.deepEqual(Object.keys(api.evaluate({ choice: 'yes', guardianConfirmed: true, guardianName: '山田', respondentId: typed }, new Date()).errors), [], typed);
  }
  assert.ok(api.evaluate({ choice: 'yes', guardianConfirmed: true, guardianName: '山田\n花子', respondentId: 'A1' }, new Date()).errors.guardianName);
});

test('Japanese keyboard input is normalized before it is stored', () => {
  const { api } = fixture();
  const now = new Date('2026-09-29T10:00:00Z');
  const result = api.evaluate({ choice: 'yes', guardianConfirmed: true,
    guardianName: '　山田　花子　', respondentId: ' １２３ー４５６７ ' }, now);
  assert.deepEqual(Object.keys(result.errors), []);
  assert.deepEqual(JSON.parse(JSON.stringify(result.payload)), answer({ guardianConsentedAt: now.toISOString() }));
  // Width, spaces and dashes are fixed; leading zeros stay, and nothing is added.
  for (const [typed, stored] of [['１２３４５６７', '1234567'], ['123-4567', '1234567'], ['12 34', '1234'], ['００１２', '0012'], ['１', '1'], ['１２３４５６７８９０', '1234567890']]) {
    assert.equal(api.normalizeRespondentId(typed), stored, typed);
  }
});

test('keys typed into the form never reach Unity while the form is open', () => {
  const pending = fixture();
  const shields = pending.windowListeners.filter(entry => ['keydown', 'keypress', 'keyup'].includes(entry.type));
  assert.equal(shields.length, 3);
  assert.ok(shields.every(entry => entry.capture === true), 'Capture listeners run before Unity registers its own.');
  const event = keyEvent();
  shields[0].callback(event);
  assert.equal(event.stopped, true);

  const studentStep = fixture({ stored: answer() });
  const during = keyEvent();
  studentStep.windowListeners.find(entry => entry.type === 'keydown').callback(during);
  assert.equal(during.stopped, true, 'The student sheet is shielded too; the browser check covers keys after it.');
});

test('a rejected answer is cleared so the next load asks the guardian again', () => {
  const { api, storage } = fixture({ stored: answer() });
  api.reset();
  assert.equal(storage.has(STORAGE_KEY), false);
  assert.equal(api.payloadJson(), '');
});

test('a new visitor waits for the recruitment check; a returning one does not', () => {
  assert.ok(fixture().classes.has('guardian-entry-checking'), 'The form stays hidden until the answer comes');
  assert.equal(fixture({ stored: answer() }).classes.has('guardian-entry-checking'), false,
    'Someone who already agreed in this tab is never turned away');
});

function entryAnswer(fetch) {
  const { api } = fixture({ fetch });
  return new Promise(resolve => api.checkEntry('https://example.test/rpc', 'key', resolve));
}

test('only a closed answer from the server shows 募集終了', async () => {
  const calls = [];
  const reply = (ok, body) => (url, init) => { calls.push({ url, init }); return Promise.resolve({ ok, json: () => Promise.resolve(body) }); };
  assert.equal(await entryAnswer(reply(true, { open: false })), false);
  assert.equal(calls[0].url, 'https://example.test/rpc');
  assert.equal(calls[0].init.method, 'POST');
  assert.equal(calls[0].init.headers.apikey, 'key');
  assert.equal(await entryAnswer(reply(true, { open: true })), true);
  // Anything unexpected keeps the form: a bad connection must not turn away someone still welcome.
  assert.equal(await entryAnswer(reply(false, { open: false })), true, 'HTTP error');
  assert.equal(await entryAnswer(reply(true, null)), true, 'empty body');
  assert.equal(await entryAnswer(() => Promise.reject(new Error('offline'))), true, 'network error');
  assert.equal(await entryAnswer(undefined), true, 'no fetch');
  const { api } = fixture({ fetch: reply(true, { open: false }) });
  assert.equal(await new Promise(resolve => api.checkEntry('', 'key', resolve)), true, 'no URL');
});

test('a server that never answers shows the form after the timeout', async () => {
  const { api } = fixture({ fetch: (url, init) => new Promise((_, reject) => init.signal.addEventListener('abort', () => reject(new Error('aborted')))) });
  const started = Date.now();
  assert.equal(await new Promise(resolve => api.checkEntry('https://example.test/rpc', 'key', resolve)), true);
  assert.ok(Date.now() - started >= 4900);
});

test('the 募集終了 sheet and its check are on the page', () => {
  const html = fs.readFileSync(path.join(rootPath, 'Assets/WebGLTemplates/FixedAspect/index.html'), 'utf8');
  assert.match(html, /data-entry-status-url="https:\/\/[a-z]+\.supabase\.co\/rest\/v1\/rpc\/research_entry_status"/);
  assert.match(html, /data-entry-status-key="sb_publishable_/);
  const sheet = html.slice(html.indexOf('id="guardian-consent-closed"'), html.indexOf('</article>', html.indexOf('id="guardian-consent-closed"')));
  assert.ok(sheet.includes('hidden'));
  assert.ok(sheet.includes('モニター調査の募集は終了しました'));
  assert.equal(sheet.includes('<button'), false, 'Nothing on the sheet leads into the game');
});
