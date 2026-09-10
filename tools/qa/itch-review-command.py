"""Send one explicit command to itch-release-review.py's local file queue."""
import json
import sys
import time
from pathlib import Path

directory = Path(__file__).resolve().parents[2]/'Logs/release-2026-09-11'
result = directory/'result.json'
result.unlink(missing_ok=True)
pending = directory/'command.tmp'
pending.write_text(json.dumps(json.loads(sys.argv[1])))
pending.replace(directory/'command.json')
deadline = time.monotonic()+240
while time.monotonic() < deadline:
    if result.exists():
        data = json.loads(result.read_text())
        print(json.dumps(data,ensure_ascii=False))
        assert data.get('ok'), data
        break
    time.sleep(.1)
else:
    raise TimeoutError('Published browser review did not respond')
