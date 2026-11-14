"""Godot/Art/shot_talisman_green.png: 晴's talisman — the same paper charm as 蓮's, with the red ink turned green.

Only the ink is recoloured (red and green swapped where the pixel is clearly red); the cream paper is left alone.
    python Tools/fx/make_talisman_green.py
"""
import os
import numpy as np
from PIL import Image

ART = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Godot", "Art")
a = np.array(Image.open(os.path.join(ART, "shot_talisman.png")).convert("RGBA")).astype(np.float32)
rgb = a[..., :3]
ink = np.clip((rgb[..., 0] - np.maximum(rgb[..., 1], rgb[..., 2]) - 40) / 60, 0, 1)[..., None]
swapped = rgb[..., [1, 0, 2]]            # red ink becomes green ink
a[..., :3] = rgb * (1 - ink) + swapped * ink
out = os.path.join(ART, "shot_talisman_green.png")
Image.fromarray(a.astype(np.uint8)).save(out)
print("wrote", out)
