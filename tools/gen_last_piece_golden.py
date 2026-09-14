#!/usr/bin/env python3
"""Golden order constraints for the 128 classic levels.

A board with a repel switch or a reset trap cares about the order its
pieces go down: a placement whose arm ends on a trap resets the board
unless it is the one that wins, and a placement whose arm ends on a switch
repels infection back off the board unless the win check gets there first
(RULES.md 4.1). So some levels have a *last piece* - a placement that can
only ever be the final one - and pinning it early (the Lock tool) leaves a
board nothing can finish.

This walks every placement order of each level's recorded solution through
the reference rules (docs/tools/verify_test_vectors.Game) and writes
docs/last_piece_classic.json: per level, the winning orders, the placements
that only ever win last, and the ones that can never open a solve.
GridInfect.Core.Solving.PlacementOrder is the C# product; this file is the
oracle it must match (PlacementOrderTests).

    python3 tools/gen_last_piece_golden.py
"""
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "docs", "tools"))
from verify_test_vectors import Game, W  # noqa: E402

OUTPUT = os.path.join(ROOT, "docs", "last_piece_classic.json")

WON, ALIVE, DEAD = "won", "alive", "dead"


def replay(board, tiles, order):
    """Play `order` (a list of (piece, cell)) from a fresh board."""
    g = Game(board, tiles)
    outcome = ALIVE
    for piece, cell in order:
        i, j = cell // W, cell % W
        if not g.can_place(piece, i, j):
            return DEAD
        if g.set_piece(piece, i, j):
            return WON
        if not g.placed:
            return DEAD          # a trap tripped: the board is back to empty
    return outcome


def walk(board, tiles, placements, prefix, used, seen):
    """Depth-first over the orders of `placements`, recording for each one
    whether it ever wins, ever precedes a win, and ever opens one. Returns
    True when some order below `prefix` wins."""
    any_win = False
    for n, p in enumerate(placements):
        if used[n]:
            continue
        prefix.append(p)
        outcome = replay(board, tiles, prefix)
        if outcome == WON:
            seen["winning"][n] = True
            seen["orders"] += 1
            if len(prefix) == 1:
                seen["first"][n] = True
            any_win = True
        elif outcome == ALIVE:
            used[n] = True
            won = walk(board, tiles, placements, prefix, used, seen)
            used[n] = False
            if won:
                seen["non_final"][n] = True
                if len(prefix) == 1:
                    seen["first"][n] = True
                any_win = True
        prefix.pop()
    return any_win


def analyse(level):
    board, tiles = level["board"], level["pieces"]
    placements = [(s["piece_index"], s["i"] * W + s["j"]) for s in level["solution"]]
    seen = {
        "orders": 0,
        "winning": [False] * len(placements),
        "non_final": [False] * len(placements),
        "first": [False] * len(placements),
    }
    walk(board, tiles, placements, [], [False] * len(placements), seen)
    text = [f"{piece}@{cell}" for piece, cell in placements]
    return {
        "id": level["level_id"],
        "pieces": len(placements),
        "switches": 3 in board,
        "traps": 5 in board,
        "orders": seen["orders"],
        # Only ever the winning placement, so never safe to pin early.
        "last_only": [text[n] for n in range(len(placements))
                      if len(placements) > 1 and seen["winning"][n] and not seen["non_final"][n]],
        "never_first": [text[n] for n in range(len(placements)) if not seen["first"][n]],
    }


def main():
    levels = json.load(open(os.path.join(ROOT, "docs", "test_vectors.json")))["levels"]
    rows = []
    for key in sorted(levels, key=int):
        row = analyse(levels[key])
        if row["orders"] == 0:
            sys.exit(f"level {key}: the recorded solution wins in no order")
        rows.append(row)
        print(f"level {key}: {row['orders']} winning orders, "
              f"last piece {row['last_only'] or '-'}", flush=True)
    constrained = [r for r in rows if r["last_only"]]
    with open(OUTPUT, "w") as f:
        json.dump({"_meta": {
            "source": "tools/gen_last_piece_golden.py over docs/test_vectors.json",
            "semantics": "orders = placement orders of the recorded solution that win; "
                         "last_only = placements that win only as the final placement "
                         "(never safe to lock early); never_first = placements that cannot "
                         "open a winning order",
        }, "levels": rows}, f, indent=1)
        f.write("\n")
    print(f"wrote {OUTPUT}: {len(rows)} levels, {len(constrained)} with a last piece")


if __name__ == "__main__":
    main()
