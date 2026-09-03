namespace GridInfect.Core
{
    // Pieces a level places locked before play: a given of the level
    // (docs/GENERATOR_V2.md §Pipeline), applied by the loading action.
    public static class Locked
    {
        // The locked pieces as the solver and the counter see them, or
        // null when there are none.
        public static PieceState[] Placed(LevelDef def, (int piece, int cell)[] locks)
        {
            if (locks == null || locks.Length == 0) return null;
            var placed = new PieceState[def.Pieces.Length];
            foreach (var (piece, cell) in locks)
            {
                placed[piece] = new PieceState
                {
                    Tile = def.Pieces[piece], Placed = true, Locked = true,
                    I = (sbyte)(cell / Grid.Width), J = (sbyte)(cell % Grid.Width),
                };
            }
            return placed;
        }

        // Place the locked pieces on a fresh session, then resolve once.
        public static void Apply(LevelSession s, (int piece, int cell)[] locks)
        {
            if (locks == null || locks.Length == 0) return;
            foreach (var (piece, cell) in locks)
            {
                s.Rules.SetPiece(s, piece, cell / Grid.Width, cell % Grid.Width);
                s.Pieces[piece].Locked = true;
            }
            s.Rules.Resolve(s);
        }
    }
}
