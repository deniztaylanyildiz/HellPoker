"""Tiny procedural painting kit on top of Pillow + numpy.

Everything works on float RGB(A) arrays in 0..1 at a supersampled resolution; `finish` downsamples for antialiasing.
Shapes are drawn as soft masks, shaded with a fake height field (blurred mask -> normals -> lambert),
which gives flat vector shapes a lit, painted look.
"""
import math

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

RNG = np.random.default_rng(666)


# ---------------------------------------------------------------- canvas & masks

class Canvas:
    def __init__(self, width, height, supersample=2):
        self.ss = supersample
        self.w = width * supersample
        self.h = height * supersample
        self.rgb = np.zeros((self.h, self.w, 3), np.float32)
        self.alpha = np.ones((self.h, self.w), np.float32)

    # Coordinates are given in final-image units (0..width) and scaled here.
    def s(self, value):
        return value * self.ss

    def pts(self, points):
        return [(x * self.ss, y * self.ss) for x, y in points]

    def mask(self, draw_fn, blur=0.0):
        img = Image.new("L", (self.w, self.h), 0)
        draw_fn(ImageDraw.Draw(img), self)
        if blur > 0:
            img = img.filter(ImageFilter.GaussianBlur(blur * self.ss))
        return np.asarray(img, np.float32) / 255.0

    def ellipse(self, cx, cy, rx, ry, blur=0.0):
        return self.mask(lambda d, c: d.ellipse([c.s(cx - rx), c.s(cy - ry), c.s(cx + rx), c.s(cy + ry)], fill=255), blur)

    def polygon(self, points, blur=0.0):
        return self.mask(lambda d, c: d.polygon(c.pts(points), fill=255), blur)

    def line(self, points, width, blur=0.0):
        return self.mask(lambda d, c: d.line(c.pts(points), fill=255, width=int(c.s(width)), joint="curve"), blur)

    def lay(self, color, mask, opacity=1.0):
        """Paints a flat colour or a colour field through a mask."""
        m = np.clip(mask * opacity, 0, 1)[..., None]
        color = np.asarray(color, np.float32)
        self.rgb = self.rgb * (1 - m) + color * m

    def add(self, color, mask, opacity=1.0):
        """Additive light (glows, rim light)."""
        self.rgb = self.rgb + np.asarray(color, np.float32) * (np.clip(mask * opacity, 0, None))[..., None]

    def multiply(self, factor_field):
        self.rgb = self.rgb * np.asarray(factor_field, np.float32)[..., None]

    def finish(self, path, size=None, with_alpha=False):
        rgb = np.clip(self.rgb, 0, 1)
        if with_alpha:
            arr = np.dstack([rgb, np.clip(self.alpha, 0, 1)])
            img = Image.fromarray((arr * 255 + 0.5).astype(np.uint8), "RGBA")
        else:
            img = Image.fromarray((rgb * 255 + 0.5).astype(np.uint8), "RGB")
        target = size or (self.w // self.ss, self.h // self.ss)
        img = img.resize(target, Image.LANCZOS)
        img.save(path)
        return img


# ---------------------------------------------------------------- fields

def blur(field, radius):
    """Gaussian blur of a 0..1 float field (radius in supersampled pixels)."""
    if radius <= 0:
        return field
    # Three box blurs ≈ a gaussian; done in float so shading normals do not band.
    out = np.asarray(field, np.float32)
    box = max(1, int(round(radius * 1.15)))
    for _ in range(3):
        out = _box(out, box, 0)
        out = _box(out, box, 1)
    return out


def _box(a, r, axis):
    pad = [(0, 0), (0, 0)]
    pad[axis] = (r + 1, r)
    p = np.pad(a, pad, mode="edge")
    cs = np.cumsum(p, axis=axis, dtype=np.float64)
    n = a.shape[axis]
    hi = np.take(cs, np.arange(2 * r + 1, 2 * r + 1 + n), axis=axis)
    lo = np.take(cs, np.arange(0, n), axis=axis)
    return ((hi - lo) / (2 * r + 1)).astype(np.float32)


def coords(c):
    ys, xs = np.mgrid[0:c.h, 0:c.w].astype(np.float32)
    return xs / c.ss, ys / c.ss


def radial(c, cx, cy, radius, power=1.0):
    xs, ys = coords(c)
    d = np.sqrt((xs - cx) ** 2 + (ys - cy) ** 2) / radius
    return np.clip(1 - d, 0, 1) ** power


def linear_y(c, y0, y1):
    _, ys = coords(c)
    return np.clip((ys - y0) / (y1 - y0), 0, 1)


def noise(c, scale, octaves=4, seed=None):
    """Fractal value noise in 0..1, `scale` = size of the largest blobs in final pixels."""
    rng = np.random.default_rng(seed) if seed is not None else RNG
    total = np.zeros((c.h, c.w), np.float32)
    amp, norm = 1.0, 0.0
    cell = scale * c.ss
    for _ in range(octaves):
        gw = max(2, int(c.w / cell) + 2)
        gh = max(2, int(c.h / cell) + 2)
        grid = rng.random((gh, gw)).astype(np.float32)
        img = Image.fromarray((grid * 255).astype(np.uint8), "L").resize((int(gw * cell), int(gh * cell)), Image.BICUBIC)
        layer = np.asarray(img, np.float32)[: c.h, : c.w] / 255.0
        if layer.shape != (c.h, c.w):
            layer = np.pad(layer, ((0, c.h - layer.shape[0]), (0, c.w - layer.shape[1])), mode="edge")
        # Soften the bicubic grid seams a little.
        total += blur(layer, max(1, cell * 0.25)) * amp
        norm += amp
        amp *= 0.5
        cell = max(2.0, cell / 2)
    return total / norm


def shade(mask, radius, light=(-0.55, -0.75), strength=6.0, ambient=0.35):
    """Fake 3D lighting for a flat mask: returns a 0..~1.4 brightness field."""
    height = blur(mask, radius)
    gy, gx = np.gradient(height)
    nx, ny, nz = -gx * strength * radius, -gy * strength * radius, np.ones_like(height)
    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    lx, ly = light
    lz = math.sqrt(max(0.0, 1 - lx * lx - ly * ly))
    lambert = np.clip((nx * lx + ny * ly + nz * lz) / length, 0, 1)
    return ambient + (1 - ambient) * lambert * 1.25


def rim(mask, radius, direction=(1, 0)):
    """Thin light along the edge of a mask facing `direction` (screen space)."""
    inner = blur(mask, radius)
    edge = np.clip(mask - inner, 0, 1) * 2.5
    gy, gx = np.gradient(inner)
    facing = np.clip(-(gx * direction[0] + gy * direction[1]) * radius * 4, 0, 1)
    return np.clip(edge * facing, 0, 1)


def glow(mask, radius, gain=1.0):
    return np.clip(blur(mask, radius) * gain, 0, 1)


def mix(a, b, t):
    t = np.asarray(t, np.float32)
    if t.ndim == 2:
        t = t[..., None]
    return np.asarray(a, np.float32) * (1 - t) + np.asarray(b, np.float32) * t


def rgb(hex_color):
    hex_color = hex_color.lstrip("#")
    return np.array([int(hex_color[i:i + 2], 16) / 255.0 for i in (0, 2, 4)], np.float32)


# ---------------------------------------------------------------- shape helpers

def bezier(points, steps=40):
    """Quadratic/cubic Bezier through control points -> polyline."""
    pts = np.asarray(points, np.float32)
    out = []
    for i in range(steps + 1):
        t = i / steps
        p = pts.copy()
        while len(p) > 1:
            p = p[:-1] * (1 - t) + p[1:] * t
        out.append(tuple(p[0]))
    return out


def ribbon(spine, widths):
    """A tapered shape around a polyline spine (for horns, hair strands). widths: start..end."""
    spine = np.asarray(spine, np.float32)
    n = len(spine)
    left, right = [], []
    for i in range(n):
        a = spine[max(0, i - 1)]
        b = spine[min(n - 1, i + 1)]
        d = b - a
        d = d / (np.linalg.norm(d) + 1e-6)
        normal = np.array([-d[1], d[0]])
        t = i / (n - 1)
        w = widths[0] * (1 - t) + widths[1] * t
        left.append(tuple(spine[i] + normal * w / 2))
        right.append(tuple(spine[i] - normal * w / 2))
    return left + right[::-1]


def mirror(points, axis_x):
    return [(2 * axis_x - x, y) for x, y in points]
