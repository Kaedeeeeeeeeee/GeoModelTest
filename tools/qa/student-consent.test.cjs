const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

const rootPath = path.resolve(__dirname, '../..');
const templatePath = path.join(rootPath, 'Assets/WebGLTemplates/FixedAspect');
const source = fs.readFileSync(path.join(templatePath, 'TemplateData/student-consent.js'), 'utf8');
const html = fs.readFileSync(path.join(templatePath, 'index.html'), 'utf8');

// The approved 生徒の同意 wording (ui.consent.* until 2026-11-11), word for word.
const APPROVED = {
  title: '理科 3D ゲーム型教材に関する研究に参加する生徒の皆さんへ',
  body: '研究開発責任者\n稲垣忠（東北学院大学人間情報学研究科教授）\n研究開発担当者\n張詩楓（東北学院大学人間情報学研究科博士前期課程）\n\n' +
    'このゲームは、理科（地学分野）の学習用 3D ゲーム教材です。ゲームの改善および学習効果の検証を行うため、あなたのゲーム中の操作を記録します。また、ゲーム終了後にアンケートへの回答にご協力いただきます。所要時間は教材体験が 15 分程度、アンケートは 5 分程度を想定しています。操作記録やアンケートの回答は、個人が特定されない形で研究に使用します。\n\n' +
    '参加するかどうかは、あなた自身で決めることができます。また、参加した後でも、気分が悪くなったり、やめたいと思ったりしたときは、いつでも途中でやめることができます。\n\n' +
    '内容を確認したうえで、次の項目にチェックしてください。',
  agreements: [
    '保護者から、この研究に参加することについて同意を得ています。',
    '上の説明を読み、研究の内容について理解しました。',
    '自分の意思で、この研究に参加することに同意します。'
  ],
  continue: '同意してゲームを開始する'
};

function fixture() {
  const classes = new Set();
  const windowListeners = [];
  const window = {
    addEventListener(type, callback, capture) { windowListeners.push({ type, callback, capture }); }
  };
  const document = {
    documentElement: { classList: { add: name => classes.add(name), remove: name => classes.delete(name) } },
    readyState: 'complete',
    getElementById: () => null,
    addEventListener() {}
  };
  const context = { window, document };
  vm.createContext(context);
  vm.runInContext(source, context);
  return { api: window.GeoModelStudentConsent, classes, windowListeners };
}

test('every page load starts with the student sheet in front of the game', () => {
  const { api, classes } = fixture();
  assert.equal(api.isPending(), true);
  assert.ok(classes.has('student-consent-pending'));
});

test('the student must confirm all three statements', () => {
  const { api } = fixture();
  assert.equal(api.studentReady([true, true, true]), true);
  for (const checks of [[], [true, true], [true, false, true], [true, true, 'true']]) {
    assert.equal(api.studentReady(checks), false, JSON.stringify(checks));
  }
});

test('keys never reach Unity while the sheet is open', () => {
  const { windowListeners } = fixture();
  const shields = windowListeners.filter(entry => ['keydown', 'keypress', 'keyup'].includes(entry.type));
  assert.equal(shields.length, 3);
  assert.ok(shields.every(entry => entry.capture === true), 'Capture listeners run before Unity registers its own.');
  const event = { stopped: false, stopImmediatePropagation() { this.stopped = true; } };
  shields[0].callback(event);
  assert.equal(event.stopped, true);
});

test('the student sheet shows the approved wording word for word', () => {
  const sheet = html.slice(html.indexOf('id="student-consent-view"'), html.indexOf('id="student-consent-declined"'));
  const plain = sheet.replace(/<br>/g, '\n').replace(/<\/p>/g, '\n\n').replace(/<[^>]+>/g, '');
  assert.ok(plain.includes(APPROVED.title));
  for (const block of APPROVED.body.split('\n\n')) assert.ok(plain.includes(block), block);
  APPROVED.agreements.forEach(text => assert.ok(plain.includes(text), text));
  assert.ok(plain.includes(APPROVED.continue));
  assert.equal((sheet.match(/name="studentAgreement"/g) || []).length, 3);
});

test('the 保護者同意 page stays removed', () => {
  assert.equal(html.includes('guardian'), false);
  assert.equal(html.includes('保護者氏名'), false);
  assert.equal(fs.existsSync(path.join(templatePath, 'TemplateData/guardian-consent.js')), false);
  assert.match(html, /<script src="TemplateData\/student-consent\.js"><\/script>\s*<\/head>/,
    'Loaded in <head> so the sheet shows before first paint.');
});

test('nothing is handed to the game before the student confirms', () => {
  const { api } = fixture();
  assert.equal(api.payloadJson(), '');
});
