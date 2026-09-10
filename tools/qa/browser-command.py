import json,sys,time
from pathlib import Path
root=Path(__file__).resolve().parents[2]/'Logs/remediation'
result=root/'browser-result.json'
result.unlink(missing_ok=True)
pending=root/'browser-command.tmp'
pending.write_text(sys.argv[1]);pending.replace(root/'browser-command.json')
for _ in range(400):
 if result.exists():
  data=json.loads(result.read_text());print(json.dumps(data,ensure_ascii=False));assert data.get('ok');break
 time.sleep(.1)
else:raise TimeoutError('Browser command did not finish')
