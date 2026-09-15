"""Edit EgoLite recordings into a Japanese guide. Keep every game frame uncropped."""
from pathlib import Path
import argparse, json, math, subprocess, wave
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[2]
WORK=ROOT/'Logs/manual-video'
OUT=ROOT/'output/video'
FONT=ROOT/'tmp/pdfs/testee-flow/NotoSansJP.ttf'
OUT.mkdir(exist_ok=True,parents=True)
(WORK/'render').mkdir(exist_ok=True)
W,H=1920,1080
GX,GY,GW,GH=160,72,1600,900
S=GW/1920
INK='#173e46'; TEAL='#177e76'; PAPER='#faf7ef'; ORANGE='#bd572b'

def font(size,weight=500):
    f=ImageFont.truetype(str(FONT),size)
    try:f.set_variation_by_axes([weight])
    except OSError:pass
    return f

def centered(draw,xy,text,f,fill):
    box=draw.textbbox((0,0),text,font=f)
    draw.text((xy[0]-(box[2]+box[0])/2,xy[1]-(box[3]+box[1])/2),text,font=f,fill=fill)

def overlay(item,index,total):
    im=Image.new('RGBA',(W,H),PAPER)
    d=ImageDraw.Draw(im)
    d.rectangle((GX,GY,GX+GW-1,GY+GH-1),fill=(0,0,0,0))
    d.rounded_rectangle((46,16,98,68),radius=26,fill=ORANGE)
    centered(d,(72,42),str(index),font(27,600),'white')
    d.text((116,16),item['title'],font=font(30,600),fill=INK)
    d.text((1455,23),'ジオクエスト / あそびかた',font=font(20),fill=TEAL)
    d.rounded_rectangle((GX-2,GY-2,GX+GW+1,GY+GH+1),radius=3,outline='#bdd4cf',width=2)
    lines=item['caption'].split('\n')
    for i,line in enumerate(lines):
        centered(d,(960,1018+(i-(len(lines)-1)/2)*37),line,font(32,600),INK)
    d.text((1808,1028),f'{index:02d}/{total:02d}',font=font(19),fill=TEAL)
    if item.get('note'):
        d.text((116,53),item['note'],font=font(13),fill=TEAL)
    name=WORK/'render'/f'{item["id"]}-layout.png';im.save(name)
    return name

def mark_image(mark,name):
    im=Image.new('RGBA',(W,H));d=ImageDraw.Draw(im)
    if mark['kind']=='ring':
        x,y=GX+mark['x']*S,GY+mark['y']*S;r=mark.get('r',66)*S
        d.ellipse((x-r,y-r,x+r,y+r),outline='#ffca64',width=7)
        d.ellipse((x-r-4,y-r-4,x+r+4,y+r+4),outline='#133840',width=2)
    elif mark['kind']=='box':
        x,y=GX+mark['x']*S,GY+mark['y']*S
        d.rounded_rectangle((x,y,x+mark['w']*S,y+mark['h']*S),radius=14,outline='#ffca64',width=6)
    elif mark['kind']=='arrow':
        x1,y1=GX+mark['x1']*S,GY+mark['y1']*S;x2,y2=GX+mark['x2']*S,GY+mark['y2']*S
        d.line((x1,y1,x2,y2),fill='#ffca64',width=8)
        a=math.atan2(y2-y1,x2-x1)
        d.polygon([(x2,y2),(x2-25*math.cos(a-.5),y2-25*math.sin(a-.5)),(x2-25*math.cos(a+.5),y2-25*math.sin(a+.5))],fill='#ffca64')
    if mark.get('label'):
        centered(d,(GX+mark.get('label_x',mark.get('x',960))*S,GY+mark.get('label_y',mark.get('y',540))*S),mark['label'],font(36,650),'#ffca64')
    im.save(name)

def audio_duration(path):
    with wave.open(str(path)) as f:return f.getnframes()/f.getframerate()

def shell(args):
    subprocess.run([str(x) for x in args],check=True)

def stamp(t):
    ms=round(t*1000);h,ms=divmod(ms,3600000);m,ms=divmod(ms,60000);s,ms=divmod(ms,1000)
    return f'{h:02d}:{m:02d}:{s:02d},{ms:03d}'

def main():
    p=argparse.ArgumentParser();p.add_argument('--only',nargs='*');p.add_argument('--assemble',action='store_true');a=p.parse_args()
    plan=json.loads((ROOT/'Docs/manual-video/edit.json').read_text())
    narration={x['id']:x['text'] for x in json.loads((ROOT/'Docs/manual-video/narration-ja.json').read_text())}
    timeline=[];start=0;srt=[];num=1
    for i,item in enumerate(plan,1):
        audio=WORK/'audio'/f'{item["id"]}.wav'
        ad=audio_duration(audio)
        pieces=item['pieces']; vd=sum(x['end']-x.get('start',0) for x in pieces)
        duration=round(max(ad+1.4,vd),3)
        dest=WORK/'render'/f'{item["id"]}.mp4'
        timeline.append({'id':item['id'],'start':start,'duration':duration,'file':str(dest)})
        sentences=[s+'。' for s in narration[item['id']].split('。') if s]
        textlen=sum(len(s) for s in sentences);t=start+.6
        for sentence in sentences:
            end=t+ad*len(sentence)/textlen
            srt.append(f'{num}\n{stamp(t)} --> {stamp(end)}\n{sentence}\n');num+=1;t=end
        start+=duration
        if a.assemble or (a.only and item['id'] not in a.only):continue
        inputs=[];filters=[]
        for j,piece in enumerate(pieces):
            src=ROOT/piece['file']
            if src.suffix=='.png':inputs+=['-loop','1','-framerate','30','-t',str(piece['end']-piece.get('start',0)),'-i',str(src)]
            else:inputs+=['-i',str(src)]
            if piece.get('pan'):
                # Scroll through a full-page capture at readable scale, preserving aspect ratio.
                resize=f"scale={GW}:-2:flags=lanczos,crop={GW}:{GH}:0:'min(ih-oh,max(0,(t-{piece.get('pan_start',2)})/{piece.get('pan_duration',6)}*(ih-oh)))'"
            else:
                resize=f'scale={GW}:{GH}:force_original_aspect_ratio=decrease:flags=lanczos,pad={GW}:{GH}:(ow-iw)/2:(oh-ih)/2:color=0x0b2028'
            filters.append(f'[{j}:v]trim=start={piece.get("start",0)}:end={piece["end"]},setpts=PTS-STARTPTS,{resize},setsar=1,fps=30[v{j}]')
        labels=''.join(f'[v{j}]' for j in range(len(pieces)))
        filters.append(labels+f'concat=n={len(pieces)}:v=1:a=0,tpad=stop_mode=clone:stop_duration={duration},trim=duration={duration},pad={W}:{H}:{GX}:{GY}:color=0xfaf7ef[base]')
        layer=overlay(item,i,len(plan));idx=len(pieces)
        inputs+=['-loop','1','-i',str(layer)]
        filters.append(f'[base][{idx}:v]overlay=0:0:shortest=1[decor]')
        current='decor';idx+=1
        for j,mark in enumerate(item.get('marks',[])):
            path=WORK/'render'/f'{item["id"]}-mark{j}.png';mark_image(mark,path)
            inputs+=['-loop','1','-i',str(path)]
            nxt=f'mark{j}'
            filters.append(f'[{current}][{idx}:v]overlay=0:0:enable=\'between(t,{mark.get("from",0)},{mark.get("to",duration)})\'[{nxt}]')
            current=nxt;idx+=1
        fade_in='fade=t=in:d=0.18,' if i>1 else ''
        filters.append(f'[{current}]{fade_in}fade=t=out:st={duration-.18}:d=0.18,format=yuv420p[vout]')
        inputs+=['-i',str(audio)]
        filters.append(f'[{idx}:a]loudnorm=I=-18:TP=-1.5:LRA=9,adelay=600|600,apad,atrim=duration={duration},aresample=48000[aout]')
        graph=WORK/'render'/f'{item["id"]}.filter';graph.write_text(';\n'.join(filters))
        print('RENDER',item['id'],duration,flush=True)
        shell(['ffmpeg','-y','-loglevel','error',*inputs,'-filter_complex',graph.read_text(),'-map','[vout]','-map','[aout]','-t',duration,'-r','30','-c:v','libx264','-preset','fast','-crf','18','-threads','4','-c:a','aac','-b:a','192k','-movflags','+faststart',dest])
    (WORK/'render/timeline.json').write_text(json.dumps(timeline,ensure_ascii=False,indent=2))
    (OUT/'ジオクエスト_操作ガイド_日本語.srt').write_text('\n'.join(srt))
    if a.assemble:
        concat=WORK/'render/concat.txt';concat.write_text(''.join("file '"+x['file'].replace("'","'\\''")+"'\n" for x in timeline))
        shell(['ffmpeg','-y','-loglevel','error','-f','concat','-safe','0','-i',concat,'-c','copy','-movflags','+faststart',OUT/'ジオクエスト_操作ガイド_日本語.mp4'])
        print('TOTAL',round(start,2),flush=True)

if __name__=='__main__':main()
