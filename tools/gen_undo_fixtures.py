#!/usr/bin/env python3
"""Generate undo/edge-case fixtures for the C# rules port.

docs/test_vectors.json exercises only the placement path; the undo path
(Game::clearPiece, RULES.md §7) has no shipped vectors. This script extends
the trusted Python reference (docs/tools/verify_test_vectors.py) with a
literal port of clearPiece from Game.cpp, runs hand-designed scenarios, and
emits the expected outcomes as C# data:

    unity/Assets/_Project/Tests/EditMode/UndoFixtures.g.cs

The C# tests replay the same scripts through the real action pipeline and
must match. Two independent ports (Python here, C# there) agreeing on
adversarial scenarios is the cross-check.

Script ops: "P<piece>@<i>,<j>" place+resolve, "C<piece>" clear (undo).
"""
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "docs", "tools"))
from verify_test_vectors import Game, W, H  # noqa: E402

OUTPUT = os.path.join(
    ROOT, "unity", "Assets", "_Project", "Tests", "EditMode", "UndoFixtures.g.cs")


class RefGame(Game):
    """The reference Game plus a literal clearPiece port and event counting."""

    def __init__(self, board, tiles):
        super().__init__(board, tiles)
        self.solved = False
        self.solved_events = 0
        self.unbound_events = 0

    def resolve_core(self):
        """Game::delayThenCheckForWin — win first, else reset, else repels."""
        win = self.check_win()
        self.solved = win
        if win:
            self.solved_events += 1
            return
        if self.reset_tripped:
            self.full_reset()
            self.unbound_events += 1
        else:
            for r in self.repels:
                self.propagate_repel(*r)

    def place_and_resolve(self, index, i, j):
        assert self.can_place(index, i, j), f"illegal placement {index}@{i},{j}"
        self.repels = []
        self.reset_tripped = False
        self.placed[index] = (i, j)
        self.propagate_piece(index)
        self.resolve_core()

    def clear_piece(self, index):
        """Literal Game::clearPiece: row/col retraction with 99 marking,
        re-propagation of remaining pieces in index order each followed by a
        synchronous resolution (repel queue and reset flag NOT cleared here),
        then 99 reversion."""
        if index not in self.placed:
            return
        pi, pj = self.placed[index]
        for i in range(H):
            for j in range(W):
                if i == pi or j == pj:
                    loc = i * W + j
                    if self.board[loc] == 1:
                        self.board[loc] = 99
                    elif self.board[loc] == 4:
                        self.board[loc] = 1
        del self.placed[index]
        for k in range(len(self.tiles)):
            if k in self.placed:
                self.propagate_piece(k)
                self.resolve_core()
        for loc in range(W * H):
            if self.board[loc] == 99:
                self.board[loc] = 1


def board(rows):
    """rows: 11 strings of 6 chars ('.'=void, digits=cell values)."""
    assert len(rows) == H and all(len(r) == W for r in rows)
    return [0 if c == "." else int(c) for r in rows for c in r]


def run(g, script):
    for op in script.split(";"):
        if op[0] == "P":
            piece, pos = op[1:].split("@")
            i, j = pos.split(",")
            g.place_and_resolve(int(piece), int(i), int(j))
        elif op[0] == "C":
            g.clear_piece(int(op[1:]))
        else:
            raise ValueError(op)


SCENARIOS = [
    # (name, rows, tiles, script)
    ("single_undo_restores_board",
     ["..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1."],
     ["UD"],
     "P0@5,2;C0"),

    ("undo_keeps_other_pieces_coverage",
     [".1...1",
      ".1...1",
      ".1...1",
      ".1...1",
      ".1...1",
      "111..1",
      ".1...1",
      ".1...1",
      "111111",
      ".1...1",
      ".1...1"],
     ["UD", "LR"],
     "P0@3,1;P1@8,3;C1"),

    ("undo_accumulates_repel_queue",
     ["..3.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1."],
     ["UD", "U"],
     "P0@5,2;P1@7,2;C1"),

    ("stale_trip_flag_full_resets_mid_undo",
     ["..1...",
      "..1...",
      "..1...",
      "..1...",
      "..1...",
      "..1...",
      "..1...",
      "..1.1.",
      "..1...",
      "..1...",
      "..5..."],
     ["D", "R"],
     "P1@7,2;P0@0,2;C1"),

    ("repel_rerun_walks_over_wall_after_undo",
     ["..1.1.",
      "..1.1.",
      "..1.1.",
      "..2.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..1.1.",
      "..3.1."],
     ["UD", "D"],
     "P0@1,2;P1@4,2;C1"),

    ("mid_undo_win_check_ignores_99_marks",
     ["..1...",
      "..1...",
      "..1...",
      "..1...",
      "..1...",
      "1.1...",
      "..1...",
      "..1...",
      "..1...",
      "..1...",
      "..1..."],
     ["UD", "R"],
     "P0@4,2;P1@5,2;C1"),
]

HEADER = """\
// <auto-generated>
// Generated by tools/gen_undo_fixtures.py against the Python reference
// implementation (docs/tools/verify_test_vectors.py + a literal clearPiece
// port). Do not edit; re-run the script instead.
// Board strings: 66 chars row-major, '.' = void, digit = cell value.
// Script ops: P<piece>@<i>,<j> = place + resolve; C<piece> = clear (undo).
// </auto-generated>

namespace GridInfect.Core.Tests
{
    public static class UndoFixtures
    {
        public struct Scenario
        {
            public string Name;
            public string Board;
            public string Pieces;
            public string Script;
            public string ExpectedBoard;
            public int ExpectedRepelQueue;
            public int ExpectedPlacedMask;
            public bool ExpectedSolved;
            public int ExpectedSolvedEvents;
            public int ExpectedUnboundEvents;
        }

        public static readonly Scenario[] All =
        {
"""

FOOTER = """\
        };
    }
}
"""


def cell_char(v):
    return "." if v == 0 else str(v)


def main():
    out = [HEADER]
    for name, rows, tiles, script in SCENARIOS:
        g = RefGame(board(rows), tiles)
        run(g, script)
        assert all(v != 99 for v in g.board), f"{name}: 99 leaked"
        expected = "".join(cell_char(v) for v in g.board)
        initial = "".join(cell_char(v) for v in board(rows))
        mask = sum(1 << k for k in g.placed)
        out.append("            new Scenario\n            {\n")
        out.append(f'                Name = "{name}",\n')
        out.append(f'                Board = "{initial}",\n')
        out.append(f'                Pieces = "{",".join(tiles)}",\n')
        out.append(f'                Script = "{script}",\n')
        out.append(f'                ExpectedBoard = "{expected}",\n')
        out.append(f"                ExpectedRepelQueue = {len(g.repels)},\n")
        out.append(f"                ExpectedPlacedMask = {mask},\n")
        out.append(f"                ExpectedSolved = {'true' if g.solved else 'false'},\n")
        out.append(f"                ExpectedSolvedEvents = {g.solved_events},\n")
        out.append(f"                ExpectedUnboundEvents = {g.unbound_events},\n")
        out.append("            },\n")
    out.append(FOOTER)
    with open(OUTPUT, "w") as f:
        f.write("".join(out))
    print(f"wrote {OUTPUT}: {len(SCENARIOS)} scenarios")
    for name, rows, tiles, script in SCENARIOS:
        g = RefGame(board(rows), tiles)
        run(g, script)
        print(f"  {name}: solved={g.solved} events={g.solved_events} "
              f"queue={len(g.repels)} placed={sorted(g.placed)}")


if __name__ == "__main__":
    main()
