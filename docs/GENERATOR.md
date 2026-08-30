# Grid Infect — Level Generation Algorithm

Source: `grid-infect-cocos2dx/Classes/LevelBuilder.cpp` /
`LevelBuilder.h`. Used by Free Play (`FreePlayMenuScene.cpp`
`freePlay1Callback` … `freePlay5Callback`), which generates **5 levels per
run** with `LevelBuilder::generateLevel(difficulty)`.

The generator works **backward from a chosen solution**: it first samples
a set of piece placements (the solution), then carves the board outward
from each placed piece, then discards the placements. The shipped level
therefore consists only of the board and the piece list; the solution is
never stored (`generateLevel`, the loop that resets every piece to
`i = j = -1` before returning).

---

## 1. RNG source and determinism

`LevelBuilder::generateLevel(Difficulty)`:

- RNG is the **C standard library `rand()`**, seeded **once per process**
  via `srand(seed)` on the first call, where
  `seed = now.tv_sec * 1000 + now.tv_usec / 1000` (wall-clock
  milliseconds from `cocos2d::CCTime::gettimeofdayCocos2d`). A
  `* getpid()` factor is commented out. The guard is the static
  `LevelBuilder::_seedset`.
- **Generation is NOT deterministic given a seed across platforms.** The
  `rand()` sequence is implementation-defined; glibc, Apple libc, and
  MSVC all differ. On one fixed platform/compiler the sequence for a
  fixed seed is reproducible, but the original never records or exposes
  the seed, and the seed is wall-clock time.
- Consequently `test_vectors.json` contains **no seed-to-board pairs** —
  none can be produced that any independent reimplementation could match.
- **What would make it deterministic** (for a port; do not confuse this
  with original behavior): replace `rand()` with a fully specified PRNG
  (e.g. a documented LCG, xorshift, or PCG with fixed constants), pass
  the seed explicitly, and preserve the exact draw order specified in §3
  and §4 below (every rejected sample still consumes draws). The draw
  order in this document is complete enough to make that port
  bit-reproducible against itself.

## 2. Difficulty configurations

`generateLevel(Difficulty)` dispatches to
`generateLevel(difficulty, piecesToSet, initial_xOffset, initial_xCount,
initial_yOffset, initial_yCount)` with these constants:

| Difficulty | pieces | xOffset | xCount | yOffset | yCount | Tile pool restriction |
|---|---|---|---|---|---|---|
| Beginner | 2 | 3 | 5 | 1 | 5 | no 3- or 4-arm tiles (`LRU, LRD, LUD, RUD, LRUD` rejected) |
| Easy | 3 | 3 | 6 | 1 | 5 | none |
| Medium | 4 | 2 | 7 | 0 | 6 | none |
| Hard | 4 | 0 | 11 | 0 | 6 | none |
| Challenging | 5 | 0 | 11 | 0 | 6 | `LR` and `UD` rejected |

`(xOffset, xCount, yOffset, yCount)` define the sampling window for piece
positions: `x ∈ [xOffset, xOffset + xCount)`, `y ∈ [yOffset,
yOffset + yCount)` — before the per-tile shrink in §3. `x` is the column
(j), `y` is the row (i).

## 3. Solution selection (piece sampling)

For each of `piecesToSet` pieces, rejection-sample until accepted
(`while (overlap)` — **unbounded**; there is no retry cap and no
backtracking across pieces — a rejected sample re-rolls only the current
piece):

Each iteration of the rejection loop, in order:

1. Draw the tile: `tile = (Tile)(rand() % 15)`. **(1 draw)**
2. Copy the window `(xOffset, xCount, yOffset, yCount)` from the
   difficulty constants.
3. Mark the sample rejected (`overlap = true`) if:
   - difficulty is Beginner and the tile has 3 or 4 arms; or
   - difficulty is Challenging and the tile is `LR` or `UD`; or
   - **any earlier accepted piece already has this tile type** (tile
     types are unique within a generated level).
4. Shrink the window by the tile's arms, giving 2 cells of margin inside
   the window for each armed direction:
   - `L`: `xOffset += 2; xCount -= 2`
   - `R`: `xCount -= 2`
   - `U`: `yOffset += 2; yCount -= 2`
   - `D`: `yCount -= 2`
   - combinations apply the union of their arms, **except**:
     `LR` is `xOffset += 2; xCount -= 4` (correct two-sided margin), but
     **`UD` is only `yOffset += 2; yCount -= 2`** — the down-side margin
     is missing. This is a bug in the original (a `UD` piece can be
     sampled flush against the bottom of its window, e.g. row 5 for
     Easy, leaving its D arm with nothing to carve). Ports must
     reproduce it to match behavior. The multi-arm cases in the `switch`
     are each written out explicitly in `generateLevel`; their exact
     values are:

     | tile | xOffset+ | xCount− | yOffset+ | yCount− |
     |---|---|---|---|---|
     | L | 2 | 2 | 0 | 0 |
     | R | 0 | 2 | 0 | 0 |
     | U | 0 | 0 | 2 | 2 |
     | D | 0 | 0 | 0 | 2 |
     | LR | 2 | 4 | 0 | 0 |
     | LU | 2 | 2 | 2 | 2 |
     | LD | 2 | 2 | 0 | 2 |
     | RU | 0 | 2 | 2 | 2 |
     | RD | 0 | 2 | 0 | 2 |
     | UD | 0 | 0 | 2 | 2 |
     | LRU | 2 | 4 | 2 | 2 |
     | LRD | 2 | 4 | 0 | 2 |
     | LUD | 2 | 2 | 2 | 4 |
     | RUD | 0 | 2 | 2 | 4 |
     | LRUD | 2 | 4 | 2 | 4 |

5. Draw the position: `x = xOffset + rand() % xCount;`
   `y = yOffset + rand() % yCount`. **(2 draws — consumed even when the
   sample was already marked rejected in step 3.)**
6. Reject if any earlier accepted piece is at the same cell, or shares
   **either the same row or the same column** (`piece->i == y ||
   piece->j == x`). The source comments this as preventing
   "'less than optimal' solutions".
7. If rejected for any reason, loop back to step 1 (tile and position
   are both re-rolled). Otherwise accept.

The accepted piece is constructed with `i = y, j = x`, appended to the
level's piece list, and immediately used to carve the board (§4). After
all pieces are placed and carved, every piece's position is reset to
(−1, −1) — the solution is discarded.

Notes:

- With the shipped configurations, `rand() % xCount` / `% yCount` never
  sees a non-positive count (worst cases: Beginner `LR` → xCount 1;
  Easy `LUD` → yCount 1). A port changing the window constants must
  re-check this — count ≤ 0 is undefined behavior in the original.
- The uniqueness rule (step 3) plus the row/column exclusivity rule
  (step 6) are the entire "constraint check" phase. There is **no
  solvability check, no connectivity check, and no uniqueness-of-solution
  check** — none is needed for solvability (§5).

## 4. Reverse construction (board carving)

`LevelBuilder::buildBoard(level, piece)` runs immediately after each
piece is accepted, on the board carved so far (board starts all-void —
`Level::Level()` default constructor):

1. Set the piece's own cell to 1.
2. Per-direction stop flags (`lStopped` etc.) are declared and checked
   **but never set** — dead code; carving never stops early.
3. For `offset = 1` to `10` (outer), for each armed direction in the
   fixed order **L, R, U, D** (inner):
   - Compute the target cell. If it is **out of bounds, skip without
     consuming a draw** (the bounds check precedes the `rand()` call).
   - Otherwise draw once and activate probabilistically:
     `if ((rand() % 20) - offset > 4) Board[cell] = 1;` **(1 draw per
     in-bounds visited cell)**.
     - Activation probability at distance `d`: `P(rand%20 > 4 + d)` =
       `(15 − d) / 20`: 70% at d=1, 65% at d=2, … 30% at d=9, 25% at
       d=10.
     - A cell that fails the roll stays void, but **the walk continues** —
       farther cells can still activate, producing gaps and disconnected
       islands on the piece's lines. This is fine because infection
       spread also jumps gaps (`RULES.md` §4). The comment at
       `buildBoard` ("FOUND IT: RUNS OVER LINES, AND OUT OF ARRAY")
       records the author discovering the over-run; the bounds check
       neutralizes the out-of-array part, the "runs over lines" part is
       kept behavior.
     - Cells already carved to 1 by earlier pieces just get re-assigned 1
       (harmless), and the draw is still consumed.
   - The `try/catch` around the roll is inert (nothing throws).
4. Generated boards contain **only values 0 and 1**. Walls (2), repel
   switches (3), and reset traps (5) are never generated — every special
   tile in the shipped 128 levels is hand-authored or hand-edited (see
   §6).

## 5. Why every generated level is solvable

By construction: every active cell lies on the row or column of the
piece that carved it, within 10 cells, in an armed direction. Generated
boards contain no walls/switches/traps, so each piece placed back at its
sampled position infects its full armed lines to the board edge
(range 10 ≥ board dimension − 1), covering exactly the cells it carved.
Pieces occupy distinct rows *and* distinct columns, so placements never
collide. Placing every piece at its original cell (any order) is
therefore always a winning solution. No verification pass exists or is
needed.

Solutions are not necessarily unique — play may place pieces on any
active cell, not just the sampled ones — and fewer than all pieces can
suffice on some boards. Note that each piece's own sampled cell is off
every other piece's row and column (§3 step 6), so the constructed
solution itself has no redundant pieces: only alternative placements can
shrink it.

## 6. What generation is used for, and the dev-dump helper

- **Free Play**: `FreePlayMenu::freePlayNCallback` generates 5 levels of
  the chosen difficulty at button press (before the scene transition).
  This is the only runtime use.
- **Classic levels 100–128 (ids 99–127)** were bootstrapped from the
  generator: `LevelBuilder::a()` is a leftover dev helper that generates
  levels at the Challenging configuration
  (`generateLevel(Challenging, 5, 0, 11, 0, 6)`) for ids 99–127 and
  returns them **as C++ `case` source text** ready to paste into
  `Level::initByLevel`. The shipped cases for those ids contain walls,
  switches, and reset traps, which the generator cannot produce — so the
  dumped levels were hand-edited before shipping. `a()` is not called
  anywhere at runtime.

## 7. Generation cost at higher configurations

All cost is in the §3 rejection loop; carving (§4) is linear and cheap.
Rejection pressure comes from three multiplicative sources, all worst at
Challenging (5 pieces):

1. **Tile-type uniqueness + pool restriction**: the 5th piece must roll
   one of the 9 unused tiles out of 15 raw rolls, with `LR`/`UD` also
   auto-rejected (13 usable): P ≈ 9/15.
2. **Row exclusivity**: pieces must occupy 5 distinct rows out of only 6.
   The 5th piece's row roll survives with P ≈ 2/6 (before window
   shrink — arms with U/D margins narrow the row window further, to as
   little as `yCount = 2` for `LUD`/`RUD`/`LRUD`, and the surviving rows
   may all be already taken, forcing rejection until a different tile is
   rolled).
3. **Column exclusivity**: P ≈ 7/11 for the 5th piece (or narrower after
   shrink).

Every rejection re-rolls tile *and* position (3 draws), so expected
iterations per piece grow roughly geometrically with piece index; the
loop is unbounded and, structurally, could spin arbitrarily long
(in practice tens of iterations). Because the 6-row board bounds distinct
rows at 6, raising `piecesToSet` above 6 would make the loop
**permanently infinite**. This — not carving — is what makes higher
configurations slow.

---

## UNKNOWN

- **Which libc `rand()` the shipped binaries used** (Apple libc for the
  iOS build, Android's Bionic for the APK). Determines the actual
  sequences players saw; unresolvable from this repo. Resolve by checking
  the shipped binaries' link targets — practically irrelevant for a port
  because the seed was wall-clock anyway.
- **Whether ids 99–127 received manual edits beyond adding special
  tiles** (piece list tweaks, cell additions). Only a diff against the
  original `a()` outputs could tell, and those outputs were never saved.
