"""Check speaker attribution through the actual coral-question buttons in a playing editor.

Uses test progress: back up PlayerPrefs and persistent files before running.
Requires the Laboratory Scene and the editor-only RemediationReview bridge.
"""
import json
import plistlib
import subprocess
import time
from pathlib import Path

from review_api import command, state

root = Path(__file__).resolve().parents[2]
checks = []


def reach_question():
    for _ in range(60):
        current = state()
        buttons = {b['name']: b for b in current['buttons']}
        if 'Choice0' in buttons:
            return
        if buttons.get('BG', {}).get('interactable'):
            command('click', 'BG')
        else:
            time.sleep(.2)
    raise AssertionError('Did not reach the coral question')


def verify_speaker(expected, label):
    current = state()
    speakers = [t['text'] for t in current['text'] if t['name'] == 'Speaker']
    assert speakers == [expected], (label, speakers)
    checks.append(label)


for language, code in [('Japanese', 'ja-JP'), ('English', 'en-US'), ('ChineseSimplified', 'zh-CN')]:
    values = {v['key']: v['value'] for v in json.loads(
        (root / 'Assets/Resources/Localization/Data' / (code + '.json')).read_text())['texts']}
    expected = values['story.speaker.drkaede']
    command('cancel_story')
    command('reset')
    command('skip_intro')
    command('language', language)
    command('story', 'Story/beat3')
    reach_question()
    verify_speaker(expected, code + ': question has its author')
    command('click', 'HintBtn')
    verify_speaker(expected, code + ': hint retains its author')
    if code == 'ja-JP':
        command('capture', '05-hint-speaker')
    reach_question()
    command('click', 'Choice1')
    verify_speaker(expected, code + ': wrong-answer explanation retains its author')
    if code == 'ja-JP':
        command('capture', '05-wrong-feedback-speaker')
    reach_question()
    command('click', 'Choice0')
    verify_speaker(expected, code + ': correct-answer explanation retains its author')
    if code == 'ja-JP':
        command('capture', '05-correct-feedback-speaker')
        command('history')
        command('capture', '11a-history-speaker')
        command('close_history')
    prefs = plistlib.loads(subprocess.check_output(
        ['defaults', 'export', 'unity.ZHANGSHIFENG.GeoModelTest', '-']))
    entries = json.loads(prefs['StorySystem.History.v1'])['entries']
    explanations = [e for e in entries if e['kind'] in ('hint', 'feedback')]
    assert len(explanations) == 3 and all(e['speaker'] == expected for e in explanations)
    checks.append(code + ': saved hint and both explanations retain their author')

command('cancel_story')
(root / 'Logs/remediation/speaker-fix/results.json').write_text(
    json.dumps({'passed': True, 'checks': checks}, ensure_ascii=False, indent=2))
print(f'{len(checks)} actual dialogue speaker checks passed', flush=True)
