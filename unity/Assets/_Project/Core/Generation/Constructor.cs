using System;
using System.Collections.Generic;
using GridInfect.Core.Solving;

namespace GridInfect.Core.Generation
{
    // A sampled solution: the pieces, where they go, and the fill their
    // spread carved, with the sampler's maximal givens (a wall past every
    // run) already on the board. Everything the constructor needs; nothing
    // about how the sampler chose it.
    public sealed class Sample
    {
        public PieceSpec[] Specs;
        public int[] Cells;        // per piece: its solution cell
        public byte[] Board;
        public byte[] CellData;    // relay arms, or null
        public int Relays;

        // A known level as a sample: its pieces at a stored solution, its
        // board as the starting givens. The classic regression goes through here.
        public static Sample FromLevel(LevelDef def, (int piece, int cell)[] solution)
        {
            var cells = new int[def.Specs.Length];
            for (int k = 0; k < cells.Length; k++) cells[k] = -1;
            foreach (var (piece, cell) in solution) cells[piece] = cell;
            var board = new byte[Grid.Cells];
            def.CopyBoardTo(board);
            var cellData = new byte[Grid.Cells];
            def.CopyCellDataTo(cellData);
            return new Sample { Specs = (PieceSpec[])def.Specs.Clone(), Cells = cells, Board = board, CellData = cellData };
        }
    }

    // Solution-first construction (docs/GENERATOR_V2.md §Pipeline): the
    // sample is the answer; the constructor subtracts information from it
    // until exactly one path leads back, then removes every given the
    // uniqueness proof does not need, then reads the difficulty off the
    // solver's trace. Deterministic: no draws, every choice is a strict
    // order over cells and kinds.
    public static class Constructor
    {
        public static GeneratedLevel Build(Sample sample, GenSpec spec, ulong seed, out Rejection rejection, List<string> log = null)
        {
            if (sample == null) throw new ArgumentNullException(nameof(sample));
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            rejection = Rejection.None;
            var watch = log != null ? System.Diagnostics.Stopwatch.StartNew() : null;

            int n = sample.Specs.Length;
            var board = (byte[])sample.Board.Clone();
            var sampled = new int[n];
            for (int k = 0; k < n; k++)
            {
                if (sample.Cells[k] < 0) { rejection = Rejection.Decoy; return null; }   // a piece the solution never places
                sampled[k] = k * Grid.Cells + sample.Cells[k];
            }
            var puzzle = new Puzzle(new LevelDef(board, sample.Specs, sample.CellData), sampled, spec);
            if (!puzzle.Valid()) { rejection = Rejection.Unwinnable; return null; }
            if (spec.RequireAllPieces && HasDecoy(new LineMap(puzzle.Def), sampled)) { rejection = Rejection.Decoy; return null; }

            // 1. Discriminate: while another solution exists, add the given
            // that leaves the fewest; a lock only when no cell reduces them.
            var added = new List<Given>();
            while (true)
            {
                var alternatives = puzzle.Alternatives(spec.SolutionCap, out bool capped);
                log?.Add($"seed {seed}: {n} pieces, {alternatives.Count} alternatives{(capped ? " (capped)" : "")}, {added.Count} givens [{watch.ElapsedMilliseconds} ms]");
                if (capped) { rejection = Rejection.TooMany; return null; }
                if (alternatives.Count == 0) break;
                if (added.Count >= spec.MaxGivens) { rejection = Rejection.NotUnique; return null; }
                if (!Choose(puzzle, alternatives, out Given given, out int after) && !ChooseLock(puzzle, alternatives, out given, out after))
                {
                    log?.Add("  no given reduces the alternatives");
                    rejection = Rejection.NotUnique;
                    return null;
                }
                puzzle.State(given);
                added.Add(given);
                log?.Add($"  {given} -> {after} alternatives");
            }

            // 2. Minimize: withdraw every given the proof does not need, the
            // loud ones first (locks, forbidden, traps), then walls, then gaps.
            var strip = new List<Given>();
            foreach (int piece in puzzle.Locks) strip.Add(new Given(GivenKind.Lock, piece));
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                if (puzzle.Board[loc] == Cell.Forbidden) strip.Add(new Given(GivenKind.Forbidden, loc));
                else if (puzzle.Board[loc] == Cell.ResetTrap) strip.Add(new Given(GivenKind.Trap, loc));
            }
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                if (puzzle.Board[loc] == Cell.Wall) strip.Add(new Given(GivenKind.Wall, loc));
            }
            foreach (Given g in added)
            {
                if (g.Kind == GivenKind.Gap) strip.Add(g);
            }
            int kept = 0, walls = 0, gaps = 0;
            foreach (Given g in strip)
            {
                puzzle.Withdraw(g);
                if (puzzle.Valid() && puzzle.Unique(spec.SolutionCap)) continue;
                puzzle.State(g);
                kept++;
                if (g.Kind == GivenKind.Wall) walls++;
                if (g.Kind == GivenKind.Gap) gaps++;
            }
            int forbidden = 0, traps = 0;
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                if (puzzle.Board[loc] == Cell.Forbidden) forbidden++;
                if (puzzle.Board[loc] == Cell.ResetTrap) traps++;
            }
            log?.Add($"  minimal: {kept} of {strip.Count} givens kept ({walls} walls, {gaps} gaps, {forbidden} forbidden, {traps} traps, {puzzle.Locks.Count} locks) [{watch.ElapsedMilliseconds} ms]");

            // 3. Trace: the human solver must finish within the depth cap.
            var def = puzzle.Def;
            var solve = Deducer.Solve(def, puzzle.Placed());
            int depth = Grader.EffectiveDepth(solve, def);
            var grade = Grader.Grade(solve, def);
            log?.Add($"  solved={solve.Solved} depth={solve.Depth}+{Grader.Translation(def)} peak={solve.PeakOpen} grade={grade} effort={Grader.Effort(solve)} tiers={string.Join(",", solve.TierCounts)} [{watch.ElapsedMilliseconds} ms]");
            if (!solve.Solved) { rejection = Rejection.NotDeducible; return null; }
            if (depth > Depth.Max) { rejection = Rejection.TooDeep; return null; }
            if (spec.RequireAllPieces && solve.Placements.Length != n) { rejection = Rejection.Decoy; return null; }
            if (grade < spec.MinGrade || grade > spec.MaxGrade) { rejection = Rejection.Grade; return null; }

            // 4. Emit. The sampled solution is the stored one, locked pieces
            // first, in an order that wins (the deduced set: the level is unique).
            var locks = new (int piece, int cell)[puzzle.Locks.Count];
            for (int m = 0; m < locks.Length; m++) locks[m] = (puzzle.Locks[m], sample.Cells[puzzle.Locks[m]]);
            return new GeneratedLevel
            {
                Def = def,
                Solution = puzzle.Order(),
                Locks = locks,
                Trace = solve.Trace,
                Grade = grade,
                Effort = Grader.Effort(solve),
                Depth = depth,
                PeakOpen = solve.PeakOpen,
                Seed = seed,
                Hash = Canonical.Hash(def, locks),
                Walls = walls,
                Gaps = gaps,
                ForbiddenCells = forbidden,
                Traps = traps,
                LockCount = locks.Length,
                Relays = sample.Relays,
                Givens = kept,
            };
        }

        // The board given that leaves the fewest alternatives while the
        // sample still solves the level. Candidates are ranked by how many
        // of the current alternatives they kill; the best few are then
        // recounted exactly (a gap withdraws a requirement, so it can let
        // new covers in) and the strictly smallest count wins. Ties: wall,
        // then gap, then forbidden, then trap (the quiet kinds first), then
        // the lowest cell.
        const int ExactCandidates = 3;

        static bool Choose(Puzzle puzzle, List<int[]> alternatives, out Given best, out int bestAfter)
        {
            var ranked = new List<(int kills, int rank, Given given)>();
            var kinds = new List<GivenKind>(4);
            var spec = puzzle.Spec;
            int forbidden = 0, traps = 0;
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                if (puzzle.Board[loc] == Cell.Forbidden) forbidden++;
                if (puzzle.Board[loc] == Cell.ResetTrap) traps++;
            }
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                byte v = puzzle.Board[loc];
                if (puzzle.Def.CellDataAt(loc) != 0 || puzzle.IsPieceCell(loc)) continue;   // never a relay, never the solution's own cells
                kinds.Clear();
                if (v == Cell.Active) kinds.Add(GivenKind.Gap);
                if (v == Cell.Void)
                {
                    if ((spec.Elements & Element.Walls) != 0) kinds.Add(GivenKind.Wall);
                    if ((spec.Elements & Element.Forbidden) != 0 && forbidden < spec.MaxForbidden) kinds.Add(GivenKind.Forbidden);
                    if ((spec.Elements & Element.Traps) != 0 && traps < spec.MaxTraps) kinds.Add(GivenKind.Trap);
                }
                foreach (GivenKind kind in kinds)
                {
                    var given = new Given(kind, loc);
                    puzzle.State(given);
                    if (puzzle.Valid())
                    {
                        var map = new LineMap(puzzle.Def);
                        int kills = 0;
                        foreach (int[] alt in alternatives)
                        {
                            if (!Survives(map, alt)) kills++;
                        }
                        if (kills > 0) ranked.Add((kills, (int)kind * Grid.Cells + loc, given));
                    }
                    puzzle.Withdraw(given);
                }
            }
            ranked.Sort((x, y) => x.kills != y.kills ? y.kills.CompareTo(x.kills) : x.rank.CompareTo(y.rank));

            best = default;
            bestAfter = alternatives.Count;
            for (int n = 0; n < ranked.Count && n < ExactCandidates; n++)
            {
                puzzle.State(ranked[n].given);
                int after = puzzle.Alternatives(bestAfter + 1, out bool capped).Count;
                puzzle.Withdraw(ranked[n].given);
                if (capped || after >= bestAfter) continue;
                best = ranked[n].given;
                bestAfter = after;
                if (after == 0) break;
            }
            return bestAfter < alternatives.Count;
        }

        // The lock that leaves the fewest alternatives: lowest piece on ties.
        static bool ChooseLock(Puzzle puzzle, List<int[]> alternatives, out Given best, out int bestAfter)
        {
            best = default;
            bestAfter = alternatives.Count;
            if (puzzle.Locks.Count >= puzzle.Spec.MaxLocks) return false;
            for (int piece = 0; piece < puzzle.Sampled.Length; piece++)
            {
                if (puzzle.Locks.Contains(piece)) continue;
                var given = new Given(GivenKind.Lock, piece);
                puzzle.State(given);
                int after = puzzle.Valid() ? puzzle.Alternatives(bestAfter + 1, out bool capped).Count : int.MaxValue;
                puzzle.Withdraw(given);
                if (after >= bestAfter) continue;
                best = given;
                bestAfter = after;
                if (after == 0) break;
            }
            return bestAfter < alternatives.Count;
        }

        // Does the set still cover the board on this map: every placement
        // on an active cell, legal, and their spreads over every active cell.
        static bool Survives(LineMap map, int[] set)
        {
            var covered = CellMask.None;
            foreach (int p in set)
            {
                int cell = p % Grid.Cells;
                if (map.Def.BoardAt(cell) != Cell.Active) return false;
                var spread = map.Spread(map.Def.Specs[p / Grid.Cells], cell);
                if (spread.Forbidden) return false;
                covered |= spread.Covered;
            }
            return covered.Contains(map.ActiveMask);
        }

        // A piece whose spread the others already cover is not needed.
        static bool HasDecoy(LineMap map, int[] sampled)
        {
            for (int k = 0; k < sampled.Length; k++)
            {
                var others = CellMask.None;
                for (int p = 0; p < sampled.Length; p++)
                {
                    if (p != k) others |= map.Coverage(map.Def.Specs[sampled[p] / Grid.Cells], sampled[p] % Grid.Cells);
                }
                if (others.Contains(map.ActiveMask)) return true;
            }
            return false;
        }

        // Every arm of every sampled piece still reaches at least one active
        // cell: a wall or gap that blinds an arm turns the tile into a
        // smaller one and hands the level a swap ambiguity nothing can prune.
        // Every relay's arms reach something beyond the relay itself.
        public static bool ArmsUseful(LineMap map, int[] set)
        {
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                byte relay = map.Def.CellDataAt(loc);
                if (relay == 0) continue;
                if (map.Def.BoardAt(loc) != Cell.Active) return false;
                for (int d = 0; d < 8; d++)
                {
                    if ((relay & (1 << d)) == 0) continue;
                    var arm = new PieceSpec(0).WithArm((Dir)d);
                    if (map.Coverage(arm, loc).Count < 2) return false;
                }
            }
            foreach (int p in set)
            {
                var spec = map.Def.Specs[p / Grid.Cells];
                int cell = p % Grid.Cells;
                if (spec.Area && map.Coverage(new PieceSpec(0, 0, true), cell).Count < 2) return false;
                for (int d = 0; d < 8; d++)
                {
                    var dir = (Dir)d;
                    if (!spec.Has(dir)) continue;
                    var arm = new PieceSpec(0).WithArm(dir).WithReach(dir, spec.ReachOf(dir));
                    if (map.Coverage(arm, cell).Count < 2) return false;
                }
            }
            return true;
        }

        // The puzzle under construction: the board, the sampled solution,
        // the locked pieces, and the checks every step asks of it.
        sealed class Puzzle
        {
            public readonly byte[] Board = new byte[Grid.Cells];
            public readonly int[] Sampled;          // piece*Cells+cell
            public readonly List<int> Locks = new List<int>();
            public readonly GenSpec Spec;
            readonly byte[] _cellData = new byte[Grid.Cells];
            readonly PieceSpec[] _specs;
            readonly string _key;
            LevelDef _def;

            public Puzzle(LevelDef def, int[] sampled, GenSpec spec)
            {
                _def = def;
                _specs = def.Specs;
                def.CopyBoardTo(Board);
                def.CopyCellDataTo(_cellData);
                Sampled = sampled;
                Spec = spec;
                _key = Key(def, sampled);
            }

            public LevelDef Def => _def ?? (_def = new LevelDef(Board, _specs, _cellData));

            public bool IsPieceCell(int loc)
            {
                foreach (int p in Sampled) if (p % Grid.Cells == loc) return true;
                return false;
            }

            public void State(Given g)
            {
                if (g.Kind == GivenKind.Lock) { Locks.Add(g.Piece); return; }
                Board[g.Cell] = g.Value;
                _def = null;
            }

            public void Withdraw(Given g)
            {
                if (g.Kind == GivenKind.Lock) { Locks.Remove(g.Piece); return; }
                Board[g.Cell] = g.Absent;
                _def = null;
            }

            // The locked pieces as the counter and the deducer see them.
            public PieceState[] Placed()
            {
                if (Locks.Count == 0) return null;
                var placed = new PieceState[Sampled.Length];
                foreach (int piece in Locks)
                {
                    int cell = Sampled[piece] % Grid.Cells;
                    placed[piece] = new PieceState { Tile = Def.Pieces[piece], Placed = true, Locked = true, I = (sbyte)(cell / Grid.Width), J = (sbyte)(cell % Grid.Width) };
                }
                return placed;
            }

            // The sampled set with the locked pieces first.
            int[] Ordered()
            {
                var set = new int[Sampled.Length];
                int n = 0;
                foreach (int piece in Locks) set[n++] = Sampled[piece];
                for (int k = 0; k < Sampled.Length; k++)
                {
                    if (!Locks.Contains(k)) set[n++] = Sampled[k];
                }
                return set;
            }

            public (int piece, int cell)[] Order() => SolutionCounter.WinningOrder(Def, Ordered(), Locks.Count);

            // The sampled placements still solve the level: each sits on an
            // active cell legally, together they cover every active cell,
            // every arm still does something (when the spec asks), and some
            // order with the locked pieces first wins through the real rules.
            public bool Valid()
            {
                var def = Def;
                var map = new LineMap(def);
                var covered = CellMask.None;
                foreach (int p in Sampled)
                {
                    int cell = p % Grid.Cells;
                    if (def.BoardAt(cell) != Cell.Active) return false;
                    var spread = map.Spread(def.Specs[p / Grid.Cells], cell);
                    if (spread.Forbidden) return false;
                    covered |= spread.Covered;
                }
                if (!covered.Contains(map.ActiveMask)) return false;
                if (Spec.RequireUsefulArms && !ArmsUseful(map, Sampled)) return false;
                return Order() != null;
            }

            public bool Unique(int cap)
            {
                var analysis = SolutionCounter.Analyse(Def, Placed(), cap);
                return !analysis.Capped && analysis.Solutions == 1;
            }

            // Every other solution: the covering sets the oracle reaches with
            // the locked pieces fixed, minus the sample, minus (on boards
            // with traps or switches) the sets no placement order wins.
            public List<int[]> Alternatives(int cap, out bool capped)
            {
                var def = Def;
                var sets = SolutionCounter.Sets(def, Placed(), cap, out capped);
                var result = new List<int[]>();
                if (capped) return result;
                var map = new LineMap(def);
                foreach (int[] set in sets)
                {
                    if (Key(def, set) == _key) continue;
                    if (map.HasDynamics && SolutionCounter.WinningOrder(def, set, Locks.Count) == null) continue;
                    result.Add(set);
                }
                return result;
            }

            // The counter's key for a set: (piece kind, cell) pairs, sorted.
            static string Key(LevelDef def, int[] set)
            {
                var kind = new int[def.Specs.Length];
                for (int k = 0; k < kind.Length; k++)
                {
                    kind[k] = k;
                    for (int p = 0; p < k; p++) if (def.Specs[p] == def.Specs[k]) { kind[k] = kind[p]; break; }
                }
                var key = new int[set.Length];
                for (int n = 0; n < set.Length; n++) key[n] = kind[set[n] / Grid.Cells] * Grid.Cells + set[n] % Grid.Cells;
                Array.Sort(key);
                return string.Join(",", key);
            }
        }
    }
}
