"""The sinner classes: a 48x48 portrait for each (shown at x2 on the choice screen) and a 16x16 badge icon strip.

Same palette as everything else (pixel.py). Order of the icon strip = SinnerRoster.All: peasant, warlock, king, jester.
"""
import os

from pixel import C, Img, RAMP_BONE, RAMP_GOLD, RAMP_LILAC, RAMP_RED, sheet

SIZE = 48
ICON = 16
ORDER = ["peasant", "warlock", "king", "jester"]


def _backdrop(img, glow):
    """A dark vault behind every portrait, a soft glow of the class colour low down."""
    img.paint(img.m_rect(0, 0, SIZE - 1, SIZE - 1), C.NIGHT)
    img.dither(img.m_ellipse(24, 46, 26, 16), C.NIGHT, glow, 0.35)
    img.dither(img.m_rect(0, 0, SIZE - 1, 6), C.NIGHT, C.BLACK, 0.5)


def _face(img, cx, cy, skin, shadow):
    head = img.m_ellipse(cx, cy, 7.5, 8.5)
    img.paint(head, skin)
    img.dither(head & img.m_rect(cx + 2, 0, SIZE, SIZE), skin, shadow, 0.5)
    img.inner_outline(head, shadow)
    img.put(cx - 3, cy, C.BLACK)
    img.put(cx + 3, cy, C.BLACK)
    return head


def peasant():
    img = Img(SIZE, SIZE)
    _backdrop(img, C.GREEN_DARK)
    # A rough brown smock, rope belt.
    body = img.m_poly([(8, 47), (12, 33), (24, 30), (36, 33), (40, 47)])
    img.shade(body, [C.BLOOD_DARK, C.BLOOD, C.CRIMSON], light=(-1, -1))
    img.paint(img.m_rect(13, 40, 35, 41), C.GOLD_MID)
    _face(img, 24, 23, C.BONE, C.BONE_DARK)
    # A straw hat: wide brim, low crown.
    img.paint(img.m_ellipse(24, 16, 15, 3.2), C.GOLD)
    img.paint(img.m_ellipse(24, 12, 7.5, 5) & img.m_rect(0, 0, SIZE, 15), C.GOLD_LIGHT)
    img.inner_outline(img.m_ellipse(24, 16, 15, 3.2), C.GOLD_MID)
    img.paint(img.m_rect(17, 14, 31, 14), C.GOLD_MID)
    # Stubble and a tired mouth.
    img.dither(img.m_rect(19, 27, 29, 30), C.BONE, C.BONE_DARK, 0.4)
    img.paint(img.m_rect(22, 28, 26, 28), C.BONE_SHADE)
    # A pitchfork over the shoulder.
    img.paint(img.m_line([(42, 47), (35, 8)], 1), C.BONE_SHADE)
    for dx in (-2, 0, 2):
        img.paint(img.m_line([(34 + dx, 9), (34 + dx, 3)], 1), C.SILVER)
    img.paint(img.m_rect(32, 9, 36, 9), C.SILVER)
    img.outline(C.BLACK, mask=img.px != C.NIGHT)
    return img


def warlock():
    img = Img(SIZE, SIZE)
    _backdrop(img, C.VIOLET)
    # A deep hood and robe: only the eyes and the chin catch the light.
    robe = img.m_poly([(6, 47), (12, 26), (24, 8), (36, 26), (42, 47)])
    img.shade(robe, RAMP_LILAC[:3], light=(-1, -1))
    hood_in = img.m_ellipse(24, 24, 7, 9)
    img.paint(hood_in, C.BLACK)
    img.put(21, 23, C.EMBER)
    img.put(22, 23, C.AMBER)
    img.put(26, 23, C.AMBER)
    img.put(27, 23, C.EMBER)
    img.paint(img.m_rect(22, 29, 26, 31), C.BONE_DARK)
    # Runes along the hem.
    for x in range(10, 40, 5):
        img.put(x, 44, C.LILAC_LIGHT)
        img.put(x + 1, 43, C.LILAC_LIGHT)
    # A staff with a glowing orb.
    img.paint(img.m_line([(40, 47), (40, 12)], 1), C.BONE_SHADE)
    orb = img.m_ellipse(40, 9, 3.5, 3.5)
    img.paint(orb, C.LILAC)
    img.put(39, 8, C.WHITE)
    img.inner_outline(orb, C.VIOLET)
    img.outline(C.BLACK, mask=img.px != C.NIGHT)
    return img


def king():
    img = Img(SIZE, SIZE)
    _backdrop(img, C.BLOOD)
    # A red cloak with an ermine collar.
    cloak = img.m_poly([(5, 47), (11, 31), (24, 28), (37, 31), (43, 47)])
    img.shade(cloak, RAMP_RED[1:4], light=(-1, -1))
    collar = img.m_ellipse(24, 33, 13, 3.5)
    img.paint(collar, C.BONE)
    for x in range(14, 36, 4):
        img.put(x, 33, C.BLACK)
    _face(img, 24, 22, C.BONE, C.BONE_DARK)
    # A full grey beard.
    beard = img.m_poly([(17, 25), (31, 25), (28, 33), (24, 35), (20, 33)])
    img.paint(beard, C.SILVER)
    img.dither(beard, C.SILVER, C.SILVER_DARK, 0.3)
    # The crown: gold band, three points, a ruby.
    img.paint(img.m_rect(16, 12, 32, 15), C.GOLD)
    for px in (16, 23, 30):
        img.paint(img.m_poly([(px, 12), (px + 1, 6), (px + 2, 12)]), C.GOLD_LIGHT)
    img.paint(img.m_rect(16, 15, 32, 15), C.GOLD_MID)
    img.put(24, 13, C.RED)
    img.put(23, 13, C.HELL)
    img.outline(C.BLACK, mask=img.px != C.NIGHT)
    return img


def _jester_cap(img, cx, top, reach, droop, bell):
    """A fool's cap: three floppy points (crimson, gold, violet) falling to the sides, a gold bell on each tip."""
    band = img.m_rect(cx - reach // 2, top + droop, cx + reach // 2, top + droop + 2)
    points = [
        ([(cx - 3, top + droop), (cx - reach, top + 1), (cx - reach - 2, top + droop + 3), (cx - reach // 2, top + droop)], C.CRIMSON, (cx - reach - 2, top + droop + 3)),
        ([(cx - 2, top + droop), (cx, top - 1), (cx + 2, top + droop)], C.GOLD, (cx, top - 1)),
        ([(cx + 3, top + droop), (cx + reach, top + 1), (cx + reach + 2, top + droop + 3), (cx + reach // 2, top + droop)], C.VIOLET, (cx + reach + 2, top + droop + 3)),
    ]
    for poly, colour, tip in points:
        img.paint(img.m_poly(poly), colour)
    img.paint(band, C.GOLD_MID)
    for _, _, (bx, by) in points:
        b = img.m_ellipse(bx, by, bell, bell)
        img.paint(b, C.GOLD_LIGHT)
        img.put(int(bx), int(by + bell), C.GOLD_DARK)


def jester():
    img = Img(SIZE, SIZE)
    _backdrop(img, C.CRIMSON)
    # Motley: a doublet split crimson and gold, a pointed ruff.
    body = img.m_poly([(7, 47), (12, 33), (24, 30), (36, 33), (41, 47)])
    img.paint(body & img.m_rect(0, 0, 23, SIZE), C.CRIMSON)
    img.paint(body & img.m_rect(24, 0, SIZE, SIZE), C.GOLD_MID)
    img.dither(body & img.m_rect(0, 40, SIZE, SIZE), C.CLEAR, C.BLOOD_DARK, 0.25)
    for k, x in enumerate(range(13, 36, 4)):
        img.paint(img.m_poly([(x, 31), (x + 2, 36), (x + 4, 31)]), C.BONE if k % 2 == 0 else C.LILAC_LIGHT)
    head = _face(img, 24, 23, C.BONE, C.BONE_DARK)
    # Painted diamonds under the eyes, a wide grin.
    for ex in (21, 27):
        img.put(ex, 25, C.CRIMSON)
        img.put(ex, 26, C.RED)
    img.paint(img.m_rect(20, 28, 28, 28), C.BLACK)
    img.put(19, 27, C.BLACK)
    img.put(29, 27, C.BLACK)
    for x in (21, 23, 25, 27):
        img.put(x, 29, C.BONE_SHADE)
    _jester_cap(img, 24, 6, 14, 8, 1.6)
    img.outline(C.BLACK, mask=img.px != C.NIGHT)
    return img


def icon(class_id):
    """The 16x16 badge: a pitchfork, a glowing eye, a crown, a fool's cap."""
    img = Img(ICON, ICON)
    if class_id == "peasant":
        img.paint(img.m_line([(8, 15), (8, 5)], 1), C.BONE_DARK)
        for dx in (-3, 0, 3):
            img.paint(img.m_line([(8 + dx, 5), (8 + dx, 1)], 1), C.SILVER)
        img.paint(img.m_rect(5, 5, 11, 5), C.SILVER)
    elif class_id == "warlock":
        eye = img.m_ellipse(8, 8, 6.5, 3.5)
        img.paint(eye, C.LILAC)
        img.paint(img.m_ellipse(8, 8, 2.5, 2.5), C.VIOLET)
        img.put(8, 8, C.BLACK)
        img.put(7, 7, C.WHITE)
        img.inner_outline(eye, C.LILAC_LIGHT)
    elif class_id == "king":
        img.paint(img.m_rect(2, 9, 13, 12), C.GOLD)
        for px in (2, 7, 12):
            img.paint(img.m_poly([(px, 9), (px + 1, 3), (px + 2, 9)]), C.GOLD_LIGHT)
        img.paint(img.m_rect(2, 12, 13, 12), C.GOLD_MID)
        img.put(7, 10, C.RED)
        img.put(8, 10, C.RED)
    elif class_id == "jester":
        _jester_cap(img, 8, 3, 6, 5, 1.2)
        img.paint(img.m_rect(4, 11, 12, 13), C.BONE)
        img.put(6, 12, C.BLACK)
        img.put(10, 12, C.BLACK)
    img.outline(C.BLACK)
    return img


PORTRAITS = {"peasant": peasant, "warlock": warlock, "king": king, "jester": jester}


def write_all(art_dir):
    out = []
    folder = os.path.join(art_dir, "Sinners")
    os.makedirs(folder, exist_ok=True)
    for class_id in ORDER:
        path = os.path.join(folder, class_id + ".png")
        PORTRAITS[class_id]().save(path)
        out.append(path)
    strip = os.path.join(art_dir, "Ui", "sinner_icons.png")
    sheet([icon(c) for c in ORDER]).save(strip)
    out.append(strip)
    return out
