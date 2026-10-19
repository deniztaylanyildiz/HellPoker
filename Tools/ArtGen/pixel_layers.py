"""Layered backdrops: one still picture, small looping sprite strips laid over it, and particles left to the game's code.

A full-screen strip of frames can only hold four 480 px frames (the importer's 2048 px limit), so motion that big skips
and jumps. Instead the backdrop is one still frame, and each moving thing gets its own small strip of 8-12 frames played at
8-12 FPS, moving at most a pixel or two per frame. Embers and motes are drawn by the game (a pixel particle system).

Rules every layer is checked against when written:
- a loop closes seamlessly: the jump from the last frame back to the first is no bigger than the biggest step between two
  neighbouring frames (periodic motion uses phase = 2π · frame / frames);
- a strip fits the importer (frames × width ≤ 2048 px).

Each backdrop folder gets a manifest (motion.txt) the game reads:
    layer <file> <x> <y> <frames> <fps> <repeat> <drift> <wrap> <offset>
    particles <kind> <x> <y> <w> <h> <count>
<file> is the strip's name without extension; a variant's own strip is <file>_<variant> (the normal look has no suffix).
<repeat> lays copies side by side; <drift> moves the layer sideways in pixels per second, wrapping every <wrap> pixels;
<offset> starts the loop that many frames in (so a row of glints does not blink all at once).
"""
import copy
import math
import os

import numpy as np

from pixel import Img, sheet

MAX_STRIP = 2048


def phase(frame, frames):
    """Where a periodic motion is at this frame: 0 .. 2π, closing exactly on the loop."""
    return 2 * math.pi * frame / frames


class Layer:
    """A small looping strip at (x, y) on the 480×270 screen; draw(img, frame) paints one w×h frame (transparent background)."""

    def __init__(self, name, x, y, w, h, frames, fps, draw, repeat=1, drift=0, wrap=0, offset=0, vignette=None):
        assert 8 <= frames <= 12 or frames == 1, f"{name}: a layer loops in 8-12 frames ({frames})."
        assert frames * w <= MAX_STRIP, f"{name}: {frames} × {w} px is wider than {MAX_STRIP} px."
        self.name, self.x, self.y, self.w, self.h = name, x, y, w, h
        self.frames, self.fps, self.draw = frames, fps, draw
        self.repeat, self.drift, self.wrap, self.offset = repeat, drift, wrap, offset
        self.vignette = vignette

    def render(self):
        out = []
        for f in range(self.frames):
            img = Img(self.w, self.h)
            self.draw(img, f)
            if self.vignette:
                self.vignette(img, self.x, self.y)
            out.append(img)
        check_loop(self.name, out)
        return out

    def manifest(self, prefix=""):
        return (f"layer {prefix}{self.name} {self.x} {self.y} {self.frames} {self.fps} {self.repeat} {self.drift} {self.wrap} "
                f"{self.offset}")


class Particles:
    """Pixels the game moves itself (embers, motes) inside a rectangle of the screen."""

    def __init__(self, kind, x, y, w, h, count):
        self.kind, self.x, self.y, self.w, self.h, self.count = kind, x, y, w, h, count

    def manifest(self):
        return f"particles {self.kind} {self.x} {self.y} {self.w} {self.h} {self.count}"


# A truly periodic motion's closing step is just one more step, but its changed-pixel count still differs a little from
# the others (thresholds and fades quantize a little differently at every phase): 2% of slack, no more.
LOOP_SLACK = 1.02


def check_loop(name, frames):
    """The last frame must lead back into the first as smoothly as any frame leads into the next."""
    if len(frames) < 2:
        return
    steps = [int(np.count_nonzero(a.px != b.px)) for a, b in zip(frames, frames[1:])]
    closing = int(np.count_nonzero(frames[-1].px != frames[0].px))
    if closing > max(steps) * LOOP_SLACK:
        raise AssertionError(f"{name}: the loop jumps ({closing} px change back to frame 0, at most {max(steps)} between frames).")


def in_zones(zones, x0, y0, w, h):
    """Mask (h×w) of the pixels of a box at (x0, y0) that fall inside any text zone (x0, y0, x1, y1 inclusive)."""
    ys, xs = np.mgrid[y0:y0 + h, x0:x0 + w]
    mask = np.zeros((h, w), bool)
    for zx0, zy0, zx1, zy1 in zones:
        mask |= (xs >= zx0) & (xs <= zx1) & (ys >= zy0) & (ys <= zy1)
    return mask


def keep_out_of_text(base, layer, frames, zones):
    """
    Nothing moves behind words: inside a text zone the layer is cleared, and the still base takes the layer's first frame
    there instead — the zone shows the layer, standing still. (Drifting layers cross the screen; they keep to the sky.)
    """
    if not zones or layer.drift:
        return frames
    zone = in_zones(zones, layer.x, layer.y, layer.w, layer.h)
    if not zone.any():
        return frames
    first = frames[0].px
    sy, sx = slice(max(0, layer.y), min(base.h, layer.y + layer.h)), slice(max(0, layer.x), min(base.w, layer.x + layer.w))
    ly, lx = slice(sy.start - layer.y, sy.stop - layer.y), slice(sx.start - layer.x, sx.stop - layer.x)
    patch = zone[ly, lx] & (first[ly, lx] != 0)
    base.px[sy, sx] = np.where(patch, first[ly, lx], base.px[sy, sx])
    for frame in frames:
        frame.px[zone] = 0
    return frames


# ---------------------------------------------------------------- Phase 3: a 480×270 scene widened to the 640×360 screen
#
# A hall is drawn on the 480×270 grid it was designed on, then widened without resampling: a 160 px band of columns in its dark
# middle and a 90 px band of rows where its sides are plain wall are each laid down twice. Every pixel stays a whole pixel, the
# edges keep their character (the left side stays left, the right side moves right, the bottom moves down), and the screen's
# readability darkening is laid over the widened picture afterwards, measured from the 640×360 table.

WIDE_W, WIDE_H = 640, 360
COLUMN_BAND = (160, 160)   # the middle's columns 160..319 twice: the old middle (240) is the new one (320)


def best_row_band(px, add=90, sides=((0, 150), (330, 480))):
    """The 90 rows whose doubling shows least: the sides are plain there (a row like the one 18 above: the walls' courses) and the
    seam at both ends of the band runs on smoothly."""
    side = np.concatenate([px[:, a:b] for a, b in sides], axis=1).astype(np.int32)
    h = px.shape[0]
    best = None
    for start in range(20, h - add - 10):
        seam = np.count_nonzero(side[start + add] != side[start]) + np.count_nonzero(side[start - 1] != side[start + add - 1])
        busy = sum(np.count_nonzero(side[y] != side[y - 18]) for y in range(start, start + add, 3))
        score = seam * 2 + busy
        if best is None or score < best[0]:
            best = (score, start)
    return best[1], add


def _new_index(n, band):
    """The old row (or column) shown at the widened picture's row n."""
    start, add = band
    return n if n < start + add else n - add


def _span(pos, size, band):
    """Where an old span [pos, pos + size) lands once its band is doubled: the widened span (it grows if it reaches into the band)."""
    start, add = band
    lo = pos if pos < start + add else pos + add
    hi = pos + size if pos + size <= start else pos + size + add
    return lo, hi - lo


def widen_px(px, cols, rows):
    """The whole picture widened: the bands' columns and rows laid down twice."""
    out = px
    if cols:
        index = [_new_index(n, cols) for n in range(px.shape[1] + cols[1])]
        out = out[:, index]
    if rows:
        index = [_new_index(n, rows) for n in range(px.shape[0] + rows[1])]
        out = out[index, :]
    return out


def _remap(layer, frames, x, w, y, h, col_of):
    """The layer's frames on a new box (x, y, w, h): every new pixel takes the old one its row and column show (col_of: the old column)."""
    out = []
    for frame in frames:
        img = Img(w, h)
        for ny in range(h):
            oy = _new_index(y + ny, layer.rows) - layer.y
            if not 0 <= oy < layer.h:
                continue
            for nx in range(w):
                ox = col_of(x + nx) - layer.x
                if 0 <= ox < layer.w:
                    img.px[ny, nx] = frame.px[oy, ox]
        out.append(img)
    return out


def widen_layer(layer, frames, cols, rows):
    """
    A layer's frames widened like the picture under it, as one layer or two: rows are simply laid down twice (a strip's height is
    free); a layer reaching into the columns' band is cut in two — its left copy keeps its place, its right copy moves right by the
    band — so no strip grows wider. A drifting layer keeps its width and wraps further.
    """
    layer.rows = rows
    y, h = _span(layer.y, layer.h, rows)
    start, add = cols
    if layer.drift:
        out = _remap(layer, frames, layer.x, layer.w, y, h, lambda n: n)
        layer.y, layer.h = y, h
        if layer.wrap:
            layer.wrap += add
        return [(layer, out)]
    x0, x1 = layer.x, layer.x + layer.w
    parts = []
    if x0 < start + add:   # the left copy: columns as they were (the band's first copy included)
        lx1 = min(x1, start + add)
        parts.append((x0, lx1 - x0, lambda n: n, ""))
    if x1 > start:   # the right copy: the band's second copy and everything right of it, moved right
        rx0 = max(x0, start) + add
        parts.append((rx0, x1 + add - rx0, lambda n: n - add, "_b" if x0 < start + add else ""))
    result = []
    for x, w, col_of, suffix in parts:
        out = _remap(layer, frames, x, w, y, h, col_of)
        part = copy.copy(layer)
        part.name, part.x, part.y, part.w, part.h = layer.name + suffix, x, y, w, h
        result.append((part, out))
    return result


def widen_particles(p, cols, rows):
    p.x, p.w = _span(p.x, p.w, cols)
    p.y, p.h = _span(p.y, p.h, rows)
    return p


def write(folder, base, layers, particles=(), variants=None, base_name=None, layer_prefix="", manifest_name="motion", text_zones=(),
          widen=False, darken=None):
    """
    Writes the still base for every variant (<variant>.png — or <base_name>.png when there is a single look), each
    layer's strip per variant (<layer_prefix><name>[_<variant>].png) and the manifest (<manifest_name>.txt).
    `variants` maps a variant name to a palette lookup table (None for the normal look); `text_zones` are the screen boxes
    where words sit (nothing moves there). Phase 3: `widen` takes a 480×270 scene to 640×360 (see above; `text_zones` are then
    the 640×360 screen's) and `darken(img, ox, oy)` lays the readability darkening over the widened picture and every layer.
    Returns the paths written.
    """
    os.makedirs(folder, exist_ok=True)
    variants = variants or {"normal": None}
    written = []
    rendered = [(layer, layer.render()) for layer in layers]
    if widen:
        # widen=True: the band chosen by the picture; a number: the band's first row, chosen by eye.
        cols, rows = COLUMN_BAND, (best_row_band(base.px) if widen is True else (int(widen), 90))
        wide = Img(base.w + cols[1], base.h + rows[1])
        wide.px = widen_px(base.px, cols, rows)
        base = wide
        rendered = [part for layer, frames in rendered for part in widen_layer(layer, frames, cols, rows)]
        layers = [layer for layer, _ in rendered]
        particles = [widen_particles(p, cols, rows) for p in particles]
    if darken:
        darken(base, 0, 0)
        for layer, frames in rendered:
            for frame in frames:
                darken(frame, layer.x, layer.y)
    rendered = [(layer, keep_out_of_text(base, layer, frames, text_zones)) for layer, frames in rendered]
    for variant, table in variants.items():
        img = Img(base.w, base.h)
        img.px = table[base.px] if table is not None else base.px
        path = os.path.join(folder, (base_name or variant) + ".png")
        img.save(path)
        written.append(path)
    for layer, frames in rendered:
        strip = sheet(frames)
        for variant, table in variants.items():
            out = Img(strip.w, strip.h)
            out.px = table[strip.px] if table is not None else strip.px
            suffix = "" if variant == "normal" else "_" + variant
            path = os.path.join(folder, f"{layer_prefix}{layer.name}{suffix}.png")
            out.save(path)
            written.append(path)
    manifest = os.path.join(folder, manifest_name + ".txt")
    with open(manifest, "w", encoding="utf-8", newline="\n") as f:
        f.write("# Hell Poker backdrop motion (Tools/ArtGen). Generated: do not edit.\n")
        for layer in layers:
            f.write(layer.manifest(layer_prefix) + "\n")
        for p in particles:
            f.write(p.manifest() + "\n")
    written.append(manifest)
    return written
