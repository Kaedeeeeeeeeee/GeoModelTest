const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

// The title-screen fullscreen button (2026-11-26): who is offered it, and that one press asks the browser once,
// from inside the browser's own release event.
const source = fs.readFileSync(path.resolve(__dirname, '../../Assets/Plugins/WebGL/MobileDeviceDetection.jslib'), 'utf8');

const UA = {
  chromebook: 'Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
  android: 'Mozilla/5.0 (Linux; Android 14; SM-X200) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
  iphone: 'Mozilla/5.0 (iPhone; CPU iPhone OS 18_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Mobile/15E148 Safari/604.1',
  ipadDesktop: 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15',
};

function load({ ua = UA.chromebook, platform = 'Linux x86_64', touchPoints = 0, coarse = false, enabled = true,
  displayMode = 'browser', inner = [1280, 650], screenSize = [1366, 768] } = {}) {
  const listeners = {};
  const requests = [];
  const timers = [];
  const document = {
    fullscreenEnabled: enabled,
    fullscreenElement: null,
    documentElement: {
      requestFullscreen(options) {
        requests.push(options);
        return Promise.resolve().then(() => { document.fullscreenElement = document.documentElement; });
      },
    },
    addEventListener(type, fn) { (listeners[type] ||= new Set()).add(fn); },
    removeEventListener(type, fn) { listeners[type]?.delete(fn); },
  };
  const orientationLocks = [];
  const window = {
    innerWidth: inner[0], innerHeight: inner[1],
    matchMedia: q => ({ matches: q === '(pointer: coarse)' ? coarse : q === `(display-mode: ${displayMode})` }),
  };
  const library = {};
  const context = {
    LibraryManager: { library },
    mergeInto: (target, add) => Object.assign(target, add),
    navigator: { userAgent: ua, platform, maxTouchPoints: touchPoints },
    document, window,
    screen: { width: screenSize[0], height: screenSize[1],
      orientation: { lock: o => { orientationLocks.push(o); return Promise.resolve(); } } },
    setTimeout: (fn, ms) => { timers.push({ fn, ms }); return timers.length; },
    clearTimeout: () => {},
    console: { warn() {} },
  };
  vm.runInNewContext(source, context);
  // Emscripten exposes $-prefixed entries as globals without the "$".
  context.GeoModelFullscreen = library.$GeoModelFullscreen;
  const fns = {};
  for (const name of ['GeoModelTest_FullscreenAvailability', 'GeoModelTest_ArmFullscreen', 'GeoModelTest_RequestFullscreen'])
    fns[name] = vm.runInNewContext(`(${library[name].toString()})`, context);
  const fire = type => [...(listeners[type] || [])].forEach(fn => fn({ type }));
  const listenerCount = () => Object.values(listeners).reduce((n, s) => n + s.size, 0);
  return { fns, fire, requests, timers, listenerCount, orientationLocks, document };
}

test('computers, Chromebooks and Android are offered fullscreen; Apple touch devices are not', () => {
  assert.equal(load().fns.GeoModelTest_FullscreenAvailability(), 1);
  assert.equal(load({ ua: UA.android, coarse: true, inner: [800, 1180], screenSize: [800, 1280] })
    .fns.GeoModelTest_FullscreenAvailability(), 2);
  assert.equal(load({ ua: UA.iphone, coarse: true }).fns.GeoModelTest_FullscreenAvailability(), 0);
  assert.equal(load({ ua: UA.ipadDesktop, platform: 'MacIntel', touchPoints: 5, coarse: true })
    .fns.GeoModelTest_FullscreenAvailability(), 0);
});

test('no offer when the window is already as big as it gets or the page may not go fullscreen', () => {
  assert.equal(load({ enabled: false }).fns.GeoModelTest_FullscreenAvailability(), 0, 'iframe without allowfullscreen');
  assert.equal(load({ displayMode: 'standalone' }).fns.GeoModelTest_FullscreenAvailability(), 0, 'home-screen app');
  assert.equal(load({ inner: [1366, 768] }).fns.GeoModelTest_FullscreenAvailability(), 0, 'F11 window');
});

test('a press arms the request and the browser release performs it exactly once', async () => {
  const page = load({ ua: UA.android, coarse: true });
  page.fns.GeoModelTest_ArmFullscreen();
  assert.equal(page.requests.length, 0, 'nothing happens on press');
  page.fire('pointerup');
  page.fire('touchend');
  page.fns.GeoModelTest_RequestFullscreen(); // the Unity click callback right after
  assert.equal(page.requests.length, 1);
  assert.equal(page.requests[0].navigationUI, 'hide');
  assert.equal(page.listenerCount(), 0, 'the one-shot listeners are removed');
  await new Promise(r => setImmediate(r));
  assert.deepEqual(page.orientationLocks, ['landscape'], 'touch screens stay landscape while fullscreen');
  page.fns.GeoModelTest_RequestFullscreen();
  assert.equal(page.requests.length, 1, 'already fullscreen');
});

test('an armed press that never becomes a click expires', () => {
  const page = load();
  page.fns.GeoModelTest_ArmFullscreen();
  assert.equal(page.timers.at(-1).ms, 3000);
  page.timers.at(-1).fn();
  page.fire('pointerup');
  assert.equal(page.requests.length, 0);
  assert.equal(page.listenerCount(), 0);
});

test('keyboard Enter on the button asks directly', () => {
  const page = load();
  page.fns.GeoModelTest_RequestFullscreen();
  assert.equal(page.requests.length, 1);
});
