const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

// 2026-09-29: fullscreen was dropped on purpose. iPadOS always exits it on a
// downward swipe, and switching between fullscreen and the page confused players.
const template = path.resolve(__dirname, '../../Assets/WebGLTemplates/FixedAspect');

function templateSources() {
  const data = path.join(template, 'TemplateData');
  return ['index.html', ...fs.readdirSync(data)
    .filter(name => /\.(js|css)$/.test(name))
    .map(name => path.join('TemplateData', name))]
    .map(name => [name, fs.readFileSync(path.join(template, name), 'utf8')]);
}

test('template never requests or exits fullscreen', () => {
  for (const [name, source] of templateSources()) {
    assert.doesNotMatch(source, /requestFullscreen|webkitRequestFullscreen|exitFullscreen/, name);
  }
});

test('template no longer ships the fullscreen helper', () => {
  assert.equal(fs.existsSync(path.join(template, 'TemplateData/fullscreen.js')), false);
  const html = fs.readFileSync(path.join(template, 'index.html'), 'utf8');
  assert.doesNotMatch(html, /fullscreen\.js|fullscreen-help|全画面にする/);
});
