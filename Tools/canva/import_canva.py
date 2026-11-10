"""Imports Canva portraits into the game.
Put the downloaded PNGs here (transparent background versions):
    Tools/canva/ren.png
    Tools/canva/sirius.png
then run:  python Tools/canva/import_canva.py
Writes Art/portrait_ren.png and Art/portrait_sirius.png (head-to-thigh crop, 900 px tall).
"""
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.normpath(os.path.join(HERE, "..", "..", "Art"))
KEEP_TOP = 0.74   # dialogue portraits show the upper ~3/4 of the full-body art
HEIGHT = 900

for src, dst in (("ren.png", "portrait_ren.png"), ("sirius.png", "portrait_sirius.png")):
    p = os.path.join(HERE, src)
    if not os.path.exists(p):
        print("missing", p)
        continue
    im = Image.open(p).convert("RGBA")
    # If the background was not removed, knock out near-white pixels connected to the border.
    a = im.getchannel("A")
    if a.getextrema()[0] == 255:
        from PIL import ImageDraw
        mask = Image.new("L", im.size, 0)
        rgb = im.convert("RGB")
        ImageDraw.floodfill(rgb, (0, 0), (255, 0, 255), thresh=28)
        px = rgb.load()
        apx = im.load()
        for y in range(im.height):
            for x in range(im.width):
                if px[x, y] == (255, 0, 255):
                    apx[x, y] = (0, 0, 0, 0)
    box = im.getchannel("A").point(lambda v: 255 if v > 10 else 0).getbbox()
    im = im.crop(box)
    im = im.crop((0, 0, im.width, int(im.height * KEEP_TOP)))
    w = round(im.width * HEIGHT / im.height)
    im = im.resize((w, HEIGHT), Image.LANCZOS)
    # Transparent border so texture wrapping can't bleed the cut-off bottom edge onto the top.
    pad = 4
    out = Image.new("RGBA", (im.width + pad * 2, im.height + pad * 2), (0, 0, 0, 0))
    out.paste(im, (pad, pad))
    out.save(os.path.join(ART, dst))
    print("wrote", dst, out.size)


# ---------------------------------------------------------------------------------------
# In-game full-body sprites (the HUD animates them by warping horizontal strips)
#   ren_back.png -> Art/player_back.png   (player, seen from behind)
#   sirius.png   -> Art/boss_body.png     (boss, full body)
# ---------------------------------------------------------------------------------------
from PIL import ImageFilter

for src, dst, h in (("ren_back.png", "player_back.png", 200), ("sirius.png", "boss_body.png", 260)):
    p = os.path.join(HERE, src)
    if not os.path.exists(p):
        print("missing", p)
        continue
    im = Image.open(p).convert("RGBA")
    box = im.getchannel("A").point(lambda v: 255 if v > 10 else 0).getbbox()
    im = im.crop(box)
    w = round(im.width * h / im.height)
    im = im.resize((w, h), Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=1.2, percent=60, threshold=2))
    pad = 6
    out = Image.new("RGBA", (w + pad * 2, h + pad * 2), (0, 0, 0, 0))
    out.paste(im, (pad, pad))
    out.save(os.path.join(ART, dst))
    print("wrote", dst, out.size)
