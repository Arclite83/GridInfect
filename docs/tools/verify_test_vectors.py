#!/usr/bin/env python3
"""Reference implementation of the Grid Infect core rules and verifier for
docs/test_vectors.json.

This file is a 1:1 port of the placement/spread/repel/reset/win semantics in
grid-infect-cocos2dx/Classes/Core/Game.cpp (see docs/RULES.md for the prose
spec). Run it to confirm that every level's recorded solution replays to a
win and that every per-step golden board matches:

    python3 verify_test_vectors.py [path/to/test_vectors.json]

A C# port is equivalent when it reproduces steps[n].board_after for every
level and wins on the final step. Steps flagged pending_check_cancelled=true
model the original's 0.3 s resolution-cancel quirk (RULES.md §4.1): the
placement's spread applies but the win check, repels, and reset are skipped.

Usage of this module from other tools: Game(board, tiles) with
set_piece(index, i, j) -> bool (won).
"""
import json
import os
import sys

# The board is transposed from the original's 11x6 to 6x11 (portrait, R-1103)
# by (i, j) -> (j, i), arms remapped L<->U and R<->D — see
# tools/transpose_board_to_portrait.py. The rules below are the same rules
# conjugated by that map, which is why the recorded solutions still replay.
W, H = 6, 11
DIRS = {"L": (0, -1), "R": (0, 1), "U": (-1, 0), "D": (1, 0)}
OPP = {"L": "R", "R": "L", "U": "D", "D": "U"}

# Game::propagatePiece visits L,R,U,D; under the transpose that order is
# U,D,L,R. It has to travel, because the repel queue is built in it.
SPREAD_ORDER = "UDLR"


def arms(tile):
    return [d for d in "LRUD" if d in tile]


class Game:
    """Exact port of Game.cpp state relevant to board resolution."""

    def __init__(self, board, tiles):
        self.board = list(board)      # int[66], row-major i*6+j
        self.tiles = list(tiles)      # tile name per piece index
        self.placed = {}              # piece index -> (i, j)
        self.repels = []              # queued (i, j, direction)
        self.reset_tripped = False

    def bp(self, i, j):
        """Game::getBoardPosition — -1 out of bounds."""
        if i < 0 or i >= H or j < 0 or j >= W:
            return -1
        return self.board[i * W + j]

    def change(self, i, j, value):
        """Game::changeBoard — writes only to in-bounds, non-void cells
        whose value differs."""
        if i < 0 or i >= H or j < 0 or j >= W:
            return False
        loc = i * W + j
        if self.board[loc] != 0 and self.board[loc] != value:
            self.board[loc] = value
            return True
        return False

    def can_place(self, index, i, j):
        """LevelMenu::ccTouchEnded placement legality."""
        if index in self.placed:
            return False
        if any(p == (i, j) for p in self.placed.values()):
            return False
        return self.bp(i, j) in (1, 4)

    def propagate_piece(self, index):
        """Game::propagatePiece — offset-major; the original's L,R,U,D under
        the transpose. Stops on 2/3/5; skips 99; passes voids, edges, and
        infected cells."""
        i0, j0 = self.placed[index]
        self.change(i0, j0, 4)
        stopped = {d: False for d in "LRUD"}
        tile_arms = arms(self.tiles[index])
        for offset in range(1, 11):
            for d in SPREAD_ORDER:
                if stopped[d] or d not in tile_arms:
                    continue
                di, dj = DIRS[d]
                i, j = i0 + di * offset, j0 + dj * offset
                v = self.bp(i, j)
                if v == 2:
                    stopped[d] = True
                elif v == 3:
                    stopped[d] = True
                    self.repels.append((i, j, OPP[d]))
                elif v == 5:
                    stopped[d] = True
                    self.reset_tripped = True
                elif v == 99:
                    pass
                else:
                    self.change(i, j, 4)

    def propagate_repel(self, ri, rj, direction):
        """Game::propagateRepel — from the switch cell outward; the whole
        repel stops at the first placed piece; converts 4 -> 1; nothing
        else stops it."""
        di, dj = DIRS[direction]
        for offset in range(1, 11):
            i, j = ri + di * offset, rj + dj * offset
            if any(p == (i, j) for p in self.placed.values()):
                return
            if self.bp(i, j) == 4:
                self.change(i, j, 1)

    def check_win(self):
        """Game::checkForWin — no cell may hold value 1."""
        return all(v != 1 for v in self.board)

    def full_reset(self):
        """Game::fullReset — every 4 -> 1, all pieces to the tray."""
        for loc, v in enumerate(self.board):
            if v == 4:
                self.board[loc] = 1
        self.placed.clear()

    def set_piece(self, index, i, j, cancel_pending_check=False):
        """Game::setPiece followed by the renderer's 0.3 s-deferred
        Game::delayThenCheckForWin. cancel_pending_check=True models the
        player cancelling the deferred resolution (RULES.md §4.1)."""
        self.repels = []
        self.reset_tripped = False
        self.placed[index] = (i, j)
        self.propagate_piece(index)
        if cancel_pending_check:
            return False
        # delayThenCheckForWin: win check FIRST, then reset, then repels.
        if self.check_win():
            return True
        if self.reset_tripped:
            self.full_reset()
        else:
            for r in self.repels:
                self.propagate_repel(*r)
        return False


def verify(path):
    data = json.load(open(path))
    levels = data["levels"]
    failures = []
    for lid in sorted(levels, key=int):
        v = levels[lid]
        g = Game(v["board"], v["pieces"])
        won = False
        try:
            for n, s in enumerate(v["steps"]):
                assert g.can_place(s["piece_index"], s["i"], s["j"]), \
                    f"step {n}: illegal placement"
                won = g.set_piece(s["piece_index"], s["i"], s["j"],
                                  s.get("pending_check_cancelled", False))
                assert g.board == s["board_after"], \
                    f"step {n}: board mismatch"
            assert won, "final step did not win"
        except AssertionError as e:
            failures.append((lid, str(e)))
    return len(levels), failures


if __name__ == "__main__":
    default = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                           "..", "test_vectors.json")
    path = sys.argv[1] if len(sys.argv) > 1 else default
    total, failures = verify(path)
    if failures:
        for lid, msg in failures:
            print(f"level {lid}: FAIL — {msg}")
        print(f"{len(failures)}/{total} levels FAILED")
        sys.exit(1)
    print(f"all {total} levels verified: solutions replay to a win and "
          f"every per-step board matches")
