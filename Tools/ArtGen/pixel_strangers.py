"""Demo 2.1's strangers (Art/Events/<id>.png, 48x48): the helpers, the swindlers and Hell's own weather.

Look-alikes share a base: a robed figure (hooded or bare-headed) for the people, an imp for the imps, a booth for the sellers; each gets
its own colours and the thing it holds out. One palette (pixel.py), no anti-aliasing.
"""

import math
import os

from pixel import C, Img, RAMP_BONE, RAMP_FIRE, RAMP_GOLD, RAMP_GREEN, RAMP_LILAC, RAMP_NIGHT, RAMP_RED, RAMP_SILVER

RAMP_ASH = [C.BLACK, C.BONE_SHADE, C.SILVER_DARK, C.SILVER]
RAMP_RUST = [C.BLOOD_DARK, C.BLOOD, C.GOLD_DARK, C.GOLD_MID]


def backdrop(img, glow):
    img.paint(img.m_rect(0, 0, 47, 47), C.BLACK)
    img.dither(img.m_ellipse(24, 30, 26, 22), C.BLACK, glow, 0.3)


def robe(img, ramp, top=24, wide=13):
    body = img.m_poly([(24 - wide, 47), (24 - wide + 5, top + 4), (24, top), (24 + wide - 5, top + 4), (24 + wide, 47)])
    img.shade(body, ramp, light=(-1, -1))
    return body


def hood(img, ramp, eyes, cy=18):
    hood_mask = img.m_ellipse(24, cy, 9, 9.5)
    img.shade(hood_mask, ramp, light=(-1, -1))
    img.paint(img.m_ellipse(24, cy + 2, 5.5, 6), C.BLACK)
    img.put(22, cy + 2, eyes)
    img.put(26, cy + 2, eyes)


def face(img, skin, cy=17, eyes=C.BLACK, mouth=C.BLOOD_DARK):
    head = img.m_ellipse(24, cy, 6, 7.5)
    img.shade(head, skin, light=(-1, -1))
    img.put(22, cy - 1, eyes)
    img.put(26, cy - 1, eyes)
    img.paint(img.m_rect(22, cy + 3, 26, cy + 3), mouth)


def imp_head(img, skin, cy=20, eyes=C.EMBER):
    head = img.m_ellipse(24, cy, 9, 8)
    img.shade(head, skin, light=(-1, -1))
    for side in (-1, 1):
        img.paint(img.m_poly([(24 + side * 6, cy - 5), (24 + side * 11, cy - 13), (24 + side * 9, cy - 4)]), C.BONE_DARK)
        img.paint(img.m_ellipse(24 + side * 3.5, cy - 1, 2, 2), C.BLACK)
        img.put(24 + side * 3 + (1 if side < 0 else 0), cy - 1, eyes)
    img.paint(img.m_rect(20, cy + 4, 28, cy + 4), C.BLACK)
    img.put(21, cy + 5, C.BONE)
    img.put(27, cy + 5, C.BONE)


def coin(img, x, y):
    img.paint(img.m_ellipse(x, y, 2.2, 2.2), C.GOLD)
    img.put(int(x) - 1, int(y) - 1, C.GOLD_LIGHT)


def finish(img):
    img.outline(C.BLACK, mask=img.px != C.BLACK)
    return img


# ------------------------------------------------------------------ help

def chained_soul():
    """A pale soul on its knees, a heavy chain from its neck to an iron ring."""
    img = Img(48, 48)
    backdrop(img, C.SILVER_DARK)
    robe(img, RAMP_ASH, top=26, wide=12)
    face(img, RAMP_BONE, cy=19, eyes=C.SILVER_DARK, mouth=C.BONE_SHADE)
    for k in range(7):
        x, y = 20 - k * 2, 26 + k * 2
        img.paint(img.m_ellipse(x, y, 1.6, 1.2), C.SILVER_DARK if k % 2 else C.SILVER)
    img.paint(img.m_ellipse(7, 42, 4, 4) & ~img.m_ellipse(7, 42, 2, 2), C.SILVER_DARK)
    return finish(img)


def fallen_angel():
    """A bare-headed figure in white with one broken, grey wing and a fading halo."""
    img = Img(48, 48)
    backdrop(img, C.GOLD_DARK)
    wing = img.m_poly([(16, 26), (3, 14), (2, 30), (12, 36)])
    img.shade(wing, RAMP_BONE, light=(1, -1))
    broken = img.m_poly([(32, 26), (40, 22), (44, 34), (36, 38)])
    img.shade(broken, RAMP_ASH, light=(1, -1))
    robe(img, RAMP_SILVER, top=25, wide=11)
    face(img, RAMP_BONE, cy=17, eyes=C.SILVER_DARK, mouth=C.BONE_SHADE)
    img.paint(img.m_ellipse(24, 7, 7, 2) & ~img.m_ellipse(24, 7, 5, 1), C.GOLD_MID)
    return finish(img)


def hungry_gambler():
    """A thin gambler in a green visor, hollow cheeks, a single card held up."""
    img = Img(48, 48)
    backdrop(img, C.GREEN_DARK)
    robe(img, RAMP_NIGHT, top=25, wide=10)
    face(img, RAMP_BONE, cy=18, eyes=C.BLACK, mouth=C.BONE_SHADE)
    img.paint(img.m_rect(17, 11, 31, 13), C.GREEN)
    img.paint(img.m_rect(15, 13, 33, 14), C.GREEN_LIGHT)
    card = img.m_rect(33, 26, 40, 36)
    img.paint(card, C.BONE)
    img.paint(img.m_rect(35, 29, 37, 31), C.CRIMSON)
    return finish(img)


def debtor_imp():
    """A small imp clutching an IOU, sweat on his brow."""
    img = Img(48, 48)
    backdrop(img, C.BLOOD_DARK)
    body = img.m_ellipse(24, 38, 10, 9)
    img.shade(body, RAMP_RED, light=(-1, -1))
    imp_head(img, RAMP_RED, cy=22, eyes=C.AMBER)
    note = img.m_rect(30, 32, 40, 40)
    img.paint(note, C.BONE)
    for y in (34, 36, 38):
        img.paint(img.m_rect(32, y, 38, y), C.BONE_SHADE)
    img.put(14, 15, C.SILVER)
    img.put(14, 16, C.SILVER)
    return finish(img)


def lost_soul():
    """A see-through soul in a hood, a lantern gone out, looking the wrong way."""
    img = Img(48, 48)
    backdrop(img, C.DUSK)
    body = robe(img, RAMP_LILAC, top=25, wide=11)
    img.dither(body, C.LILAC, C.LILAC_LIGHT, 0.3)
    hood(img, RAMP_LILAC, C.WHITE)
    lamp = img.m_rect(35, 30, 40, 37)
    img.paint(lamp, C.SILVER_DARK)
    img.inner_outline(lamp, C.BONE_SHADE)
    img.paint(img.m_line([(37, 26), (37, 30)], 1), C.SILVER_DARK)
    return finish(img)


def mammons_accountant():
    """A bent clerk in green with spectacles, a ledger and a quill."""
    img = Img(48, 48)
    backdrop(img, C.GREEN_DARK)
    robe(img, RAMP_GREEN[:3], top=25, wide=12)
    face(img, RAMP_BONE, cy=17, eyes=C.BLACK, mouth=C.BONE_SHADE)
    for x in (22, 26):
        img.paint(img.m_ellipse(x, 16, 1.8, 1.8) & ~img.m_ellipse(x, 16, 0.9, 0.9), C.GOLD)
    book = img.m_rect(13, 30, 26, 40)
    img.paint(book, C.BLOOD)
    img.paint(img.m_rect(15, 31, 25, 39), C.BONE)
    img.paint(img.m_rect(20, 31, 20, 39), C.BONE_SHADE)
    img.paint(img.m_line([(30, 26), (36, 38)], 1), C.BONE_MID)
    return finish(img)


# ------------------------------------------------------------------ swindlers

def booth(img, ramp):
    img.paint(img.m_rect(4, 36, 43, 46), ramp[1])
    img.paint(img.m_rect(4, 36, 43, 37), ramp[2])


def three_cups():
    """A smiling juggler behind a plank, three cups upside down."""
    img = Img(48, 48)
    backdrop(img, C.GOLD_DARK)
    robe(img, RAMP_RUST, top=22, wide=12)
    face(img, RAMP_BONE, cy=15, eyes=C.BLACK, mouth=C.CRIMSON)
    booth(img, RAMP_RUST)
    for x in (12, 24, 36):
        cup = img.m_poly([(x - 4, 36), (x - 3, 28), (x + 3, 28), (x + 4, 36)])
        img.shade(cup, RAMP_GOLD, light=(-1, -1))
    img.put(24, 38, C.BONE)
    return finish(img)


def fake_pardon():
    """A pardoner in a faded red robe, a scroll with a wet red seal."""
    img = Img(48, 48)
    backdrop(img, C.BLOOD_DARK)
    robe(img, RAMP_RED[:4], top=24, wide=12)
    face(img, RAMP_BONE, cy=16, eyes=C.BLACK, mouth=C.BLOOD)
    img.paint(img.m_rect(18, 7, 30, 9), C.GOLD_MID)
    scroll = img.m_rect(14, 28, 34, 38)
    img.paint(scroll, C.BONE)
    img.paint(img.m_ellipse(30, 36, 3, 3), C.CRIMSON)
    img.put(30, 40, C.CRIMSON)
    for y in (30, 32, 34):
        img.paint(img.m_rect(16, y, 26, y), C.BONE_SHADE)
    return finish(img)


def soul_insurance():
    """A neat insurer in grey, a top hat, a policy held out — its signature smoking."""
    img = Img(48, 48)
    backdrop(img, C.SILVER_DARK)
    robe(img, RAMP_SILVER[:3], top=24, wide=12)
    face(img, RAMP_BONE, cy=17, eyes=C.BLACK, mouth=C.BONE_SHADE)
    img.paint(img.m_rect(17, 9, 31, 10), C.NIGHT)
    img.paint(img.m_rect(19, 2, 29, 9), C.NIGHT)
    paper = img.m_rect(29, 28, 41, 40)
    img.paint(paper, C.BONE)
    img.paint(img.m_line([(31, 37), (39, 36)], 1), C.BONE_SHADE)
    img.dither(img.m_ellipse(36, 31, 4, 3), C.CLEAR, C.SILVER_DARK, 0.5)
    return finish(img)


def pickpocket():
    """A hooded thief running, a stolen purse swinging from his hand."""
    img = Img(48, 48)
    backdrop(img, C.NIGHT)
    body = img.m_poly([(10, 47), (16, 26), (26, 22), (34, 30), (30, 47)])
    img.shade(body, RAMP_NIGHT[1:], light=(-1, -1))
    hood(img, RAMP_NIGHT[1:] + [C.VIOLET], C.AMBER, cy=17)
    purse = img.m_ellipse(38, 36, 4, 4)
    img.shade(purse, RAMP_GREEN[:3], light=(-1, -1))
    img.paint(img.m_line([(34, 30), (37, 33)], 1), C.BONE_DARK)
    coin(img, 42, 43)
    return finish(img)


# ------------------------------------------------------------------ Hell's own

def lava_ferryman():
    """A skeletal ferryman in a boat on lava, his pole black against the glow."""
    img = Img(48, 48)
    backdrop(img, C.HELL)
    for x in range(48):
        h = 6 + int(2 * math.sin(x * 0.5))
        for k in range(h):
            img.put(x, 47 - k, RAMP_FIRE[min(len(RAMP_FIRE) - 1, k)])
    boat = img.m_poly([(6, 38), (42, 38), (36, 44), (12, 44)])
    img.shade(boat, [C.BLACK, C.BLOOD_DARK, C.BLOOD], light=(-1, -1))
    robe(img, RAMP_NIGHT, top=20, wide=7)
    skull = img.m_ellipse(24, 15, 5, 6)
    img.paint(skull, C.BONE_MID)
    img.put(22, 15, C.BLACK)
    img.put(26, 15, C.BLACK)
    img.paint(img.m_line([(34, 4), (30, 44)], 1), C.BLACK)
    return finish(img)


def hell_lottery():
    """A ticket booth with a grinning seller and a spinning wheel of bones."""
    img = Img(48, 48)
    backdrop(img, C.PLUM)
    wheel = img.m_ellipse(14, 22, 9, 9)
    img.shade(wheel, RAMP_GOLD, light=(-1, -1))
    for a in range(6):
        ang = a * math.pi / 3
        img.paint(img.m_line([(14, 22), (14 + 8 * math.cos(ang), 22 + 8 * math.sin(ang))], 1), C.BLOOD)
    robe(img, RAMP_LILAC[:3], top=24, wide=8)
    face(img, RAMP_BONE, cy=17, eyes=C.BLACK, mouth=C.CRIMSON)
    booth(img, RAMP_NIGHT)
    ticket = img.m_rect(33, 30, 41, 35)
    img.paint(ticket, C.GOLD_LIGHT)
    img.put(35, 32, C.BLOOD)
    return finish(img)


def confession_cell():
    """A grille in a stone wall; behind it, two red eyes listening."""
    img = Img(48, 48)
    backdrop(img, C.BLOOD_DARK)
    wall = img.m_rect(2, 2, 45, 45)
    img.shade(wall, [C.NIGHT, C.DUSK, C.BONE_SHADE], light=(-1, -1))
    window = img.m_rect(12, 12, 35, 35)
    img.paint(window, C.BLACK)
    for x in range(12, 36, 4):
        img.paint(img.m_rect(x, 12, x, 35), C.SILVER_DARK)
    for y in range(12, 36, 6):
        img.paint(img.m_rect(12, y, 35, y), C.SILVER_DARK)
    img.put(20, 22, C.HELL)
    img.put(27, 22, C.HELL)
    return finish(img)


def fire_rain():
    """Drops of fire falling from a dark vault onto stone."""
    img = Img(48, 48)
    backdrop(img, C.BLOOD)
    img.paint(img.m_rect(0, 0, 47, 6), C.BLACK)
    for k, (x, y) in enumerate(((6, 10), (16, 18), (28, 8), (38, 16), (11, 30), (24, 26), (34, 34), (42, 26), (20, 40))):
        drop = img.m_poly([(x, y - 4), (x + 2, y + 1), (x, y + 3), (x - 2, y + 1)])
        img.paint(drop, RAMP_FIRE[2 + k % 3])
        img.put(x, y + 1, C.EMBER)
    img.paint(img.m_rect(0, 44, 47, 47), C.BONE_SHADE)
    return finish(img)


def sin_swap():
    """A trader with a coat lined with jars, each holding a little glowing sin."""
    img = Img(48, 48)
    backdrop(img, C.VIOLET)
    coat = robe(img, RAMP_NIGHT, top=22, wide=15)
    face(img, [C.GREEN_DARK, C.GREEN, C.GREEN_LIGHT], cy=15, eyes=C.AMBER, mouth=C.BLACK)
    for x, y, glow in ((14, 30, C.HELL), (19, 38, C.LILAC), (29, 32, C.GREEN_LIGHT), (33, 40, C.GOLD), (24, 42, C.EMBER)):
        jar = img.m_rect(x, y, x + 3, y + 4)
        img.paint(jar, C.SILVER_DARK)
        img.paint(img.m_rect(x + 1, y + 1, x + 2, y + 3), glow)
    return finish(img)


def blind_seer():
    """An old seer with a bandage over her eyes, a third eye painted on it, a crystal of smoke."""
    img = Img(48, 48)
    backdrop(img, C.MAUVE)
    hood_mask = img.m_ellipse(24, 18, 10, 10)
    img.shade(hood_mask, RAMP_LILAC, light=(-1, -1))
    robe(img, RAMP_LILAC, top=25, wide=12)
    face(img, RAMP_BONE, cy=19, eyes=C.BONE_DARK, mouth=C.BONE_SHADE)
    img.paint(img.m_rect(17, 16, 31, 18), C.BONE)
    img.paint(img.m_ellipse(24, 17, 1.5, 1), C.VIOLET)
    ball = img.m_ellipse(24, 38, 5, 5)
    img.shade(ball, RAMP_SILVER, light=(-1, -1))
    img.dither(ball, C.SILVER, C.LILAC_LIGHT, 0.4)
    return finish(img)


STRANGERS = {
    "chained_soul": chained_soul, "fallen_angel": fallen_angel, "hungry_gambler": hungry_gambler, "debtor_imp": debtor_imp,
    "lost_soul": lost_soul, "mammons_accountant": mammons_accountant, "three_cups": three_cups, "fake_pardon": fake_pardon,
    "soul_insurance": soul_insurance, "pickpocket": pickpocket, "lava_ferryman": lava_ferryman, "hell_lottery": hell_lottery,
    "confession_cell": confession_cell, "fire_rain": fire_rain, "sin_swap": sin_swap, "blind_seer": blind_seer,
}



# ------------------------------------------------------------------ the black market's vendors (Demo 2.1), each on his own colour

def market_smuggler():
    """A hooded smuggler with a scarf over his face, crates behind him, a lantern."""
    img = Img(48, 48)
    backdrop(img, C.BLOOD_DARK)
    for x, y in ((3, 32), (36, 34)):
        crate = img.m_rect(x, y, x + 9, y + 9)
        img.shade(crate, RAMP_RUST, light=(-1, -1))
        img.paint(img.m_line([(x, y), (x + 9, y + 9)], 1), C.BLOOD_DARK)
    robe(img, RAMP_NIGHT, top=24, wide=12)
    hood(img, RAMP_NIGHT[1:] + [C.VIOLET], C.EMBER, cy=17)
    img.paint(img.m_rect(19, 21, 29, 23), C.BLOOD)
    return finish(img)


def market_pawnshop():
    """Mammon's pawnbroker: a fat green-coated clerk behind a grille, coins and a pawned crown."""
    img = Img(48, 48)
    backdrop(img, C.GREEN_DARK)
    robe(img, RAMP_GREEN[:3], top=24, wide=15)
    face(img, RAMP_BONE, cy=16, eyes=C.GOLD, mouth=C.BLOOD)
    img.paint(img.m_rect(16, 7, 32, 9), C.GOLD_MID)
    for x in range(6, 44, 6):
        img.paint(img.m_rect(x, 26, x, 47), C.GOLD_DARK)
    img.paint(img.m_rect(4, 26, 43, 26), C.GOLD_DARK)
    for x, y in ((10, 41), (14, 43), (36, 42)):
        coin(img, x, y)
    return finish(img)


def market_bonemerchant():
    """The bone merchant: a skull-faced trader in a coat of ribs, holding up a femur like a price tag."""
    img = Img(48, 48)
    backdrop(img, C.SILVER_DARK)
    robe(img, RAMP_ASH, top=24, wide=13)
    for y in range(28, 46, 4):
        img.paint(img.m_rect(17, y, 31, y), C.BONE_MID)
    skull = img.m_ellipse(24, 16, 6, 7)
    img.shade(skull, RAMP_BONE, light=(-1, -1))
    img.paint(img.m_ellipse(21.5, 15, 1.6, 1.8), C.BLACK)
    img.paint(img.m_ellipse(26.5, 15, 1.6, 1.8), C.BLACK)
    img.put(21, 15, C.EMBER)
    img.paint(img.m_rect(21, 20, 27, 21), C.BONE_SHADE)
    img.paint(img.m_line([(35, 18), (42, 34)], 2), C.BONE)
    return finish(img)


def market_alchemist():
    """A wandering alchemist with a pointed hat, a bubbling flask in one hand."""
    img = Img(48, 48)
    backdrop(img, C.VIOLET)
    robe(img, RAMP_LILAC[:3], top=24, wide=12)
    face(img, RAMP_BONE, cy=18, eyes=C.BLACK, mouth=C.BONE_SHADE)
    img.paint(img.m_poly([(15, 13), (24, 0), (33, 13)]), C.VIOLET)
    img.paint(img.m_rect(14, 12, 34, 13), C.MAUVE)
    flask = img.m_ellipse(37, 37, 5, 5) | img.m_rect(36, 28, 38, 33)
    img.paint(flask, C.SILVER_DARK)
    img.paint(img.m_ellipse(37, 38, 3.6, 3.4), C.GREEN_LIGHT)
    img.put(36, 25, C.GREEN_LIGHT)
    img.put(38, 23, C.GREEN)
    return finish(img)


def market_gamblersstall():
    """A grinning stallkeeper in a striped vest behind a stack of closed boxes."""
    img = Img(48, 48)
    backdrop(img, C.PLUM)
    robe(img, RAMP_RED[:4], top=22, wide=12)
    for x in range(16, 33, 4):
        img.paint(img.m_rect(x, 26, x + 1, 36), C.GOLD_MID)
    face(img, RAMP_BONE, cy=15, eyes=C.BLACK, mouth=C.CRIMSON)
    img.paint(img.m_rect(20, 18, 28, 18), C.CRIMSON)
    for x, y, c in ((6, 36, C.GOLD_DARK), (16, 38, C.BLOOD), (28, 37, C.VIOLET), (36, 39, C.GOLD_DARK)):
        box = img.m_rect(x, y, x + 8, y + 8)
        img.paint(box, c)
        img.paint(img.m_rect(x + 3, y, x + 5, y + 8), C.GOLD)
    return finish(img)


def market_tailor():
    """The devil's tailor: horned, a tape round his neck, scissors and a needle of fire."""
    img = Img(48, 48)
    backdrop(img, C.BLOOD)
    robe(img, RAMP_NIGHT, top=24, wide=12)
    face(img, [C.BLOOD_DARK, C.BLOOD, C.CRIMSON], cy=17, eyes=C.AMBER, mouth=C.BLACK)
    for side in (-1, 1):
        img.paint(img.m_poly([(24 + side * 4, 11), (24 + side * 8, 3), (24 + side * 6, 12)]), C.BONE_DARK)
    img.paint(img.m_line([(16, 25), (24, 30), (32, 25)], 1), C.GOLD)
    for k in range(3):
        img.put(18 + k * 6, 27, C.BLACK)
    img.paint(img.m_line([(33, 30), (41, 40)], 1), C.SILVER)
    img.paint(img.m_line([(41, 30), (33, 40)], 1), C.SILVER)
    img.paint(img.m_line([(8, 30), (8, 42)], 1), C.EMBER)
    return finish(img)


STRANGERS.update({
    "market_smuggler": market_smuggler, "market_pawnshop": market_pawnshop, "market_bonemerchant": market_bonemerchant,
    "market_alchemist": market_alchemist, "market_gamblersstall": market_gamblersstall, "market_tailor": market_tailor,
})

def write_all(art_dir):
    events = os.path.join(art_dir, "Events")
    os.makedirs(events, exist_ok=True)
    written = []
    for name, draw in STRANGERS.items():
        path = os.path.join(events, name + ".png")
        draw().save(path)
        written.append(path)
    return written
