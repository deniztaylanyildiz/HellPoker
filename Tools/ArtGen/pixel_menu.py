"""The title screen's backdrop (Demo 3.3): a gambling den at the very bottom of Hell, drawn on the 640×360 screen.

One still picture with small looping layers over it (see pixel_layers.py) and embers the game draws itself. Front to back:
- A rock arch frames the screen like a stage, chains hanging from its corners (they sway, a pixel at a time).
- On the ground, half buried: the Dead Man's Hand as four giant old cards (A♠ A♣ 8♠ 8♣); Mammon's spilled gold and his scales on the
  left (coins glint), Belial's red curtain and his silver serpent round a broken column on the right, Lilith's crescent above it
  (a bat crosses it).
- The sea of fire on the horizon in three bands (far tongues flicker, the sea rolls at its wild edges), the burning city's towers
  in two depths, red clouds drifting slowly across the smoke.
- Far away, in the dark: Lucifer's throne — only its steps, its chains and two burning eyes that blink now and then. Never more.
- A slow red halo pulses behind the logo (sprite frames, never alpha).
Readability first: the middle column (the logo, the words, the menu's box) stays dark and calm, and nothing moves where words sit.
The logo, the menu's iron box, its cursor and the logo's glint are drawn here too (Ui/menu_logo, menu_box, menu_cursor, menu_glint).
Output: Assets/Resources/Art/Ui/menu.png (still), menu_<layer>.png, menu_motion.txt and the sprites above.
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

from pixel import C, Img, bezier, ribbon, sheet, shifted
from pixel_layers import Layer, Particles, phase, write
from pixel_salons import DARKER, glint_layer, lut

W, H = 640, 360
HORIZON = H - 74
FRAMES = 12
HERE = os.path.dirname(os.path.abspath(__file__))
TITLE_FONT = os.path.join(HERE, "fonts", "PressStart2P-Regular.ttf")
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]

# The title menu's layout (MainMenuView): the logo from y 24; DEMO (or the simulator's label) at 96, the words from 112, the menu's box
# from y 152 (two columns of 128 px buttons, the cursor inside it), the run's card left of it, the footer, the version and BUG / IDEA
# along the bottom.
MENU_TEXT = ((176, 94, 464, 106), (40, 110, 600, 152), (156, 150, 484, 296), (4, 148, 164, 236), (256, 338, 384, 356),
             (528, 338, 639, 359), (0, 330, 120, 359))

# The logo's place (for the halo behind it): centred, from y 24.
LOGO_TOP = 24


# ---------------------------------------------------------------- the far scene

def sky(img):
    """Smoke-dark sky: black at the top, bleeding into blood red toward the burning horizon; a few dead stars at the sides."""
    full = np.ones((H, W), bool)
    img.dither(full, C.BLACK, C.NIGHT, np.clip(img.ys / 170.0, 0, 1))
    img.dither(img.m_rect(0, HORIZON - 100, W, HORIZON), C.NIGHT, C.BLOOD_DARK, np.clip((img.ys - (HORIZON - 100)) / 90.0, 0, 1))
    img.dither(img.m_rect(0, HORIZON - 34, W, HORIZON), C.BLOOD_DARK, C.BLOOD, np.clip((img.ys - (HORIZON - 34)) / 44.0, 0, 0.8))
    rng = np.random.default_rng(5)
    for _ in range(30):
        x = int(rng.integers(30, W - 30))
        if abs(x - W // 2) < 150:
            continue
        img.put(x, int(rng.integers(20, HORIZON - 120)), C.MAUVE if rng.random() < 0.7 else C.LILAC_LIGHT)


def clouds(img):
    """Still red clouds low over the sides (the drifting ones are layers): dithered bands of blood in the smoke."""
    for cx, cy, rx, ry in ((90, 96, 90, 10), (560, 84, 100, 11), (40, 120, 60, 7), (610, 128, 50, 6)):
        cloud = img.m_ellipse(cx, cy, rx, ry)
        img.dither(cloud, C.NIGHT, C.BLOOD_DARK, 0.6)
        img.dither(cloud & img.m_ellipse(cx, cy - 2, rx * 0.7, ry * 0.5), C.BLOOD_DARK, C.BLOOD, 0.35)


THRONE_X = W // 2


def throne_glow(img):
    """The sky behind the throne smoulders: a wide dithered red glow in the middle (still dark enough to read on)."""
    d = np.hypot((img.xs - THRONE_X) / 230.0, (img.ys - 210) / 120.0)
    glow = np.clip(1 - d, 0, 1)
    sky_part = img.m_rect(0, 88, W, HORIZON - 1) & (img.px <= C.NIGHT)
    img.dither(sky_part & (glow > 0), C.BLACK, C.BLOOD_DARK, glow * 0.9)


def throne(img):
    """
    Lucifer's throne, far away in the dark behind the menu: a wide stepped dais rising out of the sea's glow, the throne's black bulk
    above it, chains running from its arms out to the sides. Never a figure — the dark shape, its steps, its chains (and the eyes,
    a layer). Black against the smouldering sky; the steps' lips and the back's spikes catch a little of the fire. The back rises
    above the menu's box and its spikes reach up under the logo; the widest steps show at both sides of the box.
    """
    # The steps: five, each narrower and higher, their lips lit by the sea.
    for k in range(5):
        half = 240 - k * 36
        top = HORIZON - 12 - k * 12
        step = img.m_rect(THRONE_X - half, top, THRONE_X + half, top + 12)
        img.paint(step, C.BLACK)
        img.paint(img.m_rect(THRONE_X - half, top, THRONE_X + half, top), C.BLOOD)
        img.paint(img.m_rect(THRONE_X - half, top + 1, THRONE_X - half, top + 11) | img.m_rect(THRONE_X + half, top + 1, THRONE_X + half, top + 11),
                  C.BLOOD_DARK)
    # The throne's bulk: a tall back with spikes, two arms; black on the glow.
    seat_top = HORIZON - 84
    back = img.m_poly([(THRONE_X - 78, seat_top), (THRONE_X - 70, 104), (THRONE_X - 52, 120), (THRONE_X - 34, 92), (THRONE_X - 16, 112),
                       (THRONE_X, 84), (THRONE_X + 16, 112), (THRONE_X + 34, 92), (THRONE_X + 52, 120), (THRONE_X + 70, 104),
                       (THRONE_X + 78, seat_top)])
    img.paint(back, C.BLACK)
    img.paint(back & ~shifted(back, 0, -1), C.BLOOD_DARK)   # the spikes' top edges, faintly lit
    for side in (-1, 1):
        x0, x1 = (THRONE_X - 100, THRONE_X - 78) if side < 0 else (THRONE_X + 78, THRONE_X + 100)
        arm = img.m_rect(x0, seat_top - 10, x1, seat_top + 12)
        img.paint(arm, C.BLACK)
        img.paint(arm & ~shifted(arm, 0, -1), C.BLOOD_DARK)
    # The chains: from the arms out to the sides, sagging, link by link.
    for side in (-1, 1):
        start = (THRONE_X + side * 100, seat_top)
        end = (THRONE_X + side * 262, HORIZON - 30)
        sag = ((start[0] + end[0]) / 2, max(start[1], end[1]) + 6)
        for i, (x, y) in enumerate(bezier([start, sag, end], 30)):
            if i % 2 == 0:
                img.paint(img.m_ellipse(x, y, 2, 1.4) & ~img.m_ellipse(x, y, 0.9, 0.5), C.PLUM)
            else:
                img.put(int(x), int(y), C.DUSK)


EYES = (W // 2 - 14, W // 2 + 14)
EYES_Y = 10.5
EYES_BOX = (W // 2 - 26, 1, 53, 20)
# Over 12 frames: the glow deepens and eases, and once a loop they narrow, close and open again.
EYE_OPEN = [2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 1.2, 0.0, 1.2, 2.2]
EYE_CORE = [C.ORANGE, C.ORANGE, C.AMBER, C.AMBER, C.AMBER, C.ORANGE, C.ORANGE, C.ORANGE, C.ORANGE, C.ORANGE, C.ORANGE, C.ORANGE]


def eye_halos(img):
    for ex in EYES:
        halo = np.hypot(img.xs - ex - 0.5, img.ys - EYES_Y) / 9.0
        img.dither(halo < 1, C.BLACK, C.BLOOD_DARK, np.clip(0.5 * (1 - halo), 0, 1))


def eyes_layer():
    """His eyes, high above the throne: two slits of fire in the dark. They close once a loop."""
    ox, oy = EYES_BOX[0], EYES_BOX[1]

    def draw(img, f):
        for ex in EYES:
            cx, cy = ex + 0.5 - ox, EYES_Y - oy
            open_ = EYE_OPEN[f]
            if open_ <= 0:
                img.paint(img.m_rect(ex - 4 - ox, int(EYES_Y) - oy, ex + 4 - ox, int(EYES_Y) - oy), C.BLOOD_DARK)
                continue
            almond = img.m_ellipse(cx, cy, 5.2, open_)
            img.paint(almond, C.HELL)
            img.paint(img.m_ellipse(cx, cy, 3.6, max(0.6, open_ - 0.9)) & almond, EYE_CORE[f])
            img.paint(img.m_ellipse(cx, cy, 2.0, max(0.5, open_ - 1.4)) & almond, C.EMBER)
            for y in range(int(EYES_Y) - 1, int(EYES_Y) + 2):
                if 0 <= y - oy < img.h and almond[y - oy, ex - ox]:
                    img.put(ex - ox, y - oy, C.BLACK)
    return Layer("eyes", ox, oy, EYES_BOX[2], EYES_BOX[3], FRAMES, 8, draw)


def city(img):
    """The burning city on the horizon at both sides, in two depths: far towers in dusk, near ones black with lit windows."""
    rng = np.random.default_rng(17)
    for depth, colour, lo, hi in ((0, C.DUSK, 18, 60), (1, C.BLACK, 10, 46)):
        for x0, x1 in ((24, 190), (W - 190, W - 24)):
            x = x0 + int(rng.integers(0, 8))
            while x < x1:
                w = int(rng.integers(6, 16))
                h = int(rng.integers(lo, hi))
                top = HORIZON - h - (6 if depth == 0 else 0)
                tower = img.m_rect(x, top, x + w - 1, HORIZON + 4)
                img.paint(tower, colour)
                if rng.random() < 0.45:
                    img.paint(img.m_poly([(x + 1, top), (x + w // 2, top - int(rng.integers(5, 16))), (x + w - 2, top)]), colour)
                if depth == 1:
                    for wy in range(top + 3, HORIZON, 5):
                        for wx in range(x + 2, x + w - 2, 4):
                            if rng.random() < 0.2:
                                img.put(wx, wy, C.ORANGE if (wx + wy) % 7 else C.RED)
                x += w + int(rng.integers(1, 5 if depth == 0 else 3))


def fire_color(x, y, f):
    """The sea of fire at a screen pixel, at frame f: two crests rolling opposite ways (24 and 12 px periods close on the 12-frame
    loop), calm under the menu, wild toward the sides."""
    depth = (y - HORIZON) / (H - HORIZON)
    p = phase(f, FRAMES)
    wave = math.sin(2 * math.pi * x / 24 + y * 0.35 + p) + 0.6 * math.sin(2 * math.pi * x / 12 - p + y * 0.1)
    wave += 0.7 * math.sin(x * 0.071 + y * 0.53) * math.sin(x * 0.033 - y * 0.21)
    wave /= 2.0
    edge = min(1.0, max(0.0, (abs(x - W / 2) - 150) / 70.0))
    heat = (wave * 0.5 + 0.5) * (0.3 + 0.7 * edge)
    if heat > 0.85:
        return C.AMBER if depth < 0.4 else C.ORANGE
    if heat > 0.6:
        return C.HELL
    if heat > 0.35:
        return C.RED
    return C.CRIMSON if depth < 0.5 else C.BLOOD


def fire_sea(img):
    for y in range(HORIZON, H):
        for x in range(W):
            img.put(x, y, fire_color(x, y, 0))
    img.paint(img.m_rect(0, HORIZON, W, HORIZON), C.EMBER)


# The sea rolls at its wild edges: left of the menu past Mammon's heap, and at the far right past the cards.
FIRE_EDGES = (("fire_left", 134, 52), ("fire_right", W - 80, 58))
FIRE_FADE = 20


def fire_layer(name, x0, w):
    inner = x0 + w - 1 if x0 < W // 2 else x0

    def draw(img, f):
        for y in range(HORIZON + 1, H):
            for x in range(x0, x0 + w):
                still = 1 - min(1.0, abs(x - inner) / FIRE_FADE)
                moving = still * 16 <= BAYER[y % 4][x % 4]
                img.put(x - x0, y - HORIZON - 1, fire_color(x, y, f if moving else 0))
    return Layer(name, x0, HORIZON + 1, w, H - HORIZON - 1, FRAMES, 8, draw, vignette=quiet_middle)


def tongue_height(x, f):
    """The far flames' height along the horizon at frame f (periods that close on the loop)."""
    p = phase(f, FRAMES)
    return 4 + 3 * math.sin(x * 0.45 + p) + 2 * math.sin(x * 0.21 - 2 * p) + 1.5 * math.sin(x * 1.3 + 3 * p)


def far_flames(img, f=0, x0=0, w=W, oy=0):
    """The far band of the sea: flame tongues licking up over the horizon, low and calm in the middle."""
    for x in range(x0, x0 + w):
        calm = min(1.0, max(0.25, (abs(x - W / 2) - 110) / 80.0))
        h = int(round(tongue_height(x, f) * calm))
        for k in range(h):
            y = HORIZON - 1 - k
            colour = C.AMBER if k == 0 else C.ORANGE if k < h * 0.5 else C.HELL
            img.put(x - x0, y - oy, colour)


FLAME_BANDS = (("flames_left", 24, 120), ("flames_right", W - 144, 120))


def flames_layer(name, x0, w):
    def draw(img, f):
        far_flames(img, f, x0, w, HORIZON - 10)
    return Layer(name, x0, HORIZON - 10, w, 10, FRAMES, 10, draw)


# ---------------------------------------------------------------- the near scene

def gold(img):
    """Mammon's spilled treasure at the bottom left: a heap of coins and a tipped chest; his scales stand over it."""
    heap = img.m_ellipse(70, H + 4, 62, 30)
    img.shade(heap, [C.GOLD_DARK, C.GOLD_MID, C.GOLD, C.GOLD_LIGHT], shadow=2)
    rng = np.random.default_rng(23)
    for _ in range(90):
        x, y = int(rng.integers(10, 132)), int(rng.integers(H - 28, H))
        if heap[min(y, H - 1), x]:
            img.put(x, y, C.GOLD_DARK)
            img.put(x + 1, y, C.GOLD_LIGHT)
    b = H - 46
    chest = img.m_rect(96, b, 124, b + 16)
    img.paint(chest, C.BLOOD_DARK)
    img.paint(img.m_rect(96, b + 4, 124, b + 4), C.BLOOD)
    for bx in (99, 120):
        img.paint(img.m_rect(bx, b, bx + 1, b + 16), C.GOLD_MID)
    img.paint(img.m_rect(108, b + 5, 112, b + 9), C.GOLD)
    img.put(110, b + 7, C.BLACK)
    img.inner_outline(chest, C.BLACK)
    lid = img.m_poly([(95, b), (125, b), (128, b - 11), (98, b - 13)])
    img.paint(lid, C.BLOOD)
    img.inner_outline(lid, C.GOLD_DARK)


SCALES_X, SCALES_TOP = 58, 222


def scales(img):
    """Mammon's scales on the heap: a gold post, a beam, two pans on chains, coins on the heavier pan."""
    x = SCALES_X
    img.paint(img.m_rect(x - 2, SCALES_TOP, x + 1, H - 30), C.GOLD_MID)
    img.paint(img.m_rect(x - 2, SCALES_TOP, x - 2, H - 30), C.GOLD_LIGHT)
    img.paint(img.m_rect(x - 8, H - 32, x + 7, H - 29), C.GOLD_DARK)
    img.paint(img.m_line([(x - 30, SCALES_TOP + 3), (x + 30, SCALES_TOP - 1)], 2), C.GOLD)
    img.paint(img.m_ellipse(x, SCALES_TOP, 3, 3), C.GOLD_LIGHT)
    for px, py in ((x - 30, SCALES_TOP + 3), (x + 30, SCALES_TOP - 1)):
        for k in (-7, 7):
            img.paint(img.m_line([(px, py), (px + k, py + 20)], 1), C.GOLD_DARK)
        pan = img.m_ellipse(px, py + 21, 9, 2.5)
        img.paint(pan, C.GOLD)
        img.inner_outline(pan, C.GOLD_DARK)
    for k in range(4):   # the coins on the left pan
        img.paint(img.m_ellipse(x - 30 + (k - 1.5) * 3, SCALES_TOP + 21 - k, 3, 1.2), C.GOLD_LIGHT if k == 3 else C.GOLD_MID)


GOLD_GLINTS = ((40, H - 18), (84, H - 22), (SCALES_X - 30, SCALES_TOP + 19))


def big_card(img, cx, cy, angle, rank, suit_rows, buried):
    """A giant old card (32×46), tipped by `angle` degrees and sunk `buried` px into the ground: bone face, black ink, worn edges."""
    a = math.radians(angle)
    ca, sa = math.cos(a), math.sin(a)
    hw, hh = 16, 23

    def at(u, v):
        return (cx + u * ca - v * sa, cy + u * sa + v * ca)
    corners = [at(-hw, -hh), at(hw, -hh), at(hw, hh), at(-hw, hh)]
    face = img.m_poly(corners) & img.m_rect(0, 0, W, H - buried)
    img.paint(face, C.BONE)
    img.dither(face & ~shifted(face, 1, 0) | face & ~shifted(face, 0, 1), C.BONE, C.BONE_DARK, 0.5)
    img.inner_outline(face, C.BLACK)
    # The rank in the corner and the suit in the middle, both twice their small size (whole pixels on the tipped card).
    for (u0, v0), rows, scale in (((-hw + 3, -hh + 3), rank, 2), ((-5, -7), suit_rows, 2)):
        for r, line in enumerate(rows):
            for c, ch in enumerate(line):
                if ch in "#k":
                    for dy in range(scale):
                        for dx in range(scale):
                            x, y = at(u0 + c * scale + dx, v0 + r * scale + dy)
                            if 0 <= y < H - buried:
                                img.put(int(round(x)), int(round(y)), C.BLACK)


GLYPH_A = [".#.", "#.#", "###", "#.#", "#.#"]
GLYPH_8 = ["###", "#.#", "###", "#.#", "###"]
SPADE = ["..k..", ".kkk.", "kkkkk", "..k..", ".kkk."]
CLUB = [".kkk.", ".kkk.", "kkkkk", "k.k.k", "..k.."]
CARDS = ((470, H - 30, -14, GLYPH_A, SPADE, 14), (494, H - 26, -4, GLYPH_A, CLUB, 12),
         (518, H - 28, 7, GLYPH_8, SPADE, 13), (540, H - 32, 16, GLYPH_8, CLUB, 15))


def dead_mans_hand(img):
    """A♠ A♣ 8♠ 8♣: four giant cards stuck in the ground at the bottom right — the one hand that walks out of here."""
    for cx, cy, angle, rank, suit, buried in CARDS:
        big_card(img, cx, cy, angle, rank, suit, buried)


COLUMN_X = W - 44
SERPENT_BOX = (COLUMN_X - 14, 120, 29, HORIZON - 120 + 1)


def curtain(img):
    """Belial's red curtain hanging from the arch at the top right, gathered on the column: folds in three reds, a gold hem."""
    for i, x in enumerate(range(W - 84, W - 24, 6)):
        fold = img.m_poly([(x, 20), (x + 7, 20), (x + 5 + (i % 3), 96 + i * 4), (x - 1 + (i % 2), 94 + i * 4)])
        img.paint(fold, (C.BLOOD, C.CRIMSON, C.RED)[i % 3])
    img.paint(img.m_line([(W - 86, 22), (W - 24, 22)], 2), C.GOLD_MID)
    for x in range(W - 82, W - 26, 5):
        img.put(x, 24, C.GOLD)


def serpent_column(img):
    x = COLUMN_X
    column = img.m_rect(x - 7, 132, x + 7, HORIZON)
    img.paint(column, C.BONE_SHADE)
    img.paint(img.m_rect(x - 7, 132, x - 6, HORIZON), C.BONE_DARK)
    img.paint(img.m_poly([(x - 9, 132), (x - 4, 124), (x + 2, 130), (x + 8, 122), (x + 9, 132)]), C.BONE_SHADE)
    img.inner_outline(column, C.BLACK)


def serpent_layer():
    ox, oy, w, h = SERPENT_BOX

    def draw(img, f):
        spine = [(COLUMN_X - ox + 4 * math.sin(y * 0.16 + phase(f, FRAMES)), y - oy) for y in range(136, HORIZON, 3)]
        img.paint(img.m_poly(ribbon(spine, 4, 3)), C.SILVER_DARK)
        hx, hy = spine[0]
        img.paint(img.m_ellipse(hx + 2, hy - 2, 3, 2), C.SILVER)
        img.put(int(hx) + 3, int(hy) - 3, C.HELL)
    return Layer("serpent", ox, oy, w, h, FRAMES, 10, draw)


MOON = (W - 126, 184)


def crescent(img):
    """Lilith's moon above Belial's curtain, a cold lilac halo round it."""
    cx, cy = MOON
    halo = np.hypot(img.xs - cx, img.ys - cy) / 26.0
    img.dither(halo < 1, C.NIGHT, C.VIOLET, np.clip(0.45 * (1 - halo), 0, 1))
    moon = img.m_ellipse(cx, cy, 13, 13) & ~img.m_ellipse(cx + 6, cy - 4, 12, 12)
    img.shade(moon, [C.MAUVE, C.LILAC, C.LILAC_LIGHT, C.WHITE], light=(-1, 1), shadow=1)


# A bat crosses the moon once a loop: frame 0 and the last frames empty, in between it flies across, wings beating a pixel.
BAT_BOX = (MOON[0] - 28, MOON[1] - 14, 60, 18)
BAT_PATH = [None, None, 2, 9, 16, 23, 30, 37, 44, 51, None, None]
BAT_WING = [0, 0, -1, 1, -1, 1, -1, 1, -1, 1, 0, 0]


def bat_layer():
    def draw(img, f):
        x = BAT_PATH[f]
        if x is None:
            return
        tip = BAT_WING[f]
        y = 8 - (f % 3)
        for dx, dy in ((0, 1 + tip), (1, 1 + max(0, tip)), (2, 1), (3, 2), (4, 1), (5, 1 + max(0, tip)), (6, 1 + tip)):
            img.put(x + dx, y + dy, C.BLACK)
    return Layer("bat", BAT_BOX[0], BAT_BOX[1], BAT_BOX[2], BAT_BOX[3], FRAMES, 8, draw)


# A cloud drifts two pixels one way and back over a loop (whole pixels, a step every other frame).
CLOUD_SWAY = [0, 0, 1, 1, 2, 2, 2, 2, 1, 1, 0, 0]


def cloud_layer(name, x, y, rx, ry, offset):
    """A ragged red cloud drifting slowly to and fro in the smoke at the side of the screen (never across the words)."""
    w, h = int(rx * 2 + 6), int(ry * 2 + 2)

    def draw(img, f):
        cx = rx + 1 + CLOUD_SWAY[(f + offset) % FRAMES]
        blob = img.m_ellipse(cx, h / 2, rx, ry) | img.m_ellipse(cx - rx * 0.4, h / 2 + 1, rx * 0.6, ry * 0.8)
        # The dither is fixed to the cloud, not the strip, so the whole cloud moves rather than shimmering.
        threshold = np.array(BAYER, np.float32)[np.arange(img.h)[:, None] % 4, (np.arange(img.w)[None, :] - int(cx)) % 4] / 16.0
        img.px[blob & (0.55 > threshold)] = C.BLOOD_DARK
        core = blob & img.m_ellipse(cx, h / 2 - 1, rx * 0.6, ry * 0.5)
        img.px[core & (0.3 > threshold)] = C.BLOOD
    return Layer(name, x, y, w, h, FRAMES, 8, draw)


# ---------------------------------------------------------------- the frame: a rock arch, chains at its corners

ARCH = 22


def arch(img):
    """A rough rock arch round the screen: columns at the sides, a lintel over the top, cracked and lit from the fire below."""
    rock = img.m_rect(0, 0, ARCH - 1, H) | img.m_rect(W - ARCH, 0, W, H) | img.m_rect(0, 0, W, 9)
    # The lintel sags down into the corners (an arch), the columns' inner faces are ragged.
    rock |= img.m_ellipse(0, 0, 70, 36) | img.m_ellipse(W, 0, 70, 36)
    rng = np.random.default_rng(31)
    for y in range(0, H, 6):
        for side in (0, 1):
            d = int(rng.integers(0, 5))
            x = ARCH + d if side == 0 else W - ARCH - d - 1
            rock |= img.m_rect(min(x, ARCH), y, max(x, ARCH), y + 5) if side == 0 else img.m_rect(min(x, W - ARCH), y, max(x, W - ARCH), y + 5)
    img.paint(rock, C.NIGHT)
    img.dither(rock, C.NIGHT, C.DUSK, np.clip((img.ys - HORIZON + 60) / 120.0, 0, 0.6))   # the fire's light from below
    img.inner_outline(rock, C.BLACK)
    for _ in range(40):   # cracks
        x = int(rng.integers(0, W))
        y = int(rng.integers(0, H))
        if rock[y, x]:
            img.paint(img.m_line([(x, y), (x + int(rng.integers(-3, 4)), y + int(rng.integers(2, 6)))], 1) & rock, C.BLACK)


CHAINS = (("chain_left", 40), ("chain_right", W - 48))
CHAIN_SWAY = [0, 0, 1, 1, 1, 0, 0, 0, -1, -1, -1, 0]


def chain_layer(name, x):
    """A chain hanging from the arch's corner, swaying a pixel to each side."""
    def draw(img, f):
        sway = CHAIN_SWAY[f]
        for k, y in enumerate(range(2, 64, 4)):
            dx = int(round(sway * y / 64))
            if k % 2 == 0:
                img.paint(img.m_ellipse(4 + dx, y + 1.5, 2.2, 1.8) & ~img.m_ellipse(4 + dx, y + 1.5, 1, 0.7), C.SILVER_DARK)
            else:
                img.paint(img.m_rect(3 + dx, y, 4 + dx, y + 2), C.BONE_SHADE)
        img.paint(img.m_ellipse(4 + sway, 67, 3, 2.5) & ~img.m_ellipse(4 + sway, 67, 1.4, 1.2), C.SILVER)
    return Layer(name, x, 8, 9, 72, FRAMES, 8, draw)


# A spark falls once a loop from the city's fires: a short streak, two frames in the air, then nothing (frame 0 rests).
SPARK_FALL = [None, None, None, None, None, None, 0, 1, 2, 3, None, None]


def spark_layer(name, x, y, offset):
    def draw(img, f):
        step = SPARK_FALL[f]
        if step is None:
            return
        sy = 2 + step * 8
        img.put(2 + step, sy, C.EMBER)
        img.put(1 + step, sy - 1, C.AMBER)
        img.put(step, sy - 2, C.ORANGE)
    return Layer(name, x, y, 8, 36, FRAMES, 10, draw, offset=offset)


# ---------------------------------------------------------------- the halo behind the logo

HALO_PULSE = [0.0, 0.08, 0.16, 0.24, 0.3, 0.34, 0.34, 0.3, 0.24, 0.16, 0.08, 0.0]
HALO_PARTS = (("halo_left", W // 2 - 180), ("halo_middle", W // 2 - 60), ("halo_right", W // 2 + 60))
HALO_TOP, HALO_H = LOGO_TOP - 4, 62


def halo_layer(name, x):
    """A slow red pulse behind the logo: a dithered glow that swells and fades (frame 0 the faintest); three parts, each a narrow strip."""
    def draw(img, f):
        xs = img.xs + x
        ys = img.ys + HALO_TOP
        d = np.hypot((xs - W / 2) / 200.0, (ys - (LOGO_TOP + 28)) / 34.0)
        glow = np.clip(1 - d, 0, 1) * (0.25 + HALO_PULSE[f])
        threshold = np.array(BAYER, np.float32)[(np.arange(img.h)[:, None] + HALO_TOP) % 4, (np.arange(img.w)[None, :] + x) % 4] / 16.0
        img.px[glow > threshold] = C.BLOOD_DARK
        img.px[glow * 0.6 > threshold + 0.12] = C.BLOOD
    return Layer(name, x, HALO_TOP, 120, HALO_H, FRAMES, 8, draw)


# ---------------------------------------------------------------- readability

def quiet_middle(img, ox=0, oy=0):
    """Two steps darker over the middle column (the logo, the words, the menu's box), fading out over ~24 px."""
    darker = lut(DARKER)
    once = darker[img.px]
    twice = darker[once]
    xs, ys = img.xs + ox, img.ys + oy
    threshold = np.array(BAYER, np.float32)[(np.arange(img.h)[:, None] + oy) % 4, (np.arange(img.w)[None, :] + ox) % 4] / 16.0

    def field(x0, y0, x1, y1, fade):
        dx = np.maximum(np.maximum(x0 - xs, xs - x1), 0)
        dy = np.maximum(np.maximum(y0 - ys, ys - y1), 0)
        return np.clip(1 - np.hypot(dx, dy) / fade, 0, 1)

    middle = field(176, 40, W - 176, H - 18, 24)
    out = np.where(middle > threshold, once, img.px)
    out = np.where(middle * 0.8 > threshold + 0.05, twice, out)
    img.px = out


def menu_base():
    img = Img(W, H, C.BLACK)
    sky(img)
    clouds(img)
    fire_sea(img)
    far_flames(img)
    quiet_middle(img)
    throne_glow(img)
    throne(img)
    city(img)
    crescent(img)
    curtain(img)
    serpent_column(img)
    gold(img)
    scales(img)
    dead_mans_hand(img)
    arch(img)
    eye_halos(img)
    return img


def menu_layers():
    layers = [eyes_layer(), serpent_layer(), bat_layer()]
    layers += [halo_layer(name, x) for name, x in HALO_PARTS]
    layers += [fire_layer(name, x0, w) for name, x0, w in FIRE_EDGES]
    layers += [flames_layer(name, x0, w) for name, x0, w in FLAME_BANDS]
    layers += [chain_layer(name, x) for name, x in CHAINS]
    layers += [spark_layer("spark_left", 60, HORIZON - 64, 0), spark_layer("spark_right", W - 90, HORIZON - 70, 5)]
    for i, (x, y) in enumerate(GOLD_GLINTS):
        layers.append(glint_layer(f"glint{i}", x, y, offset=i * 3))
    layers.append(glint_layer("glint_cards", W - 140, H - 40, offset=7))
    layers += [cloud_layer("cloud_a", 28, 52, 40, 6, 0), cloud_layer("cloud_b", W - 150, 132, 34, 5, 6)]
    return layers


# Embers rise from the burning city on either side, above and below the run's card, never across the middle.
MENU_PARTICLES = [Particles("embers", 24, 30, 120, 104, 10), Particles("embers", 24, 240, 120, HORIZON - 240, 8),
                  Particles("embers", W - 140, 30, 116, HORIZON - 30, 16)]


# ---------------------------------------------------------------- the menu's sprites

def menu_logo():
    """
    HELL POKER, redrawn for the title: the title font at five times its grid (strokes 5 px), made bolder, gold at the top running into
    fire red, flames licking up from the letters' tops, the four suits in the letters' hollows (♥ in the O, ♠ in the P, ♦ in the R, ♣ between
    the words), a black outline, a blood drop shadow, and a horned bar under it with a pitchfork in its middle.
    """
    font = ImageFont.truetype(TITLE_FONT, 40)
    probe = Image.new("L", (600, 80), 0)
    draw = ImageDraw.Draw(probe)
    draw.fontmode = "1"
    draw.text((4, 4), "HELL POKER", font=font, fill=255)
    box = probe.getbbox()
    glyphs = np.asarray(probe.crop(box), np.uint8) > 0
    gh, gw = glyphs.shape
    top, left = 10, 4
    img = Img(gw + 2 * left + 2, gh + top + 22)
    mask = np.zeros((img.h, img.w), bool)
    mask[top:top + gh, left:left + gw] = glyphs
    mask |= shifted(mask, -1, 0)   # one pixel bolder
    # Gold to fire, top to bottom.
    bands = [C.GOLD_LIGHT, C.GOLD, C.AMBER, C.ORANGE, C.HELL, C.RED]
    level = np.clip(((img.ys - top) / max(1, gh) * len(bands)).astype(int), 0, len(bands) - 1)
    img.px[mask] = np.array(bands, np.uint8)[level][mask]
    img.paint(mask & ~shifted(mask, 0, -1), C.EMBER)   # the lit top edge
    # Flames licking up from the letters' tops.
    rng = np.random.default_rng(13)
    tops = mask & ~shifted(mask, 0, -1)
    ys, xs = np.nonzero(tops)
    for x, y in zip(xs, ys):
        if rng.random() < 0.18:
            h = int(rng.integers(2, 7))
            for k in range(1, h):
                img.put(int(x) + (1 if k > h // 2 and rng.random() < 0.5 else 0), int(y) - k, C.AMBER if k < h - 1 else C.ORANGE)
    # The suits in the letters' hollows: found as the holes of the letters (not reachable from outside).
    solid = img.px != C.CLEAR
    outside = np.zeros_like(solid)
    outside[0, :] = outside[-1, :] = outside[:, 0] = outside[:, -1] = True
    outside &= ~solid
    for _ in range(img.w + img.h):
        grown = outside | (shifted(outside, 1, 0) | shifted(outside, -1, 0) | shifted(outside, 0, 1) | shifted(outside, 0, -1)) & ~solid
        if (grown == outside).all():
            break
        outside = grown
    holes = ~solid & ~outside
    letter = (gw + 6) / 10.0   # the advance of one letter
    suits = {6: ("heart", C.RED), 5: ("spade", C.BLACK), 9: ("diamond", C.RED)}
    for index, (name, colour) in suits.items():
        x0, x1 = int(left + index * letter), int(left + (index + 1) * letter)
        hole = holes & img.m_rect(x0, 0, x1, img.h)
        if not hole.any():
            continue
        hy, hx = np.nonzero(hole)
        cx, cy = (hx.min() + hx.max()) / 2.0, (hy.min() + hy.max()) / 2.0
        size = max(2.0, min(hx.max() - hx.min(), hy.max() - hy.min()) / 2.0 - 0.5)
        img.paint(hole, C.BLACK)
        img.paint(suit_mask(img, name, cx, cy, size) & hole, colour if colour != C.BLACK else C.BONE)
    # The club between the words.
    cx = left + 4.5 * letter
    img.paint(suit_mask(img, "club", cx, top + gh / 2, 4.5), C.BONE)
    img.inner_outline(suit_mask(img, "club", cx, top + gh / 2, 4.5), C.BONE_SHADE)
    # The horned bar under it, a pitchfork in its middle.
    by = top + gh + 8
    mid = img.w / 2
    img.paint(img.m_rect(mid - gw * 0.36, by, mid + gw * 0.36, by + 1), C.GOLD_MID)
    for side in (-1, 1):
        tip = mid + side * gw * 0.36
        horn = img.m_poly(ribbon([(tip, by + 1), (tip + side * 6, by - 2), (tip + side * 8, by - 8)], 3, 1))
        img.paint(horn, C.BONE_DARK)
    img.paint(img.m_rect(mid - 1, by - 7, mid, by + 8), C.GOLD)
    for dx in (-4, 0, 4):
        img.paint(img.m_rect(mid - 1 + dx, by - 9, mid + dx, by - 5), C.GOLD_LIGHT)
    img.paint(img.m_rect(mid - 5, by - 5, mid + 4, by - 5), C.GOLD)
    shadow = shifted(img.px != C.CLEAR, -2, -2) & (img.px == C.CLEAR)
    img.paint(shadow, C.BLOOD_DARK)
    img.outline(C.BLACK)
    return img


def suit_mask(img, name, cx, cy, s):
    """A suit's shape centred on (cx, cy), about 2s across."""
    if name == "heart":
        return img.m_ellipse(cx - s * 0.5, cy - s * 0.25, s * 0.55, s * 0.55) | img.m_ellipse(cx + s * 0.5, cy - s * 0.25, s * 0.55, s * 0.55) | \
            img.m_poly([(cx - s, cy - s * 0.1), (cx + s, cy - s * 0.1), (cx, cy + s)])
    if name == "diamond":
        return img.m_poly([(cx, cy - s), (cx + s * 0.75, cy), (cx, cy + s), (cx - s * 0.75, cy)])
    if name == "spade":
        return img.m_ellipse(cx - s * 0.45, cy + s * 0.15, s * 0.5, s * 0.5) | img.m_ellipse(cx + s * 0.45, cy + s * 0.15, s * 0.5, s * 0.5) | \
            img.m_poly([(cx - s * 0.95, cy + s * 0.1), (cx + s * 0.95, cy + s * 0.1), (cx, cy - s)]) | img.m_rect(cx - 0.5, cy, cx + 0.5, cy + s)
    return img.m_ellipse(cx, cy - s * 0.45, s * 0.42, s * 0.42) | img.m_ellipse(cx - s * 0.5, cy + s * 0.15, s * 0.42, s * 0.42) | \
        img.m_ellipse(cx + s * 0.5, cy + s * 0.15, s * 0.42, s * 0.42) | img.m_rect(cx - 0.5, cy, cx + 0.5, cy + s)


def menu_box():
    """The menu's iron box (9-slice, a 4 px border, the middle tiles): riveted dark iron round a dithered black."""
    img = Img(24, 24)
    img.paint(img.m_rect(0, 0, 23, 23), C.BONE_SHADE)
    img.paint(img.m_rect(0, 0, 23, 0) | img.m_rect(0, 0, 0, 23), C.SILVER_DARK)   # lit top and left
    img.paint(img.m_rect(1, 1, 22, 1) | img.m_rect(1, 1, 1, 22), C.BONE_DARK)
    img.paint(img.m_rect(3, 3, 20, 20), C.BLACK)                                   # the inner rim
    for x, y in ((1, 1), (21, 1), (1, 21), (21, 21)):                              # rivets at the corners
        img.paint(img.m_rect(x, y, x + 1, y + 1), C.GOLD_MID)
        img.put(x, y, C.GOLD_LIGHT)
    for y in range(4, 20):
        for x in range(4, 20):
            img.put(x, y, C.NIGHT if BAYER[y % 4][x % 4] < 6 else C.BLACK)       # a dark dither, not a flat fill
    img.inner_outline(img.m_rect(0, 0, 23, 23), C.BLACK)
    return img


CURSOR_LEAN = [0, 1, 0, -1]


def menu_cursor():
    """The menu's cursor: a small flame, 8×8, four frames of flicker."""
    frames = []
    for f in range(4):
        img = Img(8, 8)
        lean = CURSOR_LEAN[f]
        img.paint(img.m_ellipse(4, 5.5, 2.6, 2.4), C.HELL)
        img.paint(img.m_poly([(1.6, 5), (6.4, 5), (4 + lean, 0.5)]), C.HELL)
        img.paint(img.m_ellipse(4, 5.8, 1.5, 1.4), C.AMBER)
        img.put(4 + lean // 2, 3, C.ORANGE)
        img.put(4, 6, C.EMBER)
        img.outline(C.BLACK)
        frames.append(img)
    return sheet(frames)


GLINT_STEPS = [-1, 0, 1, 2, 3, 2, 1, 0]


def menu_glint():
    """A glint on the logo's letters: 9×9, eight frames — a spark that swells into a star and fades (frame 0 empty)."""
    frames = []
    for f in range(8):
        img = Img(9, 9)
        size = GLINT_STEPS[f]
        if size >= 0:
            img.put(4, 4, C.WHITE)
            for k in range(1, size + 1):
                for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)):
                    img.put(4 + dx, 4 + dy, C.EMBER if k == 1 else C.AMBER if k == 2 else C.ORANGE)
            if size >= 2:
                for dx, dy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
                    img.put(4 + dx, 4 + dy, C.EMBER)
        frames.append(img)
    return sheet(frames)


def write_all(out_dir):
    written = write(out_dir, menu_base(), menu_layers(), MENU_PARTICLES, base_name="menu", layer_prefix="menu_",
                    manifest_name="menu_motion", text_zones=MENU_TEXT)
    for name, img in (("menu_logo", menu_logo()), ("menu_box", menu_box()), ("menu_cursor", menu_cursor()), ("menu_glint", menu_glint())):
        path = os.path.join(out_dir, name + ".png")
        img.save(path)
        written.append(path)
    return written
