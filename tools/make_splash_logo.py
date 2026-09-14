#!/usr/bin/env python3
"""Bloodhound Studios splash mark, cut from the 2014 launch image.

The studio mark is not one of the generated marks in grid-infect-style: it
predates the generator and the only masters left in the tree are the cocos2d-x
launch images, all of which carry the same artwork flattened onto white. This
lifts it off that white, so the mark can sit on the Unity splash screen's
green (ProjectSettings m_SplashScreenBackgroundColor) instead of on a plate.

  python3 tools/make_splash_logo.py [--out DIR] [--brand] [--preview]

Writes bloodhound_studios_mono.png, the white cut the splash points at;
--brand also writes the original blue/black cut, which nothing ships but
which is the only transparent copy of the mark; --preview composites each
on the splash green. Needs Pillow.

The mono cut is not a flat silhouette: the mark's black is doing two jobs,
line work inside the hound and the STUDIOS wordmark, and flattening both to
white loses the legs. Black that touches blue is line work and is knocked
out, so the background reads through it; black that touches nothing is type
and turns white. See ink_roles().
"""
import argparse
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
# The iPad landscape launch image is the only lossless copy: Resources/
# bloodhound_studios_splash.jpg is the same artwork through a JPEG.
SRC = ROOT / "grid-infect-cocos2dx/proj.ios/Default-Landscape@2x~ipad.png"
DEFAULT_OUT = ROOT / "unity/Assets/_Project/Art/Splash"

BLUE = (36, 103, 184)      # the mark's blue, sampled from the master
SPLASH_BG = (95, 139, 74)  # m_SplashScreenBackgroundColor, = style maskLo
BG_MIN = 250               # >= this on every channel is bare white
OPAQUE = 0.98              # above this coverage, keep the master's own pixel


def coverage(r, g, b):
    """Alpha of one pixel of two-ink art composited over white.

    Both inks sit on white, so P = a*F + (1-a)*255 per channel and the red
    channel carries the most contrast for either ink. Which ink is solved
    for is decided by how blue the pixel is relative to how dark it is:
    blue over white holds (b-r)/(255-r) = 0.68 at every coverage, black
    holds 0. Blends of black over blue land above OPAQUE and keep their
    own colour, so the two-ink assumption only has to hold on the edges.
    """
    dark = 255 - r
    if dark <= 0:
        return 0.0, (0, 0, 0)
    ink = BLUE if (b - r) > 0.34 * dark else (0, 0, 0)
    return min(1.0, dark / (255 - ink[0])), ink


INK_BLUE, INK_BLACK = 1, 2
LINE_L = 50  # luminance below this, in an opaque pixel, is the black ink


def luma(r, g, b):
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def ink_roles(ink, w, h):
    """Split the black ink into line work and type.

    One flattened raster is all that survives of the mark, so the two jobs
    its black does are told apart by adjacency rather than by layer: the
    hound's outline runs against the blue fill, the STUDIOS wordmark touches
    nothing. Flood each black run and keep the ones that never meet blue.
    """
    type_ink = bytearray(w * h)
    seen = bytearray(w * h)
    for start in range(w * h):
        if seen[start] or ink[start] != INK_BLACK:
            continue
        stack, run, touches_blue = [start], [], False
        seen[start] = 1
        while stack:
            i = stack.pop()
            run.append(i)
            x, y = i % w, i // w
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    nx, ny = x + dx, y + dy
                    if not (0 <= nx < w and 0 <= ny < h):
                        continue
                    j = ny * w + nx
                    if ink[j] == INK_BLUE:
                        touches_blue = True
                    elif ink[j] == INK_BLACK and not seen[j]:
                        seen[j] = 1
                        stack.append(j)
        if not touches_blue:
            for i in run:
                type_ink[i] = 1
    return type_ink


def cut(src):
    px = src.convert("RGB")
    w, h = px.size
    d = px.load()
    x0, y0, x1, y1 = w, h, -1, -1
    for y in range(h):
        for x in range(w):
            r, g, b = d[x, y]
            if min(r, g, b) < BG_MIN:
                x0, x1 = min(x0, x), max(x1, x)
                y0, y1 = min(y0, y), max(y1, y)
    if x1 < 0:
        raise SystemExit(f"{src.filename}: all white, nothing to cut")

    cw, ch = x1 - x0 + 1, y1 - y0 + 1
    alpha = [0.0] * (cw * ch)
    rgb = [None] * (cw * ch)
    ink = bytearray(cw * ch)
    for y in range(ch):
        for x in range(cw):
            r, g, b = d[x0 + x, y0 + y]
            if min(r, g, b) >= BG_MIN:
                continue
            a, edge_ink = coverage(r, g, b)
            if a <= 0:
                continue
            i = y * cw + x
            alpha[i] = a
            if a >= OPAQUE:
                rgb[i] = (r, g, b)
                ink[i] = INK_BLUE if luma(r, g, b) > LINE_L else INK_BLACK
            else:
                rgb[i] = edge_ink
                ink[i] = INK_BLUE if edge_ink == BLUE else INK_BLACK

    type_ink = ink_roles(ink, cw, ch)
    brand = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    mono = Image.new("RGBA", (cw, ch), (255, 255, 255, 0))
    bp, mp = brand.load(), mono.load()
    for i, a in enumerate(alpha):
        if a <= 0:
            continue
        x, y = i % cw, i // cw
        bp[x, y] = (*rgb[i], round(a * 255))
        # Line work is a hole in the mono cut; blue and type are the mark.
        keep = ink[i] != INK_BLACK or type_ink[i]
        mp[x, y] = (255, 255, 255, round(a * 255) if keep else 0)
    return brand, mono


def rel(p):
    try:
        return p.relative_to(ROOT)
    except ValueError:
        return p


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", type=Path, default=DEFAULT_OUT,
                    help="where the mono cut lands (default: the Unity splash folder)")
    ap.add_argument("--brand", action="store_true",
                    help="also write the blue/black cut (not used by the splash)")
    ap.add_argument("--preview", action="store_true",
                    help="also write each cut composited on the splash green")
    args = ap.parse_args()
    args.out.mkdir(parents=True, exist_ok=True)

    with Image.open(SRC) as src:
        brand, mono = cut(src)
    wanted = [("bloodhound_studios_mono", mono)]
    if args.brand:
        wanted.append(("bloodhound_studios_brand", brand))
    for name, im in wanted:
        f = args.out / f"{name}.png"
        im.save(f, optimize=True)
        print(f"  {rel(f)}  {im.width}x{im.height}")
        if args.preview:
            pad = 48
            bg = Image.new("RGB", (im.width + 2 * pad, im.height + 2 * pad), SPLASH_BG)
            bg.paste(im, (pad, pad), im)
            q = args.out / f"{name}_on_green.png"
            bg.save(q)
            print(f"  {rel(q)}  preview")


if __name__ == "__main__":
    main()
