"""Per-sprite settings for make_sheets.py.

zones : hair regions in UV (u0, u1, v0, v1, strength); the root is at v0, the tip at v1.
        Kept away from hands, fans, the umbrella and the bow so those never move.
hair  : reference hair colours (RGB); only pixels close to one of them inside a zone move.
tol   : colour distance that still counts as hair.
cloth : (core, top, amp) — the robe flutters outside a central core of half-width `core` (in U, from the centre line)
        and below `top` (in V), plus the hem at the bottom; `amp` scales the flutter (1 = normal).
keep  : rectangles (u0, u1, v0, v1) that must stay rigid (props held in hands, floating talismans, the bow, ...).
"""

SPECS = {
    # player characters (back view)
    "player_back":      dict(zones=[(0.40, 0.62, 0.02, 0.18, 0.5)], hair=[(40, 60, 140), (25, 35, 90)], tol=60,
                             cloth=(0.12, 0.22, 1.2), keep=[(0.74, 1.00, 0.00, 0.20)]),
    "player_back_sou":  dict(zones=[(0.44, 0.66, 0.06, 0.46, 1.3)], hair=[(30, 26, 28), (55, 45, 45)], tol=50,
                             cloth=(0.10, 0.40, 1.2), keep=[(0.00, 0.38, 0.00, 1.00)]),
    "player_back_haru": dict(zones=[(0.40, 0.62, 0.00, 0.18, 0.5)], hair=[(150, 110, 60), (190, 150, 95)], tol=60,
                             cloth=(0.13, 0.20, 1.2), keep=[(0.02, 0.20, 0.16, 0.32), (0.82, 1.00, 0.36, 0.52), (0.08, 0.26, 0.66, 0.80)]),
    "player_back_yuki": dict(zones=[(0.36, 0.62, 0.00, 0.20, 0.5)], hair=[(225, 225, 235)], tol=40,
                             cloth=(0.10, 0.22, 1.3), keep=[]),
    # bosses (front view)
    "boss_body":  dict(zones=[(0.12, 0.36, 0.14, 0.55, 1.0)], hair=[(222, 222, 235)], tol=28,
                       cloth=(0.14, 0.30, 1.0), keep=[(0.08, 0.40, 0.08, 0.34)]),
    "boss2_body": dict(zones=[(0.38, 0.50, 0.06, 0.25, 0.7), (0.52, 0.76, 0.08, 0.30, 1.0)], hair=[(45, 70, 45), (70, 100, 60)], tol=45,
                       cloth=(0.14, 0.28, 1.0), keep=[(0.00, 0.22, 0.10, 0.80)]),
    "boss3_body": dict(zones=[(0.55, 0.82, 0.10, 0.45, 1.0)], hair=[(170, 185, 210), (200, 210, 230)], tol=38,
                       cloth=(0.14, 0.34, 1.0), keep=[(0.00, 0.62, 0.00, 0.30)]),
    "boss4_body": dict(zones=[(0.38, 0.48, 0.08, 0.35, 0.7), (0.53, 0.62, 0.10, 0.32, 0.7)], hair=[(30, 35, 35)], tol=45,
                       cloth=(0.14, 0.30, 1.0), keep=[(0.64, 0.98, 0.24, 0.48)]),
    "boss5_body": dict(zones=[(0.40, 0.47, 0.08, 0.30, 0.6), (0.55, 0.62, 0.10, 0.33, 0.6)], hair=[(30, 25, 40)], tol=30,
                       cloth=(0.14, 0.30, 1.0), keep=[]),
    "boss6_body": dict(zones=[(0.06, 0.30, 0.20, 0.75, 1.0), (0.70, 0.95, 0.20, 0.75, 1.0)], hair=[(242, 236, 218)], tol=22,
                       cloth=(0.16, 0.34, 0.9), keep=[(0.25, 0.75, 0.00, 0.14)]),
    "boss7_body": dict(zones=[(0.38, 0.45, 0.05, 0.20, 0.5), (0.50, 0.57, 0.08, 0.28, 0.6)], hair=[(30, 25, 28)], tol=32,
                       cloth=(0.14, 0.30, 1.0), keep=[]),
    # Extra-IF beasts: hair, plus 檮杌's tail, 混沌's ribbons and 窮奇's wings
    "beast1_body": dict(zones=[(0.33, 0.45, 0.06, 0.25, 0.7), (0.55, 0.68, 0.06, 0.25, 0.7)], hair=[(35, 30, 25)], tol=35,
                        cloth=(0.15, 0.30, 1.0), keep=[]),
    "beast2_body": dict(zones=[(0.12, 0.40, 0.04, 0.40, 1.0), (0.58, 0.85, 0.04, 0.42, 1.0), (0.62, 0.92, 0.45, 0.85, 1.2)],
                        hair=[(225, 225, 225), (190, 140, 70), (40, 30, 25)], tol=32,
                        cloth=(0.15, 0.32, 0.9), keep=[]),
    "beast3_body": dict(zones=[(0.00, 0.30, 0.08, 0.90, 1.2), (0.70, 1.00, 0.08, 0.90, 1.2), (0.40, 0.50, 0.05, 0.45, 0.6)],
                        hair=[(230, 232, 240), (30, 28, 32)], tol=32,
                        cloth=(0.14, 0.30, 1.0), keep=[]),
    "beast4_body": dict(zones=[(0.00, 0.25, 0.05, 0.75, 0.9), (0.75, 1.00, 0.05, 0.75, 0.9)], hair=[(40, 45, 80), (70, 75, 110)], tol=40,
                        cloth=(0.16, 0.30, 0.9), keep=[]),
}

COLS = 12          # animation phases per pose (one full sway cycle)
POSES = 5          # lean -2 .. +2 (row 0 = leaning hard left)
PAD_X = 0.14       # padding around each frame, fraction of the sprite size
PAD_Y = 0.05
