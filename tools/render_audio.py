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
CLICK_SAMPLES = RATE * 30 // 1000
CHIME_SAMPLES = RATE * 900 // 1000
CLICK_VOLUME = 0.6
CHIME_VOLUME = 0.6
PEAK = 0.9
HOP = 0.040
HOP_PITCH_CAP = 7


def normalise(samples):
    top = max(abs(s) for s in samples)
    if top <= 0:
        return samples
    k = PEAK / top
    return [s * k for s in samples]


def build_click():
    out = []
    noise = 0x9E3779B9
    for n in range(CLICK_SAMPLES):
        t = n / RATE
        envelope = math.exp(-t * 70.0)
        body = math.sin(2 * math.pi * ROOT * t)
        sub = 0.5 * math.sin(2 * math.pi * (ROOT / 2) * t) * math.exp(-t * 120.0)
        snap = 0.4 * math.sin(2 * math.pi * 3300.0 * t) * math.exp(-t * 400.0)
        noise = (noise * 1664525 + 1013904223) & 0xFFFFFFFF
        white = (noise >> 8) / 8388608.0 - 1.0
        tick = 0.6 * white * math.exp(-t * 1500.0)
        out.append((body + sub + snap + tick) * envelope)
    return normalise(out)


def build_chime():
    out = [0.0] * CHIME_SAMPLES
    notes = [ROOT, ROOT * 1.2599, ROOT * 1.4983, ROOT * 2.0]
    gap = 0.09
    for k, f in enumerate(notes):
        last = k == len(notes) - 1
        tau = 0.35 if last else 0.16
        gain = 1.0 if last else 0.8
        first = round(k * gap * RATE)
        for n in range(first, CHIME_SAMPLES):
            t = (n - first) / RATE
            attack = min(1.0, t / 0.003)
            envelope = gain * attack * math.exp(-t / tau)
            tone = (math.sin(2 * math.pi * f * t)
                    + 0.25 * math.sin(2 * math.pi * 2 * f * t)
                    + 0.2 * math.sin(2 * math.pi * (f / 2) * t))
            out[n] += tone * envelope
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
    args = ap.parse_args()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)

    click = build_click()
    chime = build_chime()
    write(out / "click.wav", mix([(0.0, click, CLICK_VOLUME)], 0.3))
    ladder = [(d * HOP, resample_pitch(click, min(d, HOP_PITCH_CAP)), CLICK_VOLUME) for d in range(8)]
    write(out / "click-ladder.wav", mix(ladder, 0.6))
    write(out / "chime.wav", mix([(0.0, chime, CHIME_VOLUME)], 1.0))
    wave_hops = [(d * HOP, resample_pitch(click, min(d, HOP_PITCH_CAP)), CLICK_VOLUME) for d in range(6)]
    solve = wave_hops + [(6 * HOP, chime, CHIME_VOLUME)]
    write(out / "solve.wav", mix(solve, 1.3))


if __name__ == "__main__":
    main()
