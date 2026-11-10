"""Builds animation sprite sheets (Art/<name>_sheet.png) for the player and boss sprites.

Rows are lean poses (-2 .. +2: leaning left .. right, a rigid rotation, nothing bends), columns are hair phases.
Only the hair moves: hair pixels (inside the zones and close to the hair colour) are copied, swayed from the root
and drawn over the untouched body, so hands, legs, fans and the bow can never deform.

    python Tools/anim/make_sheets.py
"""
import math, os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sprite_specs import SPECS, COLS, POSES, PAD_X, PAD_Y

ART = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Art")


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def hair_mask(img, spec):
    h, w = img.shape[:2]
    v, u = np.mgrid[0:h, 0:w]
    u = (u + 0.5) / w
    v = (v + 0.5) / h
    rgb = img[..., :3]
    col = np.zeros((h, w))
    for c in spec["hair"]:
        d = np.sqrt(((rgb - np.array(c, float)) ** 2).sum(-1))
        col = np.maximum(col, 1.0 - smooth(spec["tol"] * 0.6, spec["tol"], d))
    mask = np.zeros((h, w))
    ramp = np.zeros((h, w))
    for (u0, u1, v0, v1, s) in spec["zones"]:
        zu = smooth(u0, u0 + 0.03, u) * (1 - smooth(u1 - 0.03, u1, u))
        zv = smooth(v0, v0 + 0.03, v) * (1 - smooth(v1 - 0.02, v1, v))
        z = zu * zv
        mask = np.maximum(mask, z * col)
        # how far along the strand: 0 at the root, 1 at the tip
        ramp = np.maximum(ramp, z * s * np.clip((v - v0) / max(1e-3, v1 - v0), 0, 1))
    return mask * (img[..., 3] / 255.0), ramp


def shift(layer, dx, dy):
    """Sample layer at (x - dx, y - dy) with bilinear filtering; dx/dy are per-pixel fields."""
    h, w = layer.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    sx = np.clip(xx - dx, 0, w - 1.001)
    sy = np.clip(yy - dy, 0, h - 1.001)
    x0 = np.floor(sx).astype(int); y0 = np.floor(sy).astype(int)
    fx = (sx - x0)[..., None]; fy = (sy - y0)[..., None]
    a = layer[y0, x0]; b = layer[y0, x0 + 1]; c = layer[y0 + 1, x0]; d = layer[y0 + 1, x0 + 1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def cloth_weight(h, w, spec):
    """How much each pixel flutters: outer sleeves / robe below `top`, minus the legs and rigid `keep` boxes (feathered).
    The legs and feet (below `legs`=(v_top, half_width)) never move, so nobody walks on rubber legs."""
    core, top, amp = spec.get("cloth", (0.14, 0.30, 1.0))
    leg_top, leg_half = spec.get("legs", (0.58, 0.20))
    v, u = np.mgrid[0:h, 0:w]
    u = (u + 0.5) / w
    v = (v + 0.5) / h
    wgt = smooth(core, core + 0.22, np.abs(u - 0.5)) * smooth(top, top + 0.28, v)
    wgt = wgt * (1 - smooth(0.80, 0.90, v))                       # nothing near the feet moves
    wgt = wgt * (1 - smooth(leg_top, leg_top + 0.06, v) * (1 - smooth(leg_half, leg_half + 0.06, np.abs(u - 0.5))))
    for (u0, u1, v0, v1) in spec.get("keep", []):
        f = 0.04
        box = smooth(u0 - f, u0, u) * (1 - smooth(u1, u1 + f, u)) * smooth(v0 - f, v0, v) * (1 - smooth(v1, v1 + f, v))
        wgt = wgt * (1 - box)
    return wgt * amp, u, v


def over(top, base):
    """Premultiplied 'over'."""
    return top + base * (1 - top[..., 3:4] / 255.0)


def build(name, spec):
    src = Image.open(os.path.join(ART, name + ".png")).convert("RGBA")
    img = np.asarray(src).astype(float)
    h, w = img.shape[:2]
    mask, ramp = hair_mask(img, spec)
    # dilate the motion field a little so swaying hair can move past its old outline
    ramp_img = Image.fromarray((ramp * 255).astype(np.uint8)).filter(__import__("PIL.ImageFilter").ImageFilter.MaxFilter(9))
    ramp = np.maximum(ramp, np.asarray(ramp_img).astype(float) / 255.0 * 0.9)

    pre = img.copy()
    pre[..., :3] *= pre[..., 3:4] / 255.0
    hair = pre * mask[..., None]

    px, py = int(w * PAD_X), int(h * PAD_Y)
    cw, ch = w + 2 * px, h + 2 * py
    sheet = Image.new("RGBA", (cw * COLS, ch * POSES), (0, 0, 0, 0))
    amp = w * 0.035
    yy = (np.mgrid[0:h, 0:w][0] + 0.5) / h
    cw_, uu, vv = cloth_weight(h, w, spec)
    camp = w * 0.035
    for row in range(POSES):
        pose = row - POSES // 2
        for col in range(COLS):
            ph = 2 * math.pi * col / COLS
            # cloth: a wave travelling down the robe, and the whole skirt trailing behind a lean
            cdx = cw_ * (camp * np.sin(ph - vv * 5.0 + uu * 3.0) - pose * w * 0.04 * vv)
            cdy = cw_ * (camp * 0.22 * np.cos(ph * 1.0 + uu * 4.0))
            body = shift(pre, cdx, cdy)
            # hair: swings from the root, on top of the cloth motion
            dx = ramp * (amp * np.sin(ph + yy * 4.0) - pose * w * 0.03) + cdx * 0.5
            dy = ramp * (amp * 0.25 * np.cos(ph * 1.0 + yy * 2.0)) + cdy * 0.5
            frame = over(shift(hair, dx, dy), body)
            a = np.clip(frame[..., 3:4], 1e-6, 255)
            out = frame.copy()
            out[..., :3] = np.where(a > 0.5, frame[..., :3] * 255.0 / a, 0)
            fr = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA")
            canvas = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
            canvas.paste(fr, (px, py))
            if pose:
                # rigid lean around the hips, plus a slight narrowing as if turning
                canvas = canvas.rotate(-pose * 3.5, resample=Image.BICUBIC, center=(cw / 2, py + h * 0.7))
                sq = 1.0 - 0.025 * abs(pose)
                nw = int(cw * sq)
                canvas2 = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
                canvas2.paste(canvas.resize((nw, ch), Image.LANCZOS), ((cw - nw) // 2, 0))
                canvas = canvas2
            sheet.paste(canvas, (col * cw, row * ch))
    sheet.save(os.path.join(ART, name + "_sheet.png"))
    print(name, sheet.size, "hair px:", int((mask > 0.5).sum()))


if __name__ == "__main__":
    only = sys.argv[1:]
    for n, s in SPECS.items():
        if not only or n in only:
            build(n, s)
