#!/usr/bin/env python3
"""Bake docs/strings/*.json into unity/Assets/_Project/Game/Strings.g.cs.

The generated table is the only place the game reads prose from. Every
language is baked in: a dozen languages of ~150 short strings is tens of
kilobytes, which buys zero boot IO, no JSON parse at startup, and a table the
adapter compile-check sees. See docs/I18N.md.

Gates, both run here so CI fails on the derivation rather than on a test:
  * every <tag>.json carries exactly en.json's key set, no more and no less;
  * every value's {N} placeholder set matches en.json's, so a translation
    cannot drop or invent a substitution;
  * world.<id>.name matches the name authored in docs/worlds/<id>.jsonl,
    so the display string and the baked world record cannot drift.

Two pseudolocales are generated, never authored (docs/I18N.md):
  qps-ploc   pads and accents Latin text to catch overflow and clipping;
  qps-plocm  reverses it to catch left/right assumptions before an RTL
             language exists.
Both leave placeholders and hard breaks untouched.

Usage: python3 tools/bake_strings.py
"""

import json
import re
import sys
import unicodedata
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
STRINGS = ROOT / "docs" / "strings"
WORLDS = ROOT / "docs" / "worlds"
OUT = ROOT / "unity" / "Assets" / "_Project" / "Game" / "Strings.g.cs"

DEFAULT = "en"
PSEUDO = ("qps-ploc", "qps-plocm")

# {0}, {1:00} and the like. Everything outside these is translatable text.
PLACEHOLDER = re.compile(r"\{\d+(?::[^}]*)?\}")

ACCENT = str.maketrans(
    "aeiouyAEIOUYcnCNsSzZgGdD",
    "áéíóúýÁÉÍÓÚÝçñÇÑšŠžŽğĞđĐ",
)


def die(msg):
    print(f"bake_strings: {msg}", file=sys.stderr)
    sys.exit(1)


def load(tag):
    path = STRINGS / f"{tag}.json"
    with path.open(encoding="utf-8") as fh:
        data = json.load(fh)
    # "_" is the file's own header note, not a string the game shows.
    return {k: v for k, v in data.items() if not k.startswith("_")}


def world_names():
    """The name authored on each world's header line in docs/worlds/*.jsonl."""
    names = {}
    for path in sorted(WORLDS.glob("w*.jsonl")):
        with path.open(encoding="utf-8") as fh:
            header = json.loads(fh.readline())
        world = header["world"]
        names[world["id"]] = world["name"]
    return names


def check_placeholders(tag, base, table):
    for key, want in base.items():
        got = table[key]
        if sorted(PLACEHOLDER.findall(want)) != sorted(PLACEHOLDER.findall(got)):
            die(
                f"{tag}.json: '{key}' placeholders differ from {DEFAULT}.json "
                f"({PLACEHOLDER.findall(want)} vs {PLACEHOLDER.findall(got)})"
            )


def check_keys(tag, base, table):
    missing = sorted(set(base) - set(table))
    extra = sorted(set(table) - set(base))
    if missing:
        die(f"{tag}.json: missing {len(missing)} key(s): {', '.join(missing[:8])}")
    if extra:
        die(f"{tag}.json: {len(extra)} key(s) not in {DEFAULT}.json: {', '.join(extra[:8])}")


def check_worlds(base):
    authored = world_names()
    for wid, name in authored.items():
        key = f"world.{wid}.name"
        if key not in base:
            die(f"{DEFAULT}.json: no '{key}' for the world in docs/worlds/{wid}.jsonl")
        # Case is display, not identity: the JSONL authors the name and
        # en.json authors how it is set, and the world rows are uppercase
        # chips (STYLE-GUIDE 7). Renaming a world still has to touch both.
        if base[key].casefold() != name.casefold():
            die(
                f"{DEFAULT}.json: '{key}' is {base[key]!r} but docs/worlds/{wid}.jsonl "
                f"authors {name!r}. Change both or neither."
            )
    for key in base:
        if key.startswith("world.") and key.endswith(".name"):
            wid = key[len("world."):-len(".name")]
            if wid not in authored:
                die(f"{DEFAULT}.json: '{key}' has no docs/worlds/{wid}.jsonl")


def display_width(text):
    """Latin counts 1 per character, CJK 2. TextMesh does not wrap and the
    tutorial band is one line wide, so what matters is how wide a sentence
    draws, not how many code points it has (docs/I18N.md)."""
    return sum(2 if unicodedata.east_asian_width(c) in ("W", "F") else 1 for c in text)


# The tutorial band holds one line at the body size. Measured from the
# English set, whose longest is "Two bugs. Tap a placed bug to pick it up."
TUTORIAL_WIDTH = 44


def check_tutorial(tag, table):
    """The ten sentences are the whole instruction set for a game that
    teaches nothing else, and they draw into a fixed one-line band. This ran
    as a Core test over English only; here it covers every language, which is
    where a too-long line actually comes from."""
    steps = sorted(
        (int(k.split(".")[1]), k) for k in table if k.startswith("tut.") and k.endswith(".line")
    )
    if not steps:
        die(f"{tag}.json: no tut.<n>.line keys")
    for i, (n, _) in enumerate(steps, start=1):
        if n != i:
            die(f"{tag}.json: tutorial steps must run 1..N with no gaps; found {n} at position {i}")
    for n, key in steps:
        line = table[key]
        if not line.strip():
            die(f"{tag}.json: '{key}' is empty")
        if "\n" in line:
            die(f"{tag}.json: '{key}' is two lines; the tutorial band holds one")
        width = display_width(line)
        if width > TUTORIAL_WIDTH:
            die(
                f"{tag}.json: '{key}' draws {width} wide, over {TUTORIAL_WIDTH}: {line!r}. "
                "Shorten it; the band does not wrap."
            )


def segments(text):
    """Split into (is_placeholder, chunk) so pseudo passes leave {0} alone."""
    out, at = [], 0
    for m in PLACEHOLDER.finditer(text):
        if m.start() > at:
            out.append((False, text[at:m.start()]))
        out.append((True, m.group()))
        at = m.end()
    if at < len(text):
        out.append((False, text[at:]))
    return out


def pad(chunk):
    """Grow Latin text ~40% the way a German or Finnish translation does."""
    letters = [c for c in chunk if c.isalpha()]
    if not letters:
        return chunk
    return chunk + "·" * max(1, round(len(letters) * 0.4))


def ploc(text):
    """Accent and lengthen. Hard breaks stay breaks; each line is bracketed."""
    out = []
    for line in text.split("\n"):
        body = "".join(
            chunk if is_ph else pad(chunk.translate(ACCENT))
            for is_ph, chunk in segments(line)
        )
        out.append(f"[{body}]")
    return "\n".join(out)


def plocm(text):
    """Mirror the line: the whole run of characters and placeholders in
    reverse, each placeholder kept intact. "SOLVE {0:00}" becomes
    "{0:00} EVLOS", so the number lands on the far side of the word with the
    space still between them, as a bidi renderer would lay it. string.Format
    does not care which order {0} and {1} appear in, and check_placeholders
    compares sets. Reversing only the text and leaving the placeholders put
    moved every such space to the wrong side of its word.

    No RLE/PDF marks around the result: the renderer has no bidi and would
    draw them as glyphs, and the layout is what this pseudolocale tests, not
    shaping. A bidi-aware renderer is tested with a real RTL language."""
    out = []
    for line in text.split("\n"):
        tokens = []
        for is_ph, chunk in segments(line):
            tokens.extend([chunk] if is_ph else list(chunk))
        out.append("".join(reversed(tokens)))
    return "\n".join(out)


def prop(key):
    """nav.menu -> NavMenu, cal.month.10.short -> CalMonth10Short."""
    parts = re.split(r"[.\-_]", key)
    return "".join(p[:1].upper() + p[1:] for p in parts if p)


def literal(text):
    esc = (
        text.replace("\\", "\\\\")
        .replace('"', '\\"')
        .replace("\n", "\\n")
        .replace("\r", "\\r")
        .replace("\t", "\\t")
    )
    # Keep the generated file plain ASCII so it reads the same on every
    # machine and in every diff; the pseudolocales are full of accents.
    out = []
    for ch in esc:
        out.append(ch if ord(ch) < 128 else f"\\u{ord(ch):04x}")
    return '"' + "".join(out) + '"'


def family(keys, pattern):
    """Ordered indices for a numbered key family, e.g. cal.month.<n>."""
    found = {}
    for i, key in enumerate(keys):
        m = pattern.fullmatch(key)
        if m:
            found[int(m.group(1))] = i
    return [found[n] for n in sorted(found)]


def emit(keys, tables, tags):
    idx = {k: i for i, k in enumerate(keys)}
    months = family(keys, re.compile(r"cal\.month\.(\d+)"))
    months_short = family(keys, re.compile(r"cal\.month\.(\d+)\.short"))
    days = family(keys, re.compile(r"cal\.day\.(\d+)"))
    tut = family(keys, re.compile(r"tut\.(\d+)\.line"))
    difficulty = [
        idx[f"freeplay.difficulty.{n}"]
        for n in ("beginner", "easy", "medium", "hard", "challenging")
    ]
    worlds = sorted(k for k in keys if k.startswith("world.") and k.endswith(".name"))

    L = []
    add = L.append
    add("// <auto-generated> tools/bake_strings.py from docs/strings/*.json.")
    add("// Do not edit; regenerate. Authoring notes live in docs/I18N.md.")
    add("namespace GridInfect.Game")
    add("{")
    add("    public static partial class Str")
    add("    {")
    add(f"        public const string DefaultTag = {literal(DEFAULT)};")
    add("")
    add("        // Every shipped tag, default first. The selector draws this")
    add("        // list minus the pseudolocales (docs/I18N.md).")
    add("        public static readonly string[] Tags =")
    add("        {")
    for tag in tags:
        add(f"            {literal(tag)},")
    add("        };")
    add("")
    add("        public static readonly bool[] IsPseudo =")
    add("        {")
    for tag in tags:
        add(f"            {'true' if tag in PSEUDO else 'false'},")
    add("        };")
    add("")
    for tag in tags:
        add(f"        // {tag}")
        add(f"        static readonly string[] {prop(tag)} =")
        add("        {")
        for key in keys:
            add(f"            {literal(tables[tag][key])},   // {key}")
        add("        };")
        add("")
    add("        static readonly string[][] Tables =")
    add("        {")
    for tag in tags:
        add(f"            {prop(tag)},")
    add("        };")
    add("")
    add("        static string[] _v = Tables[0];")
    add("        static int _tag;")
    add("")
    add("        // The tag in force. Set through Str.Use (Strings.cs).")
    add("        public static string CurrentTag => Tags[_tag];")
    add("")
    add("        static void Bind(int tag)")
    add("        {")
    add("            _tag = tag;")
    add("            _v = Tables[tag];")
    add("        }")
    add("")
    add("        // ---- families ----")
    add("")
    add(f"        static readonly int[] MonthIdx = {{ {', '.join(map(str, months))} }};")
    add(f"        static readonly int[] MonthShortIdx = {{ {', '.join(map(str, months_short))} }};")
    add(f"        static readonly int[] DayIdx = {{ {', '.join(map(str, days))} }};")
    add(f"        static readonly int[] TutIdx = {{ {', '.join(map(str, tut))} }};")
    add(f"        static readonly int[] DifficultyIdx = {{ {', '.join(map(str, difficulty))} }};")
    add("")
    add("        // Month 1-12, weekday 1-7 Monday first, tutorial step 1-based,")
    add("        // difficulty by its enum ordinal. Out of range is a bug, not a")
    add("        // missing translation, so these throw rather than blank.")
    add("        public static string Month(int month) => _v[MonthIdx[month - 1]];")
    add("        public static string MonthShort(int month) => _v[MonthShortIdx[month - 1]];")
    add("        public static string Day(int isoWeekday) => _v[DayIdx[isoWeekday - 1]];")
    add("        public static string TutorialLine(int step) => _v[TutIdx[step - 1]];")
    add("        public static string DifficultyName(int ordinal) => _v[DifficultyIdx[ordinal]];")
    add("")
    add("        public static string WorldName(string worldId)")
    add("        {")
    add("            switch (worldId)")
    add("            {")
    for key in worlds:
        wid = key[len("world."):-len(".name")]
        add(f"                case {literal(wid)}: return _v[{idx[key]}];")
    add("            }")
    add("            return worldId;")
    add("        }")
    add("")
    add("        // ---- keys ----")
    add("")
    seen = set()
    for key in keys:
        name = prop(key)
        if name in seen:
            die(f"two keys collide on property name {name}")
        seen.add(name)
        add(f"        public static string {name} => _v[{idx[key]}];")
    add("    }")
    add("}")
    return "\n".join(L) + "\n"


def main():
    base = load(DEFAULT)
    if not base:
        die(f"{DEFAULT}.json is empty")
    check_worlds(base)
    check_tutorial(DEFAULT, base)

    authored = sorted(
        p.stem for p in STRINGS.glob("*.json") if p.stem not in PSEUDO
    )
    if DEFAULT not in authored:
        die(f"no docs/strings/{DEFAULT}.json")
    authored.remove(DEFAULT)

    tags = [DEFAULT] + authored + list(PSEUDO)
    tables = {DEFAULT: base}
    for tag in authored:
        table = load(tag)
        check_keys(tag, base, table)
        check_placeholders(tag, base, table)
        check_tutorial(tag, table)
        tables[tag] = table
    tables["qps-ploc"] = {k: ploc(v) for k, v in base.items()}
    tables["qps-plocm"] = {k: plocm(v) for k, v in base.items()}

    # en.json's own order is the table order, so a diff of the generated file
    # reads like a diff of the source.
    keys = list(base.keys())
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(emit(keys, tables, tags), encoding="utf-8")
    print(f"wrote {OUT.relative_to(ROOT)} ({len(keys)} keys x {len(tags)} tags)")


if __name__ == "__main__":
    main()
