"""Generates Hell Poker's 16-bit pixel art into Assets/Resources/Art.

Usage (from the project root):  py Tools/ArtGen/generate_art.py [demons] [ui]
Without arguments everything is rebuilt. Then  py Tools/ArtGen/preview.py  for a preview page (local only).
Needs Pillow and numpy:  py -m pip install --user pillow numpy
Every image is drawn with the one palette in pixel.py — no anti-aliasing, no colours outside it.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import pixel_demons  # noqa: E402
import pixel_ui  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
ART = os.path.join(ROOT, "Assets", "Resources", "Art")


def main(selected):
    if not selected or "demons" in selected:
        for path in pixel_demons.write_all(os.path.join(ART, "Demons")):
            print("wrote", os.path.relpath(path, ROOT))
    if not selected or "ui" in selected:
        for path in pixel_ui.write_all(os.path.join(ART, "Ui"), os.path.join(HERE, "fonts")):
            print("wrote", os.path.relpath(path, ROOT))


if __name__ == "__main__":
    main(set(sys.argv[1:]))
