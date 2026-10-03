"""The studio logo shown on the splash screen before the game: "CAVEMAN" with a small caveman beside it.

Drawn in the game's palette and title font (Press Start 2P, no smoothing), on a transparent background (the splash screen
behind it is black). Drawn at 148×44 and scaled up 5× with nearest-neighbour, so Unity's splash scaling keeps the pixels
square. Output: Assets/Art/Splash/caveman_logo.png
"""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

from pixel import C, Img

W, H = 148, 44
SCALE = 5
FONT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Resources", "Fonts", "HellPokerPixelTitle.ttf")

# Rows of the caveman (22×32): wild hair, a bone in it, fur tunic over one shoulder, a club over the other.
#   K black  H hair  S skin  s skin shade  F fur  f fur shade  W club  w club shade  B bone  E eye
CAVEMAN = [
    "......KKKKKKK.........",
    "....KKHHHHHHHKK.......",
    "...KHHHHHHHHHHHK......",
    "..KHHHBBHHHHHHHHK.....",
    "..KHHBKBHHHHHHHHK.....",
    "..KHHHBBSSSSSHHHK.....",
    "..KHHSSSSSSSSSHHK.....",
    "...KHSEKSSSEKSHK......",
    "...KHSSSSsSSSSHK.....K",
    "....KSSSSssSSSK.....KW",
    "....KHSKKKKKSHK....KWw",
    ".....KHHSSSHHK....KWwK",
    "......KKSSSKK....KWwK.",
    "....KKFFSSSFFKK.KWwK..",
    "...KFFFFsSSFFFFKWwK...",
    "..KFFfFFsSSFFfFSSK....",
    "..KFfFFFFsSFFFFSSK....",
    "..KSSKFFfFFFFfKKK.....",
    "..KSSKFFFFFFFFK.......",
    "..KSSKFfFFfFFFK.......",
    "..KSSKFFFFFFfFK.......",
    "...KKKFfFFFFFFK.......",
    ".....KFFfFFfFFK.......",
    "......KFFFFFFK........",
    "......KSSKKSSK........",
    "......KSSK.KSSK.......",
    "......KSSK..KSSK......",
    "......KSSK..KSSK......",
    ".....KSSSK..KSSSK.....",
    "....KSSSSK..KSSSSK....",
    "....KKKKKK..KKKKKK....",
    "......................",
]
CAVEMAN_COLORS = {
    "K": C.BLACK, "H": C.BLOOD_DARK, "S": C.BONE_MID, "s": C.BONE_DARK, "F": C.GOLD_MID, "f": C.GOLD_DARK,
    "W": C.BONE_DARK, "w": C.BONE_SHADE, "B": C.BONE, "E": C.WHITE,
}


def word(text, size=16):
    """The word in the title font, hard-edged: a boolean mask the size of the text."""
    font = ImageFont.truetype(FONT, size)
    left, top, right, bottom = font.getbbox(text)
    canvas = Image.new("1", (right - left, bottom - top), 0)
    draw = ImageDraw.Draw(canvas)
    draw.fontmode = "1"
    draw.text((-left, -top), text, font=font, fill=1)
    return np.array(canvas, bool)


def logo():
    img = Img(W, H)

    # The caveman on the left, standing on a ledge of stone, his club's knobbed head over his shoulder.
    img.rows(2, 6, CAVEMAN, CAVEMAN_COLORS)
    head = img.m_ellipse(24.5, 13.5, 2.6, 3.4)
    img.shade(head, [C.BONE_SHADE, C.BONE_DARK, C.BONE_MID, C.BONE_MID], shadow=1)
    img.put(23, 12, C.BONE_SHADE)
    img.put(25, 15, C.BONE_SHADE)

    # "CAVEMAN": fire-lit like the game's own title — gold at the top, through amber and ember to red at the foot,
    # with a hard black drop of one pixel and a darker band under each letter.
    mask = word("CAVEMAN")
    h, w = mask.shape
    x0, y0 = 32, 38 - 6 - h   # the word stands on the ledge, beside the caveman
    ramp = [C.GOLD_LIGHT, C.GOLD_LIGHT, C.AMBER, C.AMBER, C.ORANGE, C.ORANGE, C.HELL, C.HELL, C.RED, C.RED, C.CRIMSON, C.CRIMSON]
    for y in range(h):
        for x in range(w):
            if mask[y, x]:
                img.put(x0 + x + 1, y0 + y + 1, C.BLOOD_DARK)
    for y in range(h):
        color = ramp[min(len(ramp) - 1, y * len(ramp) // h)]
        for x in range(w):
            if mask[y, x]:
                img.put(x0 + x, y0 + y, color)

    # A ledge of stones under both.
    for x in range(0, W, 6):
        img.paint(img.m_rect(x, 38, x + 4, 40), C.DUSK)
        img.paint(img.m_rect(x, 38, x + 4, 38), C.VIOLET)
    return img


def write_all(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    img = logo().to_rgba()
    img = img.resize((W * SCALE, H * SCALE), Image.NEAREST)
    path = os.path.join(out_dir, "caveman_logo.png")
    img.save(path)
    return [path]
