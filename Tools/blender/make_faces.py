"""Paints the 2D anime face decals (eyes / brows / mouth) used on the Blender heads.
Run with normal Python (needs Pillow + numpy):  python Tools/blender/make_faces.py
Output: Tools/blender/tex/face_<name>_<expr>.png  (1024x1024, transparent)

Decal space: the texture covers a square of FACE_W blender units centred on the head,
x -> right, z -> up.  Keep in sync with FACE_W in toon_lib.py.
"""
import math
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "tex")
os.makedirs(OUT, exist_ok=True)

RES = 1024
SS = 4
FACE_W = 1.9
N = RES * SS


def P(x, z):
    """blender face-space -> supersampled pixel"""
    return ((x / FACE_W + 0.5) * N, (0.5 - z / FACE_W) * N)


def lerp(a, b, t):
    return a + (b - a) * t


def lerpc(a, b, t):
    return tuple(int(lerp(a[i], b[i], t)) for i in range(len(a)))


def bez(p0, p1, p2, n=40):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in (i / n for i in range(n + 1))]


def cubic(p0, p1, p2, p3, n=48):
    out = []
    for i in range(n + 1):
        t = i / n
        a, b, c, d = (1 - t) ** 3, 3 * (1 - t) ** 2 * t, 3 * (1 - t) * t * t, t ** 3
        out.append((a * p0[0] + b * p1[0] + c * p2[0] + d * p3[0], a * p0[1] + b * p1[1] + c * p2[1] + d * p3[1]))
    return out


def tapered(pts, w0, w1, wmid=None):
    """Polygon of a stroke along pts (pixel space) with width w0 -> (wmid) -> w1."""
    left, right = [], []
    n = len(pts)
    for i, (x, y) in enumerate(pts):
        t = i / (n - 1)
        if wmid is None:
            w = lerp(w0, w1, t)
        else:
            w = lerp(w0, wmid, t * 2) if t < 0.5 else lerp(wmid, w1, (t - 0.5) * 2)
        x0, y0 = pts[max(0, i - 1)]
        x1, y1 = pts[min(n - 1, i + 1)]
        dx, dy = x1 - x0, y1 - y0
        L = math.hypot(dx, dy) or 1
        nx, ny = -dy / L, dx / L
        left.append((x + nx * w / 2, y + ny * w / 2))
        right.append((x - nx * w / 2, y - ny * w / 2))
    return left + right[::-1]


def vgrad(size, box, top, bottom):
    """RGBA image with a vertical gradient inside box."""
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    x0, y0, x1, y1 = [int(v) for v in box]
    h = max(1, y1 - y0)
    arr = np.zeros((h, x1 - x0, 4), dtype=np.uint8)
    t = np.linspace(0, 1, h)[:, None]
    for c in range(4):
        arr[:, :, c] = (top[c] + (bottom[c] - top[c]) * t).astype(np.uint8)
    img.paste(Image.fromarray(arr, "RGBA"), (x0, y0))
    return img


class Face:
    def __init__(self):
        self.img = Image.new("RGBA", (N, N), (0, 0, 0, 0))

    def comp(self, layer, mask=None):
        if mask is not None:
            a = Image.fromarray((np.asarray(layer.getchannel("A"), dtype=np.uint16) * np.asarray(mask, dtype=np.uint16) // 255).astype(np.uint8))
            layer = layer.copy()
            layer.putalpha(a)
        self.img = Image.alpha_composite(self.img, layer)

    def poly(self, pts, color):
        lay = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        ImageDraw.Draw(lay).polygon(pts, fill=color)
        self.comp(lay)

    def save(self, name):
        out = self.img.resize((RES, RES), Image.LANCZOS)
        out.save(os.path.join(OUT, name))
        print("wrote", name)


INK = (38, 24, 36, 255)


def draw_eye(f, cx, cz, side, w, h, iris_top, iris_mid, iris_bot, lid_ink=INK, sharp=1.0, closed=False, iris_shift=0.0):
    """side = -1 (character's right, image left) / +1. cx,cz = eye centre in face units."""
    inner = P(cx - side * w * 0.5, cz - h * 0.08)
    outer = P(cx + side * w * 0.52, cz + h * 0.02)
    top = P(cx + side * w * 0.05, cz + h * 0.55)
    bottom = P(cx + side * w * 0.05, cz - h * 0.5)

    if closed:
        arc = bez(P(cx - side * w * 0.5, cz), P(cx, cz - h * 0.35), P(cx + side * w * 0.52, cz))
        f.poly(tapered(arc, 6, 26, 30), lid_ink)
        lash = bez(P(cx + side * w * 0.42, cz - h * 0.02), P(cx + side * w * 0.58, cz - h * 0.06), P(cx + side * w * 0.66, cz - h * 0.2), 12)
        f.poly(tapered(lash, 18, 2), lid_ink)
        return

    upper = cubic(inner, P(cx - side * w * 0.3, cz + h * 0.52 * sharp), P(cx + side * w * 0.3, cz + h * 0.6), outer)
    lower = cubic(outer, P(cx + side * w * 0.35, cz - h * 0.42), P(cx - side * w * 0.25, cz - h * 0.5), inner)
    white_poly = upper + lower

    mask = Image.new("L", f.img.size, 0)
    ImageDraw.Draw(mask).polygon(white_poly, fill=255)

    # sclera with soft shadow under the upper lid
    lay = vgrad(f.img.size, (0, P(0, cz + h * 0.6)[1], N, P(0, cz - h * 0.6)[1]),
                (175, 175, 205, 255), (252, 250, 250, 255))
    f.comp(lay, mask)

    # iris
    icx = cx + side * w * 0.02 + iris_shift * w
    iw, ih = w * 0.30, h * 0.50
    ibox = (P(icx - iw, cz + ih * 1.05)[0], P(0, cz + ih * 1.05)[1], P(icx + iw, 0)[0], P(0, cz - ih * 0.95)[1])
    iris_lay = Image.new("RGBA", f.img.size, (0, 0, 0, 0))
    g1 = vgrad(f.img.size, ibox, iris_top, iris_mid)
    g2 = vgrad(f.img.size, (ibox[0], (ibox[1] + ibox[3]) / 2, ibox[2], ibox[3]), iris_mid, iris_bot)
    iris_lay = Image.alpha_composite(iris_lay, g1)
    iris_lay = Image.alpha_composite(iris_lay, g2)
    imask = Image.new("L", f.img.size, 0)
    ImageDraw.Draw(imask).ellipse(ibox, fill=255)
    iris_lay.putalpha(imask)
    d = ImageDraw.Draw(iris_lay)
    ring = tuple(int(c * 0.45) for c in iris_top[:3]) + (255,)
    d.ellipse(ibox, outline=ring, width=int(N * 0.004))
    # pupil
    pw, ph = iw * 0.42, ih * 0.5
    d.ellipse((P(icx - pw, cz + ph * 0.9)[0], P(0, cz + ph * 0.9)[1], P(icx + pw, 0)[0], P(0, cz - ph * 0.5)[1]),
              fill=tuple(int(c * 0.3) for c in iris_top[:3]) + (255,))
    # inner glow crescent
    glow = Image.new("RGBA", f.img.size, (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((P(icx - iw * 0.7, cz - ih * 0.15)[0], P(0, cz - ih * 0.15)[1], P(icx + iw * 0.7, 0)[0], P(0, cz - ih * 1.1)[1]),
                                 fill=iris_bot[:3] + (170,))
    glow = glow.filter(ImageFilter.GaussianBlur(N * 0.004))
    glow.putalpha(Image.fromarray((np.asarray(glow.getchannel("A"), dtype=np.uint16) * np.asarray(imask, dtype=np.uint16) // 255).astype(np.uint8)))
    iris_lay = Image.alpha_composite(iris_lay, glow)
    d = ImageDraw.Draw(iris_lay)
    # highlights
    hx = icx - side * iw * 0.35
    d.ellipse((P(hx - iw * 0.32, cz + ih * 0.62)[0], P(0, cz + ih * 0.62)[1], P(hx + iw * 0.32, 0)[0], P(0, cz + ih * 0.12)[1]), fill=(255, 255, 255, 255))
    hx2 = icx + side * iw * 0.4
    d.ellipse((P(hx2 - iw * 0.13, cz - ih * 0.35)[0], P(0, cz - ih * 0.35)[1], P(hx2 + iw * 0.13, 0)[0], P(0, cz - ih * 0.6)[1]), fill=(255, 255, 255, 230))
    f.comp(iris_lay, mask)

    # upper lash line: thick, with a sharp flick at the outer corner
    f.poly(tapered(upper, 16, 64, 72), lid_ink)
    flick = bez(outer, P(cx + side * w * 0.6, cz - h * 0.02), P(cx + side * w * 0.66, cz - h * 0.14), 14)
    f.poly(tapered(flick, 52, 4), lid_ink)
    # lower lash (outer half only)
    lower_outer = lower[: len(lower) // 2]
    f.poly(tapered(lower_outer, 18, 3), (120, 70, 80, 255))
    # crease
    crease = cubic(P(cx - side * w * 0.25, cz + h * 0.72), P(cx - side * w * 0.05, cz + h * 0.84), P(cx + side * w * 0.3, cz + h * 0.82), P(cx + side * w * 0.48, cz + h * 0.62), 24)
    f.poly(tapered(crease, 5, 12, 14), (150, 95, 100, 255))


def draw_brow(f, cx, cz, side, w, angle, thick=1.0):
    a = P(cx - side * w * 0.5, cz - angle * 0.5)
    m = P(cx + side * w * 0.05, cz + 0.035 - angle * 0.05)
    b = P(cx + side * w * 0.55, cz + angle * 0.3)
    f.poly(tapered(bez(a, m, b, 30), 56 * thick, 10 * thick, 48 * thick), INK)


def draw_mouth(f, kind):
    if kind == "smirk":
        pts = cubic(P(-0.11, -0.6), P(-0.04, -0.625), P(0.04, -0.62), P(0.12, -0.575), 30)
        f.poly(tapered(pts, 8, 14, 22), (110, 45, 55, 255))
    elif kind == "smile":
        pts = cubic(P(-0.1, -0.585), P(-0.05, -0.625), P(0.05, -0.625), P(0.1, -0.585), 30)
        f.poly(tapered(pts, 8, 8, 22), (110, 45, 55, 255))
    elif kind == "open":
        poly = cubic(P(-0.09, -0.58), P(-0.06, -0.6), P(0.06, -0.6), P(0.09, -0.58), 20) + cubic(P(0.09, -0.58), P(0.06, -0.7), P(-0.06, -0.7), P(-0.09, -0.58), 20)
        f.poly(poly, (120, 40, 55, 255))
        tongue = cubic(P(-0.05, -0.665), P(-0.02, -0.64), P(0.02, -0.64), P(0.05, -0.665), 12) + [P(0.0, -0.68)]
        f.poly(tongue, (220, 110, 120, 255))
    else:  # flat / serious
        pts = bez(P(-0.08, -0.6), P(0.0, -0.61), P(0.08, -0.6), 20)
        f.poly(tapered(pts, 8, 8, 20), (110, 45, 55, 255))


def draw_nose(f):
    pts = bez(P(0.015, -0.34), P(0.03, -0.39), P(0.0, -0.41), 12)
    f.poly(tapered(pts, 8, 16), (200, 130, 125, 220))


def blush(f, strength=70):
    lay = Image.new("RGBA", f.img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    for side in (-1, 1):
        x = side * 0.5
        d.ellipse((P(x - 0.16, -0.24)[0], P(0, -0.24)[1], P(x + 0.16, 0)[0], P(0, -0.36)[1]), fill=(255, 120, 130, strength))
        for k in range(3):
            xx = x - 0.08 + k * 0.07
            d.line([P(xx + 0.03, -0.26), P(xx - 0.01, -0.33)], fill=(230, 90, 110, strength + 60), width=int(N * 0.0035))
    f.comp(lay.filter(ImageFilter.GaussianBlur(N * 0.006)))


def make(name, eye_cols, brow_angle, mouth, expr, sharp=1.0, closed=False, iris_shift=0.0, brow_thick=1.0, eye_h=0.30):
    f = Face()
    for side in (-1, 1):
        draw_eye(f, side * 0.36, -0.08, side, 0.44, eye_h, *eye_cols, sharp=sharp, closed=closed, iris_shift=iris_shift * side * 0 + iris_shift)
        draw_brow(f, side * 0.36, 0.22, side, 0.42, brow_angle, brow_thick)
    draw_nose(f)
    draw_mouth(f, mouth)
    if expr in ("happy", "embarrassed"):
        blush(f, 60)
    f.save(f"face_{name}_{expr}.png")


REN_EYES = ((30, 50, 120, 255), (40, 110, 230, 255), (140, 220, 255, 255))
SIRIUS_EYES = ((60, 20, 90, 255), (150, 70, 220, 255), (240, 190, 255, 255))

if __name__ == "__main__":
    # Ren: sharp, determined
    make("ren", REN_EYES, -0.06, "smirk", "normal", sharp=0.85)
    make("ren", REN_EYES, -0.1, "open", "angry", sharp=0.8, brow_thick=1.15)
    make("ren", REN_EYES, 0.02, "smile", "happy", sharp=0.9)
    # Sirius: calm, half-lidded elegance
    make("sirius", SIRIUS_EYES, 0.03, "smile", "normal", sharp=0.7, eye_h=0.26)
    make("sirius", SIRIUS_EYES, -0.04, "flat", "serious", sharp=0.7, eye_h=0.26)
    make("sirius", SIRIUS_EYES, 0.03, "smile", "closed", closed=True)
