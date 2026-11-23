#!/usr/bin/env python3
"""Export every spoken story line on the study route to voice_lines.json for the Colab TTS notebook.

Each row: key (= textKey, also the mp3 file name), speaker (kaede / player / narration), display text,
`read`: the text TTS speaks (furigana removed, geology terms and letters spelled in kana, stage
directions such as （無線） removed) and `read_kana`: an all-kana fallback for lines the model misreads. Question choices are not voiced; prompts, hints,
feedback and card endings are.
"""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / 'Assets/Resources'
ROUTE = ['quest1.1', 'quest1.2', 'quest2.1', 'quest3.2', 'beat2', 'quest3.4', 'quest4.1',
         'quest4.2', 'core-return', 'beat3', 'beat4']
SPEAKERS = {'story.speaker.drkaede': 'kaede', 'story.speaker.protagonist': 'player',
            'story.speaker.narration': 'narration'}
RUBY = re.compile(r'([\u4e00-\u9fff々〆ヵヶ]+)（([ぁ-ゖァ-ヺー]+)）')


# Geology terms TTS may misread: speak their kana. Everything else keeps its kanji, which gives the
# model better word boundaries and intonation than all-kana text.
KANA_TERMS = {'岩芯', '鍵層', '示相化石', '示準化石', '凝灰岩', '泥岩', '砂岩', '石灰岩', '柱状図', '露頭',
              '堆積', '侵食', '運搬', '風化', '三葉虫', '古生代', '中生代', '新生代', '余震'}
REPLACE = [('G-Lab', 'ジーラボ'), ('0.06–2mm', '0.06ミリから2ミリ'), ('mm', 'ミリ'),
           ('A・B・C', 'エー、ビー、シー'), ('Dr.Kaede', 'ドクター・カエデ')]


def reading(text: str, kana_only: bool = False) -> str:
    text = re.sub(r'^（無線（むせん））|^（無線）', '', text)
    text = RUBY.sub(lambda m: m.group(2) if kana_only or m.group(1) in KANA_TERMS else m.group(1), text)
    for old, new in REPLACE:
        text = text.replace(old, new)
    return text.replace('——', '……').replace('—', '、').strip()


def main() -> None:
    ja = {row['key']: row['value'] for row in
          json.loads((ROOT / 'Localization/Data/ja-JP.json').read_text())['texts']}
    rows, seen = [], set()

    def add(key, speaker):
        if not key or key in seen or key not in ja:
            return
        seen.add(key)
        rows.append({'key': key, 'speaker': speaker, 'text': ja[key], 'read': reading(ja[key]),
                     'read_kana': reading(ja[key], True)})

    for name in ROUTE:
        for line in json.loads((ROOT / f'Story/{name}.json').read_text())['dialogues']:
            speaker = SPEAKERS.get(line.get('speakerKey'), 'narration')
            add(line.get('textKey'), speaker)
            add(line.get('hintKey'), speaker)
            for choice in line.get('choices') or []:
                add(choice.get('feedbackKey'), speaker)
            card = line.get('interaction') or {}
            for step in card.get('steps') or []:  # slides: Kaede speaks each step
                add(step.get('textKey'), speaker)
            add(card.get('doneKey'), speaker)
    out = Path(__file__).with_name('voice_lines.json')
    out.write_text(json.dumps(rows, ensure_ascii=False, indent=1) + '\n')
    counts = {s: sum(r['speaker'] == s for r in rows) for s in ('kaede', 'player', 'narration')}
    print(f'{len(rows)} lines → {out.name}  {counts}  {sum(len(r["read"]) for r in rows)} characters')


if __name__ == '__main__':
    main()
