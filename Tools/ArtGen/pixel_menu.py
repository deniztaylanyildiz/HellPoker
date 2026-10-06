"""The title screen's backdrop: the very bottom of Hell, where the story ends.

One still 480×270 picture with small looping layers over it (see pixel_layers.py) and embers the game draws itself.
- Above: a sky of smoke and dead stars, blotted out by two vast wings spreading from the top edge — and, between them, two
  burning eyes looking down (a layer: they glow and blink). Lucifer himself is never in the picture.
- Below: a sea of fire on the horizon (its wild edges roll in layers), the black ruins of a burning city on either side.
- Edges: Mammon's spilled gold (left), the Dead Man's Hand fanned on a slab (right), Lilith's crescent caught in the left
  wing's shadow, Belial's silver serpent coiled round a broken column (a layer).
Readability first: the middle column (x 70-410, y 25-260), where the logo, the text and the buttons sit, is pushed
down into the dark, and nothing moves there.
Output: Assets/Resources/Art/Ui/menu.png (still), menu_<layer>.png, menu_motion.txt
"""
import math
import os

import numpy as np

from pixel import C, Img, bezier, ribbon, shifted
from pixel_layers import Layer, Particles, phase, write
from pixel_salons import DARKER, glint_layer, lut

W, H = 480, 270
HORIZON = 200
FRAMES = 12


def sky(img):
    """Smoke-dark sky: black at the top, bleeding into blood red toward the burning horizon."""
    full = np.ones((H, W), bool)
    img.dither(full, C.BLACK, C.NIGHT, np.clip(img.ys / 120.0, 0, 1))
    img.dither(img.m_rect(0, 110, W, HORIZON), C.NIGHT, C.BLOOD_DARK, np.clip((img.ys - 110) / 80.0, 0, 1))
    img.dither(img.m_rect(0, 170, W, HORIZON), C.BLOOD_DARK, C.BLOOD, np.clip((img.ys - 170) / 40.0, 0, 0.8))
    rng = np.random.default_rng(5)
    for _ in range(50):   # dead stars, most of them soon hidden by the wings
        img.put(int(rng.integers(0, W)), int(rng.integers(0, 110)), C.MAUVE if rng.random() < 0.7 else C.LILAC_LIGHT)


def wings(img):
    """Two vast membranes spreading from the top edge, ribbed, their hooked tips reaching down the sides."""
    sway = 0
    for side in (-1, 1):
        cx = 240 + side * 20
        outer = [(cx, 0), (cx + side * 230, -4), (240 + side * 248, 40 + sway), (240 + side * 232, 92 + sway)]
        # The scalloped lower edge: membrane hanging between the ribs.
        ribs = [(240 + side * x, y + sway) for x, y in ((214, 88), (178, 60), (142, 52), (104, 34), (66, 22), (36, 8))]
        edge = []
        for (x0, y0), (x1, y1) in zip(ribs, ribs[1:]):
            mid = ((x0 + x1) / 2, max(y0, y1) + 10)
            edge += bezier([(x0, y0), mid, (x1, y1)], 6)
        poly = outer + edge + [(cx, 0)]
        wing = img.m_poly(poly)
        img.paint(wing, C.BLACK)
        for x, y in ribs:
            bone = img.m_line([(cx, 0), (x, y)], 1) & wing
            img.paint(bone, C.VIOLET)
        # The membrane's lit hem, so the wings read against the smoke.
        hem = wing & ~shifted(wing, 0, 1)
        img.paint(hem, C.MAUVE)
        img.paint(wing & ~shifted(wing, 0, 2) & ~hem, C.PLUM)
        # The hooked claw at the wing's tip.
        tx, ty = 240 + side * 232, 92 + sway
        img.paint(img.m_poly(ribbon([(tx, ty - 6), (tx + side * 3, ty), (tx, ty + 6)], 3, 1)), C.BONE_SHADE)


EYES = (226, 254)
EYES_Y = 13.5
# The eyes' layer: a box round both of them (screen x 214..266, y 3..24).
EYES_BOX = (214, 3, 53, 22)
# Over 12 frames: the glow deepens and eases, and once a loop they narrow, close and open again.
EYE_OPEN = [2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 1.2, 0.0, 1.2, 2.2]
EYE_CORE = [C.ORANGE, C.ORANGE, C.AMBER, C.AMBER, C.AMBER, C.ORANGE, C.ORANGE, C.ORANGE, C.ORANGE, C.ORANGE, C.ORANGE, C.ORANGE]


def eye_halos(img):
    """The dim red glow round his eyes (still, on the backdrop)."""
    for ex in EYES:
        halo = np.hypot(img.xs - ex - 0.5, img.ys - EYES_Y) / 9.0
        img.dither(halo < 1, C.BLACK, C.BLOOD_DARK, np.clip(0.5 * (1 - halo), 0, 1))


def eyes_layer():
    """His eyes, high above the logo: two slits of fire in the dark between the wings. They close once a loop."""
    ox, oy = EYES_BOX[0], EYES_BOX[1]

    def draw(img, f):
        for ex in EYES:
            cx, cy = ex + 0.5 - ox, EYES_Y - oy
            open_ = EYE_OPEN[f]
            if open_ <= 0:
                img.paint(img.m_rect(ex - 4 - ox, 13 - oy, ex + 4 - ox, 13 - oy), C.BLOOD_DARK)
                continue
            almond = img.m_ellipse(cx, cy, 5.2, open_)
            img.paint(almond, C.HELL)
            img.paint(img.m_ellipse(cx, cy, 3.6, max(0.6, open_ - 0.9)) & almond, EYE_CORE[f])
            img.paint(img.m_ellipse(cx, cy, 2.0, max(0.5, open_ - 1.4)) & almond, C.EMBER)
            for y in (12, 13, 14):
                if almond[y - oy, ex - ox]:
                    img.put(ex - ox, y - oy, C.BLACK)
    return Layer("eyes", ox, oy, EYES_BOX[2], EYES_BOX[3], FRAMES, 8, draw)


def crescent(img):
    """Lilith's moon, caught low in the shadow of the left wing."""
    moon = img.m_ellipse(40, 120, 12, 12) & ~img.m_ellipse(46, 116, 11, 11)
    img.shade(moon, [C.MAUVE, C.LILAC, C.LILAC_LIGHT, C.WHITE], light=(-1, 1), shadow=1)


def city(img):
    """Black ruins on the horizon at both sides, windows burning, smoke stacks leaning."""
    frame = 0
    rng = np.random.default_rng(17)
    for x0, x1 in ((0, 92), (388, W)):
        x = x0
        while x < x1:
            w = int(rng.integers(6, 15))
            h = int(rng.integers(12, 44))
            top = HORIZON - h
            tower = img.m_rect(x, top, x + w - 1, HORIZON + 4)
            img.paint(tower, C.BLACK)
            if rng.random() < 0.4:   # a broken spire
                img.paint(img.m_poly([(x + 1, top), (x + w // 2, top - int(rng.integers(5, 14))), (x + w - 2, top)]), C.BLACK)
            for wy in range(top + 3, HORIZON, 5):
                for wx in range(x + 2, x + w - 2, 4):
                    if rng.random() < 0.35:
                        lit = (wx + wy + frame) % 7 != 0
                        img.put(wx, wy, C.ORANGE if lit else C.RED)
            x += w + int(rng.integers(0, 3))


def fire_color(x, y, f):
    """
    The sea of fire at a screen pixel, at frame f of its loop. Two crests rolling opposite ways, with periods of 24 and
    12 px so that over the 12-frame loop they move 2 px and 1 px a frame — and close exactly.
    """
    depth = (y - HORIZON) / (H - HORIZON)
    p = phase(f, FRAMES)
    wave = math.sin(2 * math.pi * x / 24 + y * 0.35 + p) + 0.6 * math.sin(2 * math.pi * x / 12 - p + y * 0.1)
    # A still, irregular swell under the crests, so the sea does not read as a regular hatch.
    wave += 0.7 * math.sin(x * 0.071 + y * 0.53) * math.sin(x * 0.033 - y * 0.21)
    wave /= 2.0
    # Calm under the buttons (the middle), wild toward the sides.
    edge = min(1.0, max(0.0, (abs(x - 240) - 120) / 60.0))
    heat = (wave * 0.5 + 0.5) * (0.45 + 0.55 * edge)
    if heat > 0.85:
        return C.AMBER if depth < 0.4 else C.ORANGE
    if heat > 0.6:
        return C.HELL
    if heat > 0.35:
        return C.RED
    return C.CRIMSON if depth < 0.5 else C.BLOOD


def fire_sea(img):
    """A sea of fire from the horizon down: rolling crests, burning at the edges, only embers glowing under the menu."""
    for y in range(HORIZON, H):
        for x in range(W):
            img.put(x, y, fire_color(x, y, 0))
    img.paint(img.m_rect(0, HORIZON, W, HORIZON), C.EMBER)


# The wild edges of the sea roll (layers); the middle, under the buttons and the footer, stays still.
FIRE_EDGES = (("fire_left", 0, 88), ("fire_right", 392, 88))
# Toward the still middle the motion fades out over this many pixels (dithered), so there is no seam.
FIRE_FADE = 20
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def fire_layer(name, x0, w):
    inner = x0 + w - 1 if x0 < 240 else x0   # the edge facing the middle

    def draw(img, f):
        for y in range(HORIZON + 1, H):
            for x in range(x0, x0 + w):
                still = 1 - min(1.0, abs(x - inner) / FIRE_FADE)   # 1 at the inner edge, 0 from FIRE_FADE px out
                moving = still * 16 <= BAYER[y % 4][x % 4]
                img.put(x - x0, y - HORIZON - 1, fire_color(x, y, f if moving else 0))
    return Layer(name, x0, HORIZON + 1, w, H - HORIZON - 1, FRAMES, 8, draw, vignette=quiet_middle)


GOLD_GLINTS = ((20, 254), (52, 252), (8, 262), (66, 262))


def gold(img):
    """Mammon's spilled treasure at the bottom left: a heap of coins, a tipped chest (its coins glint in layers)."""
    heap = img.m_ellipse(34, 272, 46, 26)
    img.shade(heap, [C.GOLD_DARK, C.GOLD_MID, C.GOLD, C.GOLD_LIGHT], shadow=2)
    rng = np.random.default_rng(23)
    for _ in range(60):
        x, y = int(rng.integers(0, 78)), int(rng.integers(248, H))
        if heap[min(y, H - 1), x]:
            img.put(x, y, C.GOLD_DARK)
            img.put(x + 1, y, C.GOLD_LIGHT)
    # A dark wooden chest bound in gold, its lid thrown open and coins spilling out.
    chest = img.m_rect(46, 236, 70, 250)
    img.paint(chest, C.BLOOD_DARK)
    img.paint(img.m_rect(46, 240, 70, 240), C.BLOOD)
    for bx in (49, 67):
        img.paint(img.m_rect(bx, 236, bx + 1, 250), C.GOLD_MID)
    img.paint(img.m_rect(56, 241, 60, 245), C.GOLD)
    img.put(58, 243, C.BLACK)
    img.inner_outline(chest, C.BLACK)
    lid = img.m_poly([(45, 236), (71, 236), (74, 226), (48, 224)])
    img.paint(lid, C.BLOOD)
    img.inner_outline(lid, C.GOLD_DARK)
    for x in range(50, 68, 3):
        img.put(x, 235, C.GOLD_LIGHT if x % 2 else C.GOLD)


def card(img, x, y, rank, suit_rows, ink):
    """A tiny 14×20 card, face up: rank in the corner, the suit in the middle."""
    face = img.m_rect(x, y, x + 13, y + 19)
    img.paint(face, C.BONE)
    img.inner_outline(face, C.BLACK)
    img.rows(x + 2, y + 2, rank, {"#": ink})
    img.rows(x + 5, y + 9, suit_rows, {"k": ink})


GLYPH_A = [".#.", "#.#", "###", "#.#", "#.#"]
GLYPH_8 = ["###", "#.#", "###", "#.#", "###"]
SPADE = ["..k..", ".kkk.", "kkkkk", "..k..", ".kkk."]
CLUB = [".kkk.", ".kkk.", "kkkkk", "k.k.k", "..k.."]


def dead_mans_hand(img):
    """A♠ A♣ 8♠ 8♣ fanned on a stone slab at the bottom right — the one hand that walks out of here."""
    slab = img.m_rect(404, 236, 476, 262)
    img.paint(slab, C.DUSK)
    img.paint(img.m_rect(404, 236, 476, 237), C.PLUM)
    img.inner_outline(slab, C.BLACK)
    for i, (rank, suit) in enumerate(((GLYPH_A, SPADE), (GLYPH_A, CLUB), (GLYPH_8, SPADE), (GLYPH_8, CLUB))):
        card(img, 410 + i * 15, 230 - (1 if i in (1, 2) else 0), rank, suit, C.BLACK)


COLUMN_X = 448
# The serpent's layer: round the column from its broken top to the horizon (screen x 436..460, y 90..200).
SERPENT_BOX = (434, 90, 29, HORIZON - 90 + 1)


def serpent_column(img):
    """A broken column at the right (Belial's silver serpent winds round it in a layer)."""
    x = COLUMN_X
    column = img.m_rect(x - 7, 104, x + 7, HORIZON)
    img.paint(column, C.BONE_SHADE)
    img.paint(img.m_rect(x - 7, 104, x - 6, HORIZON), C.BONE_DARK)
    img.paint(img.m_poly([(x - 9, 104), (x - 4, 96), (x + 2, 102), (x + 8, 94), (x + 9, 104)]), C.BONE_SHADE)
    img.inner_outline(column, C.BLACK)


def serpent_layer():
    """The serpent's coils slide round the column, at most two pixels a frame; its head sways with them."""
    ox, oy, w, h = SERPENT_BOX

    def draw(img, f):
        spine = [(COLUMN_X - ox + 4 * math.sin(y * 0.16 + phase(f, FRAMES)), y - oy) for y in range(108, HORIZON, 3)]
        img.paint(img.m_poly(ribbon(spine, 4, 3)), C.SILVER_DARK)
        hx, hy = spine[0]
        img.paint(img.m_ellipse(hx + 2, hy - 2, 3, 2), C.SILVER)
        img.put(int(hx) + 3, int(hy) - 3, C.HELL)
    return Layer("serpent", ox, oy, w, h, FRAMES, 10, draw)


def quiet_middle(img, ox=0, oy=0):
    """
    Two steps darker over the middle column (x 70-410, y 25-260), fading out over ~24 px, so the logo, the text and the
    buttons always stand clear. The eyes above the logo stay lit. (ox, oy): where the image sits on the screen, so a
    layer gets the same darkness, with the same dither, as the backdrop under it.
    """
    darker = lut(DARKER)
    once = darker[img.px]
    twice = darker[once]
    xs, ys = img.xs + ox, img.ys + oy
    threshold = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]], np.float32)[
        (np.arange(img.h)[:, None] + oy) % 4, (np.arange(img.w)[None, :] + ox) % 4] / 16.0

    def field(x0, y0, x1, y1, fade):
        dx = np.maximum(np.maximum(x0 - xs, xs - x1), 0)
        dy = np.maximum(np.maximum(y0 - ys, ys - y1), 0)
        return np.clip(1 - np.hypot(dx, dy) / fade, 0, 1)

    middle = field(94, 30, 386, 256, 24)
    out = np.where(middle > threshold, once, img.px)
    out = np.where(middle * 0.8 > threshold + 0.05, twice, out)
    img.px = out


def menu_base():
    img = Img(W, H, C.BLACK)
    sky(img)
    crescent(img)
    fire_sea(img)
    city(img)
    wings(img)
    serpent_column(img)
    gold(img)
    dead_mans_hand(img)
    quiet_middle(img)
    eye_halos(img)   # above the logo, never darkened (the eyes themselves are a layer)
    return img


def menu_layers():
    layers = [eyes_layer(), serpent_layer()]
    layers += [fire_layer(name, x0, w) for name, x0, w in FIRE_EDGES]
    for i, (x, y) in enumerate(GOLD_GLINTS):
        layers.append(glint_layer(f"glint{i}", x, y, offset=i * 3))
    layers.append(glint_layer("glint_cards", 412, 230, offset=7))
    return layers


# Embers rise from the burning city on either side, never across the logo, the words or the buttons.
MENU_PARTICLES = [Particles("embers", 0, 30, 90, 170, 14), Particles("embers", 390, 30, 90, 170, 14)]

# Where words sit on the title screen (nothing moves there): the taglines, the button grid, the footer, and the version
# in the bottom right corner.
MENU_TEXT = ((88, 84, 392, 126), (100, 128, 380, 240), (176, 250, 304, 266), (400, 256, 479, 269), (204, 70, 276, 82))   # the last: DEMO


def write_all(out_dir):
    return write(out_dir, menu_base(), menu_layers(), MENU_PARTICLES, base_name="menu", layer_prefix="menu_",
                 manifest_name="menu_motion", text_zones=MENU_TEXT)
