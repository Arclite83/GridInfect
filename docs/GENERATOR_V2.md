# Generator v2 — solver, grader, constructor

Companion to `NEXT_PASS.md` decisions 1–2 and `EXECUTION_PLAN.md` stages 1–2.
Code: `GridInfect.Core.Solving` (solver, counter, grader) and
`GridInfect.Core.Generation` (sampler and constructor). Oracle:
`tools/level_metrics.py`, pinned by `docs/level_metrics_classic.json`.

The method in one sentence: never generate a puzzle; generate a solution,
then subtract information from it until exactly one path leads back, then
take away every piece of information the proof does not need, then read
the difficulty off the solver's trace. The sampler is the V1 algorithm's
steps 1–5; the constructor is what V1 and the stage-2 pruner lacked.

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
solution contains). The solver runs tiers 1–3 to a fixpoint, then one tier
4 pass, then (only when that finds nothing) one tier 5 pass, and repeats;
when nothing fires it branches (a **guess**) so the answer is always found,
but only a run with zero guesses counts as `Solved`. `Solved` implies a unique solution: every placed piece is forced,
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
cover no placement order wins) exclude *p*. One level deep.

### Tier 5 — Contradiction2

Suppose candidate *p*; from there run tiers 1–3 and tier 4 passes to a
fixpoint; if that contradicts, exclude *p*. A suppose inside a suppose:
the lookahead cap (`Depth.Max` = 2). Tried only when a tier 4 pass over
every candidate found nothing, and counted once per pass like tier 4.
Anything past it is the search fallback, which no shipped level may need.

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

When tier 5 changes nothing, the solver picks the uncovered cell with the
fewest (tile, cell) candidates and tries each in turn, recursing. Each
branch point is one guess. A result with guesses is `Complete` (an order
that wins is returned) but never `Solved`.

### Output

`SolveResult`: `Solved`, `Trace` (placements in order, each with the
highest tier that fed it and the cells the rule reasoned about), `MaxTier`,
`TierCounts` (rule firings per tier; passes for tiers 4–5), `Guesses`,
`Depth` (0 = tiers 1–3 only, 1 = a tier 4 pass was needed, 2 = a tier 5
pass, 3 = guessed), `PeakOpen` (the most **open** pieces at any forced
placement or stuck point: an open piece is unplaced with more than one
candidate cell, a decision the player still holds), plus `Complete` and
`Placements` (a winning order). `Deducer.Solve(def, placed)` starts from
pre-placed pieces (a level's locks, the Lock tool's state).
`Deducer.Candidates` exposes the surviving candidate cells per piece after
tiers 1–3, which is what the Lock tool reads for "the next forced
placement".

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

The grade is read off the trace on two axes, the model from the
escape-room and logic-grid generators: **depth** is the lookahead the
solve needed, **peak open** is how many undecided pieces the player held
at once. Piece types the player must translate before the line rules
apply count as depth (`Grader.Translation`: diagonal arms +1, the area
blot +1, relays +1; short arms are read off the piece and cost nothing);
`Grader.EffectiveDepth` is solver depth plus translation, and the
constructor rejects anything past `Depth.Max` (`TooDeep`).

| Depth \ peak open | 0–1 | 2–3 | 4+ |
|---|---|---|---|
| 0 (tiers 1–3) | G1 | G2 | G3 |
| 1 (one suppose) | G3 | G3 | G4 |
| 2 (a suppose inside a suppose) | G5 | G5 | G5 |
| guessed, or no solution | G6 | G6 | G6 |

Read: G1 is read off the board; G2 holds two or three pieces in mind; G3
holds four or more, or supposes once with little open; G4 supposes with
four or more open; G5 needs the nested suppose. `Grader.Effort` (rule
firings weighted 1/2/4/12/24 by tier) orders levels within a band; the
bake sorts on grade, trace length, effort.

Calibration (`SolverTests.ClassicGradesAreStable`, the 31 unique classic
levels): 12 G1, 12 G2, 5 G3, 2 G4 (ids 50 and 86, the two that need a
suppose), no G5. The stage-2 worlds under the old effort bands contained
no level needing a nested suppose; G5 content now comes from the
constructor at 5–6 pieces at under one percent of seeds.

## Pipeline

`GeneratorV2.Generate(GenSpec spec, ulong seed)` = `GeneratorV2.Sample`
then `Constructor.Build`; it returns a `GeneratedLevel` or null with a
`Rejection` reason. Every random draw comes from `Pcg32(seed)` in the
sampler, in a fixed order; the constructor draws nothing (every choice is
a strict order over cells and kinds) and nothing downstream depends on
hash container order, so the same spec and seed give the same bytes.

### Sampler (`GeneratorV2.Sample`)

The solution comes first. Pairing rules are the sampler's rejection
conditions; the fill is derived by carving the solution's own spread.

1. **Sample.** Piece count uniform in `[MinPieces, MaxPieces]`. For each
   piece, a tile (`rng.Next(15)`) and a cell (`rng.Next(66)`), redrawn
   while: the tile repeats (unless `AllowDuplicateTiles`); the tile is `UD`
   or `LR` (unless `AllowSymmetricTiles` — a piece with only opposite arms
   covers its whole line from any cell of it, so nothing but other pieces'
   cells can pin it); an arm has no in-bounds cell to reach; the cell
   shares a row or column with an earlier piece (`ExclusiveLines`); or it
   is closer than `MinPieceDistance` (Manhattan) to one. More than 200
   redraws rejects the seed (`Tiles`). Elements decorate the tile after
   the sample is accepted (§Elements).
2. **Carve.** `CarveMode.Runs` (default): each arm draws a length in
   `[MinRun, MaxRun]` and activates that many in-bounds cells; the cell
   past the run is recorded as a run end. `CarveMode.Gaps` is the v1 roll
   (one draw per cell, chance `BaseChance − Falloff·offset`, shape bias
   past offset 3). Active count outside `[MinActive, MaxActive]` rejects
   (`Size`).
3. **Maximal givens.** A wall goes on every void run end where the sampled
   solution still covers the board and every arm of every sampled piece
   still reaches an active cell (a wall that blinds an arm turns the tile
   into a smaller one). This is the over-constrained start the
   constructor subtracts from.

### Constructor (`Constructor.Build`)

A `Sample` (specs, cells, board, relay data) in; a level out. Any solution
can be a sample: `Sample.FromLevel` turns a stored level and its solution
into one, which is how the 128 classic solutions run through the
constructor as a fixture (`GeneratorV2Tests.ClassicSolutionsConstructToUniqueMinimalLevels`).

**Givens** (`Given`, `GivenKind`) are the units of information a level
hands the player, in three pools:

| Pool | Kind | States | Withdraws | What it kills |
|---|---|---|---|---|
| blockers | `Wall` | a void cell becomes a wall | void | every cover with an arm that crossed the cell |
| blockers | `Forbidden` (`Element.Forbidden`, ≤ `MaxForbidden`) | a void cell must stay clean | void | every cover with a spread that would touch it |
| blockers | `Trap` (`Element.Traps`, ≤ `MaxTraps`) | a void cell resets | void | every cover whose arms end on it (statically) |
| fill | `Gap` | a covered non-piece cell becomes void (arms jump it) | active | every cover with a piece on the cell; withdraws a requirement, so it can also let new covers in |
| pre-fixed piece | `Lock` (≤ `MaxLocks`, **0 by default — see below**) | a piece is placed and locked at its solution cell before play | — | every cover with that piece elsewhere |

A given is **valid** when the sample still solves the level: every sampled
piece on an active cell legally, together covering every active cell,
every arm still reaching a cell (`RequireUsefulArms`, off for foreign
samples like the classics), and some order with the locked pieces first
winning through the real rules (`SolutionCounter.WinningOrder`, a replay).

1. **Validate.** The sample must solve its own board (`Unwinnable`); with
   `RequireAllPieces`, no piece's spread may be covered by the others
   (`Decoy`).
2. **Discriminate.** The **alternatives** are every covering set the
   oracle reaches with the locked pieces fixed, minus the sample, minus
   (with traps or switches) the sets no order wins; above `SolutionCap`
   rejects (`TooMany`). While any remain: rank every valid board given by
   how many current alternatives it kills, recount the best three exactly
   (a gap can let new covers in), and state the one that leaves strictly
   the fewest — ties go to wall, then gap, then forbidden, then trap, then
   the lowest cell. When no cell reduces the count, a lock: the piece
   whose pre-placement leaves the fewest (lowest index on ties). This is
   the case no cell can break: two mirror pieces (an `L` and an `R`, say)
   on parallel closed runs swap rows. More than `MaxGivens` rounds, or no
   given that reduces the count, rejects (`NotUnique`).
3. **Minimize.** Withdraw every given whose absence keeps the level valid
   and unique (the oracle count with locks fixed = 1): locks first, then
   forbidden cells and traps, then every wall on the board (the sampler's
   run ends included), then the gaps stated in step 2. What remains is
   irreducible: `GeneratorV2Tests.EveryGivenIsLoadBearing` withdraws each
   wall, forbidden cell and lock of accepted levels and checks uniqueness
   breaks. Sampled voids are the sampler's shape, not givens, and are not
   filled.
4. **Trace.** `Deducer.Solve` with the locks placed must return `Solved`
   with zero guesses (`NotDeducible`); effective depth past `Depth.Max`
   rejects (`TooDeep`); with `RequireAllPieces` the deduced set must use
   every piece (`Decoy`); the grade must fall in `[MinGrade, MaxGrade]`
   (`Grade`).
5. **Emit** `Def`, the sampled solution in a winning order with locked
   pieces first, `Locks`, the trace, grade, effort, depth and peak, the
   seed, the givens left by kind, and the canonical hash: FNV-1a 64 over
   the smallest of the four encodings (identity, horizontal flip with
   `L<->R`, vertical flip with `U<->D`, both) of board plus sorted pieces
   (plus relay arms and locks, transformed with the board). The batch tool
   dedupes on it.

Acceptance at the default spec (2–5 pieces, walls) is 183 of the first 300
seeds, down from 238 when one lock was allowed; the stage-2 pruner
accepted under a quarter at four or more pieces. The 55 seeds the lock
used to rescue now reject as `NotUnique` — that ambiguity is exactly what
a pre-placed piece existed to break — and the rest is `Grade` (a band the
sample did not land in). The generator scans further seeds, so pool sizes
are unchanged; the cost is wall clock, and it is only steep at the top:
regenerating every world took under six minutes, of which the two G5
worlds were four (w11 3,359 seeds for 20 levels, w12 3,281 for 20).

### Locks at load

A level's locks travel with it (`GeneratedLevel.Locks`, JSONL `locks`,
`WorldData.Locks` / `DailyData.Locks`) and the loading action places them
through the rules before play (`Locked.Apply`, from `world.load`,
`daily.begin`, `endless.begin` and `endless.advance`): the piece sits on
the board locked, cannot be lifted, and survives a full reset, exactly as
a Lock-tool placement does. Stored solutions list locked pieces first;
solvers and counters take them as `placed` (`Locked.Placed`).

**No shipped level uses one.** `GenSpec.MaxLocks` is 0, so the worlds, the
Daily pools and Endless (which generates on the device from the same
`GenSpec`) never hand the player a piece they cannot move: a sample whose
ambiguity only a lock could break is rejected as `NotUnique` and the
generator takes the next seed. Everything above stays — the given kind,
the discriminator's fallback to it, `Locked.Apply` and the load path — so
raising the budget is one field, and a level that does carry a lock still
loads correctly. `GivensAndHintsTests` holds both halves:
`NoShippedLevelPreplacesAPiece` over every world level, every Daily pool
and the Endless and Daily specs, and
`LockedApplyStillPlacesInfectsAndSurvivesAFullReset` over the load path
itself, so the budgeted-out mechanism cannot rot.

The Lock *tool* is unaffected: a piece the player spends a lock on is
placed and locked the same way, and that is a placement they asked for.
Touching a locked piece nudges it and puts it back, so it still answers.

### Batch tool

`tools/gen_levels` (source `src/GenLevels`) writes accepted levels as JSONL:
`{seed, grade, effort, depth, peak, board, pieces, solution, trace, hash,
givens, walls[, gaps][, forbidden][, traps][, locks][, relays]}` and prints
the acceptance report per rejection reason. Flags cover every `GenSpec`
field (`--max-givens`, `--max-locks`, `--max-forbidden`, `--max-traps`
bound the pools); `--daily Monday` takes the weekday's spec from
`DailySpec`; `--threads N` generates seed chunks in parallel and consumes
them in seed order, so the output is independent of N.
`tools/gen_worlds.sh` regenerates the worlds, `tools/gen_daily.sh` the
seven Daily pools; `tools/bake_worlds.py` bakes both.

## Elements (stages 8–12)

Each element is a flag in `GenSpec.Elements`. Its draws come from the
same `Pcg32` but only when the flag is on, so classic specs keep their
seeds. Per element: how the sample and the carve change, what the solver
does with it, and the shipped world and daily slot.

### Short arms (`Element.ShortArms`, stage 8)

Sample: after the tile, each arm rolls `ShortArmChance`/20 to become a
short arm of reach 1 or 2 (one more draw). Carve: a short arm's run is
capped at its reach (Runs mode) or its cells beyond the reach are skipped
(Gaps mode). Constructor: the arm-useful check uses the reach. Solver: static
coverage already honours reach; no new rule. World `w13 Short Arms`
(pieces 3–5, G2–G4, chance 12/20); daily: Tuesdays.

### Area piece (`Element.Area`, stage 9)

Sample: each piece rolls `AreaChance`/20 (one draw) to be a blot instead
of its tile; at most one blot per board unless duplicate tiles are
allowed. Carve: the blot's eight neighbours each roll the ring-1 chance.
Constructor: a blot must cover at least one neighbour. Solver: the area is part
of static coverage; no new rule. World `w14 Blots` (pieces 3–5, G2–G4,
chance 8/20); daily: Wednesdays.

### Forbidden cells (`Element.Forbidden`, stage 10)

A blocker given beside walls (§Pipeline). A forbidden cell goes on a void
cell the sampled solution never crosses (so the solution stays legal);
every cover with a placement whose spread would cross it dies, which
kills more than a wall on the same cell. The constructor ranks both kinds
per cell and takes the strictly best; a tie goes to the wall (the quieter
kind); `MaxForbidden` caps them. Solver: illegal placements never enter the candidate set, and
their exclusion counts as one arm-exclusion round in the grade. World
`w15 Keep Clean` (pieces 3–5, G2–G4, up to 4 forbidden cells); daily:
Thursdays.

### Diagonal arms (`Element.Diagonals`, stage 11)

Sample: a piece rolls `DiagonalChance`/20 to gain one diagonal arm (two,
one time in three), redrawn if that leaves only an opposite pair. Carve
and constructor are direction-agnostic. Solver: the two diagonal families
join the line map; a diagonal arm is one translation layer in the grade. World `w16 Diagonals` (pieces 3–5, G2–G4, chance 14/20);
daily: Fridays.

### Relay cells (`Element.Relays`, stage 12)

Sample and carve: after a piece's arms are carved, the piece rolls
`RelayChance`/20; one of its carved cells (uniform draw) becomes a relay
with one arm (two, one time in three; diagonals only with
`Element.Diagonals`), and the relay's arms are carved as runs, so the
sampled solution lights the relay and covers what it spreads to.
Constructor: every relay arm must reach a cell; no given ever lands on a
relay cell. Solver: static coverage follows relay chains, and forbidden
legality does too; a relay is one translation layer in the grade. World `w17 Relays` (pieces 3–5,
G2–G4, chance 14/20); daily: Saturdays; Sundays mix short arms, forbidden
cells and diagonals.
