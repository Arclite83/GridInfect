# Generator v2 — solver, grader, pipeline

Companion to `NEXT_PASS.md` decisions 1–2 and `EXECUTION_PLAN.md` stages 1–2.
Code: `GridInfect.Core.Solving` (solver, counter, grader) and
`GridInfect.Core.Generation` (pipeline). Oracle: `tools/level_metrics.py`,
pinned by `docs/level_metrics_classic.json`.

## Solver

### Model

The solver reasons about **lines**, not cells. A line is a maximal run of
non-blocker cells (void or active) along one direction family's axis,
bounded by walls, switches, traps, or the board edge. Void cells stay in a
line because arms jump gaps (RULES §4). Direction families are data
(`Families.Cardinal` = rows `L/R` and columns `U/D`); a diagonal family is
one more table entry, and every rule below is written over families.

A **placement** is (piece, cell). Its **static coverage** is the cell itself
plus, for each arm, the rest of that arm's line from the cell outward. Static
coverage treats switches and traps as blockers and ignores repels, exactly
as the Python oracle does; order feasibility (repels, the one allowed trap
trip) is checked on the final assignment through the real rules.

A **candidate** is a placement not yet ruled out. Every unused piece starts
as a candidate on every active, unoccupied cell. The candidates that can
cover a cell are exactly the placements in its row line and its column line
(on it, or an arm pointing along the line at it). Rules either **place** (a
placement every solution must contain) or **exclude** (a placement no
solution contains). The solver runs tiers 1–3 to a fixpoint, then tier 4
once, and repeats; when nothing fires it branches (a **guess**) so the
answer is always found, but only a run with zero guesses counts as
`Solved`. `Solved` implies a unique solution: every placed piece is forced,
the placed set covers the board, and any other minimal cover would have to
contain the same forced placements.

Two pieces with the same tile are interchangeable, so candidates are
compared as (tile, cell) and a forced tile goes to the lowest unused index.

### Tier 1 — LineOwnership

*Every candidate that can still cover some uncovered cell is the same tile
on the same cell.* Place it. The rule is named for what it means on the
board: of the two lines through that cell, only one placement can still own
it. It fires once other tiers have thinned the candidates; on an untouched
board it cannot (any piece on any cell covers that cell), which is the
"cell sole cover never fires" observation in `NEXT_PASS.md`.

Worked example, classic level 1 (id 0), pieces `D`,`R`:

```
 2 ....1.
 3 ....1.
 4 ....1.
 5 ....1.
 6 11111.
 7 ....1.
```

After tier 3 has put `D` on (2,4) — see below — cell (6,3) is uncovered.
Its row line is (6,0)…(6,4); its column line is just (6,3). `R` on (6,3)
covers it, and `R` on (6,0), (6,1), (6,2) reach it with the right arm. But
`R` anywhere but (6,0) leaves (6,0) with no candidate at all (nothing has a
left arm, and `D` is used), so those were excluded by the tier-3 lookahead.
The only candidate left for (6,3) is `R` at (6,0): place it. Evidence: the
cells of both lines through (6,3).

### Tier 2 — ArmExclusion

Exclusions from what a placement's arms would do:

- **One trip.** Only the winning placement may trip a reset trap (RULES
  §4.1: the win check runs before the reset, and a non-winning trip resets
  the board). Once a trap-tripping placement is placed, every other
  trap-tripping candidate goes.
- **Committed trip.** If some uncovered cell can only be reached by
  trap-tripping candidates, the one allowed trip is spoken for: every
  trap-tripping candidate that does not cover that cell goes.

Candidates that cover nothing still uncovered are dropped silently
(bookkeeping, not a deduction). Later elements add rules here: a forbidden
cell (stage 10) excludes every placement whose spread would touch it.

Worked example, classic level 36 (id 35), pieces `LD`,`RUD`,`LD`, traps
at (3,2) and (7,2), wall at (5,1):

```
 2 .1.1..
 3 ..51..
 4 .111..
 5 .211..
 6 .111..
 7 ..51..
 8 .11.1.
 9 .1.1..
```

Counting places the first `LD` on (2,3) and the second on (4,2); the
second one's down arm runs (5,2), (6,2) and ends on the trap at (7,2), so
that placement is the one trip. Cell (8,4) can be reached by `RUD` on it
(already excluded by counting: it would cover nothing else), `RUD` on
(8,1) (right arm over the void at (8,3)), or `RUD` on (8,2). `RUD` on
(8,2) has its up arm ending on the trap at (7,2): excluded, tier 2. `RUD`
on (8,1) is the only candidate left for (8,4): placed, tier 1.

### Tier 3 — PieceCounting

Count pieces against jobs. A set of uncovered cells such that no single
candidate covers two of them (an **independent set**) needs one piece per
cell, so its size is a lower bound on the placements still needed. The
solver takes the best of three greedy orders (fewest candidates first,
top-down, bottom-up) over the adjacency "some candidate covers both".

- **Contradiction.** Bound greater than the pieces left.
- **Exclusion.** Placing candidate *p* leaves the rest needing more than
  the pieces left minus one (bound computed without *p*'s piece): *p* goes.

Worked example, level 1 (id 0) again. Cells (2,4) and (6,0) are
independent: (2,4) can only be reached by a piece on it (nothing has an up
arm; its row line is one cell) and (6,0) only by a piece on it (nothing has
a left arm; its column line is one cell). Two cells, two pieces. Suppose
`R` on (2,4): the rest still contains (6,0) and (6,3) — no candidate of `D`
covers both — bound 2 against one piece left: excluded. The same for `D`
anywhere off (2,4) and (6,0), and for `D` on (6,0) (then (2,4) and (6,3)
remain for `R` alone). `D` on (2,4) is the only candidate left for (2,4):
placed, tier 3. Then tier 1 finishes the level.

### Tier 4 — Contradiction1

Suppose candidate *p*; run tiers 1–3 to a fixpoint from there; if they
contradict (a cell with no candidate, the count bound broken, or a complete
cover no placement order wins) exclude *p*. One level deep, never nested.

Worked example, classic level 51 (id 50), pieces `R`,`LUD`,`RD`,`U`,
switch at (2,3), traps at (2,0) and (7,0), wall at (5,1):

```
 0 ....1.
 1 ....1.
 2 5..31.
 3 1..111
 4 ...1..
 5 12111.
 6 1..11.
 7 5...1.
 8 ..1111
 9 ...11.
10 ....1.
```

Counting places `R` on (8,2) and leaves `LUD` on (5,3) or (5,4), `RD` on
(3,0) or (3,3), `U` on (6,0), (9,3) or (10,4); nothing in tiers 1–3
separates them. Suppose `LUD` on (5,3). Counting then forces `U` on (10,4)
and ownership `RD` on (3,0); the three cover the board, but no order
wins: `RD` on (3,0) ends its down arm on the trap at (7,0) and must be the
last placement, while `LUD` on (5,3) ends its up arm on the switch at
(2,3), whose repel un-infects (3,3) and (4,3) unless `LUD` is last.
Excluded. The same happens for `RD` on (3,0), `U` on (9,3) and `U` on
(10,4) — four refutations — after which `LUD` on (5,4) is the only
candidate for (0,4) (tier 1), then `U` on (6,0) and `RD` on (3,3).

### Search fallback and `Guesses`

When tier 4 changes nothing, the solver picks the uncovered cell with the
fewest (tile, cell) candidates and tries each in turn, recursing. Each
branch point is one guess. A result with guesses is `Complete` (an order
that wins is returned) but never `Solved`.

### Output

`SolveResult`: `Solved`, `Trace` (placements in order, each with the
highest tier that fed it and the cells the rule reasoned about), `MaxTier`,
`TierCounts` (rule firings per tier), `Guesses`, plus `Complete` and
`Placements` (a winning order). `Deducer.Candidates` exposes the surviving
candidate cells per piece after tiers 1–3, which is what the Lock tool
reads for "the next forced placement".

### Counter

`SolutionCounter` is a line-for-line mirror of `level_metrics.solve`:
covering sets are enumerated most-constrained-cell first (lowest cell on
ties), deduped as sets of (tile, cell), then — on boards with a switch or a
trap — kept only if some placement order wins through the real rules. The
enumeration order is part of the contract: the count is the number of sets
*this* search reaches (a non-minimal set can appear when an early placement
is later made redundant). Without switches the order check is closed-form:
non-trippers in any order, then one trap tripper; with switches it is a
depth-first search over orders that prunes failed prefixes.

`docs/level_metrics_classic.json` (from `tools/gen_level_metrics_golden.py`)
pins all 128 counts; `SolverTests` compares. Note for the record: until
stage 1 the oracle's order check was dead code (`not hit_cap` on a list),
so every number in `NEXT_PASS.md` derived from "order-aware" counts was the
static count. With the check live there are 31 unique classic levels, not
27, and 53 levels lose static covers to order.

### Grades

`Grader.Effort` weights rule firings: ownership 1 per placement, arm
exclusion 2 per round, counting 4 per round, contradiction 12 per pass
(a pass refutes every candidate it can; capped at 4 passes). Grade bands:

| Grade | Effort | Typical board |
|---|---|---|
| G1 | ≤ 7 | 2–3 pieces, one counting round |
| G2 | ≤ 12 | 3–4 pieces, one or two counting rounds |
| G3 | ≤ 18 | 4–5 pieces, two or three counting rounds |
| G4 | ≤ 26 | 5 pieces with several rounds, or one contradiction pass |
| G5 | > 26 | a contradiction pass plus counting, or two passes |
| G6 | — | not solved by deduction (guesses, or no solution) |

Classic levels that solve by deduction: 31; grades locked in
`SolverTests.ClassicGradesAreStable`. Levels 51 and 87 (ids 50, 86) are the
two that need tier 4.

## Pipeline

`GeneratorV2.Generate(GenSpec spec, ulong seed)` returns a `GeneratedLevel`
or null with a `Rejection` reason. Every random draw comes from
`Pcg32(seed)` in a fixed order and nothing downstream depends on hash
container order, so the same spec and seed give the same bytes.

1. **Sample.** Piece count uniform in `[MinPieces, MaxPieces]`. For each
   piece, a tile (`rng.Next(15)`) and a cell (`rng.Next(66)`), redrawn
   while: the tile repeats (unless `AllowDuplicateTiles`); the tile is `UD`
   or `LR` (unless `AllowSymmetricTiles` — a piece with only opposite arms
   covers its whole line from any cell of it, so nothing but other pieces'
   cells can pin it); an arm has no in-bounds cell to reach; the cell
   shares a row or column with an earlier piece (`ExclusiveLines`); or it
   is closer than `MinPieceDistance` (Manhattan) to one. More than 200
   redraws rejects the seed (`Tiles`).
2. **Carve.** `CarveMode.Runs` (default): each arm draws a length in
   `[MinRun, MaxRun]`, activates that many in-bounds cells, and draws once
   more for an end wall right past the run (`EndWallChance`/20).
   `CarveMode.Gaps` is the v1 roll (one draw per cell, chance
   `BaseChance − Falloff·offset`, shape bias past offset 3). Active count
   outside `[MinActive, MaxActive]` rejects (`Size`). End walls are then
   applied on void cells, each kept only if the sampled solution still
   covers the board and every arm of every sampled piece still reaches an
   active cell.
3. **Count.** The oracle count with `SolutionCap`; above it rejects
   (`TooMany`).
4. **Prune.** While the board is not unique, place one wall: candidate
   cells are every void or active non-piece cell whose wall keeps the
   sampled solution covering and every sampled arm useful (a blinded arm
   turns the tile into a smaller one and hands the level a swap ambiguity
   no wall can break). Candidates are ranked by how many of the current
   covers (up to 400) survive the wall, void cells before active ones on
   ties, then the best two are counted with the fast distinct-cover search
   and the strictly smallest wins. No strict reduction, `MaxPruneSteps`,
   or `MaxWalls` rejects (`NotUnique`). The loop runs on the fast count;
   when it reaches 1 the oracle count confirms (it can still see a
   non-minimal cover, in which case pruning continues on its number).
5. **Deduce.** `Deducer.Solve` must return `Solved` with zero guesses
   (`NotDeducible`), and the deduced set must use every piece
   (`Decoy`, when `RequireAllPieces`).
6. **Grade** with `Grader`; outside `[MinGrade, MaxGrade]` rejects.
7. **Emit** `Def`, the sampled solution in a winning order, the trace, the
   grade and effort, the seed, wall count, and the canonical hash:
   FNV-1a 64 over the smallest of the four encodings (identity, horizontal
   flip with `L<->R`, vertical flip with `U<->D`, both) of board plus
   sorted tiles. The batch tool dedupes on it.

### Batch tool

`tools/gen_levels` (source `src/GenLevels`) writes accepted levels as JSONL:
`{seed, grade, effort, board, pieces, solution, trace, hash, walls}` and
prints the acceptance report per rejection reason. Flags cover every
`GenSpec` field; `--threads N` generates seed chunks in parallel and
consumes them in seed order, so the output is independent of N.

Acceptance rates (default carve, `--threads 3`, seeds from 100000) are
recorded in the stage 2 PR.

## Elements (stages 8–12)

Each element is a flag in `GenSpec.Elements`. Its draws come from the
same `Pcg32` but only when the flag is on, so classic specs keep their
seeds. Per element: how the sample and the carve change, what the solver
does with it, and the shipped world and daily slot.

### Short arms (`Element.ShortArms`, stage 8)

Sample: after the tile, each arm rolls `ShortArmChance`/20 to become a
short arm of reach 1 or 2 (one more draw). Carve: a short arm's run is
capped at its reach (Runs mode) or its cells beyond the reach are skipped
(Gaps mode). Prune: the arm-useful check uses the reach. Solver: static
coverage already honours reach; no new rule. World `w13 Short Arms`
(pieces 3–5, G2–G4, chance 12/20); daily: Tuesdays.

### Area piece (`Element.Area`, stage 9)

Sample: each piece rolls `AreaChance`/20 (one draw) to be a blot instead
of its tile; at most one blot per board unless duplicate tiles are
allowed. Carve: the blot's eight neighbours each roll the ring-1 chance.
Prune: a blot must cover at least one neighbour. Solver: the area is part
of static coverage; no new rule. World `w14 Blots` (pieces 3–5, G2–G4,
chance 8/20); daily: Wednesdays.

### Forbidden cells (`Element.Forbidden`, stage 10)

A second pruner beside walls. A forbidden cell goes on a void cell the
sampled solution never crosses (so the solution stays legal); every cover
with a placement whose spread would cross it dies, which prunes harder
than a wall on the same cell. The pruner ranks both kinds per cell and
takes the strictly best; a tie goes to the forbidden cell; `MaxForbidden`
caps them. Solver: illegal placements never enter the candidate set, and
their exclusion counts as one arm-exclusion round in the grade. World
`w15 Keep Clean` (pieces 3–5, G2–G4, up to 4 forbidden cells); daily:
Thursdays.

### Diagonal arms (`Element.Diagonals`, stage 11)

Sample: a piece rolls `DiagonalChance`/20 to gain one diagonal arm (two,
one time in three), redrawn if that leaves only an opposite pair. Carve
and prune are direction-agnostic. Solver: the two diagonal families join
the line map. World `w16 Diagonals` (pieces 3–5, G2–G4, chance 14/20);
daily: Fridays.

### Relay cells (`Element.Relays`, stage 12)

Sample and carve: after a piece's arms are carved, the piece rolls
`RelayChance`/20; one of its carved cells (uniform draw) becomes a relay
with one arm (two, one time in three; diagonals only with
`Element.Diagonals`), and the relay's arms are carved as runs, so the
sampled solution lights the relay and covers what it spreads to. Prune:
every relay arm must reach a cell; a wall on the relay would uncover its
cells, so `Covers` rejects it. Solver: static coverage follows relay
chains, and forbidden legality does too. World `w17 Relays` (pieces 3–5,
G2–G4, chance 14/20); daily: Saturdays; Sundays mix short arms, forbidden
cells and diagonals.
