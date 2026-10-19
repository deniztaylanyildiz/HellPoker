"""Hell Poker's sound effects and music loops, drawn with synth.py.

Effects are short and dry; the music loops close seamlessly (every voice that rings past the end wraps to the start).
"""
import numpy as np

from synth import (RATE, decay_env, env, loop_lowpass, lowpass, mix, normalize, note_hz, noise, pulse, seconds, triangle)

# ---------------------------------------------------------------------------------------------------------------- effects


def sfx_deal():
    """A card slides onto the felt: a soft noise swish going down."""
    s = noise(0.09, seed=3, rate_hz=9000)
    s = lowpass(s, 0.35) * np.linspace(1, 0, len(s), dtype=np.float32) ** 2
    return normalize(s, 0.6)


def sfx_flip():
    """A card turns: a short click and a tiny blip."""
    click = decay_env(noise(0.03, seed=5, rate_hz=12000), 0.006)
    blip = decay_env(pulse(note_hz("E6"), 0.04, 0.25), 0.012)
    return normalize(mix(0.05, [(0, click, 0.8), (0.005, blip, 0.3)]), 0.55)


def sfx_chip():
    """Chips on the table: two quick metallic ticks."""
    tick = lambda f: decay_env(pulse(f, 0.06, 0.125), 0.015)
    return normalize(mix(0.12, [(0, tick(1900), 0.6), (0.045, tick(2300), 0.5)]), 0.6)


def _arp(notes, step, voice=pulse, duty=0.25, half_life=0.12):
    parts = []
    for i, n in enumerate(notes):
        s = voice(note_hz(n), step * 2, duty) if voice is pulse else voice(note_hz(n), step * 2)
        parts.append((i * step, decay_env(s, half_life), 0.5))
    return mix(step * (len(notes) + 2), parts)


def sfx_win_small():
    return normalize(_arp(["C5", "E5", "G5"], 0.07), 0.6)


def sfx_win_big():
    """A fanfare: rising arpeggio and a held chord."""
    rise = _arp(["C5", "E5", "G5", "C6", "E6"], 0.07)
    chord = sum(env(pulse(note_hz(n), 0.6, 0.25), 0.01, 0.2, 0.4, 0.3) for n in ("C5", "G5", "C6"))
    return normalize(mix(1.0, [(0, rise, 0.7), (0.35, chord, 0.35)]), 0.7)


def sfx_loss():
    """A falling, sour two-note drop."""
    a = env(pulse(note_hz("E4"), 0.25, 0.5, slide_to=note_hz("D#4")), 0.005, 0.1, 0.5, 0.1)
    b = env(pulse(note_hz("A3"), 0.45, 0.5, slide_to=note_hz("G#3")), 0.005, 0.2, 0.4, 0.2)
    return normalize(lowpass(mix(0.75, [(0, a, 0.5), (0.22, b, 0.6)]), 0.4), 0.6)


def sfx_sealed():
    """The pact is sealed: a deep gong (inharmonic partials, long decay)."""
    t = np.arange(seconds(2.4), dtype=np.float32) / RATE
    partials = [(65, 1.0), (65 * 2.76, 0.5), (65 * 5.4, 0.25), (65 * 8.9, 0.12)]
    s = sum(a * np.sin(2 * np.pi * f * t) * (0.5 ** (t / (0.9 / (1 + k)))) for k, (f, a) in enumerate(partials))
    strike = decay_env(noise(0.05, seed=9), 0.01) * 0.4
    return normalize(mix(2.4, [(0, s.astype(np.float32), 1.0), (0, strike, 1.0)]), 0.75)


def sfx_cheat():
    """A demon's cheat strikes: a sly downward zap."""
    z = decay_env(pulse(1400, 0.25, 0.125, slide_to=300), 0.08)
    hiss = decay_env(noise(0.2, seed=11, rate_hz=6000), 0.05)
    return normalize(mix(0.3, [(0, z, 0.6), (0, hiss, 0.25)]), 0.6)


def sfx_backfire():
    """The cheat turns on its demon: a bright upward zap and a pop."""
    z = decay_env(pulse(300, 0.25, 0.25, slide_to=1600), 0.1)
    pop = decay_env(noise(0.06, seed=13), 0.01)
    return normalize(mix(0.35, [(0, z, 0.5), (0.2, pop, 0.5)]), 0.65)


def sfx_soul():
    """The soul goes on the table: a deep hum swelling and fading."""
    t = np.arange(seconds(2.0), dtype=np.float32) / RATE
    hum = np.sin(2 * np.pi * 55 * t) + 0.5 * np.sin(2 * np.pi * 82.5 * t) + 0.25 * triangle(110, 2.0)
    swell = np.sin(np.pi * t / 2.0) ** 2
    return normalize((hum * swell).astype(np.float32), 0.7)


def sfx_summoned():
    """Lucifer summons: a slow organ chord rising out of the dark."""
    chord = sum(env(pulse(note_hz(n), 3.0, 0.5), 1.2, 0.5, 0.8, 1.2) for n in ("C2", "G2", "C3", "D#3", "G3"))
    rumble = lowpass(noise(3.0, seed=17), 0.02) * 3
    return normalize(lowpass(chord * 0.3 + rumble, 0.25), 0.7)


def sfx_fall():
    """Cast down: a long falling whistle and a thud."""
    whistle = env(triangle(1200, 1.2, slide_to=120), 0.05, 0.3, 0.7, 0.2)
    thud = decay_env(triangle(60, 0.4, slide_to=35), 0.08)
    return normalize(mix(1.6, [(0, whistle, 0.5), (1.15, thud, 0.9)]), 0.7)


def sfx_click():
    return normalize(decay_env(pulse(note_hz("A5"), 0.03, 0.25), 0.008), 0.35)


def sfx_transition():
    """A screen change: a soft low whoosh."""
    s = lowpass(noise(0.3, seed=19), 0.06) * np.sin(np.linspace(0, np.pi, seconds(0.3), dtype=np.float32))
    return normalize(s, 0.4)


SFX = {
    "deal": sfx_deal, "flip": sfx_flip, "chip": sfx_chip, "win_small": sfx_win_small, "win_big": sfx_win_big, "loss": sfx_loss,
    "sealed": sfx_sealed, "cheat": sfx_cheat, "backfire": sfx_backfire, "soul": sfx_soul, "summoned": sfx_summoned, "fall": sfx_fall,
    "click": sfx_click, "transition": sfx_transition,
}

# ---------------------------------------------------------------------------------------------------------------- music


def _track(bpm, bars, beats=4):
    beat = 60.0 / bpm
    return beat, bars * beats * beat


def _chord_notes(chord, octave):
    """'Am' / 'F' / 'E7' / 'Cmaj7' / 'Dm7' -> note names at an octave."""
    names = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"]
    root = chord[0] + (chord[1] if len(chord) > 1 and chord[1] in "#b" else "")
    kind = chord[len(root):]
    flat = {"Db": "C#", "Eb": "D#", "Gb": "F#", "Ab": "G#", "Bb": "A#"}
    i = names.index(flat.get(root, root))
    steps = {"": [0, 4, 7], "m": [0, 3, 7], "7": [0, 4, 7, 10], "m7": [0, 3, 7, 10], "maj7": [0, 4, 7, 11], "dim": [0, 3, 6]}[kind]
    out = []
    for s in steps:
        k = i + s
        out.append(names[k % 12] + str(octave + k // 12))
    return out


def music_mammon():
    """Heavy and metallic: a slow minor march, a thin pulse bass, anvil strikes on 1 and 3."""
    beat, length = _track(68, 16)
    chords = ["Am", "Am", "F", "F", "Dm", "Dm", "E", "E"] * 2
    parts = []
    for bar, ch in enumerate(chords):
        root = _chord_notes(ch, 2)[0]
        for b in range(4):
            start = (bar * 4 + b) * beat
            parts.append((start, env(pulse(note_hz(root), beat * 0.9, 0.125), 0.005, 0.15, 0.5, 0.05), 0.35))
            if b in (0, 2):
                anvil = decay_env(pulse(2100, 0.25, 0.5) * 0.5 + noise(0.25, seed=bar * 4 + b, rate_hz=11000) * 0.5, 0.05)
                parts.append((start, anvil, 0.18))
        for k, n in enumerate(_chord_notes(ch, 4)):
            parts.append((bar * 4 * beat + k * beat * 0.5, decay_env(pulse(note_hz(n), beat, 0.25), 0.25), 0.12))
    melody = ["E5", None, "C5", None, "D5", "C5", "B4", None] * 4
    for i, n in enumerate(melody):
        if n:
            parts.append((i * beat * 2, env(pulse(note_hz(n), beat * 1.8, 0.25, vibrato=0.004), 0.02, 0.3, 0.5, 0.3), 0.14))
    return normalize(mix(length, parts), 0.6)


def music_belial():
    """A smoky cabaret: swung walking bass, jazz chords on the off-beats, brushes, a crooning lead."""
    beat, length = _track(116, 16)
    swing = beat * 0.62
    chords = ["Dm7", "G7", "Cmaj7", "A7"] * 4
    parts = []
    for bar, ch in enumerate(chords):
        tones = _chord_notes(ch, 2)
        walk = [tones[0], tones[1 % len(tones)], tones[2 % len(tones)], tones[1 % len(tones)]]
        for b in range(4):
            start = (bar * 4 + b) * beat
            parts.append((start, env(triangle(note_hz(walk[b]), beat * 0.9), 0.005, 0.1, 0.7, 0.05), 0.5))
            brush = decay_env(noise(0.08, seed=bar * 8 + b), 0.02)
            parts.append((start + swing, brush, 0.07))
            parts.append((start, brush, 0.04))
            if b in (1, 3):
                chord = sum(pulse(note_hz(n), beat * 0.4, 0.25) for n in _chord_notes(ch, 4))
                parts.append((start + swing * 0.2, env(chord, 0.005, 0.1, 0.4, 0.05), 0.05))
    lead = ["F5", "E5", "D5", None, "B4", "D5", "G5", None, "E5", None, "G5", "B5", "A5", "G5", "E5", None]
    for i, n in enumerate(lead * 2):
        if n:
            parts.append((i * beat * 2 + (beat * 0.3 if i % 2 else 0), env(pulse(note_hz(n), beat * 1.6, 0.25, vibrato=0.008),
                                                                             0.03, 0.2, 0.6, 0.3), 0.12))
    return normalize(mix(length, parts), 0.6)


def music_lilith():
    """Slow and minor, moonlit: a triangle arpeggio, a soft low pad, a sparse lead with a long tail."""
    beat, length = _track(60, 12, beats=4)
    chords = ["Dm", "Bb", "Gm", "A"] * 3
    parts = []
    for bar, ch in enumerate(chords):
        notes = _chord_notes(ch, 3)
        for k in range(8):
            n = notes[[0, 1, 2, 1][k % 4]]
            octave_up = n[:-1] + str(int(n[-1]) + (1 if k >= 4 else 0))
            parts.append(((bar * 8 + k) * beat * 0.5, decay_env(triangle(note_hz(octave_up), beat * 1.5), 0.3), 0.25))
        pad = env(triangle(note_hz(_chord_notes(ch, 2)[0]), beat * 4.2), 0.6, 0.5, 0.7, 0.8)
        parts.append((bar * 4 * beat, pad, 0.35))
    lead = ["A5", None, None, "F5", None, "G5", None, None, "E5", None, None, None]
    for i, n in enumerate(lead):
        if n:
            parts.append((i * beat * 4, env(pulse(note_hz(n), beat * 3.5, 0.125, vibrato=0.01), 0.3, 0.5, 0.5, 1.2), 0.09))
    return normalize(loop_lowpass(mix(length, parts), 0.5), 0.55)


def music_lucifer():
    """An organ in a cathedral that burned: slow stacked chords, a pedal tone, nothing else."""
    beat, length = _track(48, 8)
    chords = ["Cm", "Ab", "Fm", "G", "Cm", "Ab", "Fm", "G"]
    parts = []
    for bar, ch in enumerate(chords):
        start = bar * 4 * beat
        tones = _chord_notes(ch, 3) + [_chord_notes(ch, 4)[0]]
        organ = sum(pulse(note_hz(n), beat * 4.2, 0.5) * 0.6 + pulse(note_hz(n) * 2, beat * 4.2, 0.25) * 0.3 for n in tones)
        parts.append((start, env(organ, 0.4, 0.5, 0.8, 0.6), 0.07))
        pedal = env(triangle(note_hz(_chord_notes(ch, 1)[0]), beat * 4.2), 0.3, 0.5, 0.9, 0.5)
        parts.append((start, pedal, 0.5))
    return normalize(loop_lowpass(mix(length, parts), 0.3), 0.6)


def music_menu():
    """The bottom of Hell: a dark minor arpeggio over a slow bass, a far-off bell."""
    beat, length = _track(76, 12)
    chords = ["Em", "C", "Am", "B"] * 3
    parts = []
    for bar, ch in enumerate(chords):
        notes = _chord_notes(ch, 3)
        for k in range(8):
            n = notes[k % len(notes)]
            parts.append(((bar * 8 + k) * beat * 0.5, decay_env(triangle(note_hz(n) * 2, beat), 0.15), 0.22))
        parts.append((bar * 4 * beat, env(pulse(note_hz(_chord_notes(ch, 2)[0]), beat * 3.8, 0.5), 0.05, 0.4, 0.6, 0.4), 0.18))
        if bar % 4 == 0:
            t = np.arange(seconds(3), dtype=np.float32) / RATE
            bell = (np.sin(2 * np.pi * note_hz("B5") * t) + 0.4 * np.sin(2 * np.pi * note_hz("B5") * 2.76 * t)) * 0.5 ** (t / 0.6)
            parts.append((bar * 4 * beat, bell.astype(np.float32), 0.2))
    return normalize(loop_lowpass(mix(length, parts), 0.45), 0.55)


def music_soul_layer():
    """Under the soul's weight: a low drone and a slow heartbeat (a layer over the demon's music)."""
    length = 16.0
    t = np.arange(seconds(length), dtype=np.float32) / RATE
    drone = (np.sin(2 * np.pi * 41.2 * t) + 0.6 * np.sin(2 * np.pi * 61.7 * t + np.sin(2 * np.pi * 0.125 * t))).astype(np.float32)
    parts = [(0, drone * 0.5, 1.0)]
    for k in range(16):
        thump = decay_env(triangle(55, 0.25, slide_to=40), 0.05)
        parts.append((k * 1.0, thump, 0.9))
        parts.append((k * 1.0 + 0.28, thump, 0.6))
    return normalize(mix(length, parts), 0.6)


MUSIC = {
    "mammon": music_mammon, "belial": music_belial, "lilith": music_lilith, "lucifer": music_lucifer, "menu": music_menu,
    "soul_layer": music_soul_layer,
}

# ---------------------------------------------------------------------------------------------------------------- Phase 2's effects


def _coins(count, seed, rising=True):
    """Coins: bright metallic ticks, a few or a shower."""
    parts = []
    for k in range(count):
        f = (2400 + 180 * k) if rising else (2600 - 160 * k)
        tick = decay_env(pulse(f, 0.08, 0.125) * 0.7 + noise(0.08, seed=seed + k, rate_hz=14000) * 0.3, 0.02)
        parts.append((k * 0.045, tick, 0.5))
    return mix(0.08 + count * 0.045, parts)


def sfx_coin_win():
    return normalize(_coins(3, 31), 0.55)


def sfx_coin_win_big():
    """A shower of coins and a little rising chime."""
    chime = _arp(["E5", "G#5", "B5"], 0.06)
    return normalize(mix(0.7, [(0, _coins(8, 33), 0.8), (0.2, chime, 0.35)]), 0.62)


def sfx_coin_loss():
    """Coins slide away: a few ticks falling, a dull thump."""
    thump = decay_env(triangle(110, 0.15, slide_to=70), 0.04)
    return normalize(mix(0.3, [(0, _coins(3, 35, rising=False), 0.6), (0.12, thump, 0.5)]), 0.55)


def sfx_coin_loss_big():
    thump = decay_env(triangle(90, 0.3, slide_to=45), 0.08)
    drop = env(pulse(note_hz("D4"), 0.4, 0.5, slide_to=note_hz("A3")), 0.005, 0.2, 0.4, 0.15)
    return normalize(lowpass(mix(0.7, [(0, _coins(6, 37, rising=False), 0.6), (0.2, thump, 0.6), (0.25, drop, 0.35)]), 0.5), 0.6)


def sfx_purse_empty():
    """The purse is empty: a last coin rattles, a hollow fall."""
    rattle = decay_env(pulse(2200, 0.4, 0.125, slide_to=1600) * np.sign(np.sin(np.linspace(0, 60, seconds(0.4)))), 0.12)
    fall = env(triangle(note_hz("A3"), 1.0, slide_to=note_hz("A2")), 0.02, 0.3, 0.6, 0.4)
    return normalize(mix(1.4, [(0, rattle.astype(np.float32), 0.4), (0.35, fall, 0.7)]), 0.62)


def sfx_bar_hit():
    """The demon's bar shortens: a sharp hit."""
    hit = decay_env(noise(0.12, seed=41, rate_hz=8000), 0.025)
    snap = decay_env(pulse(880, 0.1, 0.25, slide_to=440), 0.03)
    return normalize(mix(0.15, [(0, hit, 0.6), (0, snap, 0.5)]), 0.6)


def sfx_bar_grow():
    """The demon's bar grows: a thick, slow rising swell."""
    grow = env(pulse(note_hz("A2"), 0.45, 0.5, slide_to=note_hz("D3")), 0.05, 0.2, 0.7, 0.15)
    sub = env(triangle(55, 0.45, slide_to=73), 0.05, 0.2, 0.7, 0.15)
    return normalize(lowpass(mix(0.5, [(0, grow, 0.5), (0, sub, 0.7)]), 0.3), 0.6)


def sfx_soul_warning():
    """The bar nears the soul's line: two low warning tones."""
    tone = lambda: env(pulse(note_hz("D#3"), 0.18, 0.5), 0.005, 0.05, 0.6, 0.05)
    return normalize(lowpass(mix(0.5, [(0, tone(), 0.6), (0.25, tone(), 0.6)]), 0.4), 0.58)


def sfx_node_select():
    return normalize(mix(0.12, [(0, decay_env(pulse(note_hz("G5"), 0.05, 0.25), 0.015), 0.5),
                                (0.05, decay_env(pulse(note_hz("D6"), 0.05, 0.25), 0.015), 0.4)]), 0.45)


def sfx_chapter_title():
    """A chapter opens: a low drum roll and a held minor chord."""
    roll = sum(decay_env(noise(0.06, seed=50 + k), 0.015) for k in range(1)) if False else None
    parts = [(k * 0.05, decay_env(noise(0.06, seed=50 + k, rate_hz=3000), 0.02), 0.25 + 0.03 * k) for k in range(10)]
    chord = sum(env(pulse(note_hz(n), 1.2, 0.25), 0.02, 0.3, 0.5, 0.6) for n in ("A3", "C4", "E4"))
    parts.append((0.5, chord, 0.3))
    return normalize(mix(1.8, parts), 0.62)


def sfx_loot():
    """The spoils: a chest creaks open, a sparkle."""
    creak = env(pulse(180, 0.3, 0.125, slide_to=240), 0.01, 0.1, 0.5, 0.1)
    sparkle = _arp(["C6", "E6", "G6", "C7"], 0.05, half_life=0.06)
    return normalize(mix(0.7, [(0, creak, 0.4), (0.25, sparkle, 0.4)]), 0.6)


def sfx_market_buy():
    """A purchase: coins counted out, a stamp."""
    stamp = decay_env(noise(0.08, seed=55, rate_hz=4000), 0.02)
    return normalize(mix(0.3, [(0, _coins(2, 57), 0.5), (0.15, stamp, 0.5)]), 0.55)


def sfx_fire():
    """The purgatory fire: a crackling swell."""
    t = np.arange(seconds(1.2), dtype=np.float32) / RATE
    crackle = noise(1.2, seed=59, rate_hz=5000) * (np.random.default_rng(61).uniform(0, 1, len(t)) > 0.97)
    roar = lowpass(noise(1.2, seed=63), 0.05) * np.sin(np.pi * t / 1.2) * 4
    return normalize((crackle * 0.6 + roar).astype(np.float32), 0.58)


def sfx_event_open():
    """A stranger's offer: a curious two-note question."""
    a = env(triangle(note_hz("E5"), 0.15), 0.01, 0.05, 0.6, 0.05)
    b = env(triangle(note_hz("A#5"), 0.25), 0.01, 0.05, 0.6, 0.12)
    return normalize(mix(0.45, [(0, a, 0.5), (0.14, b, 0.5)]), 0.5)


def sfx_satchel_put():
    """A card into the satchel: a cloth rustle, a soft drop."""
    rustle = lowpass(noise(0.15, seed=65), 0.2) * np.linspace(1, 0, seconds(0.15), dtype=np.float32)
    drop = decay_env(triangle(160, 0.1, slide_to=110), 0.03)
    return normalize(mix(0.2, [(0, rustle, 0.6), (0.08, drop, 0.5)]), 0.5)


def sfx_satchel_take():
    rustle = lowpass(noise(0.12, seed=67), 0.25) * np.linspace(0.3, 1, seconds(0.12), dtype=np.float32)
    blip = decay_env(pulse(note_hz("C6"), 0.05, 0.25), 0.015)
    return normalize(mix(0.18, [(0, rustle, 0.6), (0.11, blip, 0.35)]), 0.5)


def sfx_prophecy_see():
    """The prophecy: a glassy shimmer."""
    parts = [(k * 0.04, decay_env(triangle(note_hz(n), 0.3), 0.08), 0.35) for k, n in enumerate(["B5", "F#6", "B6"])]
    return normalize(mix(0.45, parts), 0.45)


def sfx_prophecy_bottom():
    """A card sent to the bottom: a falling glassy sweep."""
    sweep = decay_env(triangle(1800, 0.3, slide_to=400), 0.1)
    return normalize(mix(0.35, [(0, sweep, 0.6)]), 0.5)


def sfx_tax_gain():
    return normalize(mix(0.25, [(0, _coins(1, 69), 0.5), (0.06, decay_env(pulse(note_hz("A5"), 0.12, 0.25), 0.04), 0.4)]), 0.5)


def sfx_tax_spend():
    seal = decay_env(noise(0.1, seed=71, rate_hz=3500), 0.025)
    return normalize(mix(0.25, [(0, _coins(2, 73, rising=False), 0.5), (0.1, seal, 0.5)]), 0.52)


def sfx_seize():
    """A seizure: a quick grab, a hard royal stamp."""
    swipe = decay_env(noise(0.1, seed=75, rate_hz=9000), 0.03)
    stamp = env(pulse(note_hz("G3"), 0.25, 0.5), 0.003, 0.08, 0.4, 0.1)
    return normalize(mix(0.35, [(0, swipe, 0.5), (0.08, stamp, 0.6)]), 0.6)


def sfx_rule_interest():
    """Mammon's interest: a ledger's tick and a heavy coin."""
    tick = decay_env(pulse(1200, 0.04, 0.125), 0.01)
    coin = decay_env(pulse(note_hz("D4"), 0.25, 0.25) * 0.6 + triangle(note_hz("D3"), 0.25) * 0.4, 0.08)
    return normalize(mix(0.4, [(0, tick, 0.4), (0.06, coin, 0.6)]), 0.55)


def sfx_rule_swap():
    """Belial's role swap: two cards crossing, a theatrical flourish."""
    a = decay_env(pulse(600, 0.2, 0.25, slide_to=1200), 0.06)
    b = decay_env(pulse(1200, 0.2, 0.25, slide_to=600), 0.06)
    return normalize(mix(0.3, [(0, a, 0.5), (0.03, b, 0.5)]), 0.55)


def sfx_rule_temptation():
    """Lilith's temptation: a soft descending sigh."""
    sigh = env(triangle(note_hz("E5"), 0.5, slide_to=note_hz("A4")), 0.05, 0.2, 0.6, 0.2)
    return normalize(lowpass(mix(0.55, [(0, sigh, 0.6), (0.02, noise(0.4, seed=77) * 0.05, 1.0)]), 0.45), 0.5)


def sfx_rule_mirror():
    """Lucifer's mirror: the same note, struck and echoed back lower."""
    a = decay_env(pulse(note_hz("C5"), 0.3, 0.5), 0.08)
    b = decay_env(pulse(note_hz("C4"), 0.3, 0.5), 0.08)
    return normalize(mix(0.6, [(0, a, 0.5), (0.22, b, 0.45)]), 0.55)


def sfx_win_blaze():
    """Demo 3.1: a straight flush, a royal flush: a long rising fanfare over a roar of flame, a held major chord, a gong under it."""
    rise = _arp(["C5", "E5", "G5", "C6", "E6", "G6", "C7"], 0.08)
    chord = sum(env(pulse(note_hz(n), 1.4, 0.25), 0.01, 0.3, 0.5, 0.8) for n in ("C5", "E5", "G5", "C6"))
    t = np.arange(seconds(2.2), dtype=np.float32) / RATE
    roar = lowpass(noise(2.2, seed=81), 0.06) * np.sin(np.pi * np.clip(t / 2.2, 0, 1)) * 3
    partials = [(98, 1.0), (98 * 2.76, 0.45), (98 * 5.4, 0.2)]
    gong = sum(a * np.sin(2 * np.pi * f * t) * (0.5 ** (t / (0.7 / (1 + k)))) for k, (f, a) in enumerate(partials)).astype(np.float32)
    return normalize(mix(2.2, [(0, roar.astype(np.float32), 0.35), (0, rise, 0.6), (0.6, chord, 0.35), (0.6, gong, 0.5)]), 0.72)


SFX.update({
    "win_blaze": sfx_win_blaze,
    "coin_win": sfx_coin_win, "coin_win_big": sfx_coin_win_big, "coin_loss": sfx_coin_loss, "coin_loss_big": sfx_coin_loss_big,
    "purse_empty": sfx_purse_empty, "bar_hit": sfx_bar_hit, "bar_grow": sfx_bar_grow, "soul_warning": sfx_soul_warning,
    "node_select": sfx_node_select, "chapter_title": sfx_chapter_title, "loot": sfx_loot, "market_buy": sfx_market_buy, "fire": sfx_fire,
    "event_open": sfx_event_open, "satchel_put": sfx_satchel_put, "satchel_take": sfx_satchel_take, "prophecy_see": sfx_prophecy_see,
    "prophecy_bottom": sfx_prophecy_bottom, "tax_gain": sfx_tax_gain, "tax_spend": sfx_tax_spend, "seize": sfx_seize,
    "rule_interest": sfx_rule_interest, "rule_swap": sfx_rule_swap, "rule_temptation": sfx_rule_temptation, "rule_mirror": sfx_rule_mirror,
})

# ---------------------------------------------------------------------------------------------------------------- Phase 2's music


# Phase 3: the floors' and the maps' loops are long (32 bars or more, about two minutes) so they do not tire: an A and a B part, a breath
# between them (the bass rests, only a quiet pad), a soft bass (triangle), the anvil low (~1 kHz), rare and quiet, a pad under it all
# and now and then a short phrase of melody. Each keeps its chapter's colour: Mammon heavy and metallic, Belial a cabaret, Lilith the night.


def _pad(chord, octave, length, gain=1.0, attack=0.8, release=1.0):
    """A very quiet held chord: soft triangles, slow in and out."""
    return sum(env(triangle(note_hz(n), length), attack, 0.5, 0.7, release) for n in _chord_notes(chord, octave)) * gain


def _anvil(seed, hz=1000):
    """Mammon's anvil, low and dull: a lower ring and filtered noise."""
    ring = pulse(hz, 0.35, 0.5) * 0.35 + triangle(hz * 1.5, 0.35) * 0.25
    return decay_env(lowpass(ring + noise(0.35, seed=seed, rate_hz=6000) * 0.3, 0.35), 0.07)


def _phrase(notes, start, step, gain, parts, voice="pulse", length=1.6, vibrato=0.006):
    """A short phrase of melody: notes (None rests) a step apart from start."""
    for i, n in enumerate(notes):
        if not n:
            continue
        if voice == "pulse":
            s = env(pulse(note_hz(n), step * length, 0.5, vibrato=vibrato), 0.04, 0.3, 0.5, 0.4)
        else:
            s = env(triangle(note_hz(n), step * length), 0.02, 0.3, 0.55, 0.4)
        parts.append((start + i * step, s, gain))


def music_floor_mammon():
    """A floor of Mammon's (Phase 3): his march, long and quiet — A, B, a breath, A again, a turn home. 64 BPM, 32 bars (2 min)."""
    beat, length = _track(64, 32)
    a = ["Am", "Am", "F", "F", "Dm", "Dm", "E", "E"]
    b = ["F", "G", "Am", "Am", "Dm", "F", "E", "E"]
    breath = ["Am", "F", "Dm", "E"]
    home = ["Dm", "E", "Am", "E"]
    bars = [(c, "a") for c in a] + [(c, "b") for c in b] + [(c, "breath") for c in breath] + [(c, "a") for c in a] + [(c, "home") for c in home]
    parts = []
    for bar, (ch, part) in enumerate(bars):
        start = bar * 4 * beat
        parts.append((start, _pad(ch, 3, beat * 4.4), 0.045 if part != "breath" else 0.06))
        if part != "breath":
            root = _chord_notes(ch, 2)[0]
            for b in (0, 2):   # half notes: the march's weight without the drone of every beat
                parts.append((start + b * beat, env(triangle(note_hz(root), beat * 1.8), 0.01, 0.3, 0.6, 0.2), 0.32))
            if part == "b":
                fifth = _chord_notes(ch, 2)[2]
                parts.append((start + 3 * beat, env(triangle(note_hz(fifth), beat * 0.8), 0.01, 0.2, 0.5, 0.1), 0.18))
        if bar % 4 == 0 and part != "breath":
            parts.append((start, _anvil(90 + bar), 0.06))   # every fourth bar, far off
        if part == "breath" and bar % 2 == 0:
            parts.append((start + 2 * beat, decay_env(lowpass(noise(0.6, seed=140 + bar, rate_hz=3000), 0.1), 0.2), 0.05))   # coins settling
    # Now and then a phrase: once in B, once in the second A.
    _phrase(["E5", "D5", "C5", None, "B4", None, "A4", None], 10 * 4 * beat, beat, 0.07, parts)
    _phrase(["C5", None, "D5", "E5", None, None, "D5", "B4"], 24 * 4 * beat, beat, 0.06, parts)
    return normalize(loop_lowpass(mix(length, parts), 0.45), 0.42)


def music_floor_belial():
    """A floor of Belial's (Phase 3): the cabaret, long — the walking bass and the brushes, a breath, a crooned phrase. 96 BPM, 48 bars."""
    beat, length = _track(96, 48)
    swing = beat * 0.62
    a = ["Dm7", "G7", "Cmaj7", "A7"] * 2
    b = ["Fmaj7", "Bb", "Em7", "A7", "Dm7", "G7", "Cmaj7", "A7"]
    breath = ["Dm7", "G7", "Cmaj7", "A7"]
    bars = ([(c, "a") for c in a] + [(c, "a2") for c in a] + [(c, "b") for c in b] + [(c, "breath") for c in breath]
            + [(c, "a") for c in a] + [(c, "b") for c in b] + [(c, "turn") for c in ["Dm7", "G7", "Cmaj7", "A7"]])
    parts = []
    for bar, (ch, part) in enumerate(bars):
        start = bar * 4 * beat
        parts.append((start, _pad(ch.replace("maj7", "").replace("m7", "m").replace("7", ""), 3, beat * 4.3, attack=0.5), 0.03 if part != "breath" else 0.05))
        if part == "breath":
            parts.append((start + swing, decay_env(noise(0.08, seed=300 + bar), 0.02), 0.03))
            continue
        tones = _chord_notes(ch, 2)
        walk = [tones[0], tones[1 % len(tones)], tones[2 % len(tones)], tones[1 % len(tones)]]
        for b in range(4):
            s = start + b * beat
            parts.append((s, env(triangle(note_hz(walk[b]), beat * 0.85), 0.006, 0.1, 0.6, 0.05), 0.36))
            parts.append((s + swing, decay_env(noise(0.07, seed=100 + bar * 4 + b), 0.018), 0.04))
            if b in (1, 3) and part != "a":
                chord = sum(triangle(note_hz(n) * 2, beat * 0.35) for n in _chord_notes(ch, 3))
                parts.append((s + swing * 0.2, env(chord, 0.005, 0.1, 0.4, 0.05), 0.03))
    _phrase(["F5", "E5", "D5", None, "B4", "D5", "G5", None], 8 * 4 * beat, beat, 0.06, parts, vibrato=0.009)
    _phrase(["E5", None, "G5", "B5", "A5", "G5", "E5", None], 28 * 4 * beat, beat, 0.055, parts, vibrato=0.009)
    return normalize(mix(length, parts), 0.42)


def music_floor_lilith():
    """A floor of Lilith's (Phase 3): her arpeggio over a low drone, A and B, a breath of pad, a far phrase. 60 BPM, 32 bars."""
    beat, length = _track(60, 32)
    a = ["Dm", "Bb", "Gm", "A"] * 2
    b = ["F", "C", "Dm", "A", "Bb", "Gm", "A", "A"]
    breath = ["Dm", "Bb", "Gm", "A"]
    bars = [(c, "a") for c in a] + [(c, "b") for c in b] + [(c, "breath") for c in breath] + [(c, "a") for c in a] + [(c, "b") for c in b[:4]]
    parts = []
    t = np.arange(seconds(length), dtype=np.float32) / RATE
    drone = (np.sin(2 * np.pi * note_hz("D2") * t) + 0.3 * np.sin(2 * np.pi * note_hz("A2") * t)).astype(np.float32)
    parts.append((0, drone, 0.05))
    for bar, (ch, part) in enumerate(bars):
        start = bar * 4 * beat
        parts.append((start, _pad(ch, 3, beat * 4.4, attack=1.0, release=1.4), 0.04 if part != "breath" else 0.06))
        if part == "breath":
            continue
        notes = _chord_notes(ch, 3)
        for k in range(8):
            n = notes[[0, 1, 2, 1][k % 4]]
            if part == "b" and k >= 4:
                n = n[:-1] + str(int(n[-1]) + 1)
            parts.append((start + k * beat * 0.5, decay_env(triangle(note_hz(n), beat * 1.5), 0.3), 0.17))
    _phrase(["A5", None, None, "F5", None, "G5", None, None], 12 * 4 * beat, beat * 2, 0.05, parts, length=1.8, vibrato=0.01)
    _phrase(["D6", None, "C6", None, "A5", None, None, None], 26 * 4 * beat, beat * 2, 0.045, parts, length=1.8, vibrato=0.01)
    return normalize(loop_lowpass(mix(length, parts), 0.45), 0.42)


def music_map_mammon():
    """Mammon's vault, on the map (Phase 3): the march slowed to a creep, long — a bass far off, a counting-house bell. 56 BPM, 32 bars."""
    beat, length = _track(56, 32)
    a = ["Am", "F", "Dm", "E"] * 2
    b = ["F", "C", "Dm", "E"] * 2
    bars = [(c, "a") for c in a] + [(c, "b") for c in b] + [(c, "breath") for c in ["Am", "F", "Dm", "E"]] + [(c, "a") for c in a] + [(c, "b") for c in b[:4]]
    parts = []
    for bar, (ch, part) in enumerate(bars):
        start = bar * 4 * beat
        parts.append((start, _pad(ch, 3, beat * 4.4), 0.04))
        if part != "breath":
            parts.append((start, env(triangle(note_hz(_chord_notes(ch, 2)[0]), beat * 3.8), 0.02, 0.4, 0.5, 0.4), 0.3))
            for k, n in enumerate(_chord_notes(ch, 4)):
                parts.append((start + k * beat, decay_env(triangle(note_hz(n), beat), 0.3), 0.08))
        if bar % 8 == 4:
            parts.append((start + 2 * beat, _anvil(80 + bar, 900), 0.05))
        if bar % 8 == 0:
            tt = np.arange(seconds(3), dtype=np.float32) / RATE
            bell = (np.sin(2 * np.pi * note_hz("A5") * tt) + 0.4 * np.sin(2 * np.pi * note_hz("A5") * 2.76 * tt)) * 0.5 ** (tt / 0.6)
            parts.append((start + beat, bell.astype(np.float32), 0.06))
    return normalize(loop_lowpass(mix(length, parts), 0.4), 0.42)


def music_map_belial():
    """Belial's stage, on the map (Phase 3): the music-box waltz behind the curtain, long — the tune comes and goes. 108 BPM, 72 bars of 3."""
    beat, length = _track(108, 72, beats=3)
    a = ["Dm", "A7", "Dm", "G", "Bb", "A7", "Dm", "A7"]
    b = ["F", "C", "Dm", "A7", "Bb", "F", "G", "A7"]
    plan = [(c, "a") for c in a] * 2 + [(c, "b") for c in b] * 2 + [(c, "breath") for c in a[:4] + a[:4]] + [(c, "a") for c in a] * 2 + [(c, "b") for c in b] + [(c, "turn") for c in a]
    tune = ["A5", "F5", "D5", "E5", None, "C#5", "D5", "F5", "A5", "G5", None, "E5"]
    parts = []
    for bar, (ch, part) in enumerate(plan):
        start = bar * 3 * beat
        parts.append((start, _pad(ch.replace("7", ""), 3, beat * 3.3, attack=0.4, release=0.6), 0.03))
        if part == "breath":
            continue
        parts.append((start, env(triangle(note_hz(_chord_notes(ch, 2)[0]), beat * 0.9), 0.005, 0.1, 0.6, 0.05), 0.3))
        for b in (1, 2):
            chord = sum(triangle(note_hz(n) * 2, beat * 0.4) for n in _chord_notes(ch, 3)[:3])
            parts.append((start + b * beat, decay_env(chord, 0.06), 0.05))
    # The music box's tune only in the A parts' first time round and the last A: it comes and goes.
    for first in (0, 40):
        for i, n in enumerate(tune * 4):
            if n and i < 48:
                parts.append(((first * 3 + i) * beat, decay_env(triangle(note_hz(n) * 2, beat * 0.9), 0.12), 0.12))
    return normalize(mix(length, parts), 0.42)


def music_map_lilith():
    """Lilith's night, on the map (Phase 3): slow and violet, long — a pad, a far bell, a few notes of her song. 50 BPM, 32 bars."""
    beat, length = _track(50, 32)
    chords = ["Dm", "Bb", "Gm", "A"] * 4 + ["F", "C", "Dm", "A"] * 2 + ["Dm", "Bb", "Gm", "A"] * 2
    parts = []
    for bar, ch in enumerate(chords):
        start = bar * 4 * beat
        parts.append((start, _pad(ch, 3, beat * 4.3, attack=1.0, release=1.2), 0.07))
        if bar % 4 == 0:
            tt = np.arange(seconds(3), dtype=np.float32) / RATE
            bell = (np.sin(2 * np.pi * note_hz("D6") * tt) + 0.4 * np.sin(2 * np.pi * note_hz("D6") * 2.76 * tt)) * 0.5 ** (tt / 0.7)
            parts.append((start + beat, bell.astype(np.float32), 0.07))
        if 8 <= bar < 12 or 24 <= bar < 28:
            parts.append((start, env(triangle(note_hz(_chord_notes(ch, 2)[0]), beat * 3.9), 0.4, 0.4, 0.6, 0.6), 0.12))
    _phrase(["A5", None, "F5", None, "G5", None, "E5", None], 16 * 4 * beat, beat * 2, 0.05, parts, voice="triangle", length=1.8)
    return normalize(loop_lowpass(mix(length, parts), 0.35), 0.42)


def music_lucifer_call():
    """Before Lucifer: a long-held low organ and a heartbeat, waiting."""
    beat, length = _track(40, 4)
    parts = []
    for bar, ch in enumerate(["Cm", "Cm", "Ab", "G"]):
        organ = sum(pulse(note_hz(n), beat * 4.2, 0.5) * 0.5 for n in _chord_notes(ch, 2))
        parts.append((bar * 4 * beat, env(organ, 0.8, 0.5, 0.8, 0.8), 0.08))
        for b in range(4):
            parts.append(((bar * 4 + b) * beat, decay_env(triangle(55, 0.25, slide_to=40), 0.05), 0.6))
    return normalize(loop_lowpass(mix(length, parts), 0.3), 0.55)


def music_end_free():
    """Freedom: a bright major arpeggio climbing out of the dark."""
    beat, length = _track(92, 8)
    chords = ["C", "G", "Am", "F", "C", "G", "F", "C"]
    parts = []
    for bar, ch in enumerate(chords):
        notes = _chord_notes(ch, 4)
        for k in range(8):
            parts.append(((bar * 8 + k) * beat * 0.5, decay_env(pulse(note_hz(notes[k % 3]), beat * 0.6, 0.25), 0.12), 0.18))
        parts.append((bar * 4 * beat, env(triangle(note_hz(_chord_notes(ch, 2)[0]), beat * 3.9), 0.02, 0.3, 0.7, 0.3), 0.4))
    return normalize(mix(length, parts), 0.55)


def music_end_doom():
    """Damned (an empty purse, a burned soul, the fall): a slow falling minor line over a drone."""
    beat, length = _track(46, 8)
    t = np.arange(seconds(length), dtype=np.float32) / RATE
    drone = (np.sin(2 * np.pi * 55 * t) + 0.4 * np.sin(2 * np.pi * 82.4 * t)).astype(np.float32)
    parts = [(0, drone * 0.4, 1.0)]
    line = ["E4", "D4", "C4", "B3", "A3", "G#3", "A3", None]
    for i, n in enumerate(line):
        if n:
            parts.append((i * 4 * beat, env(pulse(note_hz(n), beat * 3.6, 0.25), 0.1, 0.5, 0.5, 0.8), 0.18))
    return normalize(loop_lowpass(mix(length, parts), 0.35), 0.5)


MUSIC.update({
    "map_mammon": music_map_mammon, "map_belial": music_map_belial, "map_lilith": music_map_lilith,
    "floor_mammon": music_floor_mammon, "floor_belial": music_floor_belial, "floor_lilith": music_floor_lilith,
    "lucifer_call": music_lucifer_call, "end_free": music_end_free, "end_doom": music_end_doom,
})