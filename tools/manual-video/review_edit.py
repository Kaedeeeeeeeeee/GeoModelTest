from pathlib import Path
import json,subprocess
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];work=ROOT/'Logs/manual-video/render';data=json.loads((work/'timeline.json').read_text());files=[]
for x in data:
 for tag,t in [('mid',x['duration']*.5),('end',x['duration']-.6)]:
  p=work/(x['id']+'-'+tag+'.jpg');subprocess.run(['ffmpeg','-y','-loglevel','error','-ss',str(t),'-i',x['file'],'-frames:v','1','-vf','scale=640:360',str(p)],check=True);files.append(p)
im=Image.new('RGB',(1920,394*((len(files)+2)//3)),'#faf7ef');d=ImageDraw.Draw(im)
for i,p in enumerate(files):x=i%3*640;y=i//3*394;im.paste(Image.open(p),(x,y));d.text((x+8,y+365),p.stem,fill='black')
im.save(work/'contact.jpg')
