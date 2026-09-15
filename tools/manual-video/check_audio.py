from pathlib import Path
import json,sys
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Logs/manual-video/pydeps'))
import mlx_whisper, soundfile as sf, numpy as np
parts=[];offset=0;segments=[]
for p in sorted((ROOT/'Logs/manual-video/audio').glob('*.wav')):
    y,sr=sf.read(p);segments.append({'id':p.stem,'start':offset,'duration':len(y)/sr,'peak':float(np.max(np.abs(y)))})
    parts.extend([y,np.zeros(sr)]);offset+=len(y)/sr+1
out=ROOT/'Logs/manual-video/asr';out.mkdir(exist_ok=True)
sf.write(out/'all.wav',np.concatenate(parts),sr)
r=mlx_whisper.transcribe(str(out/'all.wav'),path_or_hf_repo='mlx-community/whisper-large-v3-turbo-q4',language='ja',verbose=False)
(out/'all.json').write_text(json.dumps({'audio':segments,'asr':r},ensure_ascii=False,indent=2))
print(r['text'],flush=True)
