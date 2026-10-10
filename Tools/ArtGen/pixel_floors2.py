"""Phase 2: Belial's and Lilith's floors.

Belial's Stage: the Mask Imp (Mammon's little devil in silver, a smiling mask over half his face) and the False Prophet (a white
robe, a porcelain mask, a halo, a scroll). Lilith's Night: the Moth Imp (lilac, pale moth wings, feelers) and the Night Nurse
(a dark veil, a lamp turned low). Their halls, the two maps' backdrops (Ui/chapter_map_belial, Ui/chapter_map_lilith) and the new
strangers (Art/Events/<id>.png, 48x48). One palette (pixel.py), no anti-aliasing.
"""
import math
import os

import numpy as np

import pixel_demons as D
import pixel_salons as S
import pixel_floors as F
from pixel import C, Img, RAMP_GOLD, RAMP_LILAC, RAMP_SILVER, bezier, sheet
from pixel_layers import Particles, write

SIZE = 96
W, H = 480, 270


# ================================================================== the imps

def _mask(img, p, hx, hy):
    """Belial's imp: a smiling bone mask over the left half of his face."""
    mask = img.m_ellipse(42 + hx, 47 + hy, 9, 11) & img.m_rect(30 + hx, 34 + hy, 48 + hx, 60 + hy)
    img.paint(mask, C.BONE)
    img.inner_outline(mask, C.BONE_DARK)
    img.paint(img.m_rect(39 + hx, 44 + hy, 44 + hx, 45 + hy), C.BLACK)                 # the mask's eye slit
    img.put(42 + hx, 44 + hy, C.LILAC_LIGHT)
    smile = img.m_line(bezier([(36 + hx, 52 + hy), (40 + hx, 56 + hy), (47 + hx, 55 + hy)], 8), 1)
    img.paint(smile, C.CRIMSON)
    img.put(37 + hx, 49 + hy, C.CRIMSON)                                                  # a painted tear of a cheek


def _feelers(img, p, hx, hy):
    """Lilith's imp: two feathery moth feelers, swaying a pixel."""
    sway = 1 if p["t"] % 2 else 0
    for s in (-1, 1):
        stalk = img.m_line(bezier([(48 + s * 4 + hx, 34 + hy), (48 + s * 9 + hx, 22 + hy), (48 + s * 16 + hx + s * sway, 14 + hy)], 10), 1)
        img.paint(stalk, C.BONE_DARK)
        for k in range(3):
            img.put(48 + s * (11 + k * 2) + hx + s * sway, 18 + hy - k * 2, C.BONE_MID)


BELIAL_IMP = {"skin": RAMP_SILVER, "dark": C.SILVER_DARK, "mid": C.SILVER, "light": C.WHITE, "wings": [C.NIGHT, C.PLUM, C.VIOLET],
              "vein": C.NIGHT, "inner_ear": C.PLUM, "cheek": C.LILAC, "iris": C.LILAC_LIGHT, "accent": C.PLUM,
              "flash_map": {C.SILVER: C.CRIMSON, C.WHITE: C.RED, C.SILVER_DARK: C.BLOOD}, "extra": _mask}

LILITH_IMP = {"skin": RAMP_LILAC, "dark": C.VIOLET, "mid": C.MAUVE, "light": C.LILAC_LIGHT, "wings": [C.BONE_SHADE, C.BONE_DARK, C.BONE_MID],
              "vein": C.BONE_SHADE, "inner_ear": C.PLUM, "cheek": C.CRIMSON, "iris": C.EMBER, "accent": C.VIOLET,
              "flash_map": {C.LILAC: C.CRIMSON, C.LILAC_LIGHT: C.RED, C.MAUVE: C.BLOOD}, "extra": _feelers}


def belial_imp(p):
    return F.imp(p, BELIAL_IMP)


def lilith_imp(p):
    return F.imp(p, LILITH_IMP)


# ================================================================== the wardens

def _robed(p, robe, trim, face_fill, accent, flash_map, prop_left, prop_right, face, halo=None):
    """A tall robed warden in the Collector's shape: the robe and its trim, a prop in each arm, a hood with a face of its own."""
    p = D.defaults(p)
    img = Img(SIZE, SIZE)
    ox, oy = p["dx"], p["dy"]
    hx, hy = ox, oy + p["hdy"]
    t = p["t"]

    body = img.m_poly([(4, 97), (12, 74), (28, 62 + oy), (68, 62 + oy), (84, 74), (92, 97)])
    img.shade(body, robe, shadow=2)
    img.paint(img.m_line([(48 + ox, 64 + oy), (48 + ox, 97)], 1), trim)
    prop_left(img, ox, oy, t)
    prop_right(img, ox, oy, t)

    hood = img.m_poly([(24 + hx, 68 + hy), (28 + hx, 36 + hy), (40 + hx, 20 + hy), (48 + hx, 12 + hy), (56 + hx, 20 + hy), (68 + hx, 36 + hy),
                       (72 + hx, 68 + hy)])
    img.shade(hood, robe, shadow=2)
    img.inner_outline(hood, trim)
    hollow = img.m_ellipse(48 + hx, 46 + hy, 13, 16)
    img.paint(hollow, face_fill)
    face(img, p, hx, hy, hollow)
    if halo is not None:
        ring = img.m_ellipse(48 + hx, 9 + hy, 14, 4) & ~img.m_ellipse(48 + hx, 9 + hy, 11, 2.4)
        for y, x in zip(*np.nonzero(ring)):
            img.put(x, y, halo[0] if (x + t) % 3 else halo[1])

    if p["mouth"] == "laugh" or p["eyes"] == "squint":
        for i in range(3):
            S.sparkle(img, 14 + i * 34, (t * 9 + i * 15) % 34 + 4)
    if p["steam"] is not None:
        D.steam(img, 34 + hx, 16 + hy, p["steam"] % 3)
        D.steam(img, 62 + hx, 16 + hy, (p["steam"] + 1) % 3)
    if p["sparkle"]:
        D.sparkle(img, 56 + hx, 42 + hy)
    return D.finish(img, p, accent, flash_map=flash_map)


def _scroll(img, ox, oy, t):
    """The prophet's scroll of prophecies, rolled, a red seal hanging from it."""
    scroll = img.m_rect(8 + ox, 76 + oy, 30 + ox, 86 + oy)
    img.shade(scroll, [C.BONE_DARK, C.BONE_MID, C.BONE], shadow=1)
    for x in (8, 30):
        img.paint(img.m_rect(x - 1 + ox, 74 + oy, x + 1 + ox, 88 + oy), C.GOLD_DARK)
    for k in range(3):
        img.paint(img.m_rect(12 + ox, 79 + oy + k * 2, 26 + ox, 79 + oy + k * 2), C.BONE_SHADE)
    img.paint(img.m_ellipse(20 + ox, 90 + oy, 2.5, 2.5), C.CRIMSON)
    img.paint(img.m_ellipse(28 + ox, 86 + oy, 4, 3), C.BONE_MID)


def _open_hand(img, ox, oy, t):
    """The prophet's raised hand, two fingers up — a blessing, or a lie."""
    img.paint(img.m_ellipse(70 + ox, 80 + oy, 4, 3), C.BONE_MID)
    img.paint(img.m_rect(68 + ox, 72 + oy - (1 if t % 4 in (1, 2) else 0), 69 + ox, 78 + oy), C.BONE_MID)
    img.paint(img.m_rect(71 + ox, 71 + oy - (1 if t % 4 in (1, 2) else 0), 72 + ox, 78 + oy), C.BONE_MID)


def _porcelain(img, p, hx, hy, hollow):
    """A smooth porcelain mask in the hood: two narrow slits for eyes (lilac light behind them), a thin painted smile."""
    mask = img.m_ellipse(48 + hx, 46 + hy, 11, 14)
    img.shade(mask, [C.BONE_DARK, C.BONE_MID, C.BONE], shadow=1)
    img.paint(img.m_line([(52 + hx, 34 + hy), (50 + hx, 41 + hy), (53 + hx, 47 + hy)], 1) & mask, C.BONE_SHADE)   # the crack
    kind = "angry" if p["brows"] == "angry" else p["eyes"]
    for x in (43, 53):
        if kind == "blink":
            img.paint(img.m_rect(x - 2 + hx, 44 + hy, x + 2 + hx, 44 + hy), C.BONE_SHADE)
            continue
        img.paint(img.m_rect(x - 2 + hx, 44 + hy, x + 2 + hx, 45 + hy), C.BLACK)
        img.put(x + hx, 44 + hy, C.WHITE if kind == "glow" or p["glow"] >= 2 else C.LILAC_LIGHT)
        if kind == "angry":
            img.put(x - 2 + hx if x < 48 else x + 2 + hx, 43 + hy, C.BLACK)
    mouth = p["mouth"]
    width = 8 if mouth in ("laugh", "grin") else 6
    y = 53 + hy + (1 if mouth in ("talk2", "laugh") else 0)
    img.paint(img.m_rect(48 - width // 2 + hx, y, 48 + width // 2 + hx, y), C.CRIMSON)
    img.put(48 - width // 2 + hx, y - 1, C.CRIMSON)
    img.put(48 + width // 2 + hx, y - 1, C.CRIMSON)


def prophet(p):
    return _robed(p, [C.BONE_SHADE, C.BONE_DARK, C.BONE_MID, C.BONE], C.GOLD, C.BLACK, C.PLUM,
                  {C.BONE: C.HELL, C.BONE_MID: C.RED, C.BONE_DARK: C.CRIMSON, C.GOLD: C.EMBER},
                  _scroll, _open_hand, _porcelain, halo=(C.GOLD, C.GOLD_LIGHT))


def _lamp(img, ox, oy, t):
    """The nurse's lamp, turned low, held in the crook of her arm; its light breathes a pixel."""
    img.paint(img.m_rect(17 + ox, 66 + oy, 18 + ox, 72 + oy), C.BONE_SHADE)
    lamp = img.m_poly([(12 + ox, 72 + oy), (24 + ox, 72 + oy), (22 + ox, 86 + oy), (14 + ox, 86 + oy)])
    img.paint(lamp, C.GOLD_DARK)
    glass = img.m_rect(15 + ox, 75 + oy, 21 + ox, 83 + oy)
    img.paint(glass, C.AMBER if t % 4 in (1, 2) else C.ORANGE)
    img.put(18 + ox, 79 + oy, C.EMBER)
    img.paint(img.m_ellipse(26 + ox, 84 + oy, 4, 3), C.LILAC)


def _vial(img, ox, oy, t):
    """A little vial of something to make you sleep."""
    img.paint(img.m_ellipse(70 + ox, 84 + oy, 4, 3), C.LILAC)
    vial = img.m_rect(70 + ox, 74 + oy, 74 + ox, 81 + oy)
    img.paint(vial, C.VIOLET)
    img.paint(img.m_rect(71 + ox, 77 + oy, 73 + ox, 80 + oy), C.MAUVE if t % 2 else C.LILAC)
    img.paint(img.m_rect(71 + ox, 72 + oy, 73 + ox, 73 + oy), C.BONE_DARK)


def _veiled(img, p, hx, hy, hollow):
    """Under the veil: a pale band across the brow, two half-closed lilac eyes, a calm mouth."""
    img.paint(img.m_rect(36 + hx, 33 + hy, 60 + hx, 36 + hy) & hollow, C.BONE)
    img.put(48 + hx, 34 + hy, C.CRIMSON)
    img.put(47 + hx, 35 + hy, C.CRIMSON)
    img.put(49 + hx, 35 + hy, C.CRIMSON)
    face = img.m_ellipse(48 + hx, 48 + hy, 9, 11) & hollow & ~img.m_rect(30 + hx, 30 + hy, 66 + hx, 36 + hy)
    img.shade(face, [C.VIOLET, C.MAUVE, C.LILAC], shadow=1)
    kind = "angry" if p["brows"] == "angry" else p["eyes"]
    for x, inner in ((44, False), (52, True)):
        D.eye(img, x + hx, 45 + hy, "squint" if kind == "open" else kind, C.LILAC_LIGHT, p["glow"], inner_left=inner, lid=C.VIOLET)
    D.mouth(img, 48 + hx, 52 + hy, p["mouth"], width=6, teeth=C.BONE, lips=C.PLUM)


def nurse(p):
    return _robed(p, [C.BLACK, C.NIGHT, C.VIOLET, C.MAUVE], C.LILAC, C.BLACK, C.VIOLET,
                  {C.MAUVE: C.HELL, C.VIOLET: C.RED, C.LILAC: C.EMBER, C.NIGHT: C.BLOOD},
                  _lamp, _vial, _veiled)


FACES = {"imp_belial": belial_imp, "prophet": prophet, "imp_lilith": lilith_imp, "nurse": nurse}


def write_faces(out_dir):
    written = []
    for face, draw in FACES.items():
        folder = os.path.join(out_dir, face)
        os.makedirs(folder, exist_ok=True)
        for state in D.STATES:
            path = os.path.join(folder, state + ".png")
            sheet([draw(pose) for pose in D.poses(state)]).save(path)
            written.append(path)
    return written


# ================================================================== the halls

def _curtains(img, x0, x1, dark, mid, light):
    """A stage curtain hanging from the top between x0 and x1: folds every 8 px."""
    for x in range(x0, x1 + 1):
        fold = (x - x0) % 8
        color = dark if fold in (0, 7) else light if fold in (3, 4) else mid
        img.paint(img.m_rect(x, 0, x, H - 1), color)


def belial_imp_hall_base():
    """Backstage: red curtains at both sides, a row of masks on the dark plank wall, footlights along the floor."""
    img = Img(W, H)
    S.stone_wall(img, C.NIGHT, C.DUSK, C.BLACK, seed=113, row_h=12)
    _curtains(img, 0, 52, C.BLOOD_DARK, C.BLOOD, C.CRIMSON)
    _curtains(img, 428, W - 1, C.BLOOD_DARK, C.BLOOD, C.CRIMSON)
    img.paint(img.m_rect(0, 0, W - 1, 12), C.BLOOD_DARK)
    for x in range(0, W, 10):
        img.paint(img.m_poly([(x, 12), (x + 10, 12), (x + 5, 18)]), C.CRIMSON)
    for x0 in (62, 360):
        for i in range(4):
            cx, cy = x0 + 8 + i * 15, 40 + (i % 2) * 14
            face = img.m_ellipse(cx, cy, 5, 6)
            img.paint(face, C.BONE if i % 2 else C.SILVER)
            img.paint(img.m_rect(cx - 3, cy - 1, cx - 2, cy - 1), C.BLACK)
            img.paint(img.m_rect(cx + 2, cy - 1, cx + 3, cy - 1), C.BLACK)
            img.paint(img.m_line(bezier([(cx - 3, cy + 3), (cx, cy + (5 if i % 2 else 1)), (cx + 3, cy + 3)], 6), 1), C.CRIMSON)
    for x in range(64, 420, 24):
        img.paint(img.m_rect(x, 258, x + 6, 262), C.GOLD_DARK)
        img.paint(img.m_rect(x + 1, 256, x + 5, 257), C.AMBER)
    S.readable_middle(img)
    return img


def belial_imp_hall():
    layers = [S.glint_layer(f"glint{i}", x, y, offset=(i * 5) % S.LAYER_FRAMES, colors=(C.WHITE, C.LILAC_LIGHT, C.LILAC), vignette=S.readable_middle)
              for i, (x, y) in enumerate(((70, 40), (114, 54), (368, 40), (412, 54), (100, 256), (380, 256)))]
    particles = [Particles("motes", *S.LEFT_LOW, 6), Particles("motes", *S.RIGHT_SIDE, 8)]
    return belial_imp_hall_base(), layers, particles


def prophet_hall_base():
    """The stage door: two white columns, a sunburst painted badly on the backdrop, candles on the steps."""
    img = Img(W, H)
    S.stone_wall(img, C.NIGHT, C.DUSK, C.BLACK, seed=127, row_h=22)
    for cx in (40, 440):
        col = img.m_rect(cx - 14, 20, cx + 14, H - 1)
        img.shade(col, [C.BONE_SHADE, C.BONE_DARK, C.BONE_MID, C.BONE], shadow=2)
        for x in range(cx - 10, cx + 11, 6):
            img.paint(img.m_rect(x, 24, x, H - 1), C.BONE_DARK)
        img.paint(img.m_rect(cx - 18, 14, cx + 18, 20), C.GOLD_DARK)
    for k in range(12):
        a = math.pi * (k + 0.5) / 12
        for r in range(70, 120, 2):
            x, y = 240 + math.cos(a) * r * 1.6, 120 - math.sin(a) * r
            img.put(int(x), int(y), C.GOLD_DARK if k % 2 else C.DUSK)
    for x in (86, 112, 368, 394):
        img.paint(img.m_rect(x, 238, x + 3, 250), C.BONE)
        img.put(x + 1, 236, C.AMBER)
        img.put(x + 1, 235, C.EMBER)
    S.readable_middle(img)
    return img


def prophet_hall():
    layers = [S.glint_layer(f"glint{i}", x, y, offset=(i * 3 + 1) % S.LAYER_FRAMES, vignette=S.readable_middle)
              for i, (x, y) in enumerate(((22, 17), (58, 17), (422, 17), (458, 17), (87, 235), (395, 235)))]
    particles = [Particles("embers", *S.LEFT_LOW, 5), Particles("motes", *S.RIGHT_SIDE, 8)]
    return prophet_hall_base(), layers, particles


def lilith_imp_hall_base():
    """A moonlit violet room: a round window with the moon in it, ivy on the walls, candles where the moths gather."""
    img = Img(W, H)
    S.stone_wall(img, C.NIGHT, C.VIOLET, C.BLACK, seed=139, row_h=16)
    for cx in (60, 420):
        window = img.m_ellipse(cx, 60, 34, 34)
        img.paint(window, C.BLACK)
        img.dither(window, C.BLACK, C.PLUM, 0.3)
        img.paint(img.m_ellipse(cx + 8, 50, 10, 10), C.LILAC_LIGHT)
        img.paint(img.m_ellipse(cx + 12, 47, 9, 9), C.BLACK)
        img.inner_outline(window, C.MAUVE)
        img.paint(img.m_rect(cx - 1, 26, cx, 94) & window, C.MAUVE)
        img.paint(img.m_rect(cx - 34, 59, cx + 34, 60) & window, C.MAUVE)
    rng = np.random.default_rng(14)
    for x0 in (0, 410):
        for _ in range(140):
            x, y = int(rng.integers(x0, x0 + 70)), int(rng.integers(100, H))
            img.put(x, y, C.GREEN_DARK if rng.integers(0, 3) else C.GREEN)
    for x in (100, 120, 360, 380):
        img.paint(img.m_rect(x, 230, x + 3, 248), C.BONE_MID)
        img.put(x + 1, 228, C.AMBER)
    S.readable_middle(img)
    return img


def lilith_imp_hall():
    layers = [S.glint_layer(f"glint{i}", x, y, offset=(i * 4 + 2) % S.LAYER_FRAMES, colors=(C.WHITE, C.LILAC_LIGHT, C.LILAC), vignette=S.readable_middle)
              for i, (x, y) in enumerate(((101, 222), (121, 218), (361, 222), (381, 218), (68, 50), (428, 50)))]
    particles = [Particles("wisps", *S.LEFT_LOW, 6), Particles("wisps", 344, 150, 136, 96, 6)]
    return lilith_imp_hall_base(), layers, particles


def nurse_hall_base():
    """The night ward: a long dark room, rows of narrow beds under grey sheets, a lamp hanging low over each row."""
    img = Img(W, H)
    S.stone_wall(img, C.BLACK, C.NIGHT, C.BLACK, seed=151, row_h=20)
    img.dither(np.ones((H, W), bool), C.BLACK, C.NIGHT, np.clip((img.ys - 40) / 300.0, 0, 0.4))
    for x0 in (6, 380):
        for k in range(3):
            y = 150 + k * 36
            frame = img.m_rect(x0, y, x0 + 92, y + 16)
            img.paint(frame, C.SILVER_DARK)
            sheet_ = img.m_rect(x0 + 2, y - 6, x0 + 90, y + 6)
            img.shade(sheet_, [C.BONE_SHADE, C.BONE_DARK, C.BONE_MID], shadow=1)
            img.paint(img.m_ellipse(x0 + 12, y - 4, 7, 4), C.BONE)
            img.paint(img.m_rect(x0, y + 16, x0 + 2, y + 24), C.SILVER_DARK)
            img.paint(img.m_rect(x0 + 90, y + 16, x0 + 92, y + 24), C.SILVER_DARK)
    for cx in (52, 426):
        img.paint(img.m_rect(cx, 0, cx, 30), C.BONE_SHADE)
        lamp = img.m_poly([(cx - 7, 30), (cx + 7, 30), (cx + 4, 38), (cx - 4, 38)])
        img.paint(lamp, C.GOLD_DARK)
        img.paint(img.m_rect(cx - 3, 38, cx + 3, 40), C.AMBER)
        img.dither(img.m_ellipse(cx, 70, 40, 30) & (img.px == C.BLACK), C.BLACK, C.VIOLET, 0.25)
    S.readable_middle(img)
    return img


def nurse_hall():
    layers = [S.glint_layer(f"glint{i}", x, y, offset=(i * 6) % S.LAYER_FRAMES, colors=(C.WHITE, C.AMBER, C.ORANGE), vignette=S.readable_middle)
              for i, (x, y) in enumerate(((52, 39), (426, 39)))]
    particles = [Particles("wisps", *S.LEFT_LOW, 5), Particles("wisps", *S.RIGHT_SIDE, 7)]
    return nurse_hall_base(), layers, particles


HALLS = {"imp_belial": belial_imp_hall, "prophet": prophet_hall, "imp_lilith": lilith_imp_hall, "nurse": nurse_hall}


def write_halls(out_dir):
    written = []
    for hall, build in HALLS.items():
        base, layers, particles = build()
        written += write(os.path.join(out_dir, hall), base, layers, particles, S.VARIANT_TABLES, text_zones=S.TABLE_TEXT)
    return written


# ================================================================== the maps

def chapter_map_belial():
    """Down through Belial's theatre: a dark house, red curtains along the shaft's edges, a spotlight's pool at the bottom."""
    img = Img(W, H)
    full = np.ones((H, W), bool)
    depth = np.clip((img.ys - 20) / 250.0, 0, 1)
    img.dither(full, C.BLACK, C.NIGHT, 0.2 + 0.45 * depth)
    glow = np.clip(1 - np.hypot((img.xs - 298) / 170.0, (img.ys - 300) / 90.0), 0, 1)
    img.dither(glow > 0, C.NIGHT, C.BLOOD_DARK, glow * 0.55)
    rng = np.random.default_rng(23)
    for _ in range(22):
        x, y = int(rng.integers(142, 464)), int(rng.integers(8, 250))
        img.put(x, y, C.SILVER_DARK if rng.integers(0, 3) else C.LILAC)
    for x0, x1 in ((124, 136), (468, W - 1)):
        for x in range(x0, x1 + 1):
            fold = (x - x0) % 6
            img.paint(img.m_rect(x, 0, x, H - 1), C.BLOOD_DARK if fold in (0, 5) else C.CRIMSON if fold in (2, 3) else C.BLOOD)
        img.paint(img.m_rect(x0, 0, x0, H - 1), C.BLACK)
    img.dither(img.m_ellipse(298, 262, 70, 22), C.NIGHT, C.BONE_SHADE, 0.45)
    img.dither(img.m_ellipse(298, 266, 40, 10), C.BONE_SHADE, C.BONE_DARK, 0.4)
    return img


def chapter_map_lilith():
    """Down into Lilith's night: violet dark, stars, thorned vines along the edges, the moon's pale glow at the bottom."""
    img = Img(W, H)
    full = np.ones((H, W), bool)
    depth = np.clip((img.ys - 20) / 250.0, 0, 1)
    img.dither(full, C.BLACK, C.VIOLET, 0.15 + 0.35 * depth)
    glow = np.clip(1 - np.hypot((img.xs - 298) / 180.0, (img.ys - 300) / 95.0), 0, 1)
    img.dither(glow > 0, C.NIGHT, C.MAUVE, glow * 0.5)
    rng = np.random.default_rng(31)
    for _ in range(40):
        x, y = int(rng.integers(142, 464)), int(rng.integers(6, 240))
        img.put(x, y, C.WHITE if rng.integers(0, 4) == 0 else C.LILAC_LIGHT)
    for x0, x1 in ((124, 136), (468, W - 1)):
        img.paint(img.m_rect(x0, 0, x1, H - 1), C.NIGHT)
        vine = img.m_line(bezier([((x0 + x1) // 2, 0), (x0, 90), (x1, 180), ((x0 + x1) // 2, H)], 40), 2)
        img.paint(vine, C.GREEN_DARK)
        for y in range(6, H, 14):
            img.put(int(rng.integers(x0, x1 + 1)), y, C.GREEN)
            img.put(int(rng.integers(x0, x1 + 1)), y + 5, C.CRIMSON)
        img.paint(img.m_rect(x0, 0, x0, H - 1), C.BLACK)
    img.dither(img.m_ellipse(298, 262, 70, 22), C.NIGHT, C.LILAC, 0.4)
    img.dither(img.m_ellipse(298, 266, 40, 10), C.LILAC, C.LILAC_LIGHT, 0.4)
    return img


# ================================================================== the strangers (48x48)

def lying_witness():
    """The Lying Witness: a hooded figure with one huge eye, a finger to its lips."""
    img = Img(48, 48)
    F._backdrop(img, C.PLUM)
    cloak = img.m_poly([(8, 47), (14, 18), (24, 8), (34, 18), (40, 47)])
    img.shade(cloak, [C.BLACK, C.NIGHT, C.DUSK, C.PLUM], light=(-1, -1))
    img.paint(img.m_ellipse(24, 22, 8, 9), C.BLACK)
    eye = img.m_ellipse(24, 21, 6, 4)
    img.paint(eye, C.BONE)
    img.paint(img.m_ellipse(25, 21, 2.5, 3), C.LILAC)
    img.put(25, 21, C.BLACK)
    img.put(26, 20, C.WHITE)
    img.paint(img.m_rect(23, 27, 24, 33), C.BONE_MID)   # the finger to the lips
    img.paint(img.m_rect(20, 29, 28, 29), C.CRIMSON)
    img.outline(C.BLACK, mask=img.px != C.BLACK)
    return img


def spectacle():
    """The Spectacle: a stagehand under a spotlight, a laughing mask held up on a stick."""
    img = Img(48, 48)
    F._backdrop(img, C.BLOOD)
    beam = img.m_poly([(16, 0), (32, 0), (44, 47), (4, 47)])
    img.dither(beam, C.BLACK, C.BONE_SHADE, 0.3)
    body = img.m_poly([(14, 47), (17, 28), (24, 25), (31, 28), (34, 47)])
    img.shade(body, [C.NIGHT, C.DUSK, C.PLUM], light=(-1, -1))
    mask = img.m_ellipse(24, 20, 8, 9)
    img.shade(mask, RAMP_GOLD, shadow=1)
    img.paint(img.m_line(bezier([(19, 18), (21, 16), (23, 18)], 6), 1), C.BLACK)
    img.paint(img.m_line(bezier([(25, 18), (27, 16), (29, 18)], 6), 1), C.BLACK)
    img.paint(img.m_line(bezier([(18, 22), (24, 28), (30, 22)], 10), 2), C.BLOOD_DARK)
    img.paint(img.m_rect(33, 22, 34, 40), C.BONE_DARK)
    img.outline(C.BLACK, mask=img.px != C.BLACK)
    return img


def false_coin():
    """The False Coin: a coiner with a hammer over a mould, a fresh coin shining too brightly."""
    img = Img(48, 48)
    F._backdrop(img, C.GOLD_DARK)
    body = img.m_poly([(10, 47), (14, 26), (24, 22), (34, 26), (38, 47)])
    img.shade(body, [C.NIGHT, C.BONE_SHADE, C.SILVER_DARK], light=(-1, -1))
    head = img.m_ellipse(24, 16, 6, 7)
    img.paint(head, C.BONE_DARK)
    img.put(22, 15, C.BLACK)
    img.put(26, 15, C.GOLD)
    img.paint(img.m_rect(18, 9, 30, 10), C.DUSK)
    img.paint(img.m_line([(30, 30), (40, 20)], 2), C.BONE_SHADE)   # the hammer
    img.paint(img.m_rect(38, 16, 44, 21), C.SILVER_DARK)
    img.paint(img.m_rect(12, 38, 26, 44), C.BONE_SHADE)          # the mould
    D.coin(img, 19, 36)
    S.sparkle(img, 22, 33)
    img.outline(C.BLACK, mask=img.px != C.BLACK)
    return img


def night_bargain():
    """The Night Bargain: a veiled trader with a lantern, coins spilling from an open hand."""
    img = Img(48, 48)
    F._backdrop(img, C.VIOLET)
    veil = img.m_poly([(8, 47), (12, 20), (24, 8), (36, 20), (40, 47)])
    img.shade(veil, [C.BLACK, C.NIGHT, C.VIOLET, C.MAUVE], light=(-1, -1))
    img.paint(img.m_ellipse(24, 20, 6, 7), C.BLACK)
    img.put(22, 20, C.LILAC_LIGHT)
    img.put(26, 20, C.LILAC_LIGHT)
    lamp = img.m_rect(36, 28, 41, 35)
    img.paint(lamp, C.AMBER)
    img.inner_outline(lamp, C.GOLD_DARK)
    img.paint(img.m_ellipse(14, 36, 4, 3), C.LILAC)
    for x, y in ((12, 32), (15, 30), (18, 33)):
        D.coin(img, x, y)
    img.outline(C.BLACK, mask=img.px != C.BLACK)
    return img


def desire():
    """Desire: a pale hand reaching out of the dark toward a rose, its thorns red."""
    img = Img(48, 48)
    F._backdrop(img, C.CRIMSON)
    arm = img.m_line(bezier([(4, 46), (12, 34), (20, 30)], 12), 5)
    img.shade(arm, [C.LILAC, C.LILAC_LIGHT, C.BONE], light=(-1, -1))
    for k in range(4):
        img.paint(img.m_line([(20, 30 + k), (26, 26 + k * 2)], 1), C.LILAC_LIGHT)
    stem = img.m_line(bezier([(36, 46), (34, 32), (36, 18)], 12), 1)
    img.paint(stem, C.GREEN_DARK)
    for y in (40, 33, 26):
        img.put(35, y, C.CRIMSON)
    rose = img.m_ellipse(36, 14, 6, 5)
    img.shade(rose, [C.BLOOD_DARK, C.BLOOD, C.CRIMSON, C.RED], shadow=1)
    img.paint(img.m_line(bezier([(33, 14), (36, 11), (39, 14)], 6), 1), C.BLOOD_DARK)
    img.outline(C.BLACK, mask=img.px != C.BLACK)
    return img


def insomnia():
    """Insomnia: a thin figure with wide unblinking eyes and dark rings, a candle burnt to the stub."""
    img = Img(48, 48)
    F._backdrop(img, C.DUSK)
    body = img.m_poly([(14, 47), (17, 28), (24, 25), (31, 28), (34, 47)])
    img.shade(body, [C.NIGHT, C.DUSK, C.PLUM], light=(-1, -1))
    head = img.m_ellipse(24, 17, 7, 9)
    img.shade(head, [C.BONE_SHADE, C.BONE_DARK, C.BONE_MID], shadow=1)
    for x in (21, 27):
        img.paint(img.m_ellipse(x, 16, 2.6, 2.6), C.VIOLET)
        img.paint(img.m_ellipse(x, 16, 1.6, 1.6), C.WHITE)
        img.put(x, 16, C.BLACK)
    img.paint(img.m_rect(22, 23, 26, 23), C.PLUM)
    img.paint(img.m_rect(36, 38, 39, 44), C.BONE)
    img.put(37, 36, C.AMBER)
    img.put(37, 35, C.EMBER)
    img.outline(C.BLACK, mask=img.px != C.BLACK)
    return img


STRANGERS = {"lying_witness": lying_witness, "spectacle": spectacle, "false_coin": false_coin, "night_bargain": night_bargain,
             "desire": desire, "insomnia": insomnia}


def write_all(art_dir):
    written = write_faces(os.path.join(art_dir, "Demons"))
    written += write_halls(os.path.join(art_dir, "Backgrounds"))
    ui = os.path.join(art_dir, "Ui")
    for name, draw in (("chapter_map_belial", chapter_map_belial), ("chapter_map_lilith", chapter_map_lilith)):
        path = os.path.join(ui, name + ".png")
        draw().save(path)
        written.append(path)
    events = os.path.join(art_dir, "Events")
    os.makedirs(events, exist_ok=True)
    for name, draw in STRANGERS.items():
        path = os.path.join(events, name + ".png")
        draw().save(path)
        written.append(path)
    return written
