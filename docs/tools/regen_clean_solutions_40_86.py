#!/usr/bin/env python3
"""Replace the exploit-dependent solutions for level ids 40 and 86 in
test_vectors.json with verified exploit-free solutions.

Background (see RULES.md §4.1 correction, 2026-08-30): the originally
recorded solutions for these two levels relied on the 0.3 s
resolution-cancellation bug. Both levels are winnable without it — a reset
trap hit by the placement that completes the board is free because the win
check runs before the reset, so the single necessary trap-tripping
placement is simply ordered last. The clean solutions below were found by
exhaustive search over all piece-to-cell assignments (exact for these
boards: walls and traps only, no repel switches, so spread is
order-independent) and are replayed here through the reference Game class
from verify_test_vectors.py, which regenerates the golden board_after
states with resolution running after every placement.

The original recordings are preserved under solution_original_exploit /
steps_original_exploit. Idempotent: re-running is a no-op.

    python3 docs/tools/regen_clean_solutions_40_86.py
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from verify_test_vectors import Game, verify  # noqa: E402

PATH = os.path.join(HERE, "..", "test_vectors.json")

# (piece_index, i, j) — the trap-tripping placement is last in each list.
CLEAN_SOLUTIONS = {
    "40": [(0, 2, 1), (1, 5, 7), (4, 0, 8), (3, 3, 3), (2, 1, 1)],
    "86": [(3, 1, 6), (4, 3, 8), (2, 5, 7), (0, 2, 2), (1, 4, 4)],
}

CORRECTION_NOTE = (
    "CORRECTION 2026-08-30: an earlier version of this file claimed levels "
    "40 and 86 were unwinnable without the 0.3 s resolution-cancel exploit "
    "(LevelMenuScene.cpp ccTouchBegan stopAllActions on node tag 999). That "
    "claim was wrong: delayThenCheckForWin runs the win check BEFORE the "
    "reset (Game.cpp), so a reset trap hit by the placement that completes "
    "the board is free, and ordering the single necessary trap-tripping "
    "placement last wins legitimately. Exhaustive search over all "
    "piece-to-cell assignments (exact for these boards: walls/traps only, "
    "no repel switches, so spread is order-independent) shows every "
    "covering assignment needs at most ONE trap-tripping piece, even with "
    "all 5 pieces forced onto the board. solution/steps for levels 40 and "
    "86 are now exploit-free; the original exploit-dependent recordings are "
    "preserved under solution_original_exploit / steps_original_exploit. "
    "requires_reset_cancel_exploit is false everywhere; "
    "pending_check_cancelled appears only inside *_original_exploit records."
)


def main():
    data = json.load(open(PATH))
    levels = data["levels"]
    changed = False
    for lid, sol in CLEAN_SOLUTIONS.items():
        lv = levels[lid]
        if "solution_original_exploit" in lv:
            print(f"level {lid}: already regenerated, skipping")
            continue
        g = Game(lv["board"], lv["pieces"])
        steps = []
        won = False
        for pi, i, j in sol:
            assert g.can_place(pi, i, j), (lid, pi, i, j)
            won = g.set_piece(pi, i, j)  # resolution always runs
            steps.append({"piece_index": pi, "tile": lv["pieces"][pi],
                          "i": i, "j": j, "board_after": list(g.board)})
        assert won, f"level {lid}: clean solution did not win"
        lv["solution_original_exploit"] = lv["solution"]
        lv["steps_original_exploit"] = lv["steps"]
        lv["solution"] = [{"piece_index": pi, "tile": lv["pieces"][pi],
                           "i": i, "j": j} for pi, i, j in sol]
        lv["steps"] = steps
        lv["requires_reset_cancel_exploit"] = False
        changed = True
        print(f"level {lid}: clean solution written "
              f"(win on final, trap-tripping placement)")
    if changed:
        data["_meta"]["reset_cancel_exploit"] = CORRECTION_NOTE
        with open(PATH, "w") as f:
            json.dump(data, f, indent=1)
            f.write("\n")
    total, failures = verify(PATH)
    if failures:
        for lid, msg in failures:
            print(f"level {lid}: FAIL — {msg}")
        sys.exit(1)
    print(f"re-verified: all {total} levels pass")


if __name__ == "__main__":
    main()
