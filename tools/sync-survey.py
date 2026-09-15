"""Generate the browser question bundle from the canonical versioned survey definition."""
import json
import sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'Assets/StreamingAssets/Survey'
data=json.loads((root/'questions.json').read_text())
ids=[q['id'] for q in data['questions']]
assert ids and len(set(ids))==len(ids), 'Question IDs must be unique'
assert [id for section in data['sections'] for id in section['questions']]==ids, 'Sections must include each question once, in display order'
assert all(not q.get('scale') or q['scale'] in data['scales'] for q in data['questions']), 'Unknown answer scale'
text='// Generated from questions.json. Run tools/sync-survey.py after editing.\nwindow.GEOMODEL_QUESTIONS = '+json.dumps(data,ensure_ascii=False)+';\n'
target=root/'questions.js'
if '--check' in sys.argv:
    assert target.read_text()==text, 'Run python3 tools/sync-survey.py'
else:
    target.write_text(text)
print(f"{len(ids)} questionnaire items synchronized")
