import json, sys, time
from pathlib import Path
root=Path(__file__).resolve().parents[2]
command=root/'Logs/remediation/review-command.json'
result=command.with_name('review-result.json')
data=json.loads(sys.argv[1])
result.unlink(missing_ok=True)
pending = command.with_suffix(".tmp")
pending.write_text(json.dumps(data))
pending.replace(command)
for _ in range(200):
 if result.exists():
  print(result.read_text());break
 time.sleep(.1)
else: raise TimeoutError('Unity review command timed out')
