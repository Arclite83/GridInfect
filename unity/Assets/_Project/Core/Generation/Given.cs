namespace GridInfect.Core.Generation
{
    // A unit of information the puzzle hands the player (docs/GENERATOR_V2.md
    // §Pipeline). The constructor adds givens until exactly one solution is
    // left, then removes every one the uniqueness proof does not need. The
    // first four kinds are board cells; a Lock is a piece pre-placed at its
    // solution cell (the level loader places it locked), the last resort
    // for the ambiguity no cell can break: two mirror pieces swapping runs.
    public enum GivenKind : byte
    {
        Wall,        // a run end: stops arms (the sampler's maximal set puts one past every run)
        Gap,         // a covered cell left void: arms jump it, no piece may sit on it
        Forbidden,   // a clean cell: any spread that would touch it is illegal (Element.Forbidden)
        Trap,        // a reset trap: an arm ending on it loses (Element.Traps)
        Lock,        // a piece placed and locked before play; Cell holds the piece index
    }

    public readonly struct Given
    {
        public readonly GivenKind Kind;
        public readonly int Cell;      // the board cell, or the piece index for a Lock

        public int Piece => Cell;

        public Given(GivenKind kind, int cell)
        {
            Kind = kind;
            Cell = cell;
        }

        // The board value that states the given, and the one that withdraws it.
        public byte Value
        {
            get
            {
                switch (Kind)
                {
                    case GivenKind.Wall: return Core.Cell.Wall;
                    case GivenKind.Forbidden: return Core.Cell.Forbidden;
                    case GivenKind.Trap: return Core.Cell.ResetTrap;
                    default: return Core.Cell.Void;
                }
            }
        }

        public byte Absent => Kind == GivenKind.Gap ? Core.Cell.Active : Core.Cell.Void;

        public override string ToString() => $"{Kind}@{Cell}";
    }
}
