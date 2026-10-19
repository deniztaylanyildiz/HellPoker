"""The sinner classes: a 48x48 portrait for each (shown at x2 on the choice screen), a 16x16 badge icon strip, the passives' 12x12 icons
and (Demo 3.1) the animated 64x64 portraits of a chapter table's player panel.

Same palette as everything else (pixel.py). Order of the strips = SinnerRoster.PhaseTwo: peasant, warlock, king, jester, summoner, thief.

The portraits are drawn on a design grid of 48: on a `Canvas` of 48 they are the class card's pictures exactly; on a canvas of 64 the same
shapes are rasterised again at 64 (not scaled up: every edge is drawn at the new size, one pixel is one pixel). A pose (`REST` for the
class card) moves the figure by whole output pixels and changes the face: breath, a blink, the class's own gesture, a smile, joy, a
wince, tension. Animated strips: Sinners/<id>/<state>.png (idle, happy, joy, hurt, tense).
"""
import math
import os

from pixel import C, Img, RAMP_BONE, RAMP_GOLD, RAMP_LILAC, RAMP_RED, sheet

SIZE = 48
PANEL = 64
ICON = 16
ORDER = ["peasant", "warlock", "king", "jester", "summoner", "thief"]


class Canvas(Img):
    """An image drawn on the 48 design grid: coordinates are scaled to its own size and moved by an offset of whole output pixels."""

    def __init__(self, size=SIZE):
        super().__init__(size, size)
        self.k = size / SIZE
        self.ox = 0
        self.oy = 0

    def at(self, ox=0, oy=0):
        self.ox, self.oy = int(ox), int(oy)
        return self

    def _lo(self, v, o):
        return int(math.floor(v * self.k)) + o

    def _hi(self, v, o):
        return int(math.floor((v + 1) * self.k)) - 1 + o

    def m_rect(self, x0, y0, x1, y1):
        return super().m_rect(max(0, self._lo(x0, self.ox)), max(0, self._lo(y0, self.oy)), self._hi(x1, self.ox), self._hi(y1, self.oy))

    def m_ellipse(self, cx, cy, rx, ry):
        return super().m_ellipse(cx * self.k + self.ox, cy * self.k + self.oy, rx * self.k, ry * self.k)

    def m_poly(self, pts):
        return super().m_poly([(x * self.k + self.ox, y * self.k + self.oy) for x, y in pts])

    def m_line(self, pts, width=1):
        return super().m_line([(x * self.k + self.ox, y * self.k + self.oy) for x, y in pts], width)

    def put(self, x, y, color):
        for yy in range(self._lo(y, self.oy), self._hi(y, self.oy) + 1):
            for xx in range(self._lo(x, self.ox), self._hi(x, self.ox) + 1):
                super().put(xx, yy, color)


# A pose: how the figure stands in one frame. dx / dy move the body, hx / hy the head (output pixels); eyes and mouth change the face;
# g is the class's own gesture (0..1, the idle loop's phase), look the Thief's glance; sweat, sparkle and hurt are the moods' marks.
REST = dict(dx=0, dy=0, hx=0, hy=0, eyes="rest", mouth="rest", g=None, look=0, sweat=None, sparkle=None, hurt=False, panel=False)


def _pose(**kw):
    p = dict(REST)
    p.update(kw)
    return p


def _body(img, p):
    return img.at(p["dx"], p["dy"])


def _head(img, p):
    return img.at(p["dx"] + p["hx"], p["dy"] + p["hy"])


def _backdrop(img, glow, p=REST):
    """A dark vault behind every portrait, a soft glow of the class colour low down (a wince: it flares blood red)."""
    img.at(0, 0)
    img.paint(img.m_rect(0, 0, SIZE - 1, SIZE - 1), C.NIGHT)
    img.dither(img.m_ellipse(24, 46, 26, 16), C.NIGHT, C.BLOOD if p["hurt"] else glow, 0.6 if p["hurt"] else 0.35)
    img.dither(img.m_rect(0, 0, SIZE - 1, 6), C.NIGHT, C.BLACK, 0.5)


def _face(img, cx, cy, skin, shadow):
    head = img.m_ellipse(cx, cy, 7.5, 8.5)
    img.paint(head, skin)
    img.dither(head & img.m_rect(cx + 2, 0, SIZE, SIZE), skin, shadow, 0.5)
    img.inner_outline(head, shadow)
    img.put(cx - 3, cy, C.BLACK)
    img.put(cx + 3, cy, C.BLACK)
    return head


def peasant(img=None, p=REST):
    img = img or Canvas()
    _backdrop(img, C.GREEN_DARK, p)
    _body(img, p)
    # A rough brown smock, rope belt.
    body = img.m_poly([(8, 47), (12, 33), (24, 30), (36, 33), (40, 47)])
    img.shade(body, [C.BLOOD_DARK, C.BLOOD, C.CRIMSON], light=(-1, -1))
    img.paint(img.m_rect(13, 40, 35, 41), C.GOLD_MID)
    if p["panel"]:
        # His satchel at the hip, its strap over the shoulder; a hand that pats it (the idle's gesture).
        img.paint(img.m_line([(12, 38), (21, 31)], 1), C.GOLD_DARK)
        bag = img.m_ellipse(11, 41, 3.6, 3.2)
        img.paint(bag, C.GOLD_MID)
        img.inner_outline(bag, C.GOLD_DARK)
        img.put(11, 39, C.GOLD)
        g = p["g"] or 0.0
        img.paint(img.m_ellipse(16 - 3 * g, 40 - g, 1.8, 1.6), C.BONE_MID)
    _head(img, p)
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
    _body(img, p)
    img.paint(img.m_line([(42, 47), (35, 8)], 1), C.BONE_SHADE)
    for dx in (-2, 0, 2):
        img.paint(img.m_line([(34 + dx, 9), (34 + dx, 3)], 1), C.SILVER)
    img.paint(img.m_rect(32, 9, 36, 9), C.SILVER)
    img.at(0, 0)
    img.outline(C.BLACK, mask=img.px != C.NIGHT)
    return img


def oracle(img=None, p=REST):
    """The Oracle (id "warlock", kept for the saves): eyes bound with a cloth, a third eye open and glowing on the brow, an open hood."""
    img = img or Canvas()
    _backdrop(img, C.VIOLET, p)
    _body(img, p)
    # A lilac robe and an open hood framing the face.
    robe = img.m_poly([(6, 47), (12, 30), (24, 27), (36, 30), (42, 47)])
    img.shade(robe, RAMP_LILAC[:3], light=(-1, -1))
    _head(img, p)
    hood = img.m_ellipse(24, 21, 12, 14) & img.m_rect(0, 0, SIZE, 34)
    img.shade(hood, [C.BLACK, C.VIOLET, C.LILAC], light=(-1, -1))
    _face(img, 24, 22, C.BONE, C.BONE_DARK)
    # The blindfold: a dark band over the eyes, its knot's tails falling to the left.
    img.paint(img.m_rect(16, 21, 32, 23), C.PLUM)
    img.paint(img.m_rect(16, 23, 32, 23), C.BLACK)
    img.paint(img.m_line([(16, 22), (12, 27)], 1), C.PLUM)
    img.paint(img.m_line([(16, 23), (13, 29)], 1), C.PLUM)
    # The third eye on the brow: open, lit, a halo of dithered light around it (brighter at the top of the idle's breath).
    glowing = p["g"] is not None and p["g"] > 0.6
    img.dither(img.m_ellipse(24, 16, 6.5 if glowing else 5, 4 if glowing else 3), C.BONE, C.LILAC_LIGHT, 0.75 if glowing else 0.5)
    third = img.m_ellipse(24, 16, 2.6, 1.6)
    img.paint(third, C.WHITE)
    img.put(24, 16, C.VIOLET)
    img.put(23, 16, C.LILAC)
    img.inner_outline(third, C.LILAC)
    img.put(24, 13, C.LILAC_LIGHT)
    if glowing:
        img.put(24, 12, C.WHITE)
        img.put(20, 16, C.LILAC_LIGHT)
        img.put(28, 16, C.LILAC_LIGHT)
    # A calm, thin mouth.
    img.paint(img.m_rect(22, 27, 26, 27), C.BONE_SHADE)
    # Runes of fate along the hem.
    _body(img, p)
    for x in range(10, 40, 5):
        img.put(x, 44, C.LILAC_LIGHT)
        img.put(x + 1, 43, C.LILAC_LIGHT)
    img.at(0, 0)
    img.outline(C.BLACK, mask=img.px != C.NIGHT)
    return img


def king(img=None, p=REST):
    img = img or Canvas()
    _backdrop(img, C.BLOOD, p)
    _body(img, p)
    # A red cloak with an ermine collar.
    cloak = img.m_poly([(5, 47), (11, 31), (24, 28), (37, 31), (43, 47)])
    img.shade(cloak, RAMP_RED[1:4], light=(-1, -1))
    collar = img.m_ellipse(24, 33, 13, 3.5)
    img.paint(collar, C.BONE)
    for x in range(14, 36, 4):
        img.put(x, 33, C.BLACK)
    _head(img, p)
    _face(img, 24, 22, C.BONE, C.BONE_DARK)
    # A full grey beard.
    beard = img.m_poly([(17, 25), (31, 25), (28, 33), (24, 35), (20, 33)])
    img.paint(beard, C.SILVER)
    img.dither(beard, C.SILVER, C.SILVER_DARK, 0.3)
    # The crown: gold band, three points, a ruby (the idle's gesture: it slips aside and his hand sets it right).
    g = p["g"] or 0.0
    tilt = 1 if g > 0.5 else 0
    img.at(p["dx"] + p["hx"] + tilt, p["dy"] + p["hy"])
    img.paint(img.m_rect(16, 12, 32, 15), C.GOLD)
    for px in (16, 23, 30):
        img.paint(img.m_poly([(px, 12), (px + 1, 6), (px + 2, 12)]), C.GOLD_LIGHT)
    img.paint(img.m_rect(16, 15, 32, 15), C.GOLD_MID)
    img.put(24, 13, C.RED)
    img.put(23, 13, C.HELL)
    if p["panel"] and g > 0.3:
        _head(img, p)
        # A red sleeve up from the shoulder, the hand at the crown's edge.
        sleeve = img.m_poly([(34, 31), (39, 31), (37.5, 19 - 3 * g), (34.5, 19 - 3 * g)])
        img.paint(sleeve, C.CRIMSON)
        img.inner_outline(sleeve, C.BLOOD)
        img.paint(img.m_ellipse(36, 17 - 3 * g, 2.2, 2), C.BONE_MID)
    img.at(0, 0)
    img.outline(C.BLACK, mask=img.px != C.NIGHT)
    return img


def _jester_cap(img, cx, top, reach, droop, bell, swing=0):
    """A fool's cap: three floppy points (crimson, gold, violet) falling to the sides, a gold bell on each tip (swinging by `swing`)."""
    band = img.m_rect(cx - reach // 2, top + droop, cx + reach // 2, top + droop + 2)
    points = [
        ([(cx - 3, top + droop), (cx - reach + swing, top + 1), (cx - reach - 2 + swing, top + droop + 3), (cx - reach // 2, top + droop)], C.CRIMSON,
         (cx - reach - 2 + swing, top + droop + 3)),
        ([(cx - 2, top + droop), (cx + swing, top - 1), (cx + 2, top + droop)], C.GOLD, (cx + swing, top - 1)),
        ([(cx + 3, top + droop), (cx + reach + swing, top + 1), (cx + reach + 2 + swing, top + droop + 3), (cx + reach // 2, top + droop)], C.VIOLET,
         (cx + reach + 2 + swing, top + droop + 3)),
    ]
    for poly, colour, tip in points:
        img.paint(img.m_poly(poly), colour)
    img.paint(band, C.GOLD_MID)
    for _, _, (bx, by) in points:
        b = img.m_ellipse(bx, by, bell, bell)
        img.paint(b, C.GOLD_LIGHT)
        img.put(int(bx), int(by + bell), C.GOLD_DARK)


def jester(img=None, p=REST):
    img = img or Canvas()
    _backdrop(img, C.CRIMSON, p)
    _body(img, p)
    # Motley: a doublet split crimson and gold, a pointed ruff.
    body = img.m_poly([(7, 47), (12, 33), (24, 30), (36, 33), (41, 47)])
    img.paint(body & img.m_rect(0, 0, 23, SIZE), C.CRIMSON)
    img.paint(body & img.m_rect(24, 0, SIZE, SIZE), C.GOLD_MID)
    img.dither(body & img.m_rect(0, 40, SIZE, SIZE), C.CLEAR, C.BLOOD_DARK, 0.25)
    for k, x in enumerate(range(13, 36, 4)):
        img.paint(img.m_poly([(x, 31), (x + 2, 36), (x + 4, 31)]), C.BONE if k % 2 == 0 else C.LILAC_LIGHT)
    _head(img, p)
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
    # The idle's gesture: the bells swing (whole pixels, the phase round the loop).
    swing = 0 if p["g"] is None else int(round(math.sin(2 * math.pi * p["g"]) * 1.5))
    _jester_cap(img, 24, 6, 14, 8, 1.6, swing)
    img.at(0, 0)
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


def summoner(img=None, p=REST):
    img = img or Canvas()
    _backdrop(img, C.HELL, p)
    _body(img, p)
    # A dark green robe, a summoning circle glowing on the ground, two imps hovering at his shoulders.
    circle = img.m_ellipse(24, 45, 20, 3) & ~img.m_ellipse(24, 45, 17, 1.8)
    img.paint(circle, C.EMBER)
    robe = img.m_poly([(9, 47), (13, 30), (24, 27), (35, 30), (39, 47)])
    img.shade(robe, [C.BLACK, C.GREEN_DARK, C.GREEN], light=(-1, -1))
    for x in range(14, 36, 6):
        img.put(x, 41, C.EMBER)
        img.put(x + 1, 40, C.AMBER)
    _head(img, p)
    _face(img, 24, 21, C.BONE_MID, C.BONE_SHADE)
    # A shaved head with a burned sigil, a thin mouth.
    img.put(24, 15, C.HELL)
    img.put(23, 16, C.HELL)
    img.put(25, 16, C.HELL)
    img.paint(img.m_rect(22, 26, 26, 26), C.BONE_SHADE)
    # His raised hands, a flame between them.
    _body(img, p)
    img.paint(img.m_ellipse(13, 33, 2.4, 2.4), C.BONE_MID)
    img.paint(img.m_ellipse(35, 33, 2.4, 2.4), C.BONE_MID)
    img.paint(img.m_ellipse(24, 37, 2.2, 3.2), C.AMBER)
    img.put(24, 36, C.EMBER)
    _imp(img, 8, 14, C.RED, C.AMBER)
    # The idle's gesture: the imp on his shoulder fidgets (hops a pixel, its eyes go dark a moment).
    g = p["g"]
    hop = 0 if g is None else -int(round(g * 2))
    img.at(p["dx"], p["dy"] + hop)
    _imp(img, 40, 12, C.CRIMSON, C.CRIMSON if g is not None and 0.45 < g < 0.75 else C.GOLD_LIGHT)
    img.at(0, 0)
    img.outline(C.BLACK, mask=img.px != C.NIGHT)
    return img


def thief(img=None, p=REST):
    """The Thief: a dark hood and a mask over the eyes, a sly smile, a stolen card between two fingers."""
    img = img or Canvas()
    _backdrop(img, C.DUSK, p)
    _body(img, p)
    # A dark cloak, a belt with a little purse.
    cloak = img.m_poly([(7, 47), (12, 31), (24, 28), (36, 31), (41, 47)])
    img.shade(cloak, [C.BLACK, C.NIGHT, C.DUSK], light=(-1, -1))
    img.paint(img.m_rect(13, 41, 35, 42), C.BONE_SHADE)
    img.paint(img.m_ellipse(30, 44, 2.5, 2), C.GOLD_MID)
    img.put(30, 43, C.GOLD_LIGHT)
    # The hood over the head.
    _head(img, p)
    hood = img.m_ellipse(24, 21, 11, 13) & img.m_rect(0, 0, SIZE, 33)
    img.shade(hood, [C.BLACK, C.NIGHT, C.PLUM], light=(-1, -1))
    _face(img, 24, 23, C.BONE_MID, C.BONE_SHADE)
    # The mask: a black band with two eye holes, a glint in each (the idle's gesture: the eyes dart aside).
    img.paint(img.m_rect(16, 20, 32, 24), C.BLACK)
    for ex in (20, 28):
        img.paint(img.m_rect(ex - 1, 21, ex + 1, 23), C.BONE_MID)
        img.put(ex + p["look"], 22, C.BLACK)
        img.put(ex + 1, 21, C.WHITE)
    # A crooked grin.
    img.paint(img.m_line([(21, 28), (27, 27)], 1), C.BONE_SHADE)
    img.put(27, 26, C.BONE_SHADE)
    # A hand raised at the shoulder, a card between its fingers.
    _body(img, p)
    img.paint(img.m_ellipse(38, 34, 2.4, 2.4), C.BONE_MID)
    card = img.m_rect(37, 23, 43, 32)
    img.paint(card, C.BONE)
    img.inner_outline(card, C.BONE_SHADE)
    img.put(40, 27, C.RED)
    img.put(39, 26, C.RED)
    img.put(41, 26, C.RED)
    img.put(40, 28, C.RED)
    img.at(0, 0)
    img.outline(C.BLACK, mask=img.px != C.NIGHT)
    return img


# ---------------------------------------------------------------- the moods (Demo 3.1): the face over the drawn portrait

# Where each face sits on the design grid: its centre, its skin and shadow, the mouth's row, what covers the mouth (the King's beard).
FACES = {
    "peasant": (24, 23, C.BONE, C.BONE_SHADE, 28, C.BONE),
    "warlock": (24, 22, C.BONE, C.BONE_SHADE, 27, C.BONE),
    "king": (24, 22, C.BONE, C.SILVER_DARK, 27, C.SILVER),
    "jester": (24, 23, C.BONE, C.BLACK, 28, C.BONE),
    "summoner": (24, 21, C.BONE_MID, C.BONE_SHADE, 26, C.BONE_MID),
    "thief": (24, 23, C.BONE_MID, C.BONE_SHADE, 28, C.BONE_MID),
}


def _eyes(img, cid, p):
    cx, cy, skin, line, _, _ = FACES[cid]
    kind = p["eyes"]
    if kind == "rest":
        return
    if cid == "warlock":
        # Blindfolded: the third eye shows it — shut, a slit, wide, squeezed.
        if kind in ("closed", "squeeze", "narrow", "smile"):
            img.paint(img.m_ellipse(24, 16, 2.6, 1.6), C.BONE)
            img.paint(img.m_rect(22, 16, 26, 16), C.LILAC if kind == "narrow" else C.BONE_SHADE)
            if kind == "smile":
                img.put(22, 15, C.BONE_SHADE)
                img.put(26, 15, C.BONE_SHADE)
        elif kind == "wide":
            img.put(24, 13, C.WHITE)
            img.put(21, 16, C.LILAC_LIGHT)
            img.put(27, 16, C.LILAC_LIGHT)
        return
    holes = (20, 28) if cid == "thief" else (cx - 3, cx + 3)
    ey = 22 if cid == "thief" else cy
    for i, ex in enumerate(holes):
        back = C.BONE_MID if cid == "thief" else skin
        if cid == "thief":
            img.paint(img.m_rect(ex - 1, 21, ex + 1, 23), back)
        else:
            img.put(ex, ey, back)
        side = -1 if i == 0 else 1
        if kind == "closed":
            img.paint(img.m_rect(ex - 1, ey, ex + 1, ey), C.BLACK)
        elif kind == "smile":
            img.put(ex - 1, ey, C.BLACK)
            img.put(ex, ey - 1, C.BLACK)
            img.put(ex + 1, ey, C.BLACK)
        elif kind == "wide":
            img.put(ex, ey, C.BLACK)
            img.put(ex, ey - 1, C.WHITE)
        elif kind == "squeeze":
            img.put(ex - side, ey - 1, C.BLACK)
            img.put(ex, ey, C.BLACK)
            img.put(ex - side, ey + 1, C.BLACK)
        elif kind == "narrow":
            img.paint(img.m_rect(ex - 1, ey, ex + 1, ey), C.BLACK)
            if cid != "thief":
                img.put(ex - side, ey - 2, line)
                img.put(ex, ey - 2, line)


def _mouth(img, cid, p):
    cx, _, skin, line, my, cover = FACES[cid]
    kind = p["mouth"]
    if kind == "rest":
        return
    img.paint(img.m_rect(cx - 4, my - 1, cx + 4, my + 1), cover)
    if kind == "smile":
        img.paint(img.m_rect(cx - 2, my, cx + 2, my), line)
        img.put(cx - 3, my - 1, line)
        img.put(cx + 3, my - 1, line)
    elif kind == "grin":
        img.paint(img.m_rect(cx - 2, my - 1, cx + 2, my + 1), C.BLACK)
        img.paint(img.m_rect(cx - 1, my - 1, cx + 1, my - 1), C.BONE)
        img.put(cx - 3, my - 1, C.BLACK)
        img.put(cx + 3, my - 1, C.BLACK)
    elif kind == "frown":
        img.paint(img.m_rect(cx - 2, my, cx + 2, my), line)
        img.put(cx - 3, my + 1, line)
        img.put(cx + 3, my + 1, line)
    elif kind == "tight":
        img.paint(img.m_rect(cx - 1, my, cx + 1, my), line)


def _marks(img, cid, p):
    """A tense bead of sweat running down the brow; joy's sparkles round the head."""
    cx, cy = FACES[cid][0], FACES[cid][1]
    if p["sweat"] is not None:
        _head(img, p)
        y = cy - 5 + p["sweat"]
        img.put(cx + 6, y, C.LILAC_LIGHT)
        img.put(cx + 6, y + 1, C.WHITE)
    if p["sparkle"] is not None:
        img.at(0, 0)
        for i, (sx, sy) in enumerate([(7, 9), (41, 7), (43, 25), (5, 27)]):
            if (p["sparkle"] + i) % 3 != 0:
                continue
            colour = C.WHITE if i % 2 == 0 else C.GOLD_LIGHT
            img.put(sx, sy, colour)
            img.put(sx - 1, sy, C.GOLD)
            img.put(sx + 1, sy, C.GOLD)
            img.put(sx, sy - 1, C.GOLD)
            img.put(sx, sy + 1, C.GOLD)


PORTRAITS = {"peasant": peasant, "warlock": oracle, "king": king, "jester": jester, "summoner": summoner, "thief": thief}


def panel_frame(cid, p):
    """One 64x64 frame of the player panel's portrait in the pose p."""
    p = dict(p, panel=True)
    img = PORTRAITS[cid](Canvas(PANEL), p)
    _head(img, p)
    _eyes(img, cid, p)
    _mouth(img, cid, p)
    _marks(img, cid, p)
    img.at(0, 0)
    return img


def _thief_look(f, n):
    s = math.sin(2 * math.pi * f / n)
    return -1 if s > 0.6 else 1 if s < -0.6 else 0


def poses(cid, state):
    """The frames of a state: idle (8, loops) breathes and blinks and does the class's gesture; happy (6) smiles; joy (8) leaps, grins and
    sparkles; hurt (6) winces and shakes; tense (8, loops) narrows the eyes and sweats."""
    if state == "idle":
        n = 8
        out = []
        for f in range(n):
            phase = 2 * math.pi * f / n
            breath = 1 if math.sin(phase) > 0.3 else 0
            out.append(_pose(dy=-breath, g=(1 - math.cos(phase)) / 2, eyes="closed" if f == 6 else "rest",
                             look=_thief_look(f, n) if cid == "thief" else 0))
        return out
    if state == "happy":
        lift = [0, -1, -1, -1, 0, 0]
        return [_pose(dy=lift[f], eyes="smile" if 1 <= f <= 4 else "rest", mouth="smile", g=0.0) for f in range(6)]
    if state == "joy":
        jump = [0, -2, -3, -2, 0, -1, 0, 0]
        return [_pose(dy=jump[f], eyes="wide", mouth="grin", sparkle=f, g=0.0) for f in range(8)]
    if state == "hurt":
        shake = [2, -2, 1, -1, 0, 0]
        return [_pose(hx=shake[f], dx=shake[f] // 2, eyes="squeeze" if f < 4 else "rest", mouth="frown", hurt=f < 2, g=0.0) for f in range(6)]
    if state == "tense":
        n = 8
        return [_pose(hx=1 if f % 4 == 2 else 0, eyes="narrow", mouth="tight", sweat=f * 6 // n, g=0.0) for f in range(n)]
    raise ValueError(state)


STATES = ["idle", "happy", "joy", "hurt", "tense"]
LOOPS = {"idle", "tense"}


def loop_closes(frames):
    """The loop rule: the step from the last frame back to the first is no bigger than the biggest step between frames (2% slack)."""
    def diff(a, b):
        return int((a.px != b.px).sum())
    largest = max(diff(frames[i], frames[i + 1]) for i in range(len(frames) - 1))
    return diff(frames[-1], frames[0]) <= largest * 1.02 + 1


# ---------------------------------------------------------------- icons

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


def write_all(art_dir):
    out = []
    folder = os.path.join(art_dir, "Sinners")
    os.makedirs(folder, exist_ok=True)
    for class_id in ORDER:
        path = os.path.join(folder, class_id + ".png")
        PORTRAITS[class_id]().save(path)
        out.append(path)
        # Demo 3.1: the panel's animated 64x64 portrait, a strip per state.
        own = os.path.join(folder, class_id)
        os.makedirs(own, exist_ok=True)
        for state in STATES:
            frames = [panel_frame(class_id, p) for p in poses(class_id, state)]
            if state in LOOPS:
                assert loop_closes(frames), f"{class_id}/{state}: the loop jumps back to its first frame"
            strip = os.path.join(own, state + ".png")
            sheet(frames).save(strip)
            out.append(strip)
    strip = os.path.join(art_dir, "Ui", "sinner_icons.png")
    sheet([icon(c) for c in ORDER]).save(strip)
    out.append(strip)
    passives = os.path.join(art_dir, "Ui", "passive_icons.png")
    sheet([passive_icon(c) for c in ORDER]).save(passives)
    out.append(passives)
    return out
