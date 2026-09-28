const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

const rootPath = path.resolve(__dirname, '../..');
const source = fs.readFileSync(path.join(rootPath,
  'Assets/WebGLTemplates/FixedAspect/TemplateData/fullscreen.js'), 'utf8');

function target() {
  const listeners = new Map();
  return {
    hidden: true, textContent: '',
    addEventListener(name, callback) {
      if (!listeners.has(name)) listeners.set(name, []);
      listeners.get(name).push(callback);
    },
    dispatch(name, event = {}) {
      for (const callback of listeners.get(name) || []) callback(event);
    }
  };
}

function fixture(options = {}) {
  const document = target();
  const elements = Object.fromEntries(['help', 'message', 'retry', 'dismiss'].map(name =>
    ['fullscreen-' + name, target()]));
  const root = {};
  const calls = [];
  const timers = new Map();
  let nextTimer = 0;
  document.documentElement = root;
  document.getElementById = id => elements[id];
  document.fullscreenEnabled = options.enabled !== false;
  document.fullscreenElement = options.initialFullscreen ? root : null;
  const navigator = {
    userAgent: options.userAgent || '', platform: options.platform || '',
    maxTouchPoints: options.maxTouchPoints || 0, standalone: options.standalone || false
  };
  const window = {
    navigator, PointerEvent: options.legacyTouch ? undefined : function () {},
    matchMedia: query => ({ matches: (query === '(pointer: coarse)' && options.touch !== false) ||
      (query === '(display-mode: fullscreen)' && options.hostFullscreen === true) })
  };
  if (options.api !== 'missing') {
    const name = options.api === 'webkit' ? 'webkitRequestFullscreen' : 'requestFullscreen';
    root[name] = function (config) {
      calls.push({ element: this, config });
      if (options.reject) return Promise.reject(new Error('Policy denied'));
      if (options.throw) throw new Error('Not supported');
      if (options.neverSettles) return undefined;
      document[options.api === 'webkit' ? 'webkitFullscreenElement' : 'fullscreenElement'] = root;
      document.dispatch(options.api === 'webkit' ? 'webkitfullscreenchange' : 'fullscreenchange');
      return options.api === 'webkit' ? undefined : Promise.resolve();
    };
  }
  vm.runInNewContext(source, {
    window, document, navigator,
    setTimeout(callback, delay) { timers.set(++nextTimer, { callback, delay }); return nextTimer; },
    clearTimeout(id) { timers.delete(id); }
  });
  window.GeoModelFullscreen.install();
  const gameTarget = { closest: selector => selector.includes('#unity-canvas') };
  return {
    document, root, calls, elements, timers,
    tap(event = {}) {
      document.dispatch(options.legacyTouch ? 'touchend' : 'pointerup',
        { isTrusted: true, target: gameTarget, ...event });
    },
    exit() {
      document.fullscreenElement = null;
      document.webkitFullscreenElement = null;
      document.dispatch('fullscreenchange');
    },
    expire(delay) {
      for (const [id, timer] of [...timers]) if (timer.delay === delay) {
        timers.delete(id); timer.callback();
      }
    }
  };
}

test('first real completed mobile tap enters document fullscreen synchronously without swallowing game input', () => {
  const f = fixture();
  assert.equal(f.calls.length, 0);
  f.tap({ isTrusted: false });
  assert.equal(f.calls.length, 0);
  let consumed = false;
  f.tap({ preventDefault() { consumed = true; }, stopPropagation() { consumed = true; } });
  assert.equal(f.calls.length, 1);
  assert.equal(f.calls[0].element, f.root);
  assert.equal(f.calls[0].config.navigationUI, 'hide');
  assert.equal(consumed, false);
  assert.equal(f.elements['fullscreen-help'].hidden, true);
  f.tap();
  assert.equal(f.calls.length, 1);
});

test('desktop remains windowed and shows no mobile helper', () => {
  const f = fixture({ touch: false });
  f.tap();
  assert.equal(f.calls.length, 0);
  assert.equal(f.elements['fullscreen-help'].hidden, true);
});

test('iPad with a trackpad remains eligible without requiring a coarse pointer', () => {
  const f = fixture({ touch: false, platform: 'MacIntel', maxTouchPoints: 5 });
  f.tap();
  assert.equal(f.calls.length, 1);
});

test('exit gesture is respected; only a deliberate retry enters again', () => {
  const f = fixture();
  f.tap();
  f.exit();
  f.tap();
  assert.equal(f.calls.length, 1);
  assert.equal(f.elements['fullscreen-help'].hidden, false);
  assert.equal(f.elements['fullscreen-message'].hidden, true);
  f.elements['fullscreen-retry'].dispatch('click');
  assert.equal(f.calls.length, 2);
  assert.equal(f.elements['fullscreen-help'].hidden, true);
});

test('fullscreen established by the host is never exited or automatically reentered', () => {
  const f = fixture({ initialFullscreen: true });
  f.tap();
  assert.equal(f.calls.length, 0);
  f.exit();
  f.tap();
  assert.equal(f.calls.length, 0);
});

test('ancestor fullscreen does not cause automatic reentry after the host exits', () => {
  const options = { hostFullscreen: true };
  const f = fixture(options);
  f.tap();
  assert.equal(f.calls.length, 0);
  options.hostFullscreen = false;
  f.tap();
  assert.equal(f.calls.length, 0);
});

test('unsupported iPhone browser and iframe policy restriction get one temporary explanation', () => {
  for (const options of [{ api: 'missing', userAgent: 'iPhone' }, { enabled: false }]) {
    const f = fixture(options);
    f.tap();
    assert.equal(f.calls.length, 0);
    assert.equal(f.elements['fullscreen-help'].hidden, false);
    assert.equal(f.elements['fullscreen-retry'].hidden, true);
    assert.match(f.elements['fullscreen-message'].textContent, /全画面にできません/);
    f.expire(10000);
    assert.equal(f.elements['fullscreen-help'].hidden, true);
    f.tap();
    assert.equal(f.elements['fullscreen-help'].hidden, true);
  }
});

test('denied Promise is handled, retry stays available and gameplay does not repeatedly request', async () => {
  const f = fixture({ reject: true });
  f.tap();
  await Promise.resolve();
  assert.equal(f.elements['fullscreen-help'].hidden, false);
  assert.equal(f.elements['fullscreen-retry'].hidden, false);
  f.tap();
  assert.equal(f.calls.length, 1);
  f.elements['fullscreen-dismiss'].dispatch('click');
  assert.equal(f.elements['fullscreen-help'].hidden, true);
});

test('older iPhone Safari gets actionable toolbar guidance without suggesting it is fullscreen', () => {
  const f = fixture({ api: 'missing', userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 16_7 like Mac OS X) Version/16.6 Mobile/15E148 Safari/604.1' });
  f.tap();
  assert.equal(f.calls.length, 0);
  assert.match(f.elements['fullscreen-message'].textContent, /ツールバーを非表示/);
  assert.equal(f.elements['fullscreen-retry'].hidden, true);
  f.expire(10000);
  assert.equal(f.elements['fullscreen-help'].hidden, true);
});

test('prefixed Safari without PointerEvent requests on touchend and handles change events', () => {
  const f = fixture({ api: 'webkit', legacyTouch: true });
  f.tap();
  assert.equal(f.calls.length, 1);
  assert.equal(f.calls[0].config, undefined);
  assert.equal(f.document.webkitFullscreenElement, f.root);
  assert.equal(f.elements['fullscreen-help'].hidden, true);
});

test('throwing or stalled browser request recovers to a temporary manual retry', () => {
  for (const options of [{ throw: true }, { neverSettles: true }]) {
    const f = fixture(options);
    f.tap();
    f.expire(2000);
    assert.equal(f.calls.length, 1);
    assert.equal(f.elements['fullscreen-help'].hidden, false);
    assert.equal(f.elements['fullscreen-retry'].hidden, false);
  }
});

test('loading controls and standalone app do not trigger fullscreen requests', () => {
  const f = fixture();
  f.tap({ target: { closest: () => false } });
  assert.equal(f.calls.length, 0);
  f.tap();
  assert.equal(f.calls.length, 1);
  const standalone = fixture({ standalone: true });
  standalone.tap();
  assert.equal(standalone.calls.length, 0);
  assert.equal(standalone.elements['fullscreen-help'].hidden, true);
});
