"""Builds Hell Poker's pixel fonts: adds the glyphs they lack, drawn as pixel bitmaps on each font's own grid.

Usage (from the project root):  py Tools/ArtGen/fonts.py
Reads the originals from Tools/ArtGen/fonts and writes Assets/Resources/Fonts.
Needs fonttools:  py -m pip install --user fonttools

Both fonts are drawn on an 8 px em: use them in Unity at font size 8 (or 16, 24...) so every font pixel is a whole screen pixel.
"""
import os

from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "fonts")
DST = os.path.abspath(os.path.join(HERE, "..", "..", "Assets", "Resources", "Fonts"))

# 5 px tall suit bitmaps for the small body font (Tiny5: cap height 5 px).
SUITS_5 = {
    0x2660: ["..#..", ".###.", "#####", "..#..", ".###."],   # spade: pointed top, stem on a foot
    0x2663: [".###.", ".###.", "#####", "#.#.#", "..#.."],   # club: round top, three lobes
    0x2665: ["##.##", "#####", "#####", ".###.", "..#.."],   # heart
    0x2666: ["..#..", ".###.", "#####", ".###.", "..#.."],   # diamond
}

# Turkish capital dotted I for the title font (Press Start 2P): the original squeezes the I to 4 rows to fit the dot inside the
# 8 px em, and in big titles it reads as a small "i" ("LANETLENDİN"). This one is the full 7 px I, and its dot sits one empty
# pixel above it — above the em: the line height stays the same (no layout moves), the dot rides over the line box.
# Rows top to bottom: dot, gap, the I (bar, 5 stem rows, bar), the empty baseline row. 7 columns: advance 8 px like the I.
TITLE_DOTTED_I = {
    0x0130: [
        "...##..",
        ".......",
        ".######",
        "...##..",
        "...##..",
        "...##..",
        "...##..",
        "...##..",
        ".######",
        ".......",
    ],
}

# Original file -> (output file, new family name, glyphs to add as bitmaps, codepoints to alias to an existing character).
FONTS = {
    "PressStart2P-Regular.ttf": ("HellPokerPixelTitle.ttf", "Hell Poker Pixel Title", TITLE_DOTTED_I, {0x2212: "-"}),
    "Tiny5-Regular.ttf": ("HellPokerPixel.ttf", "Hell Poker Pixel", SUITS_5, {}),
}


def pixel_unit(font):
    """Font units per design pixel: these fonts are drawn on an 8 px em."""
    return font["head"].unitsPerEm // 8


def bitmap_glyph(rows, unit):
    pen = TTGlyphPen(None)
    height = len(rows)
    for r, row in enumerate(rows):
        for c, ch in enumerate(row):
            if ch != "#":
                continue
            x0, x1 = c * unit, (c + 1) * unit
            y1 = (height - r) * unit
            y0 = y1 - unit
            # Clockwise square (TrueType fills clockwise contours).
            pen.moveTo((x0, y0))
            pen.lineTo((x0, y1))
            pen.lineTo((x1, y1))
            pen.lineTo((x1, y0))
            pen.closePath()
    return pen.glyph()


def add_glyphs(font, bitmaps, aliases):
    unit = pixel_unit(font)
    order = font.getGlyphOrder()
    cmap_tables = [t for t in font["cmap"].tables if t.isUnicode()]
    best = font.getBestCmap()

    for codepoint, rows in bitmaps.items():
        name = "hp_uni%04X" % codepoint
        glyph = bitmap_glyph(rows, unit)
        font["glyf"][name] = glyph
        glyph.recalcBounds(font["glyf"])
        font["hmtx"][name] = ((len(rows[0]) + 1) * unit, getattr(glyph, "xMin", 0))
        if name not in order:
            order.append(name)
        for table in cmap_tables:
            table.cmap[codepoint] = name

    for codepoint, existing in aliases.items():
        name = best[ord(existing)]
        for table in cmap_tables:
            table.cmap[codepoint] = name

    font.setGlyphOrder(order)
    font["maxp"].numGlyphs = len(order)
    for tag in ("hdmx", "LTSH", "VDMX", "DSIG"):
        if tag in font:
            del font[tag]


def rename(font, family):
    for record in font["name"].names:
        if record.nameID in (1, 4, 16, 18):
            record.string = family
        elif record.nameID == 6:
            record.string = family.replace(" ", "")


def main():
    os.makedirs(DST, exist_ok=True)
    for source, (target, family, bitmaps, aliases) in FONTS.items():
        font = TTFont(os.path.join(SRC, source))
        add_glyphs(font, bitmaps, aliases)
        rename(font, family)
        font.save(os.path.join(DST, target))
        print("wrote", target)


if __name__ == "__main__":
    main()
