using System;
using Bloodhound.Engine;

namespace GridInfect.Core
{
    /// <summary>
    /// The Free Play level generator — a semantically exact port of
    /// LevelBuilder.cpp per docs/GENERATOR.md, with one specified change:
    /// libc rand() is replaced by the kernel's Pcg32 with an explicit seed
    /// (GENERATOR.md §1's port guidance; the original was never reproducible
    /// cross-platform). Draw order is preserved exactly — every rejected
    /// sample still consumes its three draws, carving consumes one draw per
    /// in-bounds visited cell — so generation is bit-reproducible against
    /// itself and locked by golden tests.
    ///
    /// Preserved original quirks (contract, do not fix):
    /// - the UD window shrink is missing its bottom margin (GENERATOR.md §3
    ///   step 4 table);
    /// - carving never stops early and probabilistically leaves gaps that
    ///   spread jumps anyway (§4);
    /// - the rejection loop is unbounded (guarded here only against configs
    ///   that make it structurally infinite).
    /// </summary>
    public static class LevelGenerator
    {
        struct Config
        {
            public int Pieces, XOffset, XCount, YOffset, YCount;
        }

        static readonly Config[] Configs =
        {
            // Difficulty ordinal order: Beginner, Easy, Medium, Hard, Challenging
            new Config { Pieces = 2, XOffset = 3, XCount = 5, YOffset = 1, YCount = 5 },
            new Config { Pieces = 3, XOffset = 3, XCount = 6, YOffset = 1, YCount = 5 },
            new Config { Pieces = 4, XOffset = 2, XCount = 7, YOffset = 0, YCount = 6 },
            new Config { Pieces = 4, XOffset = 0, XCount = 11, YOffset = 0, YCount = 6 },
            new Config { Pieces = 5, XOffset = 0, XCount = 11, YOffset = 0, YCount = 6 },
        };

        public static LevelDef Generate(Difficulty difficulty, ref Pcg32 rng)
        {
            return Generate(difficulty, ref rng, out _);
        }

        /// <summary>
        /// Generate a level, also returning the sampled solution placements
        /// (piece k at (i,j)) that the original discards — placing every piece
        /// at its sampled cell always wins (GENERATOR.md §5), which the tests
        /// exploit to cross-verify generator and rules against each other.
        /// </summary>
        public static LevelDef Generate(Difficulty difficulty, ref Pcg32 rng, out (int i, int j)[] solution)
        {
            Config config = Configs[(int)difficulty];
            if (config.Pieces > Grid.Height)
                throw new InvalidOperationException("piecesToSet > board height makes the rejection loop infinite (GENERATOR.md §7)");

            var board = new byte[Grid.Cells];
            var tiles = new Tile[config.Pieces];
            var pi = new int[config.Pieces];
            var pj = new int[config.Pieces];

            for (int n = 0; n < config.Pieces; n++)
            {
                while (true) // unbounded rejection loop, as in the original
                {
                    var tile = (Tile)rng.Next(15);                       // draw 1: tile
                    bool overlap = false;

                    if (difficulty == Difficulty.Beginner && TileArms.Count(tile) >= 3) overlap = true;
                    if (difficulty == Difficulty.Challenging && (tile == Tile.LR || tile == Tile.UD)) overlap = true;
                    for (int k = 0; k < n; k++)
                    {
                        if (tiles[k] == tile) overlap = true; // tile types unique per level
                    }

                    int xOffset = config.XOffset, xCount = config.XCount;
                    int yOffset = config.YOffset, yCount = config.YCount;
                    ShrinkWindow(tile, ref xOffset, ref xCount, ref yOffset, ref yCount);

                    int x = xOffset + rng.Next(xCount);                  // draw 2: column
                    int y = yOffset + rng.Next(yCount);                  // draw 3: row
                    // (both consumed even when the sample was already rejected)

                    for (int k = 0; k < n; k++)
                    {
                        if (pi[k] == y || pj[k] == x) overlap = true;    // row/column exclusivity
                    }

                    if (overlap) continue;

                    tiles[n] = tile;
                    pi[n] = y;
                    pj[n] = x;
                    Carve(board, tile, y, x, ref rng);                   // carve immediately on acceptance
                    break;
                }
            }

            solution = new (int, int)[config.Pieces];
            for (int n = 0; n < config.Pieces; n++) solution[n] = (pi[n], pj[n]);

            return new LevelDef(board, tiles);
        }

        /// <summary>
        /// The per-tile sampling-window shrink (GENERATOR.md §3 step 4),
        /// including the original's missing bottom margin on UD.
        /// </summary>
        static void ShrinkWindow(Tile tile, ref int xOffset, ref int xCount, ref int yOffset, ref int yCount)
        {
            switch (tile)
            {
                case Tile.L: xOffset += 2; xCount -= 2; break;
                case Tile.R: xCount -= 2; break;
                case Tile.U: yOffset += 2; yCount -= 2; break;
                case Tile.D: yCount -= 2; break;
                case Tile.LR: xOffset += 2; xCount -= 4; break;
                case Tile.LU: xOffset += 2; xCount -= 2; yOffset += 2; yCount -= 2; break;
                case Tile.LD: xOffset += 2; xCount -= 2; yCount -= 2; break;
                case Tile.RU: xCount -= 2; yOffset += 2; yCount -= 2; break;
                case Tile.RD: xCount -= 2; yCount -= 2; break;
                case Tile.UD: yOffset += 2; yCount -= 2; break; // original bug kept: no bottom margin
                case Tile.LRU: xOffset += 2; xCount -= 4; yOffset += 2; yCount -= 2; break;
                case Tile.LRD: xOffset += 2; xCount -= 4; yCount -= 2; break;
                case Tile.LUD: xOffset += 2; xCount -= 2; yOffset += 2; yCount -= 4; break;
                case Tile.RUD: xCount -= 2; yOffset += 2; yCount -= 4; break;
                case Tile.LRUD: xOffset += 2; xCount -= 4; yOffset += 2; yCount -= 4; break;
            }
            if (xCount <= 0 || yCount <= 0)
                throw new InvalidOperationException($"sampling window collapsed for {tile} — config changed without re-checking GENERATOR.md §3");
        }

        /// <summary>
        /// LevelBuilder::buildBoard — carve outward from an accepted piece:
        /// own cell active, then offset-major L,R,U,D walks; out-of-bounds
        /// cells are skipped without consuming a draw; in-bounds cells consume
        /// one draw and activate iff (draw%20) - offset > 4 (70% at distance 1
        /// down to 25% at 10). Failed rolls leave gaps but the walk continues.
        /// </summary>
        static void Carve(byte[] board, Tile tile, int pieceI, int pieceJ, ref Pcg32 rng)
        {
            board[Grid.Loc(pieceI, pieceJ)] = Cell.Active;

            for (int offset = 1; offset <= Grid.SpreadRange; offset++)
            {
                for (int d = 0; d < 4; d++)
                {
                    var dir = (Dir)d;
                    if (!TileArms.Has(tile, dir)) continue;

                    int i = pieceI + TileArms.Di(dir) * offset;
                    int j = pieceJ + TileArms.Dj(dir) * offset;
                    if (!Grid.InBounds(i, j)) continue;      // bounds check precedes the draw

                    if (rng.Next(20) - offset > 4)
                    {
                        board[Grid.Loc(i, j)] = Cell.Active;
                    }
                }
            }
        }
    }
}
