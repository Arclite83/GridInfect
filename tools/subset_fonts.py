#!/usr/bin/env python3
"""Build the fallback faces under unity/Assets/_Project/Resources/Fonts.

Chakra Petch and Share Tech Mono are Latin faces. Every code point a
docs/strings/<tag>.json uses that neither carries is drawn from a Noto
fallback instead (Ui.Fallbacks, on each face's TMP fallback table), and
this script cuts those fallbacks to exactly the characters the strings
use, so a language costs what it draws and not the whole of Noto:

    Fallback-Sans.ttf   Noto Sans, for Cyrillic and any Latin the design
                        faces lack (instanced at weight 500 to sit with
                        Chakra Petch Medium)
    Fallback-JP.ttf     Noto Sans JP, for Japanese
    Fallback-KR.ttf     Noto Sans KR, for Korean
    Fallback-SC.ttf     Noto Sans SC, for Simplified Chinese
    Fallback-TC.ttf     Noto Sans TC, for Traditional Chinese

Each is cut to the characters of the languages it serves, not to a
script: kanji and hanzi share code points and Japan, the mainland and
Taiwan draw many of them differently, so the same 直 has to come from the
right face, and Ui orders the fallbacks by the language in force.

Run it after a string file changes. The Noto sources are not in the repo
(megabytes each); point --fonts at a folder holding them from google/fonts
(ofl/notosans, ofl/notosansjp, ofl/notosanskr, ofl/notosanssc,
ofl/notosanstc).

Usage: python3 tools/subset_fonts.py --fonts <dir>
"""

import argparse
import io
import json
import sys
import unicodedata
from pathlib import Path

from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

ROOT = Path(__file__).resolve().parent.parent
STRINGS = ROOT / "docs" / "strings"
FONTS = ROOT / "unity" / "Assets" / "_Project" / "Resources" / "Fonts"
DESIGN = ["ChakraPetch-Medium.ttf", "ChakraPetch-Bold.ttf", "ShareTechMono-Regular.ttf"]

# (output, source, weight, the language tags whose missing characters it carries)
SOURCES = [
    ("Fallback-Sans.ttf", "NotoSans.ttf", 500, ["en", "de", "fr", "es", "pt-BR", "it", "tr", "ru"]),
    ("Fallback-JP.ttf", "NotoSansJP.ttf", 500, ["ja"]),
    ("Fallback-KR.ttf", "NotoSansKR.ttf", 500, ["ko"]),
    ("Fallback-SC.ttf", "NotoSansSC.ttf", 500, ["zh-Hans"]),
    ("Fallback-TC.ttf", "NotoSansTC.ttf", 500, ["zh-Hant"]),
]


def is_cjk(cp):
    ch = chr(cp)
    if unicodedata.east_asian_width(ch) in ("W", "F"):
        return True
    return 0x3000 <= cp <= 0x30FF or 0x4E00 <= cp <= 0x9FFF or 0xFF00 <= cp <= 0xFFEF


def strings_codepoints(tags):
    cps = set()
    for tag in tags:
        path = STRINGS / f"{tag}.json"
        if not path.exists():
            continue
        data = json.loads(path.read_text(encoding="utf-8"))
        for key, value in data.items():
            if key.startswith("_"):
                continue
            cps.update(ord(c) for c in value)
    return cps


def covered_by_design():
    cov = set()
    for name in DESIGN:
        cov.update(TTFont(FONTS / name).getBestCmap().keys())
    return cov


def instance(path, weight):
    font = TTFont(path)
    if "fvar" in font:
        axes = {a.axisTag: a for a in font["fvar"].axes}
        loc = {}
        if "wght" in axes:
            loc["wght"] = max(axes["wght"].minValue, min(axes["wght"].maxValue, weight))
        if "wdth" in axes:
            loc["wdth"] = axes["wdth"].defaultValue
        font = instancer.instantiateVariableFont(font, loc, inplace=True, updateFontNames=False)
    return font


def cut(font, cps, out):
    opts = subset.Options()
    opts.layout_features = ["*"]
    opts.name_IDs = ["*"]
    opts.notdef_outline = True
    opts.recalc_bounds = True
    s = subset.Subsetter(opts)
    s.populate(unicodes=sorted(cps))
    s.subset(font)
    font.save(out)


def meta(path, family):
    import uuid
    (path.parent / (path.name + ".meta")).write_text(
        "fileFormatVersion: 2\n"
        f"guid: {uuid.uuid4().hex}\n"
        "TrueTypeFontImporter:\n  externalObjects: {}\n  serializedVersion: 4\n  fontSize: 16\n"
        "  forceTextureCase: -2\n  characterSpacing: 0\n  characterPadding: 1\n  includeFontData: 1\n"
        f"  fontNames:\n  - {family}\n  fallbackFontReferences: []\n  customCharacters: \n"
        "  fontRenderingMode: 0\n  ascentCalculationMode: 1\n  useLegacyBoundsCalculation: 0\n"
        "  shouldRoundAdvances: 1\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    )


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--fonts", required=True)
    args = ap.parse_args()
    src = Path(args.fonts)

    design = covered_by_design()
    for out_name, src_name, weight, tags in SOURCES:
        cps = {cp for cp in strings_codepoints(tags) - design if cp >= 0x20}
        out = FONTS / out_name
        if not cps:
            if out.exists():
                out.unlink()
                (FONTS / (out_name + ".meta")).unlink(missing_ok=True)
            print(f"{out_name}: nothing to carry")
            continue
        font = instance(src / src_name, weight)
        missing = [chr(cp) for cp in cps if cp not in font.getBestCmap()]
        if missing:
            sys.exit(f"subset_fonts: {src_name} lacks {''.join(missing[:20])!r}")
        cut(font, cps, out)
        family = TTFont(out)["name"].getBestFamilyName()
        meta(out, family)
        print(f"{out_name}: {len(cps)} chars from {src_name}, {out.stat().st_size // 1024} KB")


if __name__ == "__main__":
    main()
