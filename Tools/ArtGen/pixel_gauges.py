"""Demo 3.5.1: the gauges under the demon's name.

Ui/seat_gauges.png (2 x 8x8): a coin (the imp's purse) and a chip (the ante). Ui/seat_rage.png (16x16): the rage's flame in a horned
outline, beside the demon's rule icons. One palette, whole pixels, no anti-aliasing.
"""
import os

from pixel import C, Img, sheet


def coin():
    img = Img(8, 8)
    img.paint(img.m_ellipse(4, 4, 3.6, 3.6), C.GOLD)
    img.paint(img.m_ellipse(4, 4, 3.6, 3.6) & ~img.m_ellipse(4, 4, 2.6, 2.6), C.GOLD_MID)
    img.put(3, 2, C.GOLD_LIGHT)
    img.put(4, 4, C.GOLD_DARK)
    return img


def chip():
    img = Img(8, 8)
    img.paint(img.m_ellipse(4, 4, 3.6, 3.6), C.CRIMSON)
    for x, y in ((4, 0), (4, 7), (0, 4), (7, 4)):
        img.put(x, y, C.BONE)
    img.paint(img.m_ellipse(4, 4, 1.8, 1.8), C.BONE_MID)
    img.put(4, 4, C.RED)
    return img


def rage():
    img = Img(16, 16)
    img.paint(img.m_poly([(2, 15), (8, 1), (14, 15)]), C.RED)
    img.paint(img.m_poly([(4, 15), (8, 5), (12, 15)]), C.HELL)
    img.paint(img.m_poly([(6, 15), (8, 9), (10, 15)]), C.AMBER)
    for side in (-1, 1):
        img.paint(img.m_poly([(8 + side * 4, 6), (8 + side * 7, 0), (8 + side * 6, 7)]), C.BONE_DARK)
    img.outline(C.BLACK)
    return img


def write_all(art_dir):
    ui = os.path.join(art_dir, "Ui")
    paths = [os.path.join(ui, "seat_gauges.png"), os.path.join(ui, "seat_rage.png")]
    sheet([coin(), chip()]).save(paths[0])
    rage().save(paths[1])
    return paths
