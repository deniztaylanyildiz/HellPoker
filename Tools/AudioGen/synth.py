"""A tiny chiptune synthesizer for Hell Poker: pulse / triangle / noise voices, envelopes, a step sequencer, wav out.

Everything is numpy arrays of float32 in [-1, 1] at RATE samples per second; mono, 16-bit wav.
"""
import wave

import numpy as np

RATE = 22050


def seconds(n):
    return int(round(n * RATE))


def t_axis(duration):
    return np.arange(seconds(duration), dtype=np.float32) / RATE


def note_hz(name):
    """'A4', 'C#3', 'Eb2' -> frequency. A4 = 440."""
    letters = {"C": -9, "D": -7, "E": -5, "F": -4, "G": -2, "A": 0, "B": 2}
    semis = letters[name[0]]
    rest = name[1:]
    if rest and rest[0] in "#b":
        semis += 1 if rest[0] == "#" else -1
        rest = rest[1:]
    octave = int(rest)
    return 440.0 * 2 ** ((semis + (octave - 4) * 12) / 12.0)


def pulse(freq, duration, duty=0.5, vibrato=0.0, slide_to=None):
    t = t_axis(duration)
    f = np.full_like(t, freq) if slide_to is None else np.linspace(freq, slide_to, len(t), dtype=np.float32)
    if vibrato:
        f = f * (1 + vibrato * np.sin(2 * np.pi * 5.5 * t))
    phase = np.cumsum(f) / RATE
    return np.where((phase % 1.0) < duty, 1.0, -1.0).astype(np.float32)


def triangle(freq, duration, slide_to=None):
    t = t_axis(duration)
    f = np.full_like(t, freq) if slide_to is None else np.linspace(freq, slide_to, len(t), dtype=np.float32)
    phase = np.cumsum(f) / RATE
    return (4 * np.abs((phase % 1.0) - 0.5) - 1).astype(np.float32)


def noise(duration, seed=1, rate_hz=None):
    """White noise; with rate_hz a stepped (lo-fi, NES-like) noise."""
    rng = np.random.default_rng(seed)
    n = seconds(duration)
    if not rate_hz:
        return rng.uniform(-1, 1, n).astype(np.float32)
    step = max(1, int(RATE / rate_hz))
    values = rng.uniform(-1, 1, n // step + 2).astype(np.float32)
    return np.repeat(values, step)[:n]


def env(signal, attack=0.005, decay=0.1, sustain=0.6, release=0.05):
    n = len(signal)
    a, d, r = seconds(attack), seconds(decay), seconds(release)
    curve = np.full(n, sustain, dtype=np.float32)
    a = min(a, n)
    curve[:a] = np.linspace(0, 1, a, dtype=np.float32) if a else curve[:a]
    d_end = min(n, a + d)
    if d_end > a:
        curve[a:d_end] = np.linspace(1, sustain, d_end - a, dtype=np.float32)
    if r and n > 0:
        r = min(r, n)
        curve[n - r:] *= np.linspace(1, 0, r, dtype=np.float32)
    return signal * curve


def decay_env(signal, half_life):
    t = np.arange(len(signal), dtype=np.float32) / RATE
    return signal * (0.5 ** (t / half_life)).astype(np.float32)


def mix(length_seconds, parts):
    """parts: (start_seconds, signal, gain). Wraps around the end (for seamless loops)."""
    n = seconds(length_seconds)
    out = np.zeros(n, dtype=np.float32)
    for start, signal, gain in parts:
        i = seconds(start) % n
        k = len(signal)
        while k > 0:
            take = min(k, n - i)
            out[i:i + take] += signal[len(signal) - k:len(signal) - k + take] * gain
            k -= take
            i = 0
    return out


def lowpass(signal, alpha):
    """One-pole low-pass (alpha 0..1: lower is darker)."""
    out = np.empty_like(signal)
    acc = 0.0
    for i, x in enumerate(signal):
        acc += alpha * (x - acc)
        out[i] = acc
    return out


def loop_lowpass(signal, alpha):
    """A low-pass for a loop: filtered twice round, the second pass kept — the filter's state at the seam matches."""
    return lowpass(np.concatenate([signal, signal]), alpha)[len(signal):]


def fade_edges(signal, fade_in=0.001, fade_out=0.006):
    """No click at the start or the end of a one-shot sound."""
    out = signal.copy()
    a, b = min(len(out), seconds(fade_in)), min(len(out), seconds(fade_out))
    if a: out[:a] *= np.linspace(0, 1, a, dtype=np.float32)
    if b: out[-b:] *= np.linspace(1, 0, b, dtype=np.float32)
    return out


def normalize(signal, peak=0.85):
    m = float(np.max(np.abs(signal))) or 1.0
    return (signal / m * peak).astype(np.float32)


def write(path, signal):
    data = np.clip(signal, -1, 1)
    pcm = (data * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())
