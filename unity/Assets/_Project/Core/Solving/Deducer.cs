using System;
using System.Collections.Generic;

namespace GridInfect.Core.Solving
{
    // Rule tiers, in the order a player reaches for them. Definitions and
    // worked examples: docs/GENERATOR_V2.md §Solver.
    public enum Tier
    {
        LineOwnership = 1,   // one line, one placement left that can own it
        ArmExclusion = 2,    // a placement ruled out by what its arms would do
        PieceCounting = 3,   // the piece budget pins every piece to a job
        Contradiction1 = 4,  // one-step "suppose it went here" refutation
    }

    // A placement the solver derived, with the highest tier it needed to get
    // there and the cells the rule reasoned about.
    public readonly struct Deduction
    {
        public readonly Tier Tier;
        public readonly int Piece;
        public readonly int Cell;
        public readonly int[] Evidence;

        public Deduction(Tier tier, int piece, int cell, int[] evidence)
        {
            Tier = tier;
            Piece = piece;
            Cell = cell;
            Evidence = evidence ?? Array.Empty<int>();
        }
    }

    public sealed class SolveResult
    {
        // Every cell covered by deduction alone (no guess) and the final
        // assignment wins through the real rules. Implies a unique solution.
        public bool Solved;
        public Deduction[] Trace;
        public Tier MaxTier;
        public int[] TierCounts;       // rule firings per tier (tier 4: passes), index = (int)Tier; [0] unused
        public int Guesses;            // branch points the search fallback needed
        public bool Complete;          // a winning assignment exists (with or without guesses)
        public (int piece, int cell)[] Placements;   // the winning order when Complete
    }

    // The human-rules solver: line-based deductions to a fixpoint, one-step
    // contradiction when stuck, and a plain search fallback (counted as
    // guesses) so the answer is always found when one exists.
    public static class Deducer
    {
        public static SolveResult Solve(LevelDef def, PieceState[] placed = null)
        {
            var map = new LineMap(def);
            var state = new State(map);
            if (placed != null)
            {
                for (int k = 0; k < placed.Length && k < def.Pieces.Length; k++)
                {
                    if (!placed[k].Placed) continue;
                    state.Place(k, Grid.Loc(placed[k].I, placed[k].J));
                }
            }

            var result = new SolveResult { TierCounts = new int[5], Trace = Array.Empty<Deduction>() };
            var trace = new List<Deduction>();
            int guesses = 0;
            // Placements a forbidden cell rules out are read off the board
            // before anything else: one arm-exclusion round (stage 10).
            if (state.ForbiddenExclusions > 0) result.TierCounts[(int)Tier.ArmExclusion]++;
            var final = SolveFrom(state, trace, result.TierCounts, ref guesses);

            result.Guesses = guesses;
            result.Trace = trace.ToArray();
            foreach (var d in trace)
            {
                if (d.Tier > result.MaxTier) result.MaxTier = d.Tier;
            }
            if (final != null)
            {
                result.Complete = true;
                result.Placements = final.Order;
                result.Solved = guesses == 0;
            }
            return result;
        }

        // The candidate cells left per piece after tiers 1–3 run to a
        // fixpoint from `placed` (null when they contradict). Diagnostic and
        // hint surface: the Lock tool reads the next forced placement here.
        public static CellMask[] Candidates(LevelDef def, PieceState[] placed = null, List<Deduction> trace = null)
        {
            var state = new State(new LineMap(def));
            if (placed != null)
            {
                for (int k = 0; k < placed.Length && k < def.Pieces.Length; k++)
                {
                    if (placed[k].Placed) state.Place(k, Grid.Loc(placed[k].I, placed[k].J));
                }
            }
            var pending = Tier.LineOwnership;
            return Fixpoint(state, trace, new int[5], ref pending) ? state.Cand : null;
        }

        // Propagate to a fixpoint; on a dead end return null; when stuck,
        // branch on the cell with the fewest candidates (one guess per
        // branch point, whichever branch wins).
        static State SolveFrom(State s, List<Deduction> trace, int[] tierCounts, ref int guesses)
        {
            if (!Propagate(s, trace, tierCounts)) return null;
            if (s.Done)
            {
                // A static cover that no placement order wins is a dead end
                // (switch repels or a second trap trip undo it).
                s.Order = SolutionCounter.WinningOrder(s.Map.Def, s.Assignment());
                return s.Order != null ? s : null;
            }

            int cell = s.FewestCandidatesCell();
            if (cell < 0) return null;
            guesses++;
            int traceMark = trace.Count;
            foreach (int opt in s.CandidatesFor(cell, distinctTiles: true))
            {
                var branch = s.Clone();
                branch.Place(opt / Grid.Cells, opt % Grid.Cells);
                trace.Add(new Deduction(Tier.Contradiction1, opt / Grid.Cells, opt % Grid.Cells, s.Map.LinesThrough(cell)));
                var solved = SolveFrom(branch, trace, tierCounts, ref guesses);
                if (solved != null) return solved;
                trace.RemoveRange(traceMark, trace.Count - traceMark);
            }
            return null;
        }

        enum Step { Nothing, Placed, Contradiction }

        // Tiers 1–3 to a fixpoint, then tier 4 once, repeat. False on contradiction.
        static bool Propagate(State s, List<Deduction> trace, int[] tierCounts)
        {
            var pending = s.ForbiddenExclusions > 0 && s.Remaining == s.N ? Tier.ArmExclusion : Tier.LineOwnership;
            while (!s.Done)
            {
                if (!Fixpoint(s, trace, tierCounts, ref pending)) return false;
                if (s.Done) return true;
                if (Contradict(s, tierCounts, ref pending)) continue;
                return true; // stuck, no contradiction
            }
            return true;
        }

        // Tiers 1–3 only. False on contradiction; true when done or stuck.
        static bool Fixpoint(State s, List<Deduction> trace, int[] tierCounts, ref Tier pending)
        {
            while (!s.Done)
            {
                s.PruneLocal(tierCounts, ref pending);
                var forced = Force(s, trace, tierCounts, ref pending);
                if (forced == Step.Contradiction) return false;
                if (forced == Step.Placed) continue;
                if (s.Count(tierCounts, ref pending) == State.Outcome.Contradiction) return false;
                if (s.CountChanged) { s.CountChanged = false; continue; }
                return true;
            }
            return true;
        }

        // Tier 1: an uncovered cell whose candidates are all the same tile on
        // the same cell. The candidates of a cell are exactly the placements
        // in its two lines (on it, or an arm along a line), so this is line
        // ownership: only one placement can still own the line through it.
        // Identical tiles are interchangeable, so the lowest unused one goes.
        static Step Force(State s, List<Deduction> trace, int[] tierCounts, ref Tier pending)
        {
            var rem = s.Map.ActiveMask & ~s.Covered;
            for (int c = 0; c < Grid.Cells; c++)
            {
                if (!rem.Has(c)) continue;
                int only = -1;
                bool many = false;
                for (int k = 0; k < s.N && !many; k++)
                {
                    if (s.Used[k]) continue;
                    for (int loc = 0; loc < Grid.Cells; loc++)
                    {
                        if (!s.Cand[k].Has(loc) || !s.Cov[k][loc].Has(c)) continue;
                        int key = s.Kind[k] * Grid.Cells + loc;
                        if (only < 0) only = key;
                        else if (only != key) { many = true; break; }
                    }
                }
                if (many) continue;
                if (only < 0) return Step.Contradiction;

                tierCounts[(int)Tier.LineOwnership]++;
                var tier = pending > Tier.LineOwnership ? pending : Tier.LineOwnership;
                pending = Tier.LineOwnership;
                int kind = only / Grid.Cells;
                int cell = only % Grid.Cells;
                int piece = s.FirstUnused(kind, cell);
                trace?.Add(new Deduction(tier, piece, cell, s.Map.LinesThrough(c)));
                s.Place(piece, cell);
                return Step.Placed;
            }
            return Step.Nothing;
        }

        // Tier 4: suppose candidate p; if tiers 1–3 then contradict, drop p.
        // One pass over every candidate counts as one firing (a player
        // refutes the few that matter, not every cell on the board).
        static bool Contradict(State s, int[] tierCounts, ref Tier pending)
        {
            bool any = false;
            var scratch = new int[5];
            for (int k = 0; k < s.N; k++)
            {
                if (s.Used[k]) continue;
                for (int loc = 0; loc < Grid.Cells; loc++)
                {
                    if (!s.Cand[k].Has(loc)) continue;
                    var trial = s.Clone();
                    trial.Place(k, loc);
                    var scratchTier = Tier.LineOwnership;
                    if (!Fixpoint(trial, null, scratch, ref scratchTier)
                        || (trial.Done && SolutionCounter.WinningOrder(s.Map.Def, trial.Assignment()) == null))
                    {
                        s.Cand[k] = s.Cand[k] & ~CellMask.Bit(loc);
                        if (pending < Tier.Contradiction1) pending = Tier.Contradiction1;
                        any = true;
                    }
                }
            }
            if (any) tierCounts[(int)Tier.Contradiction1]++;
            return any;
        }

        // Solver state: what is placed, what is covered, which candidate
        // placements survive (one cell mask per piece).
        sealed class State
        {
            public readonly LineMap Map;
            public readonly int N;
            public readonly PieceSpec[] Specs;
            public readonly int[] Kind;        // per piece: index of the first piece with an equal spec
            public readonly CellMask[][] Cov;  // [piece][loc] static coverage
            public readonly bool[][] Trips;    // [piece][loc] arm ends on a trap
            public CellMask[] Cand;            // per piece: cells it may still go to
            public bool[] Used;
            public int[] At;
            public CellMask Covered;
            public CellMask Occupied;
            public int Remaining;
            public bool TripperPlaced;
            public bool CountChanged;
            public int ForbiddenExclusions;         // placements a forbidden cell rules out
            public (int piece, int cell)[] Order;   // set once Done and order-feasible

            public enum Outcome { Nothing, Pruned, Contradiction }

            public bool Done => Covered.Contains(Map.ActiveMask);

            public State(LineMap map)
            {
                Map = map;
                N = map.Def.Pieces.Length;
                Specs = map.Def.Specs;
                Kind = new int[N];
                for (int k = 0; k < N; k++)
                {
                    Kind[k] = k;
                    for (int p = 0; p < k; p++) if (Specs[p] == Specs[k]) { Kind[k] = Kind[p]; break; }
                }
                Cov = new CellMask[N][];
                Trips = new bool[N][];
                Cand = new CellMask[N];
                Used = new bool[N];
                At = new int[N];
                Remaining = N;
                for (int k = 0; k < N; k++)
                {
                    Cov[k] = new CellMask[Grid.Cells];
                    Trips[k] = new bool[Grid.Cells];
                    At[k] = -1;
                    for (int loc = 0; loc < Grid.Cells; loc++)
                    {
                        if (map.Def.BoardAt(loc) != Cell.Active) continue;
                        var spread = map.Spread(Specs[k], loc);
                        if (spread.Forbidden) { ForbiddenExclusions++; continue; }   // not a legal placement (stage 10)
                        Cov[k][loc] = spread.Covered;
                        Trips[k][loc] = spread.Trips;
                        Cand[k] |= CellMask.Bit(loc);
                    }
                }
            }

            State(State o)
            {
                Map = o.Map;
                N = o.N;
                Specs = o.Specs;
                Kind = o.Kind;
                Cov = o.Cov;
                Trips = o.Trips;
                Cand = (CellMask[])o.Cand.Clone();
                Used = (bool[])o.Used.Clone();
                At = (int[])o.At.Clone();
                Covered = o.Covered;
                Occupied = o.Occupied;
                Remaining = o.Remaining;
                TripperPlaced = o.TripperPlaced;
                ForbiddenExclusions = o.ForbiddenExclusions;
            }

            public State Clone() => new State(this);

            public void Place(int k, int loc)
            {
                if (Used[k]) return;
                Used[k] = true;
                At[k] = loc;
                Remaining--;
                Covered |= Cov[k][loc];
                Occupied |= CellMask.Bit(loc);
                if (Trips[k][loc]) TripperPlaced = true;
                Cand[k] = CellMask.None;
                var free = ~CellMask.Bit(loc);
                for (int p = 0; p < N; p++) Cand[p] = Cand[p] & free;
            }

            public int FirstUnused(int kind, int loc)
            {
                for (int k = 0; k < N; k++)
                {
                    if (!Used[k] && Kind[k] == kind && Cand[k].Has(loc)) return k;
                }
                throw new InvalidOperationException("no unused piece for a forced placement");
            }

            public int[] Assignment()
            {
                var list = new List<int>();
                for (int k = 0; k < N; k++)
                {
                    if (Used[k]) list.Add(k * Grid.Cells + At[k]);
                }
                return list.ToArray();
            }

            // Bookkeeping (silent): a candidate that covers nothing still
            // uncovered is no candidate. Tier 2: a placement whose arm would
            // trip a trap is out once a trap trip is spoken for (only the
            // winning placement may trip one).
            public void PruneLocal(int[] tierCounts, ref Tier pending)
            {
                var rem = Map.ActiveMask & ~Covered;
                if (rem.IsEmpty) return;
                bool armRule = false;
                for (int k = 0; k < N; k++)
                {
                    if (Used[k]) continue;
                    for (int loc = 0; loc < Grid.Cells; loc++)
                    {
                        if (!Cand[k].Has(loc)) continue;
                        if (!Cov[k][loc].Intersects(rem))
                        {
                            Cand[k] = Cand[k] & ~CellMask.Bit(loc);
                        }
                        else if (TripperPlaced && Trips[k][loc])
                        {
                            Cand[k] = Cand[k] & ~CellMask.Bit(loc);
                            armRule = true;
                        }
                    }
                }
                // A cell only trap-facing arms can reach commits the one
                // allowed trip to it: trap-facing placements elsewhere go.
                if (!TripperPlaced)
                {
                    for (int c = 0; c < Grid.Cells && !TripperPlaced; c++)
                    {
                        if (!rem.Has(c)) continue;
                        bool allTrip = true, anyCand = false;
                        for (int k = 0; k < N && allTrip; k++)
                        {
                            if (Used[k]) continue;
                            for (int loc = 0; loc < Grid.Cells; loc++)
                            {
                                if (!Cand[k].Has(loc) || !Cov[k][loc].Has(c)) continue;
                                anyCand = true;
                                if (!Trips[k][loc]) { allTrip = false; break; }
                            }
                        }
                        if (!anyCand || !allTrip) continue;
                        for (int k = 0; k < N; k++)
                        {
                            if (Used[k]) continue;
                            for (int loc = 0; loc < Grid.Cells; loc++)
                            {
                                if (Cand[k].Has(loc) && Trips[k][loc] && !Cov[k][loc].Has(c))
                                {
                                    Cand[k] = Cand[k] & ~CellMask.Bit(loc);
                                    armRule = true;
                                }
                            }
                        }
                    }
                }
                if (armRule)
                {
                    tierCounts[(int)Tier.ArmExclusion]++;
                    if (pending < Tier.ArmExclusion) pending = Tier.ArmExclusion;
                }
            }

            // Tier 3: piece counting. A set of uncovered cells no single
            // candidate can cover two of needs one piece each, so its size
            // bounds the pieces still needed. Larger than the pieces left is
            // a contradiction; a placement after which the rest still needs
            // more than the pieces left cannot be part of the solution.
            public Outcome Count(int[] tierCounts, ref Tier pending)
            {
                var rem = Map.ActiveMask & ~Covered;
                if (rem.IsEmpty) return Outcome.Nothing;
                ComputeReach(rem);
                if (Needed(rem) > Remaining)
                {
                    tierCounts[(int)Tier.PieceCounting]++;
                    if (pending < Tier.PieceCounting) pending = Tier.PieceCounting;
                    return Outcome.Contradiction;
                }
                bool any = false;
                for (int k = 0; k < N; k++)
                {
                    if (Used[k]) continue;
                    for (int loc = 0; loc < Grid.Cells; loc++)
                    {
                        if (!Cand[k].Has(loc)) continue;
                        if (Needed(rem & ~Cov[k][loc], k) > Remaining - 1)
                        {
                            Cand[k] = Cand[k] & ~CellMask.Bit(loc);
                            any = true;
                        }
                    }
                }
                if (any)
                {
                    tierCounts[(int)Tier.PieceCounting]++;
                    if (pending < Tier.PieceCounting) pending = Tier.PieceCounting;
                    CountChanged = true;
                    return Outcome.Pruned;
                }
                return Outcome.Nothing;
            }

            // ReachBy[k][c]: every cell piece k could cover together with c;
            // Reach[c] is the union over pieces, ReachWithout[k][c] the union
            // over the other pieces. Two cells are independent when no
            // candidate reaches from one to the other.
            readonly CellMask[][] _reachBy = new CellMask[LevelDef.MaxPieces][];
            readonly CellMask[] _reach = new CellMask[Grid.Cells];
            readonly int[] _byCount = new int[Grid.Cells];
            int _byCountLength;
            int _reachWithout = -1;
            readonly CellMask[] _reachScratch = new CellMask[Grid.Cells];

            void ComputeReach(CellMask rem)
            {
                var counts = new int[Grid.Cells];
                for (int c = 0; c < Grid.Cells; c++) _reach[c] = CellMask.None;
                for (int k = 0; k < N; k++)
                {
                    if (_reachBy[k] == null) _reachBy[k] = new CellMask[Grid.Cells];
                    for (int c = 0; c < Grid.Cells; c++) _reachBy[k][c] = CellMask.None;
                    if (Used[k]) continue;
                    for (int loc = 0; loc < Grid.Cells; loc++)
                    {
                        if (!Cand[k].Has(loc)) continue;
                        var cov = Cov[k][loc] & rem;
                        for (int c = 0; c < Grid.Cells; c++)
                        {
                            if (cov.Has(c)) { _reachBy[k][c] |= cov; counts[c]++; }
                        }
                    }
                    for (int c = 0; c < Grid.Cells; c++) _reach[c] |= _reachBy[k][c];
                }
                int n = 0;
                for (int c = 0; c < Grid.Cells; c++)
                {
                    if (rem.Has(c)) _byCount[n++] = c;
                }
                Array.Sort(_byCount, 0, n, Comparer<int>.Create((a, b) =>
                    counts[a] != counts[b] ? counts[a].CompareTo(counts[b]) : a.CompareTo(b)));
                _byCountLength = n;
                _reachWithout = -1;
            }

            // Adjacency with piece `without` gone (-1: everyone).
            CellMask[] ReachWithout(int without)
            {
                if (without < 0) return _reach;
                if (_reachWithout != without)
                {
                    for (int c = 0; c < Grid.Cells; c++)
                    {
                        var m = CellMask.None;
                        for (int k = 0; k < N; k++)
                        {
                            if (k != without && !Used[k]) m |= _reachBy[k][c];
                        }
                        _reachScratch[c] = m;
                    }
                    _reachWithout = without;
                }
                return _reachScratch;
            }

            // Greedy independent set size over `rem`, best of three orders:
            // fewest candidates first, top-down, bottom-up.
            int Needed(CellMask rem, int without = -1)
            {
                var reach = ReachWithout(without);
                int best = 0;
                for (int pass = 0; pass < 3; pass++)
                {
                    var set = CellMask.None;
                    int size = 0;
                    for (int idx = 0; idx < (pass == 0 ? _byCountLength : Grid.Cells); idx++)
                    {
                        int c = pass == 0 ? _byCount[idx] : pass == 1 ? idx : Grid.Cells - 1 - idx;
                        if (!rem.Has(c) || reach[c].Intersects(set)) continue;
                        set |= CellMask.Bit(c);
                        size++;
                    }
                    if (size > best) best = size;
                }
                return best;
            }

            public int FewestCandidatesCell()
            {
                var rem = Map.ActiveMask & ~Covered;
                int best = -1, bestCount = int.MaxValue;
                for (int c = 0; c < Grid.Cells; c++)
                {
                    if (!rem.Has(c)) continue;
                    int count = CandidatesFor(c, distinctTiles: true).Count;
                    if (count < bestCount) { best = c; bestCount = count; }
                }
                return bestCount == 0 ? -1 : best;
            }

            public List<int> CandidatesFor(int c, bool distinctTiles)
            {
                var list = new List<int>();
                var seen = new HashSet<int>();
                for (int k = 0; k < N; k++)
                {
                    if (Used[k]) continue;
                    for (int loc = 0; loc < Grid.Cells; loc++)
                    {
                        if (!Cand[k].Has(loc) || !Cov[k][loc].Has(c)) continue;
                        if (distinctTiles && !seen.Add(Kind[k] * Grid.Cells + loc)) continue;
                        list.Add(k * Grid.Cells + loc);
                    }
                }
                return list;
            }
        }
    }
}
