"""Cut the five-expression sheets in Downloads into portraits: Godot/Art/portrait_<name>_e<0..4>.png.

Each sheet is five full-body poses on a flat dark background. The background is flood-filled away from the
edges (so dark clothing in the middle survives), each pose is trimmed and scaled to the height of the existing
portraits, and saved on its own.

    python Tools/art/cut_expressions.py
"""
import glob, os
import numpy as np
from PIL import Image
from scipy.ndimage import binary_dilation, binary_erosion, label
from scipy.ndimage import mean as ndmean, standard_deviation as ndstd

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ART = os.path.join(ROOT, "Godot", "Art")
SRC = os.path.join(os.path.expanduser("~"), "Downloads")
HEIGHT = 908                      # the height of the portraits already in the game
SHEETS = {"ren": "*_Ren.png", "sou": "*_Sou.png", "haru": "*_Haru.png", "yuki": "*_Yuki.png"}


def fade_edges(piece, ramp=16):
    """Fade whatever is left at the sides of a pose, so an aura the cut went through trails off
    instead of ending in a straight line."""
    a = piece[..., 3].astype(np.float32)
    w = piece.shape[1]
    k = np.ones(w, np.float32)
    r = min(ramp, w // 3)
    k[:r] = np.linspace(0, 1, r)
    k[-r:] = np.linspace(1, 0, r)
    piece[..., 3] = (a * k).astype(np.uint8)
    return piece


# 蒼's and 雪's sheets have dark navy shapes behind the figure that are part of the backdrop, not the
# costume; 蓮's navy hakama is close enough in colour that the same rule would eat holes in it, so the
# rule is only used where it is needed.
NAVY_BACKDROP = {"sou", "yuki"}


def cut(path, name):
    im = Image.open(path).convert("RGBA")
    a = np.array(im).astype(np.int16)
    bg = np.median(np.concatenate([a[0, :, :3], a[-1, :, :3], a[:, 0, :3], a[:, -1, :3]]), axis=0)
    # Background is either the flat dark colour or the half-transparent dark haze drawn around some of
    # the figures (蒼's and 雪's "wings", which show up in game as a black block). The figures themselves
    # are fully opaque, so anything see-through counts as background too.
    dist = np.abs(a[..., :3] - bg).sum(axis=2).astype(np.float32)
    # the backdrop is not quite flat: it is lighter around each figure, and those patches show up in
    # game as dark blocks beside 蒼 and 雪. They are still the same dark navy — blue above everything
    # else — while his robe and 雪's hakama are near-black but warm, and 蓮's navy hakama is far brighter.
    navy = (a[..., 2] - np.maximum(a[..., 0], a[..., 1]) > 8) & (a[..., :3].max(axis=2) < 75)
    near = (dist < 40) | (a[..., 3] < 200)
    if name in NAVY_BACKDROP:
        near |= navy & (dist < 60)
    # only the background region connected to the border counts, so dark clothing stays
    lab, n = label(near)
    border = set(lab[0, :]) | set(lab[-1, :]) | set(lab[:, 0]) | set(lab[:, -1])
    border.discard(0)
    hole = np.isin(lab, list(border))
    # 蓮's hakama pleats are drawn in the background colour, so the flood runs up into the robe and
    # shreds it. Only wide stretches are really background: open the mask so thin runs are dropped.
    R = 6
    hole = binary_dilation(binary_erosion(hole, iterations=R, border_value=1), iterations=R)
    # Pockets of background the opening sealed off: under an arm, inside the curve of 蒼's bow, between
    # strands of hair. They show up in game as a black block, so they go — but only the chunky ones.
    # Anything thin is 蓮's pleats again, drawn in the background colour, and has to stay.
    pocket = near & ~hole
    plab, pn = label(pocket)
    if pn:
        chunky = set(np.unique(plab[binary_erosion(pocket, iterations=4)]))
        chunky.discard(0)
        if chunky:
            # real background is flat: its colour barely moves. Dark cloth, which is just as dark,
            # is shaded and folded, so it varies — that is how 蒼's robe stays and the pockets go.
            ids = sorted(chunky)
            avg = np.array(ndmean(dist, plab, ids))
            dev = np.array(ndstd(dist, plab, ids))
            size = np.bincount(plab.ravel())
            flat = [i for i, m, s in zip(ids, avg, dev) if m < 14 and s < 9 and size[i] > 2500]
            if flat:
                hole |= np.isin(plab, flat)
    out = np.array(im)
    out[..., 3] = np.where(hole, 0, 255)
    # soften the cut by one pixel so it doesn't look stamped out
    solid = out[..., 3] > 0
    edge = binary_dilation(~solid, iterations=1) & solid
    out[..., 3] = np.where(edge, 160, out[..., 3])
    # Where to cut between poses: the emptiest column near each fifth, because 蒼's and 雪's dark aura
    # spills into the next pose's space and an even cut leaves it standing as a black block.
    weight = (out[..., 3] > 0).sum(0).astype(float)
    W = im.width // 5
    cuts = [0]
    for b in range(1, 5):
        lo, hi = int(b * W - W * 0.12), int(b * W + W * 0.12)
        win = weight[lo:hi]
        best = np.flatnonzero(win == win.min()) + lo
        cuts.append(int(best[np.argmin(np.abs(best - b * W))]))
    cuts.append(im.width)
    for i in range(5):
        piece = Image.fromarray(fade_edges(out[:, cuts[i]:cuts[i + 1]].copy()))
        box = piece.getbbox()
        if box is None:
            continue
        # drop slivers of the neighbouring pose that cross the column edge
        pa = np.array(piece)
        lab2, n2 = label(pa[..., 3] > 0)
        if n2 > 1:
            sizes = np.bincount(lab2.ravel()); sizes[0] = 0
            touch = set(lab2[:, 0]) | set(lab2[:, -1])        # pieces of the neighbouring pose hang off the edges
            keep = np.array([k > 0 and (sz > sizes.max() * 0.3 or (k not in touch and sz > sizes.max() * 0.004))
                             for k, sz in enumerate(sizes)])
            pa[..., 3] = np.where(keep[lab2], pa[..., 3], 0)
            piece = Image.fromarray(pa)
            if piece.getbbox() is None:
                continue
        piece = piece.crop(piece.getbbox())
        s = HEIGHT / piece.height
        piece = piece.resize((max(1, round(piece.width * s)), HEIGHT), Image.LANCZOS)
        piece.save(os.path.join(ART, f"portrait_{name}_e{i}.png"))
    print(name, "5 expressions,", W, "px wide each")


if __name__ == "__main__":
    for name, pat in SHEETS.items():
        hits = glob.glob(os.path.join(SRC, pat))
        if not hits:
            print("no sheet for", name); continue
        cut(hits[0], name)
