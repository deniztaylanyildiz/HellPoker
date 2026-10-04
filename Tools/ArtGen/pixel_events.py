"""The strangers of the events between hands: a 48x48 portrait each (Art/Events/<id>.png). A demon's own offer uses the demon.

Same palette as everything else (pixel.py).
"""
import math
import os

from pixel import C, Img, RAMP_FIRE, RAMP_LILAC

SIZE = 48


def _backdrop(img, glow):
    img.paint(img.m_rect(0, 0, SIZE - 1, SIZE - 1), C.BLACK)
    img.dither(img.m_ellipse(24, 30, 26, 22), C.BLACK, glow, 0.3)


def charon():
    """The Ferryman: a tall hood over nothing, a lantern held out, the edge of a boat and dark water."""
    img = Img(SIZE, SIZE)
    _backdrop(img, C.NIGHT)
    for y in range(40, SIZE):
        for x in range(SIZE):
            if (x + y * 3) % 7 == 0:
                img.put(x, y, C.DUSK)
    img.paint(img.m_poly([(4, 41), (44, 41), (40, 46), (8, 46)]), C.BONE_SHADE)
    robe = img.m_poly([(12, 41), (16, 18), (24, 6), (32, 18), (34, 41)])
    img.paint(robe, C.NIGHT)
    img.inner_outline(robe, C.DUSK)
    img.paint(img.m_ellipse(24, 17, 5, 6), C.BLACK)
    img.put(22, 17, C.LILAC_LIGHT)
    img.put(26, 17, C.LILAC_LIGHT)
    # The oar, and the lantern on its pole.
    img.paint(img.m_line([(8, 46), (14, 10)], 1), C.BONE_SHADE)
    img.paint(img.m_line([(34, 24), (42, 20)], 1), C.BONE_SHADE)
    lamp = img.m_rect(40, 21, 44, 26)
    img.paint(lamp, C.AMBER)
    img.put(42, 23, C.WHITE)
    img.dither(img.m_ellipse(42, 23, 6, 6) & ~lamp, C.BLACK, C.GOLD_MID, 0.25)
    return img


def soul_broker():
    """The Soul Broker: a thin grinning merchant in a top hat, a jar with a soul glowing in it."""
    img = Img(SIZE, SIZE)
    _backdrop(img, C.PLUM)
    coat = img.m_poly([(10, 47), (14, 30), (24, 27), (34, 30), (38, 47)])
    img.shade(coat, [C.NIGHT, C.DUSK, C.PLUM], light=(-1, -1))
    face = img.m_ellipse(24, 21, 6, 7)
    img.paint(face, C.BONE_DARK)
    img.put(21, 20, C.BLACK)
    img.put(27, 20, C.BLACK)
    img.paint(img.m_rect(20, 24, 28, 24), C.BLACK)
    for x in range(21, 28, 2):
        img.put(x, 24, C.BONE)
    img.paint(img.m_rect(17, 6, 31, 13), C.DUSK)
    img.paint(img.m_rect(14, 13, 34, 14), C.PLUM)
    img.paint(img.m_rect(17, 11, 31, 11), C.CRIMSON)
    jar = img.m_rect(33, 30, 41, 41)
    img.paint(jar, C.SILVER_DARK)
    img.paint(img.m_ellipse(37, 36, 3, 4), C.LILAC)
    img.put(37, 35, C.WHITE)
    img.paint(img.m_rect(33, 29, 41, 29), C.GOLD)
    img.outline(C.BLACK, mask=img.px != C.BLACK)
    return img


def lost_soul():
    """A lost soul: a pale, see-through ghost with hollow eyes, holding out five cards."""
    img = Img(SIZE, SIZE)
    _backdrop(img, C.DUSK)
    body = img.m_ellipse(24, 22, 11, 13) | img.m_poly([(13, 22), (35, 22), (37, 44), (31, 40), (27, 45), (22, 40), (17, 45), (11, 41)])
    img.dither(body, C.LILAC_LIGHT, C.BONE, 0.5)
    img.inner_outline(body, C.LILAC)
    img.paint(img.m_ellipse(20, 20, 2.2, 3), C.BLACK)
    img.paint(img.m_ellipse(28, 20, 2.2, 3), C.BLACK)
    img.paint(img.m_ellipse(24, 28, 2, 2.5), C.DUSK)
    for i in range(5):
        x = 13 + i * 5
        card = img.m_rect(x, 33, x + 4, 39)
        img.paint(card, C.BONE)
        img.inner_outline(card, C.BONE_DARK)
        img.put(x + 2, 36, C.RED if i % 2 else C.BLACK)
    return img


def burning_bridge():
    """The Burning Bridge: a stone arch over the dark, burning from both ends toward the middle."""
    img = Img(SIZE, SIZE)
    _backdrop(img, C.BLOOD_DARK)
    arch = img.m_rect(0, 26, SIZE - 1, 31) & ~img.m_ellipse(24, 40, 14, 10)
    img.paint(arch, C.BONE_SHADE)
    img.paint(img.m_rect(0, 25, SIZE - 1, 25), C.BONE_DARK)
    for x in range(0, SIZE, 6):
        img.put(x, 28, C.BLACK)
    # Flames rising from both ends, tallest at the edges.
    for x in range(SIZE):
        reach = max(0, 18 - min(x, SIZE - 1 - x)) // 1
        height = int(reach * (0.7 + 0.3 * math.sin(x * 1.7)))
        for k in range(height):
            y = 24 - k
            if y < 0:
                break
            level = min(len(RAMP_FIRE) - 1, k * len(RAMP_FIRE) // max(1, height))
            img.put(x, y, RAMP_FIRE[len(RAMP_FIRE) - 1 - level])
    img.dither(img.m_rect(0, 33, SIZE - 1, SIZE - 1), C.BLACK, C.BLOOD, 0.2)
    return img

def grave_robber():
    """The Grave Robber: a hunched figure in a muddy hood, a lantern in one hand, a shovel over the shoulder."""
    img = Img(SIZE, SIZE)
    _backdrop(img, C.GREEN_DARK)
    body = img.m_poly([(8, 47), (14, 26), (26, 20), (36, 28), (40, 47)])
    img.shade(body, [C.BLOOD_DARK, C.BONE_SHADE, C.BONE_DARK], light=(-1, -1))
    hood = img.m_ellipse(24, 22, 9, 9)
    img.paint(hood, C.BONE_SHADE)
    img.paint(img.m_ellipse(25, 24, 5.5, 6), C.BLACK)
    img.put(23, 23, C.BONE)
    img.put(27, 23, C.BONE)
    img.paint(img.m_rect(23, 27, 27, 27), C.BONE_DARK)
    # The shovel.
    img.paint(img.m_line([(38, 47), (42, 10)], 1), C.BONE_DARK)
    img.paint(img.m_poly([(39, 4), (45, 4), (44, 11), (40, 11)]), C.SILVER_DARK)
    # The lantern, low on the left.
    lamp = img.m_rect(8, 34, 13, 41)
    img.paint(lamp, C.AMBER)
    img.put(10, 37, C.EMBER)
    img.inner_outline(lamp, C.GOLD_DARK)
    img.paint(img.m_rect(10, 31, 11, 33), C.GOLD_DARK)
    img.dither(img.m_ellipse(10, 38, 8, 7) & ~lamp & (img.px == C.NIGHT), C.NIGHT, C.GOLD_DARK, 0.25)
    img.outline(C.BLACK, mask=img.px != C.NIGHT)
    return img


def cursed_chest():
    """A cursed chest: an iron-bound box, the lid ajar, something glowing inside and a hand's fingers on the rim."""
    img = Img(SIZE, SIZE)
    _backdrop(img, C.VIOLET)
    box = img.m_rect(8, 26, 40, 42)
    img.shade(box, [C.BLOOD_DARK, C.BLOOD, C.CRIMSON], light=(-1, -1))
    for x in (8, 23, 24, 40):
        img.paint(img.m_rect(x, 26, x, 42), C.SILVER_DARK)
    img.paint(img.m_rect(8, 33, 40, 33), C.SILVER_DARK)
    lid = img.m_poly([(7, 24), (11, 13), (37, 13), (41, 24)])
    img.shade(lid, [C.BLOOD_DARK, C.BLOOD, C.CRIMSON], light=(-1, -1))
    img.paint(img.m_rect(7, 24, 41, 25), C.SILVER_DARK)
    # The glow between lid and box.
    img.paint(img.m_rect(10, 25, 38, 25), C.LILAC_LIGHT)
    img.dither(img.m_rect(9, 18, 39, 24) & ~lid, C.NIGHT, C.LILAC, 0.4)
    for x in (15, 18, 21):
        img.paint(img.m_rect(x, 23, x + 1, 26), C.BONE_DARK)
    # The lock.
    img.paint(img.m_rect(22, 29, 25, 33), C.GOLD)
    img.put(23, 31, C.BLACK)
    img.outline(C.BLACK, mask=img.px != C.NIGHT)
    return img


PORTRAITS = {"charon": charon, "soul_broker": soul_broker, "lost_soul": lost_soul, "burning_bridge": burning_bridge,
             "grave_robber": grave_robber, "cursed_chest": cursed_chest}


def write_all(art_dir):
    folder = os.path.join(art_dir, "Events")
    os.makedirs(folder, exist_ok=True)
    out = []
    for event_id, draw in PORTRAITS.items():
        path = os.path.join(folder, event_id + ".png")
        draw().save(path)
        out.append(path)
    return out
