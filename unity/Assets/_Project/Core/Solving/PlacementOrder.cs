using System.Collections.Generic;

namespace GridInfect.Core.Solving
{
    // Order constraints on a solution.
    //
    // On a board with a repel switch or a reset trap the order the pieces go
    // down is part of the puzzle (RULES.md §4.1: the win check runs before
    // the reset and before the repels). A placement whose arm ends on a trap
    // resets the board unless it is the one that wins; a placement whose arm
    // ends on a switch repels infection back off the cells behind it unless
    // the win check gets there first. So a level can have a **last piece**:
    // a placement that only ever wins as the final one. Play it early and
    // the board cannot be finished — which the player fixes by lifting it,
    // and the Lock tool cannot, because a locked piece never comes up again.
    //
    // `LastOnly` names those placements (docs/last_piece_classic.json is the
    // oracle's answer for the 128 classic levels, tools/gen_last_piece_golden.py);
    // `KeepsWinnable` is the question the Lock tool actually asks before it
    // pins anything.
    public static class PlacementOrder
    {
        // How many covering sets the Lock tool's guard will look through
        // before it gives up on a candidate. It needs one that wins, not
        // all of them, so the search stops at the first.
        public const int GuardCap = 4000;

        public enum Outcome { Won, Alive, Dead }

        // Play `order` from a fresh board through the real rules. Dead =
        // an illegal placement, or a trap trip that swept the board.
        public static Outcome Play(LevelDef def, IReadOnlyList<(int piece, int cell)> order, int count = -1)
        {
            var s = new LevelSession(def);
            if (count < 0) count = order.Count;
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

        // Per placement of `set`: true when every order that wins ends on
        // it, so it is never safe to play while anything else is left. A
        // one-placement set is never constrained (it is the last piece by
        // definition), and a board with neither switch nor trap never is:
        // nothing there ever un-infects a cell.
        public static bool[] LastOnly(LevelDef def, (int piece, int cell)[] set)
        {
            var result = new bool[set.Length];
            if (set.Length < 2) return result;
            var map = new LineMap(def);
            if (!map.HasDynamics) return result;

            var winning = new bool[set.Length];
            var nonFinal = new bool[set.Length];
            Walk(def, set, new List<(int piece, int cell)>(), new bool[set.Length], winning, nonFinal);
            for (int n = 0; n < set.Length; n++) result[n] = winning[n] && !nonFinal[n];
            return result;
        }

        // The level's last piece, or null where it has none: the single
        // placement of `set` that only ever wins last.
        public static (int piece, int cell)? LastPiece(LevelDef def, (int piece, int cell)[] set)
        {
            bool[] lastOnly = LastOnly(def, set);
            (int piece, int cell)? found = null;
            for (int n = 0; n < lastOnly.Length; n++)
            {
                if (!lastOnly[n]) continue;
                if (found != null) return null;   // more than one: no single last piece
                found = set[n];
            }
            return found;
        }

        // Depth-first over the orders of `set`, recording which placements
        // can win and which can precede a win. True when an order below
        // `prefix` wins.
        static bool Walk(LevelDef def, (int piece, int cell)[] set, List<(int piece, int cell)> prefix,
                         bool[] used, bool[] winning, bool[] nonFinal)
        {
            bool anyWin = false;
            for (int n = 0; n < set.Length; n++)
            {
                if (used[n]) continue;
                prefix.Add(set[n]);
                var outcome = Play(def, prefix);
                if (outcome == Outcome.Won)
                {
                    winning[n] = true;
                    anyWin = true;
                }
                else if (outcome == Outcome.Alive)
                {
                    used[n] = true;
                    bool won = Walk(def, set, prefix, used, winning, nonFinal);
                    used[n] = false;
                    if (won)
                    {
                        nonFinal[n] = true;
                        anyWin = true;
                    }
                }
                prefix.RemoveAt(prefix.Count - 1);
            }
            return anyWin;
        }

        // Can this level still be won with exactly these pieces pinned?
        // The player's own pieces come and go, so only the locked ones
        // constrain the answer: a covering set that contains them and wins
        // for some order with them down first (SolutionCounter's order
        // check, the same one the generator accepts a level by).
        public static bool KeepsWinnable(LevelDef def, PieceState[] pinned, int cap = GuardCap)
        {
            return SolutionCounter.FirstSolution(def, pinned, cap) != null;
        }
    }
}
