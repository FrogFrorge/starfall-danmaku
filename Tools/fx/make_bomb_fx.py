"""Bomb effect textures (white on transparent; the game tints them per character and draws them additively).

  fx_seal.png     蓮: a barrier seal: rings, a band of seal characters, radial talisman strips
  fx_bolt.png     蒼: one tall jagged lightning bolt with a glow (stacked and flickered along the beam)
  fx_mandala.png  晴: a mountain mandala: petal rings, peaks, a band of characters
  fx_sigil.png    雪: a puppeteer's sigil: wisteria-like petals, a thread lattice, a star
  fx_flash.png    a sharp shockwave ring for the moment a bomb goes off
"""
import math, os, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Godot", "Art")
FONT = os.path.join(ROOT, "Godot", "Fonts", "NotoSerifTC-SemiBold.ttf")
S = 1024
random.seed(7)


def glowed(img, radii=((18, 0.9), (6, 0.8))):
    """Add a soft glow under the line art (alpha only matters: the texture is white)."""
    a = np.asarray(img.split()[-1], np.float32) / 255
    out = a.copy()
    for r, k in radii:
        b = np.asarray(img.split()[-1].filter(ImageFilter.GaussianBlur(r)), np.float32) / 255
        out = np.maximum(out, np.clip(b * k * 1.6, 0, 1))
    al = Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8))
    white = Image.new("L", img.size, 255)
    return Image.merge("RGBA", (white, white, white, al))


def ring(d, c, r, w, a=255):
    d.ellipse((c - r, c - r, c + r, c + r), outline=(255, 255, 255, a), width=w)


def text_ring(img, c, r, chars, size, a=255, rot0=0.0):
    font = ImageFont.truetype(FONT, size)
    for i, ch in enumerate(chars):
        ang = rot0 + i * 360 / len(chars)
        tile = Image.new("RGBA", (size * 2, size * 2), (0, 0, 0, 0))
        ImageDraw.Draw(tile).text((size, size), ch, font=font, fill=(255, 255, 255, a), anchor="mm")
        tile = tile.rotate(-ang, resample=Image.BICUBIC)
        x = c + r * math.sin(math.radians(ang)) - size
        y = c - r * math.cos(math.radians(ang)) - size
        img.alpha_composite(tile, (int(x), int(y)))


def seal():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img); c = S // 2
    for r, w, a in ((500, 6, 255), (484, 2, 200), (380, 4, 230), (366, 2, 180), (250, 5, 255), (120, 3, 220)):
        ring(d, c, r, w, a)
    text_ring(img, c, 432, "臨兵鬥者皆陣列在前退魔結界封", 56)
    # talisman strips between the middle rings
    for i in range(16):
        ang = math.radians(i * 22.5 + 11.25)
        ca, sa = math.cos(ang), math.sin(ang)
        pts = []
        for (u, v) in ((-16, 262), (16, 262), (16, 356), (-16, 356)):
            pts.append((c + u * ca - v * sa, c + u * sa + v * ca))
        d.polygon(pts, outline=(255, 255, 255, 230), width=3)
        for k in range(3):
            v = 280 + k * 24
            d.line([(c - 8 * ca - v * sa, c - 8 * sa + v * ca), (c + 8 * ca - v * sa, c + 8 * sa + v * ca)], fill=(255, 255, 255, 200), width=3)
    # five-pointed star (seiman) in the middle
    pts = [(c + 240 * math.sin(math.radians(k * 144)), c - 240 * math.cos(math.radians(k * 144))) for k in range(6)]
    d.line(pts, fill=(255, 255, 255, 255), width=5)
    text_ring(img, c, 0, "封", 120)
    return glowed(img)


def bolt():
    W, H = 256, 1024
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    def zig(x0, y0, y1, amp, width, a, branch):
        x, y = x0, y0; pts = [(x, y)]
        while y < y1:
            y += random.randint(18, 46); x = x0 + random.uniform(-amp, amp); pts.append((x, min(y, y1)))
            if branch and random.random() < 0.18:
                zig(x, y, min(y1, y + random.randint(80, 200)), amp * 0.6, max(2, width // 2), int(a * 0.7), False)
        d.line(pts, fill=(255, 255, 255, a), width=width, joint="curve")
    zig(W / 2, 0, H, 34, 9, 255, True)
    zig(W / 2, 0, H, 50, 4, 170, True)
    out = glowed(img, ((26, 0.8), (8, 0.9)))
    # fade the glow to nothing at the left and right edges so a stretched bolt never shows its box
    a = np.asarray(out.split()[-1], np.float32)
    x = np.abs(np.linspace(-1, 1, W))
    a *= np.clip((1 - x) / 0.35, 0, 1)[None, :]
    out.putalpha(Image.fromarray(a.astype(np.uint8)))
    return out


def mandala():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img); c = S // 2
    ring(d, c, 500, 5); ring(d, c, 470, 2, 180); ring(d, c, 300, 4); ring(d, c, 150, 3, 220)
    for i in range(24):                       # outer petals
        ang = math.radians(i * 15)
        p = [(c + r * math.sin(ang + o), c - r * math.cos(ang + o)) for r, o in ((300, -0.12), (455, 0), (300, 0.12))]
        d.line(p, fill=(255, 255, 255, 230), width=4)
    for i in range(8):                        # mountain peaks around the inner ring
        ang = math.radians(i * 45)
        base = [(c + 150 * math.sin(ang + o), c - 150 * math.cos(ang + o)) for o in (-0.36, 0.36)]
        top = (c + 285 * math.sin(ang), c - 285 * math.cos(ang))
        d.polygon([base[0], top, base[1]], outline=(255, 255, 255, 255), width=4)
    text_ring(img, c, 382, "山嶽修驗緩慢靜心", 64)
    text_ring(img, c, 0, "山", 130)
    return glowed(img)


def sigil():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img); c = S // 2
    ring(d, c, 500, 4); ring(d, c, 420, 2, 200); ring(d, c, 200, 3)
    for i in range(36):                       # wisteria-like petal drops
        ang = math.radians(i * 10)
        for k, (r, s) in enumerate(((440, 16), (468, 11), (490, 7))):
            x, y = c + r * math.sin(ang), c - r * math.cos(ang)
            d.ellipse((x - s, y - s, x + s, y + s), outline=(255, 255, 255, 220 - k * 40), width=3)
    pts = [(c + 400 * math.sin(math.radians(i * 60)), c - 400 * math.cos(math.radians(i * 60))) for i in range(6)]
    for i in range(6):                        # thread lattice
        for j in range(i + 1, 6):
            d.line([pts[i], pts[j]], fill=(255, 255, 255, 150), width=2)
    for x, y in pts:
        d.ellipse((x - 14, y - 14, x + 14, y + 14), fill=(255, 255, 255, 255))
    text_ring(img, c, 310, "操絲傀儡守護", 60)
    return glowed(img)


def flash():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img); c = S // 2
    ring(d, c, 470, 22)
    return glowed(img, ((30, 0.9), (10, 1.0)))


if __name__ == "__main__":
    for name, fn in (("seal", seal), ("bolt", bolt), ("mandala", mandala), ("sigil", sigil), ("flash", flash)):
        im = fn()
        if name != "bolt": im = im.resize((768, 768), Image.LANCZOS)
        im.save(os.path.join(OUT, f"fx_{name}.png"))
        print("fx_" + name, im.size)


def arrow():
    """蒼's arrow (pointing up): black lacquered shaft with red bands, red fletching, a silver head with a red glint."""
    W, H = 96, 512
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img); c = W // 2
    d.rectangle((c - 3, 70, c + 3, H - 30), fill=(28, 22, 26, 255))                 # shaft
    d.line((c - 1, 72, c - 1, H - 32), fill=(110, 90, 100, 255), width=1)           # lacquer shine
    for y in (110, 300, 420):                                                         # red binding bands
        d.rectangle((c - 4, y, c + 4, y + 8), fill=(190, 30, 40, 255))
    d.polygon([(c, 0), (c + 17, 64), (c + 6, 58), (c + 4, 84), (c - 4, 84), (c - 6, 58), (c - 17, 64)], fill=(225, 232, 240, 255))  # head
    d.polygon([(c, 6), (c + 5, 56), (c, 50)], fill=(255, 255, 255, 255))
    d.line((c, 10, c, 60), fill=(230, 60, 60, 255), width=2)                        # red glint down the blade
    for side in (-1, 1):                                                              # fletching
        d.polygon([(c + side * 3, H - 150), (c + side * 26, H - 120), (c + side * 26, H - 40), (c + side * 3, H - 60)], fill=(200, 35, 45, 255))
        for k in range(6):
            y = H - 140 + k * 15
            d.line((c + side * 4, y, c + side * 24, y + 14), fill=(120, 15, 25, 255), width=2)
    d.rectangle((c - 5, H - 30, c + 5, H - 18), fill=(190, 30, 40, 255))            # nock
    return img


if __name__ == "__main__":
    a = arrow()
    a.save(os.path.join(OUT, "shot_arrow.png")); print("shot_arrow", a.size)
