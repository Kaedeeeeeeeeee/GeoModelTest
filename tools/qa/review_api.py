"""Explicit editor QA commands; requires RemediationReview in a playing editor."""
import json
import time
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
DIRECTORY = ROOT / 'Logs/remediation'

def command(op, value=None, **kwargs):
    result = DIRECTORY / 'review-result.json'
    result.unlink(missing_ok=True)
    pending = DIRECTORY / 'review-command.tmp'
    pending.write_text(json.dumps(dict(op=op, value=value, **kwargs)))
    pending.replace(DIRECTORY / 'review-command.json')
    for _ in range(400):
        if result.exists():
            data = json.loads(result.read_text())
            assert data.get('ok'), data
            time.sleep(.3)
            return data
        time.sleep(.05)
    raise TimeoutError(op)

def state():
    command('state')
    return json.loads((DIRECTORY / 'ui-state.json').read_text())

def wait_scene(scene):
    deadline = time.monotonic() + 25
    while time.monotonic() < deadline:
        current = state()
        if current['scene'] == scene:
            time.sleep(1)
            return current
        time.sleep(.3)
    raise TimeoutError(scene)
