#!/usr/bin/env python3
"""One-shot migration: turn the 11x6 landscape board into a 6x11 portrait one.

The game ships portrait (R-1103), and an 11-wide board on a phone held upright
is a strip. Rather than carry a rotation in the view forever — every input,
tray and render mapping paying a transpose tax — the board itself is
transposed and the data comes with it.

The map is the plain transpose, (i, j) -> (j, i), which is its own inverse and
therefore trivially reviewable:

    cell    new[i][j]      = old[j][i]
    place   (i, j)         -> (j, i)
    arms    L <-> U, R <-> D          (a direction's axis swaps with it)

That makes the transposed game an exact conjugate of the original, so
docs/test_vectors.json stays the proof of the port rather than being
regenerated: the recorded solutions still replay, step for step, against the
transposed rules. Run docs/tools/verify_test_vectors.py afterwards — it
re-derives every golden board, so a wrong transpose fails loudly on 128 levels.

The only rule that is not symmetric by itself is the direction iteration order
in Game::propagatePiece. The original visits L,R,U,D, which under the map is
U,D,L,R; the repel queue is built in that order and resolved in it, so the
order has to travel too (Rules.SpreadOrder).

Idempotent: refuses to run twice.

    python3 tools/transpose_board_to_portrait.py
"""
import json
import os
import sys

OLD_W, OLD_H = 11, 6
NEW_W, NEW_H = OLD_H, OLD_W

# A direction's axis swaps with it under (i, j) -> (j, i).
ARM_MAP = {"L": "U", "U": "L", "R": "D", "D": "R"}
CANON = "LRUD"   # Tile enum name order; never reorder (Schema.cs)


def transpose_cells(cells):
    """Row-major 11x6 -> row-major 6x11, new[i][j] = old[j][i]."""
    assert len(cells) == OLD_W * OLD_H, len(cells)
    return [cells[j * OLD_W + i] for i in range(NEW_H) for j in range(NEW_W)]


def rows_of(cells, width):
    return [cells[r * width:(r + 1) * width] for r in range(len(cells) // width)]


def transpose_tile(name):
    arms = {ARM_MAP[a] for a in name}
    return "".join(a for a in CANON if a in arms)


def transpose_step(step):
    out = dict(step)
    out["i"], out["j"] = step["j"], step["i"]
    if "tile" in step:
        out["tile"] = transpose_tile(step["tile"])
    if "board_after" in step:
        out["board_after"] = transpose_cells(step["board_after"])
    return out


def main():
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    path = os.path.join(root, "docs", "test_vectors.json")
    data = json.load(open(path))

    meta = data["_meta"]
    if meta["board_width"] == NEW_W:
        print("already transposed; nothing to do")
        return 0
    assert meta["board_width"] == OLD_W and meta["board_height"] == OLD_H, "unexpected shape"

    for level in data["levels"].values():
        level["board"] = transpose_cells(level["board"])
        level["board_rows"] = rows_of(level["board"], NEW_W)
        level["pieces"] = [transpose_tile(t) for t in level["pieces"]]
        for key in ("solution", "solution_original_exploit", "steps", "steps_original_exploit"):
            if key in level:
                level[key] = [transpose_step(s) for s in level[key]]

    meta["board_width"] = NEW_W
    meta["board_height"] = NEW_H
    meta["board_encoding"] = (
        f"row-major, index = i*{NEW_W} + j, i=row (0=top), j=col (0=left)")
    meta["orientation"] = (
        "Transposed from the original's 11x6 landscape board by (i, j) -> (j, i), "
        "with piece arms remapped L<->U and R<->D. The game ships portrait "
        "(R-1103); the transpose is an exact conjugate, so these recorded "
        "solutions still replay step for step and remain the proof of the port. "
        "Applied once by tools/transpose_board_to_portrait.py.")

    with open(path, "w") as f:
        json.dump(data, f, indent=1, sort_keys=False)
        f.write("\n")
    print(f"transposed {len(data['levels'])} levels to {NEW_W}x{NEW_H}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
