using Bloodhound.Engine;

namespace GridInfect.Core
{
    /// <summary>
    /// piece.place — drop a tray piece on a legal cell and spread. Leaves the
    /// session ResolutionPending; the presentation beat ends with
    /// board.resolve (never skipped, only fast-forwarded).
    /// </summary>
    public sealed class PlacePieceAction : GameAction<GameState>
    {
        public override string Name => "piece.place";

        public override string Validate(GameState state, ActionInput input)
        {
            var s = state.Session;
            if (s == null) return "no level loaded";
            if (s.ResolutionPending) return "resolution pending — dispatch board.resolve first";
            int piece = input.Int("piece");
            int i = input.Int("i");
            int j = input.Int("j");
            if (piece < 0 || piece >= s.Pieces.Length) return $"piece index {piece} out of range";
            if (s.Pieces[piece].Placed) return $"piece {piece} is already placed (clear it first)";
            if (!Rules.CanPlace(s, piece, i, j)) return $"illegal placement at ({i},{j})";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            Rules.SetPiece(state.Session, input.Int("piece"), input.Int("i"), input.Int("j"));
        }
    }

    /// <summary>
    /// board.resolve — the deferred consequence of the last placement:
    /// win check first, else reset if tripped, else repels in queue order.
    /// </summary>
    public sealed class ResolveBoardAction : GameAction<GameState>
    {
        public override string Name => "board.resolve";

        public override string Validate(GameState state, ActionInput input)
        {
            var s = state.Session;
            if (s == null) return "no level loaded";
            if (!s.ResolutionPending) return "no resolution pending";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            Rules.Resolve(state.Session);
        }
    }

    /// <summary>
    /// piece.clear — undo: pick a placed piece off the board (RULES.md §7).
    /// Free and unlimited.
    /// </summary>
    public sealed class ClearPieceAction : GameAction<GameState>
    {
        public override string Name => "piece.clear";

        public override string Validate(GameState state, ActionInput input)
        {
            var s = state.Session;
            if (s == null) return "no level loaded";
            if (s.ResolutionPending) return "resolution pending — dispatch board.resolve first";
            int piece = input.Int("piece");
            if (piece < 0 || piece >= s.Pieces.Length) return $"piece index {piece} out of range";
            if (!s.Pieces[piece].Placed) return $"piece {piece} is not placed";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            Rules.ClearPiece(state.Session, input.Int("piece"));
        }
    }
}
