# Grid Infect — Mechanical Specification

Complete rules of the original game (cocos2d-x, 2014), extracted from
`grid-infect-cocos2dx/Classes/`. Every rule cites the file and function it
came from. This document is written so the game can be reimplemented without
reading the C++.

Companion files:

- `GENERATOR.md` — free-play level generation.
- `MODES.md` — progression, timed mode, scoring, services.
- `test_vectors.json` — all 128 shipped levels with verified solutions and
  per-step golden board states.

---

## 1. Board

- Every level is a fixed **11 rows × 6 columns** grid (transposed from the original's 6×11 for portrait — see ARCHITECTURE §2)
  (`Classes/Core/Level.h`: `Height = 6`, `Width = 11`). There is no
  per-level size variation; level shapes are made by marking cells void.
- Storage is a flat row-major `int Board[66]`; index `loc = i * 11 + j`
  where `i` is the row (0 = top) and `j` is the column (0 = left)
  (`Level.h`, and renderer layout in
  `Classes/Renderers/LevelMenuScene.cpp` `LevelMenu::init`: row 0 is drawn
  highest, column 5 is screen-center).
- The 128 classic levels are hard-coded C arrays in
  `Classes/Core/Level.cpp` `Level::initByLevel` (cases 0–127). Free-play
  boards are generated (`GENERATOR.md`).

### 1.1 Cell values

| Value | Meaning | Texture (`LevelMenu::bindLevel`) |
|---|---|---|
| 0 | Void — not part of the board. Rendered invisible (sprite opacity 0). Infection **passes over** void cells (see §4). | none |
| 1 | Active, uninfected cell | `tile_blue.png` |
| 2 | Wall — stops infection spread | `tile_yellow.png` |
| 3 | Repel switch — stops spread and fires a repel back along the line (§5) | `tile_purple.png` |
| 4 | Infected cell | `tile_red.png` |
| 5 | Reset trap — stops spread and trips a full board reset (§6) | `tile_black.png` |
| 99 | Transient marker used only inside the undo routine (§7); never visible between moves | n/a |

Shipped levels contain only values 0, 1, 2, 3, 5 (verified by extraction:
no level starts with pre-infected cells). Values 2, 3, 5 never change
during play; 1 ↔ 4 (and transiently 99) are the only mutable states.

## 2. Pieces

- Each level ships an **ordered list of pieces** (2–6 in shipped levels;
  the renderer allocates 8 tray slots — `LevelMenuScene.cpp` init loop
  `for (int i = 0; i < 8; i++)`). Level 32 (id 31) is the only level with
  6 pieces.
- A piece is one of 15 tile types — every non-empty combination of the
  four cardinal arms **L**eft, **R**ight, **U**p, **D**own
  (`Classes/Core/Enums.h` `enum Tile`):
  `L, R, U, D, LR, LU, LD, RU, RD, UD, LRU, LRD, LUD, RUD, LRUD`.
- A piece has state `placed` (bool) and board position `i, j`
  (−1,−1 when in the tray) (`Classes/Core/Piece.h`).
- Pieces start in a tray at the bottom of the screen and are dragged onto
  the board. Shipped levels may repeat tile types (nine do, e.g. level 29
  / id 28 ships three LRUDs); only the *generator* enforces unique tile
  types per level (`GENERATOR.md` §3), so duplicates mark hand-authored
  or hand-edited levels.

## 3. Placement rules

From `LevelMenu::ccTouchEnded` (`Classes/Renderers/LevelMenuScene.cpp`):

- A piece may be dropped on a cell iff:
  1. the cell's current value is **1 or 4** (uninfected or infected —
     placing onto already-infected cells is legal), and
  2. no other placed piece occupies that cell.
- Dropping anywhere else (void, wall, switch, trap, occupied cell,
  off-board) returns the piece to its tray slot; if it had been placed, it
  is now cleared from the board (undo, §7).
- **Touching a piece immediately unplaces it**: `LevelMenu::ccTouchBegan`
  calls `Game::clearPiece` on the touched piece the moment the touch
  starts, before any drag. Re-placing the same piece is therefore
  clear-then-place, and `Game::setPiece` additionally calls
  `clearPiece(index)` first as a safety.
- There is no rotation; a piece's arms are fixed.
- There is **no move limit and no placement budget** beyond the piece
  list. Not every piece must be placed to win (§8).

## 4. Infection spread

Source: `Game::setPiece` and the two `Game::propagatePiece` overloads
(`Classes/Core/Game.cpp`).

When a piece is placed at `(i, j)`:

1. The repel queue is cleared and the reset-tripped flag is cleared
   (`setPiece`).
2. The piece's own cell is set to 4 (infected) via `changeBoard`
   (no-op if the cell is void — cannot happen given placement rules).
3. Spread runs **synchronously and instantaneously** (no per-cell
   animation; see `ASSETS.md` §Timing). For `offset = 1` to `10`
   (outer loop), for each direction in the fixed order **L, R, U, D**
   (inner order in `propagatePiece(piece, fireEvents, offset)`), if the
   piece's tile has that arm and that direction has not been stopped:
   - Compute the target cell: `offset` cells from the piece in that
     direction.
   - Look up its value (`getBoardPosition`; out-of-bounds returns −1):
     - **2 (wall):** stop this direction permanently (for this
       propagation).
     - **3 (repel switch):** stop this direction; append
       `Repel(switchCell, oppositeDirection)` to the repel queue. The
       switch cell itself is never infected.
     - **5 (reset trap):** stop this direction; set the reset-tripped
       flag. The trap cell is never infected.
     - **99 (undo marker):** skip — do not change the cell, do **not**
       stop the direction.
     - **anything else** (1, 4, 0, out of bounds): call
       `changeBoard(cell, 4)`. This infects value-1 cells; it is a no-op
       for void (0), for already-infected (4), and out of bounds — and in
       all of these cases **propagation continues**.
4. Because the board is 6 wide and 11 tall and the range is 10, an
   unobstructed arm always reaches the board edge.

Consequences that must be preserved:

- **Infection jumps gaps.** Void cells and board edges do not stop
  spread; only walls (2), switches (3), and traps (5) do. A piece can
  infect disconnected islands of cells along its row/column. Level 3
  (id 2) depends on this, and generated levels routinely contain gaps
  (`LevelBuilder.cpp` comment at `buildBoard`: "FOUND IT: RUNS OVER
  LINES, AND OUT OF ARRAY" — the carving mirror of the same behavior).
- Placed pieces do **not** block other pieces' spread (their cells are
  simply infected cells; nothing in `propagatePiece` checks pieces).
- The four arms advance in lock-step rings (offset-major). Because
  spread only ever writes 4s and the stopping cells (2/3/5) are static,
  the L/R/U/D interleaving is unobservable in the final board state; it
  only fixes (a) the order board-change events fire, and (b) the order
  repels enter the queue: by offset first, then L, R, U, D.

### 4.1 Deferred resolution — the 0.3 s delay

The renderer, not the core, sequences the *consequences* of a placement
(`LevelMenu::ccTouchEnded` → `delayFinished` →
`Game::delayThenCheckForWin`):

- On a successful drop, the spread happens immediately, then a
  `CCDelayTime(0.3f)` followed by `delayFinished` is scheduled **on the
  invisible node with tag 999** (`event_placeholder.png` sprite).
- 0.3 s later `Game::delayThenCheckForWin` runs, in this order:
  1. **Win check first** (§8). If the board is fully infected, the level
     is solved and *neither repels nor the reset run* — a winning
     placement ignores any switch or trap it hit. This ordering is
     confirmed intentional by the in-game tutorial on level 26
     (`message_26.png`: "If the board is infected before the repel
     fires, you still win!").
  2. Otherwise, if the reset-tripped flag is set: `Game::fullReset` (§6).
  3. Otherwise: run every queued repel, in queue order (§5).
- **Cancellation quirk (confirmed bug — not ported):**
  `LevelMenu::ccTouchBegan` calls
  `this->getChildByTag(999)->stopAllActions()` whenever the player
  touches any tray/board piece. Touching a piece within 0.3 s of a drop
  therefore **cancels the pending resolution entirely**: no win check, no
  reset, no repels for that placement. The queued repels and the
  reset-tripped flag are discarded at the next `setPiece`.

  **Correction (2026-08-30):** this document previously claimed levels 41
  and 87 (ids 40 and 86) were unwinnable without the cancellation. That
  claim was wrong. Because the win check runs *before* the reset (step 1
  above), a reset trap hit by the placement that completes the board is
  free — and an exhaustive search over all piece-to-cell assignments
  (exact for these two levels: they contain only walls and traps, no
  repel switches, so spread is order-independent) shows every covering
  assignment needs at most **one** trap-tripping piece, even when all
  five pieces are forced onto the board. Ordering that placement last
  wins legitimately. Verified clean solutions (piece @ (row, col),
  trap-tripper last):
  - Level 41 (id 40): `R@(2,1)`, `LRU@(5,7)`, `LD@(0,8)`, `LRD@(3,3)`,
    `D@(1,1)` — D trips the trap and completes the board.
  - Level 87 (id 86): `LD@(1,6)`, `LRUD@(3,8)`, `RU@(5,7)`, `RD@(2,2)`,
    `L@(4,4)` — L trips the trap and completes the board.

  The *recorded original solutions* in `test_vectors.json` for these two
  levels do use the cancellation (`requires_reset_cancel_exploit`,
  `pending_check_cancelled`) — read that flag as "this stored solution
  relies on the exploit", not "the level requires it". The author has
  confirmed the cancellation is a bug; the Unity rebuild does not
  reproduce it (`REQUIREMENTS.md` R-107), and no level needs it.
  `docs/tools/regen_clean_solutions_40_86.py` regenerates the two
  levels' vector entries exploit-free.

## 5. Repel switches (value 3)

Source: `Game::propagateRepel` (`Classes/Core/Game.cpp`).

- When spread traveling in direction `d` hits a switch, a repel is queued
  with origin = **the switch's cell** and direction = **opposite of
  `d`** — i.e. pointing back along the line toward the piece whose spread
  hit it.
- At resolution time (0.3 s later, only if the placement did not win and
  did not trip a reset), each queued repel walks `offset = 1..10` from
  the switch in its direction:
  - If the current cell is occupied by **any placed piece**, the entire
    repel stops immediately (`return`) — the piece's own cell stays
    infected, and nothing beyond it is touched.
  - Otherwise, if the cell's value is 4, it is set back to 1
    (un-infected). Any other value (0, 1, 2, 5, edge) is walked over
    **without stopping** — a repel is stopped only by a placed piece.
- Net effect in normal play: the infection line between the switch and
  the triggering piece is retracted (exclusive of the piece's own cell).
  Because the walk retracts *any* infected cell on its path, it can also
  un-infect cells that other pieces infected on that line.
- Repels run sequentially in queue order. There is no re-check of the
  win condition after repels run; the next placement's resolution
  re-evaluates everything.
- The repel queue is cleared only at the next `setPiece`
  (`Game.cpp` `_repelsToRun.clear()`); running the queue does not empty
  it. This matters only for the undo re-queue quirk (§7).

## 6. Reset traps (value 5)

Source: `Game::fullReset` (`Classes/Core/Game.cpp`).

- If any arm of a placement's spread reaches a trap (unblocked, within
  10 cells), the reset-tripped flag is set.
- At resolution (0.3 s later), **if the placement did not win**, the
  whole board resets:
  - every infected cell (4) reverts to 1;
  - every piece is unplaced and returned to the tray
    (`EventHandler::onUnbindPieces`).
  - Walls, switches, traps, voids are untouched. Since shipped levels
    start with no infection, this restores the exact initial state.
- If the placement **did** complete the infection, the win check runs
  first and the reset never happens — tripping a trap on the winning
  move is free (§4.1 step 1).
- The tripped flag is not cleared by `fullReset` itself; it is cleared at
  the next `setPiece`, which is equivalent in observable behavior.

## 7. Undo (clearing a piece)

Undo is implicit: touch a placed piece (it is cleared instantly,
§3) and either drag it elsewhere or drop it off-board. There is no move
counter, so undo is unlimited and free. Source: `Game::clearPiece` and
`Game::resetBoard` (`Classes/Core/Game.cpp`).

Clearing placed piece P at `(pi, pj)` does, in order:

1. **`resetBoard(pi, pj)`** — for every cell on P's row **or** column:
   - value 4 → 1 (retract infection on P's lines);
   - value 1 → **99** (mark "was uninfected", so step 2 cannot infect
     it).
   Cells off P's row/column are untouched (safe: a piece only ever
   infects its own row/column).
2. P is marked unplaced. Then **for each remaining placed piece, in
   piece-index order**: re-run `propagatePiece(piece, fireEvents=false)`
   and then *synchronously* call `delayThenCheckForWin()` (no 0.3 s delay
   on this path). Notes:
   - Re-propagation infects value-1 cells on the remaining pieces' lines
     (re-infecting the cleared row/column where covered) but skips 99
     cells, so cells that were uninfected before the clear stay
     uninfected.
   - Re-propagation hitting a switch **pushes new repels into the queue
     that is never cleared between these calls**, and each synchronous
     `delayThenCheckForWin` re-runs the whole accumulated queue. Repel
     re-runs are near-idempotent (4→1 walks), but the queue grows with
     every undo until the next `setPiece`.
   - If a re-propagation trips a trap the synchronous resolution performs
     a full reset mid-undo. (Cannot normally occur: the same spread would
     have tripped at original placement time — but it is the literal
     control flow.)
   - The win check inside these synchronous calls can fire
     `onLevelSolved` **during an undo** if the removal plus
     re-propagation leaves the board fully infected.
3. Finally every 99 cell reverts to 1, and a board-change event is
   re-fired for every non-void cell so the renderer resyncs.

Net effect in the common case: after an undo, the board equals the union
of the remaining pieces' spreads — with the caveat about repel re-runs
above.

## 8. Win condition

`Game::checkForWin` (`Classes/Core/Game.cpp`): the level is solved when
**no cell on the board has value 1**. Voids, walls, switches, and traps
never count against the win; infected cells (4) satisfy it.

- The check runs at each placement's deferred resolution (§4.1) and
  synchronously during undo re-propagation (§7).
- All pieces do **not** need to be placed; only full infection matters.
- On win, `EventHandler::onLevelSolved` fires: Classic mode unlocks the
  next level and shows the COMPLETE popup; Free Play advances to the next
  generated level or stops the timer after the fifth (see `MODES.md`).
- The solved flag (`Level::isSolved`) is stored in a **file-scope global**
  in `Level.cpp` (`bool _solved;` at line 12) — it is shared by every
  `Level` instance, not per-level. Constructing any new `Level` resets
  it. Observable consequence: in Free Play the flag always refers to the
  current level; in Classic the replay button
  (`LevelMenu::replayCallback`) checks it to decide between "reload
  level" (solved) and "full reset" (not solved).

## 9. Loss condition and limits

There is none. No timer failure (Free Play's clock only records the
duration), no move limit, no piece budget beyond the fixed list,
unlimited undo, and a manual reset via the replay button
(`LevelMenu::replayCallback` → `Game::fullReset` while unsolved).
The only setback mechanic is the reset trap (§6).

## 10. Board-change events (for renderer parity)

`Game::changeBoard` fires `EventHandler::onChangeBoardIndex(i, j, value)`
for each individual cell change when `fireEvents` is true (placement
path). The undo path re-propagates with `fireEvents=false` and instead
re-fires an event per non-void cell at the end (with 99s already
converted back to 1). The renderer's response is a plain texture swap per
cell — there is no animation on infection (see `ASSETS.md` §Timing).

---

## UNKNOWN

- ~~**Whether the 0.3 s cancellation (§4.1) was intentional design**~~
  Resolved 2026-08-30: the author confirmed it is a bug, and the §4.1
  correction shows no level depends on it. It is not ported.
- **Multi-touch behavior.** `LevelMenu::registerWithTouchDispatcher`
  registers a targeted delegate with `swallowsTouches=true`, priority 0.
  Behavior when a second finger touches during a drag depends on
  cocos2d-x 2.2.3 dispatcher internals (engine not vendored in this
  repo). Resolve by testing a build against
  `cocos2d-x-2.2.3/cocos2dx/touch_dispatcher/CCTouchDispatcher.cpp`.
