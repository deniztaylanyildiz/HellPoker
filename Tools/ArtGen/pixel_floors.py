"""Phase 2: Mammon's floors.

The faces met on the floors — the Coin Imp and the Golden-Eyed Collector — as animated 96x96 portraits in every state the demons
have (Art/Demons/<id>/<state>.png); their halls (Art/Backgrounds/<id>/..., built like the demons' salons); the chapter map's
backdrop (Ui/chapter_map.png) and its node icons (Ui/map_nodes.png, 16x16, in NodeKind order then the gate); and the floors'
strangers (Art/Events/<id>.png, 48x48). One palette (pixel.py), no anti-aliasing.
"""
import math
import os

import numpy as np

import pixel_demons as D
import pixel_salons as S
from pixel import C, Img, RAMP_FIRE, RAMP_GOLD, RAMP_GREEN, bezier, sheet, shifted
from pixel_layers import Particles, write

SIZE = 96
W, H = 480, 270

RAMP_ROBE = [C.BLACK, C.NIGHT, C.DUSK, C.PLUM, C.VIOLET]
RAMP_SACK = [C.BONE_SHADE, C.BONE_DARK, C.BONE_MID]


# ================================================================== THE COIN IMP — small, green, greedy, a sack of coins in his arms

# An imp's colours: Mammon's green one; Belial's and Lilith's imps (pixel_floors2) are the same little devil in their own colours.
MAMMON_IMP = {"skin": RAMP_GREEN, "dark": C.GREEN_DARK, "mid": C.GREEN, "light": C.GREEN_LIGHT, "wings": [C.BLOOD_DARK, C.BLOOD, C.CRIMSON],
              "vein": C.BLOOD_DARK, "inner_ear": C.CRIMSON, "cheek": C.CRIMSON, "iris": C.AMBER, "accent": C.GREEN_DARK, "flash_map": None,
              "extra": None}


def imp(p, scheme=None):
    s_ = scheme or MAMMON_IMP
    p = D.defaults(p)
    img = Img(SIZE, SIZE)
    ox, oy = p["dx"], p["dy"]
    hx, hy = ox, oy + p["hdy"]
    t = p["t"]
    skin3 = s_["skin"][:3]

    # Little bat wings behind him, flapping a pixel.
    flap = 1 if t % 2 else 0
    for s in (-1, 1):
        wing = img.m_poly([(48 + s * 12 + ox, 64 + oy), (48 + s * 38 + ox, 42 + oy - flap), (48 + s * 33 + ox, 55 + oy),
                           (48 + s * 41 + ox, 60 + oy - flap), (48 + s * 30 + ox, 67 + oy), (48 + s * 35 + ox, 76 + oy),
                           (48 + s * 17 + ox, 76 + oy)])
        img.shade(wing, s_["wings"], shadow=1)
        img.paint(img.m_line([(48 + s * 15 + ox, 66 + oy), (48 + s * 36 + ox, 45 + oy - flap)], 1) & wing, s_["vein"])
        img.paint(img.m_line([(48 + s * 16 + ox, 70 + oy), (48 + s * 38 + ox, 61 + oy - flap)], 1) & wing, s_["vein"])

    # A tail with an arrow tip, curling up on the right.
    tail = img.m_line(bezier([(60 + ox, 90 + oy), (80 + ox, 94), (88 + ox, 78), (80 + ox, 70)], 16), 2)
    img.paint(tail, s_["dark"])
    img.paint(img.m_poly([(76 + ox, 64), (84 + ox, 68), (78 + ox, 72)]), s_["dark"])

    # A pot belly.
    body = img.m_ellipse(48 + ox, 82 + oy, 17, 15)
    img.shade(body, skin3, shadow=2)
    img.dither(img.m_ellipse(48 + ox, 86 + oy, 10, 8) & body, s_["mid"], s_["light"], 0.35)

    # The sack of coins, clutched in front; a coin or two spill out of the neck.
    sack = img.m_ellipse(48 + ox, 90 + oy, 14, 9) | img.m_poly([(41 + ox, 83 + oy), (55 + ox, 83 + oy), (52 + ox, 76 + oy), (44 + ox, 76 + oy)])
    img.shade(sack, RAMP_SACK, shadow=1)
    img.paint(img.m_rect(43 + ox, 80 + oy, 53 + ox, 81 + oy), C.GOLD_DARK)
    img.paint(img.m_line([(46 + ox, 88 + oy), (50 + ox, 92 + oy)], 1), C.BONE_SHADE)
    D.coin(img, 45 + ox, 74 + oy, edge=t % 4 == 1)
    D.coin(img, 52 + ox, 73 + oy, edge=t % 4 == 3)
    for s in (-1, 1):   # little clawed hands round the sack
        hand = img.m_ellipse(48 + s * 13 + ox, 86 + oy, 3.5, 3)
        img.shade(hand, skin3, shadow=1)

    # Big pointed ears, little horns, a round head.
    for s in (-1, 1):
        ear = img.m_poly([(48 + s * 12 + hx, 40 + hy), (48 + s * 33 + hx, 27 + hy), (48 + s * 20 + hx, 50 + hy)])
        img.shade(ear, skin3, shadow=1)
        img.paint(img.m_line([(48 + s * 16 + hx, 42 + hy), (48 + s * 28 + hx, 31 + hy)], 1) & ear, s_["inner_ear"])
        horn = img.m_poly([(48 + s * 6 + hx, 35 + hy), (48 + s * 11 + hx, 24 + hy), (48 + s * 11 + hx, 36 + hy)])
        img.paint(horn, C.BONE_DARK)
        img.put(48 + s * 11 + hx, 24 + hy, C.BONE)
    head = img.m_ellipse(48 + hx, 47 + hy, 16, 14)
    img.shade(head, s_["skin"], shadow=2)
    for cx in (37, 59):
        img.dither(img.m_ellipse(cx + hx, 52 + hy, 3, 2), s_["mid"], s_["cheek"], 0.5)

    D.brows(img, 44 + hx, 52 + hx, 40 + hy, p["brows"], s_["dark"])
    D.eye(img, 42 + hx, 45 + hy, p["eyes"], s_["iris"], p["glow"], inner_left=False, lid=s_["dark"])
    D.eye(img, 54 + hx, 45 + hy, p["eyes"], s_["iris"], p["glow"], inner_left=True, lid=s_["dark"])
    img.put(46 + hx, 50 + hy, s_["dark"])
    img.put(50 + hx, 50 + hy, s_["dark"])
    kind = p["mouth"] if p["mouth"] != "rest" else "grin"
    D.mouth(img, 48 + hx, 53 + hy, kind, width=12, teeth=C.BONE, lips=s_["dark"])
    if kind in ("grin", "laugh"):
        img.put(50 + hx, 54 + hy, C.GOLD_LIGHT)   # the one gold tooth
    if s_["extra"]:
        s_["extra"](img, p, hx, hy)

    if p["mouth"] == "laugh" or p["eyes"] == "squint":
        for i in range(3):
            D.coin(img, 18 + i * 30, (t * 11 + i * 17) % 40 + 6, edge=(t + i) % 2 == 1)
    if p["steam"] is not None:
        D.steam(img, 30 + hx, 30 + hy, p["steam"] % 3)
        D.steam(img, 66 + hx, 30 + hy, (p["steam"] + 1) % 3)
    if p["sparkle"]:
        D.sparkle(img, 56 + hx, 43 + hy)
    return D.finish(img, p, s_["accent"], flash_map=s_["flash_map"])


# ================================================================== THE COLLECTOR — a deep hood, two golden eyes, the ledger and the chain

def gold_eye(img, x, y, kind, glow):
    """A narrow glowing eye in the dark of the hood (x, y: its middle)."""
    if kind == "blink":
        img.paint(img.m_rect(x - 2, y, x + 2, y), C.GOLD_DARK)
        return
    if kind == "squint":
        img.put(x - 2, y, C.GOLD)
        img.put(x - 1, y - 1, C.GOLD_LIGHT)
        img.put(x, y - 1, C.GOLD_LIGHT)
        img.put(x + 1, y - 1, C.GOLD_LIGHT)
        img.put(x + 2, y, C.GOLD)
        return
    img.paint(img.m_rect(x - 2, y, x + 2, y), C.GOLD)
    img.paint(img.m_rect(x - 1, y - 1, x + 1, y - 1), C.GOLD_LIGHT)
    img.put(x, y, C.WHITE if glow >= 2 or kind == "glow" else C.GOLD_LIGHT)
    if kind == "angry":   # the brow comes down over the inner corner
        img.put(x - 2, y - 1, C.BLACK)
        img.put(x + 2, y - 1, C.BLACK)


def collector(p):
    p = D.defaults(p)
    img = Img(SIZE, SIZE)
    ox, oy = p["dx"], p["dy"]
    hx, hy = ox, oy + p["hdy"]
    t = p["t"]

    # The robe, its gold hem down the front.
    robe = img.m_poly([(4, 97), (12, 74), (28, 62 + oy), (68, 62 + oy), (84, 74), (92, 97)])
    img.shade(robe, RAMP_ROBE[1:], shadow=2)
    img.paint(img.m_line([(48 + ox, 64 + oy), (48 + ox, 97)], 1), C.GOLD_DARK)
    for side in (-1, 1):
        img.paint(img.m_line([(48 + side * 20 + ox, 64 + oy), (48 + side * 30 + ox, 97)], 1), C.NIGHT)

    # The chain of office across the chest, a key hanging from it.
    chain = img.m_line(bezier([(30 + ox, 66 + oy), (40 + ox, 78 + oy), (48 + ox, 80 + oy), (56 + ox, 78 + oy), (66 + ox, 66 + oy)], 16), 1)
    for y, x in zip(*np.nonzero(chain)):
        img.put(x, y, C.GOLD if (x + y) % 2 else C.GOLD_DARK)
    img.paint(img.m_rect(47 + ox, 81 + oy, 49 + ox, 89 + oy), C.GOLD)
    img.paint(img.m_ellipse(48 + ox, 81 + oy, 3, 3) & ~img.m_ellipse(48 + ox, 81 + oy, 1.4, 1.4), C.GOLD_LIGHT)
    img.paint(img.m_rect(49 + ox, 87 + oy, 51 + ox, 87 + oy), C.GOLD)
    img.paint(img.m_rect(49 + ox, 89 + oy, 50 + ox, 89 + oy), C.GOLD)

    # The ledger in the crook of his left arm.
    book = img.m_rect(8 + ox, 72 + oy, 30 + ox, 92 + oy)
    img.shade(book, [C.BLOOD_DARK, C.BLOOD, C.CRIMSON], shadow=1)
    img.paint(img.m_rect(9 + ox, 73 + oy, 29 + ox, 74 + oy), C.BONE_MID)
    for cx, cy in ((8, 72), (30, 72), (8, 92), (30, 92)):
        img.put(cx + ox, cy + oy, C.GOLD)
    img.paint(img.m_rect(18 + ox, 80 + oy, 20 + ox, 84 + oy), C.GOLD_MID)
    hand = img.m_ellipse(28 + ox, 86 + oy, 4, 3)
    img.paint(hand, C.BONE_SHADE)

    # A coin turning between the fingers of the right hand.
    img.paint(img.m_ellipse(70 + ox, 84 + oy, 4, 3), C.BONE_SHADE)
    D.coin(img, 72 + ox, 79 + oy + (1 if t % 4 in (1, 2) else 0), edge=t % 2 == 1)

    # The hood: tall and pointed, deep black inside.
    hood = img.m_poly([(24 + hx, 68 + hy), (28 + hx, 36 + hy), (40 + hx, 20 + hy), (48 + hx, 12 + hy), (56 + hx, 20 + hy), (68 + hx, 36 + hy), (72 + hx, 68 + hy)])
    img.shade(hood, RAMP_ROBE[1:], shadow=2)
    img.inner_outline(hood, C.GOLD_DARK)
    face = img.m_ellipse(48 + hx, 46 + hy, 13, 16)
    img.paint(face, C.BLACK)

    # The eyes: the only thing in there; a faint golden haze around them.
    glow = p["glow"]
    haze = (img.m_ellipse(42 + hx, 44 + hy, 6, 4) | img.m_ellipse(54 + hx, 44 + hy, 6, 4)) & face
    img.dither(haze, C.BLACK, C.GOLD_DARK, 0.25 + 0.1 * glow)
    kind = p["eyes"]
    if p["brows"] == "angry":
        kind = "angry"
    gold_eye(img, 42 + hx, 44 + hy, kind, glow)
    gold_eye(img, 54 + hx, 44 + hy, kind, glow)
    # A mouth only shows when he speaks: a thin glint of gold teeth in the dark.
    mouth = p["mouth"]
    if mouth in ("talk2", "laugh", "grin", "snarl"):
        width = 6 if mouth != "laugh" else 8
        for k in range(-width // 2, width // 2 + 1, 2):
            img.put(48 + hx + k, 54 + hy, C.GOLD_MID)
        if mouth in ("laugh", "snarl"):
            for k in range(-width // 2 + 1, width // 2, 2):
                img.put(48 + hx + k, 56 + hy, C.GOLD_DARK)
    elif mouth == "talk1":
        img.put(47 + hx, 54 + hy, C.GOLD_DARK)
        img.put(49 + hx, 54 + hy, C.GOLD_DARK)

    if p["mouth"] == "laugh" or p["eyes"] == "squint":
        for i in range(3):
            D.coin(img, 14 + i * 34, (t * 9 + i * 15) % 34 + 4, edge=(t + i) % 2 == 1)
    if p["steam"] is not None:
        D.steam(img, 34 + hx, 16 + hy, p["steam"] % 3)
        D.steam(img, 62 + hx, 16 + hy, (p["steam"] + 1) % 3)
    if p["sparkle"]:
        D.sparkle(img, 56 + hx, 42 + hy)
    return D.finish(img, p, C.GOLD_DARK, flash_map={C.PLUM: C.CRIMSON, C.VIOLET: C.RED, C.DUSK: C.BLOOD, C.GOLD: C.HELL, C.GOLD_LIGHT: C.AMBER})


FACES = {"imp": imp, "collector": collector}


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


# ================================================================== the floors' halls

def imp_hall_base():
    """The counting room: a low green vault, shelves of coin jars, a hanging lamp, coins swept into the corners."""
    img = Img(W, H)
    S.stone_wall(img, C.NIGHT, C.DUSK, C.BLACK, seed=71, row_h=16)
    stone = img.px == C.NIGHT   # the stones go green toward the floor (the vault's damp); the mortar stays black
    img.dither(stone, C.NIGHT, C.GREEN_DARK, np.clip((img.ys - 60) / 400.0, 0, 0.35))
    # Shelves with jars along the top corners.
    for x0 in (0, 364):
        for shelf_y in (40, 72):
            img.paint(img.m_rect(x0, shelf_y, x0 + 116, shelf_y + 2), C.BONE_SHADE)
            for i in range(9):
                x = x0 + 6 + i * 12
                jar = img.m_rect(x, shelf_y - 11, x + 7, shelf_y - 1)
                img.paint(jar, C.SILVER_DARK if i % 3 else C.GREEN_DARK)
                img.paint(img.m_rect(x + 1, shelf_y - 6, x + 6, shelf_y - 1), C.GOLD_MID if (i + shelf_y) % 2 else C.GOLD)
                img.put(x + 2, shelf_y - 5, C.GOLD_LIGHT)
                img.paint(img.m_rect(x + 1, shelf_y - 12, x + 6, shelf_y - 12), C.BONE_SHADE)
    # The lamp on its chain over the middle.
    img.paint(img.m_rect(239, 0, 240, 14), C.BONE_SHADE)
    lamp = img.m_poly([(232, 14), (248, 14), (244, 22), (236, 22)])
    img.paint(lamp, C.GOLD_DARK)
    img.paint(img.m_rect(237, 22, 243, 24), C.AMBER)
    img.dither(img.m_ellipse(240, 30, 44, 14) & (img.px == C.NIGHT), C.NIGHT, C.DUSK, 0.4)
    # Coin piles in the corners and beside the cards.
    for cx, w in ((40, 90), (440, 90)):
        S.gold_heap(img, cx, H + 4, w, 20, seed=cx + 3)
    for i, (x, height) in enumerate(((120, 18), (360, 22), (132, 12), (348, 14))):
        S.coin_stack(img, x, 236, height, seed=90 + i)
    S.readable_middle(img)
    return img


def imp_hall():
    layers = [S.glint_layer(f"glint{i}", x, y, offset=(i * 4) % S.LAYER_FRAMES, vignette=S.readable_middle)
              for i, (x, y) in enumerate(((22, 34), (70, 66), (402, 34), (450, 66), (120, 184), (360, 170), (36, 254), (446, 252)))]
    particles = [Particles("motes", *S.LEFT_LOW, 6), Particles("motes", *S.RIGHT_SIDE, 8)]
    return imp_hall_base(), layers, particles


def collector_hall_base():
    """The toll gate: dark stone, a portcullis along the top, two great golden eyes carved over the way, a counting desk."""
    img = Img(W, H)
    S.stone_wall(img, C.NIGHT, C.DUSK, C.BLACK, seed=83, row_h=20)
    # The portcullis.
    img.paint(img.m_rect(0, 0, W - 1, 30), C.BLACK)
    for x in range(4, W, 14):
        img.paint(img.m_rect(x, 0, x + 2, 30), C.BONE_SHADE)
        img.put(x + 1, 30, C.SILVER_DARK)
        img.paint(img.m_poly([(x - 1, 30), (x + 4, 30), (x + 1, 36)]), C.SILVER_DARK)
    img.paint(img.m_rect(0, 8, W - 1, 10), C.BONE_SHADE)
    img.paint(img.m_rect(0, 20, W - 1, 22), C.BONE_SHADE)
    # The eyes carved in the wall over the corners.
    for cx in (56, 424):
        almond = img.m_ellipse(cx, 66, 26, 11)
        img.paint(almond, C.GOLD_DARK)
        img.paint(img.m_ellipse(cx, 66, 23, 8.5), C.BLACK)
        iris = img.m_ellipse(cx, 66, 7, 7.5) & img.m_ellipse(cx, 66, 23, 8.5)
        img.shade(iris, RAMP_GOLD, shadow=1)
        img.paint(img.m_rect(cx - 1, 60, cx, 72) & iris, C.BLACK)
        img.inner_outline(almond, C.BLACK)
    # Toll chests and the counting desk's ledgers along the floor.
    for x0 in (10, 404):
        box = img.m_rect(x0, 226, x0 + 66, 262)
        img.paint(box, C.BLOOD)
        img.paint(img.m_rect(x0, 226, x0 + 66, 231), C.GOLD_DARK)
        img.paint(img.m_rect(x0 + 30, 236, x0 + 36, 246), C.GOLD)
        img.inner_outline(box, C.BLACK)
    for i, x in enumerate((108, 352)):
        for k in range(5):
            book = img.m_rect(x + k % 2, 232 - k * 6, x + 22 + k % 2, 236 - k * 6)
            img.paint(book, [C.CRIMSON, C.GREEN_DARK, C.BLOOD, C.BONE_SHADE, C.PLUM][(k + i) % 5])
            img.inner_outline(book, C.BLACK)
    S.readable_middle(img)
    return img


def collector_hall():
    layers = [S.glint_layer(f"eye{i}", x, 63, offset=i * 6 % S.LAYER_FRAMES, vignette=S.readable_middle) for i, x in enumerate((54, 422))]
    layers += [S.glint_layer(f"glint{i}", x, y, offset=(i * 5 + 2) % S.LAYER_FRAMES, vignette=S.readable_middle)
               for i, (x, y) in enumerate(((43, 240), (437, 240), (118, 210), (362, 214)))]
    particles = [Particles("motes", *S.LEFT_LOW, 6), Particles("embers", *S.RIGHT_SIDE, 6)]
    return collector_hall_base(), layers, particles


HALLS = {"imp": imp_hall, "collector": collector_hall}


def write_halls(out_dir):
    written = []
    for hall, build in HALLS.items():
        base, layers, particles = build()
        written += write(os.path.join(out_dir, hall), base, layers, particles, S.VARIANT_TABLES, text_zones=S.TABLE_TEXT)
    return written


# ================================================================== the chapter's map

def chapter_map():
    """Down into the vault: dark at the top, the gold glow of the vault at the bottom; a shaft's walls at the edges, faint ledges
    under the floors' rows. The map's middle stays quiet so the paths and the nodes read."""
    img = Img(W, H)
    full = np.ones((H, W), bool)
    depth = np.clip((img.ys - 20) / 250.0, 0, 1)
    img.dither(full, C.BLACK, C.NIGHT, 0.25 + 0.5 * depth)
    # The vault's gold glows up from below the gate, fading out long before the floors.
    glow = np.clip(1 - np.hypot((img.xs - 298) / 190.0, (img.ys - 300) / 95.0), 0, 1)
    img.dither(glow > 0, C.NIGHT, C.GOLD_DARK, glow * 0.55)
    # A few coins glint in the dark of the shaft.
    rng_coins = np.random.default_rng(11)
    for _ in range(26):
        x, y = int(rng_coins.integers(142, 464)), int(rng_coins.integers(8, 250))
        img.put(x, y, C.GOLD_MID if rng_coins.integers(0, 3) else C.GOLD)
    # The shaft's walls: stone at both edges of the map, coins pressed into the mortar.
    rng = np.random.default_rng(5)
    for x0, x1 in ((128, 136), (468, W - 1)):
        img.paint(img.m_rect(x0, 0, x1, H - 1), C.DUSK)
        for y in range(0, H, 12):
            img.paint(img.m_rect(x0, y, x1, y), C.BLACK)
            if rng.integers(0, 3) == 0:
                img.put(int(rng.integers(x0 + 1, x1)), y + 6, C.GOLD_MID)
        img.paint(img.m_rect(x0, 0, x0, H - 1), C.BLACK)
    # A faint ledge under each floor's row.
    for f in range(8):
        y = 22 + f * 28 + 23
        for x in range(140, 466, 3):
            img.put(x, y, C.DUSK)
    # The vault door's glow behind the gate.
    img.dither(img.m_ellipse(298, 262, 70, 22), C.NIGHT, C.GOLD_DARK, 0.45)
    img.dither(img.m_ellipse(298, 266, 40, 10), C.GOLD_DARK, C.GOLD_MID, 0.4)
    return img


# ------------------------------------------------------------------ node icons (16x16): table, event, market, warden, treasure, fire, gate

def icon_table():
    img = Img(16, 16)
    for k, (x, y, pip) in enumerate(((2, 4, C.BLACK), (7, 3, C.CRIMSON))):
        card = img.m_rect(x, y, x + 6, y + 9)
        img.paint(card, C.BONE)
        img.inner_outline(card, C.BONE_DARK)
        img.put(x + 3, y + 4, pip)
        img.put(x + 2, y + 5, pip)
        img.put(x + 4, y + 5, pip)
    img.outline(C.BLACK)
    return img


def icon_event():
    img = Img(16, 16)
    hood = img.m_poly([(3, 15), (4, 7), (8, 2), (12, 7), (13, 15)])
    img.shade(hood, [C.NIGHT, C.DUSK, C.PLUM, C.VIOLET], shadow=1)
    img.paint(img.m_ellipse(8, 9, 3, 3.5), C.BLACK)
    img.put(7, 9, C.LILAC_LIGHT)
    img.put(9, 9, C.LILAC_LIGHT)
    img.outline(C.BLACK)
    return img


def icon_market():
    img = Img(16, 16)
    img.paint(img.m_rect(7, 2, 8, 13), C.GOLD_DARK)
    img.paint(img.m_rect(2, 4, 13, 4), C.GOLD)
    for cx in (3, 12):
        pan = img.m_ellipse(cx + 0.5, 9, 3, 1.4)
        img.paint(img.m_line([(cx, 5), (cx - 2, 8)], 1) | img.m_line([(cx + 1, 5), (cx + 3, 8)], 1), C.GOLD_DARK)
        img.paint(pan, C.GOLD)
    img.paint(img.m_rect(4, 13, 11, 14), C.GOLD_DARK)
    img.put(12, 8, C.GOLD_LIGHT)
    img.outline(C.BLACK)
    return img


def icon_warden():
    img = Img(16, 16)
    almond = img.m_ellipse(8, 8, 7.4, 4.2)
    img.paint(almond, C.GOLD_DARK)
    img.paint(img.m_ellipse(8, 8, 6, 3), C.BLACK)
    iris = img.m_ellipse(8, 8, 2.6, 2.8)
    img.paint(iris, C.GOLD_LIGHT)
    img.paint(img.m_rect(7, 6, 8, 10) & iris, C.BLACK)
    img.put(6, 6, C.WHITE)
    img.paint(img.m_rect(3, 2, 12, 2), C.HELL)
    img.outline(C.BLACK)
    return img


def icon_treasure():
    img = Img(16, 16)
    box = img.m_rect(2, 8, 13, 14)
    img.paint(box, C.BLOOD)
    img.paint(img.m_rect(2, 8, 13, 9), C.GOLD_DARK)
    img.paint(img.m_rect(7, 10, 8, 12), C.GOLD)
    heap = img.m_ellipse(8, 8, 5.5, 3)
    img.paint(heap & ~box, C.GOLD)
    img.put(6, 6, C.GOLD_LIGHT)
    img.put(10, 7, C.GOLD_LIGHT)
    img.outline(C.BLACK)
    return img


def icon_fire():
    img = Img(16, 16)
    flame = img.m_poly([(3, 14), (2, 9), (5, 5), (6, 8), (8, 1), (10, 6), (11, 4), (14, 9), (13, 14)])
    img.paint(flame, C.HELL)
    inner = img.m_poly([(5, 14), (5, 10), (8, 5), (11, 10), (11, 14)])
    img.paint(inner & flame, C.AMBER)
    img.paint(img.m_poly([(7, 14), (7, 11), (8, 9), (9, 11), (9, 14)]) & flame, C.EMBER)
    img.outline(C.BLACK)
    return img


def icon_gate():
    img = Img(16, 16)
    door = img.m_ellipse(8, 8, 7.4, 7.4)
    img.shade(door, RAMP_GOLD, shadow=1)
    img.paint(img.m_ellipse(8, 8, 5, 5), C.GOLD_DARK)
    for dx, dy in ((0, -4), (0, 4), (-4, 0), (4, 0), (-3, -3), (3, 3), (-3, 3), (3, -3)):
        img.paint(img.m_line([(8, 8), (8 + dx, 8 + dy)], 1), C.GOLD_LIGHT)
    img.paint(img.m_ellipse(8, 8, 1.6, 1.6), C.BLACK)
    img.outline(C.BLACK)
    return img


ICONS = [icon_table, icon_event, icon_market, icon_warden, icon_treasure, icon_fire, icon_gate]


# ================================================================== the floors' strangers (48x48)

def _backdrop(img, glow):
    img.paint(img.m_rect(0, 0, 47, 47), C.BLACK)
    img.dither(img.m_ellipse(24, 30, 26, 22), C.BLACK, glow, 0.3)


def usurer():
    """The Usurer of Purgatory: a thin grey figure with a long nose and a smile, a fat purse held up on a string."""
    img = Img(48, 48)
    _backdrop(img, C.GREEN_DARK)
    coat = img.m_poly([(12, 47), (16, 28), (24, 25), (32, 28), (36, 47)])
    img.shade(coat, [C.NIGHT, C.BONE_SHADE, C.SILVER_DARK], light=(-1, -1))
    face = img.m_ellipse(24, 18, 6, 8)
    img.paint(face, C.BONE_DARK)
    img.paint(img.m_poly([(26, 17), (34, 22), (26, 21)]), C.BONE_MID)   # the long nose
    img.put(21, 16, C.BLACK)
    img.put(25, 16, C.GOLD)
    img.paint(img.m_rect(20, 23, 25, 23), C.BLACK)
    img.paint(img.m_rect(17, 9, 31, 10), C.DUSK)   # a narrow top hat, its band green
    img.paint(img.m_rect(20, 3, 28, 9), C.DUSK)
    img.paint(img.m_rect(20, 8, 28, 8), C.GREEN)
    img.paint(img.m_line([(36, 14), (38, 26)], 1), C.BONE_SHADE)
    purse = img.m_ellipse(38, 31, 5, 5)
    img.shade(purse, [C.GREEN_DARK, C.GREEN, C.GREEN_LIGHT], light=(-1, -1))
    img.paint(img.m_rect(36, 26, 40, 27), C.GOLD)
    img.put(37, 30, C.GOLD_LIGHT)
    img.outline(C.BLACK, mask=img.px != C.BLACK)
    return img


def gambler_ghost():
    """The Gambler's Ghost: a pale see-through gambler in a brimmed hat, a pair of dice in his open hand."""
    img = Img(48, 48)
    _backdrop(img, C.DUSK)
    body = img.m_ellipse(24, 24, 11, 12) | img.m_poly([(13, 24), (35, 24), (37, 44), (31, 40), (26, 45), (21, 40), (16, 45), (11, 41)])
    img.dither(body, C.LILAC_LIGHT, C.BONE, 0.45)
    img.inner_outline(body, C.LILAC)
    img.paint(img.m_rect(10, 12, 38, 13), C.SILVER_DARK)
    img.paint(img.m_rect(16, 4, 32, 12), C.SILVER_DARK)
    img.paint(img.m_rect(16, 10, 32, 10), C.CRIMSON)
    img.paint(img.m_ellipse(20, 20, 2, 2.5), C.BLACK)
    img.paint(img.m_ellipse(28, 20, 2, 2.5), C.BLACK)
    img.paint(img.m_rect(21, 27, 27, 27), C.DUSK)
    for x, y in ((12, 33), (18, 36)):
        die = img.m_rect(x, y, x + 5, y + 5)
        img.paint(die, C.BONE)
        img.inner_outline(die, C.BONE_DARK)
        img.put(x + 1, y + 1, C.BLACK)
        img.put(x + 4, y + 4, C.BLACK)
        img.put(x + 2 + (x % 2), y + 2 + (y % 2), C.CRIMSON)
    return img


def black_market():
    """The Fence: a broad cloaked figure behind a lantern, his coat hung with stolen trinkets."""
    img = Img(48, 48)
    _backdrop(img, C.BLOOD_DARK)
    coat = img.m_poly([(6, 47), (12, 22), (24, 14), (36, 22), (42, 47)])
    img.shade(coat, [C.BLACK, C.NIGHT, C.DUSK, C.PLUM], light=(-1, -1))
    hood = img.m_ellipse(24, 18, 9, 9)
    img.shade(hood, [C.NIGHT, C.DUSK, C.PLUM], light=(-1, -1))
    img.paint(img.m_ellipse(24, 20, 6, 6), C.BLACK)
    img.put(22, 20, C.EMBER)
    img.put(26, 20, C.EMBER)
    for x, y, color in ((14, 30, C.GOLD), (18, 36, C.SILVER), (31, 31, C.GOLD_LIGHT), (34, 38, C.LILAC), (22, 40, C.CRIMSON), (28, 35, C.GOLD)):
        img.paint(img.m_rect(x, y, x + 2, y + 2), color)
        img.put(x + 1, y - 1, C.BONE_SHADE)
    lamp = img.m_rect(38, 30, 43, 37)
    img.paint(lamp, C.AMBER)
    img.inner_outline(lamp, C.GOLD_DARK)
    img.put(40, 33, C.EMBER)
    img.dither(img.m_ellipse(40, 34, 8, 8) & (img.px == C.BLACK), C.BLACK, C.GOLD_DARK, 0.3)
    return img


def treasure():
    """Coins spilled between bones, an open chest and a skull keeping watch."""
    img = Img(48, 48)
    _backdrop(img, C.GOLD_DARK)
    box = img.m_rect(10, 26, 38, 42)
    img.shade(box, [C.BLOOD_DARK, C.BLOOD, C.CRIMSON], light=(-1, -1))
    img.paint(img.m_rect(10, 26, 38, 28), C.GOLD_DARK)
    img.paint(img.m_poly([(9, 26), (12, 14), (36, 14), (39, 26)]) & ~box, C.BLOOD)
    heap = img.m_ellipse(24, 26, 13, 6)
    img.shade(heap, RAMP_GOLD, shadow=1)
    for x, y in ((18, 23), (27, 22), (31, 25), (21, 26)):
        img.put(x, y, C.GOLD_LIGHT)
    skull = img.m_ellipse(9, 41, 5, 4.5)
    img.paint(skull, C.BONE_MID)
    img.put(7, 41, C.BLACK)
    img.put(10, 41, C.BLACK)
    img.paint(img.m_line([(30, 45), (44, 41)], 1), C.BONE_DARK)
    img.outline(C.BLACK, mask=img.px != C.BLACK)
    return img


def purgatory_fire():
    """A low fire on the last floor: tall flames, two faces of souls in it, ash around."""
    img = Img(48, 48)
    _backdrop(img, C.BLOOD)
    for x in range(48):
        h = 26 + 7 * math.sin(x * 0.45) + 4 * math.sin(x * 1.3 + 1.0)
        reach = max(0.0, 1 - abs(x - 24) / 22.0)
        h = int(h * reach)
        for k in range(h):
            y = 44 - k
            level = min(len(RAMP_FIRE) - 1, k * len(RAMP_FIRE) // max(1, h))
            img.put(x, y, RAMP_FIRE[len(RAMP_FIRE) - 1 - level])
    for cx in (19, 29):
        img.paint(img.m_ellipse(cx, 30, 3, 4), C.ORANGE)
        img.put(cx - 1, 29, C.BLOOD_DARK)
        img.put(cx + 1, 29, C.BLOOD_DARK)
        img.put(cx, 32, C.BLOOD_DARK)
    img.paint(img.m_rect(6, 44, 41, 46), C.BONE_SHADE)
    for x in range(8, 40, 5):
        img.put(x, 45, C.BLACK)
    return img


STRANGERS = {"purgatory_usurer": usurer, "gambler_ghost": gambler_ghost, "black_market": black_market, "treasure": treasure,
             "purgatory_fire": purgatory_fire}


def write_all(art_dir):
    written = write_faces(os.path.join(art_dir, "Demons"))
    written += write_halls(os.path.join(art_dir, "Backgrounds"))
    ui = os.path.join(art_dir, "Ui")
    path = os.path.join(ui, "chapter_map.png")
    chapter_map().save(path)
    written.append(path)
    path = os.path.join(ui, "map_nodes.png")
    sheet([draw() for draw in ICONS]).save(path)
    written.append(path)
    events = os.path.join(art_dir, "Events")
    os.makedirs(events, exist_ok=True)
    for name, draw in STRANGERS.items():
        path = os.path.join(events, name + ".png")
        draw().save(path)
        written.append(path)
    return written
