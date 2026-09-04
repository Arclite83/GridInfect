using System;
using System.Collections.Generic;

namespace GridInfect.Core.Solving
{
    // Exact solution count, a C# mirror of tools/level_metrics.py `solve`:
    // enumerate covering sets by most-constrained-cell first (lowest cell on
    // ties, options filtered by unused piece and unoccupied cell), dedupe by
    // the set of (tile, cell) pairs, then — on boards with switches or traps —
    // keep only sets that win for at least one placement order through the
    // real rules. The search order is part of the contract: the count is the
    // number of sets *this* search reaches, and the golden file
    // docs/level_metrics_classic.json pins it for the 128 classic levels.
    public static class SolutionCounter
    {
        public const int DefaultCap = 100000;

        public sealed class Result
        {
            public int Solutions;   // order-feasible covering sets
            public int Static;      // covering sets before the order check
            public int MinPieces;   // fewest pieces in any feasible set, 0 when none
            public bool Capped;     // hit the cap; Solutions is then a lower bound
        }

        public static int Count(LevelDef def, int cap = DefaultCap) => Analyse(def, cap).Solutions;

        public static Result Analyse(LevelDef def, int cap = DefaultCap) => Analyse(def, null, cap);

        // Count with some pieces already fixed on the board (the Lock tool's
        // safety check): fixed placements are part of every set.
        public static int Count(LevelDef def, PieceState[] placed, int cap = DefaultCap) => Analyse(def, placed, cap).Solutions;

        public static Result Analyse(LevelDef def, PieceState[] placed, int cap)
        {
            var search = new Search(def, cap);
            int fixedCount = Fix(search, def, placed);
            search.Run();
            var result = new Result { Static = search.Sets.Count, Capped = search.HitCap };
            int min = 0;
            if (search.Map.HasDynamics && !search.HitCap)
            {
                foreach (int[] set in search.Sets.Values)
                {
                    if (WinningOrder(def, set, fixedCount) == null) continue;
                    result.Solutions++;
                    if (min == 0 || set.Length < min) min = set.Length;
                }
            }
            else
            {
                result.Solutions = search.Sets.Count;
                foreach (int[] set in search.Sets.Values)
                {
                    if (min == 0 || set.Length < min) min = set.Length;
                }
            }
            result.MinPieces = min;
            return result;
        }

        // Every covering set the search reaches (feasibility not checked),
        // each as piece*Cells+cell placements, pre-placed pieces first. For
        // the constructor's discriminator.
        public static List<int[]> Sets(LevelDef def, int cap, out bool capped) => Sets(def, null, cap, out capped);

        public static List<int[]> Sets(LevelDef def, PieceState[] placed, int cap, out bool capped)
        {
            var search = new Search(def, cap);
            Fix(search, def, placed);
            search.Run();
            capped = search.HitCap;
            return new List<int[]>(search.Sets.Values);
        }

        static int Fix(Search search, LevelDef def, PieceState[] placed)
        {
            int count = 0;
            if (placed == null) return 0;
            for (int k = 0; k < placed.Length && k < def.Pieces.Length; k++)
            {
                if (!placed[k].Placed) continue;
                search.Fix(k, Grid.Loc(placed[k].I, placed[k].J));
                count++;
            }
            return count;
        }

        // A cheaper count for ranking (the generator's wall pruner): the
        // same search, but an option tried at a branch point is excluded from
        // its later siblings, so each cover is reached once instead of once
        // per order. Not the oracle's number (some non-minimal covers the
        // oracle reaches are skipped), so never the final verdict.
        public static int CountFast(LevelDef def, int cap)
        {
            var search = new Search(def, cap) { Distinct = true };
            search.Run();
            return search.HitCap ? cap : search.Sets.Count;
        }

        // The first feasible covering set in search order, as (piece, cell)
        // placements in an order that wins through the real rules. Null when
        // the level has no solution.
        public static (int piece, int cell)[] FirstSolution(LevelDef def)
        {
            var search = new Search(def, int.MaxValue) { StopAtFirstFeasible = true };
            search.Run();
            return search.FirstFeasible;
        }

        // A placement order for `set` (encoded piece*Cells+cell) that wins
        // through the real rules, or null. The first `fixedPrefix` entries
        // are pre-placed (locked at load) and stay first. Without switches
        // nothing is ever un-infected, so the only constraint is that at
        // most one placement trips a trap and that one goes last (the win
        // check runs before the reset, RULES §4.1). With switches, orders
        // are searched depth-first so a prefix that already failed prunes
        // every order behind it.
        public static (int piece, int cell)[] WinningOrder(LevelDef def, int[] set, int fixedPrefix = 0)
        {
            var order = new (int piece, int cell)[set.Length];
            for (int n = 0; n < set.Length; n++) order[n] = (set[n] / Grid.Cells, set[n] % Grid.Cells);

            var map = new LineMap(def);
            if (!map.HasSwitches)
            {
                // Non-trippers first (any order), then one tripper: the win
                // check runs before the reset, so the order wins iff the
                // board is complete by then. A second tripper never plays.
                // A pre-placed tripper would reset the board at load.
                int n = 0;
                for (int i = 0; i < order.Length; i++)
                {
                    bool trips = map.TripsTrap(def.Specs[order[i].piece], order[i].cell);
                    if (i < fixedPrefix && trips) return null;
                    if (!trips) order[n++] = (order[i].piece, order[i].cell);
                }
                var trippers = new List<(int piece, int cell)>();
                for (int i = 0; i < set.Length; i++)
                {
                    var p = (set[i] / Grid.Cells, set[i] % Grid.Cells);
                    if (map.TripsTrap(def.Specs[p.Item1], p.Item2)) trippers.Add(p);
                }
                for (int t = 0; t < Math.Max(1, trippers.Count); t++)
                {
                    int at = n;
                    for (int i = 0; i < trippers.Count; i++)
                    {
                        order[at++] = trippers[(t + i) % trippers.Count];
                    }
                    if (Wins(def, order)) return order;
                    if (trippers.Count == 0) break;
                }
                return null;
            }
            var chosen = new (int piece, int cell)[set.Length];
            var used = new bool[set.Length];
            for (int i = 0; i < fixedPrefix; i++)
            {
                chosen[i] = order[i];
                used[i] = true;
            }
            if (fixedPrefix > 0)
            {
                var outcome = Replay(def, chosen, fixedPrefix);
                if (outcome == Outcome.Dead) return null;
                if (outcome == Outcome.Won) return fixedPrefix == set.Length ? chosen : null;
                if (fixedPrefix == set.Length) return null;
            }
            return OrderSearch(def, order, chosen, used, fixedPrefix) ? chosen : null;
        }

        static bool OrderSearch(LevelDef def, (int piece, int cell)[] all, (int piece, int cell)[] chosen, bool[] used, int depth)
        {
            for (int n = 0; n < all.Length; n++)
            {
                if (used[n]) continue;
                chosen[depth] = all[n];
                var outcome = Replay(def, chosen, depth + 1);
                if (outcome == Outcome.Won) return true;
                if (outcome == Outcome.Alive && depth + 1 < all.Length)
                {
                    used[n] = true;
                    if (OrderSearch(def, all, chosen, used, depth + 1)) return true;
                    used[n] = false;
                }
            }
            return false;
        }

        enum Outcome { Won, Alive, Dead }

        static Outcome Replay(LevelDef def, (int piece, int cell)[] order, int count)
        {
            var s = new LevelSession(def);
            for (int n = 0; n < count; n++)
            {
                var (piece, cell) = order[n];
                int i = cell / Grid.Width, j = cell % Grid.Width;
                if (!s.Rules.CanPlace(s, piece, i, j)) return Outcome.Dead;
                s.Rules.SetPiece(s, piece, i, j);
                s.Rules.Resolve(s);
                if (s.Solved) return Outcome.Won;
                bool any = false;
                for (int k = 0; k < s.Pieces.Length; k++) any |= s.Pieces[k].Placed;
                if (!any) return Outcome.Dead;
            }
            return Outcome.Alive;
        }

        // Mirrors the oracle's order check: illegal placement or a full reset
        // (no piece left placed) fails the order; a win anywhere passes it.
        public static bool Wins(LevelDef def, (int piece, int cell)[] order)
        {
            var s = new LevelSession(def);
            foreach (var (piece, cell) in order)
            {
                int i = cell / Grid.Width, j = cell % Grid.Width;
                if (!s.Rules.CanPlace(s, piece, i, j)) return false;
                s.Rules.SetPiece(s, piece, i, j);
                s.Rules.Resolve(s);
                if (s.Solved) return true;
                bool any = false;
                for (int k = 0; k < s.Pieces.Length; k++) any |= s.Pieces[k].Placed;
                if (!any) return false;
            }
            return s.Solved;
        }

        sealed class Search
        {
            public readonly LineMap Map;
            public readonly Dictionary<string, int[]> Sets = new Dictionary<string, int[]>(StringComparer.Ordinal);
            public bool HitCap;
            public bool Distinct;
            public bool StopAtFirstFeasible;
            public (int piece, int cell)[] FirstFeasible;

            readonly LevelDef _def;
            readonly int _cap;
            readonly int _n;
            readonly int[] _specId;            // per piece: index of the first piece with an equal spec
            readonly CellMask[] _cov;          // [piece*Cells+loc]
            readonly int[][] _covCells;        // [option] -> cells it covers
            readonly bool[] _removed;          // option no longer available
            readonly int[] _avail;             // per cell: available options covering it
            readonly int[][] _optsByCell;      // per cell: options covering it (k-major)
            readonly int[] _chosen;
            readonly int[] _undo;              // stack of removed options
            int _undoTop;
            bool _stop;

            public Search(LevelDef def, int cap)
            {
                _def = def;
                _cap = cap;
                Map = new LineMap(def);
                _n = def.Pieces.Length;
                _specId = new int[_n];
                for (int k = 0; k < _n; k++)
                {
                    _specId[k] = k;
                    for (int p = 0; p < k; p++) if (def.Specs[p] == def.Specs[k]) { _specId[k] = _specId[p]; break; }
                }
                int options = _n * Grid.Cells;
                _cov = new CellMask[options];
                _covCells = new int[options][];
                _removed = new bool[options];
                _avail = new int[Grid.Cells];
                var byCell = new List<int>[Grid.Cells];
                for (int c = 0; c < Grid.Cells; c++) byCell[c] = new List<int>();
                for (int k = 0; k < _n; k++)
                {
                    for (int loc = 0; loc < Grid.Cells; loc++)
                    {
                        int opt = k * Grid.Cells + loc;
                        if (def.BoardAt(loc) != Cell.Active || Map.IsIllegal(def.Specs[k], loc))
                        {
                            _removed[opt] = true;
                            _covCells[opt] = Array.Empty<int>();
                            continue;
                        }
                        var cov = Map.Coverage(def.Specs[k], loc);
                        _cov[opt] = cov;
                        var cells = new List<int>();
                        for (int c = 0; c < Grid.Cells; c++)
                        {
                            if (!cov.Has(c)) continue;
                            cells.Add(c);
                            _avail[c]++;
                            byCell[c].Add(opt);
                        }
                        _covCells[opt] = cells.ToArray();
                    }
                }
                _optsByCell = new int[Grid.Cells][];
                for (int c = 0; c < Grid.Cells; c++) _optsByCell[c] = byCell[c].ToArray();
                _chosen = new int[_n];
                _undo = new int[options * (_n + 1)];
            }

            CellMask _fixedCovered;
            int _fixedDepth;

            // Pre-place a piece: it is in every set the search reaches.
            public void Fix(int k, int loc)
            {
                int opt = k * Grid.Cells + loc;
                _chosen[_fixedDepth++] = opt;
                _fixedCovered |= _cov[opt];
                for (int l = 0; l < Grid.Cells; l++) Remove(k * Grid.Cells + l);
                for (int p = 0; p < _n; p++) Remove(p * Grid.Cells + loc);
            }

            public void Run() => Rec(_fixedCovered, _fixedDepth);

            // The oracle's recursion: most-constrained uncovered cell first
            // (lowest cell on ties, dead end on zero), options in piece-major
            // order. Option availability is kept incrementally: placing (k,
            // loc) removes every option of piece k and every option on loc.
            void Rec(CellMask covered, int depth)
            {
                if (_stop) return;
                if (Sets.Count >= _cap) { HitCap = true; return; }
                if (covered.Contains(Map.ActiveMask))
                {
                    Found(depth);
                    return;
                }

                int best = -1, bestCount = int.MaxValue;
                var rem = Map.ActiveMask & ~covered;
                for (int c = 0; c < Grid.Cells; c++)
                {
                    if (!rem.Has(c)) continue;
                    if (_avail[c] < bestCount)
                    {
                        best = c;
                        bestCount = _avail[c];
                        if (bestCount == 0) return;
                    }
                }

                int outer = _undoTop;
                foreach (int opt in _optsByCell[best])
                {
                    if (_removed[opt]) continue;
                    int k = opt / Grid.Cells, loc = opt % Grid.Cells;
                    _chosen[depth] = opt;
                    int mark = _undoTop;
                    for (int l = 0; l < Grid.Cells; l++) Remove(k * Grid.Cells + l);
                    for (int p = 0; p < _n; p++) Remove(p * Grid.Cells + loc);
                    Rec(covered | _cov[opt], depth + 1);
                    Undo(mark);
                    if (_stop) return;
                    if (Distinct) Remove(opt);   // later siblings never re-find a cover with this option
                }
                Undo(outer);
            }

            void Undo(int mark)
            {
                while (_undoTop > mark)
                {
                    int o = _undo[--_undoTop];
                    _removed[o] = false;
                    foreach (int c in _covCells[o]) _avail[c]++;
                }
            }

            void Remove(int opt)
            {
                if (_removed[opt]) return;
                _removed[opt] = true;
                foreach (int c in _covCells[opt]) _avail[c]--;
                _undo[_undoTop++] = opt;
            }

            void Found(int depth)
            {
                // Key: the set of (piece kind, cell) pairs, so identical pieces
                // at swapped cells count once (as the oracle's frozenset does).
                var key = new int[depth];
                for (int n = 0; n < depth; n++)
                {
                    int k = _chosen[n] / Grid.Cells, loc = _chosen[n] % Grid.Cells;
                    key[n] = _specId[k] * Grid.Cells + loc;
                }
                Array.Sort(key);
                string text = string.Join(",", key);
                if (Sets.ContainsKey(text)) return;

                var set = new int[depth];
                Array.Copy(_chosen, set, depth);
                Sets.Add(text, set);

                if (StopAtFirstFeasible)
                {
                    var order = WinningOrder(_def, set);
                    if (order != null)
                    {
                        FirstFeasible = order;
                        _stop = true;
                    }
                }
            }
        }
    }
}
