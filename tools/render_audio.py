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
CHIME_SAMPLES = RATE * 450 // 1000
CLICK_VOLUME = 0.6
CHIME_VOLUME = 0.5
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


def build_chime(drive=3.0, noise=0.35, variant_len=CHIME_SAMPLES):
    """The solve: two detuned sawtooths (and a sub an octave down) through
    a resonant lowpass whose cutoff sweeps up and back, the pitch sliding
    in from a fifth below, a breath of noise on the attack, and a tanh
    drive. An analogue confirm, not a chip one."""
    out = []
    target = ROOT / 2               # 590 Hz: the click's root an octave down
    phase = [0.0, 0.0, 0.0]
    rates = [1.0, 1.006, 0.5]       # unison, +10 cents, the sub
    gains = [1.0, 0.8, 0.4]
    low = band = 0.0
    seed = 0x9E3779B9
    for n in range(variant_len):
        t = n / RATE
        # pitch: a fifth below at t = 0, settling on the note in ~50 ms
        f0 = target * 2 ** (-(7 / 12) * math.exp(-t / 0.03))
        x = 0.0
        for k in range(3):
            f = f0 * rates[k]
            phase[k] += f / RATE
            if phase[k] >= 1.0:
                phase[k] -= 1.0
            harmonics = max(1, int(16000 / f))
            saw = 0.0
            for h in range(1, min(harmonics, 40) + 1):
                saw += math.sin(2 * math.pi * h * phase[k]) / h
            x += gains[k] * saw * (2 / math.pi)
        seed = (seed * 1664525 + 1013904223) & 0xFFFFFFFF
        white = (seed >> 8) / 8388608.0 - 1.0
        x += noise * white * math.exp(-t * 300)
        # filter: cutoff opens over 50 ms, then closes with a 140 ms tail
        sweep = min(1.0, t / 0.05) * math.exp(-max(0.0, t - 0.05) / 0.14)
        cutoff = 350 + (5200 - 350) * sweep
        fc = 2 * math.sin(math.pi * min(cutoff, 6000) / RATE)
        q = 1 / 3.5
        low += fc * band
        high = x - low - q * band
        band += fc * high
        y = low
        y = math.tanh(y * drive) / math.tanh(drive)
        attack = min(1.0, t / 0.002)
        env = attack * (1.0 if t < 0.06 else math.exp(-(t - 0.06) / 0.16))
        out.append(y * env)
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
        clean = build_chime(drive=1.2, noise=0.0)
        write(out / "solve-clean-synth.wav", mix(wave_hops + [(6 * HOP, clean, CHIME_VOLUME)], 0.9))


if __name__ == "__main__":
    main()
