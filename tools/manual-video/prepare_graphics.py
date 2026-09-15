from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
E='Logs/manual-video/ego/';A='Docs/manual-ja/assets/'
def clip(name,start,end,**kw):return dict(file=E+name+'.webm',start=start,end=end,**kw)
def pic(name,secs,**kw):return dict(file=A+name+'.png',start=0,end=secs,**kw)
def ring(x,y,r=72,**kw):return dict(kind='ring',x=x,y=y,r=r,**kw)
def box(x,y,w,h,**kw):return dict(kind='box',x=x,y=y,w=w,h=h,**kw)
def item(id,title,caption,pieces,marks=[],**kw):return dict(id=id,title=title,caption=caption,pieces=pieces,marks=marks,**kw)
# Intro orientation card, using the actual title-screen capture inside a phone outline.
im=Image.new('RGB',(1920,1080),'#faf7ef');d=ImageDraw.Draw(im)
f=ImageFont.truetype(str(ROOT/'tmp/pdfs/testee-flow/NotoSansJP.ttf'),48)
d.rounded_rectangle((230,130,1690,950),radius=80,fill='#173e46')
photo=Image.open(ROOT/(E+'49-title.png')).convert('RGB').resize((1328,747),Image.Resampling.LANCZOS);im.paste(photo,(296,166))
d.rounded_rectangle((256,433,269,617),radius=6,fill='#8eb6b1')
d.text((750,34),'横向きであそぼう',font=f,fill='#177e76')
im.save(ROOT/(E+'ready-card.png'))
