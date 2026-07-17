#!/usr/bin/env python3
# Build an "original (top) vs revised (bottom)" contact sheet for review.
# Usage: python3 make_contact.py <id> [<id> ...]   (no args = all revised present)
# Output: /tmp/contact_<id>.png per id, plus prints which were built.
import sys, os
from PIL import Image, ImageDraw

SRC = "/Users/user/Unity/GeoModelTest/Assets/Resources/Story/Illustrations"
REV = "/Users/user/Unity/GeoModelTest/tools/art-pipeline/revised"

ids = sys.argv[1:]
if not ids:
    ids = sorted(f[:-4] for f in os.listdir(REV) if f.endswith(".png"))

def labeled(im, text):
    w, h = im.size
    bar = 34
    canvas = Image.new("RGB", (w, h + bar), (245, 245, 245))
    canvas.paste(im, (0, bar))
    d = ImageDraw.Draw(canvas)
    d.text((8, 8), text, fill=(20, 20, 20))
    return canvas

built = []
for i in ids:
    sp = os.path.join(SRC, f"{i}.png")
    rp = os.path.join(REV, f"{i}.png")
    if not (os.path.exists(sp) and os.path.exists(rp)):
        continue
    o = Image.open(sp).convert("RGB")
    r = Image.open(rp).convert("RGB")
    W = 760
    def fit(im):
        return im.resize((W, int(im.height * W / im.width)))
    o, r = labeled(fit(o), f"{i}  —  ORIGINAL"), labeled(fit(r), f"{i}  —  REVISED (codex)")
    H = o.height + r.height + 6
    c = Image.new("RGB", (W, H), (255, 255, 255))
    c.paste(o, (0, 0)); c.paste(r, (0, o.height + 6))
    out = f"/tmp/contact_{i}.png"
    c.save(out); built.append(out)

print("built:", *built, sep="\n  ")
