"""Pixel UI art: stone backdrop, old-RPG boxes and buttons (9-slice), 32×48 cards, suits, digits, title, coin, flames."""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

from pixel import C, Img, RAMP_FIRE, RAMP_GOLD, bezier, sheet, shifted

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


def divider(w=48):
    img = Img(w, 3)
    img.paint(img.m_rect(2, 1, w - 3, 1), C.GOLD_MID)
    img.put(w // 2, 0, C.GOLD)
    img.put(w // 2 - 1, 1, C.GOLD_LIGHT)
    img.put(w // 2, 1, C.RED)
    img.put(w // 2 + 1, 1, C.GOLD_LIGHT)
    img.put(w // 2, 2, C.GOLD)
    return img


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
