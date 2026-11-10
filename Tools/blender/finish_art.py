"""Turns Blender renders (Tools/blender/renders) into game-ready PNGs in Art/.
Run with normal Python after build_chars.py:  python Tools/blender/finish_art.py"""
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "renders")
ART = os.path.normpath(os.path.join(HERE, "..", "..", "Art"))

# raw render -> (Art file, target height in px)
JOBS = {
    "ren_portrait_normal.png": ("portrait_ren.png", 900),
    "sirius_portrait_normal.png": ("portrait_sirius.png", 900),
    "ren_sprite.png": ("player.png", 128),
    "sirius_sprite.png": ("boss.png", 150),
    "fairy_blue.png": ("fairy_blue.png", 64),
    "fairy_red.png": ("fairy_red.png", 64),
}

for src, (dst, h) in JOBS.items():
    p = os.path.join(RAW, src)
    if not os.path.exists(p):
        print("skip (missing)", src)
        continue
    im = Image.open(p).convert("RGBA")
    bbox = im.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()
    if bbox:
        im = im.crop(bbox)
    w = max(1, round(im.width * h / im.height))
    im = im.resize((w, h), Image.LANCZOS)
    pad = max(2, h // 40)
    out = Image.new("RGBA", (w + pad * 2, h + pad * 2), (0, 0, 0, 0))
    out.paste(im, (pad, pad))
    out.save(os.path.join(ART, dst))
    print("wrote", dst, out.size)


# ---------------------------------------------------------------------------------------
# Sprite sheets: renders/sheet/<name>_<row>_<col>.png -> Art/<out> (grid, same cell size)
# ---------------------------------------------------------------------------------------
SHEETS = {
    "ren": ("player_sheet.png", 3, 8, 128),
    "sirius": ("boss_sheet.png", 1, 8, 150),
}

for name, (dst, rows, cols, h) in SHEETS.items():
    frames = {}
    for r in range(rows):
        for c in range(cols):
            p = os.path.join(RAW, "sheet", "%s_%d_%d.png" % (name, r, c))
            if os.path.exists(p):
                frames[(r, c)] = Image.open(p).convert("RGBA")
    if len(frames) != rows * cols:
        print("skip sheet (missing frames)", name, len(frames))
        continue
    # union bbox keeps every frame aligned on the same anchor
    box = None
    for im in frames.values():
        b = im.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()
        if b:
            box = b if box is None else (min(box[0], b[0]), min(box[1], b[1]), max(box[2], b[2]), max(box[3], b[3]))
    cw = max(1, round((box[2] - box[0]) * h / (box[3] - box[1])))
    pad = 3
    sheet = Image.new("RGBA", ((cw + pad * 2) * cols, (h + pad * 2) * rows), (0, 0, 0, 0))
    for (r, c), im in frames.items():
        cell = im.crop(box).resize((cw, h), Image.LANCZOS)
        sheet.paste(cell, (c * (cw + pad * 2) + pad, r * (h + pad * 2) + pad))
    sheet.save(os.path.join(ART, dst))
    print("wrote", dst, sheet.size)
