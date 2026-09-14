using System.Collections.Generic;
using Bloodhound.Engine;
using GridInfect.Core.Solving;

namespace GridInfect.Core
{
    // piece.lock { }: spend one lock to place one piece at its solution cell
    // and lock it there (NEXT_PASS "Lock"). Which piece: the deducer's next
    // forced placement given the player's currently correct pieces; failing
    // that, the unplaced piece with the largest coverage in the stored
    // solution — in both cases one the board will take now and not only at
    // the end (Lock.ChooseTarget). A player piece on the target cell goes
    // back to the tray.
    //
    // On a replay (Queries.IsReplay) the lock is free and the wallet is not
    // even consulted: the level is already beaten, so a free solve on it is
    // not a shortcut past anything the player has not already done.
    public sealed class LockPieceAction : GameAction<GameState>
    {
        public override string Name => "piece.lock";

        public override string Validate(GameState state, ActionInput input)
        {
            var s = state.Session;
            if (s == null) return "no level loaded";
            if (s.ResolutionPending) return "resolution pending — dispatch board.resolve first";
            if (s.Solved) return "level already solved";
            if (state.Profile.Locks <= 0 && !Queries.IsReplay(state)) return "no locks left";
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
            if (!Queries.IsReplay(state))
            {
                state.Profile.Locks--;
                state.Profile.Dirty = true;
            }
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
        //
        // Order matters on a board with a switch or a trap: a placement
        // whose arm ends on a trap resets the board, and one whose arm ends
        // on a switch repels the infection back off the cells behind it —
        // neither of which happens to the placement that *wins*, because
        // the win check runs first (RULES §4.1). So such a placement is
        // often the level's last piece, and the player can play one early
        // and lift it again where a lock never comes up. A candidate whose
        // arms reach a switch or a trap is therefore passed over until it
        // is the placement that wins. Every last piece reaches one — that
        // is what makes it last — so the arm test never misses one, and
        // no search is needed to find it.
        //
        // On a board where the arm test rules out everything (Legacy 26 is
        // the only one that ships), the exact question gets asked instead:
        // would this level still be winnable with the piece pinned
        // (PlacementOrder.KeepsWinnable). It is the slow way round and the
        // arm test almost always answers first.
        public static (int piece, int cell)? ChooseTarget(GameState state)
        {
            var s = state.Session;
            var correct = CorrectPieces(state);
            var map = new LineMap(s.Def);
            return Pick(state, map, correct, exact: false) ?? Pick(state, map, correct, exact: true);
        }

        static (int piece, int cell)? Pick(GameState state, LineMap map, PieceState[] correct, bool exact)
        {
            var s = state.Session;

            // 1. The deducer's next forced placement from the correct pieces.
            var solve = Deducer.Solve(s.Def, correct);
            if (solve.Solved && solve.Trace.Length > 0)
            {
                foreach (var d in solve.Trace)
                {
                    var target = Resolve(state, correct, (d.Piece, d.Cell));
                    if (Safe(state, map, target, exact)) return target;
                }
            }

            // 2. Fallback: the unclaimed stored placement with the largest
            //    coverage, skipping any the guard rules out.
            var claimed = ClaimedEntries(state, correct);
            var ranked = new List<(int index, int coverage)>();
            for (int n = 0; n < state.Solution.Length; n++)
            {
                if (claimed[n]) continue;
                var (piece, cell) = state.Solution[n];
                ranked.Add((n, map.Coverage(s.Def.Specs[piece], cell).Count));
            }
            ranked.Sort((a, b) => b.coverage.CompareTo(a.coverage));
            foreach (var (index, _) in ranked)
            {
                var target = Resolve(state, correct, state.Solution[index]);
                if (Safe(state, map, target, exact)) return target;
            }
            return null;
        }

        // May this placement be pinned now? A board with neither switch nor
        // trap never un-infects a cell, so order cannot matter there and
        // anything goes. Everywhere else the placement that wins is always
        // safe — it wins iff its spread takes every cell the live board
        // still leaves uninfected, and only while no piece of the player's
        // has to be evicted off the cell first, since that eviction would
        // un-infect cells of its own. Anything else that reaches a switch
        // or a trap waits, unless `exact` is set, when the level itself is
        // asked whether it survives the pin.
        static bool Safe(GameState state, LineMap map, (int piece, int cell)? target, bool exact)
        {
            if (target == null) return false;
            if (!map.HasDynamics) return true;
            var s = state.Session;
            var (piece, cell) = target.Value;
            var spread = map.Spread(s.Def.Specs[piece], cell);
            if (!spread.Trips && !spread.Switches) return true;
            if (!Occupied(s, cell) && Covers(spread, s)) return true;
            return exact && KeepsWinnable(s, piece, cell);
        }

        // The pinned set as it would stand — the locks already down, plus
        // this one — put to the solver. The player's own pieces are not in
        // it: those they can lift, and a full reset lifts them anyway.
        static bool KeepsWinnable(LevelSession s, int piece, int cell)
        {
            var pinned = new PieceState[s.Pieces.Length];
            for (int k = 0; k < pinned.Length; k++)
            {
                if (s.Pieces[k].Locked) pinned[k] = s.Pieces[k];
            }
            pinned[piece] = new PieceState
            {
                Tile = s.Pieces[piece].Tile, Placed = true, Locked = true,
                I = (sbyte)(cell / Grid.Width), J = (sbyte)(cell % Grid.Width),
            };
            return PlacementOrder.KeepsWinnable(s.Def, pinned);
        }

        static bool Occupied(LevelSession s, int cell)
        {
            int i = cell / Grid.Width, j = cell % Grid.Width;
            for (int k = 0; k < s.Pieces.Length; k++)
            {
                if (s.Pieces[k].Placed && s.Pieces[k].I == i && s.Pieces[k].J == j) return true;
            }
            return false;
        }

        // Does this placement's spread take every cell the live board still
        // leaves uninfected? Then placing it wins.
        static bool Covers(LineMap.SpreadResult spread, LevelSession s)
        {
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                if (s.Board[loc] == Cell.Active && !spread.Covered.Has(loc)) return false;
            }
            return true;
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
