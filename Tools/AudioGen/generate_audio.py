"""Generates Hell Poker's sounds into Assets/Resources/Audio (Sfx/<id>.wav, Music/<id>.wav).

Usage (from the project root):  py Tools/AudioGen/generate_audio.py [sfx] [music]
Needs numpy:  py -m pip install --user numpy
Every sound is synthesized (pulse / triangle / noise, envelopes): nothing is recorded, nothing is edited by hand.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import sounds  # noqa: E402
from synth import RATE, fade_edges, write  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
AUDIO = os.path.join(ROOT, "Assets", "Resources", "Audio")


def main(selected):
    if not selected or "sfx" in selected:
        folder = os.path.join(AUDIO, "Sfx")
        os.makedirs(folder, exist_ok=True)
        for name, make in sounds.SFX.items():
            path = os.path.join(folder, name + ".wav")
            data = fade_edges(make())
            write(path, data)
            print("wrote", os.path.relpath(path, ROOT), f"{len(data) / RATE:.2f}s")
    if not selected or "music" in selected:
        folder = os.path.join(AUDIO, "Music")
        os.makedirs(folder, exist_ok=True)
        for name, make in sounds.MUSIC.items():
            path = os.path.join(folder, name + ".wav")
            data = make()
            write(path, data)
            print("wrote", os.path.relpath(path, ROOT), f"{len(data) / RATE:.1f}s")


if __name__ == "__main__":
    main(set(sys.argv[1:]))
