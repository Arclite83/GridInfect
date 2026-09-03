# RulesV2 — the mechanics for new content

Stage 7 of `EXECUTION_PLAN.md`. `RULES.md` is the frozen classic
specification (Legacy, the 128 vectors). RulesV2 (`GridInfect.Core.RulesV2`,
selected by `LevelDef.Version == 2`, which every generated level carries)
keeps the classic placement path and replaces undo. Stages 8–12 extend this
file, one section per element.

## 1. Pieces

A piece is a `PieceSpec`: a set of arms out of eight directions (L, R, U,
D and the diagonals UL, UR, DL, DR), a reach per arm (0 = to the edge, else
a number of cells — stage 8), and an optional 3×3 area (stage 9). A classic
tile is the spec with unlimited cardinal arms and no area; `Tile` stays
the wire form for Legacy and for drawing the cardinal part.

Text form (`PieceSpec.Encode/Parse`): the unlimited cardinal arms as a tile
name, then `+` tokens: `L2` a short arm, `ul`/`ur`/`dl`/`dr` a diagonal
(with an optional reach), `A` the area. `LRD`, `LR+U1`, `L+ur2`, `L+A`.

## 2. Board

Cell values as `RULES.md` §1.1 plus `6` **forbidden** (stage 10). Relay
arms (stage 12) are a second byte per cell (`LevelDef.CellDataAt`): a
non-zero arm mask on an active cell.

## 3. Placement (`piece.place`, `RulesV2.SetPiece`)

Legality as `RULES.md` §3 (cell holds 1 or 4, piece in the tray, no piece
on the cell) plus: a placement whose spread would touch a forbidden cell is
illegal (§6). The spread is the classic one generalised:

1. The piece's cell is infected. If the piece has an area, every active
   cell in its 3×3 neighbourhood is infected; walls, switches and traps
   inside it are inert.
2. Arms walk outward in rings 1..10 with inner order U, D, L, R, UL, UR,
   DL, DR (`TileArms.SpreadOrderV2`; the classic four first, so the repel
   queue of a classic tile is built in the classic order). A wall stops
   the arm; a switch stops it and queues a repel back along the arm; a
   trap stops it and trips the reset; a void or the board edge is passed
   over; an active cell is infected. An arm with reach *n* stops after
   ring *n*.
3. A cell that turns from 1 to 4 and carries relay arms spreads those arms
   from itself (rings as above, unlimited reach), at most once per
   propagation.

The repel queue and the trip flag are fresh at every placement. The
placement leaves the resolution pending; `board.resolve` lands it.

## 4. Resolution (`board.resolve`)

The classic order (`RULES.md` §4.1): win first (no cell holds 1), else a
full reset if a trap tripped, else the repels in queue order. Then the
queue is emptied — it never accumulates. A repel walks from the switch away
from the arm that hit it, turning 4 into 1, and stops at the first placed
piece (walls and voids are walked over), as in V1.

## 5. Undo (`piece.clear`)

Not the classic row/column retraction. The board goes back to its initial
state, every piece still placed re-propagates in index order (§3, one
fresh queue for all of them), and one resolution runs (§4). A board change
event is raised for every non-void cell so the view resyncs. The result is
the union of the remaining spreads; the V1 repel-queue accumulation and
the mid-undo "win through 99 marks" quirks are not carried.

A locked piece (stage 5) is an ordinary placed piece here. A full reset
(trap or the reset button) sends unlocked pieces to the tray and leaves
locked ones placed and re-propagated, as in V1 with locks.

## 6. Forbidden cells (stage 10)

Value 6. Never infected. `CanPlace` simulates the spread (arms, area,
relays) on a scratch board and refuses a placement that would touch one;
the adapter bounces the piece to the tray. The solver treats them as
blockers for lines and excludes such placements (tier 2, ArmExclusion).

## 7. Equivalence with the classic rules

On a board with only classic tiles and no forbidden or relay cells, §3 and
§4 are the classic placement path step for step:
`RulesV2Tests.PlacementPathMatchesTheClassicRulesOnAll128Boards` replays
every vector solution through both and compares boards, queues and flags
after each placement. The 128 vectors themselves stay on V1
(`VectorReplayTests`); the generated worlds, the daily and endless boards
run on V2 (`WorldTests`, `DailyTests` go through the action pipeline).
