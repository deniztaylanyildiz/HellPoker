"""The demons' gambling halls: a still 480×270 backdrop each, with small looping layers and particles for the motion, in
three variants.

normal — the hall as it is; hell — reddened for the final stretch; soul — cold, pale and ghostly for when the soul is on the table.
Readability first: the middle of the screen, where the cards, the talk and the controls sit, is pushed down into the dark,
and nothing moves there; the character of each hall lives at the edges (the top band, the gutters beside the cards, the
bottom strip). Every moving thing is its own small strip (see pixel_layers.py): a pixel or two per frame, loops that close.
Output: Assets/Resources/Art/Backgrounds/<dealerId>/<variant>.png (one still frame), <layer>[_<variant>].png, motion.txt.
"""
import math
import os

import numpy as np

from pixel import C, Img, bezier, ribbon, shifted
from pixel_layers import Layer, Particles, phase, write

W, H = 480, 270
LAYER_FRAMES = 12
LAYER_FPS = 10

# Where words sit on the table, over the hall (x0, y0, x1, y1, inclusive): nothing moves there.
# The demon's name and title under the portrait; the middle column (messages, the stake, the hand's name, the banners);
# the shortcut line along the bottom. Everything else with words is on a panel.
TABLE_TEXT = ((0, 104, 112, 132), (138, 88, 342, 240), (118, 248, 476, 266))

# Particles keep to the sides, clear of the words: above and below the demon's name on the left, beside the panels on the right.
LEFT_HIGH, LEFT_LOW, RIGHT_SIDE = (0, 0, 136, 100), (0, 134, 136, 112), (344, 0, 136, 246)

# One step darker for every palette colour (used for the central vignette).
DARKER = {
    C.NIGHT: C.BLACK, C.DUSK: C.NIGHT, C.PLUM: C.DUSK, C.VIOLET: C.PLUM, C.MAUVE: C.VIOLET,
    C.BLOOD_DARK: C.BLACK, C.BLOOD: C.BLOOD_DARK, C.CRIMSON: C.BLOOD, C.RED: C.CRIMSON, C.HELL: C.RED,
    C.ORANGE: C.HELL, C.AMBER: C.ORANGE, C.EMBER: C.AMBER,
    C.BONE: C.BONE_MID, C.BONE_MID: C.BONE_DARK, C.BONE_DARK: C.BONE_SHADE, C.BONE_SHADE: C.DUSK,
    C.GOLD_DARK: C.BONE_SHADE, C.GOLD_MID: C.GOLD_DARK, C.GOLD: C.GOLD_MID, C.GOLD_LIGHT: C.GOLD,
    C.GREEN_DARK: C.BLACK, C.GREEN: C.GREEN_DARK, C.GREEN_LIGHT: C.GREEN,
    C.LILAC: C.MAUVE, C.LILAC_LIGHT: C.LILAC, C.SILVER_DARK: C.BONE_SHADE, C.SILVER: C.SILVER_DARK, C.WHITE: C.SILVER,
}

# Final stretch: everything runs hot.
HELL = {
    C.NIGHT: C.BLOOD_DARK, C.DUSK: C.BLOOD, C.PLUM: C.CRIMSON, C.VIOLET: C.RED, C.MAUVE: C.HELL,
    C.GREEN_DARK: C.BLOOD_DARK, C.GREEN: C.BLOOD, C.GREEN_LIGHT: C.CRIMSON,
    C.LILAC: C.HELL, C.LILAC_LIGHT: C.ORANGE, C.SILVER_DARK: C.CRIMSON, C.SILVER: C.HELL, C.WHITE: C.AMBER,
    C.BONE_SHADE: C.BLOOD, C.BONE_DARK: C.RED, C.BONE_MID: C.ORANGE, C.BONE: C.AMBER,
}

# The soul on the table: warmth drains away, everything turns cold, pale and ghostly.
SOUL = {
    C.BLOOD_DARK: C.NIGHT, C.BLOOD: C.DUSK, C.CRIMSON: C.PLUM, C.RED: C.VIOLET, C.HELL: C.MAUVE,
    C.ORANGE: C.LILAC, C.AMBER: C.LILAC_LIGHT, C.EMBER: C.WHITE,
    C.GOLD_DARK: C.PLUM, C.GOLD_MID: C.SILVER_DARK, C.GOLD: C.SILVER, C.GOLD_LIGHT: C.WHITE,
    C.GREEN_DARK: C.NIGHT, C.GREEN: C.DUSK, C.GREEN_LIGHT: C.SILVER_DARK,
    C.BONE_SHADE: C.PLUM, C.BONE_DARK: C.VIOLET, C.BONE_MID: C.SILVER_DARK, C.BONE: C.SILVER,
}


def lut(mapping):
    table = np.arange(256, dtype=np.uint8)
    for a, b in mapping.items():
        table[a] = b
    return table


VARIANT_TABLES = {"normal": None, "hell": lut(HELL), "soul": lut(SOUL)}


# ------------------------------------------------------------------ shared pieces

def stone_wall(img, base, light, mortar, seed, row_h=18):
    rng = np.random.default_rng(seed)
    img.px[:] = base
    for row in range(H // row_h + 1):
        y0 = row * row_h
        x = -int(rng.integers(0, 30))
        while x < W:
            w = int(rng.integers(26, 46))
            img.paint(img.m_rect(x + 1, y0 + 1, x + w - 2, y0 + 1), light)
            img.paint(img.m_rect(x + w - 1, y0 + 1, x + w - 1, y0 + row_h - 1), mortar)
            x += w
        img.paint(img.m_rect(0, y0, W, y0), mortar)


def readable_middle(img, ox=0, oy=0):
    """
    A soft, dithered vignette over the middle column: one step darker there, fading out over ~24 px, and a second step
    behind the two rows of cards so they always stand clear of the hall. (ox, oy) is where the image sits on the screen —
    a layer gets exactly the darkness of the hall under it, with the same dither.
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

    middle = field(150, 30, 330, 236, 26)
    hint = field(70, 251, 474, 263, 6)   # the shortcut line along the bottom
    cards = np.maximum(field(146, 36, 334, 88, 10), field(146, 130, 334, 190, 10))
    out = np.where(np.maximum(middle, hint) > threshold, once, img.px)
    out = np.where(cards * 0.75 > threshold, twice, out)
    img.px = out


def sparkle(img, x, y, size=1):
    img.put(x, y, C.WHITE)
    for k in range(1, size + 1):
        for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)):
            img.put(x + dx, y + dy, C.GOLD_LIGHT if k == 1 else C.GOLD)


# A glint swells and fades over a few frames, then rests: frame 0 is always at rest, so the loop closes.
GLINT_SIZES = [-1, -1, -1, -1, -1, -1, 0, 1, 2, 1, 0, -1]


def glint_layer(name, x, y, offset, colors=(C.WHITE, C.GOLD_LIGHT, C.GOLD), vignette=None):
    """A coin or a card catching the light: a 7×7 layer centred on (x, y)."""
    def draw(img, f):
        size = GLINT_SIZES[f]
        if size < 0:
            return
        img.put(3, 3, colors[0] if size > 0 else colors[1])
        for k in range(1, size + 1):
            for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)):
                img.put(3 + dx, 3 + dy, colors[1] if k == 1 else colors[2])
    return Layer(name, x - 3, y - 3, 7, 7, LAYER_FRAMES, LAYER_FPS, draw, offset=offset, vignette=vignette)


def coin_stack(img, x, bottom, height, seed):
    """A column of gold coins seen from the side, slightly uneven."""
    rng = np.random.default_rng(seed)
    for i in range(height):
        y = bottom - i * 3
        jitter = int(rng.integers(-1, 2))
        m = img.m_ellipse(x + jitter + 0.5, y + 0.5, 7, 2.2)
        img.paint(m, C.GOLD_MID)
        img.paint(m & ~shifted(m, 0, 1), C.GOLD_DARK)
        img.paint(m & ~shifted(m, 0, -1), C.GOLD_LIGHT if i == height - 1 else C.GOLD)


def gold_heap(img, cx, bottom, width, height, seed):
    """A mound of loose coins."""
    m = img.m_ellipse(cx, bottom, width / 2, height)
    img.shade(m, [C.GOLD_DARK, C.GOLD_MID, C.GOLD, C.GOLD_LIGHT], shadow=2)
    rng = np.random.default_rng(seed)
    for _ in range(int(width * height / 30)):
        x = int(rng.integers(cx - width // 2 + 3, cx + width // 2 - 3))
        y = int(rng.integers(bottom - height + 3, bottom))
        if m[min(y, H - 1), min(max(x, 0), W - 1)]:
            img.put(x, y, C.GOLD_DARK)
            img.put(x + 1, y, C.GOLD_LIGHT)


# ================================================================== MAMMON — the usurer's treasure vault

def mammon_base():
    img = Img(W, H)
    stone_wall(img, C.GREEN_DARK, C.GREEN, C.BLACK, seed=21)
    # Iron bands riveted across the vault wall.
    for y in (60, 150):
        img.paint(img.m_rect(0, y, W, y + 3), C.BLACK)
        for x in range(6, W, 24):
            img.put(x, y + 1, C.GOLD_DARK)

    # A great balance hanging over the hall: the post and the beam stand still; the pans sway (layers).
    img.paint(img.m_rect(239, 0, 241, 30), C.GOLD_DARK)
    img.paint(img.m_line([(140, 30), (340, 30)], 3), C.GOLD)
    img.paint(img.m_line([(140, 29), (340, 29)], 1), C.GOLD_LIGHT)

    # Shelves of debt ledgers along the top corners.
    for x0 in (0, 360):
        img.paint(img.m_rect(x0, 44, x0 + 120, 46), C.BONE_SHADE)
        for i in range(14):
            x = x0 + 4 + i * 8
            h = 14 + (i * 7) % 6
            color = [C.CRIMSON, C.GREEN_DARK, C.BONE_SHADE, C.BLOOD][i % 4]
            img.paint(img.m_rect(x, 44 - h, x + 6, 43), color)
            img.put(x + 3, 44 - h + 3, C.GOLD)

    # Columns of coins beside the cards.
    for i, (x, height) in enumerate(((118, 30), (132, 22), (348, 26), (362, 34))):
        coin_stack(img, x, 236, height, seed=40 + i)

    # Locked strongboxes and heaps of gold along the floor.
    for cx, w in ((60, 110), (250, 150), (430, 110)):
        gold_heap(img, cx, H + 6, w, 24, seed=cx)
    for x0 in (8, 400):
        box = img.m_rect(x0, 228, x0 + 64, 262)
        img.paint(box, C.BONE_SHADE)
        img.paint(img.m_rect(x0, 228, x0 + 64, 233), C.GOLD_DARK)
        for bx in (x0 + 10, x0 + 54):
            img.paint(img.m_rect(bx, 228, bx + 2, 262), C.GOLD_MID)
        img.paint(img.m_rect(x0 + 28, 238, x0 + 36, 248), C.GOLD)
        img.put(x0 + 32, 243, C.BLACK)
        img.inner_outline(box, C.BLACK)

    readable_middle(img)
    return img


def pan_layer(name, px, side):
    """One pan of the balance with its chains, bobbing a pixel up and down — the two pans in opposite phase."""
    x0, y0, w, h = px - 21, 28, 43, 44

    def draw(img, f):
        dy = int(round(math.sin(phase(f, LAYER_FRAMES)))) * side
        lx, ly = px - x0, 30 - y0
        for k in (-14, 14):
            img.paint(img.m_line([(lx, ly), (lx + k, ly + 26 + dy)], 1), C.GOLD_DARK)
        pan = img.m_ellipse(lx + 0.5, ly + 28 + dy, 17, 5)
        img.shade(pan, [C.GOLD_DARK, C.GOLD_MID, C.GOLD, C.GOLD_LIGHT], shadow=1)
        img.inner_outline(pan, C.BLACK)
        coin_stack(img, lx, ly + 24 + dy, 3 + (side > 0), seed=px)
    return Layer(name, x0, y0, w, h, LAYER_FRAMES, 8, draw, vignette=readable_middle)


def mammon():
    layers = [pan_layer("pan_left", 140, 1), pan_layer("pan_right", 340, -1)]
    # Coins glint in turn, at the edges only (never under the text).
    glints = [(118, 150), (348, 160), (362, 136), (60, 254), (462, 244), (132, 172), (36, 200), (452, 186), (124, 96), (366, 110)]
    for i, (x, y) in enumerate(glints):
        layers.append(glint_layer(f"glint{i}", x, y, offset=(i * 5) % LAYER_FRAMES, vignette=readable_middle))
    particles = [Particles("motes", *LEFT_LOW, 8), Particles("motes", *RIGHT_SIDE, 10)]
    return mammon_base(), layers, particles


# ================================================================== BELIAL — the silver tongue's theatre

CURTAINS = ((96, 152), (328, 384))
CURTAIN_TOP, CURTAIN_BOTTOM = 36, 235


def curtain_color(x, x0, f):
    wave = math.sin((x - x0) * 0.55 + phase(f, LAYER_FRAMES))
    return C.SILVER if wave > 0.55 else C.SILVER_DARK if wave > -0.3 else C.BONE_SHADE


def curtain_sway(y, f):
    return int(round(math.sin(y * 0.04 + phase(f, LAYER_FRAMES)) * 1.2))


def masks(img, ox, oy):
    """Comedy and tragedy masks pinned to the curtains."""
    for cx, sad in ((124, False), (356, True)):
        cx, cy = cx - ox, 100 - oy
        face = img.m_ellipse(cx, cy, 11, 13)
        img.shade(face, [C.BONE_SHADE, C.BONE_DARK, C.BONE_MID, C.BONE], shadow=1)
        img.inner_outline(face, C.BLACK)
        for ex in (cx - 5, cx + 3):
            img.paint(img.m_rect(ex, cy - 5, ex + 2, cy - 2), C.BLACK)
        mouth = bezier([(cx - 5, cy + 7), (cx, cy + 11 if not sad else cy + 4), (cx + 5, cy + 7)], 8)
        img.paint(img.m_line(mouth, 1), C.BLACK)
        img.put(cx - 6, cy - 8, C.RED)
        img.put(cx + 5, cy - 8, C.RED)


def belial_base():
    img = Img(W, H)
    stone_wall(img, C.BLOOD_DARK, C.BLOOD, C.BLACK, seed=7)

    # The silver curtains as they hang at rest; their outer folds ripple in their own layers.
    for x0, x1 in CURTAINS:
        for x in range(x0, x1):
            color = curtain_color(x, x0, 0)
            for y in range(28, H):
                img.put(x + curtain_sway(y, 0), y, color)
        img.inner_outline(img.m_rect(x0, 28, x1 - 1, H - 1), C.BLACK)

    # Blood-red valance with gold trim and silver tassels across the top.
    valance = img.m_rect(0, 0, W - 1, 22)
    img.paint(valance, C.CRIMSON)
    for x in range(0, W, 24):
        scallop = img.m_ellipse(x + 12, 22, 12, 7)
        img.paint(scallop, C.CRIMSON)
        img.paint(scallop & ~shifted(scallop, 0, 1), C.GOLD_MID)
        img.paint(img.m_rect(x + 11, 28, x + 12, 34), C.SILVER)
        img.put(x + 11, 35, C.WHITE)
    for x in range(0, W, 4):
        img.put(x, 3, C.GOLD)
        img.put(x + 2, 6, C.BLOOD)

    masks(img, 0, 0)

    # Rows of empty red seats in the dark (the serpent columns above them are layers).
    for row, y in enumerate((236, 252)):
        for x in range(4 - row * 10, W, 20):
            seat = img.m_rect(x, y, x + 14, y + 12)
            img.paint(seat, C.BLOOD if row == 0 else C.BLOOD_DARK)
            img.paint(img.m_rect(x, y, x + 14, y + 1), C.CRIMSON)
            img.inner_outline(seat, C.BLACK)

    readable_middle(img)
    return img


def curtain_layer(name, x0, x1, lx0, lx1):
    """The outer folds of a curtain (screen x lx0..lx1), rippling about a pixel a frame; the masks stay pinned on top."""
    w, h = lx1 - lx0 + 1, CURTAIN_BOTTOM - CURTAIN_TOP + 1

    def draw(img, f):
        for x in range(x0, x1):
            color = curtain_color(x, x0, f)
            for y in range(CURTAIN_TOP, CURTAIN_BOTTOM + 1):
                sx = x + curtain_sway(y, f)
                if lx0 <= sx <= lx1:
                    img.put(sx - lx0, y - CURTAIN_TOP, color)
        if lx0 <= x0 <= lx1:
            img.paint(img.m_rect(x0 - lx0, 0, x0 - lx0, h - 1), C.BLACK)
        if lx0 <= x1 - 1 <= lx1:
            img.paint(img.m_rect(x1 - 1 - lx0, 0, x1 - 1 - lx0, h - 1), C.BLACK)
        masks(img, lx0, CURTAIN_TOP)
    return Layer(name, lx0, CURTAIN_TOP, w, h, LAYER_FRAMES, LAYER_FPS, draw, vignette=readable_middle)


def column_layer(name, cx):
    """A column with Belial's silver serpent winding up it, the coils drifting at most two pixels a frame."""
    x0, y0 = cx - 11, 30
    w, h = 23, CURTAIN_BOTTOM - y0 + 1

    def draw(img, f):
        lx = cx - x0
        img.paint(img.m_rect(lx - 5, 0, lx + 5, h - 1), C.BONE_SHADE)
        img.paint(img.m_rect(lx - 5, 0, lx - 4, h - 1), C.BONE_DARK)
        spine = [(lx + 3 * math.sin(y * 0.12 + phase(f, LAYER_FRAMES)), y - y0) for y in range(40, CURTAIN_BOTTOM + 4, 4)]
        img.paint(img.m_poly(ribbon(spine, 4, 4)), C.SILVER_DARK)
    return Layer(name, x0, y0, w, h, LAYER_FRAMES, LAYER_FPS, draw, vignette=readable_middle)


def belial():
    # Only the outer folds move: the inner edge of each curtain (next to the text) stays still.
    layers = [curtain_layer("curtain_left", 96, 152, 95, 140), curtain_layer("curtain_right", 328, 384, 340, 385),
              column_layer("column_left", 100), column_layer("column_right", 372)]
    return belial_base(), layers, []


# ================================================================== LILITH — the garden of night, the moon crypt

def lilith_base():
    img = Img(W, H, C.NIGHT)
    # Night sky lightening toward a violet horizon, so the black trees stand out against it.
    full = np.ones((H, W), bool)
    img.dither(full, C.BLACK, C.NIGHT, np.clip(img.ys / 90.0, 0, 1))
    img.dither(img.m_rect(0, 90, W, H), C.NIGHT, C.DUSK, np.clip((img.ys - 90) / 70.0, 0, 1))
    img.dither(img.m_rect(0, 160, W, H), C.DUSK, C.PLUM, np.clip((img.ys - 160) / 70.0, 0, 1))
    img.dither(img.m_rect(0, 220, W, H), C.PLUM, C.VIOLET, np.clip((img.ys - 220) / 80.0, 0, 0.7))
    rng = np.random.default_rng(31)
    for _ in range(70):
        x, y = int(rng.integers(0, W)), int(rng.integers(0, 150))
        img.put(x, y, C.LILAC_LIGHT if rng.random() < 0.3 else C.MAUVE)

    # A huge crescent moon over the garden wall.
    moon = img.m_ellipse(126, 22, 30, 30) & ~img.m_ellipse(140, 12, 27, 27)
    img.shade(moon, [C.LILAC, C.LILAC_LIGHT, C.SILVER, C.WHITE], light=(-1, 1), shadow=2)
    halo = img.m_ellipse(126, 22, 36, 36) & ~img.m_ellipse(126, 22, 31, 31) & ~img.m_ellipse(140, 12, 27, 27)
    img.dither(halo, C.NIGHT, C.VIOLET, 0.4)

    # Dead trees reaching up beside the cards.
    def branch(x, y, angle, length, width, depth):
        if depth == 0 or length < 4:
            return
        x2 = x + math.cos(angle) * length
        y2 = y - math.sin(angle) * length
        img.paint(img.m_line([(x, y), (x2, y2)], max(1, int(width))), C.BLACK)
        branch(x2, y2, angle + 0.45, length * 0.72, width * 0.7, depth - 1)
        branch(x2, y2, angle - 0.5, length * 0.68, width * 0.7, depth - 1)

    branch(118, H, math.pi / 2 + 0.05, 60, 5, 6)
    branch(364, H, math.pi / 2 - 0.1, 66, 5, 6)

    # Gravestones overgrown with purple vines, a candle on one (its flame is a layer).
    for x0, h in ((10, 34), (60, 26), (196, 22), (270, 28), (410, 36), (450, 24)):
        stone = img.m_rect(x0, H - h, x0 + 22, H) | img.m_ellipse(x0 + 11, H - h, 11, 8)
        img.shade(stone, [C.DUSK, C.PLUM, C.VIOLET, C.MAUVE], shadow=1)
        img.inner_outline(stone, C.BLACK)
        img.paint(img.m_rect(x0 + 8, H - h + 4, x0 + 14, H - h + 5), C.DUSK)
        img.paint(img.m_rect(x0 + 10, H - h + 1, x0 + 11, H - h + 10), C.DUSK)
        vine = bezier([(x0 + 2, H), (x0 + 6, H - h * 0.6), (x0 + 16, H - h * 0.4), (x0 + 20, H - h + 2)], 16)
        img.paint(img.m_line(vine, 1) & stone, C.MAUVE)
        for k in range(0, 16, 4):
            vx, vy = vine[k]
            img.put(int(vx) + 1, int(vy), C.LILAC)
    img.paint(img.m_rect(281, H - 31, 282, H - 27), C.BONE)

    readable_middle(img)
    return img


# A candle flame: flickering in place, its tip leaning a pixel left and right.
FLAME_COLORS = [C.AMBER, C.EMBER, C.EMBER, C.AMBER, C.ORANGE, C.AMBER, C.EMBER, C.AMBER]
FLAME_LEAN = [0, 0, 1, 1, 0, 0, -1, -1]


def flame_layer():
    def draw(img, f):
        img.put(2, 3, FLAME_COLORS[f])
        img.put(2 + FLAME_LEAN[f], 2, C.AMBER)
        if f % 4 in (1, 2):
            img.put(2 + FLAME_LEAN[f], 1, C.ORANGE)
    return Layer("flame", 279, H - 35, 5, 4, 8, 8, draw)


# A night bird: its wingtips rise and fall a pixel at a time.
WING = [-1, -1, 0, 1, 1, 1, 0, -1]


def bird_layer(name, x, y, offset):
    def draw(img, f):
        tip = WING[f]
        for dx, dy in ((0, 1 + tip), (1, 1 + max(0, tip)), (2, 1), (3, 1), (4, 1), (5, 1 + max(0, tip)), (6, 1 + tip)):
            img.put(dx, dy, C.BLACK)
    # Flies right across the top band, about 9 px a second, coming round again off the left edge.
    return Layer(name, x, y, 7, 3, 8, 8, draw, drift=9, wrap=W + 7, offset=offset)


def star_layer(name, x, y, offset):
    def draw(img, f):
        size = GLINT_SIZES[f]
        if size < 0:
            return
        img.put(1, 1, C.WHITE if size > 0 else C.LILAC_LIGHT)
        if size > 1:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                img.put(1 + dx, 1 + dy, C.LILAC)
    return Layer(name, x - 1, y - 1, 3, 3, LAYER_FRAMES, 8, draw, offset=offset)


def lilith():
    layers = [flame_layer()]
    for i, (x, y) in enumerate(((180, 12), (232, 6), (300, 16))):
        layers.append(bird_layer(f"bird{i}", x, y, offset=i * 3))
    rng = np.random.default_rng(32)
    for i in range(6):
        layers.append(star_layer(f"star{i}", int(rng.integers(150, 330)), int(rng.integers(4, 26)), offset=i * 2))
    particles = [Particles("wisps", *LEFT_LOW, 6), Particles("wisps", 344, 150, 136, 96, 6)]
    return lilith_base(), layers, particles


# ================================================================== LUCIFER — the foot of a throne too high to see
#
# Only the lowest steps of a colossal throne; chains run up into the dark beyond the top of the frame. Red by default,
# embers rising at the sides. Nothing of him, nor of the throne's seat, is ever in the picture.

# His final moments: the red runs hotter still.
HOTTER = {
    C.BLACK: C.BLOOD_DARK, C.NIGHT: C.BLOOD_DARK, C.DUSK: C.BLOOD, C.PLUM: C.CRIMSON,
    C.BLOOD_DARK: C.BLOOD, C.BLOOD: C.CRIMSON, C.CRIMSON: C.RED, C.RED: C.HELL, C.HELL: C.ORANGE,
    C.BONE_SHADE: C.CRIMSON, C.SILVER_DARK: C.RED, C.ORANGE: C.AMBER,
}

# The chains: (top x, bottom x, bottom y). The two over the middle hang still; the outer four sway.
STILL_CHAINS = ((190, 214, 172), (300, 270, 174))
SWAYING_CHAINS = ((20, 70, 200), (60, 110, 168), (430, 380, 190), (470, 446, 220))


def chain(img, x0, y0, x1, y1, sway):
    """A hanging chain of alternating links, from (x0, y0) down to (x1, y1)."""
    n = int(math.hypot(x1 - x0, y1 - y0) // 5)
    for k in range(n):
        t = k / max(1, n - 1)
        x = x0 + (x1 - x0) * t + math.sin(t * math.pi) * sway
        y = y0 + (y1 - y0) * t
        if k % 2 == 0:
            link = img.m_ellipse(x + 0.5, y + 0.5, 2.0, 3.2) & ~img.m_ellipse(x + 0.5, y + 0.5, 0.9, 2.0)
            img.paint(link, C.BONE_SHADE)
            img.put(int(x) - 1, int(y) - 2, C.SILVER_DARK)
        else:
            img.paint(img.m_rect(int(x) - 1, int(y) - 1, int(x) + 1, int(y) + 1), C.BLACK)
            img.put(int(x), int(y), C.BONE_SHADE)


def lucifer_base():
    img = Img(W, H, C.BLACK)
    # Darkness above, a red glow pooling down toward the steps.
    full = np.ones((H, W), bool)
    img.dither(full, C.BLACK, C.BLOOD_DARK, np.clip((img.ys - 40) / 140.0, 0, 1))
    img.dither(img.m_rect(0, 150, W, H), C.BLOOD_DARK, C.BLOOD, np.clip((img.ys - 150) / 120.0, 0, 0.85))

    # The steps: each wider and lower than the one above; the throne itself is somewhere far above the frame.
    for i, (top, half) in enumerate(((176, 150), (196, 190), (218, 230), (242, 260))):
        step = img.m_rect(240 - half, top, 240 + half, H)
        img.paint(step, C.BLOOD_DARK)
        img.paint(img.m_rect(240 - half, top, 240 + half, top + 1), C.CRIMSON)        # lit tread edge
        img.paint(img.m_rect(240 - half, top + 2, 240 + half, top + 2), C.BLOOD)
        for x in range(240 - half + 17 * (i % 2), 240 + half, 34):                   # joints between the blocks
            img.paint(img.m_rect(x, top + 3, x, top + 20), C.BLACK)
    # Two colossal feet of the throne, cut off by the top of the frame.
    for x0 in (96, 352):
        leg = img.m_rect(x0, 0, x0 + 32, 176)
        img.paint(leg, C.NIGHT)
        img.paint(img.m_rect(x0, 0, x0 + 2, 176), C.DUSK)
        img.paint(img.m_rect(x0 - 6, 166, x0 + 38, 176), C.BLOOD_DARK)
        img.paint(img.m_rect(x0 - 6, 166, x0 + 38, 166), C.CRIMSON)
        img.inner_outline(leg, C.BLACK)

    for x0, x1, y1 in STILL_CHAINS:
        chain(img, x0, -4, x1, y1, 0)

    readable_middle(img)
    return img


def chain_layer(name, x0, x1, y1):
    """A chain swaying a pixel either way at its middle."""
    left, right = min(x0, x1) - 5, max(x0, x1) + 5
    w, h = right - left + 1, y1 + 6

    def draw(img, f):
        chain(img, x0 - left, -4, x1 - left, y1, int(round(math.sin(phase(f, 8)))))
    return Layer(name, left, 0, w, h, 8, 8, draw, vignette=readable_middle)


def lucifer():
    layers = [chain_layer(f"chain{i}", x0, x1, y1) for i, (x0, x1, y1) in enumerate(SWAYING_CHAINS)]
    # Embers rise through the sides of the hall (never across the cards and the words).
    particles = [Particles("embers", *LEFT_HIGH, 8), Particles("embers", *LEFT_LOW, 10), Particles("embers", *RIGHT_SIDE, 18)]
    return lucifer_base(), layers, particles


SALONS = {"mammon": mammon, "belial": belial, "lilith": lilith, "lucifer": lucifer}
# Lucifer's hall has no soul variant (the soul never goes on his table) and its own, hotter final moments.
SALON_VARIANTS = {"lucifer": {"normal": None, "hell": lut(HOTTER)}}


def write_all(out_dir):
    written = []
    for dealer, build in SALONS.items():
        base, layers, particles = build()
        folder = os.path.join(out_dir, dealer)
        written += write(folder, base, layers, particles, SALON_VARIANTS.get(dealer, VARIANT_TABLES), text_zones=TABLE_TEXT)
    return written
