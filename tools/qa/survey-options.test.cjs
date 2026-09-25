const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const root = path.resolve(__dirname, '../../Assets/StreamingAssets/Survey');
const definition = JSON.parse(fs.readFileSync(path.join(root, 'questions.json'), 'utf8'));
const ticket = 'a'.repeat(64);
const draftKey = 'geomodel.survey.draft.' + ticket;

async function fixture({ preview = false, draft = null } = {}) {
  const nodes = [];
  class Element {
    constructor(tag) {
      this.tag = tag; this.children = []; this.hidden = false; this.textContent = '';
      this.attributes = {}; this.listeners = {};
      const classes = new Set();
      this.classList = { add: value => classes.add(value), remove: value => classes.delete(value), contains: value => classes.has(value) };
      nodes.push(this);
    }
    append(...children) { this.children.push(...children); }
    replaceChildren(...children) { this.children = children; }
    addEventListener(name, handler) { this.listeners[name] = handler; }
    setAttribute(name, value) { this.attributes[name] = value; }
    removeAttribute(name) { delete this.attributes[name]; }
    focus() {}
    scrollIntoView() {}
  }
  const html = fs.readFileSync(path.join(root, preview ? 'preview.html' : 'index.html'), 'utf8');
  for (const match of html.matchAll(/id="([^"]+)"/g)) { const node = new Element('div'); node.id = match[1]; }
  const byId = id => nodes.find(node => node.id === id);
  const storage = new Map();
  if (draft) storage.set(draftKey, JSON.stringify({ version: definition.version, step: 0, answers: draft }));
  const requests = [];
  let offline = false;
  const context = vm.createContext({
    window: { GEOMODEL_QUESTIONS: definition, GEOMODEL_SURVEY: { apiUrl: 'https://example.test/survey' }, scrollTo() {} },
    document: { getElementById: byId, createElement: tag => new Element(tag), createTextNode: text => ({ textContent: text }) },
    sessionStorage: { getItem: key => storage.get(key) ?? null, setItem: (key, value) => storage.set(key, value), removeItem: key => storage.delete(key) },
    location: { hash: '#ticket=' + ticket, hostname: 'example.test', pathname: '/index.html', search: '' },
    history: { replaceState() {} }, URL, URLSearchParams, AbortController, setTimeout, clearTimeout,
    fetch: async (_url, options) => {
      const request = JSON.parse(options.body); requests.push(request);
      if (offline && request.action === 'submit') throw new Error('offline');
      return { ok: true, json: async () => ({ ok: true, submitted: false, surveyVersion: definition.version }) };
    }
  });
  vm.runInContext(fs.readFileSync(path.join(root, preview ? 'preview.js' : 'survey.js'), 'utf8'), context);
  await new Promise(resolve => setImmediate(resolve));
  const radios = nodes.filter(node => node.type === 'radio');
  return {
    nodes, radios, byId, storage, requests,
    setOffline(value) { offline = value; },
    select(id, value) {
      const input = radios.find(node => node.name === id && node.value === value);
      assert.ok(input, `Missing option ${id}=${value}`);
      input.checked = true;
      (input.listeners.change || input.onchange)();
    },
    write(id, value) {
      const input = nodes.find(node => node.tag === 'textarea' && node.name === id);
      input.value = value;
      (input.listeners.input || input.oninput)();
    },
    submit: () => byId('survey').onsubmit({ preventDefault() {} })
  };
}

const completeAnswers = () => Object.fromEntries(definition.questions.filter(q => q.scale).map(q => [q.id, '3']));

test('Formal survey and preview show 55 options and retain two optional text fields', async () => {
  for (const preview of [false, true]) {
    const f = await fixture({ preview });
    assert.equal(f.radios.length, 55);
    assert.ok(f.radios.every(input => /^[1-5]$/.test(input.value)));
    const textareas = f.nodes.filter(node => node.tag === 'textarea');
    assert.equal(textareas.length, 2);
    assert.ok(textareas.every(input => input.maxLength === 300));
    assert.ok(!f.byId('privacy').textContent.includes('答えたくない'));
  }
});

test('Old skip draft is cleared while valid choices and free text survive', async () => {
  const f = await fixture({ draft: { ...completeAnswers(), q7: 'skip', q13: '地層の観察', unknown: 'skip' } });
  const saved = JSON.parse(f.storage.get(draftKey)).answers;
  assert.equal(saved.q7, undefined);
  assert.equal(saved.unknown, undefined);
  assert.equal(saved.q8, '3');
  assert.equal(saved.q13, '地層の観察');
  await f.submit();
  assert.equal(f.requests.filter(r => r.action === 'submit').length, 0);
  assert.equal(f.byId('field-q7').attributes['aria-invalid'], 'true');
  assert.match(f.byId('form-error').textContent, /各質問で答えを1つ選んでください/);
  assert.ok(!f.byId('form-error').textContent.includes('答えたくない'));
  f.select('q7', '4');
  await f.submit();
  assert.equal(f.requests.at(-1).answers.q7, '4');
  assert.equal(f.requests.at(-1).answers.q13, '地層の観察');
  assert.equal(f.storage.has(draftKey), false);
});

test('Blank free answers can submit after a failed request without losing choices', async () => {
  const f = await fixture({ draft: completeAnswers() });
  f.setOffline(true);
  await f.submit();
  assert.equal(f.byId('complete').hidden, true);
  assert.equal(JSON.parse(f.storage.get(draftKey)).answers.q1, '3');
  assert.match(f.byId('form-error').textContent, /送信を確認できませんでした/);
  f.setOffline(false);
  await f.submit();
  assert.equal(f.byId('complete').hidden, false);
  assert.equal(Object.keys(f.requests.at(-1).answers).length, 11);
  assert.ok(Object.values(f.requests.at(-1).answers).every(value => value === '3'));
});

test('Preview requires the 11 choices but permits both free-answer fields to remain blank', async () => {
  const f = await fixture({ preview: true });
  await f.submit();
  assert.equal(f.byId('form-error').hidden, false);
  assert.match(f.byId('form-error').textContent, /各質問で答えを1つ選んでください/);
  for (const question of definition.questions.filter(q => q.scale)) f.select(question.id, '5');
  await f.submit();
  assert.equal(f.byId('preview-complete').hidden, false);
  assert.equal(f.requests.length, 0);
});

test('An oversized old draft survives intact but requires shortening before submission', async () => {
  for (const id of ['q13', 'q14']) {
    const f = await fixture({ draft: { ...completeAnswers(), [id]: 'あ'.repeat(301) } });
    assert.equal(JSON.parse(f.storage.get(draftKey)).answers[id].length, 301);
    await f.submit();
    assert.equal(f.requests.filter(r => r.action === 'submit').length, 0);
    assert.equal(f.byId('form-error').textContent, '自由回答は300文字以内で入力してください。');
    f.write(id, 'あ'.repeat(300));
    await f.submit();
    assert.equal(f.requests.at(-1).answers[id].length, 300);
    assert.equal(f.byId('complete').hidden, false);
  }
});

test('Preview uses the same 300-character boundary for both free answers', async () => {
  for (const id of ['q13', 'q14']) {
    const f = await fixture({ preview: true });
    for (const question of definition.questions.filter(q => q.scale)) f.select(question.id, '3');
    f.write(id, 'あ'.repeat(301));
    await f.submit();
    assert.equal(f.byId('form-error').textContent, '自由回答は300文字以内で入力してください。');
    f.write(id, 'あ'.repeat(300));
    await f.submit();
    assert.equal(f.byId('preview-complete').hidden, false);
  }
});
