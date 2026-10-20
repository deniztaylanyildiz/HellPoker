"""Demo 3.5: the black market's scene — each vendor's stall, his portrait large, the services' icons.

Backgrounds/market_<vendor>/normal.png + motion.txt (640x360, drawn at the screen's own size): the stall's back wall in the vendor's
colours, an awning under the scene's top strip, shelves of his wares behind the cards, a counter, the floor; two lanterns beside his
portrait and a prop on the floor move (8 frames, 8-10 FPS), dust or embers drift by the portrait. Nothing moves where the scene's words
and cards sit (MARKET_TEXT; the C# copy is BackdropMotionTests.MarketText).

Market/<vendor>.png (96x96): the vendor's portrait, his 48 px design (pixel_strangers) drawn again on a canvas twice the size — every
shape filled at 96, not scaled up — with a little more light in his eyes.

Ui/market_services.png (12 x 16x16, MarketService's order): haggle, new stall, the imp's eye, a joker in, a joker out, throw one away,
sell, pawn the soul, the crucible, burn a curse, open the box, double or nothing.
"""
import math
import os

import numpy as np

from pixel import C, Img, RAMP_BONE, RAMP_GOLD, RAMP_GREEN, RAMP_LILAC, RAMP_NIGHT, RAMP_RED, RAMP_SILVER, sheet
import pixel_layers
from pixel_layers import Layer, Particles, phase
import pixel_strangers

W, H = 640, 360
VENDORS = ["smuggler", "pawnshop", "bonemerchant", "alchemist", "gamblersstall", "tailor"]

# Where the scene's words and cards sit (inclusive boxes): the top strip, the portrait and the name, the bubble, the stall and the tiles
# and LEAVE (Views/MarketView).
MARKET_TEXT = [(0, 0, 639, 25), (60, 36, 164, 158), (16, 152, 207, 336), (224, 32, 623, 352)]

# The vendor's colours: the awning's two stripes, the wall's glow, the lanterns' flame ramp, the drifting pixels.
SCHEMES = {
    "smuggler":      (C.BLOOD_DARK, C.NIGHT,       C.BLOOD_DARK, [C.BLOOD, C.HELL, C.AMBER],        "motes"),
    "pawnshop":      (C.GREEN_DARK, C.GOLD_DARK,   C.GREEN_DARK, [C.GOLD_MID, C.GOLD, C.GOLD_LIGHT], "motes"),
    "bonemerchant":  (C.BONE_SHADE, C.SILVER_DARK, C.BONE_SHADE, [C.SILVER_DARK, C.SILVER, C.WHITE], "wisps"),
    "alchemist":     (C.VIOLET,     C.DUSK,        C.VIOLET,     [C.GREEN, C.GREEN_LIGHT, C.GOLD],   "wisps"),
    "gamblersstall": (C.CRIMSON,    C.GOLD_DARK,   C.PLUM,       [C.RED, C.ORANGE, C.EMBER],        "embers"),
    "tailor":        (C.BLOOD,      C.BLACK,       C.BLOOD,      [C.CRIMSON, C.HELL, C.ORANGE],      "embers"),
}


# ---------------------------------------------------------------- the stall

def wall(img, glow):
    """Dark stone courses, a warm glow in the vendor's colour behind his portrait, darker toward the edges."""
    img.paint(img.m_rect(0, 0, W - 1, H - 1), C.BLACK)
    img.dither(img.m_rect(0, 26, W - 1, 336), C.BLACK, C.NIGHT, 0.55)
    for y in range(34, 336, 12):
        img.paint(img.m_rect(0, y, W - 1, y), C.BLACK)
        off = 0 if (y // 12) % 2 == 0 else 12
        for x in range(off, W, 24):
            img.paint(img.m_rect(x, y + 1, x, y + 11), C.BLACK)
    img.dither(img.m_ellipse(112, 96, 104, 72) & img.m_rect(0, 49, 215, 336), C.NIGHT, glow, 0.12)


def awning(img, a, b):
    """The stall's awning under the top strip: stripes 16 px wide, a scalloped edge."""
    for x in range(0, W, 16):
        img.paint(img.m_rect(x, 26, x + 15, 42), a if (x // 16) % 2 == 0 else b)
        img.paint(img.m_ellipse(x + 8, 42, 8, 5) & img.m_rect(x, 42, x + 15, 47), a if (x // 16) % 2 == 0 else b)
    img.paint(img.m_rect(0, 26, W - 1, 26), C.GOLD_MID)
    edge = img.m_rect(0, 27, W - 1, 48) & (img.px != C.BLACK) & (img.px != C.NIGHT)
    img.outline(C.BLACK, mask=edge)


def shelves(img, vendor):
    """Behind the cards: two shelves of the vendor's wares (mostly hidden by the cards; they show between and beside them)."""
    for y in (124, 208):
        plank = img.m_rect(216, y, 631, y + 3)
        img.shade(plank, [C.BLOOD_DARK, C.GOLD_DARK, C.GOLD_MID], light=(0, -1))
        img.outline(C.BLACK, mask=plank)
        for x in range(220, 632, 18):
            ware(img, vendor, x, y - 1, (x * 7 + y) % 5)


def ware(img, vendor, x, base, k):
    """One thing on a shelf, standing on `base`."""
    if vendor == "smuggler":
        img.shade(img.m_rect(x, base - 10, x + 11, base), [C.BLOOD_DARK, C.BLOOD, C.GOLD_DARK], light=(-1, -1))
        img.paint(img.m_line([(x, base - 10), (x + 11, base)], 1), C.BLOOD_DARK)
    elif vendor == "pawnshop":
        for i in range(1 + k % 3):
            img.paint(img.m_ellipse(x + 6, base - 1 - i * 3, 5, 1.6), C.GOLD if i % 2 == 0 else C.GOLD_MID)
        if k == 4:
            img.shade(img.m_poly([(x + 1, base - 4), (x + 2, base - 12), (x + 5, base - 8), (x + 8, base - 13), (x + 11, base - 4)]), RAMP_GOLD)
    elif vendor == "bonemerchant":
        if k % 2 == 0:
            skull = img.m_ellipse(x + 6, base - 6, 5, 5)
            img.shade(skull, RAMP_BONE)
            img.put(x + 4, base - 6, C.BLACK)
            img.put(x + 8, base - 6, C.BLACK)
        else:
            img.paint(img.m_line([(x + 1, base - 1), (x + 11, base - 9)], 2), C.BONE_MID)
    elif vendor == "alchemist":
        color = [C.GREEN_LIGHT, C.LILAC, C.HELL, C.GOLD, C.BLUE][k]
        body = img.m_ellipse(x + 6, base - 4, 4, 4) | img.m_rect(x + 5, base - 11, x + 7, base - 7)
        img.paint(body, C.SILVER_DARK)
        img.paint(img.m_ellipse(x + 6, base - 3, 3, 2.4), color)
    elif vendor == "gamblersstall":
        color = [C.BLOOD, C.VIOLET, C.GOLD_DARK, C.GREEN_DARK, C.BLOOD_DARK][k]
        img.paint(img.m_rect(x + 1, base - 9, x + 10, base), color)
        img.paint(img.m_rect(x + 5, base - 9, x + 6, base), C.GOLD)
    else:  # the tailor's bolts of cloth
        color = [C.CRIMSON, C.PLUM, C.BLOOD, C.VIOLET, C.NIGHT][k]
        img.shade(img.m_ellipse(x + 6, base - 5, 6, 5), [C.BLACK, color, color], light=(-1, -1))
        img.put(x + 6, base - 5, C.GOLD)
    if vendor != "pawnshop" or k == 4:
        img.outline(C.BLACK, mask=img.m_rect(x, base - 14, x + 12, base) & (img.px != C.BLACK) & (img.px != C.NIGHT) &
                    ~img.m_rect(216, base + 1, 631, base + 4))


def counter(img):
    """The counter under the cards, and the floor."""
    top = img.m_rect(216, 236, 631, 247)
    img.shade(top, [C.BLACK, C.BLOOD_DARK, C.BLOOD, C.GOLD_DARK], light=(0, -1))
    img.paint(img.m_rect(216, 248, 631, 336), C.BLOOD_DARK)
    img.dither(img.m_rect(216, 248, 631, 336), C.BLOOD_DARK, C.BLACK, 0.6)
    img.outline(C.BLACK, mask=top)
    img.paint(img.m_rect(0, 337, W - 1, H - 1), C.NIGHT)
    img.dither(img.m_rect(0, 337, W - 1, H - 1), C.NIGHT, C.BLACK, 0.5)
    img.paint(img.m_rect(0, 337, W - 1, 337), C.DUSK)


def hook(img, x):
    """A lantern's chain from the awning."""
    img.paint(img.m_rect(x + 9, 48, x + 10, 52), C.SILVER_DARK)


def lantern_frames(ramp, seed):
    """A hanging lantern (20x30): an iron cage, a flame that leans and flickers (8 frames, closing on itself)."""
    frames = []
    for f in range(8):
        img = Img(20, 30)
        a = phase(f, 8) + seed
        cage = img.m_rect(4, 6, 15, 24)
        img.paint(cage, C.BLACK)
        glow = img.m_rect(6, 8, 13, 22)
        img.dither(glow, C.BLOOD_DARK, ramp[0], 0.5 + 0.25 * math.sin(a))
        lean = int(round(1.2 * math.sin(a)))
        tall = 9 + int(round(1.5 * math.sin(2 * a)))
        flame = img.m_poly([(7, 21), (9 + lean, 21 - tall), (10 + lean, 21 - tall), (12, 21)])
        img.paint(flame, ramp[1])
        img.paint(img.m_poly([(8, 21), (9 + lean, 23 - tall + 3), (10 + lean, 23 - tall + 3), (11, 21)]), ramp[2])
        for x in (4, 9, 15):
            img.paint(img.m_rect(x, 6, x, 24), C.SILVER_DARK)
        img.paint(img.m_rect(3, 4, 16, 5) | img.m_rect(3, 25, 16, 26), C.SILVER_DARK)
        img.paint(img.m_rect(8, 0, 11, 3), C.SILVER_DARK)
        frames.append(img)
    return frames


def lantern_layer(name, x, ramp, seed):
    frames = lantern_frames(ramp, seed)
    return Layer(name, x, 52, 20, 30, 8, 8, lambda img, f: img.blit(frames[f], 0, 0))


def prop_layer(vendor):
    """A thing on the floor that moves (48x22, under the bubble)."""
    def draw(img, f):
        a = phase(f, 8)
        if vendor == "alchemist":   # a flask bubbling
            img.paint(img.m_ellipse(24, 15, 9, 6) | img.m_rect(21, 3, 27, 10), C.SILVER_DARK)
            img.paint(img.m_ellipse(24, 16, 7, 4), C.GREEN)
            for k in range(3):
                y = 12 - (f + k * 3) % 8
                img.put(22 + k * 2, y + 2, C.GREEN_LIGHT)
            img.outline(C.BLACK)
        elif vendor == "pawnshop":   # a heap of coins, a glint running over it
            for i, (cx, cy) in enumerate(((14, 18), (22, 17), (30, 18), (18, 14), (26, 14), (22, 11))):
                img.paint(img.m_ellipse(cx, cy, 4, 1.8), C.GOLD if i % 2 else C.GOLD_MID)
            gx = 12 + int(round(10 + 10 * math.sin(a)))
            img.put(gx, 14, C.WHITE)
            img.outline(C.BLACK)
        elif vendor == "bonemerchant":   # a candle in a skull
            img.shade(img.m_ellipse(24, 15, 7, 6), RAMP_BONE)
            img.put(21, 15, C.BLACK)
            img.put(27, 15, C.BLACK)
            lean = int(round(math.sin(a)))
            img.paint(img.m_rect(23, 4, 25, 9), C.BONE)
            img.paint(img.m_poly([(23, 4), (24 + lean, 0), (25 + lean, 0), (26, 4)]), C.AMBER)
            img.outline(C.BLACK)
        elif vendor == "gamblersstall":   # a die rocking
            tilt = int(round(1.5 * math.sin(a)))
            die = img.m_poly([(16 + tilt, 8), (30 + tilt, 8 - tilt), (31, 21), (17, 21)])
            img.paint(die, C.BONE)
            for px, py in ((20, 12), (26, 16), (23, 14)):
                img.put(px + tilt // 2, py, C.BLOOD)
            img.outline(C.BLACK)
        elif vendor == "tailor":   # the needle of fire, flickering
            img.paint(img.m_line([(10, 20), (38, 6)], 1), C.SILVER)
            t = int(round(2 * math.sin(a)))
            img.paint(img.m_line([(38, 6), (42, 3 + t)], 1), C.ORANGE)
            img.put(43, 2 + t, C.EMBER)
            img.paint(img.m_line([(10, 20), (6, 21), (4, 18)], 1), C.CRIMSON)
            img.outline(C.BLACK)
        else:   # the smuggler: a rat's tail twitching out of a crate
            img.shade(img.m_rect(8, 6, 30, 21), [C.BLOOD_DARK, C.BLOOD, C.GOLD_DARK], light=(-1, -1))
            img.paint(img.m_line([(8, 6), (30, 21)], 1), C.BLOOD_DARK)
            sway = int(round(2 * math.sin(a)))
            img.paint(img.m_line([(30, 18), (36, 16 + sway), (42, 18 + sway)], 1), C.BONE_DARK)
            img.outline(C.BLACK)
    return Layer("prop", 24, 338, 48, 22, 8, 10, draw)


def stall(vendor):
    a, b, glow, ramp, particles = SCHEMES[vendor]
    img = Img(W, H)
    wall(img, glow)
    shelves(img, vendor)
    counter(img)
    awning(img, a, b)
    for x in (20, 186):
        hook(img, x)
    layers = [lantern_layer("lantern_a", 20, ramp, 0.0), lantern_layer("lantern_b", 186, ramp, 1.9), prop_layer(vendor)]
    drift = [Particles(particles, 16, 86, 40, 60, 6), Particles(particles, 168, 86, 36, 60, 6)]
    return img, layers, drift


# ---------------------------------------------------------------- the portraits

class Twice(Img):
    """A 96x96 canvas that takes the 48 px design's coordinates: every shape is mapped (x -> 2x) and filled at 96."""

    def __init__(self):
        super().__init__(96, 96)

    def m_rect(self, x0, y0, x1, y1):
        return super().m_rect(2 * x0, 2 * y0, 2 * x1 + 1, 2 * y1 + 1)

    def m_ellipse(self, cx, cy, rx, ry):
        return super().m_ellipse(2 * cx, 2 * cy, 2 * rx, 2 * ry)

    def m_poly(self, pts):
        return super().m_poly([(2 * x, 2 * y) for x, y in pts])

    def m_line(self, pts, width=1):
        return super().m_line([(2 * x + 0.5, 2 * y + 0.5) for x, y in pts], 2 * width)

    def put(self, x, y, color):
        self.paint(self.m_rect(x, y, x, y), color)


def portrait(vendor):
    img = Twice()
    pixel_strangers.STRANGERS["market_" + vendor](img)
    # A little more light: a 1 px glint on every eye-sized bright spot's top left (the eyes, the coins).
    px = img.px.copy()
    bright = np.isin(px, [C.EMBER, C.GOLD, C.AMBER, C.GREEN_LIGHT, C.GOLD_LIGHT])
    ys, xs = np.nonzero(bright)
    for y, x in zip(ys, xs):
        if y > 0 and x > 0 and not bright[y - 1, x] and not bright[y, x - 1] and (y + x) % 3 == 0:
            img.px[y, x] = C.WHITE
    return img


# ---------------------------------------------------------------- the services' icons

def icons():
    out = []

    def new():
        return Img(16, 16)

    def coin(img, x, y):
        img.paint(img.m_ellipse(x, y, 3.5, 3.5), C.GOLD)
        img.put(int(x) - 1, int(y) - 1, C.GOLD_LIGHT)
        img.put(int(x) + 1, int(y) + 1, C.GOLD_MID)

    # haggle: a coin between an arrow down (green) and up (red)
    img = new()
    coin(img, 8, 9)
    img.paint(img.m_poly([(1, 4), (5, 4), (3, 8)]), C.GREEN_LIGHT)
    img.paint(img.m_poly([(11, 8), (15, 8), (13, 4)]), C.RED)
    out.append(img)
    # a new stall: a ring of two arrows
    img = new()
    ring = img.m_ellipse(8, 8, 6, 6) & ~img.m_ellipse(8, 8, 3.6, 3.6)
    img.paint(ring & ~img.m_rect(7, 0, 9, 4) & ~img.m_rect(7, 12, 9, 15), C.GOLD)
    img.paint(img.m_poly([(9, 0), (9, 6), (13, 3)]), C.GOLD_LIGHT)
    img.paint(img.m_poly([(7, 10), (7, 16), (3, 13)]), C.GOLD_LIGHT)
    out.append(img)
    # the imp's eye
    img = new()
    img.paint(img.m_ellipse(8, 8, 7, 4), C.BONE)
    img.paint(img.m_ellipse(8, 8, 3, 3), C.HELL)
    img.paint(img.m_rect(8, 6, 8, 10), C.BLACK)
    img.put(6, 6, C.WHITE)
    out.append(img)
    # a joker in / out: the jester's cap and a sign
    for sign in ("+", "-"):
        img = new()
        img.paint(img.m_poly([(2, 12), (4, 3), (8, 9)]), C.RED)
        img.paint(img.m_poly([(8, 9), (8, 1), (12, 12)]), C.GOLD)
        img.paint(img.m_rect(2, 11, 12, 13), C.BONE_MID)
        img.put(4, 2, C.GOLD_LIGHT)
        img.put(8, 0, C.RED)
        if sign == "+":
            img.paint(img.m_rect(13, 9, 13, 15) | img.m_rect(10, 12, 15, 12), C.GREEN_LIGHT)
        else:
            img.paint(img.m_rect(10, 12, 15, 13), C.RED)
        out.append(img)
    # throw one away: a relic falling into a pit
    img = new()
    img.paint(img.m_ellipse(8, 13, 7, 2.5), C.BLACK)
    img.paint(img.m_ellipse(8, 13, 7, 2.5) & ~img.m_ellipse(8, 13, 5.5, 1.6), C.BLOOD_DARK)
    img.paint(img.m_poly([(8, 1), (12, 5), (8, 9), (4, 5)]), C.LILAC)
    img.put(7, 3, C.LILAC_LIGHT)
    out.append(img)
    # sell: a relic for a coin
    img = new()
    img.paint(img.m_poly([(4, 1), (8, 5), (4, 9), (0, 5)]), C.LILAC)
    img.paint(img.m_rect(7, 9, 9, 9) | img.m_poly([(9, 7), (11, 9), (9, 11)]), C.BONE_MID)
    coin(img, 12, 11)
    out.append(img)
    # pawn the soul: a lilac flame in a chain
    img = new()
    img.paint(img.m_poly([(4, 13), (8, 1), (12, 13)]), C.LILAC)
    img.paint(img.m_poly([(6, 13), (8, 6), (10, 13)]), C.LILAC_LIGHT)
    for x in (2, 6, 10, 14):
        img.paint(img.m_ellipse(x, 14, 2, 1.4) & ~img.m_ellipse(x, 14, 1, 0.6), C.SILVER)
    out.append(img)
    # the crucible: a flask
    img = new()
    img.paint(img.m_ellipse(8, 11, 5, 4) | img.m_rect(7, 2, 9, 7), C.SILVER_DARK)
    img.paint(img.m_ellipse(8, 12, 4, 2.6), C.GREEN_LIGHT)
    img.put(7, 0, C.GREEN)
    out.append(img)
    # burn a curse: a flame over a broken line
    img = new()
    img.paint(img.m_poly([(3, 14), (8, 1), (13, 14)]), C.HELL)
    img.paint(img.m_poly([(5, 14), (8, 5), (11, 14)]), C.AMBER)
    img.paint(img.m_rect(7, 10, 9, 14), C.EMBER)
    out.append(img)
    # open the box
    img = new()
    img.paint(img.m_rect(2, 8, 13, 15), C.BLOOD)
    img.paint(img.m_rect(7, 8, 8, 15), C.GOLD)
    img.paint(img.m_poly([(1, 7), (5, 2), (14, 4), (13, 8)]), C.CRIMSON)
    img.put(8, 1, C.GOLD_LIGHT)
    img.put(11, 0, C.WHITE)
    out.append(img)
    # double or nothing: two boxes
    img = new()
    for x, y, c in ((1, 6, C.VIOLET), (8, 2, C.BLOOD)):
        img.paint(img.m_rect(x, y, x + 6, y + 6), c)
        img.paint(img.m_rect(x + 3, y, x + 3, y + 6), C.GOLD)
    img.paint(img.m_rect(9, 12, 14, 13), C.GOLD_LIGHT)
    out.append(img)
    for img in out:
        img.outline(C.BLACK)
    return out


# ---------------------------------------------------------------- writing

def write_all(art_dir):
    written = []
    for vendor in VENDORS:
        base, layers, drift = stall(vendor)
        written += pixel_layers.write(os.path.join(art_dir, "Backgrounds", "market_" + vendor), base, layers, drift, text_zones=MARKET_TEXT)
    os.makedirs(os.path.join(art_dir, "Market"), exist_ok=True)
    for vendor in VENDORS:
        path = os.path.join(art_dir, "Market", vendor + ".png")
        portrait(vendor).save(path)
        written.append(path)
    path = os.path.join(art_dir, "Ui", "market_services.png")
    sheet(icons()).save(path)
    written.append(path)
    return written
