namespace GridInfect.Core.Solving
{
    // Six bands. The grade is read off the solve trace: the lookahead depth
    // the solve needed (plus the translation layers the board's piece types
    // add) sets the band's floor, the peak number of undecided pieces the
    // player had to hold at once moves within it. Thresholds are documented
    // in docs/GENERATOR_V2.md §Solver and locked by the classic-level table
    // in SolverTests.
    public enum Grade
    {
        G1 = 1, G2, G3, G4, G5, G6
    }

    public static class Grader
    {
        // Rule firings weighted by tier, for ordering within a band: a
        // placement by ownership is the unit, a trap exclusion counts double,
        // a counting round four, a round of "suppose" refutations twelve
        // (capped at four rounds), a two-deep round twenty-four.
        public const int OwnershipWeight = 1;
        public const int ArmWeight = 2;
        public const int CountingWeight = 4;
        public const int ContradictionWeight = 12;
        public const int Contradiction2Weight = 24;
        public const int ContradictionCap = 4;

        public static int Effort(SolveResult r)
        {
            int t4 = r.TierCounts[(int)Tier.Contradiction1];
            if (t4 > ContradictionCap) t4 = ContradictionCap;
            int t5 = r.TierCounts[(int)Tier.Contradiction2];
            if (t5 > ContradictionCap) t5 = ContradictionCap;
            return r.TierCounts[(int)Tier.LineOwnership] * OwnershipWeight
                   + r.TierCounts[(int)Tier.ArmExclusion] * ArmWeight
                   + r.TierCounts[(int)Tier.PieceCounting] * CountingWeight
                   + t4 * ContradictionWeight
                   + t5 * Contradiction2Weight;
        }

        // A piece type the player must translate before the line rules
        // apply counts as a lookahead layer: diagonal arms, the area blot,
        // relays. Short arms are read off the piece and cost nothing.
        public static int Translation(LevelDef def)
        {
            bool diagonal = false, area = false;
            foreach (var spec in def.Specs)
            {
                diagonal |= spec.HasDiagonal;
                area |= spec.Area;
            }
            return (diagonal ? 1 : 0) + (area ? 1 : 0) + (def.HasRelays ? 1 : 0);
        }

        // The solve's depth plus the board's translation layers.
        public static int EffectiveDepth(SolveResult r, LevelDef def) => r.Depth + Translation(def);

        // Band by (depth, peak open pieces); peak is clamped to the last
        // column. Read: G1 is read off the board, G2 holds two or three
        // undecided pieces, G3 holds four or more (or one suppose with few
        // open), G4 supposes with four or more open, G5 needs a suppose
        // inside a suppose.
        static readonly Solving.Grade[][] Bands =
        {
            new[] { Solving.Grade.G1, Solving.Grade.G1, Solving.Grade.G2, Solving.Grade.G2, Solving.Grade.G3, Solving.Grade.G3 },   // depth 0
            new[] { Solving.Grade.G3, Solving.Grade.G3, Solving.Grade.G3, Solving.Grade.G3, Solving.Grade.G4, Solving.Grade.G4 },   // depth 1
            new[] { Solving.Grade.G5, Solving.Grade.G5, Solving.Grade.G5, Solving.Grade.G5, Solving.Grade.G5, Solving.Grade.G5 },   // depth 2
        };

        public static Solving.Grade Grade(SolveResult r) => Band(r, r?.Depth ?? 0);

        public static Solving.Grade Grade(SolveResult r, LevelDef def) => Band(r, r == null ? 0 : EffectiveDepth(r, def));

        static Solving.Grade Band(SolveResult r, int depth)
        {
            if (r == null || !r.Solved) return Solving.Grade.G6;
            if (depth > Depth.Max) depth = Depth.Max;
            int peak = r.PeakOpen > Bands[depth].Length - 1 ? Bands[depth].Length - 1 : r.PeakOpen;
            return Bands[depth][peak];
        }
    }
}
