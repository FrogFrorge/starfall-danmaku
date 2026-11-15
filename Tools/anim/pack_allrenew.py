"""Pack the redrawn sheets in NewArt/ALLRENEW into the game's animation sheets.

Source layouts (all in NewArt/ALLRENEW):
  6x6   36 frames: rows 1-2 idle, 3-4 left, 5-6 right (12 frames each)   players and bosses
  12x3  36 frames: row 1 idle, row 2 left, row 3 right                   蒼 and 天狼
  8x3   24 frames: row 1 idle, row 2 left, row 3 right                   the blue and red fairies
  4x2    8 frames: idle only                                             the four masks

The figures are drawn wider than their slot and drift sideways through an animation, so a plain grid
cut slices them. Each row is instead split on the empty columns between figures, and every frame is
then lined up on the first idle frame by matching their silhouettes, so the body stays put on screen
while the pose changes.

Output: Godot/Art/anim_<name>.png, rows = idle / left / right (one row for the masks), each frame
scaled so the first idle frame is REF_H tall.

    python Tools/anim/pack_allrenew.py            (everything)
    python Tools/anim/pack_allrenew.py ren sou    (just these)
"""
import os, sys
import numpy as np
from PIL import Image
from scipy.signal import fftconvolve
from scipy.ndimage import label

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "NewArt", "ALLRENEW")
DST = os.path.join(ROOT, "Godot", "Art")
REF_H = 256

# name -> (source file, columns, rows, frames per animation, animations)
SHEETS = {
    "ren":          ("RenNewSpirit",          6, 6, 12, 3),
    "sou":          ("SouNewSpirit",         12, 3, 12, 3),
    "haru":         ("HaruNewSpirit",         6, 6, 12, 3),
    "yuki":         ("YukiNewSpirit",         6, 6, 12, 3),
    "boss1":        ("TenRouNewSpirit",      12, 3, 12, 3),
    "boss2":        ("千秋NewSpirit",          6, 6, 12, 3),
    "boss3":        ("ArashiNewSpirit",       6, 6, 12, 3),
    "boss4":        ("墨竹NewSpirit",          6, 6, 12, 3),
    "boss5":        ("GenNewSpirit",          6, 6, 12, 3),
    "boss6":        ("YohNewSpirit",          6, 6, 12, 3),
    "boss7":        ("IssoNewSpirit.png",     6, 6, 12, 3),
    "beast1":       ("TaoTieNewSpirit.png",   6, 6, 12, 3),
    "beast2":       ("TaoWuNewSpirit.png",    6, 6, 12, 3),
    "beast3":       ("HunDunNewSpirit.png",   6, 6, 12, 3),
    "beast4":       ("QiongQiNewSpirit.png",  6, 6, 12, 3),
    "fairy_blue":   ("BlueMinionNewSpirit",   8, 3,  8, 3),
    "fairy_red":    ("RedMinionNewSpirit",    8, 3,  8, 3),
    "mask_fox":     ("FoxMaskNewSpirit",      4, 2,  8, 1),
    "mask_god":     ("GodMaskNewSpirit",      4, 2,  8, 1),
    "mask_mahjong": ("MahjongMaskNewSpirit",  4, 2,  8, 1),
    "mask_oni":     ("OniMaskNewSpirit",      4, 2,  8, 1),
}


def split_row(strip, cols):
    """The figures in one source row, split on the empty columns between them."""
    solid = (strip[..., 3] > 24).sum(0) > 0
    runs, st = [], None
    for i, s in enumerate(solid):
        if not s and st is None:
            st = i
        elif s and st is not None:
            runs.append((st, i)); st = None
    if st is not None:
        runs.append((st, len(solid)))
    inner = [r for r in runs if r[0] > 0 and r[1] < len(solid)]
    if len(inner) == cols - 1:
        cuts = [0] + [(a + b) // 2 for a, b in inner] + [len(solid)]
    else:                                   # no clean gaps: fall back to an even split
        cuts = [round(c * len(solid) / cols) for c in range(cols + 1)]
    return [trim_strays(strip[:, cuts[i]:cuts[i + 1]].copy()) for i in range(cols)]


def trim_strays(f):
    """Drop anything that is separate from the figure and runs off the side of its slot: that is the
    hair or sleeve of the frame drawn next to it, which the cut carried over."""
    lab, n = label(f[..., 3] > 0)
    if n <= 1:
        return f
    sizes = np.bincount(lab.ravel()); sizes[0] = 0
    main = sizes.argmax()
    edge = set(lab[:, 0]) | set(lab[:, -1])
    keep = np.array([k == main or (k > 0 and k not in edge and sz > sizes[main] * 0.004)
                     for k, sz in enumerate(sizes)])
    f[..., 3] = np.where(keep[lab], f[..., 3], 0)
    return f


def cut(path, cols, rows):
    im = np.array(Image.open(path).convert("RGBA"))
    im[..., 3][im[..., 3] < 24] = 0          # the soft glow baked around every figure
    H = im.shape[0]
    ys = [round(r * H / rows) for r in range(rows + 1)]
    out = []
    for r in range(rows):
        out += split_row(im[ys[r]:ys[r + 1]], cols)
    return out


def crop(f):
    ys, xs = np.nonzero(f[..., 3] > 24)
    if len(xs) == 0:
        return None
    return f[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def scaled(f, s):
    im = Image.fromarray(f)
    return im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)


def align(ref_mask, mask):
    """Where this frame's silhouette sits against the reference one: the shift that matches best."""
    c = fftconvolve(ref_mask, mask[::-1, ::-1], mode="full")
    dy, dx = np.unravel_index(int(np.argmax(c)), c.shape)
    return dx - (mask.shape[1] - 1), dy - (mask.shape[0] - 1)   # ref position of the frame's top-left


def pack(name):
    src, cols, rows, per, anims = SHEETS[name]
    cells = [crop(f) for f in cut(os.path.join(SRC, src), cols, rows)]
    assert len(cells) == per * anims and all(c is not None for c in cells), (name, len(cells))

    s = REF_H / cells[0].shape[0]
    imgs = [scaled(f, s) for f in cells]
    ref = np.asarray(imgs[0])[..., 3].astype(np.float32) / 255.0

    # where each frame's top-left goes, in the reference frame's coordinates
    places = [(0, 0)]
    for im in imgs[1:]:
        places.append(align(ref, np.asarray(im)[..., 3].astype(np.float32) / 255.0))
    x0 = min(p[0] for p in places); y0 = min(p[1] for p in places)
    x1 = max(p[0] + im.width for p, im in zip(places, imgs))
    y1 = max(p[1] + im.height for p, im in zip(places, imgs))
    cw, ch = x1 - x0 + 4, y1 - y0 + 4

    sheet = Image.new("RGBA", (cw * per, ch * anims), (0, 0, 0, 0))
    for i, (im, (px, py)) in enumerate(zip(imgs, places)):
        r, c = divmod(i, per)
        sheet.paste(im, (c * cw + px - x0 + 2, r * ch + py - y0 + 2))
    sheet.save(os.path.join(DST, f"anim_{name}.png"))
    print(f"{name:13s} {per}x{anims}  cell {cw}x{ch}  <- {src}")


if __name__ == "__main__":
    for n in (sys.argv[1:] or list(SHEETS)):
        pack(n)
