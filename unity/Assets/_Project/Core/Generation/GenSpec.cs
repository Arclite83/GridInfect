using System;
using System.Collections.Generic;
using Bloodhound.Engine;
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
        ShortArms = 8,     // stage 8: an arm reaches 1 or 2 cells
        Area = 16,         // stage 9: the 3x3 blot piece
        Forbidden = 32,    // stage 10: cells that must stay clean
        Diagonals = 64,    // stage 11: diagonal arms
        Relays = 128,      // stage 12: cells that emit arms when infected
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
        public int MaxGivens = 12;             // discriminating givens the constructor may add
        public int MaxLocks = 1;               // pieces the constructor may pre-place, when no cell breaks the ambiguity
        public bool AllowDuplicateTiles = false;
        // UD and LR cover their whole line from any cell of it, so nothing
        // but other pieces' cells can pin them; excluded unless asked for.
        public bool AllowSymmetricTiles = false;
        public bool ExclusiveLines = true;     // no two piece cells share a row or a column (v1 did this too)
        public int MinPieceDistance = 2;       // Manhattan distance between piece cells
        public bool RequireAllPieces = true;   // no decoy pieces (NEXT_PASS: cut)
        public bool RequireUsefulArms = true;  // every arm reaches a cell (off for boards sampled elsewhere, e.g. the classics)
        public int SolutionCap = 4000;         // above this a sample is rejected as hopeless

        // Element tunables (each only draws from the RNG when its element is on).
        public int ShortArmChance = 10;        // out of 20, per arm: reach 1 or 2 instead of the edge
        public int AreaChance = 6;             // out of 20, per piece: a 3x3 blot instead of a tile
        public int MaxForbidden = 4;           // forbidden cells the constructor may add (Element.Forbidden)
        public int MaxTraps = 2;               // reset traps the constructor may add (Element.Traps)
        public int DiagonalChance = 10;        // out of 20, per piece: one or two diagonal arms join its tile
        public int RelayChance = 10;           // out of 20, per piece with arms: one carved cell on an arm becomes a relay

        // The spec as data (docs/worlds headers, the daily spec): every
        // field, so a spec round-trips and a world regenerates from its header.
        public string ToJson()
        {
            return MiniJson.Write(new Dictionary<string, object>
            {
                ["elements"] = (int)Elements,
                ["pieces"] = new List<object> { MinPieces, MaxPieces },
                ["grades"] = new List<object> { (int)MinGrade, (int)MaxGrade },
                ["mode"] = Carve.Mode == CarveMode.Runs ? "runs" : "gaps",
                ["baseChance"] = Carve.BaseChance,
                ["falloff"] = Carve.Falloff,
                ["runs"] = new List<object> { Carve.MinRun, Carve.MaxRun },
                ["active"] = new List<object> { Carve.MinActive, Carve.MaxActive },
                ["shapeBias"] = Carve.ShapeBias,
                ["maxGivens"] = MaxGivens,
                ["maxLocks"] = MaxLocks,
                ["dupTiles"] = AllowDuplicateTiles,
                ["symmetricTiles"] = AllowSymmetricTiles,
                ["exclusiveLines"] = ExclusiveLines,
                ["distance"] = MinPieceDistance,
                ["allPieces"] = RequireAllPieces,
                ["usefulArms"] = RequireUsefulArms,
                ["cap"] = SolutionCap,
                ["shortArmChance"] = ShortArmChance,
                ["areaChance"] = AreaChance,
                ["maxForbidden"] = MaxForbidden,
                ["maxTraps"] = MaxTraps,
                ["diagonalChance"] = DiagonalChance,
                ["relayChance"] = RelayChance,
            });
        }

        public static GenSpec FromJson(string json) => FromJson(MiniJson.Parse(json) as Dictionary<string, object>);

        public static GenSpec FromJson(Dictionary<string, object> raw)
        {
            var spec = new GenSpec();
            if (raw == null) return spec;
            var input = new ActionInput(raw);
            spec.Elements = (Element)input.IntOr("elements", (int)spec.Elements);
            if (raw.TryGetValue("pieces", out object p) && p is List<object> pieces && pieces.Count == 2)
            {
                spec.MinPieces = (int)(long)pieces[0];
                spec.MaxPieces = (int)(long)pieces[1];
            }
            if (raw.TryGetValue("grades", out object g) && g is List<object> grades && grades.Count == 2)
            {
                spec.MinGrade = (Grade)(int)(long)grades[0];
                spec.MaxGrade = (Grade)(int)(long)grades[1];
            }
            if (raw.TryGetValue("mode", out object m) && m is string mode) spec.Carve.Mode = mode == "gaps" ? CarveMode.Gaps : CarveMode.Runs;
            spec.Carve.BaseChance = input.IntOr("baseChance", spec.Carve.BaseChance);
            spec.Carve.Falloff = input.IntOr("falloff", spec.Carve.Falloff);
            if (raw.TryGetValue("runs", out object r) && r is List<object> runs && runs.Count == 2)
            {
                spec.Carve.MinRun = (int)(long)runs[0];
                spec.Carve.MaxRun = (int)(long)runs[1];
            }
            if (raw.TryGetValue("active", out object a) && a is List<object> active && active.Count == 2)
            {
                spec.Carve.MinActive = (int)(long)active[0];
                spec.Carve.MaxActive = (int)(long)active[1];
            }
            spec.Carve.ShapeBias = input.IntOr("shapeBias", spec.Carve.ShapeBias);
            spec.MaxGivens = input.IntOr("maxGivens", spec.MaxGivens);
            spec.MaxLocks = input.IntOr("maxLocks", spec.MaxLocks);
            if (raw.TryGetValue("dupTiles", out object d) && d is bool dup) spec.AllowDuplicateTiles = dup;
            if (raw.TryGetValue("symmetricTiles", out object st) && st is bool sym) spec.AllowSymmetricTiles = sym;
            if (raw.TryGetValue("exclusiveLines", out object el) && el is bool ex) spec.ExclusiveLines = ex;
            spec.MinPieceDistance = input.IntOr("distance", spec.MinPieceDistance);
            if (raw.TryGetValue("allPieces", out object ap) && ap is bool all) spec.RequireAllPieces = all;
            if (raw.TryGetValue("usefulArms", out object ua) && ua is bool useful) spec.RequireUsefulArms = useful;
            spec.SolutionCap = input.IntOr("cap", spec.SolutionCap);
            spec.ShortArmChance = input.IntOr("shortArmChance", spec.ShortArmChance);
            spec.AreaChance = input.IntOr("areaChance", spec.AreaChance);
            spec.MaxForbidden = input.IntOr("maxForbidden", spec.MaxForbidden);
            spec.MaxTraps = input.IntOr("maxTraps", spec.MaxTraps);
            spec.DiagonalChance = input.IntOr("diagonalChance", spec.DiagonalChance);
            spec.RelayChance = input.IntOr("relayChance", spec.RelayChance);
            return spec;
        }

        public GenSpec Clone()
        {
            return new GenSpec
            {
                Elements = Elements, MinPieces = MinPieces, MaxPieces = MaxPieces,
                MinGrade = MinGrade, MaxGrade = MaxGrade, MaxGivens = MaxGivens, MaxLocks = MaxLocks,
                AllowDuplicateTiles = AllowDuplicateTiles, AllowSymmetricTiles = AllowSymmetricTiles,
                ExclusiveLines = ExclusiveLines, MinPieceDistance = MinPieceDistance,
                RequireAllPieces = RequireAllPieces, RequireUsefulArms = RequireUsefulArms, SolutionCap = SolutionCap, ShortArmChance = ShortArmChance,
                AreaChance = AreaChance, MaxForbidden = MaxForbidden, MaxTraps = MaxTraps, DiagonalChance = DiagonalChance, RelayChance = RelayChance,
                Carve = new CarveParams
                {
                    Mode = Carve.Mode, BaseChance = Carve.BaseChance, Falloff = Carve.Falloff,
                    MinRun = Carve.MinRun, MaxRun = Carve.MaxRun,
                    MinActive = Carve.MinActive, MaxActive = Carve.MaxActive, ShapeBias = Carve.ShapeBias,
                },
            };
        }
    }

    public sealed class GeneratedLevel
    {
        public LevelDef Def;
        public (int piece, int cell)[] Solution;   // the sampled solution, in a winning order, locked pieces first
        public (int piece, int cell)[] Locks;      // pieces the loader places locked before play (empty when none)
        public Deduction[] Trace;
        public Grade Grade;
        public int Effort;                         // rule firings, weighted: orders levels within a band
        public int Depth;                          // lookahead the solve needed plus the board's translation layers
        public int PeakOpen;                       // most undecided pieces held at once
        public ulong Seed;
        public string Hash;                        // canonical under the board's symmetry group
        public int Walls;                          // givens left after minimization, by kind
        public int Gaps;
        public int ForbiddenCells;
        public int Traps;
        public int LockCount;
        public int Relays;
        public int Givens;                         // all givens left after minimization
    }

    // Why a seed was rejected; the batch CLI reports the distribution.
    public enum Rejection
    {
        None,
        Tiles,          // could not sample distinct tiles
        Size,           // active cell count outside the carve band
        TooMany,        // covering sets above the cap
        NotUnique,      // still ambiguous after MaxGivens givens, or no given kills an alternative
        NotDeducible,   // solver needed a guess
        Decoy,          // a piece is not needed
        Grade,          // outside the grade band
        Unwinnable,     // the sample's own solution does not win (an arm blinded, a relay loop)
        TooDeep,        // solvable, but past the lookahead cap once translation layers count
    }
}
