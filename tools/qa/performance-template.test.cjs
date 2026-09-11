const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

const root = path.resolve(__dirname, '../..');
function fixture() {
  const canvas = { width: 3840, height: 2160, style: { width: '100%', height: 'auto' } };
  const config = {};
  const library = {};
  const document = { hidden: false, getElementById: () => null };
  const window = { navigator: {}, devicePixelRatio: 3 };
  const context = vm.createContext({ window, document, navigator: window.navigator,
    LibraryManager: { library }, mergeInto: Object.assign });
  vm.runInContext(fs.readFileSync(path.join(root, 'Assets/WebGLTemplates/FixedAspect/TemplateData/performance.js'), 'utf8'), context);
  vm.runInContext(fs.readFileSync(path.join(root, 'Assets/Plugins/WebGL/GamePerformance.jslib'), 'utf8'), context);
  const state = window.GeoModelPerformanceInstaller.install(canvas, config);
  return { canvas, config, state, library, document };
}

test('Retina rendering starts at 720p while preserving CSS layout', () => {
  const { canvas, config } = fixture();
  assert.deepEqual([canvas.width, canvas.height], [1280, 720]);
  assert.equal(config.devicePixelRatio, 1);
  assert.equal(config.matchWebGLToCanvasSize, false);
  assert.deepEqual(canvas.style, { width: '100%', height: 'auto' });
});

test('Unity quality bridge changes actual canvas size and restores manual settings', () => {
  const { canvas, state, library } = fixture();
  library.GeoModel_SetRenderResolution(960, 540, 0, 0);
  assert.deepEqual([canvas.width, canvas.height, state.quality, state.manual], [960, 540, 0, false]);
  library.GeoModel_SetRenderResolution(1280, 720, 3, 1);
  assert.deepEqual([canvas.width, canvas.height, state.quality, state.manual], [1280, 720, 3, true]);
});

test('Invalid and oversized requests cannot create oversized render buffers', () => {
  const { canvas, state } = fixture();
  state.setResolution(7680, 4320);
  assert.deepEqual([canvas.width, canvas.height], [1280, 720]);
  for (const value of [NaN, Infinity, -1, 0]) state.setResolution(value, 720);
  assert.deepEqual([canvas.width, canvas.height], [1280, 720]);
});

test('Hidden tabs and portrait overlays are excluded from gameplay measurements', () => {
  const { document, library } = fixture();
  assert.equal(library.GeoModel_IsPerformanceSampleEligible(), 1);
  document.hidden = true;
  assert.equal(library.GeoModel_IsPerformanceSampleEligible(), 0);
  document.hidden = false;
  document.getElementById = () => ({ classList: { contains: name => name === 'is-visible' } });
  assert.equal(library.GeoModel_IsPerformanceSampleEligible(), 0);
});

test('Missing optional hardware properties do not prevent safe startup', () => {
  const { state } = fixture();
  assert.equal(state.logicalProcessors, 0);
  assert.equal(state.memoryMb, 0);
  assert.equal(state.quality, 1);
});
