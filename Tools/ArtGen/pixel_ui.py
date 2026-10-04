"""Pixel UI art: stone backdrop, old-RPG boxes and buttons (9-slice), 32×48 cards, suits, digits, title, coin, flames."""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

from pixel import C, Img, RAMP_FIRE, RAMP_GOLD, bezier, ribbon, sheet, shifted

SCREEN_W, SCREEN_H = 480, 270


# ------------------------------------------------------------------ backdrop

def background():
    """Dark, quiet stone wall: irregular blocks, black mortar, a few lit edges, darkening toward the corners."""
    img = Img(SCREEN_W, SCREEN_H, C.NIGHT)
    rng = np.random.default_rng(13)
    row_h = 18
    for row in range(SCREEN_H // row_h + 1):
        y0 = row * row_h
        x = -int(rng.integers(0, 30))
        while x < SCREEN_W:
            w = int(rng.integers(26, 46))
            block = img.m_rect(x + 1, y0 + 1, x + w - 1, y0 + row_h - 1)
            tone = C.NIGHT if rng.random() < 0.7 else C.DUSK
            img.paint(block, tone)
            # lit top-left edges, dark bottom-right edges
            img.paint(img.m_rect(x + 1, y0 + 1, x + w - 2, y0 + 1) & block, C.DUSK if tone == C.NIGHT else C.PLUM)
            img.paint(img.m_rect(x + w - 1, y0 + 1, x + w - 1, y0 + row_h - 1) & block, C.BLACK)
            # a few cracks and pits
            if rng.random() < 0.35:
                cx, cy = x + int(rng.integers(4, max(5, w - 4))), y0 + int(rng.integers(4, row_h - 4))
                for k in range(int(rng.integers(2, 5))):
                    img.put(cx + k, cy + (k % 2), C.BLACK)
            x += w
        img.paint(img.m_rect(0, y0, SCREEN_W, y0), C.BLACK)
    # Vignette: dither toward black near the edges so the middle stays calm.
    full = np.ones((img.h, img.w), bool)
    dx = (img.xs - SCREEN_W / 2) / (SCREEN_W / 2)
    dy = (img.ys - SCREEN_H / 2) / (SCREEN_H / 2)
    edge = np.clip(np.hypot(dx * 0.9, dy) - 0.75, 0, 1) * 2.2
    darken = img.px.copy()
    img.dither(full, 0, C.BLACK, edge)
    img.px = np.where(img.px == 0, darken, img.px)
    return img


# ------------------------------------------------------------------ boxes (9-slice sources, border 4)

def box(fill, border, inner, highlight=None, size=12):
    """Old RPG menu box: black outer line, a coloured frame line, a darker inner line, flat fill."""
    img = Img(size, size, fill)
    full = img.m_rect(0, 0, size - 1, size - 1)
    img.inner_outline(full, C.BLACK)
    ring1 = img.m_rect(1, 1, size - 2, size - 2)
    img.inner_outline(ring1, border)
    ring2 = img.m_rect(2, 2, size - 3, size - 3)
    img.inner_outline(ring2, inner)
    # rounded outer corners
    for x, y in ((0, 0), (size - 1, 0), (0, size - 1), (size - 1, size - 1)):
        img.put(x, y, C.CLEAR)
    for x, y in ((1, 1), (size - 2, 1), (1, size - 2), (size - 2, size - 2)):
        img.put(x, y, C.BLACK)
    if highlight is not None:
        img.put(2, 1, highlight)
        img.put(1, 2, highlight)
    return img


def button(fill, light, dark, size=12):
    """Raised button: black outline, light top edge, dark bottom edge, flat face."""
    img = Img(size, size, fill)
    full = img.m_rect(0, 0, size - 1, size - 1)
    img.inner_outline(full, C.BLACK)
    img.paint(img.m_rect(1, 1, size - 2, 1), light)
    img.paint(img.m_rect(1, 1, 1, size - 3), light)
    img.paint(img.m_rect(1, size - 3, size - 2, size - 2), dark)
    img.paint(img.m_rect(size - 2, 2, size - 2, size - 2), dark)
    for x, y in ((0, 0), (size - 1, 0), (0, size - 1), (size - 1, size - 1)):
        img.put(x, y, C.CLEAR)
    return img


# ------------------------------------------------------------------ cards (32×48)

CARD_W, CARD_H = 32, 48


def card_shape(img):
    m = img.m_rect(0, 0, CARD_W - 1, CARD_H - 1)
    for x, y in ((0, 0), (CARD_W - 1, 0), (0, CARD_H - 1), (CARD_W - 1, CARD_H - 1)):
        m[y, x] = False
    return m


def card_face():
    img = Img(CARD_W, CARD_H)
    m = card_shape(img)
    img.paint(m, C.BONE)
    img.inner_outline(m, C.BLACK)
    img.paint(img.m_rect(1, CARD_H - 2, CARD_W - 2, CARD_H - 2), C.BONE_MID)
    img.paint(img.m_rect(CARD_W - 2, 1, CARD_W - 2, CARD_H - 2), C.BONE_MID)
    return img


def card_back():
    img = Img(CARD_W, CARD_H)
    m = card_shape(img)
    img.paint(m, C.BLOOD)
    inner = img.m_rect(3, 3, CARD_W - 4, CARD_H - 4)
    # diamond lattice
    lattice = ((img.xs.astype(int) + img.ys.astype(int)) % 6 == 0) | ((img.xs.astype(int) - img.ys.astype(int)) % 6 == 0)
    img.paint(inner, C.BLOOD_DARK)
    img.paint(inner & lattice, C.CRIMSON)
    img.inner_outline(img.m_rect(2, 2, CARD_W - 3, CARD_H - 3), C.GOLD_MID)
    # pentagram medallion
    cx, cy, r = CARD_W / 2, CARD_H / 2, 9
    disc = img.m_ellipse(cx, cy, r + 1.5, r + 1.5)
    img.paint(disc, C.BLACK)
    ring = img.m_ellipse(cx, cy, r + 0.5, r + 0.5) & ~img.m_ellipse(cx, cy, r - 0.6, r - 0.6)
    img.paint(ring, C.GOLD)
    star = [(cx + (r - 0.5) * math.sin(math.radians(144 * k)), cy - (r - 0.5) * math.cos(math.radians(144 * k))) for k in range(6)]
    img.paint(img.m_line(star, 1) & disc, C.GOLD)
    img.inner_outline(m, C.BLACK)
    return img


def card_slot():
    img = Img(CARD_W, CARD_H)
    m = card_shape(img)
    edge = m & ~img.m_rect(1, 1, CARD_W - 2, CARD_H - 2)
    dashes = ((img.xs.astype(int) + img.ys.astype(int)) // 2) % 2 == 0
    img.paint(edge & dashes, C.PLUM)
    return img


# Suits: 5×5 for card corners, 11×11 for the card centre. 'k' = ink, 'h' = highlight.
SUITS_SMALL = {
    "spades": ["..k..", ".kkk.", "kkkkk", "..k..", ".kkk."],
    "clubs": [".kkk.", ".kkk.", "kkkkk", "k.k.k", "..k.."],
    "hearts": ["kk.kk", "kkkkk", "kkkkk", ".kkk.", "..k.."],
    "diamonds": ["..k..", ".kkk.", "kkkkk", ".kkk.", "..k.."],
}
SUITS_BIG = {
    "spades": [".....k.....", "....kkk....", "...kkkkk...", "..kkhkkkk..", ".kkhkkkkkk.", "kkkkkkkkkkk",
               "kkkkkkkkkkk", ".kkk.k.kkk.", ".....k.....", "....kkk....", "...kkkkk..."],
    "clubs": ["....kkk....", "...khkkk...", "...kkkkk...", "....kkk....", ".kkk.k.kkk.", "khkkkkkkkkk",
              "kkkkkkkkkkk", ".kkk.k.kkk.", ".....k.....", "....kkk....", "...kkkkk..."],
    "hearts": [".kkk...kkk.", "khkkk.kkkkk", "khkkkkkkkkk", "kkkkkkkkkkk", "kkkkkkkkkkk", ".kkkkkkkkk.",
               "..kkkkkkk..", "...kkkkk...", "....kkk....", ".....k.....", "..........."],
    "diamonds": [".....k.....", "....kkk....", "...khkkk...", "..khkkkkk..", ".kkkkkkkkk.", "kkkkkkkkkkk",
                 ".kkkkkkkkk.", "..kkkkkkk..", "...kkkkk...", "....kkk....", ".....k....."],
}
SUIT_INK = {"spades": (C.BLACK, C.DUSK), "clubs": (C.BLACK, C.DUSK), "hearts": (C.CRIMSON, C.HELL), "diamonds": (C.CRIMSON, C.HELL)}


def suit(name, big):
    rows = SUITS_BIG[name] if big else SUITS_SMALL[name]
    ink, light = SUIT_INK[name]
    img = Img(len(rows[0]), len(rows))
    img.rows(0, 0, rows, {"k": ink, "h": light})
    return img


# ------------------------------------------------------------------ digits, title, coin, flames, divider

DIGITS_5X7 = {
    "0": [".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###."],
    "1": ["..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###."],
    "2": [".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####"],
    "3": [".###.", "#...#", "....#", "..##.", "....#", "#...#", ".###."],
    "4": ["...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#."],
    "5": ["#####", "#....", "####.", "....#", "....#", "#...#", ".###."],
    "6": ["..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###."],
    "7": ["#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..."],
    "8": [".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###."],
    "9": [".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.."],
}
DIGIT_W, DIGIT_H = 12, 16   # 5×7 scaled ×2, plus a 1 px outline all round


def digits():
    frames = []
    bands = [C.GOLD_LIGHT, C.GOLD_LIGHT, C.GOLD, C.GOLD, C.GOLD, C.GOLD_MID, C.GOLD_MID]
    for d in "0123456789":
        img = Img(DIGIT_W, DIGIT_H)
        for r, row in enumerate(DIGITS_5X7[d]):
            for c, ch in enumerate(row):
                if ch == "#":
                    color = bands[r]
                    for yy in (0, 1):
                        for xx in (0, 1):
                            img.put(1 + c * 2 + xx, 1 + r * 2 + yy, color if yy == 0 or r < 6 else C.GOLD_DARK)
        img.outline(C.BLACK)
        frames.append(img)
    return sheet(frames)


def title(font_path, text="HELL POKER", size=16):
    """The logo from the pixel title font: fire gradient letters, black outline, blood drop shadow."""
    font = ImageFont.truetype(font_path, size)
    probe = Image.new("L", (400, 40), 0)
    draw = ImageDraw.Draw(probe)
    draw.fontmode = "1"
    draw.text((2, 2), text, font=font, fill=255)
    box_ = probe.getbbox()
    glyphs = np.asarray(probe.crop(box_), np.uint8) > 0
    gh, gw = glyphs.shape
    img = Img(gw + 4, gh + 4)
    mask = np.zeros((img.h, img.w), bool)
    mask[1:1 + gh, 1:1 + gw] = glyphs
    bands = [C.EMBER, C.AMBER, C.ORANGE, C.HELL, C.RED]
    level = np.clip(((img.ys - 1) / max(1, gh) * len(bands)).astype(int), 0, len(bands) - 1)
    img.px[mask] = np.array(bands, np.uint8)[level][mask]
    shadow = shifted(mask, -1, -1) & ~mask
    img.paint(shadow, C.BLOOD_DARK)
    img.outline(C.BLACK)
    return img


def coin_chip(size=16):
    img = Img(size, size)
    m = img.m_ellipse(size / 2, size / 2, size / 2 - 0.5, size / 2 - 0.5)
    img.shade(m, RAMP_GOLD, shadow=2)
    inner = img.m_ellipse(size / 2, size / 2, size / 2 - 3, size / 2 - 3)
    img.inner_outline(inner, C.GOLD_MID)
    img.inner_outline(m, C.BLACK)
    return img


def flames(frames=4, w=32, h=20):
    """Tileable strip of flames for the screen edge, animated."""
    out = []
    for f in range(frames):
        img = Img(w, h)
        for x in range(w):
            hh = 9 + 4 * math.sin((x / w) * 2 * math.pi * 2 + f * math.pi / 2) + 3 * math.sin((x / w) * 2 * math.pi * 3 - f * math.pi / 2)
            top = h - int(hh)
            for y in range(max(0, top), h):
                depth = (y - top) / max(1.0, hh)
                idx = 0 if depth < 0.15 else 1 if depth < 0.35 else 2 if depth < 0.55 else 3 if depth < 0.75 else 4
                img.put(x, y, RAMP_FIRE[idx])
        out.append(img)
    return sheet(out)


def soul_lamp(frames=4, w=16, h=24):
    """A glass phial holding the soul: a pale, ghostly flame that flickers."""
    out = []
    for f in range(frames):
        img = Img(w, h)
        glass = img.m_ellipse(8, 15.5, 6.5, 7.5) | img.m_rect(5, 3, 10, 9)
        img.paint(glass, C.NIGHT)
        img.inner_outline(glass, C.SILVER_DARK)
        img.put(4, 12, C.SILVER)
        img.put(4, 13, C.SILVER)
        cork = img.m_rect(5, 0, 10, 2)
        img.paint(cork, C.BONE_SHADE)
        img.inner_outline(cork, C.BLACK)
        # Flame: a teardrop that sways and breathes.
        sway = [0, 1, 0, -1][f]
        tall = [0, 1, 2, 1][f]
        flame = img.m_ellipse(8 + sway * 0.5, 17, 3.2, 3.6) | img.m_poly([(5 + sway, 17), (8 + sway, 9 - tall), (11 + sway, 17)])
        img.paint(flame & glass, C.LILAC)
        core = img.m_ellipse(8 + sway * 0.5, 18, 1.6, 2.2) | img.m_poly([(7 + sway, 18), (8 + sway, 13 - tall), (9 + sway, 18)])
        img.paint(core & glass, C.WHITE)
        img.paint(flame & ~core & glass & img.m_rect(0, 18, w, h), C.SILVER)
        out.append(img)
    return sheet(out)


def divider(w=48):
    img = Img(w, 3)
    img.paint(img.m_rect(2, 1, w - 3, 1), C.GOLD_MID)
    img.put(w // 2, 0, C.GOLD)
    img.put(w // 2 - 1, 1, C.GOLD_LIGHT)
    img.put(w // 2, 1, C.RED)
    img.put(w // 2 + 1, 1, C.GOLD_LIGHT)
    img.put(w // 2, 2, C.GOLD)
    return img


# ------------------------------------------------------------------ the demons' cheats: icons, malice pips, card marks

# Order matters: the game reads these strips by position (UiArt.CheatIconIds, MalicePipOrder, CardMarkOrder).
CHEAT_ICON_IDS = ["collateral", "tithe", "buyout", "false_face", "forked_tongue", "serpent_swap", "night_veil", "thorn",
                  "moonless", "gaze", "rewrite", "burning_card", "the_fall"]
ICON = 16


def cheat_icon(cheat):
    """A 16×16 sign for each cheat, outlined in black so it reads over cards and portraits."""
    img = Img(ICON, ICON)
    if cheat == "collateral":            # a gold chain link over a padlock
        for x0 in (2, 8):
            ring = img.m_ellipse(x0 + 3, 6, 3.4, 2.6) & ~img.m_ellipse(x0 + 3, 6, 1.6, 1.0)
            img.paint(ring, C.GOLD)
        lock = img.m_rect(5, 9, 11, 14)
        img.paint(lock, C.GOLD_MID)
        img.put(8, 11, C.BLACK)
        img.put(8, 12, C.BLACK)
    elif cheat == "tithe":               # three coins, one in flight
        for cx, cy in ((5, 11), (11, 11), (8, 4)):
            coin = img.m_ellipse(cx, cy, 3.2, 3.2)
            img.shade(coin, RAMP_GOLD, shadow=1)
    elif cheat == "buyout":              # two arrows trading places, a coin between
        img.paint(img.m_line([(2, 5), (12, 5)], 1), C.GOLD_LIGHT)
        img.paint(img.m_poly([(11, 2), (15, 5), (11, 8)]), C.GOLD_LIGHT)
        img.paint(img.m_line([(4, 11), (14, 11)], 1), C.GOLD)
        img.paint(img.m_poly([(5, 8), (1, 11), (5, 14)]), C.GOLD)
    elif cheat == "false_face":          # a silver mask
        mask = img.m_ellipse(8, 8, 6, 5)
        img.shade(mask, [C.SILVER_DARK, C.SILVER, C.WHITE], shadow=1)
        for ex in (5, 10):
            img.paint(img.m_rect(ex, 6, ex + 1, 7), C.BLACK)
        img.paint(img.m_line([(5, 11), (8, 12), (11, 11)], 1), C.BLACK)
    elif cheat == "forked_tongue":       # a forked silver tongue
        img.paint(img.m_line([(8, 1), (8, 9)], 2), C.SILVER)
        img.paint(img.m_line([(8, 9), (4, 14)], 1), C.SILVER)
        img.paint(img.m_line([(8, 9), (12, 14)], 1), C.SILVER)
        img.put(8, 1, C.RED)
    elif cheat == "serpent_swap":        # a serpent biting its tail, an S
        body = img.m_poly(ribbon(bezier([(3, 3), (13, 3), (3, 13), (13, 13)], 16), 3, 2))
        img.paint(body, C.SILVER_DARK)
        img.put(3, 3, C.HELL)
    elif cheat == "night_veil":          # a crescent behind a dark veil
        moon = img.m_ellipse(8, 8, 6, 6) & ~img.m_ellipse(10, 6, 5, 5)
        img.paint(moon, C.LILAC_LIGHT)
        img.dither(img.m_rect(0, 8, 15, 15), C.CLEAR, C.PLUM, 0.6)
    elif cheat == "thorn":               # a thorn with a drop of blood
        img.paint(img.m_poly([(3, 14), (12, 2), (8, 14)]), C.BONE_SHADE)
        img.paint(img.m_line([(4, 13), (11, 3)], 1), C.BONE_DARK)
        img.put(12, 2, C.WHITE)
        img.paint(img.m_ellipse(12, 12, 1.6, 2.0), C.RED)
    elif cheat == "moonless":            # a black moon with a pale rim
        moon = img.m_ellipse(8, 8, 6, 6)
        img.paint(moon, C.BLACK)
        img.inner_outline(moon, C.LILAC)
    elif cheat == "gaze":                # one burning eye
        eye = img.m_ellipse(8, 8, 7, 3.5)
        img.paint(eye, C.HELL)
        img.paint(img.m_ellipse(8, 8, 4, 2.2) & eye, C.AMBER)
        img.paint(img.m_rect(8, 5, 8, 11) & eye, C.BLACK)
    elif cheat == "rewrite":             # a quill crossing out a line
        img.paint(img.m_poly(ribbon([(13, 1), (8, 7), (3, 14)], 3, 1)), C.BONE)
        img.paint(img.m_line([(1, 9), (14, 9)], 1), C.RED)
    elif cheat == "burning_card":        # a small card on fire
        card_ = img.m_rect(4, 6, 11, 15)
        img.paint(card_, C.BONE)
        flame = img.m_poly([(3, 9), (6, 1), (8, 5), (10, 0), (13, 9)])
        img.paint(flame, C.ORANGE)
        img.paint(img.m_poly([(6, 9), (8, 4), (10, 9)]), C.AMBER)
    elif cheat == "the_fall":            # a falling star
        img.paint(img.m_line([(1, 1), (9, 9)], 1), C.ORANGE)
        star = img.m_poly([(11, 6), (12, 9), (15, 10), (12, 11), (11, 14), (10, 11), (7, 10), (10, 9)])
        img.paint(star, C.EMBER)
        img.put(11, 10, C.WHITE)
    img.outline(C.BLACK)
    return img


def cheat_icons():
    return sheet([cheat_icon(c) for c in CHEAT_ICON_IDS])


MALICE_ORDER = ["mammon", "belial", "lilith", "lucifer"]


def malice_pip(dealer, full):
    """An 8×8 pip of a demon's malice gauge: a coin, a serpent scale, a thorn, an ember — dark when empty."""
    img = Img(8, 8)
    if dealer == "mammon":
        coin = img.m_ellipse(4, 4, 3.4, 3.4)
        img.shade(coin, RAMP_GOLD if full else [C.NIGHT, C.DUSK, C.PLUM, C.PLUM], shadow=1)
    elif dealer == "belial":
        scale = img.m_poly([(1, 2), (7, 2), (4, 7)])
        img.paint(scale, C.SILVER if full else C.DUSK)
        img.inner_outline(scale, C.SILVER_DARK if full else C.NIGHT)
    elif dealer == "lilith":
        thorn = img.m_poly([(1, 7), (4, 0), (7, 7)])
        img.paint(thorn, C.MAUVE if full else C.DUSK)
        if full:
            img.put(4, 2, C.LILAC_LIGHT)
    else:
        ember = img.m_ellipse(4, 4, 3, 3)
        img.paint(ember, C.ORANGE if full else C.BLOOD_DARK)
        if full:
            img.paint(img.m_ellipse(4, 4, 1.4, 1.4), C.EMBER)
    img.outline(C.BLACK)
    return img


def malice_pips():
    return sheet([malice_pip(d, f) for d in MALICE_ORDER for f in (False, True)])


MARK_ORDER = ["chained", "thorned", "veiled", "false_face", "protected"]


def card_mark(mark):
    """A 32×48 overlay laid over a card that a cheat has marked."""
    img = Img(CARD_W, CARD_H)
    if mark == "chained":                # a gold chain across the card, a padlock in the middle
        for k in range(0, 40, 6):
            ring = img.m_ellipse(2 + k * 0.7, 6 + k, 3.0, 2.2) & ~img.m_ellipse(2 + k * 0.7, 6 + k, 1.4, 0.9)
            img.paint(ring, C.GOLD if k % 12 == 0 else C.GOLD_MID)
        lock = img.m_rect(12, 22, 20, 30)
        img.paint(lock, C.GOLD)
        img.inner_outline(lock, C.GOLD_DARK)
        img.put(16, 25, C.BLACK)
        img.put(16, 26, C.BLACK)
        img.paint(img.m_ellipse(16, 21, 3, 3) & ~img.m_ellipse(16, 21, 1.6, 1.6) & img.m_rect(0, 0, 31, 21), C.GOLD_MID)
    elif mark == "thorned":              # thick blood-red vines up both edges, big pale thorns, drops of blood
        # Clear of the rank in the top left corner and the suit in the middle; readable at a glance (it costs years).
        for side in (0, 1):
            pts = [(2, 47), (9, 36), (1, 22), (6, 10)]
            if side:
                pts = [(CARD_W - 1 - x, y) for x, y in pts]
            vine = bezier(pts, 28)
            body = img.m_line(vine, 3)
            img.paint(body, C.RED)
            img.inner_outline(body, C.BLOOD_DARK)
            for k in range(2, 28, 4):
                x, y = int(vine[k][0]), int(vine[k][1])
                d = -1 if side else 1                         # thorns point into the card
                for px, py, col in ((2, 0, C.BONE_DARK), (2, -1, C.BONE_DARK), (3, -1, C.BONE), (3, -2, C.BONE), (4, -2, C.WHITE)):
                    img.put(x + px * d, y + py, col)
            drop_x = CARD_W - 6 if side else 5
            img.paint(img.m_ellipse(drop_x, 38 if side else 30, 1.6, 2.4), C.RED)
            img.put(drop_x, 37 if side else 29, C.HELL)
    elif mark == "veiled":               # darkness over the card, a thin crescent
        full = np.ones((img.h, img.w), bool)
        img.dither(full, C.CLEAR, C.BLACK, 0.5)
        moon = img.m_ellipse(16, 24, 7, 7) & ~img.m_ellipse(19, 21, 6, 6)
        img.paint(moon, C.LILAC)
    elif mark == "protected":           # the King's protection: a small gold crown in the top right, a gold frame
        frame = img.m_rect(0, 0, CARD_W - 1, CARD_H - 1) & ~img.m_rect(1, 1, CARD_W - 2, CARD_H - 2)
        img.paint(frame, C.GOLD)
        img.paint(img.m_rect(20, 6, 29, 9), C.GOLD)
        for px in (20, 24, 28):
            img.paint(img.m_poly([(px - 0.5, 6), (px + 0.5, 2), (px + 1.5, 6)]), C.GOLD_LIGHT)
        img.paint(img.m_rect(20, 9, 29, 9), C.GOLD_MID)
        img.put(24, 7, C.RED)
        img.outline(C.BLACK, mask=img.m_rect(19, 1, 30, 10) & (img.px != C.CLEAR))
    elif mark == "false_face":           # the faintest silver sheen: a broken silver frame, two glints
        frame = img.m_rect(0, 0, CARD_W - 1, CARD_H - 1) & ~img.m_rect(1, 1, CARD_W - 2, CARD_H - 2)
        dashes = ((img.xs.astype(int) + img.ys.astype(int)) // 3) % 3 == 0
        img.paint(frame & dashes, C.SILVER)
        img.put(4, 4, C.WHITE)
        img.put(27, 43, C.WHITE)
    return img


def card_marks():
    return sheet([card_mark(m) for m in MARK_ORDER])


def fade(steps=4):
    """A slow fall into darkness without any transparency blending: black laid over the screen in growing Bayer patterns
    (¼, ½, ¾, all), one full-screen frame per step."""
    out = []
    for k in range(1, steps + 1):
        img = Img(SCREEN_W, SCREEN_H)
        full = np.ones((img.h, img.w), bool)
        img.dither(full, C.CLEAR, C.BLACK, k / steps - 0.01 if k < steps else 1.0)
        out.append(img)
    return sheet(out)


def write_all(out_dir, fonts_dir):
    os.makedirs(out_dir, exist_ok=True)
    stone = background()
    hell = Img(stone.w, stone.h)
    hell.px = stone.px.copy()
    hell.recolor({C.NIGHT: C.BLOOD_DARK, C.DUSK: C.BLOOD, C.PLUM: C.CRIMSON})
    jobs = {
        "background": stone,
        "background_hell": hell,
        "panel": box(C.NIGHT, C.GOLD_MID, C.DUSK, highlight=C.GOLD),
        "panel_hot": box(C.NIGHT, C.ORANGE, C.BLOOD, highlight=C.EMBER),
        "dialog": box(C.DUSK, C.BONE, C.NIGHT, highlight=C.WHITE),
        "dialog_lucifer": box(C.BLACK, C.HELL, C.BLOOD_DARK, highlight=C.EMBER),
        "fade": fade(),
        "cheat_icons": cheat_icons(),
        "malice_pips": malice_pips(),
        "card_marks": card_marks(),
        "button_blood": button(C.CRIMSON, C.RED, C.BLOOD),
        "button_ember": button(C.HELL, C.ORANGE, C.RED),
        "button_ash": button(C.PLUM, C.VIOLET, C.DUSK),
        "card_face": card_face(),
        "card_back": card_back(),
        "card_slot": card_slot(),
        "digits": digits(),
        "title": title(os.path.join(fonts_dir, "PressStart2P-Regular.ttf")),
        "coin": coin_chip(),
        "flames": flames(),
        "soul_lamp": soul_lamp(),
        "divider": divider(),
    }
    for name in SUITS_SMALL:
        jobs["suit_" + name] = suit(name, big=True)
        jobs["suit_" + name + "_small"] = suit(name, big=False)

    written = []
    for name, img in jobs.items():
        path = os.path.join(out_dir, name + ".png")
        img.save(path)
        written.append(path)
    return written
