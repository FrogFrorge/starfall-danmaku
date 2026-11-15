"""Cut the five-expression sheets in Downloads into portraits: Godot/Art/portrait_<name>_e<0..4>.png.

Each sheet is five full-body poses on a flat dark background. The background is flood-filled away from the
edges (so dark clothing in the middle survives), each pose is trimmed and scaled to the height of the existing
portraits, and saved on its own.

    python Tools/art/cut_expressions.py
"""
import glob, os
import numpy as np
from PIL import Image
from scipy.ndimage import binary_dilation, label

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ART = os.path.join(ROOT, "Godot", "Art")
SRC = os.path.join(os.path.expanduser("~"), "Downloads")
HEIGHT = 908                      # the height of the portraits already in the game
SHEETS = {"ren": "*_Ren.png", "sou": "*_Sou.png", "haru": "*_Haru.png", "yuki": "*_Yuki.png"}


def cut(path, name):
    im = Image.open(path).convert("RGBA")
    a = np.array(im).astype(np.int16)
    bg = np.median(np.concatenate([a[0, :, :3], a[-1, :, :3], a[:, 0, :3], a[:, -1, :3]]), axis=0)
    near = (np.abs(a[..., :3] - bg).sum(axis=2) < 40)
    # only the background region connected to the border counts, so dark clothing stays
    lab, n = label(near)
    border = set(lab[0, :]) | set(lab[-1, :]) | set(lab[:, 0]) | set(lab[:, -1])
    border.discard(0)
    out = np.array(im)
    out[..., 3] = np.where(np.isin(lab, list(border)), 0, 255)
    # soften the cut by one pixel so it doesn't look stamped out
    solid = out[..., 3] > 0
    edge = binary_dilation(~solid, iterations=1) & solid
    out[..., 3] = np.where(edge, 160, out[..., 3])
    W = im.width // 5
    for i in range(5):
        piece = Image.fromarray(out[:, i * W:(i + 1) * W])
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
