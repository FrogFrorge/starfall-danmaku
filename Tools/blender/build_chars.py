"""Builds and renders the cel-shaded characters.
Usage (from the project folder):
  blender -b --factory-startup -P Tools/blender/build_chars.py -- <what>
  <what> = ren_portrait | sirius_portrait | ren_sprite | sirius_sprite | test_ren_head | all
Raw renders go to Tools/blender/renders/, then Tools/blender/finish_art.py turns them into Art/*.png.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib
import toon_lib as T
importlib.reload(T)

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "renders")
os.makedirs(RAW, exist_ok=True)

SKIN = (255, 230, 215)
SKIN_SH = (242, 190, 178)
LINE = (34, 22, 40)


# Animation state used while building: hair / sleeve tips sway with `phase`,
# `lean` (-1..1, + = moving screen-right) makes them trail and tilts the body.
# `sx` = world X direction of screen-right for the current camera (+1 front view, -1 back view).
ANIM = {"phase": 0.0, "lean": 0.0, "amp": 0.0, "sx": 1.0}


def anim_pts(pts, k, kind="hair"):
    amp, lean = ANIM["amp"], ANIM["lean"]
    if amp == 0 and lean == 0:
        return pts
    big = 1.5 if kind == "sleeve" else 1.0
    n = len(pts)
    out = []
    for i, p in enumerate(pts):
        w = (i / (n - 1)) ** 1.5
        sway = amp * 0.12 * big * math.sin(ANIM["phase"] + k * 0.9) * w
        trail = -lean * ANIM["sx"] * 0.45 * big * w
        lift = abs(lean) * 0.15 * w if kind == "sleeve" else abs(lean) * 0.05 * w
        out.append((p[0] + sway + trail, p[1] + 0.05 * amp * math.cos(ANIM["phase"] + k) * w, p[2] + lift))
    return out


def skin():
    return T.toon("Skin", SKIN, SKIN_SH, shadow_at=0.07)


# ========================================================================================
# Shared parts
# ========================================================================================
def head_shape(co):
    x, y, z = co
    if z < 0.2:
        t = (0.2 - z) / 1.2
        x *= 1 - 0.5 * t ** 1.4
        y *= 1 - 0.22 * t
        y -= 0.10 * t * t
    if y < 0:
        y *= 0.84
    return Vector((x, y, z))


def make_head(objs, face_tex, line=LINE, ears=False):
    head = T.uv_sphere("Head", (0, 0, 0), (0.88, 0.86, 1.0), 64, 40, head_shape)
    T.set_mat(head, T.toon("Face", SKIN, T.shade(SKIN, 0.97), shadow_at=0.02), line, 0.018)
    objs.append(head)
    if ears:
        for side in (-1, 1):
            ear = T.uv_sphere("Ear", (side * 0.84, 0.05, -0.12), (0.1, 0.07, 0.2))
            T.set_mat(ear, skin(), line, 0.012)
            objs.append(ear)
    decal = T.face_decal("Face", head, os.path.join(T.TEX, face_tex), (0, 0, 0.02), width=1.62)
    objs.append(decal)
    neck = T.lathe("Neck", [(0.25, -0.6), (0.26, -1.0), (0.3, -1.45)], sy=0.9)
    T.set_mat(neck, T.toon("SkinNeck", SKIN_SH, T.shade(SKIN_SH, 0.9)), line, 0.012)
    objs.append(neck)
    return head


def hair_cap(objs, mat, line, keep_front_z=0.55, scale=(0.95, 0.93, 1.06), nape=-0.75):
    cap = T.uv_sphere("HairCap", (0, 0.02, 0.02), scale, 64, 40, head_shape)
    bm = bmesh.new()
    bm.from_mesh(cap.data)
    kill = [v for v in bm.verts if (v.co.y < -0.15 and v.co.z < keep_front_z) or v.co.z < nape or (v.co.y < 0.25 and v.co.z < -0.25)]
    bmesh.ops.delete(bm, geom=kill, context="VERTS")
    bm.to_mesh(cap.data)
    bm.free()
    T.set_mat(cap, mat, line, 0.02)
    objs.append(cap)
    return cap


def add_strands(objs, mat, line, specs, thick=0.016, name="Hair"):
    for i, spec in enumerate(specs):
        pts, r0 = anim_pts(spec[0], i, "hair"), spec[1]
        power = spec[2] if len(spec) > 2 else 0.85
        s = T.strand("%s%d" % (name, i), pts, r0, 0.0, power=power)
        T.set_mat(s, mat, line, thick)
        objs.append(s)


def tube(objs, name, pts, r0, r1, mat, line, thick=0.018, flat_axis=None, flat=1.0):
    if name.startswith(("Sleeve", "Tassel")):
        pts = anim_pts(pts, len(name) * 3 + ord(name[-1]), "sleeve")
    s = T.strand(name, pts, r0, r1, power=1.0, flat_axis=flat_axis, flat=flat)
    T.set_mat(s, mat, line, thick)
    objs.append(s)
    return s


def ball(objs, name, loc, scale, mat, line, thick=0.014):
    b = T.uv_sphere(name, loc, scale, 32, 20)
    T.set_mat(b, mat, line, thick)
    objs.append(b)
    return b


def coat(objs, name, profile, mat, line, sy=0.62, open_front=0.0, thickness=0.035, thick=0.022, deform=None):
    c = T.lathe(name, profile, sy=sy, open_front=open_front, deform=deform, seg=64)
    if thickness:
        so = c.modifiers.new("Thick", "SOLIDIFY")
        so.thickness = thickness
        so.offset = 0.0
    T.set_mat(c, mat, line, thick)
    objs.append(c)
    return c


def edge_line(profile, angle_deg, sy, side, out=1.02):
    a = math.radians(angle_deg)
    return [(side * math.sin(a) * r * out, -math.cos(a) * r * sy * out, z) for (r, z) in profile]


def front_y(profile, z, sy, extra=0.0):
    pts = sorted(profile, key=lambda p: p[1])
    for (r0, z0), (r1, z1) in zip(pts, pts[1:]):
        if z0 <= z <= z1:
            t = (z - z0) / (z1 - z0) if z1 != z0 else 0
            return -(r0 + (r1 - r0) * t) * sy - extra
    return -pts[-1][0] * sy - extra


def group(objs, start, loc=(0, 0, 0), scale=1.0):
    """Parent objs[start:] to an empty so a whole body can be moved/scaled together."""
    g = bpy.data.objects.new("Group%d" % start, None)
    bpy.context.collection.objects.link(g)
    for o in objs[start:]:
        if o.parent is None:
            o.parent = g
    g.location = loc
    g.scale = (scale, scale, scale)
    objs.append(g)
    return g


def finish(objs, yaw=0.0):
    root = bpy.data.objects.new("Root", None)
    bpy.context.collection.objects.link(root)
    T.parent_all(root, objs)
    tilt = ANIM["lean"] * ANIM["sx"] * 9.0
    root.rotation_euler = (0, math.radians(tilt), math.radians(yaw))
    root.location = (0, 0, 0.05 * ANIM["amp"] * math.sin(ANIM["phase"] * 2))
    return root


# ========================================================================================
# Ren - young shrine exorcist (player). Colours: white haori, indigo, red accents.
# ========================================================================================
def ren_mats():
    return dict(
        hair=T.toon("RenHair", (44, 48, 88), (26, 28, 56), (115, 135, 210), shadow_at=0.36, high_at=0.965),
        white=T.toon("RenWhite", (246, 246, 252), (196, 202, 228), shadow_at=0.3),
        indigo=T.toon("RenIndigo", (52, 64, 140), (32, 38, 92), shadow_at=0.3),
        trim=T.toon("RenTrim", (70, 92, 190), (44, 56, 130), shadow_at=0.3),
        red=T.toon("RenRed", (220, 48, 62), (150, 26, 42), shadow_at=0.3),
        dark=T.toon("RenDark", (34, 34, 60), (20, 20, 40), shadow_at=0.3),
        gold=T.toon("RenGold", (255, 210, 110), (210, 150, 60), shadow_at=0.3),
    )


def ren_hair(objs, M):
    line = (18, 16, 34)
    hair_cap(objs, M["hair"], line)
    S = []
    # Bangs: crown -> forward/down, tips stopping around the brows
    for x, tipz, sway, r in [(-0.62, 0.2, -0.12, 0.17), (-0.4, 0.1, -0.05, 0.19), (-0.15, 0.06, 0.04, 0.2), (0.1, 0.16, 0.1, 0.19),
                             (0.34, 0.08, 0.08, 0.19), (0.58, 0.22, 0.14, 0.17), (-0.02, 0.28, 0.0, 0.18)]:
        S.append(([(x * 0.5, 0.05, 1.05), (x * 0.95, -0.62, 0.92), (x * 1.04 + sway * 0.5, -0.86, 0.55), (x + sway, -0.84, tipz)], r))
    for side in (-1, 1):
        S.append(([(side * 0.7, -0.2, 0.7), (side * 0.9, -0.42, 0.25), (side * 0.88, -0.48, -0.2), (side * 0.78, -0.45, -0.6)], 0.15))
        S.append(([(side * 0.8, 0.0, 0.6), (side * 0.98, -0.15, 0.1), (side * 0.96, -0.18, -0.42)], 0.15))
    for k in range(9):
        a = math.radians(-80 + k * 20)
        dx, dy = math.sin(a), math.cos(a)
        up = 0.3 + 0.2 * math.cos(a * 2)
        S.append(([(dx * 0.3, dy * 0.2, 0.95), (dx * 0.9, dy * 0.85, 0.7), (dx * 1.3, dy * 1.1, 0.25 + up * 0.4), (dx * 1.55, dy * 1.2, 0.05 + up)], 0.24))
    for k in range(7):
        a = math.radians(-60 + k * 20)
        dx, dy = math.sin(a), math.cos(a)
        S.append(([(dx * 0.4, dy * 0.4, 0.6), (dx * 1.0, dy * 0.9, 0.1), (dx * 1.15, dy * 1.05, -0.35), (dx * 1.3, dy * 1.2, -0.62)], 0.22))
    S.append(([(0.05, -0.1, 1.0), (0.1, -0.15, 1.35), (0.35, -0.1, 1.5), (0.45, 0.0, 1.32)], 0.07))
    add_strands(objs, M["hair"], line, S)


def ren_body(objs, M, raised_arm=True, back_crest=False):
    line = LINE
    if back_crest:
        star_mesh(objs, "BackCrest", (0, 0.9, -2.35), 0.42, M["trim"], line, points=6, inner=0.5)
        star_mesh(objs, "BackCrestIn", (0, 0.95, -2.35), 0.18, M["gold"], None, points=6, inner=0.5)
    # Inner kimono (indigo) torso
    inner = [(0.3, -1.3), (0.6, -1.5), (0.95, -1.75), (1.0, -2.3), (0.9, -3.1), (0.88, -3.8), (0.98, -4.8), (1.05, -5.6), (1.1, -7.0)]
    coat(objs, "Kimono", inner, M["indigo"], line, sy=0.62, thickness=0)
    # White under-collar crossing left over right
    for side, ex in ((1, 0.07), (-1, 0.1)):
        pts = [(side * 0.34, 0.08, -1.24), (side * 0.4, front_y(inner, -1.5, 0.62, ex) * 0.8, -1.5)]
        for k, z in enumerate([-1.85, -2.25, -2.65]):
            x = side * (0.3 - k * 0.2)
            pts.append((x, front_y(inner, z, 0.62, ex), z))
        tube(objs, "Collar%d" % side, pts, 0.085, 0.08, M["white"], line, flat_axis=(0, 1, 0), flat=0.4)
    # Haori (open front)
    hp = [(0.42, -1.32), (0.85, -1.5), (1.32, -1.82), (1.38, -2.4), (1.3, -3.2), (1.36, -4.2), (1.45, -5.6), (1.52, -7.0)]
    coat(objs, "Haori", hp, M["white"], line, sy=0.62, open_front=24)
    for side in (-1, 1):
        tube(objs, "Trim%d" % side, edge_line(hp, 24, 0.62, side, 1.03), 0.085, 0.09, M["trim"], line, flat_axis=(0, 1, 0), flat=0.5)
    # Haori cord (himo) with tassel
    for side in (-1, 1):
        tube(objs, "Cord%d" % side, [(side * 0.62, -0.9, -2.65), (side * 0.3, -0.98, -2.78), (0, -0.99, -2.82)], 0.045, 0.045, M["red"], line)
    ball(objs, "Knot", (0, -1.0, -2.82), (0.11, 0.08, 0.11), M["red"], line)
    for side in (-1, 1):
        tube(objs, "Tassel%d" % side, [(side * 0.03, -1.0, -2.9), (side * 0.06, -1.02, -3.25), (side * 0.07, -1.0, -3.55)], 0.04, 0.05, M["red"], line)

    # Character's right arm (image left): hangs at the side inside a big kimono sleeve
    tube(objs, "SleeveR", [(-1.25, -0.05, -1.85), (-1.62, -0.2, -2.6), (-1.75, -0.25, -3.5), (-1.7, -0.25, -4.3)], 0.46, 0.62, M["white"], line,
         flat_axis=(0, 1, 0), flat=0.55)
    tube(objs, "SleeveRTrim", [(-1.45, -0.42, -4.1), (-1.72, -0.48, -4.35), (-2.0, -0.4, -4.2)], 0.06, 0.06, M["trim"], line)

    if raised_arm:
        # Character's left arm raised, holding talismans beside the face
        tube(objs, "UpperArmL", [(1.2, -0.05, -1.85), (1.6, -0.25, -2.3), (1.85, -0.45, -2.55)], 0.34, 0.3, M["white"], line, flat_axis=(0, 1, 0), flat=0.8)
        tube(objs, "ForeArmL", [(1.85, -0.45, -2.55), (1.95, -0.75, -1.9), (1.75, -1.0, -1.05)], 0.27, 0.24, M["white"], line)
        # Hanging sleeve drape under the forearm
        tube(objs, "SleeveL", [(1.95, -0.55, -2.3), (2.25, -0.4, -2.9), (2.35, -0.3, -3.6)], 0.4, 0.55, M["white"], line, flat_axis=(1, 0, 0), flat=0.45)
        tube(objs, "SleeveLTrim", [(1.85, -0.9, -1.2), (1.75, -1.02, -1.18), (1.62, -1.06, -1.25)], 0.05, 0.05, M["trim"], line)
        ball(objs, "HandL", (1.66, -1.12, -0.88), (0.2, 0.16, 0.22), skin(), line)
        ofuda = T.image_mat("Ofuda", os.path.join(T.TEX, "ofuda.png"))
        for k, (dx, rot) in enumerate([(-0.18, 14), (0.0, 0), (0.18, -14)]):
            o = T.box("Ofuda%d" % k, (0.34, 0.01, 1.0), (1.62 + dx, -1.25 - k * 0.01, -0.35), (0, rot, 0))
            # UV the front face to the talisman texture
            me = o.data
            if not me.uv_layers:
                me.uv_layers.new()
            uvl = me.uv_layers.active.data
            for poly in me.polygons:
                for li in poly.loop_indices:
                    vco = me.vertices[me.loops[li].vertex_index].co
                    uvl[li].uv = (vco.x / 0.34 + 0.5, vco.z / 1.0 + 0.5)
            o.data.materials.append(ofuda)
            objs.append(o)
    else:
        tube(objs, "SleeveL", [(1.25, -0.05, -1.85), (1.62, -0.2, -2.6), (1.75, -0.25, -3.5), (1.7, -0.25, -4.3)], 0.46, 0.62, M["white"], line,
             flat_axis=(0, 1, 0), flat=0.55)


def ren_portrait(expr="normal"):
    T.reset()
    objs = []
    M = ren_mats()
    make_head(objs, "face_ren_%s.png" % expr)
    ren_hair(objs, M)
    start = len(objs)
    ren_body(objs, M)
    group(objs, start, (0, 0, 0.315), 0.95)
    finish(objs, yaw=14)
    T.add_sun((40, 0, -12))
    T.add_camera(-1.75, 7.6, yaw_deg=0, pitch_deg=3)
    T.render(os.path.join(RAW, "ren_portrait_%s.png" % expr), 1000, 1500)


def test_ren_head():
    T.reset()
    objs = []
    M = ren_mats()
    make_head(objs, "face_ren_normal.png")
    ren_hair(objs, M)
    T.add_sun((52, 0, -40))
    T.add_camera(0.1, 3.4, yaw_deg=-18, pitch_deg=4)
    T.render(os.path.join(RAW, "test_ren_head.png"), 800, 800)


# ========================================================================================
# Sirius - star-reading onmyoji (boss). Colours: white kariginu, violet, black eboshi, silver hair.
# ========================================================================================
def sirius_mats():
    return dict(
        hair=T.toon("SirHair", (222, 226, 244), (165, 170, 210), (255, 255, 255), shadow_at=0.36, high_at=0.96),
        white=T.toon("SirWhite", (248, 247, 252), (200, 196, 226), shadow_at=0.3),
        violet=T.toon("SirViolet", (118, 70, 196), (72, 40, 132), shadow_at=0.3),
        lilac=T.toon("SirLilac", (196, 168, 240), (140, 112, 196), shadow_at=0.3),
        navy=T.toon("SirNavy", (36, 30, 70), (22, 18, 44), shadow_at=0.3),
        black=T.toon("SirBlack", (36, 34, 46), (18, 16, 24), (90, 86, 120), shadow_at=0.3, high_at=0.95),
        gold=T.toon("SirGold", (255, 214, 120), (214, 156, 64), shadow_at=0.3),
    )


def star_mesh(objs, name, loc, size, mat, line=None, points=4, inner=0.38, rot_y=0.0, depth=0.03):
    bm = bmesh.new()
    verts_f, verts_b = [], []
    for i in range(points * 2):
        r = size if i % 2 == 0 else size * inner
        a = math.pi / 2 + i * math.pi / points
        verts_f.append(bm.verts.new((math.cos(a) * r, -depth, math.sin(a) * r)))
        verts_b.append(bm.verts.new((math.cos(a) * r, depth, math.sin(a) * r)))
    bm.faces.new(verts_f[::-1])
    bm.faces.new(verts_b)
    n = len(verts_f)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((verts_f[i], verts_f[j], verts_b[j], verts_b[i]))
    o = T.mesh_from_bm(name, bm)
    o.location = loc
    o.rotation_euler = (0, math.radians(rot_y), 0)
    T.set_mat(o, mat, line, 0.01 if line else 0)
    objs.append(o)
    return o


def fan_mesh(objs, name, pivot, radius, a0, a1, mat, rim_mat, line, tilt=(0, 0, 0)):
    bm = bmesh.new()
    n = 16
    inner_r = radius * 0.28
    outer, innerv = [], []
    for i in range(n + 1):
        a = math.radians(a0 + (a1 - a0) * i / n)
        fold = 0.035 if i % 2 else -0.035
        outer.append(bm.verts.new((math.cos(a) * radius, fold, math.sin(a) * radius)))
        innerv.append(bm.verts.new((math.cos(a) * inner_r, 0, math.sin(a) * inner_r)))
    for i in range(n):
        bm.faces.new((innerv[i], innerv[i + 1], outer[i + 1], outer[i]))
    o = T.mesh_from_bm(name, bm)
    so = o.modifiers.new("Thick", "SOLIDIFY")
    so.thickness = 0.02
    T.set_mat(o, mat, line, 0.014)
    o.location = pivot
    o.rotation_euler = tuple(math.radians(v) for v in tilt)
    objs.append(o)
    # gold rim + gold stars on the fan
    arc = [(math.cos(math.radians(a0 + (a1 - a0) * i / 8)) * radius, -0.03, math.sin(math.radians(a0 + (a1 - a0) * i / 8)) * radius) for i in range(9)]
    rim = T.strand(name + "Rim", arc, 0.03, 0.03, power=1.0)
    T.set_mat(rim, rim_mat, line, 0.008)
    rim.parent = o
    objs.append(rim)
    for k, (fa, fr, sz) in enumerate([(0.3, 0.7, 0.09), (0.55, 0.55, 0.06), (0.75, 0.78, 0.07), (0.45, 0.85, 0.05)]):
        a = math.radians(a0 + (a1 - a0) * fa)
        st = star_mesh(objs, name + "Star%d" % k, (math.cos(a) * radius * fr, -0.06, math.sin(a) * radius * fr), sz, rim_mat)
        st.parent = o
    return o


def sirius_hair(objs, M, long_back=True):
    line = (90, 86, 130)
    hair_cap(objs, M["hair"], line, keep_front_z=0.5)
    S = []
    # Centre-parted bangs
    for side in (-1, 1):
        for x, tipz, r in [(0.14, 0.18, 0.2), (0.36, 0.02, 0.21), (0.6, -0.1, 0.2)]:
            S.append(([(side * 0.04, -0.2, 1.04), (side * x * 1.1, -0.78, 0.86), (side * (x * 1.35 + 0.08), -0.88, 0.45), (side * (x * 1.45 + 0.18), -0.8, tipz)], r))
        # long face-framing locks falling over the chest
        S.append(([(side * 0.55, -0.45, 0.8), (side * 0.9, -0.55, 0.1), (side * 0.95, -0.6, -1.0), (side * 0.85, -0.7, -2.2), (side * 0.75, -0.75, -2.9)], 0.17, 0.7))
        S.append(([(side * 0.7, -0.3, 0.7), (side * 1.0, -0.35, -0.1), (side * 1.12, -0.4, -1.4), (side * 1.05, -0.45, -2.6)], 0.17, 0.7))
    if long_back:
        for k in range(11):
            a = math.radians(-100 + k * 20)
            dx, dy = math.sin(a), math.cos(a)
            S.append(([(dx * 0.3, dy * 0.3, 0.95), (dx * 0.95, dy * 0.95, 0.4), (dx * 1.15, dy * 1.1, -0.8), (dx * 1.3, dy * 1.3, -2.4), (dx * 1.25, dy * 1.4, -3.6)], 0.27, 0.6))
    add_strands(objs, M["hair"], line, S)


def eboshi(objs, M, scale=1.0):
    line = (16, 14, 22)
    prof = [(0.001, 2.3), (0.22, 2.27), (0.4, 2.17), (0.52, 2.0), (0.6, 1.75), (0.66, 1.45), (0.73, 1.15), (0.82, 0.85), (0.9, 0.62), (0.93, 0.45), (0.93, 0.35)]
    def lean(co, a):
        t = max(0.0, (co.z - 0.4) / 2.0)
        return Vector((co.x * scale, co.y * scale + 0.22 + t * t * 0.35, co.z + 0.18))
    hat = T.lathe("Eboshi", prof, sx=1.0, sy=0.95, deform=lean, seg=48)
    T.set_mat(hat, M["black"], line, 0.022)
    objs.append(hat)
    band = T.lathe("EboshiBand", [(0.965, 0.73), (0.965, 0.6)], sy=0.97, seg=48, loc=(0, 0.22, 0))
    T.set_mat(band, M["violet"], line, 0.014)
    objs.append(band)


def sirius_body(objs, M):
    line = LINE
    inner = [(0.3, -1.3), (0.6, -1.5), (0.98, -1.78), (1.04, -2.3), (0.95, -3.1), (0.95, -3.9), (1.1, -5.0), (1.25, -7.0)]
    coat(objs, "Inner", inner, M["navy"], line, sy=0.62, thickness=0)
    # Lilac under-collar V
    for side, ex in ((1, 0.07), (-1, 0.1)):
        pts = [(side * 0.32, 0.08, -1.24), (side * 0.38, front_y(inner, -1.5, 0.62, ex) * 0.8, -1.5)]
        for k, z in enumerate([-1.85, -2.25, -2.65]):
            pts.append((side * (0.28 - k * 0.19), front_y(inner, z, 0.62, ex), z))
        tube(objs, "Collar%d" % side, pts, 0.085, 0.08, M["lilac"], line, flat_axis=(0, 1, 0), flat=0.4)
    # Kariginu: white outer robe, open front, with a raised round collar
    kp = [(0.45, -1.3), (0.9, -1.48), (1.42, -1.8), (1.45, -2.4), (1.38, -3.2), (1.45, -4.3), (1.6, -5.6), (1.7, -7.0)]
    coat(objs, "Kariginu", kp, M["white"], line, sy=0.64, open_front=26)
    for side in (-1, 1):
        tube(objs, "Trim%d" % side, edge_line(kp, 26, 0.64, side, 1.03), 0.08, 0.09, M["violet"], line, flat_axis=(0, 1, 0), flat=0.5)
    collar = T.lathe("RoundCollar", [(0.46, -1.0), (0.52, -1.15), (0.58, -1.35)], sy=0.85, open_front=34, seg=48)
    so = collar.modifiers.new("Thick", "SOLIDIFY")
    so.thickness = 0.05
    T.set_mat(collar, M["violet"], line, 0.016)
    objs.append(collar)
    # Kikutoji (decorative tassels) down the front
    for k, z in enumerate([-2.4, -3.3]):
        for side in (-1, 1):
            ball(objs, "Kiku%d_%d" % (k, side), (side * 0.62, front_y(kp, z, 0.64, 0.1), z), (0.09, 0.06, 0.09), M["violet"], line, 0.01)

    # Big sleeves, violet outside with white lining, stars
    for side in (-1, 1):
        sl = tube(objs, "Sleeve%d" % side, [(side * 1.3, -0.05, -1.85), (side * 1.78, -0.15, -2.6), (side * 1.95, -0.2, -3.6), (side * 1.9, -0.2, -4.8)],
                  0.5, 0.72, M["violet"], line, flat_axis=(0, 1, 0), flat=0.5)
        tube(objs, "SleeveLining%d" % side, [(side * 1.45, -0.5, -4.4), (side * 1.85, -0.55, -4.75), (side * 2.25, -0.45, -4.5)], 0.09, 0.09, M["white"], line)
        for k, (dz, dx) in enumerate([(-2.6, 0.0), (-3.3, 0.25), (-3.9, -0.15), (-4.4, 0.2)]):
            star_mesh(objs, "SleeveStar%d_%d" % (side, k), (side * (1.82 + dx), -0.62, dz), 0.1, M["lilac"])
    # Hand holding the open fan near the face (character's right = image left)
    ball(objs, "Hand", (-1.25, -1.2, -1.95), (0.19, 0.16, 0.22), skin(), line)
    tube(objs, "Forearm", [(-1.75, -0.6, -2.85), (-1.5, -0.95, -2.4), (-1.3, -1.15, -2.05)], 0.26, 0.22, M["violet"], line)
    fan_mesh(objs, "Fan", (-1.25, -1.28, -1.9), 1.05, 70, 175, M["navy"], M["gold"], line,
             tilt=(0, 10 + 7 * ANIM["amp"] * math.sin(ANIM["phase"]), 6))


def sirius_portrait(expr="normal"):
    T.reset()
    objs = []
    M = sirius_mats()
    make_head(objs, "face_sirius_%s.png" % expr)
    sirius_hair(objs, M)
    eboshi(objs, M)
    start = len(objs)
    sirius_body(objs, M)
    group(objs, start, (0, 0, 0.315), 0.95)
    # floating stars around him
    for k, (x, z, s) in enumerate([(2.1, 1.6, 0.22), (2.5, 0.6, 0.12), (-2.2, 2.2, 0.15), (2.0, 2.6, 0.1), (-2.6, 0.9, 0.1), (2.7, -0.4, 0.08)]):
        star_mesh(objs, "Float%d" % k, (x, 0.5, z), s, T.flat("StarGlow", (255, 236, 170)))
    finish(objs, yaw=-14)
    T.add_sun((40, 0, 12))
    T.add_camera(-1.0, 9.6, yaw_deg=0, pitch_deg=3)
    T.render(os.path.join(RAW, "sirius_portrait_%s.png" % expr), 1000, 1500)


# ========================================================================================
# Chibi in-game sprites
# ========================================================================================
def chibi_render(name, build_head, build_body, body_scale=0.5, yaw=0.0, pitch=14, extra=None, cam_z=-1.0, cam_scale=6.4,
                 cam_yaw=0.0, res=(640, 800), samples=None):
    T.reset()
    T.OUTLINE_SCALE[0] = 2.6
    objs = []
    build_head(objs)
    start = len(objs)
    build_body(objs)
    # body top (-1.3) -> -0.78
    group(objs, start, (0, 0, -0.78 + 1.3 * body_scale), body_scale)
    if extra:
        extra(objs)
    finish(objs, yaw=yaw)
    T.add_sun((40, 0, -10 + cam_yaw))
    T.add_camera(cam_z, cam_scale, yaw_deg=cam_yaw, pitch_deg=pitch)
    if samples:
        bpy.context.scene.eevee.taa_render_samples = samples
    T.render(os.path.join(RAW, name), res[0], res[1])
    T.OUTLINE_SCALE[0] = 1.0


def ren_sprite():
    def head(objs):
        make_head(objs, "face_ren_normal.png")
        ren_hair(objs, ren_mats())
    chibi_render("ren_sprite.png", head, lambda objs: ren_body(objs, ren_mats(), raised_arm=False))


def sirius_sprite():
    def head(objs):
        make_head(objs, "face_sirius_normal.png")
        sirius_hair(objs, sirius_mats(), long_back=True)
        eboshi(objs, sirius_mats())
    def body(objs):
        sirius_body(objs, sirius_mats())
    chibi_render("sirius_sprite.png", head, body, body_scale=0.5, cam_z=-0.4, cam_scale=7.6)


def shikigami(name, mark):
    """Paper-doll familiar (hitogata) used as a small enemy."""
    T.reset()
    T.OUTLINE_SCALE[0] = 2.0
    pts2d = [(0, 1.0), (0.3, 0.92), (0.36, 0.6), (0.2, 0.46), (0.95, 0.3), (0.9, 0.12), (0.28, 0.16), (0.42, -0.9), (0.14, -0.9), (0.0, -0.35),
             (-0.14, -0.9), (-0.42, -0.9), (-0.28, 0.16), (-0.9, 0.12), (-0.95, 0.3), (-0.2, 0.46), (-0.36, 0.6), (-0.3, 0.92)]
    bm = bmesh.new()
    vf = [bm.verts.new((x, -0.03, z)) for x, z in pts2d]
    vb = [bm.verts.new((x, 0.03, z)) for x, z in pts2d]
    bm.faces.new(vf[::-1])
    bm.faces.new(vb)
    for i in range(len(vf)):
        j = (i + 1) % len(vf)
        bm.faces.new((vf[i], vf[j], vb[j], vb[i]))
    o = T.mesh_from_bm("Doll", bm)
    T.set_mat(o, T.toon("Paper", (252, 249, 238), (220, 212, 190), shadow_at=0.3), (40, 30, 40), 0.02)
    mk = T.toon("Mark" + name, mark, T.shade(mark, 0.7), shadow_at=0.3)
    ball([], "Eye", (0, -0.06, 0.72), (0.12, 0.02, 0.12), mk, None)
    for k, (w, z) in enumerate([(0.42, 0.22), (0.3, 0.02), (0.42, -0.18)]):
        b = T.box("Mark%d" % k, (w, 0.02, 0.07), (0, -0.06, z))
        T.set_mat(b, mk)
    o.rotation_euler = (0, math.radians(8), 0)
    T.add_sun((40, 0, -15))
    T.add_camera(0, 2.3, 0, 0)
    T.render(os.path.join(RAW, name), 320, 320)
    T.OUTLINE_SCALE[0] = 1.0


def ren_sheet():
    """In-game player sprite: seen from behind (facing up the screen), 3 rows x 8 frames:
    row 0 idle, row 1 moving left, row 2 moving right (first 4 = transition, last 4 = loop)."""
    def head(objs):
        make_head(objs, "face_ren_normal.png")
        ren_hair(objs, ren_mats())
    body = lambda objs: ren_body(objs, ren_mats(), raised_arm=False, back_crest=True)
    for row in range(3):
        for col in range(8):
            ANIM["phase"] = 2 * math.pi * col / 8
            ANIM["amp"] = 1.0
            ANIM["sx"] = -1.0
            if row == 0:
                ANIM["lean"] = 0.0
            else:
                ANIM["lean"] = (-1 if row == 1 else 1) * (min(1.0, (col + 1) / 4))
            chibi_render("sheet/ren_%d_%d.png" % (row, col), head, body, cam_yaw=180, pitch=22, res=(320, 400), samples=16)
    ANIM.update(phase=0.0, lean=0.0, amp=0.0, sx=1.0)


def sirius_sheet():
    """Boss sprite, front view, 8 idle frames (hair, sleeves and fan sway)."""
    def head(objs):
        make_head(objs, "face_sirius_normal.png")
        sirius_hair(objs, sirius_mats(), long_back=True)
        eboshi(objs, sirius_mats())
    for col in range(8):
        ANIM.update(phase=2 * math.pi * col / 8, amp=1.0, lean=0.0, sx=1.0)
        chibi_render("sheet/sirius_0_%d.png" % col, head, lambda objs: sirius_body(objs, sirius_mats()), body_scale=0.5,
                     cam_z=-0.4, cam_scale=7.6, res=(320, 400), samples=16)
    ANIM.update(phase=0.0, lean=0.0, amp=0.0, sx=1.0)


def sheets():
    ren_sheet()
    sirius_sheet()


def enemies():
    shikigami("fairy_blue.png", (60, 110, 230))
    shikigami("fairy_red.png", (215, 45, 60))


def all():
    for f in (lambda: ren_portrait("normal"), lambda: sirius_portrait("normal"), ren_sprite, sirius_sprite, enemies):
        f()


# ========================================================================================
if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    what = argv[0] if argv else "test_ren_head"
    args = argv[1:]
    globals()[what](*args)
