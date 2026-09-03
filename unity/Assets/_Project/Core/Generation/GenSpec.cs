using System;
using GridInfect.Core.Solving;

namespace GridInfect.Core.Generation
{
    // Board elements a spec may use. Walls are the pruning tool; switches
    // and traps are carried on the enum for later stages' pruners.
    [Flags]
    public enum Element
    {
        None = 0,
        Walls = 1,
        Switches = 2,
        Traps = 4,
    }

    public enum CarveMode
    {
        Gaps,   // v1 style: one draw per cell along each arm, gaps allowed
        Runs,   // each arm is a contiguous run of random length, optionally capped by a wall
    }

    // How a sampled solution is carved into a board. `Gaps` mirrors the v1
    // roll (activate iff draw(20) - offset > 4, i.e. chance (15 - offset)/20)
    // with the curve exposed; `Runs` carves corridors, which is what the
    // classic set's unique levels look like and what walls can pin.
    public sealed class CarveParams
    {
        public CarveMode Mode = CarveMode.Runs;
        public int BaseChance = 15;    // Gaps: out of 20 at offset 0
        public int Falloff = 1;        // Gaps: chance lost per cell of offset
        public int MinRun = 1;         // Runs: arm length band
        public int MaxRun = 5;
        public int EndWallChance = 14; // Runs: out of 20, a wall right past the run's end (void cells only)
        public int MinActive = 6;
        public int MaxActive = 40;
        // Shape bias: > 0 favours long arms (slower falloff past offset 3),
        // < 0 favours compact blobs (faster falloff past offset 3).
        public int ShapeBias = 0;

        public int ChanceAt(int offset)
        {
            int chance = BaseChance - Falloff * offset;
            if (offset > 3) chance += ShapeBias * (offset - 3);
            if (chance < 0) chance = 0;
            if (chance > 20) chance = 20;
            return chance;
        }
    }

    public sealed class GenSpec
    {
        public Element Elements = Element.Walls;
        public int MinPieces = 2;
        public int MaxPieces = 5;
        public Grade MinGrade = Grade.G1;
        public Grade MaxGrade = Grade.G5;
        public CarveParams Carve = new CarveParams();
        public int MaxPruneSteps = 12;
        public int MaxWalls = 12;
        public bool AllowDuplicateTiles = false;
        // UD and LR cover their whole line from any cell of it, so nothing
        // but other pieces' cells can pin them; excluded unless asked for.
        public bool AllowSymmetricTiles = false;
        public bool ExclusiveLines = true;     // no two piece cells share a row or a column (v1 did this too)
        public int MinPieceDistance = 2;       // Manhattan distance between piece cells
        public bool RequireAllPieces = true;   // no decoy pieces (NEXT_PASS: cut)
        public int SolutionCap = 4000;         // above this a sample is rejected as hopeless

        public GenSpec Clone()
        {
            return new GenSpec
            {
                Elements = Elements, MinPieces = MinPieces, MaxPieces = MaxPieces,
                MinGrade = MinGrade, MaxGrade = MaxGrade, MaxPruneSteps = MaxPruneSteps,
                MaxWalls = MaxWalls, AllowDuplicateTiles = AllowDuplicateTiles, AllowSymmetricTiles = AllowSymmetricTiles,
                ExclusiveLines = ExclusiveLines, MinPieceDistance = MinPieceDistance,
                RequireAllPieces = RequireAllPieces, SolutionCap = SolutionCap,
                Carve = new CarveParams
                {
                    Mode = Carve.Mode, BaseChance = Carve.BaseChance, Falloff = Carve.Falloff,
                    MinRun = Carve.MinRun, MaxRun = Carve.MaxRun, EndWallChance = Carve.EndWallChance,
                    MinActive = Carve.MinActive, MaxActive = Carve.MaxActive, ShapeBias = Carve.ShapeBias,
                },
            };
        }
    }

    public sealed class GeneratedLevel
    {
        public LevelDef Def;
        public (int piece, int cell)[] Solution;   // the sampled solution, in a winning order
        public Deduction[] Trace;
        public Grade Grade;
        public int Effort;
        public ulong Seed;
        public string Hash;                        // canonical under the board's symmetry group
        public int Walls;
        public int PruneSteps;
    }

    // Why a seed was rejected; the batch CLI reports the distribution.
    public enum Rejection
    {
        None,
        Tiles,          // could not sample distinct tiles
        Size,           // active cell count outside the carve band
        TooMany,        // solution count above the cap before pruning
        NotUnique,      // still ambiguous after MaxPruneSteps walls
        NotDeducible,   // solver needed a guess
        Decoy,          // a piece is not needed
        Grade,          // outside the grade band
    }
}
