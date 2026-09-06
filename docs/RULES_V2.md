# RulesV2 — the mechanics for new content

Stage 7 of `EXECUTION_PLAN.md`. `RULES.md` is the frozen classic
specification (Legacy, the 128 vectors). RulesV2 (`GridInfect.Core.RulesV2`,
selected by `LevelDef.Version == 2`, which every generated level carries)
keeps the classic placement path and replaces undo. Stages 8–12 extend this
file, one section per element.

## 1. Pieces

A piece is a `PieceSpec`: a set of arms of **one family** — cardinal (L,
R, U, D: the fifteen classic tiles) or diagonal (UL, UR, DL, DR), never
both — and/or the 3×3 area (stage 9). Every arm reaches to the edge;
there is no per-arm reach. The blot is the one short-range piece: it is
not directional, it takes the eight neighbours. The constructor enforces
the one-family rule, so no level, save or generator draw can hold a mixed
piece. A classic tile is the spec with cardinal arms and no area; `Tile`
stays the wire form for Legacy and for drawing the cardinal part.

Text form (`PieceSpec.Encode/Parse`): the cardinal arms as a tile name
(`LRD`), or `+`-joined diagonal tokens `ul`/`ur`/`dl`/`dr` (`ul+dr`), then
`+A` for the area (`A`, `L+A`). A digit on a token or a mix of families is
a parse error.

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
   over; an active cell is infected.
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
the adapter bounces the piece to the tray (the same tray-return as any
illegal drop). The solver treats them as blockers for lines and excludes
such placements (tier 2, ArmExclusion). Drawn amber with a ring glyph
(`BoardPalette.Forbidden`, `_ColForbidden`).

## 7. Equivalence with the classic rules

On a board with only classic tiles and no forbidden or relay cells, §3 and
§4 are the classic placement path step for step:
`RulesV2Tests.PlacementPathMatchesTheClassicRulesOnAll128Boards` replays
every vector solution through both and compares boards, queues and flags
after each placement. The 128 vectors themselves stay on V1
(`VectorReplayTests`); the generated worlds, the daily and endless boards
run on V2 (`WorldTests`, `DailyTests` go through the action pipeline).

## 8. Short arms (retired)

Stage 8 shipped a per-arm reach (`L2`, `U1`: an arm that stopped after
one or two rings). It is cut: a short arm was a second short-range
symbology beside the blot for a rule the player read off the piece, not
the board. The blot (§9) is the only short-range piece. Every arm reaches
the edge; the reach field, its text form, the stop-bar glyph and the
`ShortArms` element are gone, and world `w13` with them (its id is
retired, not reused, so saved world progress keeps its keys).

## 9. The area piece (stage 9)

A `PieceSpec` with `Area` and no arms (text `A`, the "blot") infects its
cell and every active cell of its 3×3 neighbourhood. Walls, switches and
traps inside the neighbourhood are inert: nothing stops, nothing is
queued, nothing trips. A forbidden cell inside it makes the placement
illegal (§6). A relay cell inside it lights (§3 step 3). Repels walk over
a blot's cells like any infected cells and stop at the blot itself as at
any placed piece. Blots with arms are held back (NEXT_PASS: later).

## 11. Diagonal arms (stage 11)

`Dir` gains UL, UR, DL, DR; a diagonal arm walks (±1, ±1) per ring with
exactly the cardinal rules: walls stop it, a switch stops it and queues a
repel back along the same diagonal, a trap trips, a forbidden cell makes
the placement illegal, voids and the edge are passed over. A **diagonal
piece** has diagonal arms only (§1): the fifteen non-empty subsets of the
four diagonals, as the tiles are of the four cardinals; an opposite-only
pair (`ul+dr`, `ur+dl`) is the diagonal `UD` and is excluded from
generated content for the same reason. A relay's arms follow the same
rule. Arms are visited cardinal first, then UL, UR, DL, DR (only one
family is ever present on a piece). The solver adds two line families
(the diagonal and the antidiagonal) when a level has a diagonal arm
anywhere; every rule is written over families, so nothing else changes.
Text form `ul`, `ul+dr`.

## 12. Relay cells (stage 12)

A relay is an active cell carrying an arm mask (`LevelDef.CellDataAt`,
text `loc:arms` in the world files). It must be infected like any active
cell. The moment it turns from 1 to 4 — by a piece's arm, an area, or
another relay — it spreads its own arms from itself with the rules of §3
(unlimited reach; walls stop, switches repel, traps trip, forbidden cells
make the originating placement illegal, voids are jumped), at most once
per propagation. Chains are allowed and end on their own because a relay
only fires on its 1 → 4 transition. Undo rebuilds them (§5) since it
re-propagates from the initial board. Drawn as a small hub with a stub per
arm over the cell (`BoardView`), a shape and not a colour (R-1001).
