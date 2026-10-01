"""Preview page of every demon animation frame (and the UI art), scaled up with nearest-neighbour.

Usage (from the project root):  py Tools/ArtGen/preview.py
Output: Tools/ArtGen/preview/index.html and contact sheets — local only, not in the repo (.gitignore).
"""
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import pixel_demons  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
ART = os.path.join(ROOT, "Assets", "Resources", "Art")
OUT = os.path.join(HERE, "preview")
SCALE = 3
FPS = {"idle": 5, "talk": 8, "gloat": 8, "angry": 8, "reraise": 8, "final": 8, "soul": 8}


def scaled(img, scale=SCALE):
    return img.resize((img.width * scale, img.height * scale), Image.NEAREST)


def demon_sheet(demon):
    """All states of one demon, one row per state, frames left to right."""
    rows = []
    for state in pixel_demons.STATES:
        path = os.path.join(ART, "Demons", demon, state + ".png")
        if os.path.exists(path):
            rows.append((state, Image.open(path).convert("RGBA")))
    width = max(img.width for _, img in rows) + 70
    height = sum(img.height + 4 for _, img in rows)
    sheet = Image.new("RGBA", (width, height), (11, 6, 16, 255))
    draw = ImageDraw.Draw(sheet)
    y = 0
    for state, img in rows:
        draw.text((2, y + 2), state, fill=(242, 232, 208, 255))
        sheet.alpha_composite(img, (70, y))
        y += img.height + 4
    return sheet


def ui_sheet():
    folder = os.path.join(ART, "Ui")
    images = [(name, Image.open(os.path.join(folder, name)).convert("RGBA")) for name in sorted(os.listdir(folder)) if name.endswith(".png")]
    width = 500
    x = y = row_h = 0
    placed = []
    for name, img in images:
        if x + img.width > width:
            x, y, row_h = 0, y + row_h + 12, 0
        placed.append((name, img, x, y))
        x += img.width + 8
        row_h = max(row_h, img.height)
    sheet = Image.new("RGBA", (width, y + row_h + 12), (40, 40, 48, 255))
    draw = ImageDraw.Draw(sheet)
    for name, img, px, py in placed:
        sheet.alpha_composite(img, (px, py + 10))
        draw.text((px, py), name[:-4][:14], fill=(200, 200, 200, 255))
    return sheet


def main():
    os.makedirs(OUT, exist_ok=True)
    html = ["<!doctype html><meta charset=utf-8><title>Hell Poker art preview</title>",
            "<style>body{background:#0b0610;color:#f2e8d0;font:14px monospace}img{image-rendering:pixelated;margin:4px}"
            ".strip{display:inline-block;width:%dpx;height:%dpx;background-repeat:no-repeat;image-rendering:pixelated;margin:4px}</style>"
            % (96 * SCALE, 96 * SCALE)]
    css = []
    for demon in pixel_demons.DEMONS:
        sheet = demon_sheet(demon)
        name = demon + "_sheet.png"
        scaled(sheet).save(os.path.join(OUT, name))
        html.append(f"<h2>{demon}</h2><div>")
        for state in pixel_demons.STATES:
            src = os.path.join(ART, "Demons", demon, state + ".png")
            if not os.path.exists(src):
                continue
            strip = Image.open(src)
            frames = strip.width // strip.height
            big = scaled(strip)
            file = f"{demon}_{state}.png"
            big.save(os.path.join(OUT, file))
            cls = f"{demon}-{state}"
            css.append(f".{cls}{{background-image:url({file});animation:{cls} {frames / FPS[state]:.3f}s steps({frames}) infinite}}"
                       f"@keyframes {cls}{{to{{background-position:-{big.width}px 0}}}}")
            html.append(f"<figure style=display:inline-block><div class='strip {cls}'></div><figcaption>{state} ({frames})</figcaption></figure>")
        html.append(f"</div><img src={name}>")
    salons = os.path.join(ART, "Backgrounds")
    if os.path.isdir(salons):
        html.append("<h2>salons</h2>")
        for demon in sorted(os.listdir(salons)):
            for variant in ("normal", "hell", "soul"):
                src = os.path.join(salons, demon, variant + ".png")
                if not os.path.exists(src):
                    continue
                strip = Image.open(src)
                frames = max(1, strip.width // 480)
                file = f"salon_{demon}_{variant}.png"
                big = scaled(strip, 2)
                big.save(os.path.join(OUT, file))
                cls = f"salon-{demon}-{variant}"
                css.append(f".{cls}{{width:960px;height:540px;background-image:url({file});background-repeat:no-repeat;"
                           f"image-rendering:pixelated;animation:{cls} {frames / 4:.2f}s steps({frames}) infinite}}"
                           f"@keyframes {cls}{{to{{background-position:-{big.width}px 0}}}}")
                html.append(f"<figure><div class='{cls}'></div><figcaption>{demon} — {variant} ({frames} frames)</figcaption></figure>")
    if os.path.isdir(os.path.join(ART, "Ui")):
        scaled(ui_sheet(), 2).save(os.path.join(OUT, "ui_sheet.png"))
        html.append("<h2>ui</h2><img src=ui_sheet.png>")
    html.insert(1, "<style>" + "".join(css) + "</style>")
    with open(os.path.join(OUT, "index.html"), "w", encoding="utf-8") as f:
        f.write("\n".join(html))
    print("wrote", os.path.relpath(os.path.join(OUT, "index.html"), ROOT))


if __name__ == "__main__":
    main()
