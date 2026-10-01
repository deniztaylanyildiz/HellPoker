"""Adds the glyphs Hell Poker needs (♠ ♥ ♦ ♣ ↑ ↓) to the OFL fonts that lack them.

Usage (from the project root):  py Tools/ArtGen/fonts.py
Reads the originals from Tools/ArtGen/fonts and writes Assets/Resources/Fonts.
Needs fonttools:  py -m pip install --user fonttools
"""
import math
import os

from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "fonts")
DST = os.path.abspath(os.path.join(HERE, "..", "..", "Assets", "Resources", "Fonts"))

# Original file -> (output file, new family name). Modified OFL fonts get their own names.
FONTS = {
    "Cinzel-Bold.ttf": ("HellPokerDisplay.ttf", "Hell Poker Display"),
    "IMFellEnglish-Regular.ttf": ("HellPokerSerif.ttf", "Hell Poker Serif"),
    "IMFellEnglish-Italic.ttf": ("HellPokerSerif-Italic.ttf", "Hell Poker Serif Italic"),
}


def circle(cx, cy, r, steps=32):
    # Counter-clockwise in font space (y up) is a TrueType hole, so go clockwise.
    return [(cx + r * math.cos(-2 * math.pi * i / steps), cy + r * math.sin(-2 * math.pi * i / steps)) for i in range(steps)]


def shapes(kind):
    """Contours in a 0..1 box (y up). Overlapping clockwise contours merge under the non-zero fill rule."""
    if kind == "spade":
        return [circle(0.31, 0.44, 0.2), circle(0.69, 0.44, 0.2),
                [(0.11, 0.5), (0.5, 0.96), (0.89, 0.5), (0.5, 0.34)][::-1],
                [(0.5, 0.45), (0.66, 0.04), (0.34, 0.04)][::-1]]
    if kind == "heart":
        return [circle(0.31, 0.64, 0.22), circle(0.69, 0.64, 0.22),
                [(0.1, 0.55), (0.5, 0.06), (0.9, 0.55), (0.5, 0.6)][::-1]]
    if kind == "diamond":
        return [[(0.5, 0.98), (0.86, 0.5), (0.5, 0.02), (0.14, 0.5)]]
    if kind == "club":
        r = 0.19
        return [circle(0.5, 0.72, r), circle(0.28, 0.42, r), circle(0.72, 0.42, r), circle(0.5, 0.5, r * 0.6),
                [(0.5, 0.5), (0.66, 0.04), (0.34, 0.04)][::-1]]
    if kind == "up":
        return [[(0.5, 0.98), (0.88, 0.58), (0.62, 0.58), (0.62, 0.02), (0.38, 0.02), (0.38, 0.58), (0.12, 0.58)]]
    if kind == "down":
        return [[(0.5, 0.02), (0.12, 0.42), (0.38, 0.42), (0.38, 0.98), (0.62, 0.98), (0.62, 0.42), (0.88, 0.42)]]
    raise ValueError(kind)


GLYPHS = {0x2660: "spade", 0x2665: "heart", 0x2666: "diamond", 0x2663: "club", 0x2191: "up", 0x2193: "down"}


def add_glyphs(font):
    upm = font["head"].unitsPerEm
    cap = font["OS/2"].sCapHeight if getattr(font["OS/2"], "sCapHeight", 0) else int(upm * 0.7)
    size = cap * 1.05
    margin = upm * 0.06
    order = font.getGlyphOrder()
    cmap_tables = [t for t in font["cmap"].tables if t.isUnicode()]

    for codepoint, kind in GLYPHS.items():
        name = "hp_" + kind
        pen = TTGlyphPen(None)
        for contour in shapes(kind):
            pts = [(round(margin + x * size), round(y * size)) for x, y in contour]
            # TrueType fills clockwise contours; a counter-clockwise one would punch a hole where shapes overlap.
            area = sum(x0 * y1 - x1 * y0 for (x0, y0), (x1, y1) in zip(pts, pts[1:] + pts[:1]))
            if area > 0:
                pts.reverse()
            pen.moveTo(pts[0])
            for p in pts[1:]:
                pen.lineTo(p)
            pen.closePath()
        glyph = pen.glyph()
        font["glyf"][name] = glyph
        glyph.recalcBounds(font["glyf"])
        font["hmtx"][name] = (round(size + margin * 2), glyph.xMin if hasattr(glyph, "xMin") else 0)
        if name not in order:
            order.append(name)
        for table in cmap_tables:
            if table.format in (4, 12) or table.format == 6:
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
    for source, (target, family) in FONTS.items():
        font = TTFont(os.path.join(SRC, source))
        add_glyphs(font)
        rename(font, family)
        font.save(os.path.join(DST, target))
        print("wrote", target)


if __name__ == "__main__":
    main()
