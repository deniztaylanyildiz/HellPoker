"""Animated 96×96 pixel portraits of the demon dealers.

Each demon is drawn from a pose (breathing offset, eyes, mouth, brows, shake, fire...). Every animation state is a list of
poses; a state's frames are laid side by side into Assets/Resources/Art/Demons/<id>/<state>.png.
"""
import math

import numpy as np

from pixel import (C, Img, RAMP_BONE, RAMP_FIRE, RAMP_GOLD, RAMP_GREEN, RAMP_LILAC, RAMP_NIGHT, RAMP_RED, RAMP_SILVER,
                   bezier, ribbon, sheet, shifted)

SIZE = 96
CX = 48


# ------------------------------------------------------------------ poses per animation state

def poses(state):
    """The pose of every frame of a state. Keys: dy (breath), hdy (head), dx (shake), eyes, mouth, brows, flash, fire, sparkle, t."""
    if state == "idle":
        return [dict(dy=d, eyes="blink" if i == 4 else "open", t=i) for i, d in enumerate([0, 0, 0, 1, 1, 1])]
    if state == "talk":
        return [dict(mouth=m, t=i) for i, m in enumerate(["talk1", "talk2", "talk1", "rest"])]
    if state == "gloat":
        return [dict(hdy=h, eyes="squint", mouth="laugh" if i % 2 == 0 else "grin", t=i)
                for i, h in enumerate([0, -1, 0, -1, 0, 0])]
    if state == "angry":
        return [dict(dx=d, eyes="angry", brows="angry", mouth="snarl", flash=i in (0, 2), steam=i, t=i)
                for i, d in enumerate([1, -1, 1, -1, 0])]
    if state == "reraise":
        return [dict(eyes="glow", brows="sly", mouth="sly", sparkle=i in (2, 3), glow=min(i, 3), t=i) for i in range(5)]
    if state == "final":
        return [dict(dy=d, eyes="glow", fire=i, glow=2, t=i) for i, d in enumerate([0, 1, 0, 1])]
    if state == "soul":
        # The soul on the table: the same intensity as the final stretch, but cold — ghost fire, pale glowing eyes.
        return [dict(dy=d, eyes="glow", fire=i, glow=3 if i % 2 else 2, cold=True, t=i) for i, d in enumerate([0, 0, 1, 1])]
    raise ValueError(state)


STATES = ["idle", "talk", "gloat", "angry", "reraise", "final", "soul"]

RAMP_COLD = [C.DUSK, C.VIOLET, C.LILAC, C.SILVER, C.WHITE]
COLD_EYES = {C.EMBER: C.WHITE, C.AMBER: C.LILAC_LIGHT, C.ORANGE: C.LILAC, C.HELL: C.MAUVE}


def defaults(p):
    base = dict(dy=0, hdy=0, dx=0, eyes="open", mouth="rest", brows="calm", flash=False, fire=None, sparkle=False,
                glow=0, steam=None, cold=False, t=0)
    base.update(p)
    return base


# ------------------------------------------------------------------ shared pieces

def backdrop(img, accent, stars=None):
    """Quiet dark backdrop: night in the middle fading to black, a faint dither of the demon's colour behind the head."""
    full = np.ones((img.h, img.w), bool)
    dist = np.hypot(img.xs - CX, img.ys - 40) / 60.0
    img.dither(full, C.NIGHT, C.BLACK, np.clip(dist * 1.3 - 0.35, 0, 1))
    inner = dist < 0.5
    img.dither(inner, C.NIGHT, accent, np.clip(0.3 - dist * 0.5, 0, 0.25))
    if stars:
        rng = np.random.default_rng(stars)
        for _ in range(12):
            img.put(int(rng.integers(2, img.w - 2)), int(rng.integers(2, 50)), C.LILAC_LIGHT)


def fire_back(img, phase, ramp=RAMP_FIRE):
    """Tall flames licking up behind the demon (final stretch; cold ghost fire when the soul is on the table)."""
    for x in range(img.w):
        h = 40 + 12 * math.sin(x * 0.31 + phase * 1.6) + 7 * math.sin(x * 0.83 - phase * 2.4) + 4 * math.sin(x * 2.1 + phase)
        top = img.h - int(h)
        for y in range(max(0, top), img.h):
            depth = (y - top) / max(1.0, h)
            idx = 0 if depth < 0.12 else 1 if depth < 0.3 else 2 if depth < 0.5 else 3 if depth < 0.7 else 4
            img.put(x, y, ramp[idx])


def finish(fig, p, accent, stars=None, flash_map=None):
    """Outlines the figure, applies the angry flash and lays it over the backdrop (and the fire)."""
    fig.outline(C.BLACK)
    if p["flash"]:
        if flash_map:
            fig.recolor(flash_map)
        else:
            red_flash(fig)
    if p["cold"]:
        fig.recolor(COLD_EYES)
    img = Img(SIZE, SIZE)
    backdrop(img, accent, stars)
    if p["fire"] is not None:
        fire_back(img, p["fire"], RAMP_COLD if p["cold"] else RAMP_FIRE)
    img.blit(fig, 0, 0)
    return img


def eye(img, x, y, kind, iris, glow_level=0, inner_left=True, lid=C.BLACK):
    """A 3 px demon eye. inner_left: the inner corner (towards the nose) is on the left."""
    i = 1 if inner_left else -1   # direction from inner to outer corner
    if kind == "blink":
        for k in (-1, 0, 1):
            img.put(x + k, y, lid)
        return
    if kind == "squint":
        img.put(x - 1, y, lid)
        img.put(x, y - 1, lid)
        img.put(x + 1, y, lid)
        return
    # open / angry / glow
    color = iris
    if kind == "glow":
        color = C.EMBER if glow_level >= 2 else C.AMBER
    for k in (-1, 0, 1):
        img.put(x + k, y, color)
    img.put(x, y, C.WHITE if kind == "glow" and glow_level >= 3 else C.BLACK)
    # lid: slanted up toward the outer corner; angry slants down toward the nose
    if kind == "angry":
        img.put(x - i, y - 1, lid)
        img.put(x, y - 1, lid)
        img.put(x - i, y, lid)
    else:
        img.put(x - i, y - 1, lid)
        img.put(x, y - 1, lid)
        img.put(x + i, y - 2, lid)
    if kind == "glow" and glow_level >= 1:
        img.put(x + 2 * i, y, C.ORANGE)
        if glow_level >= 2:
            img.put(x - 2 * i, y, C.HELL)


def brows(img, lx, rx, y, kind, color=C.BLACK):
    """Left brow ends at lx (inner end), right brow starts at rx (inner end)."""
    for side, inner in ((-1, lx), (1, rx)):
        if kind == "angry":
            pts = [(inner, y + 1), (inner + side * 1, y), (inner + side * 2, y - 1), (inner + side * 3, y - 1)]
        elif kind == "sly":
            pts = [(inner, y), (inner + side * 1, y), (inner + side * 2, y - (1 if side > 0 else 0)), (inner + side * 3, y - (2 if side > 0 else 0))]
        else:
            pts = [(inner, y), (inner + side * 1, y - 1), (inner + side * 2, y - 1), (inner + side * 3, y)]
        for px, py in pts:
            img.put(px, py, color)


def sparkle(img, x, y):
    img.put(x, y, C.WHITE)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        img.put(x + dx, y + dy, C.EMBER)
    for dx, dy in ((2, 0), (-2, 0), (0, 2), (0, -2)):
        img.put(x + dx, y + dy, C.AMBER)


def coin(img, x, y, edge=False):
    if edge:   # seen edge-on while spinning
        for dy in range(-2, 3):
            img.put(x, y + dy, C.GOLD)
        img.put(x, y - 2, C.GOLD_LIGHT)
        return
    m = img.m_ellipse(x + 0.5, y + 0.5, 3.2, 3.2)
    img.paint(m, C.GOLD)
    img.inner_outline(m, C.GOLD_DARK)
    img.put(x - 1, y - 1, C.GOLD_LIGHT)
    img.put(x, y, C.GOLD_MID)


def steam(img, x, y, t):
    for k in range(3):
        img.put(x + (k + t) % 2, y - k * 2 - t, C.BONE_MID)


def red_flash(img):
    """Angry flash: warm everything up one step."""
    img.recolor({C.GREEN: C.CRIMSON, C.GREEN_LIGHT: C.RED, C.GREEN_DARK: C.BLOOD,
                 C.LILAC: C.RED, C.LILAC_LIGHT: C.HELL, C.MAUVE: C.CRIMSON,
                 C.RED: C.HELL, C.CRIMSON: C.RED})


# ------------------------------------------------------------------ mouths (centred on x, top row at y)

def mouth(img, x, y, kind, width=8, teeth=C.BONE, lips=C.BLACK, inside=C.BLOOD_DARK):
    h = width // 2
    if kind in ("rest", "sly"):
        tilt = 1 if kind == "sly" else 0
        for k in range(-h, h + 1):
            yy = y + (1 if abs(k) < h - 1 else 0) - (tilt if k > h - 3 else 0)
            img.put(x + k, yy, lips)
        if kind == "sly":
            img.put(x + h + 1, y - 2, lips)
        return
    if kind in ("talk1", "talk2", "laugh", "grin", "snarl"):
        open_h = {"talk1": 1, "talk2": 3, "laugh": 4, "grin": 2, "snarl": 2}[kind]
        w = h if kind in ("talk1", "talk2") else h + 1
        m = img.m_ellipse(x + 0.5, y + open_h / 2 + 0.5, w + 0.5, open_h / 2 + 0.6)
        img.paint(m, inside)
        img.inner_outline(m, lips)
        if kind in ("grin", "laugh", "snarl"):
            for k in range(-w + 2, w - 1, 2):
                img.put(x + k, y + 1, teeth)
            if kind == "snarl":
                img.put(x - w + 2, y + open_h, teeth)
                img.put(x + w - 2, y + open_h, teeth)


# ================================================================== MAMMON — the usurer: round, crowned, coins everywhere

def mammon(p):
    p = defaults(p)
    img = Img(SIZE, SIZE)
    ox, oy = p["dx"], p["dy"]
    hx, hy = ox, oy + p["hdy"]

    # Robe and fur.
    robe = shifted(img.m_poly([(4, 97), (8, 78), (24, 68), (72, 68), (88, 78), (92, 97)]), 0, -oy)
    img.shade(robe, RAMP_RED[:4], shadow=2)
    fur = img.m_poly([(28 + ox, 66 + oy), (68 + ox, 66 + oy), (62 + ox, 96), (34 + ox, 96)])
    img.dither(fur, C.BONE_MID, C.BONE, 0.45)
    img.inner_outline(fur, C.BONE_DARK)

    # Chain and medallion.
    chain = img.m_line(bezier([(30 + ox, 70 + oy), (40 + ox, 84 + oy), (48 + ox, 86 + oy), (56 + ox, 84 + oy), (66 + ox, 70 + oy)], 16), 1)
    img.paint(chain, C.GOLD)
    coin(img, 48 + ox, 88 + oy)

    # Head: big round face with a double chin and ears.
    for ex in (21, 75):
        ear = img.m_ellipse(ex + hx, 46 + hy, 4, 6)
        img.shade(ear, RAMP_GREEN[:3], shadow=1)
    chin = img.m_ellipse(48 + hx, 61 + hy, 20, 8)
    head = img.m_ellipse(48 + hx, 45 + hy, 25, 21) | chin
    img.shade(head, RAMP_GREEN, shadow=2)
    fold = img.m_line(bezier([(32 + hx, 59 + hy), (48 + hx, 63 + hy), (64 + hx, 59 + hy)], 12), 1)
    img.paint(fold & head, C.GREEN_DARK)
    # Rosy greedy cheeks.
    for cx in (33, 63):
        img.dither(img.m_ellipse(cx + hx, 52 + hy, 4, 2), C.GREEN, C.CRIMSON, 0.5)

    # Crown with stubby horns.
    for sx in (-1, 1):
        horn = img.m_poly([(48 + sx * 18 + hx, 28 + hy), (48 + sx * 24 + hx, 18 + hy), (48 + sx * 22 + hx, 29 + hy)])
        img.paint(horn, C.BONE_DARK)
    band = img.m_rect(30 + hx, 25 + hy, 66 + hx, 30 + hy)
    spikes = np.zeros_like(band)
    for i in range(5):
        x0 = 30 + i * 9 + hx
        spikes |= img.m_poly([(x0, 26 + hy), (x0 + 4.5, 17 + hy - (2 if i == 2 else 0)), (x0 + 9, 26 + hy)])
    crown = band | spikes
    img.shade(crown, RAMP_GOLD, shadow=1)
    for i, gem in enumerate((C.RED, C.GREEN_LIGHT, C.RED)):
        gx = 37 + i * 11 + hx
        img.put(gx, 27 + hy, gem)
        img.put(gx + 1, 27 + hy, gem)
        img.put(gx, 28 + hy, gem)
        img.put(gx + 1, 28 + hy, C.BLOOD if gem == C.RED else C.GREEN_DARK)

    # Face.
    brows(img, 44 + hx, 52 + hx, 37 + hy, p["brows"], C.GREEN_DARK)
    eye(img, 41 + hx, 42 + hy, p["eyes"], C.GREEN_LIGHT, p["glow"], inner_left=False, lid=C.GREEN_DARK)
    eye(img, 55 + hx, 42 + hy, p["eyes"], C.GREEN_LIGHT, p["glow"], inner_left=True, lid=C.GREEN_DARK)
    # Monocle on the right eye.
    ring = img.m_ellipse(55.5 + hx, 42.5 + hy, 5, 5) & ~img.m_ellipse(55.5 + hx, 42.5 + hy, 4, 4)
    img.paint(ring, C.GOLD)
    chain2 = img.m_line(bezier([(60 + hx, 45 + hy), (66 + hx, 56 + hy), (62 + hx, 68 + oy)], 10), 1)
    img.paint(chain2, C.GOLD_MID)
    # Snout.
    snout = img.m_ellipse(48 + hx, 50 + hy, 5, 3.4)
    img.shade(snout, RAMP_GREEN, shadow=1)
    img.put(46 + hx, 50 + hy, C.GREEN_DARK)
    img.put(50 + hx, 50 + hy, C.GREEN_DARK)
    # A wide grin full of gold teeth by default.
    kind = p["mouth"] if p["mouth"] != "rest" else "grin"
    mouth(img, 48 + hx, 55 + hy, kind, width=16, teeth=C.GOLD_LIGHT, lips=C.GREEN_DARK)

    # Floating coins, bobbing (and raining down when he gloats).
    t = p["t"]
    for i, (cx, cy) in enumerate([(12, 30), (84, 24), (80, 52)]):
        bob = int(round(math.sin((t + i * 2) * math.pi / 3)))
        coin(img, cx, cy + bob, edge=(t + i) % 4 == 3)
    if p["mouth"] == "laugh" or p["eyes"] == "squint":
        for i in range(4):
            coin(img, 14 + i * 22, (t * 9 + i * 13) % 90, edge=(t + i) % 2 == 1)

    if p["steam"] is not None:
        steam(img, 22 + hx, 22 + hy, p["steam"] % 3)
        steam(img, 74 + hx, 22 + hy, (p["steam"] + 1) % 3)
    if p["sparkle"]:
        sparkle(img, 57 + hx, 40 + hy)
    return finish(img, p, C.GOLD_DARK)


# ================================================================== BELIAL — the silver tongue: tall, horned, a serpent on his shoulders

def belial(p):
    p = defaults(p)
    img = Img(SIZE, SIZE)
    ox, oy = p["dx"], p["dy"]
    hx, hy = ox, oy + p["hdy"]

    # Long ram horns sweeping up and out (behind the head).
    for sx in (-1, 1):
        spine = bezier([(48 + sx * 10 + hx, 30 + hy), (48 + sx * 24 + hx, 22 + hy), (48 + sx * 34 + hx, 6 + hy),
                        (48 + sx * 26 + hx, 2 + hy)], 20)
        horn = img.m_poly(ribbon(spine, 8, 1))
        img.shade(horn, RAMP_BONE, light=(-sx, -1), shadow=1)
        for k in (5, 10, 15):
            x, y = spine[k]
            img.put(int(x), int(y), C.BONE_SHADE)

    # Black suit, white shirt, red tie.
    suit = shifted(img.m_poly([(8, 97), (14, 80), (32, 72), (64, 72), (82, 80), (88, 97)]), 0, -oy)
    img.shade(suit, RAMP_NIGHT, shadow=1)
    shirt = img.m_poly([(40 + ox, 72 + oy), (56 + ox, 72 + oy), (48 + ox, 96)])
    img.shade(shirt, RAMP_BONE, shadow=1)
    tie = img.m_poly([(45 + ox, 74 + oy), (51 + ox, 74 + oy), (49 + ox, 84 + oy), (48 + ox, 86 + oy), (47 + ox, 84 + oy)])
    img.paint(tie, C.RED)
    for sx in (-1, 1):
        lapel = img.m_poly([(48 + sx * 8 + ox, 72 + oy), (48 + sx * 18 + ox, 76 + oy), (48 + sx * 6 + ox, 96)])
        img.paint(lapel, C.DUSK)

    # Neck and long, narrow head with a pointed chin.
    neck = img.m_rect(44 + hx, 60 + hy, 52 + hx, 73 + oy)
    img.shade(neck, RAMP_RED[1:4], shadow=1, base_level=1)
    img.paint(neck & img.m_rect(0, 60 + hy, SIZE, 66 + hy), C.BLOOD)   # shadow under the jaw
    for sx in (-1, 1):
        ear = img.m_poly([(48 + sx * 13 + hx, 38 + hy), (48 + sx * 21 + hx, 30 + hy), (48 + sx * 15 + hx, 46 + hy)])
        img.shade(ear, RAMP_RED[1:], shadow=1)
    head = img.m_ellipse(48 + hx, 40 + hy, 14, 17) | img.m_poly([(35 + hx, 44 + hy), (61 + hx, 44 + hy), (52 + hx, 62 + hy),
                                                                 (48 + hx, 65 + hy), (44 + hx, 62 + hy)])
    img.shade(head, RAMP_RED[1:], shadow=2)
    # Cheek hollows.
    for sx in (-1, 1):
        img.paint(img.m_line([(48 + sx * 9 + hx, 47 + hy), (48 + sx * 7 + hx, 53 + hy)], 1), C.BLOOD)

    # Slick hair with a widow's peak.
    hair = img.m_poly([(33 + hx, 38 + hy), (34 + hx, 26 + hy), (42 + hx, 22 + hy), (54 + hx, 22 + hy), (62 + hx, 26 + hy),
                       (63 + hx, 38 + hy), (58 + hx, 30 + hy), (48 + hx, 36 + hy), (38 + hx, 30 + hy)])
    img.paint(hair, C.BLACK)
    for k in range(4):
        img.put(40 + k * 5 + hx, 25 + hy + (k % 2), C.DUSK)

    # Face.
    brows(img, 45 + hx, 51 + hx, 38 + hy, p["brows"] if p["brows"] != "calm" else "sly", C.BLACK)
    eye(img, 42 + hx, 42 + hy, p["eyes"], C.AMBER, p["glow"], inner_left=False, lid=C.BLOOD_DARK)
    eye(img, 54 + hx, 42 + hy, p["eyes"], C.AMBER, p["glow"], inner_left=True, lid=C.BLOOD_DARK)
    img.paint(img.m_line([(48 + hx, 44 + hy), (49 + hx, 50 + hy)], 1), C.BLOOD)
    # Thin moustache and pointed goatee.
    for sx in (-1, 1):
        img.paint(img.m_line([(48 + sx * 1 + hx, 54 + hy), (48 + sx * 6 + hx, 55 + hy)], 1), C.BLACK)
    goatee = img.m_poly([(45.5 + hx, 59 + hy), (50.5 + hx, 59 + hy), (48 + hx, 66 + hy)])
    img.paint(goatee, C.BLACK)
    kind = p["mouth"] if p["mouth"] != "rest" else "sly"
    mouth(img, 48 + hx, 56 + hy, kind, width=8, teeth=C.BONE, lips=C.BLOOD_DARK)
    # The silver tongue: forked, it slips out when he talks or schemes.
    if p["mouth"] in ("talk2", "sly", "laugh"):
        tip = 64 + hy if p["mouth"] != "sly" else 62 + hy
        img.paint(img.m_line([(48 + hx, 58 + hy), (48 + hx, tip)], 1), C.SILVER)
        img.put(47 + hx, tip + 1, C.SILVER)
        img.put(49 + hx, tip + 1, C.SILVER)
        img.put(48 + hx, 59 + hy, C.WHITE)

    # The serpent coiled over his shoulders, head raised by his right ear.
    t = p["t"]
    body = bezier([(14, 92 + oy), (20, 80 + oy), (34, 74 + oy), (60, 76 + oy), (74, 72 + oy), (78, 62 + oy)], 30)
    snake = img.m_poly(ribbon(body, 6, 4))
    img.shade(snake, RAMP_SILVER[:3], shadow=1)
    for k in range(3, len(body) - 3, 3):
        x, y = body[k]
        img.put(int(x), int(y), C.GREEN_DARK)
    sh = img.m_ellipse(79.5, 59.5 + oy + (1 if t % 2 else 0), 4, 3)
    img.shade(sh, RAMP_SILVER[:3], shadow=1)
    img.put(81, 58 + oy + (1 if t % 2 else 0), C.HELL)
    if t % 3 == 0 or p["mouth"] in ("sly", "laugh"):   # flick of the tongue
        yy = 60 + oy + (1 if t % 2 else 0)
        img.put(84, yy, C.RED)
        img.put(85, yy - 1, C.RED)
        img.put(85, yy + 1, C.RED)

    if p["steam"] is not None:
        steam(img, 28 + hx, 20 + hy, p["steam"] % 3)
        steam(img, 68 + hx, 20 + hy, (p["steam"] + 1) % 3)
    if p["sparkle"]:
        sparkle(img, 55 + hx, 40 + hy)
    return finish(img, p, C.BLOOD_DARK)


# ================================================================== LILITH — queen of the night: winged, crescent-crowned

def lilith(p):
    p = defaults(p)
    img = Img(SIZE, SIZE)
    ox, oy = p["dx"], p["dy"]
    hx, hy = ox, oy + p["hdy"]
    t = p["t"]
    flap = [0, -1, -2, -1, 0, 1][t % 6] if p["fire"] is None else -3

    # Pale bone-lilac skin: light enough to stand out from her dark wings and the night behind her.
    skin = [C.LILAC, C.LILAC_LIGHT, C.BONE_MID, C.BONE]

    # Great night-bird wings, feathers in ranks, with a crisp lilac edge so the silhouette reads.
    for sx in (-1, 1):
        tip_y = 12 + flap
        outline = [(48 + sx * 10, 70 + oy), (48 + sx * 20, 40 + oy), (48 + sx * 34, tip_y), (48 + sx * 46, tip_y + 6),
                   (48 + sx * 47, 40), (48 + sx * 44, 62), (48 + sx * 38, 76), (48 + sx * 30, 84), (48 + sx * 18, 82)]
        wing = img.m_poly(outline)
        img.shade(wing, [C.BLACK, C.NIGHT, C.DUSK, C.PLUM], light=(-sx, -1), shadow=1)
        # Feather lines and scalloped lower edge.
        for row in range(3):
            pts = bezier([(48 + sx * (16 + row * 4), 46 + row * 10 + oy), (48 + sx * 32, 40 + row * 12),
                          (48 + sx * (44 - row * 2), 30 + row * 14 + flap)], 14)
            img.paint(img.m_line(pts, 1) & wing, C.MAUVE if row == 0 else C.VIOLET)
        img.inner_outline(wing, C.LILAC)
        for k in range(5):
            fx = 48 + sx * (20 + k * 6)
            fy = 82 - k * 5
            img.put(fx, fy, C.BLACK)
            img.put(fx + sx, fy - 1, C.BLACK)

    # Long black hair behind the shoulders.
    back_hair = img.m_poly([(33 + hx, 34 + hy), (63 + hx, 34 + hy), (66 + hx, 60 + hy), (64 + hx, 80 + oy), (32 + hx, 80 + oy),
                            (30 + hx, 60 + hy)])
    img.paint(back_hair, C.BLACK)

    # Gown with a high, flaring collar and an amethyst.
    gown = shifted(img.m_poly([(14, 97), (20, 80), (36, 68), (60, 68), (76, 80), (82, 97)]), 0, -oy)
    img.shade(gown, [C.BLACK, C.NIGHT, C.DUSK, C.PLUM], shadow=1)
    neck = img.m_rect(45 + hx, 54 + hy, 51 + hx, 70 + oy)
    img.shade(neck, skin[:3], shadow=1, base_level=2)
    chest = img.m_poly([(42 + ox, 68 + oy), (54 + ox, 68 + oy), (50 + ox, 78 + oy), (46 + ox, 78 + oy)])
    img.shade(chest, skin, shadow=1)
    for sx in (-1, 1):
        collar = img.m_poly([(48 + sx * 6 + ox, 70 + oy), (48 + sx * 15 + ox, 50 + oy), (48 + sx * 18 + ox, 52 + oy),
                             (48 + sx * 12 + ox, 74 + oy)])
        img.shade(collar, [C.VIOLET, C.MAUVE, C.LILAC], light=(-sx, -1), shadow=1)
    for dx, dy, col in ((0, 0, C.MAUVE), (0, -1, C.LILAC_LIGHT), (-1, 0, C.VIOLET), (1, 0, C.VIOLET), (0, 1, C.VIOLET)):
        img.put(48 + ox + dx, 81 + oy + dy, col)

    # An oval face with a delicate chin.
    head = img.m_ellipse(48 + hx, 40 + hy, 13, 16) | img.m_poly([(36 + hx, 44 + hy), (60 + hx, 44 + hy), (52 + hx, 56 + hy),
                                                                 (48 + hx, 58 + hy), (44 + hx, 56 + hy)])
    img.shade(head, skin, shadow=2)

    # Small black horns curving up from the hairline.
    for sx in (-1, 1):
        spine = bezier([(48 + sx * 7 + hx, 27 + hy), (48 + sx * 11 + hx, 16 + hy), (48 + sx * 17 + hx, 12 + hy)], 10)
        img.paint(img.m_poly(ribbon(spine, 4, 1)), C.BLACK)
        img.put(48 + sx * 10 + hx, 18 + hy, C.DUSK)

    # Front hair: centre part framing the face, a sheen on each lock.
    for sx in (-1, 1):
        lock = img.m_poly([(48 + hx, 25 + hy), (48 + sx * 15 + hx, 28 + hy), (48 + sx * 18 + hx, 44 + hy),
                           (48 + sx * 17 + hx, 68 + oy), (48 + sx * 12 + hx, 68 + oy), (48 + sx * 13 + hx, 42 + hy),
                           (48 + sx * 6 + hx, 29 + hy)])
        img.paint(lock, C.BLACK)
        img.paint(img.m_line([(48 + sx * 14 + hx, 36 + hy), (48 + sx * 15 + hx, 64 + oy)], 1) & lock, C.PLUM)

    # A bright crescent crown above the brow, on a thin silver circlet.
    img.paint(img.m_line(bezier([(37 + hx, 31 + hy), (48 + hx, 27 + hy), (59 + hx, 31 + hy)], 12), 1), C.SILVER)
    cres = img.m_ellipse(48 + hx, 23 + hy, 5.5, 5.5) & ~img.m_ellipse(50.5 + hx, 21.5 + hy, 4.6, 4.6)
    img.paint(cres, C.WHITE)
    img.inner_outline(cres, C.SILVER)
    for dx, dy in ((-6, 21), (2, 16), (-1, 29)):
        img.put(48 + dx + hx, dy + hy, C.LILAC_LIGHT)

    # Face.
    brows(img, 45 + hx, 51 + hx, 37 + hy, p["brows"], C.BLACK)
    eye(img, 43 + hx, 41 + hy, p["eyes"], C.MAUVE if p["eyes"] != "glow" else C.LILAC_LIGHT, p["glow"], inner_left=False, lid=C.BLACK)
    eye(img, 53 + hx, 41 + hy, p["eyes"], C.MAUVE if p["eyes"] != "glow" else C.LILAC_LIGHT, p["glow"], inner_left=True, lid=C.BLACK)
    img.put(48 + hx, 47 + hy, C.LILAC)
    kind = p["mouth"]
    if kind == "rest":
        # Dark lips, a knowing half smile.
        for k in range(-3, 4):
            img.put(48 + k + hx, 53 + hy + (0 if k < 2 else -1), C.PLUM)
        img.put(48 + hx, 54 + hy, C.VIOLET)
    else:
        mouth(img, 48 + hx, 52 + hy, kind, width=6, teeth=C.BONE, lips=C.PLUM, inside=C.BLACK)
    img.put(55 + hx, 51 + hy, C.BLACK)   # beauty mark

    if p["steam"] is not None:
        steam(img, 32 + hx, 22 + hy, p["steam"] % 3)
        steam(img, 64 + hx, 22 + hy, (p["steam"] + 1) % 3)
    if p["sparkle"]:
        sparkle(img, 54 + hx, 39 + hy)
    # Her pale skin needs its own angry flush.
    flush = {C.LILAC: C.CRIMSON, C.LILAC_LIGHT: C.RED, C.BONE_MID: C.HELL, C.BONE: C.ORANGE, C.MAUVE: C.CRIMSON}
    return finish(img, p, C.DUSK, stars=5, flash_map=flush)


DEMONS = {"mammon": mammon, "belial": belial, "lilith": lilith}


def frames(demon, state):
    return [DEMONS[demon](pose) for pose in poses(state)]


def write_all(out_dir):
    import os
    written = []
    for demon in DEMONS:
        folder = os.path.join(out_dir, demon)
        os.makedirs(folder, exist_ok=True)
        for state in STATES:
            path = os.path.join(folder, state + ".png")
            sheet(frames(demon, state)).save(path)
            written.append(path)
    return written
