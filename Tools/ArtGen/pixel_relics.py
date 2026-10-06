"""The cursed relics: a 16x16 icon each (Ui/relic_icons.png, in RelicRoster.All order), shown beside the portrait.

Order = RelicRoster.All: bone_die, rusty_crown, ferrymans_coin, thorned_rosary, jesters_rattle (the earned one, last).
"""
import math
import os

from pixel import C, Img, sheet

ICON = 16
ORDER = ["bone_die", "rusty_crown", "ferrymans_coin", "thorned_rosary", "jesters_rattle"]


def icon(relic_id):
    img = Img(ICON, ICON)
    if relic_id == "bone_die":
        # A yellowed bone cube, seen from a corner: a light top, a darker side, black pips.
        top = img.m_poly([(2, 5), (8, 2), (14, 5), (8, 8)])
        left = img.m_poly([(2, 5), (8, 8), (8, 15), (2, 12)])
        right = img.m_poly([(8, 8), (14, 5), (14, 12), (8, 15)])
        img.paint(left, C.BONE_MID)
        img.paint(right, C.BONE_DARK)
        img.paint(top, C.BONE)
        img.put(8, 5, C.BLACK)
        for x, y in ((4, 8), (6, 11)):
            img.put(x, y, C.BLACK)
        for x, y in ((10, 9), (12, 8), (10, 12), (12, 11)):
            img.put(x, y, C.BLOOD)
    elif relic_id == "rusty_crown":
        # A crown gone brown with rust, one point bent.
        img.paint(img.m_rect(2, 9, 13, 13), C.GOLD_MID)
        for px in (2, 7, 12):
            img.paint(img.m_poly([(px, 9), (px + 1, 4 if px != 12 else 6), (px + 2, 9)]), C.GOLD)
        img.dither(img.m_rect(2, 9, 13, 13), C.GOLD_MID, C.BLOOD_DARK, 0.45)
        img.paint(img.m_rect(2, 13, 13, 13), C.GOLD_DARK)
        img.put(7, 11, C.CRIMSON)
        img.put(8, 11, C.CRIMSON)
    elif relic_id == "ferrymans_coin":
        # An old silver obol with a hole for the string, a skull stamped on it.
        coin = img.m_ellipse(7.5, 8, 6.5, 6.5)
        img.paint(coin, C.SILVER)
        img.dither(coin & img.m_rect(9, 0, ICON, ICON), C.SILVER, C.SILVER_DARK, 0.5)
        img.inner_outline(coin, C.SILVER_DARK)
        img.paint(img.m_ellipse(7.5, 7, 2.5, 2.5), C.BONE_SHADE)
        img.put(6, 7, C.BLACK)
        img.put(9, 7, C.BLACK)
        img.paint(img.m_rect(6, 10, 9, 10), C.BONE_SHADE)
        img.put(7, 3, C.BLACK)
        img.put(8, 3, C.BLACK)
    elif relic_id == "thorned_rosary":
        # A loop of dark beads with thorns between them, a small cross hanging below.
        for i in range(10):
            a = 2 * math.pi * i / 10
            x = int(round(8 + 5.5 * math.cos(a)))
            y = int(round(6 + 4.5 * math.sin(a)))
            img.put(x, y, C.CRIMSON if i % 2 == 0 else C.GREEN_DARK)
        img.paint(img.m_rect(7, 11, 8, 15), C.BONE_DARK)
        img.paint(img.m_rect(5, 13, 10, 13), C.BONE_DARK)
        img.put(3, 3, C.GREEN)
        img.put(13, 3, C.GREEN)
    elif relic_id == "jesters_rattle":
        # A fool's stick: a little grinning head in a three-pointed cap on a crimson handle, gold bells on the points.
        img.paint(img.m_line([(9, 15), (12, 9)], 1), C.CRIMSON)
        img.paint(img.m_rect(11, 14, 12, 15), C.GOLD_MID)
        head = img.m_ellipse(7, 8, 3.5, 3.5)
        img.paint(head, C.BONE)
        img.put(6, 8, C.BLACK)
        img.put(8, 8, C.BLACK)
        img.paint(img.m_rect(6, 10, 8, 10), C.CRIMSON)
        img.paint(img.m_poly([(5, 5), (1, 2), (3, 6)]), C.CRIMSON)
        img.paint(img.m_poly([(6, 5), (7, 0), (8, 5)]), C.GOLD)
        img.paint(img.m_poly([(9, 5), (13, 2), (11, 6)]), C.VIOLET)
        img.paint(img.m_rect(4, 5, 10, 5), C.GOLD_MID)
        for x, y in ((1, 2), (7, 0), (13, 2)):
            img.put(x, y, C.GOLD_LIGHT)
    img.outline(C.BLACK)
    return img


def write_all(art_dir):
    path = os.path.join(art_dir, "Ui", "relic_icons.png")
    sheet([icon(r) for r in ORDER]).save(path)
    return [path]