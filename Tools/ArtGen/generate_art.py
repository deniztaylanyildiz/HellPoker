"""Generates Hell Poker's art into Assets/Resources/Art.

Usage (from the project root):  py Tools/ArtGen/generate_art.py [name ...]
With names, only those images are rebuilt (e.g. `belial card_back`).
Needs Pillow and numpy:  py -m pip install --user pillow numpy
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import demons  # noqa: E402
import ui  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
ART = os.path.join(ROOT, "Assets", "Resources", "Art")
FONTS = os.path.join(HERE, "fonts")


def main(selected):
    demon_dir = os.path.join(ART, "Demons")
    os.makedirs(demon_dir, exist_ok=True)
    jobs = [(name, (lambda n=name, f=painter: f(os.path.join(demon_dir, n + ".png")))) for name, painter in demons.DEMONS.items()]
    jobs += ui.generate(os.path.join(ART, "Ui"), FONTS)

    for name, job in jobs:
        if selected and name not in selected:
            continue
        job()
        print("wrote", name)


if __name__ == "__main__":
    main(set(sys.argv[1:]))
