namespace GridInfect.Core.Solving
{
    // Six bands. Thresholds are documented in docs/GENERATOR_V2.md §Solver
    // and locked by the classic-level table in SolverTests.
    public enum Grade
    {
        G1 = 1, G2, G3, G4, G5, G6
    }

    public static class Grader
    {
        // Rule firings weighted by tier: a placement by ownership is the
        // unit, a trap exclusion counts double, a counting round four, and a
        // round of "suppose" refutations twelve (capped at four rounds).
        public const int OwnershipWeight = 1;
        public const int ArmWeight = 2;
        public const int CountingWeight = 4;
        public const int ContradictionWeight = 12;
        public const int ContradictionCap = 4;

        public static int Effort(SolveResult r)
        {
            int t4 = r.TierCounts[(int)Tier.Contradiction1];
            if (t4 > ContradictionCap) t4 = ContradictionCap;
            return r.TierCounts[(int)Tier.LineOwnership] * OwnershipWeight
                   + r.TierCounts[(int)Tier.ArmExclusion] * ArmWeight
                   + r.TierCounts[(int)Tier.PieceCounting] * CountingWeight
                   + t4 * ContradictionWeight;
        }

        public const int G1Max = 7;
        public const int G2Max = 12;
        public const int G3Max = 18;
        public const int G4Max = 26;

        public static Grade Grade(SolveResult r)
        {
            if (r == null || !r.Solved) return Solving.Grade.G6;
            int effort = Effort(r);
            if (effort <= G1Max) return Solving.Grade.G1;
            if (effort <= G2Max) return Solving.Grade.G2;
            if (effort <= G3Max) return Solving.Grade.G3;
            if (effort <= G4Max) return Solving.Grade.G4;
            return Solving.Grade.G5;
        }
    }
}
