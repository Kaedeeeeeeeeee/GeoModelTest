"""Local Japanese narration with the owner's existing speaker profile."""
import argparse
import json
from pathlib import Path
import sys

parser = argparse.ArgumentParser()
parser.add_argument('manifest')
parser.add_argument('--only', nargs='*')
parser.add_argument('--force', action='store_true')
parser.add_argument('--runtime', type=Path, required=True, help='Local Dots TTS runtime directory')
parser.add_argument('--profile', type=Path, required=True, help='Authorized local speaker profile')
parser.add_argument('--weights', type=Path, required=True, help='Local model weights directory')
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
runtime = args.runtime.expanduser().resolve()
sys.path.insert(0, str(runtime))
sys.path.insert(0, str(root / 'Logs/manual-video/pydeps'))
import mlx.core as mx
import numpy as np
import soundfile as sf
from dots_tts_mlx.loader import from_pretrained
from dots_tts_mlx.profile import SpeakerProfile

out = root / 'Logs/manual-video/audio'
out.mkdir(parents=True, exist_ok=True)
profile = SpeakerProfile.load(str(args.profile.expanduser().resolve()))
model = from_pretrained(str(args.weights.expanduser().resolve()), dtype=mx.bfloat16).model
for n, item in enumerate(json.loads(Path(args.manifest).read_text())):
    if args.only and item['id'] not in args.only: continue
    target = out / (item['id'] + '.wav')
    if target.exists() and not args.force: continue
    print('GENERATING', item['id'], item['text'], flush=True)
    result = model.generate(text=item.get('tts_text',item['text']), profile=profile, language='JA',
                            num_steps=4, guidance_scale=1.2, seed=20260912+n,
                            trim_onset=False, max_generate_length=600)
    audio = np.asarray(result['audio'].astype(mx.float32)).reshape(-1)
    sf.write(str(target), audio, int(result['sample_rate']), subtype='PCM_16')
    print('DONE', item['id'], round(len(audio)/int(result['sample_rate']), 2), flush=True)
