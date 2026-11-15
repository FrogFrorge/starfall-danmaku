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
    "boss1":        ("TenRouNewSpirit",       6, 6, 12, 3),   # redrawn on the 6x6 layout like the others
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


def thin_columns(weight, thr):
    """Runs of columns with next to nothing in them, as their middles; the sheet edges don't count."""
    W = len(weight)
    runs, st = [], None
    for i, w in enumerate(weight):
        if w <= thr and st is None:
            st = i
        elif w > thr and st is not None:
            runs.append((st, i)); st = None
    if st is not None:
        runs.append((st, W))
    return [(x0 + x1) // 2 for x0, x1 in runs if x0 > 0 and x1 < W]


def count_figures(strip):
    """How many figures are really drawn in this row. 天狼's sheet has eleven where the others have
    twelve, which used to leave a blank frame and made her vanish for a moment in game."""
    lab, n = label(strip[..., 3] > 24)
    if n == 0:
        return 0
    sizes = np.bincount(lab.ravel()); sizes[0] = 0
    return int((sizes > sizes.max() * 0.15).sum())


def split_row(strip, cols):
    """The figures in one source row. They are drawn wider than their slot and drift sideways through
    the animation, and two of them sometimes touch, so the cuts are chosen among the emptiest columns:
    the set that leaves the most even run of figures wins."""
    weight = (strip[..., 3] > 24).sum(0).astype(np.float64)
    W = len(weight)
    step = W / cols
    cand = []
    for thr in (0, weight.max() * 0.02, weight.max() * 0.05, weight.max() * 0.12, weight.max() * 0.25):
        cand = thin_columns(weight, thr)
        if len(cand) >= cols - 1:
            break
    if len(cand) < cols - 1:                       # nothing to go on: fall back to an even split
        cuts = [round(c * step) for c in range(cols + 1)]
    else:
        cand = np.array(cand, float)
        # pick cols-1 of them, in order, so the figures come out as evenly wide as possible
        N = len(cand)
        INF = float("inf")
        cost = np.full((cols, N), INF)
        back = np.zeros((cols, N), int)
        cost[0] = (cand - step) ** 2
        for j in range(1, cols - 1):
            for i in range(N):
                prev = cost[j - 1, :i]
                if len(prev) == 0:
                    continue
                c = prev + (cand[i] - cand[:i] - step) ** 2
                k = int(np.argmin(c))
                cost[j, i] = c[k]; back[j, i] = k
        last = cost[cols - 2] + (W - cand - step) ** 2
        i = int(np.argmin(last))
        picked = [i]
        for j in range(cols - 2, 0, -1):
            i = back[j, i]; picked.append(i)
        cuts = [0] + [int(cand[k]) for k in reversed(picked)] + [W]
    return [trim_strays(strip[:, cuts[i]:cuts[i + 1]].copy()) for i in range(cols)]


def trim_strays(f):
    """Drop anything that is separate from the figure and runs off the edge of its slot: that is a
    piece of the frame drawn next to or above it, which the cut carried over."""
    lab, n = label(f[..., 3] > 0)
    if n <= 1:
        return f
    sizes = np.bincount(lab.ravel()); sizes[0] = 0
    main = sizes.argmax()
    edge = set(lab[:, 0]) | set(lab[:, -1]) | set(lab[0, :]) | set(lab[-1, :])
    keep = np.array([k == main or (k > 0 and k not in edge and sz > sizes[main] * 0.004)
                     for k, sz in enumerate(sizes)])
    f[..., 3] = np.where(keep[lab], f[..., 3], 0)
    return f


# sheets drawn on a pale, half-transparent backdrop instead of on nothing
WASHED = {"boss4", "boss6"}


def strip_wash(im):
    """Clear the pale wash some sheets are drawn over: everything see-through and nearly white that
    joins up with the edge of the sheet. It is what draws a white box around the figure in game."""
    wash = (im[..., 3] < 185) & (im[..., :3].min(axis=2) > 185)
    lab, n = label(wash)
    if n == 0:
        return im
    edge = set(lab[0, :]) | set(lab[-1, :]) | set(lab[:, 0]) | set(lab[:, -1])
    edge.discard(0)
    im[..., 3] = np.where(np.isin(lab, list(edge)), 0, im[..., 3])
    return im


def cut(path, cols, rows, washed=False):
    im = np.array(Image.open(path).convert("RGBA"))
    if washed:
        im = strip_wash(im)
    im[..., 3][im[..., 3] < 24] = 0          # the soft glow baked around every figure
    H = im.shape[0]
    ys = [round(r * H / rows) for r in range(rows + 1)]
    out = []
    for r in range(rows):
        strip = im[ys[r]:ys[r + 1]]
        n = count_figures(strip)
        if not cols - 3 <= n <= cols:        # a wild count means the figures ran together; trust the layout
            n = cols
        if n != cols:
            print(f"  row {r}: {n} figures drawn, not {cols}")
        out.append(split_row(strip, n))
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
    raw_rows = cut(os.path.join(SRC, src), cols, rows, name in WASHED)
    srows = rows // anims                      # source rows per animation (2 on the 6x6 sheets)
    cells = []
    for A in range(anims):
        got = [f for r in range(A * srows, (A + 1) * srows) for f in raw_rows[r]]
        got = [crop(f) for f in got]
        got = [f for f in got if f is not None]
        assert got, (name, A)
        # a row that is short of frames (天狼) is stretched over the sheet's columns instead of leaving a gap
        cells += [got[i * len(got) // per] for i in range(per)]
    assert len(cells) == per * anims, (name, len(cells))

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
