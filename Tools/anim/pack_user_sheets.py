"""Pack the hand-made idle/left/right sprite sheets in NewArt/ into the game's 12x3 sheets.

Output: Godot/Art/anim_<name>.png, rows = idle, left, right; 12 columns each.
Every frame is rescaled so the idle pose's first frame is REF_H pixels tall, and the cell is
centred on that frame's visible box, so the game can draw any frame at the same anchor.
Also prints, for each playable character, the peak (fully leaned) and mid (closest to idle)
frame of the left/right animations; Hud/WorldCore use those to hold the lean while the key is held.
"""
import os, sys
from collections import deque
import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "NewArt")
DST = os.path.join(ROOT, "Godot", "Art")
REF_H = 256

# name -> (file pattern with {a} for idle/left/right, layout)
CHARS = {
    "ren":    ("Ren_Sou_Game_Sprites/Ren_{a}_12frames.png", "strip"),
    "sou":    ("Ren_Sou_Game_Sprites/Sou_{a}_12frames.png", "strip"),
    "haru":   ("Haru_{a}_12frames.png", "strip"),
    "yuki":   ("Yuki_{a}_12frames.png", "strip"),
    "boss1":  ("Boss1_{a}_12frames_4x3.png", "grid"),
    "boss2":  ("Boss2_{a}_12frames_4x3.png", "grid"),
    "boss3":  ("Boss_3_4_Game_Sprites/Boss3_{a}_12frames_4x3.png", "grid"),
    "boss4":  ("Boss_3_4_Game_Sprites/Boss4_{a}_12frames_4x3.png", "grid"),
    "boss5":  ("Boss_5_6_Game_Sprites/Boss5_{a}_12frames_4x3.png", "grid"),
    "boss6":  ("Boss_5_6_Game_Sprites/Boss6_{a}_12frames_4x3.png", "grid"),
    "boss7":  ("Extra_Boss_Game_Sprites/ExtraBoss_{a}_12frames_4x3.png", "grid"),
    "beast1": ("Extra_IF_Boss_Sprites/饕餮_Taotie_{a}_12frames_4x3.png", "grid"),
    "beast2": ("Extra_IF_Boss_Sprites/檮杌_Taowu_{a}_12frames_4x3.png", "grid"),
    "beast3": ("Extra_IF_Boss_Sprites/混沌_Hundun_{a}_12frames_4x3.png", "grid"),
    "beast4": ("Extra_IF_Boss_Sprites/窮奇_Qiongqi_{a}_12frames_4x3.png", "grid"),
}
ANIMS = ("idle", "left", "right")


def split(path, layout):
    im = np.array(Image.open(path).convert("RGBA"))
    H, W = im.shape[:2]
    if layout == "strip":
        fw = W // 12
        return [im[:, i * fw:(i + 1) * fw].copy() for i in range(12)]
    ys = [round(r * H / 3) for r in range(4)]
    xs = [round(c * W / 4) for c in range(5)]
    return [im[ys[r]:ys[r + 1], xs[c]:xs[c + 1]].copy() for r in range(3) for c in range(4)]


def clean_glow(fr):
    """Boss1: a faint grey glow (alpha < 40) surrounds the figure; drop it."""
    a = fr[..., 3]
    a[a < 40] = 0
    return fr


def clean_dust(fr):
    """Extra-IF sheets: near-invisible specks (alpha < 24) all over the canvas."""
    fr[..., 3][fr[..., 3] < 24] = 0
    return fr


def clean_pale_halo(fr):
    """Taotie's left sheet: a soft white halo around a dark figure. Drop see-through pale pixels."""
    a = fr[..., 3].astype(np.int32)
    pale = fr[..., :3].min(axis=2) > 140
    fr[..., 3] = np.where(pale & (a < 235), 0, a).astype(np.uint8)
    return clean_dust(fr)


def clean_soft_halo(fr):
    """Hundun's idle sheet: a white halo around a white figure, so colour can't tell them apart;
    the halo is see-through while the figure and ribbons are nearly opaque. Ramp alpha 150..230."""
    a = fr[..., 3].astype(np.float32)
    fr[..., 3] = (np.clip((a - 150) / 80, 0, 1) * 255).astype(np.uint8)
    return fr


def clean_white(fr):
    """Boss2: an opaque white backdrop. Flood-fill near-white from the border, then
    un-mix white from the fringe next to it so no white halo is left."""
    h, w = fr.shape[:2]
    rgb = fr[..., :3].astype(np.int32)
    whiteish = (rgb.min(axis=2) > 225) & ((rgb.max(axis=2) - rgb.min(axis=2)) < 18)
    bg = np.zeros((h, w), bool)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if whiteish[y, x] and not bg[y, x]: bg[y, x] = True; q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if whiteish[y, x] and not bg[y, x]: bg[y, x] = True; q.append((y, x))
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and not bg[ny, nx] and whiteish[ny, nx]:
                bg[ny, nx] = True; q.append((ny, nx))
    out = fr.astype(np.float32)
    out[..., 3] = np.where(bg, 0, 255)
    # fringe: pixels within 2 px of the background get colour-to-alpha against white
    from scipy.ndimage import binary_dilation
    ring = binary_dilation(bg, iterations=2) & ~bg
    c = out[..., :3] / 255.0
    alpha = np.clip((1.0 - c.min(axis=2)) / 0.35, 0, 1)
    fa = np.where(ring, np.maximum(alpha, 0.0), 1.0)
    un = np.where(fa[..., None] > 0.01, (c - (1 - fa[..., None])) / np.maximum(fa[..., None], 0.01), c)
    out[..., :3] = np.where(ring[..., None], np.clip(un, 0, 1) * 255, out[..., :3])
    out[..., 3] = np.where(ring, fa * 255, out[..., 3])
    return out.astype(np.uint8)


def bbox(fr, thr=24):
    ys, xs = np.nonzero(fr[..., 3] > thr)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def diff(a, b):
    h, w = min(a.shape[0], b.shape[0]), min(a.shape[1], b.shape[1])
    a, b = a[:h, :w], b[:h, :w]
    pa = a[..., :3].astype(np.float32) * a[..., 3:] / 255
    pb = b[..., :3].astype(np.float32) * b[..., 3:] / 255
    return float(np.abs(pa - pb).mean())


def main(names):
    meta = {}
    for name in names:
        pat, layout = CHARS[name]
        frames = {a: split(os.path.join(SRC, pat.format(a=a)), layout) for a in ANIMS}
        if name == "boss1":
            frames = {a: [clean_glow(f) for f in fs] for a, fs in frames.items()}
        if name == "boss2":
            frames = {a: [clean_white(f) for f in fs] for a, fs in frames.items()}
        if name.startswith("beast"):
            frames = {a: [clean_dust(f) for f in fs] for a, fs in frames.items()}
        if name == "beast1":
            frames["left"] = [clean_pale_halo(f) for f in frames["left"]]
        if name == "beast3":
            frames["idle"] = [clean_soft_halo(f) for f in frames["idle"]]
        # all frames of one character share a canvas size within one source sheet; anchor on idle frame 0
        x0, y0, x1, y1 = bbox(frames["idle"][0])
        s = REF_H / (y1 - y0)
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        ext = [0, 0]  # half extents around (cx, cy) in source px
        for fs in frames.values():
            for f in fs:
                bx0, by0, bx1, by1 = bbox(f)
                ext[0] = max(ext[0], cx - bx0, bx1 - cx)
                ext[1] = max(ext[1], cy - by0, by1 - cy)
        cw, ch = int(np.ceil(ext[0] * 2 * s)) + 4, int(np.ceil(ext[1] * 2 * s)) + 4
        sheet = Image.new("RGBA", (cw * 12, ch * 3), (0, 0, 0, 0))
        for r, a in enumerate(ANIMS):
            for c, f in enumerate(frames[a]):
                im = Image.fromarray(f)
                im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
                ox = round(c * cw + cw / 2 - cx * s)
                oy = round(r * ch + ch / 2 - cy * s)
                cell = Image.new("RGBA", (cw * 12, ch * 3), (0, 0, 0, 0))
                cell.paste(im, (ox, oy))
                # keep each frame inside its own cell
                mask = Image.new("L", cell.size, 0)
                mask.paste(255, (c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))
                sheet = Image.composite(cell, sheet, mask)
        sheet.save(os.path.join(DST, f"anim_{name}.png"))
        # lean timing: peak = most different from idle, mid = closest to idle
        idle0 = frames["idle"][0]
        info = {}
        for a in ("left", "right"):
            d = [diff(f, idle0) for f in frames[a]]
            info[a] = (int(np.argmax(d)), int(np.argmin(d)))
        meta[name] = (cw, ch, info)
        print(f"{name}: cell {cw}x{ch}  left peak/mid {info['left']}  right peak/mid {info['right']}")
    return meta


if __name__ == "__main__":
    main(sys.argv[1:] or list(CHARS))
