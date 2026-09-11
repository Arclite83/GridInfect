#!/usr/bin/env python3
"""Render the board's synthesised sounds to WAV, to listen to off-device.

The game synthesises its two sounds at runtime (Game/Audio/HopClickAudio.cs):
the hop click, pitched up a semitone per ray depth to +7, and the solve
chime. This is the same arithmetic in Python, so a change to either can be
heard before it goes near a phone. Keep the two in step by hand.

Writes, into --out (default: ./audio-preview):
  click.wav        one click at the root pitch
  click-ladder.wav eight clicks 40 ms apart, depth 0..7, as a wave plays them
  chime.wav        the solve chime
  solve.wav        a six-hop wave and then the chime, as a solve sounds

Usage: python3 tools/render_audio.py [--out DIR]
"""

import argparse
import math
import struct
import wave
from pathlib import Path

RATE = 44100
ROOT = 1180.0
CLICK_SAMPLES = RATE * 35 // 1000
CHIME_SAMPLES = RATE * 320 // 1000
CLICK_VOLUME = 0.6
CHIME_VOLUME = 0.55
PEAK = 0.9
HOP = 0.040
HOP_PITCH_CAP = 7


def normalise(samples):
    top = max(abs(s) for s in samples)
    if top <= 0:
        return samples
    k = PEAK / top
    return [s * k for s in samples]


# Struck glass. A free bar's modes are inharmonic, 1 : 2.756 : 5.404 :
# 8.933, which is what makes a strike read as glass or metal rather than
# as a note; the higher modes die fastest. The fundamental is two partials
# a few cents apart so it shimmers a little as it fades.
MODES = [1.0, 2.756, 5.404, 8.933]


def strike(length_s, f0, gains, taus, noise, shimmer, seed):
    out = []
    n_total = int(length_s * RATE)
    for n in range(n_total):
        t = n / RATE
        x = 0.0
        for k, ratio in enumerate(MODES):
            f = f0 * ratio
            if f > 18000 or gains[k] <= 0:
                continue
            x += gains[k] * math.sin(2 * math.pi * f * t) * math.exp(-t / taus[k])
        if shimmer > 0:
            x += shimmer * math.sin(2 * math.pi * f0 * 1.004 * t) * math.exp(-t / taus[0])
        seed = (seed * 1664525 + 1013904223) & 0xFFFFFFFF
        white = (seed >> 8) / 8388608.0 - 1.0
        x += noise * white * math.exp(-t * 2500)
        attack = min(1.0, t / 0.0004)
        out.append(x * attack)
    return normalise(out)


def build_click():
    """The hop: a tap on glass. 35 ms, the fundamental at the root and
    the modes above it gone almost at once."""
    return strike(CLICK_SAMPLES / RATE, ROOT,
                  gains=[1.0, 0.55, 0.3, 0.12], taus=[0.016, 0.008, 0.004, 0.002],
                  noise=0.5, shimmer=0.0, seed=0x9E3779B9)


def build_chime(two=True):
    """The solve: a short grace strike at the fifth, then the main strike
    an octave above the click's root with a quieter strike at the root
    under it for body. Snappy — under 0.4 s — and glassy from the modes
    and the shimmer on the fundamental. two=False drops the grace."""
    top = strike(CHIME_SAMPLES / RATE, ROOT * 2,
                 gains=[1.0, 0.45, 0.22, 0.0], taus=[0.19, 0.07, 0.03, 0.01],
                 noise=0.35, shimmer=0.35, seed=0x2545F491)
    body = strike(CHIME_SAMPLES / RATE, ROOT,
                  gains=[1.0, 0.3, 0.1, 0.0], taus=[0.09, 0.04, 0.02, 0.01],
                  noise=0.0, shimmer=0.0, seed=0x2545F491)
    out = [a + 0.45 * b for a, b in zip(top, body)]
    if two:
        grace = strike(0.12, ROOT * 1.4983,
                       gains=[1.0, 0.4, 0.2, 0.0], taus=[0.05, 0.03, 0.015, 0.01],
                       noise=0.3, shimmer=0.0, seed=0x2545F491)
        shift = int(0.055 * RATE)
        out = [(grace[n] * 0.7 if n < len(grace) else 0.0) for n in range(len(out) + shift)]
        for n in range(len(top)):
            out[n + shift] += top[n] + 0.45 * body[n]
    return normalise(out)


def resample_pitch(clip, semitones):
    """What AudioSource.pitch does: play the same samples faster."""
    rate = 2 ** (semitones / 12)
    length = int(len(clip) / rate)
    out = []
    for n in range(length):
        x = n * rate
        i = int(x)
        frac = x - i
        a = clip[i]
        b = clip[i + 1] if i + 1 < len(clip) else 0.0
        out.append(a + (b - a) * frac)
    return out


def mix(events, length_s):
    """events: (start_s, samples, volume)."""
    out = [0.0] * int(length_s * RATE)
    for start, samples, volume in events:
        at = int(start * RATE)
        for n, s in enumerate(samples):
            if at + n < len(out):
                out[at + n] += s * volume
    return out


def write(path, samples):
    with wave.open(str(path), "wb") as fh:
        fh.setnchannels(1)
        fh.setsampwidth(2)
        fh.setframerate(RATE)
        fh.writeframes(b"".join(
            struct.pack("<h", int(max(-1.0, min(1.0, s)) * 32767)) for s in samples))
    peak = max(abs(s) for s in samples)
    rms = math.sqrt(sum(s * s for s in samples) / len(samples))
    print(f"{path.name:18s} {len(samples) / RATE:5.2f} s  peak {peak:.2f}  rms {rms:.3f}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default="audio-preview")
    ap.add_argument("--variants", action="store_true", help="also render alternates to compare")
    args = ap.parse_args()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)

    click = build_click()
    chime = build_chime()
    write(out / "click.wav", mix([(0.0, click, CLICK_VOLUME)], 0.3))
    ladder = [(d * HOP, resample_pitch(click, min(d, HOP_PITCH_CAP)), CLICK_VOLUME) for d in range(8)]
    write(out / "click-ladder.wav", mix(ladder, 0.6))
    write(out / "chime.wav", mix([(0.0, chime, CHIME_VOLUME)], 0.6))
    wave_hops = [(d * HOP, resample_pitch(click, min(d, HOP_PITCH_CAP)), CLICK_VOLUME) for d in range(6)]
    solve = wave_hops + [(6 * HOP, chime, CHIME_VOLUME)]
    write(out / "solve.wav", mix(solve, 0.9))
    if args.variants:
        one = build_chime(two=False)
        write(out / "solve-one-strike.wav", mix(wave_hops + [(6 * HOP, one, CHIME_VOLUME)], 0.9))


if __name__ == "__main__":
    main()
