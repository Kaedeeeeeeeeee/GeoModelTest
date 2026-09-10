"""Generate the browser question bundle from the canonical versioned survey definition."""
import json
import sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'Assets/StreamingAssets/Survey'
data=json.loads((root/'questions.json').read_text())
assert len(data['questions'])==14
assert len({q['id'] for q in data['questions']})==14
text='// Generated from questions.json. Run tools/sync-survey.py after editing.\nwindow.GEOMODEL_QUESTIONS = '+json.dumps(data,ensure_ascii=False)+';\n'
target=root/'questions.js'
if '--check' in sys.argv:
    assert target.read_text()==text, 'Run python3 tools/sync-survey.py'
else:
    target.write_text(text)
print('14 questionnaire items synchronized')
