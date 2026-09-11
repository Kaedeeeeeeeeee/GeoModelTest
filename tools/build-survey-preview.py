"""Package the teacher preview alone, including a self-contained offline HTML copy."""
import argparse
import base64
import hashlib
import shutil
import subprocess
import sys
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
source = Path(__file__).resolve().parents[1] / 'Assets/StreamingAssets/Survey'
subprocess.run([sys.executable, str(Path(__file__).with_name('sync-survey.py')), '--check'], check=True)
output = args.output.resolve()
output.mkdir(parents=True, exist_ok=True)
allowed = {'index.html', 'survey.css', 'preview.css', 'questions.js', 'preview.js',
           'survey-preview-offline.html', '_headers', 'robots.txt'}
unexpected = {p.name for p in output.iterdir()} - allowed
if unexpected:
    raise SystemExit('Use a dedicated preview output directory; unexpected files: ' + ', '.join(sorted(unexpected)))
html = (source / 'preview.html').read_text()
(output / 'index.html').write_text(html)
for name in ['survey.css', 'preview.css', 'questions.js', 'preview.js']:
    shutil.copyfile(source / name, output / name)

def csp_hash(content):
    return "'sha256-" + base64.b64encode(hashlib.sha256(content.encode()).digest()).decode() + "'"

script_hashes = []
style_hashes = []
for name in ['survey.css', 'preview.css']:
    content = (source / name).read_text()
    html = html.replace(f'<link rel="stylesheet" href="{name}">', f'<style>{content}</style>')
    style_hashes.append(csp_hash(content))
scripts = []
for name in ['questions.js', 'preview.js']:
    content = (source / name).read_text().replace('</script', '<\\/script')
    html = html.replace(f'<script src="{name}" defer></script>', '')
    scripts.append(f'<script>{content}</script>')
    script_hashes.append(csp_hash(content))
html = html.replace("script-src 'self'", 'script-src ' + ' '.join(script_hashes))
html = html.replace("style-src 'self'", 'style-src ' + ' '.join(style_hashes))
html = html.replace('</body>', '\n'.join(scripts) + '\n</body>')
(output / 'survey-preview-offline.html').write_text(html)
(output / 'robots.txt').write_text('User-agent: *\nDisallow: /\n')
(output / '_headers').write_text('/*\n  X-Robots-Tag: noindex, nofollow\n  Referrer-Policy: no-referrer\n  X-Content-Type-Options: nosniff\n')
print(f'Teacher preview packaged at {output}')
