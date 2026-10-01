"""Gothic-casino UI art: backdrop, table, frames, buttons, chips, cards, suits, title logo."""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

from paint import Canvas, bezier, blur, glow, linear_y, mix, noise, radial, rgb, shade

GOLD = rgb("#e8b84a")
GOLD_LIGHT = rgb("#fff0b0")
GOLD_DARK = rgb("#5a3408")


def rounded(c, x0, y0, x1, y1, r, blur_px=0.0):
    return c.mask(lambda d, cc: d.rounded_rectangle([cc.s(x0), cc.s(y0), cc.s(x1), cc.s(y1)], radius=cc.s(r), fill=255), blur_px)


def gilded(c, m, radius=4, ambient=0.35):
    """Paints a mask as polished gold."""
    sh = shade(m, radius * c.ss, light=(-0.5, -0.8), strength=6, ambient=ambient)
    col = mix(GOLD_DARK, GOLD, np.clip(sh, 0, 1)) + GOLD_LIGHT * np.clip(sh - 1.0, 0, 1)[..., None] * 1.5
    c.lay(col, m)


def transparent(c):
    c.alpha = np.zeros((c.h, c.w), np.float32)


def stamp(c, m, color=None, painter=None):
    """Lays colour and adds the mask to the alpha channel (for RGBA sprites)."""
    if painter is not None:
        painter(m)
    elif color is not None:
        c.lay(color, m)
    c.alpha = np.clip(c.alpha + m * (1 - c.alpha), 0, 1)


def unpremultiply_finish(c, path, size=None):
    """Colour under transparent pixels bleeds when filtered; flood it with the nearest opaque colour first."""
    a = c.alpha
    solid = blur(a, 6 * c.ss) + 1e-4
    bleed = np.stack([blur(c.rgb[..., i] * a, 6 * c.ss) for i in range(3)], -1) / solid[..., None]
    c.rgb = np.where(a[..., None] > 0.01, c.rgb, bleed)
    c.finish(path, size, with_alpha=True)


# ------------------------------------------------------------------ backdrop

def background(path):
    w, h = 1920, 1080
    c = Canvas(w, h, supersample=1)
    t = radial(c, w * 0.42, h * 0.5, 1150, power=1.4)
    c.rgb[:] = mix(rgb("#050101"), rgb("#3a0806"), t)

    smoke = noise(c, 260, octaves=5, seed=5)
    c.lay(rgb("#6a140a"), np.clip(smoke - 0.42, 0, 1) * 2.0 * t, 0.5)

    # Gothic arcade: pointed arches and pillars in silhouette.
    arches = np.zeros((c.h, c.w), np.float32)
    for i in range(-1, 8):
        x = i * 300 + 60
        pillar = c.polygon([(x - 34, h), (x - 28, 300), (x + 28, 300), (x + 34, h)])
        arch_outer = c.polygon(bezier([(x + 28, 330), (x + 40, 120), (x + 150, 40)], 20) + bezier([(x + 150, 40), (x + 260, 120), (x + 272, 330)], 20)
                               + [(x + 272, 330), (x + 272, 0), (x + 28, 0)])
        arch_inner = c.polygon(bezier([(x + 48, 340), (x + 58, 150), (x + 150, 74)], 20) + bezier([(x + 150, 74), (x + 242, 150), (x + 252, 340)], 20)
                               + [(x + 252, 1100), (x + 48, 1100)])
        arches = np.clip(arches + pillar + np.clip(arch_outer - arch_inner, 0, 1), 0, 1)
    c.lay(rgb("#0a0202"), arches * (0.55 + 0.45 * (1 - t)), 0.9)
    c.add(rgb("#ff4a10"), np.clip(arches - blur(arches, 6), 0, 1) * t, 0.5)

    # Hellfire glow rising from the floor.
    floor = linear_y(c, h * 0.55, h) ** 2.2
    flames = noise(c, 70, octaves=4, seed=9)
    c.add(rgb("#ff3a08"), floor * np.clip(flames * 1.3 - 0.3, 0, 1), 0.55)
    c.add(rgb("#ffaa30"), floor ** 3 * np.clip(flames - 0.5, 0, 1), 0.6)

    t2 = radial(c, w * 0.5, h * 0.5, 1250, power=0.8)
    c.multiply(0.35 + 0.65 * t2)
    c.finish(path)


def table(path):
    w, h = 1300, 860
    c = Canvas(w, h, supersample=1)
    transparent(c)
    outer = rounded(c, 10, 10, w - 10, h - 10, 300)
    rail = outer
    wood = noise(c, 40, octaves=4, seed=14)
    grain = (np.sin(np.mgrid[0:c.h, 0:c.w][0] / 3.0 + wood * 18) * 0.5 + 0.5)
    rail_col = mix(rgb("#1a0805"), rgb("#4a1e10"), grain * 0.6 + wood * 0.4) * shade(outer, 30, ambient=0.45)[..., None]
    stamp(c, rail, rail_col)

    inlay = np.clip(rounded(c, 48, 48, w - 48, h - 48, 262) - rounded(c, 54, 54, w - 54, h - 54, 256), 0, 1)
    gilded(c, inlay, 2)

    felt = rounded(c, 70, 70, w - 70, h - 70, 240)
    velvet = noise(c, 3, octaves=2, seed=3) * 0.5 + noise(c, 60, octaves=3, seed=4) * 0.5
    spot = radial(c, w * 0.5, h * 0.48, 760, power=1.2)
    felt_col = mix(rgb("#1e0303"), rgb("#7a0d0b"), spot) * (0.85 + 0.3 * velvet)[..., None]
    c.lay(felt_col, felt)
    c.lay(rgb("#050000"), np.clip(felt - blur(felt, 30), 0, 1), 0.7)

    # A faint sigil embossed in the felt.
    sigil = np.zeros((c.h, c.w), np.float32)
    cx, cy, r = w / 2, h / 2, 230
    ring = np.clip(c.ellipse(cx, cy, r, r) - c.ellipse(cx, cy, r - 5, r - 5), 0, 1)
    ring2 = np.clip(c.ellipse(cx, cy, r - 22, r - 22) - c.ellipse(cx, cy, r - 25, r - 25), 0, 1)
    star = [(cx + (r - 24) * math.sin(math.radians(144 * k)), cy - (r - 24) * math.cos(math.radians(144 * k))) for k in range(6)]
    lines = c.line(star, 4)
    sigil = np.clip(ring + ring2 + lines, 0, 1)
    c.lay(rgb("#160202"), sigil * felt, 0.35)
    c.add(rgb("#ff5a2a"), np.clip(sigil - blur(sigil, 3), 0, 1) * felt, 0.08)

    unpremultiply_finish(c, path)


# ------------------------------------------------------------------ frames & panels (9-slice)

def frame(path, size=192):
    c = Canvas(size, size, supersample=4)
    transparent(c)
    m = np.clip(rounded(c, 4, 4, size - 4, size - 4, 14) - rounded(c, 18, 18, size - 18, size - 18, 6), 0, 1)
    inner = np.clip(rounded(c, 24, 24, size - 24, size - 24, 4) - rounded(c, 27, 27, size - 27, size - 27, 3), 0, 1)
    corners = np.zeros((c.h, c.w), np.float32)
    for (x, y) in ((0, 0), (size, 0), (0, size), (size, size)):
        sx = 1 if x == 0 else -1
        sy = 1 if y == 0 else -1
        corners += c.ellipse(x + sx * 22, y + sy * 22, 14, 14)
        corners += c.polygon([(x + sx * 8, y + sy * 44), (x + sx * 22, y + sy * 22), (x + sx * 44, y + sy * 8), (x + sx * 30, y + sy * 30)])
    shape = np.clip(m + inner + corners, 0, 1)
    stamp(c, shape, painter=lambda mm: gilded(c, mm, 3))
    for (x, y) in ((0, 0), (size, 0), (0, size), (size, size)):
        sx = 1 if x == 0 else -1
        sy = 1 if y == 0 else -1
        gem = c.ellipse(x + sx * 22, y + sy * 22, 6, 6)
        c.lay(rgb("#b0101a") * shade(gem, 8, ambient=0.5)[..., None], gem)
    unpremultiply_finish(c, path)


def panel(path, size=128):
    c = Canvas(size, size, supersample=4)
    transparent(c)
    body = rounded(c, 2, 2, size - 2, size - 2, 12)
    tex = noise(c, 10, octaves=3, seed=21)
    stamp(c, body, mix(rgb("#120404"), rgb("#2a0806"), tex))
    border = np.clip(rounded(c, 2, 2, size - 2, size - 2, 12) - rounded(c, 6, 6, size - 6, size - 6, 9), 0, 1)
    gilded(c, border, 2)
    thin = np.clip(rounded(c, 11, 11, size - 11, size - 11, 6) - rounded(c, 12.5, 12.5, size - 12.5, size - 12.5, 5), 0, 1)
    c.lay(GOLD * 0.7, thin, 0.8)
    unpremultiply_finish(c, path)


def button(path, top, bottom, w=256, h=96):
    c = Canvas(w, h, supersample=4)
    transparent(c)
    body = rounded(c, 2, 2, w - 2, h - 2, 18)
    grad = linear_y(c, 6, h - 6)
    fill = mix(top, bottom, grad) * shade(body, 20, ambient=0.75)[..., None]
    stamp(c, body, fill)
    c.add(np.array([1, 1, 1]), rounded(c, 10, 8, w - 10, h * 0.42, 12, 2), 0.08)
    border = np.clip(body - rounded(c, 6, 6, w - 6, h - 6, 14), 0, 1)
    gilded(c, border, 2, ambient=0.45)
    unpremultiply_finish(c, path)


def chip(path, body_color, edge_color, size=128):
    c = Canvas(size, size, supersample=4)
    transparent(c)
    r = size / 2 - 3
    disc = c.ellipse(size / 2, size / 2, r, r)
    stamp(c, disc, body_color * shade(disc, 12, ambient=0.6)[..., None])
    xs, ys = np.mgrid[0:c.h, 0:c.w][::-1].astype(np.float32) / c.ss
    ang = np.arctan2(ys - size / 2, xs - size / 2)
    stripes = (np.sin(ang * 8) > 0.55).astype(np.float32)
    edge = np.clip(disc - c.ellipse(size / 2, size / 2, r - 12, r - 12), 0, 1)
    c.lay(edge_color, edge * stripes, 0.95)
    ring = np.clip(c.ellipse(size / 2, size / 2, r - 18, r - 18) - c.ellipse(size / 2, size / 2, r - 22, r - 22), 0, 1)
    gilded(c, ring, 1)
    c.add(np.array([1, 1, 1]), c.ellipse(size / 2 - 14, size / 2 - 18, 22, 12, 8), 0.12)
    unpremultiply_finish(c, path)


# ------------------------------------------------------------------ cards

CARD_W, CARD_H = 320, 448


def card_face(path):
    c = Canvas(CARD_W, CARD_H, supersample=2)
    transparent(c)
    body = rounded(c, 2, 2, CARD_W - 2, CARD_H - 2, 22)
    tex = noise(c, 30, octaves=4, seed=41)
    paper = mix(rgb("#d8c8a2"), rgb("#f6ecd4"), radial(c, CARD_W / 2, CARD_H / 2, 300, 0.6)) * (0.93 + 0.1 * tex)[..., None]
    stamp(c, body, paper)
    c.lay(rgb("#6a4a20"), np.clip(body - blur(body, 14), 0, 1), 0.45)
    border = np.clip(rounded(c, 14, 14, CARD_W - 14, CARD_H - 14, 12) - rounded(c, 17, 17, CARD_W - 17, CARD_H - 17, 10), 0, 1)
    c.lay(rgb("#8a5a1a"), border, 0.65)
    unpremultiply_finish(c, path)


def card_back(path):
    c = Canvas(CARD_W, CARD_H, supersample=2)
    transparent(c)
    body = rounded(c, 2, 2, CARD_W - 2, CARD_H - 2, 22)
    stamp(c, body, rgb("#2a0405"))
    inner = rounded(c, 18, 18, CARD_W - 18, CARD_H - 18, 12)
    xs, ys = np.mgrid[0:c.h, 0:c.w][::-1].astype(np.float32) / c.ss
    lattice = ((np.sin((xs + ys) / 9.0) > 0.86) | (np.sin((xs - ys) / 9.0) > 0.86)).astype(np.float32)
    c.lay(rgb("#6a0a0c"), inner)
    c.lay(rgb("#3a0406"), inner * lattice, 0.9)
    c.lay(rgb("#120102"), np.clip(inner - blur(inner, 20), 0, 1), 0.7)
    frame_line = np.clip(inner - rounded(c, 22, 22, CARD_W - 22, CARD_H - 22, 10), 0, 1)
    gilded(c, frame_line, 1)
    # Gold pentacle medallion.
    cx, cy, r = CARD_W / 2, CARD_H / 2, 78
    disc = c.ellipse(cx, cy, r + 8, r + 8)
    c.lay(rgb("#1a0203"), disc)
    ring = np.clip(c.ellipse(cx, cy, r, r) - c.ellipse(cx, cy, r - 6, r - 6), 0, 1)
    star = [(cx + (r - 6) * math.sin(math.radians(144 * k)), cy - (r - 6) * math.cos(math.radians(144 * k))) for k in range(6)]
    gilded(c, np.clip(ring + c.line(star, 4), 0, 1), 1)
    c.add(rgb("#ff7a2a"), glow(disc, 30, 0.5) * (1 - disc), 0.35)
    unpremultiply_finish(c, path)


def card_slot(path):
    c = Canvas(CARD_W, CARD_H, supersample=2)
    transparent(c)
    body = rounded(c, 2, 2, CARD_W - 2, CARD_H - 2, 22)
    stamp(c, body * 0.35, rgb("#0a0101"))
    edge = np.clip(body - rounded(c, 6, 6, CARD_W - 6, CARD_H - 6, 19), 0, 1)
    c.lay(GOLD * 0.5, edge)
    c.alpha = np.clip(c.alpha + edge * 0.5, 0, 1)
    unpremultiply_finish(c, path)


def suit(path, kind, size=128):
    """White suit glyphs, tinted in Unity."""
    c = Canvas(size, size, supersample=4)
    transparent(c)
    s = size
    if kind == "hearts":
        lobes = c.ellipse(s * 0.31, s * 0.36, s * 0.22, s * 0.22) + c.ellipse(s * 0.69, s * 0.36, s * 0.22, s * 0.22)
        point = c.polygon([(s * 0.1, s * 0.45), (s * 0.5, s * 0.92), (s * 0.9, s * 0.45), (s * 0.5, s * 0.4)])
        m = np.clip(lobes + point, 0, 1)
    elif kind == "diamonds":
        m = c.polygon(bezier([(s * 0.5, s * 0.04), (s * 0.68, s * 0.3), (s * 0.88, s * 0.5)], 12) +
                      bezier([(s * 0.88, s * 0.5), (s * 0.68, s * 0.7), (s * 0.5, s * 0.96)], 12) +
                      bezier([(s * 0.5, s * 0.96), (s * 0.32, s * 0.7), (s * 0.12, s * 0.5)], 12) +
                      bezier([(s * 0.12, s * 0.5), (s * 0.32, s * 0.3), (s * 0.5, s * 0.04)], 12))
    elif kind == "spades":
        lobes = c.ellipse(s * 0.31, s * 0.56, s * 0.21, s * 0.2) + c.ellipse(s * 0.69, s * 0.56, s * 0.21, s * 0.2)
        point = c.polygon([(s * 0.11, s * 0.5), (s * 0.5, s * 0.04), (s * 0.89, s * 0.5), (s * 0.5, s * 0.66)])
        stem = c.polygon([(s * 0.5, s * 0.55), (s * 0.66, s * 0.95), (s * 0.34, s * 0.95)])
        m = np.clip(lobes + point + stem, 0, 1)
    else:  # clubs
        r = s * 0.19
        m = c.ellipse(s * 0.5, s * 0.28, r, r) + c.ellipse(s * 0.28, s * 0.58, r, r) + c.ellipse(s * 0.72, s * 0.58, r, r)
        m += c.ellipse(s * 0.5, s * 0.5, r * 0.6, r * 0.6)
        m += c.polygon([(s * 0.5, s * 0.5), (s * 0.66, s * 0.95), (s * 0.34, s * 0.95)])
        m = np.clip(m, 0, 1)
    sh = np.clip(shade(m, 8 * c.ss / 4, ambient=0.75), 0.75, 1.15)
    stamp(c, m, np.ones(3, np.float32) * np.clip(sh, 0, 1)[..., None])
    unpremultiply_finish(c, path)


# ------------------------------------------------------------------ title & ornaments

def title_logo(path, font_path, text="HELL   POKER", width=1500, height=380):
    font = ImageFont.truetype(font_path, 190)
    probe = ImageDraw.Draw(Image.new("L", (10, 10)))
    box = probe.textbbox((0, 0), text, font=font)
    tw, th = box[2] - box[0], box[3] - box[1]
    c = Canvas(width, height, supersample=1)
    transparent(c)
    img = Image.new("L", (c.w, c.h), 0)
    ImageDraw.Draw(img).text(((c.w - tw) / 2 - box[0], (c.h - th) / 2 - box[1]), text, font=font, fill=255)
    m = np.asarray(img, np.float32) / 255.0

    # Fade the halo out well before the image edge, so it never shows a rectangle.
    xs, ys = np.mgrid[0:c.h, 0:c.w][::-1].astype(np.float32)
    edge = np.clip(np.minimum(np.minimum(xs, c.w - 1 - xs), np.minimum(ys, c.h - 1 - ys)) / 50.0, 0, 1)
    halo = glow(m, 16, 1.3) * edge
    c.rgb[:] = rgb("#ff4a0a")
    c.alpha = np.clip(halo * 0.7, 0, 1)
    fire = noise(c, 30, octaves=3, seed=77)
    c.add(rgb("#ffa020"), halo * fire, 0.4)

    grad = linear_y(c, (c.h - th) / 2, (c.h + th) / 2)
    face = mix(GOLD_LIGHT, rgb("#c0661a"), grad) * shade(m, 5, ambient=0.55)[..., None]
    c.lay(rgb("#1a0402"), np.clip(blur(m, 3) * 1.6, 0, 1))
    c.lay(face, m)
    c.alpha = np.clip(c.alpha + np.clip(blur(m, 3) * 1.6, 0, 1), 0, 1)
    c.finish(path, with_alpha=True)


def divider(path, w=512, h=48):
    c = Canvas(w, h, supersample=4)
    transparent(c)
    line = c.polygon([(10, h / 2), (w / 2 - 30, h / 2 - 2.5), (w / 2 + 30, h / 2 - 2.5), (w - 10, h / 2), (w / 2 + 30, h / 2 + 2.5), (w / 2 - 30, h / 2 + 2.5)])
    gem = c.polygon([(w / 2, 6), (w / 2 + 18, h / 2), (w / 2, h - 6), (w / 2 - 18, h / 2)])
    dots = c.ellipse(w / 2 - 40, h / 2, 5, 5) + c.ellipse(w / 2 + 40, h / 2, 5, 5)
    stamp(c, np.clip(line + dots, 0, 1), painter=lambda mm: gilded(c, mm, 1))
    stamp(c, gem, painter=lambda mm: gilded(c, mm, 2))
    inner = c.polygon([(w / 2, 14), (w / 2 + 9, h / 2), (w / 2, h - 14), (w / 2 - 9, h / 2)])
    c.lay(rgb("#b0101a"), inner)
    unpremultiply_finish(c, path)


def speech(path, w=192, h=128):
    c = Canvas(w, h, supersample=4)
    transparent(c)
    body = rounded(c, 2, 2, w - 2, h - 2, 18)
    tex = noise(c, 20, octaves=3, seed=51)
    stamp(c, body, mix(rgb("#cdb88e"), rgb("#efe2c2"), tex))
    c.lay(rgb("#6a4a20"), np.clip(body - blur(body, 10), 0, 1), 0.5)
    border = np.clip(body - rounded(c, 5, 5, w - 5, h - 5, 15), 0, 1)
    gilded(c, border, 1)
    unpremultiply_finish(c, path)


def soft_glow(path, size=128):
    c = Canvas(size, size, supersample=1)
    c.rgb[:] = 1
    c.alpha = radial(c, size / 2, size / 2, size / 2, power=2.0)
    c.finish(path, with_alpha=True)


def generate(out_dir, fonts_dir):
    os.makedirs(out_dir, exist_ok=True)
    p = lambda name: os.path.join(out_dir, name + ".png")
    jobs = [
        ("background", lambda: background(p("background"))),
        ("table", lambda: table(p("table"))),
        ("frame", lambda: frame(p("frame"))),
        ("panel", lambda: panel(p("panel"))),
        ("button_blood", lambda: button(p("button_blood"), rgb("#9a1810"), rgb("#4a0604"))),
        ("button_ember", lambda: button(p("button_ember"), rgb("#e0661a"), rgb("#8a2006"))),
        ("button_ash", lambda: button(p("button_ash"), rgb("#3a2220"), rgb("#140808"))),
        ("chip", lambda: chip(p("chip"), rgb("#8a0e10"), rgb("#f0e2c0"))),
        ("chip_selected", lambda: chip(p("chip_selected"), rgb("#e09a2a"), rgb("#4a0604"))),
        ("card_face", lambda: card_face(p("card_face"))),
        ("card_back", lambda: card_back(p("card_back"))),
        ("card_slot", lambda: card_slot(p("card_slot"))),
        ("suit_spades", lambda: suit(p("suit_spades"), "spades")),
        ("suit_hearts", lambda: suit(p("suit_hearts"), "hearts")),
        ("suit_diamonds", lambda: suit(p("suit_diamonds"), "diamonds")),
        ("suit_clubs", lambda: suit(p("suit_clubs"), "clubs")),
        ("title", lambda: title_logo(p("title"), os.path.join(fonts_dir, "CinzelDecorative-Bold.ttf"))),
        ("divider", lambda: divider(p("divider"))),
        ("speech", lambda: speech(p("speech"))),
        ("glow", lambda: soft_glow(p("glow"))),
    ]
    return jobs
