"""Build the student-facing illustrated Japanese manual from verified UI captures."""
from pathlib import Path
import argparse
import math
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
WORK = ROOT / 'tmp/pdfs/manual-ja'
OUT = ROOT / 'output/pdf/ジオクエスト_あそびかたガイド_日本語.pdf'
parser = argparse.ArgumentParser()
parser.add_argument('--font-source', type=Path, default=ROOT / 'tmp/pdfs/testee-flow/NotoSansJP.ttf')
parser.add_argument('--fonttools-deps', type=Path, default=ROOT / 'tmp/pdfs/testee-flow/deps')
args = parser.parse_args()
sys.path.insert(0, str(args.fonttools_deps))

from PIL import Image
from fontTools.ttLib import TTFont as FTFont
from fontTools import subset
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools.pens.boundsPen import BoundsPen
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4, landscape
from reportlab.lib.colors import HexColor
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import Paragraph
from reportlab.lib.utils import ImageReader

WORK.mkdir(parents=True, exist_ok=True)
OUT.parent.mkdir(parents=True, exist_ok=True)
font = FTFont(args.font_source)
sub = subset.Subsetter()
sub.populate(text=Path(__file__).read_text(encoding='utf-8'))
sub.subset(font)
font.save(WORK / 'subset.ttf')
for name, weight in [('JP', 400), ('JPBold', 700)]:
    f = instantiateVariableFont(FTFont(WORK / 'subset.ttf'), {'wght': weight}, inplace=True)
    f.save(WORK / (name + '.ttf'))
    pdfmetrics.registerFont(TTFont(name, str(WORK / (name + '.ttf'))))

# Center the visible digit shapes instead of the font's paragraph line box.
number_font = FTFont(WORK / 'JPBold.ttf')
number_glyphs = number_font.getGlyphSet()
NUMBER_UNITS = number_font['head'].unitsPerEm
NUMBER_METRICS = {}
for digit in '0123456789':
    glyph_name = number_font.getBestCmap()[ord(digit)]
    pen = BoundsPen(number_glyphs)
    number_glyphs[glyph_name].draw(pen)
    NUMBER_METRICS[digit] = (pen.bounds, number_font['hmtx'][glyph_name][0])

W, H = landscape(A4)
INK = HexColor('#163944')
MUTED = HexColor('#536B70')
TEAL = HexColor('#16796F')
ORANGE = HexColor('#B95728')
CREAM = HexColor('#FCF8EF')
PALE = HexColor('#EAF3EF')
WHITE = HexColor('#FFFFFF')
LINE = HexColor('#D4E1DA')
GOLD = HexColor('#F0BC57')
TOTAL = 12
c = canvas.Canvas(str(OUT), pagesize=(W, H), pageCompression=1)
c.setTitle('ジオクエスト | あそびかたガイド')
c.setAuthor('ジオクエスト 研究チーム')
c.setSubject('中学生向け・参加コードからゲーム操作、アンケートの送信まで')
TEXT_LOG = []


def rect(x, y, w, h, color=WHITE, radius=0, stroke=None):
    c.setFillColor(color)
    c.setStrokeColor(stroke or color)
    c.setLineWidth(1)
    if radius:
        c.roundRect(x, H-y-h, w, h, radius, fill=1, stroke=bool(stroke))
    else:
        c.rect(x, H-y-h, w, h, fill=1, stroke=bool(stroke))


def txt(s, x, y, w, size=14, color=INK, bold=False, height=80, align=0, leading=None):
    p = Paragraph(escape(s).replace('\n', '<br/>'), ParagraphStyle(
        'label', fontName='JPBold' if bold else 'JP', fontSize=size,
        leading=leading or size*1.5, textColor=color, wordWrap='CJK', alignment=align))
    _, ph = p.wrap(w, height)
    if ph > height + .1 or y + ph > H - 12:
        raise ValueError(f'Overflow: {s}: {ph}, {x}, {y}')
    p.drawOn(c, x, H-y-ph)
    TEXT_LOG.append(s)
    return ph


def ln(x1, y1, x2, y2, color=TEAL, width=2):
    c.setStrokeColor(color)
    c.setLineWidth(width)
    c.line(x1, H-y1, x2, H-y2)


def arrow(x1, y1, x2, y2, color=ORANGE, width=2.2):
    ln(x1, y1, x2, y2, color, width)
    a = math.atan2(y2-y1, x2-x1)
    for delta in [-.55, .55]:
        ln(x2, y2, x2-9*math.cos(a+delta), y2-9*math.sin(a+delta), color, width)


def circle(x, y, r, color=ORANGE, fill=True, width=2):
    c.setFillColor(color)
    c.setStrokeColor(color)
    c.setLineWidth(width)
    c.circle(x, H-y, r, stroke=1, fill=fill)


def num(n, x, y, r=14):
    circle(x, y, r)
    label = str(n)
    boxes, advance = [], 0
    for digit in label:
        (left, bottom, right, top), width = NUMBER_METRICS[digit]
        boxes.append((left+advance, bottom, right+advance, top))
        advance += width
    left = min(b[0] for b in boxes)
    bottom = min(b[1] for b in boxes)
    right = max(b[2] for b in boxes)
    top = max(b[3] for b in boxes)
    scale = 14 / NUMBER_UNITS
    c.setFillColor(WHITE)
    c.setFont('JPBold', 14)
    c.drawString(x-(left+right)*scale/2, H-y-(bottom+top)*scale/2, label)
    TEXT_LOG.append(label)


def badge(s, x, y, w, color=TEAL):
    rect(x, y, w, 27, color, 13)
    txt(s, x+6, y+4, w-12, 11, WHITE, True, height=19, align=1)


def photo(name, x, y, w, crop=None, border=True):
    im = Image.open(HERE / 'assets' / name)
    if crop:
        im = im.crop(crop)
    h = w * im.height / im.width
    c.drawImage(ImageReader(im), x, H-y-h, width=w, height=h, mask='auto')
    if border:
        c.setStrokeColor(LINE)
        c.setLineWidth(.65)
        c.rect(x, H-y-h, w, h, fill=0, stroke=1)
    return h


def highlight(x, y, w, h):
    c.setStrokeColor(GOLD)
    c.setLineWidth(3)
    c.roundRect(x, H-y-h, w, h, 6, fill=0, stroke=1)


def panel(x, y, w, h, color=WHITE):
    rect(x, y, w, h, color, 12, LINE)


def step(n, title, x, y, w=300, detail=None):
    num(n, x+14, y+14)
    txt(title, x+39, y+1, w-39, 17, bold=True, height=55)
    if detail:
        txt(detail, x+39, y+35, w-39, 12, MUTED, height=50)


def frame(page, section, title, subtitle):
    rect(0, 0, W, H, CREAM)
    rect(0, 0, 10, H, TEAL)
    txt('ジオクエスト  /  あそびかたガイド', 32, 19, 500, 10, TEAL, True, height=17)
    badge(section, W-175, 16, 143)
    txt(title, 32, 47, W-64, 29, bold=True, height=47)
    txt(subtitle, 32, 96, W-64, 13, MUTED, height=24)
    ln(32, 558, W-32, 558, LINE, .8)
    txt('画面は操作の例です。表示や配置が少し変わることがあります。', 32, 565, 640, 8.5, MUTED, height=16)
    txt(f'{page:02d} / {TOTAL:02d}', W-95, 563, 63, 10, TEAL, True, height=18, align=2)


def end():
    c.showPage()


def key(s, x, y, w=50, h=45):
    rect(x, y+3, w, h, LINE, 7)
    rect(x, y, w, h, WHITE, 7, LINE)
    txt(s, x+4, y+(h-25)/2, w-8, 17, TEAL, True, height=28, align=1)


def rock(x, y, scale=1):
    pts = [(0, 42), (20, 8), (62, 0), (92, 30), (80, 73), (20, 79)]
    p = c.beginPath()
    p.moveTo(x, H-y-42*scale)
    for dx, dy in pts[1:]:
        p.lineTo(x+dx*scale, H-y-dy*scale)
    p.close()
    c.setFillColor(HexColor('#9BA9A5'))
    c.setStrokeColor(INK)
    c.setLineWidth(1.7)
    c.drawPath(p, fill=1, stroke=1)
    ln(x+20*scale, y+8*scale, x+38*scale, y+41*scale, INK, 1.2)
    ln(x+38*scale, y+41*scale, x+80*scale, y+73*scale, INK, 1.2)
    ln(x+38*scale, y+41*scale, x+92*scale, y+30*scale, INK, 1.2)


def tower(x, y, scale=1):
    ln(x, y+115*scale, x+25*scale, y, TEAL, 5*scale)
    ln(x+50*scale, y+115*scale, x+25*scale, y, TEAL, 5*scale)
    for a, b in [(25, 41), (49, 65), (73, 89)]:
        ln(x+(25-a/5)*scale, y+a*scale, x+(25+b/5)*scale, y+b*scale, TEAL, 2*scale)
    ln(x+25*scale, y+9*scale, x+25*scale, y+133*scale, INK, 3*scale)
    rect(x-10*scale, y+115*scale, 70*scale, 10*scale, TEAL, 2)


# 01: Preserve both complete screens so the modal can be located on the title page.
frame(1, 'はじめる', 'じぶんのコードで、はじめよう', '案内されたゲームのリンクを開きます。参加コードを手元に用意しましょう。')
panel(32, 136, 381, 407)
step(1, '「研究に参加」を押す', 48, 153, 349)
photo('title.png', 48, 214, 349)
sx = 349/1366
highlight(48+86*sx, 214+561*sx, 448*sx, 62*sx)
txt('はじめの画面の、左下にあります。', 48, 442, 349, 16, bold=True, height=30)
txt('参加コードは、案内でもらったものを使おう。', 48, 492, 349, 12, MUTED, height=24)
panel(429, 136, 381, 407)
step(2, 'もらったコードを入力', 445, 153, 349)
photo('code.png', 445, 214, 349)
highlight(445+435*sx, 214+351*sx, 496*sx, 58*sx)
highlight(445+431*sx, 214+521*sx, 240*sx, 64*sx)
step(3, '「コードを確認」を押す', 445, 436, 349)
txt('確認できると、ゲームが始まります。\nうまくいかないときは、コードをもう一度確認。', 445, 484, 349, 12, MUTED, height=45)
end()

# 02: Touchable controls with numbers next to the actual buttons.
frame(2, 'スマホ・タブレット', '指で動かしてみよう', 'スマホ・タブレットは横向きで。ボタンのない場所をなぞると、まわりを見られます。')
px, py, pw = 118, 136, 606
photo('mobile-play.png', px, py, pw)
scale = pw/694
for n, tx, ty, ox, oy in [(1,69,330,-49,-27),(2,440,225,-34,-31),(3,156,130,0,-42),(4,648,285,43,-14),(5,589,314,-40,8)]:
    xx, yy = px+tx*scale, py+ty*scale
    if n != 2:
        circle(xx, yy, 23*scale, GOLD, False, 2.5)
    num(n, xx+ox, yy+oy, 12)
    arrow(xx+ox*.6, yy+oy*.6, xx, yy)
arrow(px+389*scale, py+232*scale, px+478*scale, py+232*scale, GOLD, 3)
for i, (title, desc) in enumerate([
    ('移動する', '左の丸を動かす'),
    ('まわりを見る', '空いている所をなぞる'),
    ('道具を選ぶ', '「道具」を押す'),
    ('調べる', 'たたく・置く・拾う'),
    ('使う', 'ドリルでほる')]):
    x = 32+i*157
    panel(x, 483, 149, 63)
    num(i+1, x+19, 503, 10)
    txt(title, x+35, 490, 108, 13, bold=True, height=25)
    txt(desc, x+10, 520, 132, 10, MUTED, height=22, align=1)
end()

# 03: Desktop input is different from mobile primary/secondary controls.
frame(3, 'パソコン', 'キーボードとマウスで動こう', 'パソコンで遊ぶ人は、このページを見てください。')
panel(32, 137, 367, 243)
txt('移動する', 52, 150, 300, 18, bold=True)
key('W', 184, 213)
key('A', 128, 263)
key('S', 184, 263)
key('D', 240, 263)
txt('前', 196, 184, 27, 13, TEAL, align=1)
txt('左', 91, 274, 28, 13, TEAL)
txt('右', 302, 274, 28, 13, TEAL)
txt('後ろ', 185, 324, 70, 13, TEAL)
panel(415, 137, 395, 243)
txt('まわりを見る / 道具を使う', 435, 150, 355, 18, bold=True)
rect(451, 218, 76, 116, WHITE, 35, TEAL)
rect(456, 225, 31, 42, PALE, 12)
ln(489, 218, 489, 276, TEAL, 1.2)
ln(451, 276, 527, 276, TEAL, 1.2)
rect(485, 237, 8, 17, TEAL, 4)
arrow(439, 285, 419+5, 285, TEAL)
arrow(539, 285, 553, 285, TEAL)
txt('マウスを動かす', 564, 218, 220, 16, bold=True)
txt('まわりを見る', 564, 248, 220, 12, MUTED)
txt('左クリック', 564, 290, 220, 16, bold=True)
txt('ハンマーでたたく・道具を置く', 564, 320, 220, 12, MUTED)
for i, (k, title, detail) in enumerate([
    ('Tab', '道具を選ぶ', '開いたら、道具をクリック'),
    ('E', '話す・拾う', '近づいて、相手や石を見る'),
    ('F', '場所を移る・ほる', '使いたい道具を選んでから')]):
    x = 32+i*264
    panel(x, 397, 250, 91)
    key(k, x+13, 413, 55, 40)
    txt(title, x+79, 410, 159, 14, bold=True)
    txt(detail, x+12, 459, 226, 10.5, MUTED, height=20)
rect(32, 505, 778, 38, PALE, 8)
txt('走る：Shift　/　ジャンプ：Space　/　メニュー：Esc', 48, 513, 746, 12, TEAL, height=22)
end()

# 04: Entire scene in both screenshots; annotations stay at their real UI locations.
frame(4, '会話・クイズ', '画面の案内を見ながら進もう', '話を読んで、調べて、クイズに答えていきます。')
panel(32, 136, 381, 407)
step(1, '画面の下の、話を読む', 48, 151, 349)
photo('dialogue.png', 48, 212, 349)
sx = 349/1366
highlight(48+109*sx, 212+505*sx, 1148*sx, 236*sx)
highlight(48+17*sx, 212+17*sx, 461*sx, 120*sx)
txt('下の大きな枠を押すと、話が進みます。', 48, 433, 349, 15, bold=True, height=46)
txt('迷ったら、左上の「次にすること」を見よう。', 48, 493, 349, 12, MUTED, height=25)
panel(429, 136, 381, 407)
step(2, '画面の上で、答えを選ぶ', 445, 151, 349)
photo('quiz.png', 445, 212, 349)
sx = 349/694
highlight(445+104*sx, 212+104*sx, 486*sx, 90*sx)
txt('自分で考えて、答えを１つ押そう。', 445, 433, 349, 15, bold=True, height=46)
txt('スマホ：タップ　/　パソコン：クリック', 445, 493, 349, 12, MUTED, height=25)
end()

# 05: Keep the whole tool wheel and the whole scene behind the travel dialog.
frame(5, '場所を移る', 'ドアの道具で、場所を変えよう', '研究室と外の調査エリアを行き来するときに使います。')
panel(32, 136, 381, 407)
step(1, '「道具」を開く', 48, 151, 349, 'パソコン：Tab')
photo('mobile-wheel.png', 48, 231, 349)
sx = 349/694
circle(48+347*sx, 231+80*sx, 23, GOLD, False, 3)
arrow(48+256*sx, 231+80*sx, 48+307*sx, 231+80*sx)
txt('上のドアの絵を押します。', 48, 454, 349, 16, bold=True, height=29)
txt('道具の名前は「フェーズシフター」。', 48, 496, 349, 12, MUTED, height=24)
panel(429, 136, 381, 407)
step(2, '「調べる」を押す', 445, 151, 349, 'パソコン：左クリック / F')
photo('travel.png', 445, 231, 349)
highlight(445+201*sx, 231+94*sx, 292*sx, 210*sx)
txt('まん中の画面で、行き先を選ぼう。', 445, 454, 349, 16, bold=True, height=29)
txt('外：野外調査エリア　/　研究室：G-Lab研究室', 445, 496, 349, 11.5, MUTED, height=24)
end()

# 06: The full tool wheel locates the hammer; the diagram explains the actions.
frame(6, '石を集める', 'ハンマーで、石をとろう', '外で石を集めるように案内されたら、「地質ハンマー」を選びます。')
panel(32, 136, 472, 407)
step(1, '「地質ハンマー」を選ぶ', 48, 151, 440)
photo('mobile-wheel.png', 48, 215, 440)
sx = 440/694
circle(48+427*sx, 215+274*sx, 28, GOLD, False, 3)
arrow(48+514*sx, 215+274*sx, 48+476*sx, 215+274*sx)
txt('「道具」を開く → 右下のハンマーを押す', 48, 490, 440, 13, TEAL, True, height=26)
panel(520, 136, 290, 407)
step(2, '同じ所を３回たたく', 536, 151, 258)
rock(555, 222, .72)
circle(582, 247, 10, ORANGE, False)
txt('１ → ２ → ３', 639, 235, 145, 17, TEAL, True, height=31)
txt('スマホ：「調べる」\nパソコン：左クリック', 540, 307, 254, 13, height=43)
ln(536, 365, 794, 365, LINE, 1)
step(3, '出てきた石を拾う', 536, 382, 258)
txt('石に近づいて、石を見ます。', 540, 426, 254, 12, MUTED, height=23)
txt('スマホ：「調べる」\nパソコン：E', 540, 465, 254, 13, height=43)
txt('右の石の絵は、操作のイメージです。', 528, 525, 274, 8.5, MUTED, height=15, align=2)
end()

# 07: Give the drill selection its own full-scene page.
frame(7, 'ドリルを選ぶ', '「ドリルタワー」は、この道具', '地面の下を調べるように案内されたら、道具を切り替えます。')
photo('mobile-wheel.png', 32, 153, 558)
sx = 558/694
circle(32+460*sx, 153+192*sx, 32, GOLD, False, 3)
arrow(588, 153+192*sx, 32+508*sx, 153+192*sx)
panel(610, 153, 200, 314)
step(1, '道具を開く', 624, 174, 171)
txt('スマホ：「道具」\nパソコン：Tab', 628, 223, 165, 13, height=44)
step(2, '右の絵を押す', 624, 300, 171)
txt('逆三角形のような絵が\n「ドリルタワー」です。', 628, 349, 164, 13, height=64)
rect(32, 491, 778, 52, PALE, 9)
txt('道具を選んだら、次のページの「置く → ほる → 拾う」へ進もう。', 48, 506, 746, 15, TEAL, True, height=29)
end()

# 08: Separate drilling sequence avoids squeezing the scene and diagrams together.
frame(8, 'ドリルを使う', '置く → ほる → 拾う', '「ドリルタワー」を選んだら、この順番で操作します。')
for i, title in enumerate(['地面に置く', '近くで、ほる', '石を拾う']):
    x = 32+i*264
    panel(x, 137, 250, 342)
    num(i+1, x+24, 167)
    txt(title, x+57, 153, 180, 18, bold=True, align=1)
    ln(x+17, 357, x+233, 357, LINE, 2)
    if i == 0:
        c.saveState()
        c.setFillAlpha(.45)
        c.setStrokeAlpha(.45)
        tower(x+102, 216, .9)
        c.restoreState()
        txt('近くの地面を見る。\n半透明の形が出たら…', x+12, 371, 226, 13, height=44, align=1)
        txt('スマホ：「調べる」\nパソコン：左クリック', x+12, 425, 226, 12, TEAL, True, height=41, align=1)
    elif i == 1:
        tower(x+102, 216, .9)
        arrow(x+170, 242, x+170, 320)
        txt('ドリルの近くで押し、\n少し待つ。', x+12, 371, 226, 13, height=44, align=1)
        txt('スマホ：「使う」\nパソコン：F', x+12, 425, 226, 12, TEAL, True, height=41, align=1)
    else:
        rect(x+106, 237, 44, 83, HexColor('#B7A188'), 9, INK)
        for yy in [256, 279, 302]:
            ln(x+107, yy, x+149, yy, WHITE, 2)
        arrow(x+61, 278, x+96, 278)
        txt('まわりや少し上を見て、\n出てきた石を見つける。', x+12, 371, 226, 13, height=44, align=1)
        txt('スマホ：「調べる」\nパソコン：E', x+12, 425, 226, 12, TEAL, True, height=41, align=1)
txt('図は操作のイメージです。', 602, 484, 204, 8.5, MUTED, align=2, height=17)
rect(32, 505, 778, 39, PALE, 8)
txt('石を拾ったら、案内に合わせて「G-Lab研究室」へ戻ろう。場所の変え方は５ページ。', 48, 515, 746, 12, TEAL, True, height=24)
end()

# 09: Full report, including surrounding scene and the actual button location.
frame(9, 'ゲームのあと', '結果の画面から、アンケートへ', '「調査が完了しました！」と出たら、ゲームのあとに感想を教えてください。')
photo('report.png', 32, 151, 600)
sx = 600/1366
highlight(32+710*sx, 151+609*sx, 361*sx, 59*sx)
arrow(628, 151+638*sx, 32+1093*sx, 151+638*sx)
panel(648, 151, 162, 337)
num(1, 674, 181)
txt('右下のボタン', 664, 219, 130, 17, bold=True, height=53)
txt('「アンケートに\n答える」を\n押します。', 664, 292, 130, 16, height=80)
txt('画面の点数は\n表示の例です。', 664, 431, 130, 11, MUTED, height=40)
rect(32, 505, 778, 38, PALE, 8)
txt('ボタンを押すと、アンケートのページが開きます。', 48, 514, 746, 14, TEAL, True, height=26)
end()

# 10: The complete long survey page shows the relation between answers and Next.
frame(10, 'アンケート', '答えを選んで、下の「次へ」へ', '正解・不正解はありません。感じたことを、そのまま選ぼう。')
photo('survey.png', 46, 132, 382)
sx = 382/1280
highlight(46+232*sx, 132+615*sx, 816*sx, 204*sx)
highlight(46+822*sx, 132+1179*sx, 220*sx, 56*sx)
for n, title, detail, y in [
    (1, '質問ごとに、答えを１つ選ぶ', '「答えたくない」も選べます。', 141),
    (2, '下へスクロールする', '下にある質問にも答えていこう。', 277),
    (3, '右下の「次へ」を押す', '次のページの質問に進みます。', 413)]:
    panel(456, y, 354, 112)
    step(n, title, 470, y+14, 326)
    txt(detail, 474, y+69, 316, 13, MUTED, height=27)
arrow(434, 132+1207*sx, 46+1060*sx, 132+1207*sx)
txt('左は、ページを上から下まで写した例です。', 465, 535, 337, 9, MUTED, height=17)
end()

# 11: Wide viewport-sized lower portion preserves the field, both buttons and footer.
# The upper optional answer contains QA text, so it is not included in the manual.
frame(11, 'アンケートの送信', '最後のページで、回答を送ろう', '最後まで進んだら、ページの下へスクロールします。')
panel(32, 136, 526, 407)
txt('最後のページの、下の方', 48, 151, 494, 17, bold=True, height=30)
photo('survey-last.png', 48, 212, 494, (0, 838, 1280, 1414))
sx = 494/1280
highlight(48+822*sx, 212+(1214-838)*sx, 220*sx, 54*sx)
txt('書く欄の下、右側にあります。', 48, 464, 494, 13, MUTED, height=26)
panel(574, 136, 236, 407)
num(1, 601, 167)
txt('「回答を送信する」\nを押す', 590, 209, 204, 18, bold=True, height=63)
txt('自由に書く欄は、\n空欄でも大丈夫。', 590, 306, 204, 14, MUTED, height=50)
txt('名前や学校名は\n書かないでください。', 590, 391, 204, 14, MUTED, height=50)
txt('次に、完了の画面を確認！', 590, 489, 204, 12, TEAL, True, height=28)
end()

# 12: Full completion screen, including header and footer, not just the message box.
frame(12, 'さいご', '「回答を受け取りました」で、おしまい', '送信したあと、この画面が出たことを確認します。')
photo('survey-done.png', 99, 140, 186)
sx = 186/390
highlight(99+28*sx, 140+235*sx, 334*sx, 52*sx)
arrow(337, 140+261*sx, 99+379*sx, 140+261*sx)
panel(370, 140, 440, 187)
num(1, 399, 170)
txt('このメッセージが出たら、完了！', 390, 211, 400, 19, bold=True, height=60)
txt('ここまでできたら、ページを閉じてOK。', 390, 282, 400, 14, TEAL, True, height=28)
rect(370, 348, 440, 195, PALE, 11)
txt('うまく進まないとき', 390, 367, 400, 17, TEAL, True, height=31)
txt('ゲーム：左上の案内を確認しよう。\n\n送信エラー：ページを閉じずに、通信を確認して再送信。\n\n困ったままなら、参加を案内した人に相談しよう。', 390, 414, 400, 12, MUTED, height=118)
end()

c.save()
(WORK / 'manual-text.txt').write_text('\n'.join(TEXT_LOG), encoding='utf-8')
print(OUT)
