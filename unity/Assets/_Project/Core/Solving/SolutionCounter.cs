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

        public static Result Analyse(LevelDef def, int cap = DefaultCap)
        {
            var search = new Search(def, cap);
            search.Run();
            var result = new Result { Static = search.Sets.Count, Capped = search.HitCap };
            int min = 0;
            if (search.Map.HasDynamics && !search.HitCap)
            {
                foreach (int[] set in search.Sets.Values)
                {
                    if (WinningOrder(def, set) == null) continue;
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
        // through the real rules, or null. Without switches nothing is ever
        // un-infected, so the only constraint is that at most one placement
        // trips a trap and that one goes last (the win check runs before the
        // reset, RULES §4.1). With switches, orders are searched depth-first
        // so a prefix that already failed prunes every order behind it.
        public static (int piece, int cell)[] WinningOrder(LevelDef def, int[] set)
        {
            var order = new (int piece, int cell)[set.Length];
            for (int n = 0; n < set.Length; n++) order[n] = (set[n] / Grid.Cells, set[n] % Grid.Cells);

            var map = new LineMap(def);
            if (!map.HasSwitches)
            {
                // Non-trippers first (any order), then one tripper: the win
                // check runs before the reset, so the order wins iff the
                // board is complete by then. A second tripper never plays.
                int n = 0;
                for (int i = 0; i < order.Length; i++)
                {
                    if (!map.TripsTrap(def.Pieces[order[i].piece], order[i].cell)) order[n++] = (order[i].piece, order[i].cell);
                }
                var trippers = new List<(int piece, int cell)>();
                for (int i = 0; i < set.Length; i++)
                {
                    var p = (set[i] / Grid.Cells, set[i] % Grid.Cells);
                    if (map.TripsTrap(def.Pieces[p.Item1], p.Item2)) trippers.Add(p);
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
            return OrderSearch(def, order, chosen, used, 0) ? chosen : null;
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
                if (!Rules.CanPlace(s, piece, i, j)) return Outcome.Dead;
                Rules.SetPiece(s, piece, i, j);
                Rules.Resolve(s);
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
                if (!Rules.CanPlace(s, piece, i, j)) return false;
                Rules.SetPiece(s, piece, i, j);
                Rules.Resolve(s);
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
            public bool StopAtFirstFeasible;
            public (int piece, int cell)[] FirstFeasible;

            readonly LevelDef _def;
            readonly int _cap;
            readonly int _n;
            readonly int[] _cells;
            readonly CellMask[][] _cov;       // [piece][loc]
            readonly List<int>[] _opts;        // per loc: piece*Cells+loc options
            readonly int[] _chosen;
            bool _stop;

            public Search(LevelDef def, int cap)
            {
                _def = def;
                _cap = cap;
                Map = new LineMap(def);
                _n = def.Pieces.Length;
                var cells = new List<int>();
                for (int loc = 0; loc < Grid.Cells; loc++)
                {
                    if (def.BoardAt(loc) == Cell.Active) cells.Add(loc);
                }
                _cells = cells.ToArray();
                _cov = new CellMask[_n][];
                for (int k = 0; k < _n; k++)
                {
                    _cov[k] = new CellMask[Grid.Cells];
                    foreach (int loc in _cells) _cov[k][loc] = Map.Coverage(def.Pieces[k], loc);
                }
                _opts = new List<int>[Grid.Cells];
                foreach (int c in _cells)
                {
                    var o = new List<int>();
                    for (int k = 0; k < _n; k++)
                    {
                        foreach (int loc in _cells)
                        {
                            if (_cov[k][loc].Has(c)) o.Add(k * Grid.Cells + loc);
                        }
                    }
                    _opts[c] = o;
                }
                _chosen = new int[_n];
            }

            public void Run() => Rec(CellMask.None, 0, CellMask.None, 0);

            void Rec(CellMask covered, int used, CellMask occ, int depth)
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
                    int count = 0;
                    foreach (int opt in _opts[c])
                    {
                        int k = opt / Grid.Cells, loc = opt % Grid.Cells;
                        if ((used >> k & 1) != 0 || occ.Has(loc)) continue;
                        count++;
                    }
                    if (count < bestCount)
                    {
                        best = c;
                        bestCount = count;
                        if (count == 0) return;
                    }
                }

                foreach (int opt in _opts[best])
                {
                    int k = opt / Grid.Cells, loc = opt % Grid.Cells;
                    if ((used >> k & 1) != 0 || occ.Has(loc)) continue;
                    _chosen[depth] = opt;
                    Rec(covered | _cov[k][loc], used | 1 << k, occ | CellMask.Bit(loc), depth + 1);
                    if (_stop) return;
                }
            }

            void Found(int depth)
            {
                // Key: the set of (tile, cell) pairs, so duplicate tiles at
                // swapped cells count once (as the oracle's frozenset does).
                var key = new int[depth];
                for (int n = 0; n < depth; n++)
                {
                    int k = _chosen[n] / Grid.Cells, loc = _chosen[n] % Grid.Cells;
                    key[n] = (int)_def.Pieces[k] * Grid.Cells + loc;
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
