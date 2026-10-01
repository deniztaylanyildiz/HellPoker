"""Demon dealer portraits (512x640). Each demon is painted from simple shapes with fake lighting."""
import numpy as np

from paint import (Canvas, bezier, blur, glow, linear_y, mirror, mix, noise, radial, rgb, ribbon, rim, shade)

W, H = 512, 640
CX = W / 2


def backdrop(c, inner, outer, center=(CX, 250), radius=420, smoke_color=None, seed=1):
    t = radial(c, center[0], center[1], radius, power=1.3)
    c.rgb[:] = mix(outer, inner, t)
    smoke = noise(c, 140, octaves=5, seed=seed)
    c.lay(smoke_color if smoke_color is not None else inner * 1.4, np.clip(smoke - 0.45, 0, 1) * 1.6 * t, 0.55)


def embers(c, color, count=60, seed=3, y_min=300):
    rng = np.random.default_rng(seed)
    dots = c.mask(lambda d, cc: [d.ellipse([cc.s(x - r), cc.s(y - r), cc.s(x + r), cc.s(y + r)], fill=255)
                                 for x, y, r in zip(rng.uniform(0, W, count), rng.uniform(y_min, H, count), rng.uniform(0.8, 2.6, count))])
    c.add(color, glow(dots, 6, 2.5), 0.9)
    c.add(color * 1.5, dots, 0.9)


def eye(c, x, y, w, h, tilt, iris, pupil_slit=True, glow_color=None, glow_size=26):
    """Almond eye with a glowing iris and slit pupil. tilt>0 raises the outer corner."""
    outer = 1 if x > CX else -1
    pts = bezier([(x - w * outer, y + tilt * outer * 0), (x, y - h * 1.3), (x + w * outer, y - tilt)], 20) + \
          bezier([(x + w * outer, y - tilt), (x, y + h * 0.9), (x - w * outer, y)], 20)
    m = c.polygon(pts)
    c.lay(rgb("#120404"), blur(m, 3), 0.9)
    c.lay(iris, m)
    c.add(iris * 0.8, radial(c, x, y - h * 0.2, w * 0.9, 1.5) * m)
    if pupil_slit:
        c.lay(rgb("#0a0000"), c.ellipse(x, y - h * 0.2, w * 0.13, h * 1.0) * m)
    else:
        c.lay(rgb("#0a0000"), c.ellipse(x, y - h * 0.2, w * 0.3, w * 0.3) * m)
    c.add(np.array([1, 1, 1]), c.ellipse(x - w * 0.3, y - h * 0.6, w * 0.13, w * 0.1), 0.8)
    c.add(glow_color if glow_color is not None else iris, glow(m, glow_size, 1.6), 0.55)


def horn(c, spine_pts, widths, base, tip, light_dir=(-0.5, -0.8), rings=0):
    spine = bezier(spine_pts, 60)
    m = c.polygon(ribbon(spine, widths))
    sh = shade(m, 10, light=light_dir, strength=5, ambient=0.25)
    along = np.zeros_like(m)
    # Colour fades from base to tip along the horn: approximate by distance to the tip point.
    tx, ty = spine[-1]
    bx, by = spine[0]
    ys, xs = np.mgrid[0:c.h, 0:c.w].astype(np.float32) / c.ss
    total = np.hypot(tx - bx, ty - by) + 1e-3
    along = np.clip(1 - np.hypot(xs - tx, ys - ty) / (total * 1.1), 0, 1)
    color = mix(base, tip, along) * sh[..., None]
    if rings:
        ring = (np.sin(np.hypot(xs - bx, ys - by) / total * rings * np.pi * 2) * 0.5 + 0.5) ** 6
        color = color * (1 - 0.35 * ring[..., None])
    c.lay(color, m)
    return m


def bust(c, points, color, light=(-0.4, -0.8), ambient=0.3, radius=30):
    m = c.polygon(points)
    c.lay(color * shade(m, radius, light=light, strength=4, ambient=ambient)[..., None], m)
    return m


def head_mask(c, cranium, jaw):
    cx, cy, rx, ry = cranium
    return np.clip(c.ellipse(cx, cy, rx, ry) + c.polygon(jaw), 0, 1)


def skin(c, m, base, shadow, light=(-0.45, -0.7), radius=40, texture_seed=7, strength=5):
    sh = shade(m, radius, light=light, strength=strength, ambient=0.2)
    tex = noise(c, 18, octaves=3, seed=texture_seed)
    col = mix(shadow, base, np.clip(sh, 0, 1.3) / 1.3) * (0.92 + 0.16 * tex)[..., None]
    col = col * np.clip(sh, 0.4, 1.35)[..., None]
    c.lay(col, m)


def underlight(c, m, color, y0, y1, strength=0.6):
    c.add(color, m * linear_y(c, y0, y1) ** 2, strength)


def vignette(c, power=2.2, amount=0.75):
    t = radial(c, CX, H * 0.45, H * 0.85, power=0.9)
    c.multiply(1 - amount * (1 - t) ** power)


# ================================================================= BELIAL

def belial(path):
    c = Canvas(W, H)
    backdrop(c, rgb("#7a1208"), rgb("#120203"), smoke_color=rgb("#c4300f"), seed=11)
    embers(c, rgb("#ff7a2a"), 70, seed=4)

    # Horns behind the head: long ram horns sweeping up and back.
    for side in (-1, 1):
        sx = lambda x: CX + side * (x - CX)
        horn(c, [(sx(205), 175), (sx(150), 120), (sx(95), 70), (sx(120), 12)], (54, 6),
             rgb("#1e0c08"), rgb("#e0c49a"), light_dir=(-0.5 * side, -0.8), rings=7)

    # Black tailcoat and high collar.
    coat = bust(c, [(0, 640), (0, 560), (70, 505), (185, 470), (225, 455), (287, 455), (327, 470), (442, 505), (512, 560), (512, 640)],
                rgb("#1a1214"), radius=40)
    shirt = c.polygon([(215, 462), (297, 462), (256, 600)])
    c.lay(rgb("#d9cfc0") * shade(shirt, 12, ambient=0.5)[..., None], shirt)
    for side in (-1, 1):
        lapel = c.polygon([(CX + side * 40, 455), (CX + side * 110, 480), (CX + side * 30, 640), (CX + side * 4, 600)])
        c.lay(rgb("#2a0d10") * shade(lapel, 14, ambient=0.4)[..., None], lapel)
    tie = c.polygon([(CX - 34, 472), (CX, 488), (CX + 34, 472), (CX + 30, 508), (CX, 494), (CX - 30, 508)])
    c.lay(rgb("#a1101a") * shade(tie, 6, ambient=0.5)[..., None], tie)
    c.add(rgb("#ff5a2a"), rim(coat, 10, (1, 0)), 0.6)

    # Neck and head.
    neck = c.polygon([(226, 380), (286, 380), (296, 468), (216, 468)])
    skin(c, neck, rgb("#7a1a14"), rgb("#1a0303"), radius=20)
    c.lay(rgb("#120202"), blur(c.ellipse(CX, 430, 60, 40), 14) * neck, 0.85)

    head = head_mask(c, (CX, 255, 96, 118), [(165, 280), (347, 280), (318, 380), (282, 432), (256, 448), (230, 432), (194, 380)])
    ears = np.clip(c.polygon([(166, 250), (118, 196), (148, 292), (172, 300)]) + c.polygon(mirror([(166, 250), (118, 196), (148, 292), (172, 300)], CX)), 0, 1)
    skin(c, ears, rgb("#a3241b"), rgb("#2a0505"), radius=10)
    skin(c, head, rgb("#b8291e"), rgb("#2a0505"), radius=46)

    # Cheekbone hollows and brow shadow.
    for side in (-1, 1):
        c.lay(rgb("#2a0505"), blur(c.polygon([(CX + side * 78, 300), (CX + side * 40, 330), (CX + side * 70, 370)]), 14), 0.55)
    c.lay(rgb("#2a0505"), blur(c.ellipse(CX, 236, 80, 18), 10), 0.55)

    # Slicked-back hair with a sharp widow's peak.
    hair = c.polygon(bezier([(162, 240), (160, 130), (256, 118), (352, 130), (350, 240)], 30) +
                     [(330, 190), (300, 168), (256, 212), (212, 168), (182, 190)])
    c.lay(rgb("#0c0606") * shade(hair, 16, ambient=0.6)[..., None], hair)
    for i in range(7):
        x = 190 + i * 22
        strand = c.line(bezier([(x, 200 - abs(x - CX) * 0.2), (x + 6, 150), (x + 20, 126)], 12), 2)
        c.add(rgb("#5a2a26"), strand * hair, 0.45)

    # Brows: sharp, angled upward toward the temples.
    for side in (-1, 1):
        brow = c.polygon([(CX + side * 14, 238), (CX + side * 82, 214), (CX + side * 92, 204), (CX + side * 80, 224), (CX + side * 18, 248)])
        c.lay(rgb("#0c0404"), brow)

    for side in (-1, 1):
        eye(c, CX + side * 42, 262, 26, 9, tilt=8, iris=rgb("#ffcc33"), glow_color=rgb("#ff9a1a"))

    # Nose: long, sharp; shadow on the far side and lit bridge.
    c.lay(rgb("#3a0606"), blur(c.polygon([(258, 262), (270, 330), (262, 344), (250, 342)]), 4), 0.6)
    c.add(rgb("#ff8a5a"), blur(c.line([(252, 270), (254, 328)], 3), 2), 0.35)
    for side in (-1, 1):
        c.lay(rgb("#1a0202"), c.ellipse(CX + side * 10, 343, 6, 3), 0.8)

    # Sly crooked grin.
    smile = bezier([(212, 372), (240, 384), (282, 380), (306, 358)], 30)
    lips = c.line(smile, 6)
    teeth = c.polygon(bezier([(222, 375), (256, 388), (296, 368)], 16) + bezier([(296, 368), (258, 382), (222, 375)], 16))
    c.lay(rgb("#1a0202"), blur(lips, 1.5))
    c.lay(rgb("#e8dcc0"), c.polygon(bezier([(226, 376), (256, 386), (294, 368)], 16) + bezier([(294, 370), (258, 381), (226, 378)], 16)), 0.9)
    c.lay(rgb("#2a0303"), blur(c.ellipse(310, 356, 8, 6), 3), 0.6)
    del teeth

    # Pointed goatee.
    goatee = c.polygon([(236, 404), (256, 398), (276, 404), (270, 450), (256, 486), (242, 450)])
    c.lay(rgb("#0c0505") * shade(goatee, 8, ambient=0.6)[..., None], goatee)
    mustache = c.polygon(bezier([(214, 366), (240, 350), (256, 356)], 10) + bezier([(256, 356), (272, 350), (302, 352)], 10) +
                         [(298, 358), (256, 364), (216, 372)])
    c.lay(rgb("#0c0505"), mustache)

    # Hellfire rim light from the right and below.
    face = np.clip(head + ears + neck, 0, 1)
    c.add(rgb("#ff7a2a"), rim(face, 12, (1, 0.2)), 0.9)
    underlight(c, face, rgb("#ff5a1a"), 250, 470, 0.35)

    vignette(c)
    c.finish(path)


# ================================================================= MAMMON

def mammon(path):
    c = Canvas(W, H)
    backdrop(c, rgb("#3a3008"), rgb("#070502"), smoke_color=rgb("#8a6a10"), seed=21)
    embers(c, rgb("#ffcf4a"), 90, seed=8, y_min=0)

    gold, gold_dark = rgb("#f2c14a"), rgb("#4a2a05")

    # Fur-trimmed crimson robe over a huge belly-chest.
    robe = bust(c, [(0, 640), (0, 540), (60, 470), (170, 430), (342, 430), (452, 470), (512, 540), (512, 640)], rgb("#5a0c1e"), radius=50)
    for side in (-1, 1):
        fur = c.polygon(bezier([(CX + side * 70, 440), (CX + side * 150, 460), (CX + side * 120, 640)], 20) + [(CX + side * 60, 640)])
        tex = noise(c, 6, octaves=3, seed=30 + side)
        c.lay(mix(rgb("#3a2a20"), rgb("#d8c8a8"), tex) * shade(fur, 16, ambient=0.45)[..., None], fur)
    c.add(rgb("#ffcf4a"), rim(robe, 10, (1, 0)), 0.4)

    # Neck is all chins.
    head = head_mask(c, (CX, 275, 128, 118), [(140, 300), (372, 300), (360, 380), (300, 440), (212, 440), (152, 380)])
    chin = c.ellipse(CX, 430, 120, 44)
    skin_base, skin_shadow = rgb("#8f8a3a"), rgb("#1e1a04")
    skin(c, chin, skin_base * 0.85, skin_shadow, radius=24)
    c.lay(skin_shadow, blur(c.ellipse(CX, 410, 110, 12), 8), 0.6)

    # Heavy gold chain with a coin, over the robe.
    chain = c.line(bezier([(140, 450), (200, 560), (256, 590), (312, 560), (372, 450)], 40), 9)
    c.lay(gold_dark, chain)
    links = c.line(bezier([(140, 450), (200, 560), (256, 590), (312, 560), (372, 450)], 40), 5) * \
        (np.sin(np.arange(c.w)[None, :] / 5.0 * c.ss) * 0.5 + 0.5)
    c.lay(gold, links, 0.9)
    coin = c.ellipse(CX, 600, 34, 34)
    c.lay(gold * shade(coin, 14, ambient=0.45)[..., None], coin)
    c.lay(gold_dark, c.ellipse(CX, 600, 24, 24) - c.ellipse(CX, 600, 21, 21), 0.8)
    c.lay(gold_dark, c.polygon([(CX - 3, 584), (CX + 3, 584), (CX + 3, 616), (CX - 3, 616)]), 0.8)
    c.add(rgb("#fff0b0"), glow(c.ellipse(CX - 10, 590, 6, 6), 10, 2), 0.7)

    ears = np.clip(c.ellipse(140, 280, 26, 38) + c.ellipse(372, 280, 26, 38), 0, 1)
    skin(c, ears, skin_base * 0.9, skin_shadow, radius=10)
    skin(c, head, skin_base, skin_shadow, radius=60, texture_seed=9)

    # Jowls and cheeks: lighter puffs with shadow folds.
    for side in (-1, 1):
        cheek = c.ellipse(CX + side * 78, 330, 52, 44)
        c.add(rgb("#c8b060"), blur(cheek, 14) * head, 0.18)
        c.lay(skin_shadow, blur(c.line(bezier([(CX + side * 40, 340), (CX + side * 70, 390), (CX + side * 110, 380)], 12), 4), 5), 0.55)
        c.lay(rgb("#b04020"), blur(c.ellipse(CX + side * 92, 330, 28, 18), 12), 0.25)

    # Gold crown with stubby horns poking through.
    for side in (-1, 1):
        horn(c, [(CX + side * 70, 190), (CX + side * 92, 150), (CX + side * 120, 128)], (34, 4), rgb("#1a1208"), rgb("#9a8a60"),
             light_dir=(-0.5 * side, -0.8), rings=4)
    band = c.polygon([(150, 200), (362, 200), (356, 168), (156, 168)])
    points = []
    for i in range(6):
        x0 = 156 + i * 33.3
        points += [(x0, 168), (x0 + 16.6, 112 if i % 2 == 0 else 128)]
    points += [(356, 168)]
    spikes = c.polygon(points + [(356, 172), (156, 172)])
    crown = np.clip(band + spikes, 0, 1)
    c.lay(gold * shade(crown, 10, ambient=0.4)[..., None], crown)
    for i, color in enumerate(("#b0102a", "#1a7a3a", "#b0102a")):
        gem = c.ellipse(206 + i * 50, 184, 9, 9)
        c.lay(rgb(color) * shade(gem, 4, ambient=0.5)[..., None], gem)
        c.add(np.array([1, 1, 1]), c.ellipse(203 + i * 50, 181, 2.5, 2.5), 0.9)
    c.add(rgb("#ffe08a"), glow(crown, 18, 0.8), 0.25)

    # Small, greedy eyes under heavy lids.
    for side in (-1, 1):
        eye(c, CX + side * 50, 270, 18, 7, tilt=-3, iris=rgb("#7aff3a"), glow_color=rgb("#9aff4a"), glow_size=18)
        lid = c.polygon(bezier([(CX + side * 26, 266), (CX + side * 50, 248), (CX + side * 76, 264)], 12) +
                        bezier([(CX + side * 76, 264), (CX + side * 50, 258), (CX + side * 26, 266)], 12))
        c.lay(skin_base * 0.7, lid)
        c.lay(skin_shadow, blur(c.line(bezier([(CX + side * 24, 240), (CX + side * 52, 228), (CX + side * 82, 240)], 12), 6), 3), 0.8)

    # Monocle on the right eye, with a chain.
    ring = c.ellipse(CX + 50, 268, 30, 30) - c.ellipse(CX + 50, 268, 25, 25)
    c.lay(gold * shade(np.clip(ring, 0, 1), 3, ambient=0.5)[..., None], np.clip(ring, 0, 1))
    c.add(np.array([0.7, 0.9, 1.0]), c.ellipse(CX + 40, 258, 8, 5), 0.25)
    c.lay(gold, c.line(bezier([(CX + 78, 280), (CX + 110, 360), (CX + 96, 440)], 20), 2), 0.8)

    # Pig-like snout nose.
    nose = c.ellipse(CX, 318, 30, 24)
    skin(c, nose, skin_base * 1.05, skin_shadow, radius=12)
    for side in (-1, 1):
        c.lay(rgb("#1a0a02"), c.ellipse(CX + side * 11, 324, 6, 8), 0.85)

    # Enormous grin full of gold teeth.
    mouth_pts = bezier([(170, 352), (256, 372), (342, 352)], 24) + bezier([(342, 352), (256, 432), (170, 352)], 24)
    mouth = c.polygon(mouth_pts)
    c.lay(rgb("#1a0303"), mouth)
    teeth_top = c.polygon(bezier([(182, 358), (256, 378), (330, 358)], 20) + bezier([(330, 358), (256, 396), (182, 358)], 20)) * mouth
    tooth_lines = (np.abs(np.sin((np.arange(c.w)[None, :] / c.ss - CX) / 16.0 * np.pi)) > 0.12).astype(np.float32)
    c.lay(gold * shade(teeth_top, 6, ambient=0.6)[..., None], teeth_top * tooth_lines)
    teeth_bot = c.polygon(bezier([(200, 380), (256, 420), (312, 380)], 20) + bezier([(312, 380), (256, 412), (200, 380)], 20)) * mouth
    c.lay(rgb("#e0d0a0") * 0.8, teeth_bot * tooth_lines, 0.9)
    c.lay(skin_shadow, blur(c.line(bezier([(166, 350), (256, 372), (346, 350)], 20), 4), 2), 0.7)

    face = np.clip(head + ears + chin, 0, 1)
    c.add(rgb("#ffcf4a"), rim(face, 12, (1, 0.1)), 0.7)
    c.add(rgb("#ffe08a"), glow(c.ellipse(CX, 600, 34, 34), 30, 1), 0.25)

    vignette(c)
    c.finish(path)


# ================================================================= LILITH

def lilith(path):
    c = Canvas(W, H)
    backdrop(c, rgb("#2e1450"), rgb("#05020c"), smoke_color=rgb("#6a2ab0"), seed=31)
    embers(c, rgb("#c58aff"), 60, seed=12, y_min=0)

    skin_base, skin_shadow = rgb("#c6b2cf"), rgb("#1c0f2a")
    hair_color = rgb("#0c0812")

    # Long hair behind everything.
    back_hair = c.polygon(bezier([(150, 260), (140, 140), (256, 100), (372, 140), (362, 260)], 30) +
                          bezier([(362, 260), (400, 420), (420, 560), (380, 640)], 20) + [(132, 640)] +
                          bezier([(92, 560), (112, 420), (150, 260)], 20))
    c.lay(hair_color * shade(back_hair, 30, ambient=0.7)[..., None], back_hair)
    for i in range(14):
        side = -1 if i % 2 else 1
        x = CX + side * (90 + (i // 2) * 7)
        strand = c.line(bezier([(x - side * 30, 200), (x + side * 20, 380), (x + side * 10 + side * i * 2, 600)], 30), 2)
        c.add(rgb("#4a3a6a"), strand * back_hair, 0.35)

    # High-collared black gown with an amethyst.
    gown = bust(c, [(20, 640), (40, 540), (130, 480), (210, 455), (302, 455), (382, 480), (472, 540), (492, 640)], rgb("#120a1a"), radius=40)
    chest = c.polygon([(214, 455), (298, 455), (276, 540), (256, 560), (236, 540)])
    skin(c, chest, skin_base * 0.85, skin_shadow, radius=20)
    for side in (-1, 1):
        collar = c.polygon([(CX + side * 70, 470), (CX + side * 150, 360), (CX + side * 170, 380), (CX + side * 120, 500)])
        c.lay(rgb("#2a1240") * shade(collar, 10, ambient=0.5)[..., None], collar)
        c.add(rgb("#b07aff"), rim(collar, 6, (side, -0.4)), 0.5)
    gem = c.polygon([(CX, 520), (CX + 14, 542), (CX, 572), (CX - 14, 542)])
    c.lay(rgb("#8a2ad0") * shade(gem, 4, ambient=0.5)[..., None], gem)
    c.add(rgb("#d0a0ff"), glow(gem, 20, 1.2), 0.5)

    neck = c.polygon([(238, 370), (274, 370), (284, 466), (228, 466)])
    skin(c, neck, skin_base * 0.8, skin_shadow, radius=16)
    c.lay(skin_shadow, blur(c.ellipse(CX, 392, 44, 26), 12) * neck, 0.75)

    head = head_mask(c, (CX, 262, 78, 102), [(180, 282), (332, 282), (310, 346), (276, 386), (256, 394), (236, 386), (202, 346)])
    skin(c, head, skin_base, skin_shadow, radius=44, texture_seed=4, strength=3, light=(-0.2, -0.6))

    # Small black horns curving back from the hairline.
    for side in (-1, 1):
        horn(c, [(CX + side * 40, 170), (CX + side * 64, 108), (CX + side * 110, 84), (CX + side * 128, 104)], (26, 3),
             rgb("#050308"), rgb("#4a3a5a"), light_dir=(-0.5 * side, -0.8), rings=0)

    # Front hair: centre part sweeping down over the temples, framing the face.
    for side in (-1, 1):
        lock = c.polygon(bezier([(CX + side * 2, 150), (CX + side * 90, 140), (CX + side * 104, 260), (CX + side * 96, 430)], 30) +
                         bezier([(CX + side * 66, 430), (CX + side * 72, 300), (CX + side * 60, 220), (CX + side * 4, 196)], 30))
        c.lay(hair_color * shade(lock, 12, ambient=0.6)[..., None], lock)
        c.add(rgb("#7a6a9a"), rim(lock, 6, (-side, -0.6)), 0.5)
        for k in range(4):
            strand = c.line(bezier([(CX + side * (10 + k * 14), 160), (CX + side * (70 + k * 6), 200), (CX + side * (78 + k * 5), 420)], 24), 1.5)
            c.add(rgb("#5a4a7a"), strand * lock, 0.5)

    # Silver crescent circlet.
    crescent = np.clip(c.ellipse(CX, 156, 22, 22) - c.ellipse(CX + 10, 150, 19, 19), 0, 1)
    c.lay(rgb("#dcd6e8") * shade(crescent, 4, ambient=0.6)[..., None], crescent)
    band = c.line(bezier([(176, 196), (256, 170), (336, 196)], 20), 3)
    c.lay(rgb("#b8b0c8"), band, 0.8)
    c.add(rgb("#f0e8ff"), glow(crescent, 14, 1), 0.4)

    # Cheekbones and contour.
    for side in (-1, 1):
        c.lay(skin_shadow, blur(c.polygon([(CX + side * 76, 300), (CX + side * 36, 334), (CX + side * 70, 350)]), 12), 0.4)
        c.add(rgb("#ffffff"), blur(c.ellipse(CX + side * 52, 294, 18, 8), 10) * head, 0.12)

    # Arched brows and smoky lids.
    for side in (-1, 1):
        brow = c.line(bezier([(CX + side * 16, 226), (CX + side * 50, 210), (CX + side * 76, 218)], 16), 3)
        c.lay(rgb("#0c0612"), brow)
        c.lay(rgb("#3a1050"), blur(c.ellipse(CX + side * 44, 246, 30, 14), 8), 0.6)
        eye(c, CX + side * 40, 252, 22, 8, tilt=6, iris=rgb("#c070ff"), glow_color=rgb("#a050ff"), glow_size=22)
        lash = c.line(bezier([(CX + side * 18, 252), (CX + side * 40, 238), (CX + side * 66, 244), (CX + side * 72, 238)], 16), 3)
        c.lay(rgb("#050208"), lash)

    # Slim nose.
    c.lay(skin_shadow, blur(c.line([(260, 262), (264, 312)], 3), 3), 0.45)
    c.lay(skin_shadow, blur(c.ellipse(CX, 318, 12, 4), 3), 0.5)

    # Dark lips, a knowing half smile.
    upper = c.polygon(bezier([(226, 346), (244, 336), (256, 342), (268, 336), (290, 342)], 20) + bezier([(290, 342), (256, 350), (226, 346)], 12))
    lower = c.polygon(bezier([(228, 347), (256, 351), (290, 343)], 12) + bezier([(290, 343), (258, 370), (228, 347)], 16))
    lips = np.clip(upper + lower, 0, 1)
    c.lay(rgb("#3a0a3a") * shade(lips, 6, ambient=0.5)[..., None], lips)
    c.add(rgb("#ff9aff"), c.ellipse(258, 356, 10, 2.5), 0.25)
    c.lay(rgb("#120412"), c.line(bezier([(226, 346), (256, 350), (292, 340)], 16), 1.5))
    c.lay(rgb("#120412"), c.ellipse(308, 330, 2.5, 2.5), 0.9)

    face = np.clip(head + neck, 0, 1)
    c.add(rgb("#b07aff"), rim(face, 10, (-1, 0.2)), 0.6)
    c.add(rgb("#ff7aa0"), rim(face, 10, (1, 0.3)), 0.25)

    vignette(c)
    c.finish(path)


DEMONS = {
    "mammon": mammon,
    "belial": belial,
    "lilith": lilith,
}
