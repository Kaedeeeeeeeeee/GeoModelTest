"""Exercise the actual Unity dialogue buttons using review fixtures (all current correct choices are index 0)."""
import json
import sys
import time
from pathlib import Path

root = Path(__file__).resolve().parents[2]
directory = root / "Logs/remediation"
def command(op, value=None):
    result = directory / "review-result.json"
    result.unlink(missing_ok=True)
    (directory / "review-command.tmp").write_text(json.dumps(dict(op=op, value=value)))
    (directory / "review-command.tmp").replace(directory / "review-command.json")
    for _ in range(200):
        if result.exists():
            response = json.loads(result.read_text())
            assert response.get("ok"), response
            time.sleep(.12)
            return
        time.sleep(.05)
    raise TimeoutError(op)

wrong = "--wrong-once" in sys.argv
started = False
for iteration in range(300):
    command("state")
    state = json.loads((directory / "ui-state.json").read_text())
    if any(canvas["name"] == "ReportCanvas" and canvas["active"] for canvas in state["canvases"]):
        print("Reached the investigation report")
        break
    if not state["story"]:
        if started or iteration > 10:
            print("Dialogue sequence completed")
            break
        time.sleep(.1)
        continue
    started = True
    buttons = {b["name"]: b for b in state["buttons"]}
    if "Choice0" in buttons:
        if "--stop-at-quiz" in sys.argv:
            print("Reached quiz choices")
            break
        if wrong and buttons.get("Choice1", {}).get("interactable"):
            command("capture", "05-quiz-choices")
            command("click", "Choice1")
            command("capture", "05-wrong-feedback")
            wrong = False
        else:
            command("click", "Choice0")
    elif buttons.get("BG", {}).get("interactable"):
        command("click", "BG")
    else:
        time.sleep(.1)
else:
    raise RuntimeError("Dialogue did not complete within 300 steps")
