using System.Collections.Generic;
using Bloodhound.Engine;
using GridInfect.Core.Solving;

namespace GridInfect.Core
{
    // piece.lock { }: spend one lock to place one piece at its solution cell
    // and lock it there (NEXT_PASS "Lock"). Which piece: the deducer's next
    // forced placement given the player's currently correct pieces; failing
    // that, the unplaced piece with the largest coverage in the stored
    // solution. A player piece on the target cell goes back to the tray.
    public sealed class LockPieceAction : GameAction<GameState>
    {
        public override string Name => "piece.lock";

        public override string Validate(GameState state, ActionInput input)
        {
            var s = state.Session;
            if (s == null) return "no level loaded";
            if (s.ResolutionPending) return "resolution pending — dispatch board.resolve first";
            if (s.Solved) return "level already solved";
            if (state.Profile.Locks <= 0) return "no locks left";
            if (state.Solution == null) return "no stored solution for this level";
            if (Lock.ChooseTarget(state) == null) return "nothing left to lock";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            var s = state.Session;
            var target = Lock.ChooseTarget(state).Value;
            int i = target.cell / Grid.Width, j = target.cell % Grid.Width;

            // Evict whoever sits on the target cell, then the target piece
            // itself if the player has it somewhere wrong.
            for (int k = 0; k < s.Pieces.Length; k++)
            {
                if (s.Pieces[k].Placed && !s.Pieces[k].Locked && s.Pieces[k].I == i && s.Pieces[k].J == j)
                {
                    s.Rules.ClearPiece(s, k);
                }
            }
            if (s.Pieces[target.piece].Placed) s.Rules.ClearPiece(s, target.piece);

            s.Rules.SetPiece(s, target.piece, i, j);
            s.Pieces[target.piece].Locked = true;
            state.Profile.Locks--;
            state.Profile.Dirty = true;
        }
    }

    // locks.grant { amount, reason }: "rewarded" (an ad) is uncapped; any
    // other reason ("streak") tops the wallet up to the cap at most.
    public sealed class GrantLocksAction : GameAction<GameState>
    {
        public const string Rewarded = "rewarded";

        public override string Name => "locks.grant";

        public override string Validate(GameState state, ActionInput input)
        {
            int amount = input.Int("amount");
            if (amount < 1 || amount > 100) return $"amount {amount} out of range";
            if (string.IsNullOrEmpty(input.Str("reason"))) return "reason required";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            int amount = input.Int("amount");
            string reason = input.Str("reason");
            var profile = state.Profile;
            int before = profile.Locks;
            if (reason == Rewarded)
            {
                profile.Locks += amount;
            }
            else
            {
                int capped = System.Math.Min(Profile.LocksCap, profile.Locks + amount);
                if (capped > profile.Locks) profile.Locks = capped;
            }
            if (profile.Locks != before) profile.Dirty = true;
        }
    }

    public static class Lock
    {
        // Correct = a placed piece whose (tile, cell) matches a stored
        // solution entry not already claimed by another placed piece.
        public static PieceState[] CorrectPieces(GameState state)
        {
            var s = state.Session;
            var result = new PieceState[s.Pieces.Length];
            var claimed = new bool[state.Solution.Length];
            for (int k = 0; k < s.Pieces.Length; k++)
            {
                result[k] = new PieceState { Tile = s.Pieces[k].Tile, Placed = false, I = -1, J = -1 };
                if (!s.Pieces[k].Placed) continue;
                int cell = Grid.Loc(s.Pieces[k].I, s.Pieces[k].J);
                for (int n = 0; n < state.Solution.Length; n++)
                {
                    var (piece, solCell) = state.Solution[n];
                    if (claimed[n] || solCell != cell || s.Def.Specs[piece] != s.Def.Specs[k]) continue;
                    claimed[n] = true;
                    result[k] = s.Pieces[k];
                    break;
                }
            }
            return result;
        }

        // The next placement to lock: (piece index, cell), or null when
        // every solution cell is already correctly held.
        public static (int piece, int cell)? ChooseTarget(GameState state)
        {
            var s = state.Session;
            var correct = CorrectPieces(state);
            var map = new LineMap(s.Def);

            // 1. The deducer's next forced placement from the correct pieces,
            //    preferring one whose arms trip no trap (a locked tripper
            //    resets the board every time a later placement trips).
            var solve = Deducer.Solve(s.Def, correct);
            if (solve.Solved && solve.Trace.Length > 0)
            {
                var candidates = new List<(int piece, int cell)>();
                foreach (var d in solve.Trace) candidates.Add((d.Piece, d.Cell));
                foreach (var c in candidates)
                {
                    if (!map.TripsTrap(s.Def.Specs[c.piece], c.cell)) return Resolve(state, correct, c);
                }
                return Resolve(state, correct, candidates[0]);
            }

            // 2. Fallback: the unclaimed stored placement with the largest coverage.
            var claimed = ClaimedEntries(state, correct);
            int best = -1, bestCoverage = -1;
            for (int n = 0; n < state.Solution.Length; n++)
            {
                if (claimed[n]) continue;
                var (piece, cell) = state.Solution[n];
                int coverage = map.Coverage(s.Def.Specs[piece], cell).Count;
                bool trips = map.TripsTrap(s.Def.Specs[piece], cell);
                int score = coverage - (trips ? 1000 : 0);
                if (score > bestCoverage) { best = n; bestCoverage = score; }
            }
            if (best < 0) return null;
            return Resolve(state, correct, state.Solution[best]);
        }

        // Map a (piece, cell) from a solver or the stored solution to an
        // actual piece: the lowest piece of that tile not already correct.
        static (int piece, int cell)? Resolve(GameState state, PieceState[] correct, (int piece, int cell) target)
        {
            var s = state.Session;
            PieceSpec spec = s.Def.Specs[target.piece];
            if (!correct[target.piece].Placed) return (target.piece, target.cell);
            for (int k = 0; k < s.Pieces.Length; k++)
            {
                if (s.Def.Specs[k] == spec && !correct[k].Placed) return (k, target.cell);
            }
            return null;
        }

        static bool[] ClaimedEntries(GameState state, PieceState[] correct)
        {
            var claimed = new bool[state.Solution.Length];
            for (int k = 0; k < correct.Length; k++)
            {
                if (!correct[k].Placed) continue;
                int cell = Grid.Loc(correct[k].I, correct[k].J);
                for (int n = 0; n < state.Solution.Length; n++)
                {
                    if (!claimed[n] && state.Solution[n].cell == cell) { claimed[n] = true; break; }
                }
            }
            return claimed;
        }
    }
}
