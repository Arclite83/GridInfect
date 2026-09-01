using System;
using Bloodhound.Engine;

namespace GridInfect.Core
{
    // Exact port of LevelBuilder.cpp (docs/GENERATOR.md) with libc rand()
    // replaced by seeded Pcg32, conjugated by the board transpose (Grid): the
    // sampling windows swap axes, the carve walks TileArms.SpreadOrder, and
    // the missing-margin quirk travels from UD to LR. Generated boards
    // therefore differ from the landscape build for the same seed — Free Play
    // is procedural, so that is a reshape, not a regression.
    //
    // Draw order is contract: every rejected sample still consumes its three
    // draws, carving one draw per in-bounds cell — golden tests lock the
    // sequences. Original quirks kept: the LR window shrink is missing its
    // right margin, carving never stops early, and the rejection loop is
    // unbounded.
    public static class LevelGenerator
    {
        struct Config
        {
            public int Pieces, XOffset, XCount, YOffset, YCount;
        }

        // LevelBuilder.cpp's sampling windows, with x and y swapped: the board
        // is transposed from the original's 11x6 (Grid), so the original's
        // column window is now the row window and vice versa.
        static readonly Config[] Configs =
        {
            // Difficulty ordinal order
            new Config { Pieces = 2, XOffset = 1, XCount = 5, YOffset = 3, YCount = 5 },
            new Config { Pieces = 3, XOffset = 1, XCount = 5, YOffset = 3, YCount = 6 },
            new Config { Pieces = 4, XOffset = 0, XCount = 6, YOffset = 2, YCount = 7 },
            new Config { Pieces = 4, XOffset = 0, XCount = 6, YOffset = 0, YCount = 11 },
            new Config { Pieces = 5, XOffset = 0, XCount = 6, YOffset = 0, YCount = 11 },
        };

        public static LevelDef Generate(Difficulty difficulty, ref Pcg32 rng)
        {
            return Generate(difficulty, ref rng, out _);
        }

        // Also returns the sampled solution the original discards — placing
        // every piece at its sampled cell always wins (GENERATOR §5), which
        // the tests use to cross-verify generator and rules.
        public static LevelDef Generate(Difficulty difficulty, ref Pcg32 rng, out (int i, int j)[] solution)
        {
            Config config = Configs[(int)difficulty];
            // Row *and* column exclusivity, so the short edge is what binds —
            // the original guarded on rows because 6 was its short edge.
            if (config.Pieces > Math.Min(Grid.Width, Grid.Height))
                throw new InvalidOperationException("more pieces than the short edge makes the rejection loop infinite (GENERATOR §7)");

            var board = new byte[Grid.Cells];
            var tiles = new Tile[config.Pieces];
            var pi = new int[config.Pieces];
            var pj = new int[config.Pieces];

            for (int n = 0; n < config.Pieces; n++)
            {
                while (true)
                {
                    var tile = (Tile)rng.Next(15);                       // draw 1
                    bool overlap = false;

                    if (difficulty == Difficulty.Beginner && TileArms.Count(tile) >= 3) overlap = true;
                    if (difficulty == Difficulty.Challenging && (tile == Tile.LR || tile == Tile.UD)) overlap = true;
                    for (int k = 0; k < n; k++)
                    {
                        if (tiles[k] == tile) overlap = true;
                    }

                    int xOffset = config.XOffset, xCount = config.XCount;
                    int yOffset = config.YOffset, yCount = config.YCount;
                    ShrinkWindow(tile, ref xOffset, ref xCount, ref yOffset, ref yCount);

                    int x = xOffset + rng.Next(xCount);                  // draws 2 and 3,
                    int y = yOffset + rng.Next(yCount);                  // consumed even when rejected

                    for (int k = 0; k < n; k++)
                    {
                        if (pi[k] == y || pj[k] == x) overlap = true;    // row/column exclusivity
                    }

                    if (overlap) continue;

                    tiles[n] = tile;
                    pi[n] = y;
                    pj[n] = x;
                    Carve(board, tile, y, x, ref rng);
                    break;
                }
            }

            solution = new (int, int)[config.Pieces];
            for (int n = 0; n < config.Pieces; n++) solution[n] = (pi[n], pj[n]);

            return new LevelDef(board, tiles);
        }

        static void ShrinkWindow(Tile tile, ref int xOffset, ref int xCount, ref int yOffset, ref int yCount)
        {
            switch (tile)
            {
                case Tile.L: xOffset += 2; xCount -= 2; break;
                case Tile.R: xCount -= 2; break;
                case Tile.U: yOffset += 2; yCount -= 2; break;
                case Tile.D: yCount -= 2; break;
                case Tile.LR: xOffset += 2; xCount -= 2; break; // original bug kept: no right margin (was UD)
                case Tile.LU: xOffset += 2; xCount -= 2; yOffset += 2; yCount -= 2; break;
                case Tile.LD: xOffset += 2; xCount -= 2; yCount -= 2; break;
                case Tile.RU: xCount -= 2; yOffset += 2; yCount -= 2; break;
                case Tile.RD: xCount -= 2; yCount -= 2; break;
                case Tile.UD: yOffset += 2; yCount -= 4; break;
                case Tile.LRU: xOffset += 2; xCount -= 4; yOffset += 2; yCount -= 2; break;
                case Tile.LRD: xOffset += 2; xCount -= 4; yCount -= 2; break;
                case Tile.LUD: xOffset += 2; xCount -= 2; yOffset += 2; yCount -= 4; break;
                case Tile.RUD: xCount -= 2; yOffset += 2; yCount -= 4; break;
                case Tile.LRUD: xOffset += 2; xCount -= 4; yOffset += 2; yCount -= 4; break;
            }
            if (xCount <= 0 || yCount <= 0)
                throw new InvalidOperationException($"sampling window collapsed for {tile} (GENERATOR §3)");
        }

        // LevelBuilder::buildBoard — activate iff (draw%20) - offset > 4;
        // failed rolls leave gaps the walk continues past, out-of-bounds
        // cells are skipped before the draw
        static void Carve(byte[] board, Tile tile, int pieceI, int pieceJ, ref Pcg32 rng)
        {
            board[Grid.Loc(pieceI, pieceJ)] = Cell.Active;

            for (int offset = 1; offset <= Grid.SpreadRange; offset++)
            {
                for (int n = 0; n < TileArms.SpreadOrder.Length; n++)
                {
                    Dir dir = TileArms.SpreadOrder[n];
                    if (!TileArms.Has(tile, dir)) continue;

                    int i = pieceI + TileArms.Di(dir) * offset;
                    int j = pieceJ + TileArms.Dj(dir) * offset;
                    if (!Grid.InBounds(i, j)) continue;

                    if (rng.Next(20) - offset > 4)
                    {
                        board[Grid.Loc(i, j)] = Cell.Active;
                    }
                }
            }
        }
    }
}
