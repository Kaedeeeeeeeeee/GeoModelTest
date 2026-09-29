const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

const rootPath = path.resolve(__dirname, '../..');
const source = fs.readFileSync(path.join(rootPath,
  'Assets/WebGLTemplates/FixedAspect/TemplateData/guardian-consent.js'), 'utf8');
const STORAGE_KEY = 'geomodel-guardian-consent';
const VERSION = 'guardian-ja-2026-09-29';

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
    addEventListener(type, callback, capture) { windowListeners.push({ type, callback, capture }); }
  };
  const document = {
    documentElement: { classList: { add: name => classes.add(name), remove: name => classes.delete(name) } },
    readyState: 'complete',
    getElementById: () => null,
    addEventListener() {}
  };
  const context = { window, document, URLSearchParams };
  vm.createContext(context);
  vm.runInContext(source, context);
  return { api: window.GeoModelGuardianConsent, classes, storage, windowListeners };
}

function answer(overrides = {}) {
  return Object.assign({ consentVersion: VERSION, guardianAgreed: true, guardianConfirmed: true,
    guardianName: '山田　花子', respondentId: 'T-0001', guardianConsentedAt: '2026-09-29T01:02:03.456Z' }, overrides);
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

test('a reload in the same tab reuses the answer instead of asking again', () => {
  const { api, classes } = fixture({ stored: answer() });
  assert.equal(api.isPending(), false);
  assert.ok(!classes.has('guardian-consent-pending'));
  assert.deepEqual(JSON.parse(api.payloadJson()), answer());
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
  assert.ok(api.evaluate({ choice: 'yes', guardianConfirmed: true, guardianName: '山田', respondentId: 'テスト01' }, new Date()).errors.respondentId);
  assert.ok(api.evaluate({ choice: 'yes', guardianConfirmed: true, guardianName: '山田\n花子', respondentId: 'A1' }, new Date()).errors.guardianName);
});

test('Japanese keyboard input is normalized before it is stored', () => {
  const { api } = fixture();
  const now = new Date('2026-09-29T10:00:00Z');
  const result = api.evaluate({ choice: 'yes', guardianConfirmed: true,
    guardianName: '　山田　花子　', respondentId: ' ＴＥＳＴー０１ ' }, now);
  assert.deepEqual(Object.keys(result.errors), []);
  assert.deepEqual(JSON.parse(JSON.stringify(result.payload)), answer({ respondentId: 'TEST-01', guardianConsentedAt: now.toISOString() }));
  assert.equal(api.normalizeRespondentId('ab‐cd−ef'), 'ab-cd-ef');
});

test('keys typed into the form never reach Unity while the form is open', () => {
  const pending = fixture();
  const shields = pending.windowListeners.filter(entry => ['keydown', 'keypress', 'keyup'].includes(entry.type));
  assert.equal(shields.length, 3);
  assert.ok(shields.every(entry => entry.capture === true), 'Capture listeners run before Unity registers its own.');
  const event = keyEvent();
  shields[0].callback(event);
  assert.equal(event.stopped, true);

  const done = fixture({ stored: answer() });
  const after = keyEvent();
  done.windowListeners.find(entry => entry.type === 'keydown').callback(after);
  assert.equal(after.stopped, false, 'Once answered, the game receives keys normally.');
});

test('a rejected answer is cleared so the next load asks the guardian again', () => {
  const { api, storage } = fixture({ stored: answer() });
  api.reset();
  assert.equal(storage.has(STORAGE_KEY), false);
  assert.equal(api.payloadJson(), '');
});
