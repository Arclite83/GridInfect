#!/usr/bin/env python3
"""Rewrite the shipped BoardPalette asset from the code defaults.

BoardPalette.cs (the ship skin) -> _Project/Resources/BoardPalette.asset

BoardPalette.Default loads Resources first, so the asset — not the code —
is what the game draws. Editing a colour in C# alone changes nothing on
screen; the asset silently keeps the old one. This makes the asset a
derived file: the C# defaults are the source, CI diffs the result.

    python3 tools/sync_palette_asset.py
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = os.path.join(ROOT, "unity", "Assets", "_Project")
SOURCE = os.path.join(PROJECT, "Game", "View", "BoardPalette.cs")
ASSET = os.path.join(PROJECT, "Resources", "BoardPalette.asset")

HEADER = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 20dc65c4de4f4856849afb2ae8decece, type: 3}
  m_Name: BoardPalette
  m_EditorClassIdentifier: 
"""

COLOR = re.compile(r'public Color (\w+) = (Alpha\()?Hex\("#([0-9A-Fa-f]{6})"\)(?:, ([\d.]+)f\))?;')
NUMBER = re.compile(r'public float (\w+) = ([\d.]+)f;')


def num(value):
    """Unity's own float form: six decimals, trailing zeros dropped."""
    text = f"{round(value, 6):.6f}".rstrip("0").rstrip(".")
    return text or "0"


def channel(hex6, index):
    return int(hex6[index * 2:index * 2 + 2], 16) / 255.0


def main():
    with open(SOURCE, encoding="utf-8") as handle:
        source = handle.read()

    # Only the field defaults: everything past Skins.Apply is a swap, not the
    # ship skin, and the getters below it are not fields at all.
    source = source.split("public static Color Hex(")[0]

    lines = []
    for line in source.splitlines():
        color = COLOR.search(line)
        if color:
            name, _, hex6, alpha = color.groups()
            rgba = [channel(hex6, i) for i in range(3)] + [float(alpha) if alpha else 1.0]
            r, g, b, a = (num(v) for v in rgba)
            lines.append(f"  {name}: {{r: {r}, g: {g}, b: {b}, a: {a}}}")
            continue
        number = NUMBER.search(line)
        if number:
            lines.append(f"  {number.group(1)}: {num(float(number.group(2)))}")

    if len(lines) < 20:
        sys.exit(f"only parsed {len(lines)} fields out of {SOURCE}; the pattern moved")

    with open(ASSET, "w", encoding="utf-8") as handle:
        handle.write(HEADER + "\n".join(lines) + "\n")
    print(f"{ASSET}: {len(lines)} fields")


if __name__ == "__main__":
    main()
