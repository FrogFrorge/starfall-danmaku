"""
Generates all placeholder character art for Starfall Danmaku into ../Art/*.png.
Original characters (all male cast):
  Ren    - young shrine exorcist (player)
  Sirius - star-reading onmyoji (stage 1 boss)
Run:  python Tools/make_art.py
The game loads these PNGs at runtime; just restart the game after regenerating.
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Art")
os.makedirs(OUT, exist_ok=True)

SS = 3  # supersampling factor


# ----------------------------------------------------------------------------------------
# Helpers
# ----------------------------------------------------------------------------------------
class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))

    def layer(self):
        return Image.new("RGBA", self.img.size, (0, 0, 0, 0))

    def draw(self):
        return ImageDraw.Draw(self.img)

    def composite(self, lay, mask=None):
        if mask is not None:
            a = ImageChops.multiply(lay.getchannel("A"), mask)
            lay = lay.copy()
            lay.putalpha(a)
        self.img = Image.alpha_composite(self.img, lay)

    def save(self, name, outline=None):
        out = self.img.resize((self.w, self.h), Image.LANCZOS)
        if outline:
            out = add_glow(out, outline)
        out.save(os.path.join(OUT, name))
        print("wrote", name, out.size)


def S(pts):
    return [(x * SS, y * SS) for x, y in pts]


def bez(p0, p1, p2, p3, n=24):
    out = []
    for i in range(n + 1):
        t = i / n
        a = (1 - t) ** 3
        b = 3 * (1 - t) ** 2 * t
        c = 3 * (1 - t) * t * t
        d = t ** 3
        out.append((a * p0[0] + b * p1[0] + c * p2[0] + d * p3[0],
                    a * p0[1] + b * p1[1] + c * p2[1] + d * p3[1]))
    return out


def smooth(pts, n=10, closed=True):
    """Catmull-Rom through points."""
    res = []
    P = pts + pts[:3] if closed else [pts[0]] + pts + [pts[-1]]
    count = len(pts) if closed else len(pts) - 1
    for i in range(count):
        p0, p1, p2, p3 = P[i], P[i + 1], P[i + 2], P[i + 3]
        for k in range(n):
            t = k / n
            t2, t3 = t * t, t * t * t
            x = 0.5 * ((2 * p1[0]) + (-p0[0] + p2[0]) * t + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2 + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3)
            y = 0.5 * ((2 * p1[1]) + (-p0[1] + p2[1]) * t + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2 + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3)
            res.append((x, y))
    if not closed:
        res.append(pts[-1])
    return res


def poly(c, pts, fill, line=(25, 20, 35, 255), width=3):
    d = c.draw()
    sp = S(pts)
    d.polygon(sp, fill=fill)
    if line and width:
        d.line(sp + [sp[0]], fill=line, width=int(width * SS), joint="curve")


def ellipse(c, box, fill, line=(25, 20, 35, 255), width=3):
    d = c.draw()
    x0, y0, x1, y1 = box
    d.ellipse([x0 * SS, y0 * SS, x1 * SS, y1 * SS], fill=fill,
              outline=line if width else None, width=int(width * SS) if width else 0)


def stroke(c, pts, color, width):
    c.draw().line(S(pts), fill=color, width=int(width * SS), joint="curve")


def masked(c, mask_pts, draw_fn):
    """Draw something clipped to a polygon."""
    lay = c.layer()
    draw_fn(ImageDraw.Draw(lay))
    m = Image.new("L", c.img.size, 0)
    ImageDraw.Draw(m).polygon(S(mask_pts), fill=255)
    c.composite(lay, m)


def add_glow(img, color, radius=3):
    pad = radius * 3
    padded = Image.new("RGBA", (img.width + pad * 2, img.height + pad * 2), (0, 0, 0, 0))
    padded.paste(img, (pad, pad))
    img = padded
    a = img.getchannel("A").filter(ImageFilter.MaxFilter(radius * 2 + 1)).filter(ImageFilter.GaussianBlur(radius))
    glow = Image.new("RGBA", img.size, color + (0,))
    glow.putalpha(a.point(lambda v: int(v * 0.8)))
    return Image.alpha_composite(glow, img)


def star_pts(cx, cy, r_out, r_in, n=5, rot=-90):
    pts = []
    for i in range(n * 2):
        r = r_out if i % 2 == 0 else r_in
        a = math.radians(rot + i * 180 / n)
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts


INK = (28, 22, 40, 255)
SKIN = (255, 226, 205, 255)
SKIN_SH = (236, 190, 170, 255)


# ----------------------------------------------------------------------------------------
# Faces
# ----------------------------------------------------------------------------------------
def draw_face(c, cx, cy, iris, iris_dark, brow_angle=8, calm=False):
    """Male anime face centred at (cx, cy) = middle of the eyes line. ~200px wide."""
    # Ears
    ellipse(c, (cx - 112, cy - 20, cx - 88, cy + 30), SKIN)
    ellipse(c, (cx + 88, cy - 20, cx + 112, cy + 30), SKIN)
    # Face with angular jaw
    face = smooth([(cx - 100, cy - 90), (cx - 100, cy + 10), (cx - 82, cy + 62), (cx - 40, cy + 98),
                   (cx, cy + 108), (cx + 40, cy + 98), (cx + 82, cy + 62), (cx + 100, cy + 10), (cx + 100, cy - 90)], 8)
    poly(c, face, SKIN)
    # Neck shadow under jaw
    # Soft blush
    for side in (-1, 1):
        lay = c.layer()
        ImageDraw.Draw(lay).ellipse(S([(cx + side * 58 - 18, cy + 30), (cx + side * 58 + 18, cy + 44)])[0] + S([(cx + side * 58 + 18, cy + 44)])[0], fill=(255, 150, 150, 45))
        c.composite(lay)

    for side in (-1, 1):
        ex = cx + side * 45
        # Eye white (narrow, sharp)
        eye = [(ex - side * 32, cy + 2), (ex - side * 10, cy - 12), (ex + side * 22, cy - 10), (ex + side * 32, cy - 2),
               (ex + side * 24, cy + 14), (ex - side * 10, cy + 16)]
        eye = [(x, y) for x, y in eye]
        poly(c, eye, (250, 250, 255, 255), None, 0)

        def iris_fn(d, ex=ex):
            d.ellipse(S([(ex - 15, cy - 16), (ex + 15, cy + 20)])[0] + S([(ex + 15, cy + 20)])[0], fill=iris)
            d.ellipse(S([(ex - 15, cy - 16), (ex + 15, cy + 2)])[0] + S([(ex + 15, cy + 2)])[0], fill=iris_dark)
            d.ellipse(S([(ex - 6, cy - 6), (ex + 6, cy + 10)])[0] + S([(ex + 6, cy + 10)])[0], fill=INK)
            d.ellipse(S([(ex - 10, cy - 10), (ex - 2, cy - 2)])[0] + S([(ex - 2, cy - 2)])[0], fill=(255, 255, 255, 255))
            d.ellipse(S([(ex + 4, cy + 8), (ex + 8, cy + 12)])[0] + S([(ex + 8, cy + 12)])[0], fill=(255, 255, 255, 200))
        masked(c, eye, iris_fn)
        # Upper lid: thick line
        lid = [(ex - side * 36, cy + 4), (ex - side * 12, cy - 13), (ex + side * 22, cy - 12), (ex + side * 36, cy - 4)]
        stroke(c, smooth(lid, 6, closed=False), INK, 5.5)
        stroke(c, [(ex - side * 8, cy + 17), (ex + side * 22, cy + 14)], (120, 80, 90, 255), 1.6)
        # Brow
        by = cy - 32
        stroke(c, [(ex - side * 32, by + brow_angle * 0.6), (ex + side * 30, by - brow_angle * 0.4)], INK, 5 if not calm else 3.5)

    # Nose & mouth
    stroke(c, [(cx + 2, cy + 22), (cx - 4, cy + 46), (cx + 6, cy + 48)], (190, 140, 130, 255), 2.2)
    if calm:
        stroke(c, smooth([(cx - 18, cy + 72), (cx, cy + 76), (cx + 18, cy + 70)], 6, closed=False), (150, 80, 80, 255), 2.4)
    else:
        stroke(c, smooth([(cx - 16, cy + 72), (cx + 4, cy + 72), (cx + 18, cy + 66)], 6, closed=False), (150, 80, 80, 255), 2.4)


# ----------------------------------------------------------------------------------------
# Ren - young shrine exorcist
# ----------------------------------------------------------------------------------------
HAIR_REN = (36, 40, 66, 255)
HAIR_REN_HI = (80, 96, 150, 255)


def portrait_ren():
    c = Canvas(600, 900)
    cx, cy = 300, 330

    # Back hair
    back = smooth([(cx - 120, cy - 40), (cx - 130, cy - 130), (cx - 60, cy - 190), (cx + 30, cy - 200), (cx + 110, cy - 160),
                   (cx + 135, cy - 70), (cx + 128, cy + 30), (cx + 105, cy + 60), (cx - 105, cy + 60), (cx - 130, cy + 20)], 8)
    poly(c, back, HAIR_REN)

    # Body: haori
    body = [(cx - 40, cy + 120), (cx - 170, cy + 200), (cx - 240, cy + 260), (cx - 260, 900), (cx + 260, 900),
            (cx + 240, cy + 260), (cx + 170, cy + 200), (cx + 40, cy + 120)]
    poly(c, body, (240, 242, 250, 255))
    # Haori shading folds
    stroke(c, [(cx - 150, cy + 300), (cx - 175, 900)], (200, 205, 225, 255), 6)
    stroke(c, [(cx + 150, cy + 300), (cx + 175, 900)], (200, 205, 225, 255), 6)
    # Neck
    poly(c, [(cx - 34, cy + 80), (cx - 36, cy + 150), (cx + 36, cy + 150), (cx + 34, cy + 80)], SKIN_SH)
    # Inner kimono (indigo, V collar)
    poly(c, [(cx - 70, cy + 130), (cx, cy + 300), (cx + 70, cy + 130), (cx + 95, cy + 150), (cx + 60, 900), (cx - 60, 900), (cx - 95, cy + 150)],
         (40, 52, 110, 255))
    # White under-collar lines
    stroke(c, [(cx - 58, cy + 128), (cx, cy + 270)], (245, 245, 255, 255), 9)
    stroke(c, [(cx + 58, cy + 128), (cx, cy + 270)], (245, 245, 255, 255), 9)
    # Haori front trim (indigo stripe with wave pattern)
    for side in (-1, 1):
        trim = [(cx + side * 80, cy + 140), (cx + side * 110, cy + 150), (cx + side * 120, 900), (cx + side * 82, 900)]
        poly(c, trim, (58, 72, 150, 255))
        for k in range(8):
            y = cy + 190 + k * 50
            stroke(c, smooth([(cx + side * 86, y), (cx + side * 98, y - 8), (cx + side * 112, y)], 4, closed=False), (180, 200, 255, 255), 2.5)
    # Red cord with knot
    stroke(c, smooth([(cx - 118, cy + 360), (cx - 40, cy + 380), (cx + 40, cy + 380), (cx + 118, cy + 360)], 6, closed=False), (200, 40, 50, 255), 8)
    ellipse(c, (cx - 18, cy + 362, cx + 18, cy + 398), (220, 50, 60, 255))
    stroke(c, [(cx - 6, cy + 395), (cx - 14, cy + 470)], (200, 40, 50, 255), 6)
    stroke(c, [(cx + 6, cy + 395), (cx + 14, cy + 470)], (200, 40, 50, 255), 6)
    # Shoulder crest (star mark)
    poly(c, star_pts(cx - 175, cy + 240, 22, 9), (90, 110, 200, 255), INK, 2)

    # Raised hand with ofuda (talisman)
    arm = [(cx + 200, cy + 230), (cx + 230, cy + 120), (cx + 250, cy + 60), (cx + 290, cy + 70), (cx + 270, cy + 140), (cx + 250, cy + 260)]
    poly(c, arm, (232, 235, 248, 255))
    ofuda = [(cx + 238, cy - 60), (cx + 278, cy - 66), (cx + 300, cy + 70), (cx + 260, cy + 76)]
    poly(c, ofuda, (250, 245, 225, 255))
    stroke(c, [(cx + 262, cy - 30), (cx + 280, cy + 50)], (200, 40, 50, 255), 4)
    stroke(c, [(cx + 255, cy - 10), (cx + 285, cy - 14)], (200, 40, 50, 255), 3)
    stroke(c, [(cx + 260, cy + 20), (cx + 290, cy + 16)], (200, 40, 50, 255), 3)
    ellipse(c, (cx + 240, cy + 50, cx + 280, cy + 92), SKIN)  # fist

    draw_face(c, cx, cy, (90, 160, 255, 255), (40, 70, 160, 255), brow_angle=12)

    # Front hair: spiky bangs
    spikes = [
        [(cx - 115, cy - 110), (cx - 108, cy + 30), (cx - 80, cy - 60)],
        [(cx - 95, cy - 140), (cx - 66, cy - 6), (cx - 40, cy - 90)],
        [(cx - 60, cy - 150), (cx - 18, cy - 12), (cx + 6, cy - 110)],
        [(cx - 20, cy - 160), (cx + 26, cy - 30), (cx + 40, cy - 120)],
        [(cx + 10, cy - 160), (cx + 70, cy - 8), (cx + 76, cy - 110)],
        [(cx + 50, cy - 150), (cx + 108, cy + 20), (cx + 110, cy - 100)],
        [(cx + 96, cy - 120), (cx + 125, cy + 50), (cx + 132, cy - 60)],
    ]
    cap = smooth([(cx - 120, cy - 90), (cx - 100, cy - 170), (cx, cy - 205), (cx + 100, cy - 175), (cx + 132, cy - 90), (cx + 60, cy - 120), (cx - 60, cy - 120)], 8)
    poly(c, cap, HAIR_REN, None, 0)
    for sp in spikes:
        poly(c, sp, HAIR_REN)
    # Hair highlight
    stroke(c, smooth([(cx - 80, cy - 150), (cx - 20, cy - 175), (cx + 50, cy - 168)], 8, closed=False), HAIR_REN_HI, 8)
    c.save("portrait_ren.png")


# ----------------------------------------------------------------------------------------
# Sirius - star-reading onmyoji
# ----------------------------------------------------------------------------------------
HAIR_SIR = (215, 220, 240, 255)
HAIR_SIR_SH = (160, 165, 200, 255)


def portrait_sirius():
    c = Canvas(600, 900)
    cx, cy = 300, 360

    # Long silver back hair falling over shoulders
    back = smooth([(cx - 115, cy - 80), (cx - 140, cy + 80), (cx - 160, cy + 330), (cx - 110, cy + 420), (cx + 110, cy + 420),
                   (cx + 160, cy + 330), (cx + 140, cy + 80), (cx + 115, cy - 80), (cx, cy - 150)], 8)
    poly(c, back, HAIR_SIR_SH)

    # Kariginu robe: wide shoulders, violet
    body = [(cx - 40, cy + 120), (cx - 190, cy + 190), (cx - 280, cy + 280), (cx - 300, 900), (cx + 300, 900),
            (cx + 280, cy + 280), (cx + 190, cy + 190), (cx + 40, cy + 120)]
    poly(c, body, (245, 245, 252, 255))
    # Big violet sleeves/shoulder panels
    for side in (-1, 1):
        sh = [(cx + side * 120, cy + 170), (cx + side * 280, cy + 270), (cx + side * 300, 900), (cx + side * 170, 900), (cx + side * 140, cy + 300)]
        poly(c, sh, (100, 60, 170, 255))
        for k in range(5):
            sx = cx + side * (180 + (k % 2) * 50)
            sy = cy + 330 + k * 90
            poly(c, star_pts(sx, sy, 14, 6), (230, 210, 255, 255), None, 0)
    # Neck
    poly(c, [(cx - 32, cy + 80), (cx - 34, cy + 150), (cx + 34, cy + 150), (cx + 32, cy + 80)], SKIN_SH)
    # High round collar (agekubi)
    poly(c, smooth([(cx - 80, cy + 120), (cx - 60, cy + 175), (cx, cy + 195), (cx + 60, cy + 175), (cx + 80, cy + 120), (cx, cy + 150)], 6),
         (70, 40, 130, 255))
    # Inner layers
    poly(c, [(cx - 50, cy + 190), (cx, cy + 330), (cx + 50, cy + 190), (cx + 70, 900), (cx - 70, 900)], (30, 24, 60, 255))
    stroke(c, [(cx - 44, cy + 196), (cx, cy + 310)], (200, 170, 255, 255), 6)
    stroke(c, [(cx + 44, cy + 196), (cx, cy + 310)], (200, 170, 255, 255), 6)

    # Folding fan held near the chin
    fan_c = (cx - 150, cy + 210)
    fan = [fan_c]
    for i in range(13):
        a = math.radians(-150 + i * 9)
        fan.append((fan_c[0] + math.cos(a) * 150, fan_c[1] + math.sin(a) * 150))
    poly(c, fan, (25, 20, 45, 255), (200, 170, 255, 255), 2.5)
    for i in range(0, 13, 2):
        a = math.radians(-150 + i * 9)
        stroke(c, [fan_c, (fan_c[0] + math.cos(a) * 150, fan_c[1] + math.sin(a) * 150)], (120, 90, 180, 255), 1.5)
    for (sx, sy, r) in [(cx - 250, cy + 120, 10), (cx - 200, cy + 80, 7), (cx - 160, cy + 100, 12), (cx - 240, cy + 170, 6)]:
        poly(c, star_pts(sx, sy, r, r * 0.42), (255, 230, 140, 255), None, 0)
    ellipse(c, (cx - 172, cy + 190, cx - 128, cy + 236), SKIN)  # hand

    draw_face(c, cx, cy, (190, 120, 255, 255), (100, 50, 170, 255), brow_angle=-2, calm=True)

    # Hair cap over the forehead
    poly(c, smooth([(cx - 112, cy - 30), (cx - 108, cy - 110), (cx, cy - 135), (cx + 108, cy - 110), (cx + 112, cy - 30), (cx + 60, cy - 80), (cx - 60, cy - 80)], 8), HAIR_SIR, None, 0)
    # Front hair: long centre-parted bangs
    for side in (-1, 1):
        bang = smooth([(cx, cy - 120), (cx + side * 60, cy - 100), (cx + side * 105, cy - 30), (cx + side * 118, cy + 120),
                       (cx + side * 130, cy + 260), (cx + side * 96, cy + 120), (cx + side * 80, cy - 20), (cx + side * 30, cy - 60)], 8)
        poly(c, bang, HAIR_SIR)
        stroke(c, smooth([(cx + side * 40, cy - 100), (cx + side * 92, cy - 30), (cx + side * 112, cy + 120)], 8, closed=False), (255, 255, 255, 255), 4)
    poly(c, [(cx - 40, cy - 115), (cx, cy - 30), (cx + 40, cy - 115)], HAIR_SIR)

    # Eboshi (tall black court hat)
    hat = smooth([(cx - 95, cy - 110), (cx - 85, cy - 220), (cx - 40, cy - 330), (cx + 30, cy - 350), (cx + 70, cy - 300),
                  (cx + 95, cy - 200), (cx + 100, cy - 110)], 8)
    poly(c, hat, (22, 20, 30, 255), (90, 80, 130, 255), 3)
    stroke(c, [(cx - 90, cy - 118), (cx + 98, cy - 118)], (120, 100, 170, 255), 8)
    stroke(c, smooth([(cx - 30, cy - 300), (cx + 10, cy - 320), (cx + 50, cy - 290)], 6, closed=False), (70, 65, 95, 255), 5)
    # Chin cord
    stroke(c, [(cx - 92, cy - 110), (cx - 98, cy + 40)], (230, 220, 255, 255), 3)
    stroke(c, [(cx + 98, cy - 110), (cx + 102, cy + 40)], (230, 220, 255, 255), 3)

    # Floating star sparkles
    for (sx, sy, r) in [(cx + 210, cy - 200, 18), (cx + 250, cy - 120, 9), (cx - 220, cy - 230, 12), (cx + 180, cy - 290, 7), (cx - 250, cy - 120, 7)]:
        poly(c, star_pts(sx, sy, r, r * 0.35, 4, -90), (255, 240, 180, 255), None, 0)

    c.save("portrait_sirius.png")


# ----------------------------------------------------------------------------------------
# In-game sprites (chibi)
# ----------------------------------------------------------------------------------------
def sprite_ren():
    c = Canvas(96, 128)
    cx = 48
    # Haori body
    poly(c, [(cx - 10, 60), (cx - 30, 74), (cx - 34, 118), (cx + 34, 118), (cx + 30, 74), (cx + 10, 60)], (240, 242, 250, 255), INK, 2)
    poly(c, [(cx - 12, 66), (cx, 100), (cx + 12, 66), (cx + 16, 118), (cx - 16, 118)], (40, 52, 110, 255), INK, 1.5)
    stroke(c, [(cx - 26, 92), (cx + 26, 92)], (200, 40, 50, 255), 3)
    for side in (-1, 1):
        poly(c, [(cx + side * 18, 66), (cx + side * 24, 68), (cx + side * 26, 118), (cx + side * 19, 118)], (58, 72, 150, 255), None, 0)
    # Head
    ellipse(c, (cx - 26, 12, cx + 26, 64), HAIR_REN, INK, 2)
    ellipse(c, (cx - 20, 24, cx + 20, 62), SKIN, INK, 1.5)
    for sp in [[(cx - 26, 30), (cx - 18, 50), (cx - 12, 30)], [(cx - 16, 22), (cx - 4, 46), (cx + 2, 24)],
               [(cx - 2, 20), (cx + 10, 46), (cx + 14, 22)], [(cx + 10, 22), (cx + 22, 48), (cx + 26, 28)]]:
        poly(c, sp, HAIR_REN, INK, 1.2)
    poly(c, [(cx - 26, 32), (cx - 22, 14), (cx, 8), (cx + 22, 14), (cx + 26, 32), (cx, 24)], HAIR_REN, None, 0)
    for side in (-1, 1):
        ellipse(c, (cx + side * 9 - 4, 42, cx + side * 9 + 4, 52), (60, 120, 230, 255), INK, 1)
    c.save("player.png", outline=(120, 170, 255))


def sprite_sirius():
    c = Canvas(112, 144)
    cx = 56
    # Hair behind
    poly(c, [(cx - 26, 48), (cx - 32, 110), (cx + 32, 110), (cx + 26, 48)], HAIR_SIR_SH, INK, 1.5)
    # Robe with wide sleeves
    poly(c, [(cx - 12, 74), (cx - 46, 88), (cx - 52, 138), (cx + 52, 138), (cx + 46, 88), (cx + 12, 74)], (245, 245, 252, 255), INK, 2)
    for side in (-1, 1):
        poly(c, [(cx + side * 22, 86), (cx + side * 48, 92), (cx + side * 52, 138), (cx + side * 28, 138)], (100, 60, 170, 255), INK, 1.5)
    poly(c, [(cx - 10, 84), (cx, 110), (cx + 10, 84), (cx + 12, 138), (cx - 12, 138)], (30, 24, 60, 255), None, 0)
    # Head
    ellipse(c, (cx - 22, 34, cx + 22, 80), SKIN, INK, 1.5)
    for side in (-1, 1):
        poly(c, [(cx, 36), (cx + side * 24, 44), (cx + side * 26, 96), (cx + side * 16, 70), (cx + side * 8, 46)], HAIR_SIR, INK, 1.2)
        ellipse(c, (cx + side * 9 - 4, 58, cx + side * 9 + 4, 66), (170, 100, 240, 255), INK, 1)
    # Eboshi hat
    poly(c, smooth([(cx - 20, 40), (cx - 16, 14), (cx - 4, 2), (cx + 12, 2), (cx + 20, 18), (cx + 22, 40)], 6), (22, 20, 30, 255), (120, 100, 170, 255), 1.5)
    c.save("boss.png", outline=(190, 140, 255))


def sprite_shikigami(name, mark):
    """Paper-doll familiar (hitogata)."""
    c = Canvas(64, 64)
    doll = [(32, 6), (40, 10), (42, 20), (36, 24), (56, 30), (54, 36), (38, 34), (42, 58), (34, 58), (32, 44), (30, 58), (22, 58), (26, 34),
            (10, 36), (8, 30), (28, 24), (22, 20), (24, 10)]
    poly(c, doll, (250, 248, 238, 255), INK, 1.5)
    stroke(c, [(32, 28), (32, 40)], mark, 3)
    stroke(c, [(27, 32), (37, 32)], mark, 2.5)
    ellipse(c, (29, 12, 35, 18), mark, None, 0)
    c.save(name, outline=tuple(mark[:3]))


def sprite_spirit():
    c = Canvas(128, 128)
    flame = smooth([(64, 6), (78, 30), (96, 22), (100, 50), (112, 64), (104, 96), (82, 118), (46, 118), (24, 96), (16, 64),
                    (28, 50), (32, 22), (50, 30)], 8)
    poly(c, flame, (150, 80, 230, 255), (60, 20, 100, 255), 2)
    inner = smooth([(64, 34), (80, 56), (88, 84), (74, 106), (54, 106), (40, 84), (48, 56)], 8)
    poly(c, inner, (210, 160, 255, 255), None, 0)
    # Mask
    ellipse(c, (40, 52, 88, 100), (248, 244, 236, 255), INK, 2)
    poly(c, [(46, 70), (58, 66), (60, 74), (48, 76)], INK, None, 0)
    poly(c, [(82, 70), (70, 66), (68, 74), (80, 76)], INK, None, 0)
    stroke(c, smooth([(54, 88), (64, 92), (74, 88)], 4, closed=False), (200, 40, 60, 255), 2.5)
    stroke(c, [(64, 56), (64, 62)], (200, 40, 60, 255), 3)
    c.save("spirit.png", outline=(180, 110, 255))


def sprite_option():
    c = Canvas(48, 48)
    ellipse(c, (4, 4, 44, 44), (255, 170, 70, 255), INK, 2)
    poly(c, star_pts(24, 24, 16, 7, 6, -90), (255, 245, 210, 255), (180, 90, 30, 255), 1.2)
    c.save("option.png", outline=(255, 170, 70))


# ----------------------------------------------------------------------------------------
# Title logo
# ----------------------------------------------------------------------------------------
def title_logo():
    w, h = 900, 380
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    fz = ImageFont.truetype("C:/Windows/Fonts/msjhbd.ttc", 150)
    fe = ImageFont.truetype("C:/Windows/Fonts/georgiab.ttf", 64)

    text_mask = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(text_mask)
    d.text((w // 2, 140), "星墜彈幕", font=fz, anchor="mm", fill=255)
    d.text((w // 2, 290), "STARFALL DANMAKU", font=fe, anchor="mm", fill=255)

    grad = Image.new("RGBA", (w, h))
    gd = ImageDraw.Draw(grad)
    for y in range(h):
        t = y / h
        r = int(170 + 85 * t)
        g = int(150 + 30 * math.sin(t * 3))
        b = int(255 - 60 * t)
        gd.line([(0, y), (w, y)], fill=(r, g, b, 255))
    text = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    text.paste(grad, (0, 0), text_mask)

    outline = text_mask.filter(ImageFilter.MaxFilter(9))
    ol = Image.new("RGBA", (w, h), (30, 15, 60, 255))
    ol.putalpha(outline)
    glow = Image.new("RGBA", (w, h), (170, 120, 255, 0))
    glow.putalpha(outline.filter(ImageFilter.GaussianBlur(14)).point(lambda v: int(v * 0.9)))
    img = Image.alpha_composite(img, glow)
    img = Image.alpha_composite(img, ol)
    img = Image.alpha_composite(img, text)
    sd = ImageDraw.Draw(img)
    for (sx, sy, r) in [(110, 60, 22), (800, 80, 16), (760, 220, 10), (140, 230, 12), (450, 20, 9)]:
        sd.polygon(star_pts(sx, sy, r, r * 0.35, 4), fill=(255, 240, 190, 255))
    img.save(os.path.join(OUT, "title.png"))
    print("wrote title.png", img.size)


if __name__ == "__main__":
    # Characters and paper dolls now come from Blender (Tools/blender/). Pass --legacy to regenerate the old flat versions.
    import sys
    if "--legacy" in sys.argv:
        portrait_ren()
        portrait_sirius()
        sprite_ren()
        sprite_sirius()
        sprite_shikigami("fairy_blue.png", (60, 110, 230, 255))
        sprite_shikigami("fairy_red.png", (210, 40, 60, 255))
    sprite_spirit()
    sprite_option()
    title_logo()
