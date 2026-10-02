"""The title screen's backdrop: the very bottom of Hell, where the story ends.

480×270, 4 frames side by side (1920 px, within the importer's 2048 px limit), played at ~4 FPS.
- Above: a sky of smoke and dead stars, blotted out by two vast wings spreading from the top edge — and, between them, two
  burning eyes looking down. Lucifer himself is never in the picture.
- Below: a sea of fire on the horizon, the black ruins of a burning city on either side.
- Edges: Mammon's spilled gold (left), the Dead Man's Hand fanned on a slab (right), Lilith's crescent caught in the left
  wing's shadow, Belial's silver serpent coiled round a broken column.
Readability first: the middle column (x 70-410, y 25-260), where the logo, the text and the buttons sit, is pushed
down into the dark; every detail lives at the edges and the top.
Output: Assets/Resources/Art/Ui/menu.png
"""
import math
import os

import numpy as np

from pixel import C, Img, bezier, ribbon, sheet, shifted
from pixel_salons import DARKER, lut, sparkle

W, H = 480, 270
FRAMES = 4
HORIZON = 200


def sky(img):
    """Smoke-dark sky: black at the top, bleeding into blood red toward the burning horizon."""
    full = np.ones((H, W), bool)
    img.dither(full, C.BLACK, C.NIGHT, np.clip(img.ys / 120.0, 0, 1))
    img.dither(img.m_rect(0, 110, W, HORIZON), C.NIGHT, C.BLOOD_DARK, np.clip((img.ys - 110) / 80.0, 0, 1))
    img.dither(img.m_rect(0, 170, W, HORIZON), C.BLOOD_DARK, C.BLOOD, np.clip((img.ys - 170) / 40.0, 0, 0.8))
    rng = np.random.default_rng(5)
    for _ in range(50):   # dead stars, most of them soon hidden by the wings
        img.put(int(rng.integers(0, W)), int(rng.integers(0, 110)), C.MAUVE if rng.random() < 0.7 else C.LILAC_LIGHT)


def wings(img, frame):
    """Two vast membranes spreading from the top edge, ribbed, their hooked tips reaching down the sides."""
    sway = [0, 1, 1, 0][frame]
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


def eyes(img, frame):
    """His eyes, high above the logo: two slits of fire in the dark between the wings. They close once in a while."""
    closed = frame == 3
    for ex in (226, 254):
        if closed:
            img.paint(img.m_rect(ex - 4, 13, ex + 4, 13), C.BLOOD_DARK)
            continue
        halo = np.hypot(img.xs - ex - 0.5, img.ys - 13.5) / 9.0
        img.dither(halo < 1, C.BLACK, C.BLOOD_DARK, np.clip(0.5 * (1 - halo), 0, 1))
        almond = img.m_ellipse(ex + 0.5, 13.5, 5.2, 2.2)
        img.paint(almond, C.HELL)
        img.paint(img.m_ellipse(ex + 0.5, 13.5, 3.6, 1.3) & almond, C.AMBER if frame % 2 else C.ORANGE)
        img.paint(img.m_ellipse(ex + 0.5, 13.5, 2.0, 0.8) & almond, C.EMBER)
        img.put(ex, 12, C.BLACK)
        img.put(ex, 13, C.BLACK)
        img.put(ex, 14, C.BLACK)


def crescent(img):
    """Lilith's moon, caught low in the shadow of the left wing."""
    moon = img.m_ellipse(40, 120, 12, 12) & ~img.m_ellipse(46, 116, 11, 11)
    img.shade(moon, [C.MAUVE, C.LILAC, C.LILAC_LIGHT, C.WHITE], light=(-1, 1), shadow=1)


def city(img, frame):
    """Black ruins on the horizon at both sides, windows burning, smoke stacks leaning."""
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


def fire_sea(img, frame):
    """A sea of fire from the horizon down: rolling crests, burning at the edges, only embers glowing under the menu."""
    for y in range(HORIZON, H):
        depth = (y - HORIZON) / (H - HORIZON)
        for x in range(W):
            wave = math.sin(x * 0.09 + y * 0.35 + frame * 1.57) + 0.6 * math.sin(x * 0.23 - frame * 1.57 + y * 0.1)
            # Calm under the buttons (the middle), wild toward the sides.
            edge = min(1.0, max(0.0, (abs(x - 240) - 120) / 60.0))
            heat = (wave * 0.5 + 0.5) * (0.45 + 0.55 * edge)
            if heat > 0.85:
                color = C.AMBER if depth < 0.4 else C.ORANGE
            elif heat > 0.6:
                color = C.HELL
            elif heat > 0.35:
                color = C.RED
            else:
                color = C.CRIMSON if depth < 0.5 else C.BLOOD
            img.put(x, y, color)
    img.paint(img.m_rect(0, HORIZON, W, HORIZON), C.EMBER)


def gold(img, frame):
    """Mammon's spilled treasure at the bottom left: a heap of coins, a tipped chest, a coin or two catching the light."""
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
    for i, (x, y) in enumerate(((20, 254), (52, 252), (8, 262), (66, 262))):
        if i % FRAMES == frame:
            sparkle(img, x, y, size=1)


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


def dead_mans_hand(img, frame):
    """A♠ A♣ 8♠ 8♣ fanned on a stone slab at the bottom right — the one hand that walks out of here."""
    slab = img.m_rect(404, 236, 476, 262)
    img.paint(slab, C.DUSK)
    img.paint(img.m_rect(404, 236, 476, 237), C.PLUM)
    img.inner_outline(slab, C.BLACK)
    for i, (rank, suit) in enumerate(((GLYPH_A, SPADE), (GLYPH_A, CLUB), (GLYPH_8, SPADE), (GLYPH_8, CLUB))):
        card(img, 410 + i * 15, 230 - (1 if i in (1, 2) else 0), rank, suit, C.BLACK)
    if frame == 1:
        sparkle(img, 412, 230, size=1)


def serpent_column(img, frame):
    """A broken column at the right with Belial's silver serpent wound round it."""
    x = 448
    column = img.m_rect(x - 7, 104, x + 7, HORIZON)
    img.paint(column, C.BONE_SHADE)
    img.paint(img.m_rect(x - 7, 104, x - 6, HORIZON), C.BONE_DARK)
    img.paint(img.m_poly([(x - 9, 104), (x - 4, 96), (x + 2, 102), (x + 8, 94), (x + 9, 104)]), C.BONE_SHADE)
    img.inner_outline(column, C.BLACK)
    spine = [(x + 8 * math.sin(y * 0.16 + frame * 0.8), y) for y in range(108, HORIZON, 3)]
    img.paint(img.m_poly(ribbon(spine, 4, 3)), C.SILVER_DARK)
    hx, hy = spine[0]
    img.paint(img.m_ellipse(hx + 2, hy - 2, 3, 2), C.SILVER)
    img.put(int(hx) + 3, int(hy) - 3, C.HELL)


def embers(img, frame):
    rng = np.random.default_rng(66)
    for _ in range(40):
        x = int(rng.integers(0, W))
        y = (int(rng.integers(30, HORIZON)) - frame * int(rng.integers(4, 9))) % HORIZON
        img.put(x, y, [C.AMBER, C.ORANGE, C.HELL][int(rng.integers(0, 3))])


def quiet_middle(img):
    """
    Two steps darker over the middle column (x 70-410, y 25-260), fading out over ~24 px, so the logo, the text and the
    buttons always stand clear. The eyes above the logo stay lit.
    """
    darker = lut(DARKER)
    once = darker[img.px]
    twice = darker[once]
    threshold = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]], np.float32)[
        np.arange(img.h)[:, None] % 4, np.arange(img.w)[None, :] % 4] / 16.0

    def field(x0, y0, x1, y1, fade):
        dx = np.maximum(np.maximum(x0 - img.xs, img.xs - x1), 0)
        dy = np.maximum(np.maximum(y0 - img.ys, img.ys - y1), 0)
        return np.clip(1 - np.hypot(dx, dy) / fade, 0, 1)

    middle = field(94, 30, 386, 256, 24)
    out = np.where(middle > threshold, once, img.px)
    out = np.where(middle * 0.8 > threshold + 0.05, twice, out)
    img.px = out


def menu(frame):
    img = Img(W, H, C.BLACK)
    sky(img)
    crescent(img)
    fire_sea(img, frame)
    city(img, frame)
    embers(img, frame)
    wings(img, frame)
    serpent_column(img, frame)
    gold(img, frame)
    dead_mans_hand(img, frame)
    quiet_middle(img)
    eyes(img, frame)   # above the logo, never darkened
    return img


def write_all(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, "menu.png")
    sheet([menu(f) for f in range(FRAMES)]).save(path)
    return [path]
