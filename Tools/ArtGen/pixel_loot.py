"""Demo 3.5: the spoils scene (ARAM's choice) — what lies where a beaten demon sat, and the back of a relic card.

Ui/loot_fallen_mammon.png, Ui/loot_fallen_belial.png (160x64: the demons whose fall leaves spoils; Lilith's leads to Lucifer): the throne
toppled in the demon's colours on a dark floor. Ui/loot_sigil.png (32x32): the pentacle on a card's back, shown at 2x while it turns.
"""
import math
import os

from pixel import C, Img, RAMP_GOLD, RAMP_GREEN, RAMP_RED, RAMP_SILVER, RAMP_BONE

W, H = 160, 64


def floor_shadow(img):
    img.dither(img.m_ellipse(80, 54, 76, 9), C.NIGHT, C.BLACK, 0.35)


def mammon():
    """Mammon's counting throne on its side: the gilt back cracked, the green cushion spilled, his crown rolled away, coins everywhere,
    one pan of his scales snapped off."""
    img = Img(W, H)
    floor_shadow(img)
    back = img.m_poly([(34, 50), (62, 14), (86, 22), (60, 56)])
    img.shade(back, RAMP_GOLD, light=(-1, -1))
    img.paint(img.m_line([(60, 20), (52, 36), (58, 46)], 1), C.GOLD_DARK)            # the crack
    seat = img.m_poly([(58, 56), (84, 24), (104, 30), (80, 58)])
    img.shade(seat, RAMP_GREEN, light=(-1, -1))
    img.paint(img.m_rect(96, 46, 99, 55), C.GOLD_MID)                               # a leg, broken off
    crown = img.m_poly([(112, 52), (114, 42), (118, 47), (122, 40), (126, 47), (130, 42), (131, 52)])
    img.shade(crown, RAMP_GOLD, light=(-1, -1))
    for x in (117, 122, 127):
        img.put(x, 49, C.GREEN_LIGHT)
    for cx, cy in ((22, 52), (28, 56), (138, 55), (144, 51), (108, 58), (47, 59), (150, 57)):
        coin = img.m_ellipse(cx, cy, 3, 1.6)
        img.paint(coin, C.GOLD)
        img.put(cx - 1, cy - 1, C.GOLD_LIGHT)
    pan = img.m_ellipse(14, 44, 7, 2.5)
    img.paint(pan, C.GOLD_MID)
    img.paint(img.m_line([(14, 42), (22, 30)], 1), C.SILVER_DARK)                    # its chain, hanging from nothing
    img.outline(C.BLACK)
    return img


def belial():
    """Belial's stage after the fall: the crimson curtain heaped on the boards, his mask cracked in two, the silver staff snapped, the
    serpent limp across it."""
    img = Img(W, H)
    floor_shadow(img)
    for i, x in enumerate((20, 40, 60, 80, 100, 120)):
        fold = img.m_poly([(x, 58), (x + 6, 30 + (i % 2) * 6), (x + 18, 26 + (i % 3) * 4), (x + 26, 58)])
        img.shade(fold, RAMP_RED, light=(-1, -1))
    mask_l = img.m_ellipse(72, 38, 9, 11) & img.m_rect(60, 24, 72, 52)
    mask_r = img.m_ellipse(84, 40, 9, 11) & img.m_rect(84, 26, 96, 54)
    img.shade(mask_l, RAMP_BONE, light=(-1, -1))
    img.paint(mask_r, C.DUSK)
    img.inner_outline(mask_r, C.PLUM)
    img.paint(img.m_rect(66, 34, 69, 36), C.BLACK)                                    # its eyes
    img.paint(img.m_rect(87, 36, 90, 38), C.BLOOD)
    img.paint(img.m_line([(64, 46), (70, 48)], 1), C.BLACK)
    staff_a = img.m_line([(18, 50), (52, 42)], 2)
    staff_b = img.m_line([(108, 46), (146, 54)], 2)
    img.paint(staff_a | staff_b, C.SILVER)
    img.put(146, 54, C.GOLD_LIGHT)
    pts = [(30 + i * 4, 46 + int(round(3 * math.sin(i * 0.9)))) for i in range(26)]
    img.paint(img.m_line(pts, 2), C.SILVER_DARK)
    img.put(pts[-1][0] + 1, pts[-1][1], C.BLOOD)                                       # the serpent's tongue
    img.outline(C.BLACK)
    return img


def sigil():
    """A card's back: a gold pentacle in a dark ring."""
    img = Img(32, 32)
    ring = img.m_ellipse(16, 16, 14, 14) & ~img.m_ellipse(16, 16, 11.5, 11.5)
    img.paint(img.m_ellipse(16, 16, 14, 14), C.DUSK)
    img.paint(ring, C.GOLD_MID)
    star = []
    for k in range(5):
        a = -math.pi / 2 + k * 4 * math.pi / 5
        star.append((16 + 10 * math.cos(a), 16 + 10 * math.sin(a)))
    img.paint(img.m_line([(round(x), round(y)) for x, y in star + star[:1]], 1), C.GOLD)
    img.put(16, 16, C.EMBER)
    img.outline(C.BLACK)
    return img


def write_all(art_dir):
    paths = []
    for name, img in (("loot_fallen_mammon", mammon()), ("loot_fallen_belial", belial()), ("loot_sigil", sigil())):
        path = os.path.join(art_dir, "Ui", name + ".png")
        img.save(path)
        paths.append(path)
    return paths
