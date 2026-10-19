"""The sinner classes: a 48x48 portrait for each (shown at x2 on the choice screen) and a 16x16 badge icon strip.

Same palette as everything else (pixel.py). Order of the icon strip = SinnerRoster.PhaseTwo: peasant, warlock, king, jester, summoner.
"""
import os

from pixel import C, Img, RAMP_BONE, RAMP_GOLD, RAMP_LILAC, RAMP_RED, sheet

SIZE = 48
ICON = 16
ORDER = ["peasant", "warlock", "king", "jester", "summoner", "thief"]


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


def oracle():
    """The Oracle (id "warlock", kept for the saves): eyes bound with a cloth, a third eye open and glowing on the brow, an open hood."""
    img = Img(SIZE, SIZE)
    _backdrop(img, C.VIOLET)
    # A lilac robe and an open hood framing the face.
    robe = img.m_poly([(6, 47), (12, 30), (24, 27), (36, 30), (42, 47)])
    img.shade(robe, RAMP_LILAC[:3], light=(-1, -1))
    hood = img.m_ellipse(24, 21, 12, 14) & img.m_rect(0, 0, SIZE, 34)
    img.shade(hood, [C.BLACK, C.VIOLET, C.LILAC], light=(-1, -1))
    _face(img, 24, 22, C.BONE, C.BONE_DARK)
    # The blindfold: a dark band over the eyes, its knot's tails falling to the left.
    img.paint(img.m_rect(16, 21, 32, 23), C.PLUM)
    img.paint(img.m_rect(16, 23, 32, 23), C.BLACK)
    img.paint(img.m_line([(16, 22), (12, 27)], 1), C.PLUM)
    img.paint(img.m_line([(16, 23), (13, 29)], 1), C.PLUM)
    # The third eye on the brow: open, lit, a halo of dithered light around it.
    img.dither(img.m_ellipse(24, 16, 5, 3), C.BONE, C.LILAC_LIGHT, 0.5)
    third = img.m_ellipse(24, 16, 2.6, 1.6)
    img.paint(third, C.WHITE)
    img.put(24, 16, C.VIOLET)
    img.put(23, 16, C.LILAC)
    img.inner_outline(third, C.LILAC)
    img.put(24, 13, C.LILAC_LIGHT)
    # A calm, thin mouth.
    img.paint(img.m_rect(22, 27, 26, 27), C.BONE_SHADE)
    # Runes of fate along the hem.
    for x in range(10, 40, 5):
        img.put(x, 44, C.LILAC_LIGHT)
        img.put(x + 1, 43, C.LILAC_LIGHT)
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


def _imp(img, cx, cy, colour, eye):
    """A small horned imp: a round head, two horns, two lit eyes, a pointed tail curling under it."""
    head = img.m_ellipse(cx, cy, 3.2, 2.8)
    img.paint(head, colour)
    img.put(cx - 2, cy - 3, colour)
    img.put(cx - 3, cy - 4, colour)
    img.put(cx + 2, cy - 3, colour)
    img.put(cx + 3, cy - 4, colour)
    img.put(cx - 1, cy, eye)
    img.put(cx + 1, cy, eye)
    img.paint(img.m_line([(cx, cy + 3), (cx + 2, cy + 5), (cx + 4, cy + 4)], 1), colour)


def summoner():
    img = Img(SIZE, SIZE)
    _backdrop(img, C.HELL)
    # A dark green robe, a summoning circle glowing on the ground, two imps hovering at his shoulders.
    circle = img.m_ellipse(24, 45, 20, 3) & ~img.m_ellipse(24, 45, 17, 1.8)
    img.paint(circle, C.EMBER)
    robe = img.m_poly([(9, 47), (13, 30), (24, 27), (35, 30), (39, 47)])
    img.shade(robe, [C.BLACK, C.GREEN_DARK, C.GREEN], light=(-1, -1))
    for x in range(14, 36, 6):
        img.put(x, 41, C.EMBER)
        img.put(x + 1, 40, C.AMBER)
    _face(img, 24, 21, C.BONE_MID, C.BONE_SHADE)
    # A shaved head with a burned sigil, a thin mouth.
    img.put(24, 15, C.HELL)
    img.put(23, 16, C.HELL)
    img.put(25, 16, C.HELL)
    img.paint(img.m_rect(22, 26, 26, 26), C.BONE_SHADE)
    # His raised hands, a flame between them.
    img.paint(img.m_ellipse(13, 33, 2.4, 2.4), C.BONE_MID)
    img.paint(img.m_ellipse(35, 33, 2.4, 2.4), C.BONE_MID)
    img.paint(img.m_ellipse(24, 37, 2.2, 3.2), C.AMBER)
    img.put(24, 36, C.EMBER)
    _imp(img, 8, 14, C.RED, C.AMBER)
    _imp(img, 40, 12, C.CRIMSON, C.GOLD_LIGHT)
    img.outline(C.BLACK, mask=img.px != C.NIGHT)
    return img

def thief():
    """The Thief: a dark hood and a mask over the eyes, a sly smile, a stolen card between two fingers."""
    img = Img(SIZE, SIZE)
    _backdrop(img, C.DUSK)
    # A dark cloak, a belt with a little purse.
    cloak = img.m_poly([(7, 47), (12, 31), (24, 28), (36, 31), (41, 47)])
    img.shade(cloak, [C.BLACK, C.NIGHT, C.DUSK], light=(-1, -1))
    img.paint(img.m_rect(13, 41, 35, 42), C.BONE_SHADE)
    img.paint(img.m_ellipse(30, 44, 2.5, 2), C.GOLD_MID)
    img.put(30, 43, C.GOLD_LIGHT)
    # The hood over the head.
    hood = img.m_ellipse(24, 21, 11, 13) & img.m_rect(0, 0, SIZE, 33)
    img.shade(hood, [C.BLACK, C.NIGHT, C.PLUM], light=(-1, -1))
    _face(img, 24, 23, C.BONE_MID, C.BONE_SHADE)
    # The mask: a black band with two eye holes, a glint in each.
    img.paint(img.m_rect(16, 20, 32, 24), C.BLACK)
    for ex in (20, 28):
        img.paint(img.m_rect(ex - 1, 21, ex + 1, 23), C.BONE_MID)
        img.put(ex, 22, C.BLACK)
        img.put(ex + 1, 21, C.WHITE)
    # A crooked grin.
    img.paint(img.m_line([(21, 28), (27, 27)], 1), C.BONE_SHADE)
    img.put(27, 26, C.BONE_SHADE)
    # A hand raised at the shoulder, a card between its fingers.
    img.paint(img.m_ellipse(38, 34, 2.4, 2.4), C.BONE_MID)
    card = img.m_rect(37, 23, 43, 32)
    img.paint(card, C.BONE)
    img.inner_outline(card, C.BONE_SHADE)
    img.put(40, 27, C.RED)
    img.put(39, 26, C.RED)
    img.put(41, 26, C.RED)
    img.put(40, 28, C.RED)
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
        # The Oracle: a blindfold band, the third eye open above it.
        img.paint(img.m_rect(1, 10, 14, 12), C.PLUM)
        img.paint(img.m_rect(1, 12, 14, 12), C.VIOLET)
        eye = img.m_ellipse(8, 5, 5.5, 3)
        img.paint(eye, C.WHITE)
        img.paint(img.m_ellipse(8, 5, 2, 2), C.VIOLET)
        img.put(8, 5, C.BLACK)
        img.put(7, 4, C.LILAC_LIGHT)
        img.inner_outline(eye, C.LILAC)
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
    elif class_id == "summoner":
        # A summoning circle with an imp rising out of it.
        ring = img.m_ellipse(8, 12, 7, 3) & ~img.m_ellipse(8, 12, 5, 1.6)
        img.paint(ring, C.EMBER)
        _imp(img, 8, 6, C.RED, C.AMBER)
    elif class_id == "thief":
        # A mask with two eye holes over a card held aslant.
        card = img.m_poly([(9, 5), (15, 7), (12, 15), (6, 13)])
        img.paint(card, C.BONE)
        img.put(11, 10, C.RED)
        img.put(10, 10, C.RED)
        img.paint(img.m_rect(1, 3, 10, 6), C.BLACK)
        img.paint(img.m_rect(2, 4, 3, 5), C.BONE_MID)
        img.paint(img.m_rect(7, 4, 8, 5), C.BONE_MID)
    img.outline(C.BLACK)
    return img


PASSIVE = 12


def passive_icon(class_id):
    """Demo 3.1: the passive's 12x12 icon on the player's panel (the Jester's: a joker card)."""
    img = Img(PASSIVE, PASSIVE)
    if class_id == "peasant":
        # SILENT FIELD: a closed eye over the furrows.
        img.paint(img.m_rect(0, 9, 11, 11), C.GREEN_DARK)
        for x in range(1, 11, 3):
            img.put(x, 9, C.GREEN)
        img.paint(img.m_line([(2, 4), (5, 6), (8, 6), (10, 4)], 1), C.BONE)
        for x in (3, 6, 9):
            img.put(x, 7, C.BONE_DARK)
    elif class_id == "warlock":
        # SEES THE TRUTH: an open eye, a violet iris.
        eye = img.m_ellipse(6, 6, 5.5, 3)
        img.paint(eye, C.WHITE)
        img.paint(img.m_ellipse(6, 6, 2, 2), C.VIOLET)
        img.put(6, 6, C.BLACK)
        img.put(5, 5, C.LILAC_LIGHT)
        img.inner_outline(eye, C.LILAC)
    elif class_id == "king":
        # ROYAL DISCOUNT: a gold coin cut in half.
        coin = img.m_ellipse(6, 6, 5, 5)
        img.paint(coin, C.GOLD)
        img.inner_outline(coin, C.GOLD_MID)
        img.put(5, 5, C.GOLD_LIGHT)
        img.put(6, 4, C.GOLD_LIGHT)
        img.paint(img.m_line([(9, 1), (3, 11)], 1), C.BLACK)
    elif class_id == "jester":
        # His jokers: a joker card, a fool's cap on it.
        card = img.m_rect(2, 0, 9, 11)
        img.paint(card, C.BONE)
        img.inner_outline(card, C.BONE_SHADE)
        img.put(4, 4, C.CRIMSON)
        img.put(5, 3, C.GOLD)
        img.put(6, 3, C.GOLD)
        img.put(7, 4, C.VIOLET)
        img.paint(img.m_rect(4, 5, 7, 5), C.GOLD_MID)
        img.put(5, 7, C.BLACK)
        img.put(6, 7, C.BLACK)
    elif class_id == "summoner":
        # LOYAL IMP: a small imp's head.
        _imp(img, 6, 6, C.RED, C.AMBER)
    elif class_id == "thief":
        # GLANCE: a closed card, one corner lifted, a glint under it.
        card = img.m_rect(1, 2, 8, 11)
        img.paint(card, C.CRIMSON)
        img.inner_outline(card, C.BLOOD_DARK)
        img.paint(img.m_poly([(8, 2), (11, 1), (9, 6)]), C.BONE)
        img.put(10, 7, C.WHITE)
        img.put(11, 8, C.GOLD_LIGHT)
    img.outline(C.BLACK)
    return img


PORTRAITS = {"peasant": peasant, "warlock": oracle, "king": king, "jester": jester, "summoner": summoner, "thief": thief}


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
    passives = os.path.join(art_dir, "Ui", "passive_icons.png")
    sheet([passive_icon(c) for c in ORDER]).save(passives)
    out.append(passives)
    return out
