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


def write(folder, base, layers, particles=(), variants=None, base_name=None, layer_prefix="", manifest_name="motion", text_zones=()):
    """
    Writes the still base for every variant (<variant>.png — or <base_name>.png when there is a single look), each
    layer's strip per variant (<layer_prefix><name>[_<variant>].png) and the manifest (<manifest_name>.txt).
    `variants` maps a variant name to a palette lookup table (None for the normal look); `text_zones` are the screen boxes
    where words sit (nothing moves there). Returns the paths written.
    """
    os.makedirs(folder, exist_ok=True)
    variants = variants or {"normal": None}
    written = []
    rendered = [(layer, keep_out_of_text(base, layer, layer.render(), text_zones)) for layer in layers]
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
