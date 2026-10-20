"""Demo 3.5: every relic's big picture, 32x32 (shown at 2x on the relic cards: 64x64).

The same object as its 16x16 icon (pixel_relics.icon), drawn again on a 32x32 canvas that maps the icon's coordinates (the shapes are
filled at the bigger size, so their edges come out finer), then the details a card can afford: light from the top left (a lit rim, a
shaded side), the parts' own edges, a 1 px dark outline, a halo in the rarity's colour behind it (common bone, rare blood / copper,
legendary gold; a demon's own relic its demon's colour), a small mark of its demon (Mammon, Belial, Lilith, Lucifer) or a small badge
in its class's colour.

Ui/relic_art.png: one strip in RelicRoster.IconOrder (the icons' order). Ui/RelicGlint/<id>.png: each legendary's sparkle, a one-shot
burst of GLINT_FRAMES frames (frame 0 the picture at rest; the card plays it now and then). Screenshots/demo35/relic_sheet.png: all 54 at
2x with their names (read from UiText.Relics.cs).
"""
import os
import re

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import pixel_relics
from pixel import C, Img, sheet, shifted

ART = 32
SCALE = 26 / 16       # the icon's 16 px onto 26: room for the outline, the halo and the marks
OFFSET = 3
GLINT_FRAMES = 6

# Rarity (ChapterRelics.RarityOf; the demo's: the Bone Die rare, the rest common).
LEGENDARY = {"black_book", "hangmans_rope", "liliths_tear", "abyss_coin", "lucifers_feather", "mammons_gold_tooth", "harvest_moon",
             "star_chart", "golden_scepter", "black_grimoire", "shadow_cloak", "mocking_mask"}
RARE = {"bone_die", "iron_vow", "pilgrims_lantern", "raven_feather", "blood_ring", "old_map", "silver_bell", "salt_circle", "liars_tongue",
        "blood_pact", "lost_souls_letter", "twin_dice", "jesters_hat", "seers_spectacles", "peasants_scythe", "kings_signet",
        "summoners_leash", "devils_compass", "lockpick"}
BOSS = {"mammons_seal", "mammons_scales", "belials_mask", "belials_tongue"}

# A demon's own relics: his mark (and, for the spoils, his halo).
DEMON_OF = {"mammons_seal": "mammon", "mammons_scales": "mammon", "mammons_gold_tooth": "mammon", "belials_mask": "belial",
            "belials_tongue": "belial", "liliths_tear": "lilith", "lucifers_feather": "lucifer", "devils_compass": "lucifer"}

# A class's relics (ChapterRelics.ClassChain and the class relics of Demo 2.1): its badge.
CLASS_OF = {"peasants_scythe": "peasant", "rope_belt": "peasant", "harvest_moon": "peasant",
            "seers_spectacles": "warlock", "cracked_orb": "warlock", "star_chart": "warlock",
            "kings_signet": "king", "tax_ledger": "king", "golden_scepter": "king",
            "summoners_leash": "summoner", "chalk_circle": "summoner", "black_grimoire": "summoner",
            "lockpick": "thief", "worn_gloves": "thief", "shadow_cloak": "thief",
            "jesters_hat": "jester", "bell_collar": "jester", "mocking_mask": "jester"}

HALO = {"common": (C.BONE_SHADE, C.BONE_DARK), "rare": (C.BLOOD, C.GOLD_MID), "legendary": (C.GOLD_DARK, C.GOLD),
        "mammon": (C.GREEN_DARK, C.GREEN), "belial": (C.SILVER_DARK, C.SILVER), "lilith": (C.VIOLET, C.LILAC),
        "lucifer": (C.BLOOD_DARK, C.HELL)}
DEMON_INK = {"mammon": (C.GREEN, C.GREEN_LIGHT), "belial": (C.SILVER_DARK, C.SILVER), "lilith": (C.MAUVE, C.LILAC_LIGHT),
             "lucifer": (C.RED, C.ORANGE)}
CLASS_INK = {"peasant": (C.GREEN, C.GREEN_LIGHT), "warlock": (C.MAUVE, C.LILAC), "king": (C.GOLD_MID, C.GOLD_LIGHT),
             "jester": (C.CRIMSON, C.RED), "summoner": (C.HELL, C.AMBER), "thief": (C.SILVER_DARK, C.SILVER)}

# Light steps along the palette's ramps (the lit rim one up, the shaded side one down).
RAMPS = [
    [C.BONE_SHADE, C.BONE_DARK, C.BONE_MID, C.BONE, C.WHITE],
    [C.GOLD_DARK, C.GOLD_MID, C.GOLD, C.GOLD_LIGHT, C.EMBER],
    [C.BLOOD_DARK, C.BLOOD, C.CRIMSON, C.RED, C.HELL, C.ORANGE, C.AMBER, C.EMBER],
    [C.GREEN_DARK, C.GREEN, C.GREEN_LIGHT],
    [C.BLACK, C.NIGHT, C.DUSK, C.PLUM, C.VIOLET, C.MAUVE, C.LILAC, C.LILAC_LIGHT],
    [C.SILVER_DARK, C.SILVER, C.WHITE],
]


def step(color, by):
    for ramp in RAMPS:
        if color in ramp:
            return ramp[max(0, min(len(ramp) - 1, ramp.index(color) + by))]
    return color


def rarity(relic_id):
    if relic_id in BOSS:
        return DEMON_OF[relic_id]
    if relic_id in LEGENDARY:
        return "legendary"
    if relic_id in RARE:
        return "rare"
    return "common"


class Big(Img):
    """A 32x32 canvas that takes the icon's 16x16 coordinates: every shape is mapped (x -> OFFSET + SCALE * x) and filled at 32."""

    def __init__(self):
        super().__init__(ART, ART)

    @staticmethod
    def _x(v):
        return OFFSET + SCALE * v

    def m_rect(self, x0, y0, x1, y1):
        bx0, by0 = int(np.floor(self._x(x0))), int(np.floor(self._x(y0)))
        bx1, by1 = int(np.ceil(self._x(x1 + 1))) - 1, int(np.ceil(self._x(y1 + 1))) - 1
        return super().m_rect(bx0, by0, max(bx0, bx1), max(by0, by1))

    def m_ellipse(self, cx, cy, rx, ry):
        return super().m_ellipse(self._x(cx), self._x(cy), rx * SCALE, ry * SCALE)

    def m_poly(self, pts):
        return super().m_poly([(self._x(x), self._x(y)) for x, y in pts])

    def m_line(self, pts, width=1):
        mapped = [(self._x(x + 0.5) - 0.5, self._x(y + 0.5) - 0.5) for x, y in pts]
        return super().m_line(mapped, max(1, int(round(width * SCALE))))

    def put(self, x, y, color):
        self.paint(self.m_rect(x, y, x, y), color)


def lit(img):
    """Light from the top left: the rim facing it one step up, the side away from it one step down (two pixels deep, dithered)."""
    solid = img.px != C.CLEAR
    px = img.px.copy()
    rim = solid & ~shifted(solid, -1, -1) & ~shifted(solid, -1, 0)
    rim |= solid & ~shifted(solid, 0, -1)
    away1 = solid & ~shifted(solid, 1, 1)
    away2 = solid & ~shifted(solid, 2, 2) & ~away1
    checker = (np.add.outer(np.arange(img.h), np.arange(img.w)) % 2) == 0
    for (y, x) in zip(*np.nonzero(rim)):
        px[y, x] = step(img.px[y, x], 1)
    for (y, x) in zip(*np.nonzero(away1)):
        px[y, x] = step(img.px[y, x], -1)
    for (y, x) in zip(*np.nonzero(away2 & checker)):
        px[y, x] = step(img.px[y, x], -1)
    img.px = px


def halo(img, kind):
    """A soft glow in the rarity's colour behind the object: two rings, the outer one dithered."""
    dark, light = HALO[kind]
    solid = img.px != C.CLEAR
    grown1 = solid.copy()
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
        grown1 |= shifted(solid, dx, dy)
    grown2 = grown1.copy()
    for dx, dy in ((2, 0), (-2, 0), (0, 2), (0, -2), (1, 1), (1, -1), (-1, 1), (-1, -1)):
        grown2 |= shifted(grown1, dx // 2 if abs(dx) == 2 else dx, dy // 2 if abs(dy) == 2 else dy)
    ring1 = grown1 & ~solid
    ring2 = grown2 & ~grown1
    checker = (np.add.outer(np.arange(img.h), np.arange(img.w)) % 2) == 0
    img.px[ring1] = light
    img.px[ring2 & checker] = dark


def demon_mark(img, demon):
    """The demon's small sign in the bottom right corner: Mammon a coin, Belial a fork, Lilith a crescent, Lucifer a flame."""
    ink, light = DEMON_INK[demon]
    x, y = ART - 7, ART - 7
    m = Img(7, 7)
    if demon == "mammon":
        coin = m.m_ellipse(3.5, 3.5, 2.6, 2.6)
        m.paint(coin, ink)
        m.put(3, 2, light)
        m.put(3, 4, light)
    elif demon == "belial":
        m.paint(m.m_rect(3, 2, 3, 6), ink)
        for px_ in (1, 3, 5):
            m.paint(m.m_rect(px_, 1, px_, 2), light)
        m.paint(m.m_rect(1, 3, 5, 3), ink)
    elif demon == "lilith":
        moon = m.m_ellipse(3.5, 3.5, 2.8, 2.8) & ~m.m_ellipse(4.6, 2.8, 2.2, 2.2)
        m.paint(moon, light)
    else:
        flame = m.m_poly([(3.5, 0.5), (6, 4), (3.5, 6.5), (1, 4)])
        m.paint(flame, ink)
        m.paint(m.m_ellipse(3.5, 4.4, 1.0, 1.4), light)
    m.outline(C.BLACK)
    # the mark sits over the halo and the object's corner, its own outline keeping it apart
    for yy in range(7):
        for xx in range(7):
            if m.px[yy, xx]:
                img.px[y + yy, x + xx] = m.px[yy, xx]


def class_badge(img, cls):
    """The class's small badge in the bottom left corner: a little shield in the class's colour."""
    ink, light = CLASS_INK[cls]
    b = Img(7, 8)
    shield = b.m_poly([(0.5, 0.5), (6.5, 0.5), (6.5, 4), (3.5, 7.5), (0.5, 4)])
    b.paint(shield, ink)
    b.paint(shield & b.m_rect(0, 0, 3, 3), light)
    b.inner_outline(shield, C.BLACK)
    for yy in range(8):
        for xx in range(7):
            if b.px[yy, xx]:
                img.px[ART - 8 + yy, xx] = b.px[yy, xx]


def art(relic_id):
    img = pixel_relics.icon(relic_id, Big(), outline=False)
    lit(img)
    img.outline(C.BLACK)
    halo(img, rarity(relic_id))
    if relic_id in DEMON_OF:
        demon_mark(img, DEMON_OF[relic_id])
    if relic_id in CLASS_OF:
        class_badge(img, CLASS_OF[relic_id])
    return img


def glint_spot(img):
    """Where a legendary's sparkle catches: the topmost lit pixel of the object, the rightmost on that row."""
    body = (img.px != C.CLEAR) & (img.px != C.BLACK)
    rows = np.nonzero(body.any(axis=1))[0]
    y = int(rows[0]) if len(rows) else ART // 2
    xs = np.nonzero(body[y])[0]
    return (int(xs[-1]) if len(xs) else ART // 2), y


# A four-pointed sparkle growing and fading: frame 0 is the picture at rest, the last a single pixel (so the loop back is gentle).
GLINT = [[], [(0, 0, C.GOLD_LIGHT)], [(0, 0, C.WHITE), (1, 0, C.GOLD_LIGHT), (-1, 0, C.GOLD_LIGHT), (0, 1, C.GOLD_LIGHT), (0, -1, C.GOLD_LIGHT)],
         [(0, 0, C.WHITE), (1, 0, C.WHITE), (-1, 0, C.WHITE), (0, 1, C.WHITE), (0, -1, C.WHITE), (2, 0, C.GOLD_LIGHT), (-2, 0, C.GOLD_LIGHT),
          (0, 2, C.GOLD_LIGHT), (0, -2, C.GOLD_LIGHT)],
         [(0, 0, C.WHITE), (1, 0, C.GOLD_LIGHT), (-1, 0, C.GOLD_LIGHT), (0, 1, C.GOLD_LIGHT), (0, -1, C.GOLD_LIGHT)],
         [(0, 0, C.GOLD_LIGHT)]]
assert len(GLINT) == GLINT_FRAMES


def glint_frames(img):
    x, y = glint_spot(img)
    frames = []
    for stars in GLINT:
        f = Img(ART, ART)
        f.px = img.px.copy()
        for dx, dy, color in stars:
            if 0 <= x + dx < ART and 0 <= y + dy < ART:
                f.px[y + dy, x + dx] = color
        frames.append(f)
    return frames


def relic_names(project_root):
    """The relics' English names, as the game shows them: UiText.Relics.cs's RelicName, its constants read from the id classes."""
    ids = {}
    for path in ("Assets/Scripts/Core/Relics/Relics.cs", "Assets/Scripts/Core/Relics/ChapterRelics.cs"):
        text = open(os.path.join(project_root, path), encoding="utf-8-sig").read()
        for line in re.findall(r'public const string [^;]+;', text, re.S):   # several constants may share a line
            for m in re.finditer(r'(\w+)\s*=\s*"(\w+)"', line):
                ids[m.group(1)] = m.group(2)
    text = open(os.path.join(project_root, "Assets/Scripts/Presentation/Ui/UiText.Relics.cs"), encoding="utf-8-sig").read()
    body = text[text.index("public static string RelicName("):]
    body = body[:body.index("default:")]
    names = {}
    for m in re.finditer(r'case \w+\.(\w+): return L\("([^"]+)"', body):
        if m.group(1) in ids:
            names[ids[m.group(1)]] = m.group(2)
    return names


def write_sheet(path, pictures, names, font_path):
    cols, cell_w, cell_h = 9, 104, 92
    rows = (len(pictures) + cols - 1) // cols
    out = Image.new("RGBA", (cols * cell_w + 16, rows * cell_h + 16), (11, 6, 16, 255))
    draw = ImageDraw.Draw(out)
    draw.fontmode = "1"   # no anti-aliasing
    font = ImageFont.truetype(font_path, 8)
    for i, (relic_id, img) in enumerate(pictures):
        cx, cy = 8 + (i % cols) * cell_w, 8 + (i // cols) * cell_h
        big = img.to_rgba().resize((ART * 2, ART * 2), Image.NEAREST)
        out.alpha_composite(big, (cx + (cell_w - ART * 2) // 2, cy + 4))
        name = names.get(relic_id, relic_id.replace("_", " ").upper())
        words, line, lines = name.split(" "), "", []
        for w in words:
            trial = (line + " " + w).strip()
            if draw.textlength(trial, font=font) > cell_w - 6 and line:
                lines.append(line)
                line = w
            else:
                line = trial
        lines.append(line)
        for k, text in enumerate(lines[:2]):
            tw = draw.textlength(text, font=font)
            draw.text((cx + (cell_w - tw) // 2, cy + 72 + k * 9), text, font=font, fill=(242, 232, 208, 255))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    out.save(path)


def coin_pile():
    """The coins card's picture (32x32): a little pile of old gold coins, lit from the top left, outlined, with a gold halo."""
    img = Img(ART, ART)
    # back to front: the top coin first, the two at the bottom over it
    for cx, cy, rx in ((16, 10, 6), (15.5, 16.5, 6.5), (9.5, 22.5, 6.5), (21.5, 22.5, 6.5)):
        face = img.m_ellipse(cx, cy, rx, rx * 0.5)
        edge = img.m_ellipse(cx, cy + 2, rx, rx * 0.5) | img.m_rect(cx - rx + 0.5, cy, cx + rx - 0.5, cy + 2)
        coin = face | edge
        img.paint(coin, C.GOLD_DARK)
        img.paint(face, C.GOLD)
        img.inner_outline(face, C.GOLD_MID)
        img.paint(img.m_ellipse(cx - 1.5, cy - 0.5, rx * 0.45, rx * 0.2), C.GOLD_LIGHT)
        img.outline(C.BLACK, mask=coin)   # each coin apart from the one behind it
    lit(img)
    img.outline(C.BLACK)
    halo(img, "legendary")
    return img


def upgrade_icon():
    """The upgrade card's small sign (16x16): a gold arrow up out of an ember spark."""
    img = Img(16, 16)
    img.paint(img.m_poly([(8, 1), (14, 8), (10.5, 8), (10.5, 14), (5.5, 14), (5.5, 8), (2, 8)]), C.GOLD)
    img.paint(img.m_poly([(8, 1), (14, 8), (10.5, 8), (10.5, 14), (8, 14)]), C.GOLD_MID)
    img.paint(img.m_rect(7, 10, 8, 12), C.EMBER)
    img.outline(C.BLACK)
    return img


def write_all(art_dir, project_root=None, sheet_path=None):
    pictures = [(r, art(r)) for r in pixel_relics.ORDER]
    paths = []
    for name, img in (("card_coins", coin_pile()), ("card_upgrade", upgrade_icon())):
        path = os.path.join(art_dir, "Ui", name + ".png")
        img.save(path)
        paths.append(path)
    strip = os.path.join(art_dir, "Ui", "relic_art.png")
    sheet([img for _, img in pictures]).save(strip)
    paths.append(strip)
    glint_dir = os.path.join(art_dir, "Ui", "RelicGlint")
    os.makedirs(glint_dir, exist_ok=True)
    for relic_id, img in pictures:
        if relic_id in LEGENDARY:
            path = os.path.join(glint_dir, relic_id + ".png")
            sheet(glint_frames(img)).save(path)
            paths.append(path)
    if project_root and sheet_path:
        font = os.path.join(project_root, "Assets", "Resources", "Fonts", "HellPokerPixel.ttf")
        write_sheet(sheet_path, pictures, relic_names(project_root), font)
        paths.append(sheet_path)
    return paths


if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    root = os.path.abspath(os.path.join(here, "..", ".."))
    for p in write_all(os.path.join(root, "Assets", "Resources", "Art"), root, os.path.join(root, "Screenshots", "demo35", "relic_sheet.png")):
        print(p)
