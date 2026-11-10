"""Turns the Canva exports in Tools/canva/raw/ into game assets in Art/.
    python Tools/canva/make_assets.py
Expected raw files: doll.png spirit.png option.png bg_stage.png bg_spell.png ui_pattern.png title_art.png
"""
import os
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "raw")
ART = os.path.normpath(os.path.join(HERE, "..", "..", "Art"))


def load(name):
    return Image.open(os.path.join(RAW, name)).convert("RGBA")


def sprite(im, h, pad=6):
    box = im.getchannel("A").point(lambda v: 255 if v > 10 else 0).getbbox()
    im = im.crop(box)
    w = round(im.width * h / im.height)
    im = im.resize((w, h), Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=1.0, percent=50, threshold=2))
    out = Image.new("RGBA", (w + pad * 2, h + pad * 2), (0, 0, 0, 0))
    out.paste(im, (pad, pad))
    return out


def hue_shift(im, shift):
    """Rotate hue of saturated pixels (keeps white paper white)."""
    rgba = np.asarray(im).astype(np.float32) / 255.0
    rgb, a = rgba[..., :3], rgba[..., 3:]
    hsv = np.asarray(Image.fromarray((rgb * 255).astype(np.uint8), "RGB").convert("HSV")).astype(np.float32)
    hsv[..., 0] = (hsv[..., 0] + shift * 255.0) % 255.0
    rgb2 = np.asarray(Image.fromarray(hsv.astype(np.uint8), "HSV").convert("RGB")).astype(np.float32) / 255.0
    out = np.concatenate([rgb2, a], axis=-1)
    return Image.fromarray((out * 255).astype(np.uint8), "RGBA")


def tile_vertical(im, blend_frac=0.18):
    """Make an image wrap seamlessly top<->bottom by cross-fading a band."""
    a = np.asarray(im.convert("RGB")).astype(np.float32)
    H = a.shape[0]
    B = int(H * blend_frac)
    t = a[: H - B].copy()
    w = (np.arange(B, dtype=np.float32) / B)[:, None, None]
    t[:B] = a[:B] * w + a[H - B:] * (1 - w)
    return Image.fromarray(t.clip(0, 255).astype(np.uint8), "RGB")


def tile_both(im, blend_frac=0.12):
    im = tile_vertical(im, blend_frac)
    return tile_vertical(im.transpose(Image.Transpose.ROTATE_90), blend_frac).transpose(Image.Transpose.ROTATE_270)


def save(im, name):
    im.save(os.path.join(ART, name))
    print("wrote", name, im.size)


def lum_alpha(im, size, premult=False, keep_color=False, flip=False):
    """Glow art on black -> transparent sprite. Brightness becomes alpha.
    keep_color=False gives a white sprite that the game tints per bullet."""
    a = np.asarray(im.convert("RGB")).astype(np.float32) / 255.0
    L = a.max(axis=2)
    alpha = np.clip((L - 0.03) / 0.97, 0.0, 1.0)
    if keep_color:
        rgb = a / np.maximum(L, 1e-4)[..., None]
    else:
        rgb = np.ones_like(a)
    if premult:
        rgb = rgb * alpha[..., None]
    out = np.concatenate([rgb, alpha[..., None]], axis=-1)
    img = Image.fromarray((out * 255).clip(0, 255).astype(np.uint8), "RGBA")
    box = img.getchannel("A").point(lambda v: 255 if v > 6 else 0).getbbox()
    img = img.crop(box)
    s = max(img.width, img.height)
    sq = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    sq.paste(img, ((s - img.width) // 2, (s - img.height) // 2))
    sq = sq.resize((size, size), Image.LANCZOS)
    if flip:
        sq = sq.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    return sq


def portrait(im, keep_top=0.74, height=900, pad=4):
    box = im.getchannel("A").point(lambda v: 255 if v > 10 else 0).getbbox()
    im = im.crop(box)
    im = im.crop((0, 0, im.width, int(im.height * keep_top)))
    w = round(im.width * height / im.height)
    im = im.resize((w, height), Image.LANCZOS)
    out = Image.new("RGBA", (w + pad * 2, height + pad * 2), (0, 0, 0, 0))
    out.paste(im, (pad, pad))
    return out


def characters_and_effects():
    # New playable characters: dialogue / select portraits and in-game back views
    for n in ("sou", "haru", "yuki"):
        save(portrait(load(n + ".png")), "portrait_%s.png" % n)
        save(sprite(load(n + "_back.png"), 200), "player_back_%s.png" % n)
    # Bullets (white, tinted in game). Crescent flipped so its convex edge leads.
    save(lum_alpha(load("b_orb.png"), 128), "bullet_orb.png")
    save(lum_alpha(load("b_petal.png"), 128), "bullet_petal.png")
    save(lum_alpha(load("b_star.png"), 128), "bullet_star.png")
    save(lum_alpha(load("b_crescent.png"), 128, flip=True), "bullet_crescent.png")
    # Gold magic circle, premultiplied for additive drawing
    save(lum_alpha(load("circle.png"), 512, premult=True, keep_color=True), "magic_circle.png")
    # Shots, items, icons
    save(sprite(load("shot.png"), 96, pad=2), "shot_talisman.png")
    save(sprite(load("item_power.png"), 48, pad=2), "item_power.png")
    save(sprite(load("item_point.png"), 48, pad=2), "item_point.png")
    save(sprite(load("icon_life.png"), 64, pad=2), "icon_life.png")
    save(sprite(load("icon_bomb.png"), 64, pad=2), "icon_bomb.png")


def stage2():
    # 千秋, the four-seasons painter: dialogue portrait and full-body in-game sprite
    boss = load("boss2.png")
    save(portrait(boss), "portrait_boss2.png")
    save(sprite(boss, 260), "boss2_body.png")
    bg = tile_vertical(load("bg_stage2.png"))
    save(bg.resize((768, round(bg.height * 768 / bg.width)), Image.LANCZOS), "bg_stage2.png")
    for n in ("sakura", "maple", "snow", "water", "fire"):
        save(lum_alpha(load("b_%s.png" % n), 128), "bullet_%s.png" % n)


def later_stages():
    # Stage 3-6 and Extra (stage index 6 -> art number 7): bosses, backgrounds, and two more bullets
    for i in range(3, 8):
        boss = load("boss%d.png" % i)
        save(portrait(boss), "portrait_boss%d.png" % i)
        save(sprite(boss, 260), "boss%d_body.png" % i)
        bg = tile_vertical(load("bg_stage%d.png" % i))
        save(bg.resize((768, round(bg.height * 768 / bg.width)), Image.LANCZOS), "bg_stage%d.png" % i)
    for n in ("chrys", "tile"):
        save(lum_alpha(load("b_%s.png" % n), 128), "bullet_%s.png" % n)



def extra_if():
    # Extra-IF (stage index 7 -> art number 8): the four perils are art indices 7..10 -> portrait_beastN / beastN_body
    for i in range(1, 5):
        beast = load("beast%d.png" % i)
        save(portrait(beast), "portrait_beast%d.png" % i)
        save(sprite(beast, 260), "beast%d_body.png" % i)
    bg = tile_vertical(load("bg_stage8.png"))
    save(bg.resize((768, round(bg.height * 768 / bg.width)), Image.LANCZOS), "bg_stage8.png")


if __name__ == "__main__":
    characters_and_effects()
    doll = sprite(load("doll.png"), 120)
    save(doll, "fairy_blue.png")
    save(hue_shift(doll, 0.40), "fairy_red.png")           # blue ink -> crimson ink
    save(sprite(load("spirit.png"), 170), "spirit.png")
    save(sprite(load("option.png"), 64, pad=3), "option.png")

    stage = tile_vertical(load("bg_stage.png"))
    save(stage.resize((768, round(stage.height * 768 / stage.width)), Image.LANCZOS), "bg_stage.png")
    spell = tile_vertical(load("bg_spell.png"))
    save(spell.resize((768, round(spell.height * 768 / spell.width)), Image.LANCZOS), "bg_spell.png")
    pat = tile_both(load("ui_pattern.png"))
    save(pat.resize((512, 512), Image.LANCZOS), "ui_pattern.png")
    title = load("title_art.png").convert("RGB")
    save(title, "title_art.png")
