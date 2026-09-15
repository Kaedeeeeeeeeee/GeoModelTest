"""Adjust only the isolated recording copy; never the shipped game."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
player = root / 'Logs/manual-video/player'
index = player / 'index.html'
html = index.read_text()
html = html.replace('<script src="TemplateData/performance.js">', '''<script>
// The tutorial demonstrates the shipped tablet controls on a desktop recorder.
Object.defineProperty(navigator, 'maxTouchPoints', {get: () => 5});
Object.defineProperty(navigator, 'platform', {get: () => 'MacIntel'});
// This isolated recording session must never contact the research backend.
const tutorialFetch = window.fetch.bind(window);
window.fetch = function(input, init) {
  const url = typeof input === 'string' ? input : input.url;
  if (/supabase\\.(co|in)/.test(url)) return Promise.reject(new Error('Tutorial offline session'));
  return tutorialFetch(input, init);
};
setInterval(() => {
  if (!window.unityInstance || window.tutorialQualityApplied) return;
  window.tutorialQualityApplied = true;
  window.unityInstance.SendMessage('GamePerformanceSettings', 'SetManualQualityLevel', 5);
  window.unityInstance.Module.setCanvasSize(1920, 1080);
}, 1000);
</script>
<script src="TemplateData/performance.js">''')
html = html.replace('canvas.width = 1280;', 'canvas.width = 1920;').replace('canvas.height = 720;', 'canvas.height = 1080;')
index.write_text(html)
perf = player / 'TemplateData/performance.js'
js = perf.read_text()
js = re.sub(r'var targetWidth = .*?;', 'var targetWidth = 1920;', js)
perf.write_text(js)
print(player)
