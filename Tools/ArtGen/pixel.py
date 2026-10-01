"""Pixel-art kit: one fixed palette, integer shapes, outlines, band shading and ordered dithering. No anti-aliasing anywhere.

Every image is an array of palette indices (0 = transparent) and is saved through the palette, so no colour outside it can
ever appear. Coordinates are whole pixels; shapes are filled by testing pixel centres.
"""
import numpy as np
from PIL import Image

# ---------------------------------------------------------------- the palette (index 0 is transparent; at most 32 entries)

PALETTE_HEX = [
    None,
    # night: near-black purples
    "#0b0610", "#160b1e", "#22122c", "#321a3c", "#4a2856", "#6a3a74",
    # hellfire: blood to flame
    "#3a0a10", "#5e0f16", "#8c1a1a", "#b8261c", "#e0401c", "#ff7a1c", "#ffb02e", "#ffe08a",
    # bone
    "#f2e8d0", "#c8b89a", "#8e7c64", "#5a4c3e",
    # old gold
    "#6a4410", "#a8701c", "#e0a828", "#ffd860",
    # sickly green (Mammon)
    "#2e3a14", "#5a6a22", "#8a9a3a",
    # lilac (Lilith)
    "#9a7aa8", "#c8a8d4",
    # silver (Belial's tongue, the serpent)
    "#7a8494", "#b8c0cc",
    # pure highlight
    "#ffffff",
]
assert len(PALETTE_HEX) <= 32, "The palette is limited to 32 entries."

PALETTE = [(0, 0, 0, 0)] + [tuple(int(h[i:i + 2], 16) for i in (1, 3, 5)) + (255,) for h in PALETTE_HEX[1:]]


class C:
    """Palette indices by name."""
    CLEAR = 0
    BLACK, NIGHT, DUSK, PLUM, VIOLET, MAUVE = 1, 2, 3, 4, 5, 6
    BLOOD_DARK, BLOOD, CRIMSON, RED, HELL, ORANGE, AMBER, EMBER = 7, 8, 9, 10, 11, 12, 13, 14
    BONE, BONE_MID, BONE_DARK, BONE_SHADE = 15, 16, 17, 18
    GOLD_DARK, GOLD_MID, GOLD, GOLD_LIGHT = 19, 20, 21, 22
    GREEN_DARK, GREEN, GREEN_LIGHT = 23, 24, 25
    LILAC, LILAC_LIGHT = 26, 27
    SILVER_DARK, SILVER = 28, 29
    WHITE = 30


# Ramps from dark to light, used by band shading.
RAMP_RED = [C.BLOOD_DARK, C.BLOOD, C.CRIMSON, C.RED, C.HELL]
RAMP_GOLD = [C.GOLD_DARK, C.GOLD_MID, C.GOLD, C.GOLD_LIGHT]
RAMP_BONE = [C.BONE_SHADE, C.BONE_DARK, C.BONE_MID, C.BONE]
RAMP_GREEN = [C.GREEN_DARK, C.GREEN, C.GREEN_LIGHT, C.GOLD]
RAMP_LILAC = [C.VIOLET, C.MAUVE, C.LILAC, C.LILAC_LIGHT]
RAMP_NIGHT = [C.BLACK, C.NIGHT, C.DUSK, C.PLUM]
RAMP_SILVER = [C.BONE_SHADE, C.SILVER_DARK, C.SILVER, C.WHITE]
RAMP_FIRE = [C.BLOOD, C.RED, C.HELL, C.ORANGE, C.AMBER, C.EMBER]

BAYER4 = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]], np.float32) / 16.0


class Img:
    def __init__(self, w, h, fill=C.CLEAR):
        self.w, self.h = w, h
        self.px = np.full((h, w), fill, np.uint8)
        ys, xs = np.mgrid[0:h, 0:w]
        self.xs = xs.astype(np.float32) + 0.5
        self.ys = ys.astype(np.float32) + 0.5

    # ------------------------------------------------------------ masks (boolean arrays)

    def m_rect(self, x0, y0, x1, y1):
        """Inclusive pixel rectangle."""
        m = np.zeros((self.h, self.w), bool)
        x0, y0 = max(0, int(x0)), max(0, int(y0))
        m[y0:int(y1) + 1, x0:int(x1) + 1] = True
        return m

    def m_ellipse(self, cx, cy, rx, ry):
        return ((self.xs - cx) / rx) ** 2 + ((self.ys - cy) / ry) ** 2 <= 1.0

    def m_poly(self, pts):
        """Even-odd polygon fill tested at pixel centres."""
        m = np.zeros((self.h, self.w), bool)
        n = len(pts)
        for i in range(n):
            x0, y0 = pts[i]
            x1, y1 = pts[(i + 1) % n]
            if y0 == y1:
                continue
            cond = (self.ys >= min(y0, y1)) & (self.ys < max(y0, y1))
            xi = x0 + (self.ys - y0) * (x1 - x0) / (y1 - y0)
            m ^= cond & (self.xs < xi)
        return m

    def m_line(self, pts, width=1):
        """Thick polyline from squares stamped along Bresenham lines."""
        m = np.zeros((self.h, self.w), bool)
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            for x, y in bresenham(int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))):
                r = width // 2
                m[max(0, y - r):max(0, y - r + width), max(0, x - r):max(0, x - r + width)] = True
        return m

    # ------------------------------------------------------------ painting

    def paint(self, mask, color):
        self.px[mask] = color

    def put(self, x, y, color):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[int(y), int(x)] = color

    def rows(self, x, y, rows, palette_map):
        """Stamps a small pattern: rows of characters mapped to palette indices ('.' or ' ' is skipped)."""
        for r, row in enumerate(rows):
            for c, ch in enumerate(row):
                if ch in ". " or ch not in palette_map:
                    continue
                self.put(x + c, y + r, palette_map[ch])

    def dither(self, mask, color_a, color_b, amount):
        """Mixes two colours over a mask with a 4×4 Bayer pattern; amount is the share of colour_b (0..1, or a field)."""
        threshold = BAYER4[(np.arange(self.h)[:, None] % 4), (np.arange(self.w)[None, :] % 4)]
        b = np.asarray(amount, np.float32) > threshold
        self.px[mask & ~b] = color_a
        self.px[mask & b] = color_b

    def shade(self, mask, ramp, light=(-1, -1), shadow=2, rim=1, base_level=None):
        """
        Band shading for a flat shape: the body gets the middle of the ramp, a band along the edge facing away from the light
        steps down the ramp (darkest at the very edge), and a thin rim on the lit side gets the top colour.
        """
        lx, ly = light
        n = len(ramp)
        base = base_level if base_level is not None else n // 2
        level = np.full((self.h, self.w), base, np.int32)
        for k in range(shadow, 0, -1):
            away = ~shifted(mask, -lx * k, -ly * k)
            level[away] = max(base - (shadow - k + 1), 0)
        for k in range(1, rim + 1):
            level[~shifted(mask, lx * k, ly * k)] = n - 1
        self.px[mask] = np.array(ramp, np.uint8)[np.clip(level, 0, n - 1)][mask]

    def outline(self, color, mask=None, diagonal=False):
        """Draws a 1 px line just outside every opaque pixel (or outside a given mask)."""
        solid = (self.px != C.CLEAR) if mask is None else mask
        grown = solid.copy()
        steps = [(1, 0), (-1, 0), (0, 1), (0, -1)] + ([(1, 1), (1, -1), (-1, 1), (-1, -1)] if diagonal else [])
        for dx, dy in steps:
            grown |= shifted(solid, dx, dy, fill=False)
        ring = grown & ~solid
        self.px[ring] = color

    def inner_outline(self, mask, color):
        """Colours the edge pixels of a mask itself."""
        edge = mask & ~(shifted(mask, 1, 0) & shifted(mask, -1, 0) & shifted(mask, 0, 1) & shifted(mask, 0, -1))
        self.px[edge] = color

    def blit(self, other, x, y):
        """Copies the opaque pixels of another image."""
        for yy in range(other.h):
            ty = y + yy
            if not 0 <= ty < self.h:
                continue
            for xx in range(other.w):
                tx = x + xx
                v = other.px[yy, xx]
                if v and 0 <= tx < self.w:
                    self.px[ty, tx] = v

    def flipped(self):
        out = Img(self.w, self.h)
        out.px = self.px[:, ::-1].copy()
        return out

    def recolor(self, mapping):
        lut = np.arange(256, dtype=np.uint8)
        for a, b in mapping.items():
            lut[a] = b
        self.px = lut[self.px]

    # ------------------------------------------------------------ output

    def to_rgba(self):
        lut = np.array(PALETTE + [(0, 0, 0, 0)] * (256 - len(PALETTE)), np.uint8)
        return Image.fromarray(lut[self.px], "RGBA")

    def save(self, path):
        self.to_rgba().save(path)


def shifted(mask, dx, dy, fill=False):
    """mask shifted so that out[y, x] = mask[y + dy, x + dx]; outside the image counts as `fill`."""
    h, w = mask.shape
    out = np.full((h, w), fill, bool)
    ys = slice(max(0, -dy), min(h, h - dy))
    xs = slice(max(0, -dx), min(w, w - dx))
    ys_src = slice(max(0, dy), min(h, h + dy))
    xs_src = slice(max(0, dx), min(w, w + dx))
    out[ys, xs] = mask[ys_src, xs_src]
    return out


def bresenham(x0, y0, x1, y1):
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
    err = dx + dy
    while True:
        yield x0, y0
        if x0 == x1 and y0 == y1:
            return
        e2 = 2 * err
        if e2 >= dy:
            err += dy
            x0 += sx
        if e2 <= dx:
            err += dx
            y0 += sy


def bezier(points, steps=24):
    pts = np.asarray(points, np.float32)
    out = []
    for i in range(steps + 1):
        t = i / steps
        p = pts.copy()
        while len(p) > 1:
            p = p[:-1] * (1 - t) + p[1:] * t
        out.append((float(p[0][0]), float(p[0][1])))
    return out


def ribbon(spine, w0, w1):
    """Polygon around a polyline, tapering from width w0 to w1 (horns, tails, tongues)."""
    spine = np.asarray(spine, np.float32)
    n = len(spine)
    left, right = [], []
    for i in range(n):
        a, b = spine[max(0, i - 1)], spine[min(n - 1, i + 1)]
        d = b - a
        d = d / (np.linalg.norm(d) + 1e-6)
        nrm = np.array([-d[1], d[0]])
        w = (w0 + (w1 - w0) * i / (n - 1)) / 2
        left.append(tuple(spine[i] + nrm * w))
        right.append(tuple(spine[i] - nrm * w))
    return left + right[::-1]


def sheet(frames):
    """Lays frames side by side into one horizontal strip."""
    w, h = frames[0].w, frames[0].h
    out = Img(w * len(frames), h)
    for i, f in enumerate(frames):
        out.px[:, i * w:(i + 1) * w] = f.px
    return out


def nine_slice_check(img, border):
    """Sanity check for 9-slice sources: the border must fit."""
    assert img.w > 2 * border and img.h > 2 * border, "9-slice border does not fit the image."
