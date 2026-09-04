using System;
using System.Collections.Generic;
using Bloodhound.Engine;
using GridInfect.Core.Solving;

namespace GridInfect.Core.Generation
{
    // Generator v2 (docs/GENERATOR_V2.md §Pipeline): sample a solution and
    // carve its fill (this file), then hand it to the Constructor, which
    // subtracts givens to a unique, minimal, graded level. Deterministic:
    // every draw comes from Pcg32(seed) in a fixed order, and nothing else
    // in the pipeline depends on iteration order of a hash container.
    public static class GeneratorV2
    {
        public static GeneratedLevel Generate(GenSpec spec, ulong seed) => Generate(spec, seed, out _);

        public static GeneratedLevel Generate(GenSpec spec, ulong seed, out Rejection rejection, List<string> log = null)
        {
            var sample = Sample(spec, seed, out rejection);
            return sample == null ? null : Constructor.Build(sample, spec, seed, out rejection, log);
        }

        // The sampler: tiles and cells under the pairing rules (distinct
        // tiles, no symmetric slider, exclusive lines, a minimum distance),
        // element decoration, the carve, relays, and the maximal given set:
        // a wall past every run end where the sample still solves the board.
        public static Sample Sample(GenSpec spec, ulong seed, out Rejection rejection)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            var rng = new Pcg32(seed);
            rejection = Rejection.None;

            int count = spec.MinPieces + (spec.MaxPieces > spec.MinPieces ? rng.Next(spec.MaxPieces - spec.MinPieces + 1) : 0);
            var tiles = new Tile[count];
            var specs = new PieceSpec[count];
            var cells = new int[count];
            for (int n = 0; n < count; n++)
            {
                int tries = 0;
                while (true)
                {
                    var tile = (Tile)rng.Next(15);
                    int cell = rng.Next(Grid.Cells);
                    bool clash = !spec.AllowSymmetricTiles && (tile == Tile.UD || tile == Tile.LR);
                    // Every arm needs at least one cell to reach.
                    for (int d = 0; d < 4; d++)
                    {
                        var dir = (Dir)d;
                        if (TileArms.Has(tile, dir) && !Grid.InBounds(cell / Grid.Width + TileArms.Di(dir), cell % Grid.Width + TileArms.Dj(dir))) clash = true;
                    }
                    for (int k = 0; k < n; k++)
                    {
                        if (cells[k] == cell) clash = true;
                        if (!spec.AllowDuplicateTiles && tiles[k] == tile && !specs[k].Area) clash = true;
                        int ki = cells[k] / Grid.Width, kj = cells[k] % Grid.Width;
                        int ci = cell / Grid.Width, cj = cell % Grid.Width;
                        if (spec.ExclusiveLines && (ki == ci || kj == cj)) clash = true;
                        if (Math.Abs(ki - ci) + Math.Abs(kj - cj) < spec.MinPieceDistance) clash = true;
                    }
                    if (!clash)
                    {
                        // Decoration draws only after the sample is accepted, so
                        // element-free specs keep their draw sequence. A diagonal
                        // arm with no cell to reach from this corner is dropped.
                        var decorated = Decorate(PieceSpec.FromTile(tile), spec, ref rng);
                        for (int d = 4; d < 8; d++)
                        {
                            var dir = (Dir)d;
                            if (decorated.Has(dir) && !Grid.InBounds(cell / Grid.Width + TileArms.Di(dir), cell % Grid.Width + TileArms.Dj(dir)))
                                decorated = decorated.WithArm(dir, false);
                        }
                        if (!spec.AllowDuplicateTiles && decorated.Area)
                        {
                            bool secondBlot = false;
                            for (int k = 0; k < n; k++) secondBlot |= specs[k].Area;
                            if (secondBlot) decorated = PieceSpec.FromTile(tile);   // one blot per board
                        }
                        tiles[n] = tile;
                        specs[n] = decorated;
                        cells[n] = cell;
                        break;
                    }
                    if (++tries > 200) { rejection = Rejection.Tiles; return null; }
                }
            }
            var board = new byte[Grid.Cells];
            var cellData = new byte[Grid.Cells];
            var sampled = new int[count];
            for (int n = 0; n < count; n++) sampled[n] = n * Grid.Cells + cells[n];
            var endWalls = new List<int>();
            var carved = new List<int>();
            int relays = 0;
            for (int n = 0; n < count; n++)
            {
                carved.Clear();
                Carve(board, specs[n], cells[n], spec.Carve, ref rng, endWalls, carved);
                // A relay (stage 12): one carved cell on this piece's arms
                // gets arms of its own, carved as runs like a piece's.
                if ((spec.Elements & Element.Relays) != 0 && carved.Count > 0 && rng.Next(20) < spec.RelayChance)
                {
                    int at = carved[rng.Next(carved.Count)];
                    if (cellData[at] == 0)
                    {
                        int arms = 0, armCount = rng.Next(3) == 0 ? 2 : 1;
                        int dirs = (spec.Elements & Element.Diagonals) != 0 ? 8 : 4;
                        for (int a = 0; a < armCount; a++) arms |= 1 << rng.Next(dirs);
                        var relaySpec = new PieceSpec((byte)arms);
                        cellData[at] = (byte)arms;
                        Carve(board, relaySpec, at, spec.Carve, ref rng, endWalls, null);
                        relays++;
                    }
                }
            }

            int active = 0;
            for (int loc = 0; loc < Grid.Cells; loc++) if (board[loc] == Cell.Active) active++;
            if (active < spec.Carve.MinActive || active > spec.Carve.MaxActive) { rejection = Rejection.Size; return null; }

            // The maximal given set: a wall past every run end, kept where
            // the sampled solution still covers the board (a wall on a void
            // another arm jumps would not). The constructor withdraws the
            // ones its uniqueness proof does not need.
            if ((spec.Elements & Element.Walls) != 0)
            {
                foreach (int w in endWalls)
                {
                    if (board[w] != Cell.Void) continue;
                    board[w] = Cell.Wall;
                    var map = new LineMap(new LevelDef(board, specs, cellData));
                    if (!Covers(map, sampled, w) || !Constructor.ArmsUseful(map, sampled)) board[w] = Cell.Void;
                }
            }
            if (!Constructor.ArmsUseful(new LineMap(new LevelDef(board, specs, cellData)), sampled)) { rejection = Rejection.Size; return null; }

            return new Sample { Specs = specs, Cells = cells, Board = board, CellData = cellData, Relays = relays };
        }

        // Element decoration of a sampled tile. Draws happen only for the
        // elements the spec turns on, so classic specs keep their goldens.
        static PieceSpec Decorate(PieceSpec piece, GenSpec spec, ref Pcg32 rng)
        {
            if ((spec.Elements & Element.Area) != 0 && rng.Next(20) < spec.AreaChance)
            {
                return new PieceSpec(0, 0, area: true);   // the blot: 3x3, no arms
            }
            if ((spec.Elements & Element.Diagonals) != 0 && rng.Next(20) < spec.DiagonalChance)
            {
                // The curated set: a tile plus one diagonal arm, or two
                // diagonal arms that are not an opposite pair (an
                // opposite-only pair slides along its line like UD does).
                int count = rng.Next(3) == 0 ? 2 : 1;
                for (int n = 0; n < count; n++)
                {
                    var diag = (Dir)(4 + rng.Next(4));
                    piece = piece.WithArm(diag);
                }
                if ((piece.Arms & 0x0F) == 0 && (piece.Arms == 0x90 || piece.Arms == 0x60))
                {
                    piece = piece.WithArm(Dir.UL, false).WithArm(Dir.DR, false).WithArm(Dir.UR, false).WithArm(Dir.DL, false)
                        .WithArm((Dir)(4 + rng.Next(4)));
                }
            }
            if ((spec.Elements & Element.ShortArms) != 0)
            {
                for (int d = 0; d < 8; d++)
                {
                    var dir = (Dir)d;
                    if (!piece.Has(dir)) continue;
                    if (rng.Next(20) < spec.ShortArmChance) piece = piece.WithReach(dir, 1 + rng.Next(2));
                }
            }
            return piece;
        }

        // Gaps: as LevelGenerator does — walk each arm outward, one draw per
        // in-bounds cell, gaps allowed — with the chance curve from CarveParams.
        // Runs: each arm draws a length and activates that many in-bounds
        // cells; the cell past the run is recorded as an end wall.
        static void Carve(byte[] board, PieceSpec spec, int cell, CarveParams carve, ref Pcg32 rng, List<int> endWalls, List<int> carved)
        {
            board[cell] = Cell.Active;
            int pi = cell / Grid.Width, pj = cell % Grid.Width;
            if (spec.Area)
            {
                // The blot's neighbourhood: each in-bounds neighbour with the
                // ring-1 chance, so a blot leaves a blob, not always a full square.
                for (int di = -1; di <= 1; di++)
                {
                    for (int dj = -1; dj <= 1; dj++)
                    {
                        if (di == 0 && dj == 0) continue;
                        if (!Grid.InBounds(pi + di, pj + dj)) continue;
                        if (rng.Next(20) < carve.ChanceAt(1)) board[Grid.Loc(pi + di, pj + dj)] = Cell.Active;
                    }
                }
            }
            if (carve.Mode == CarveMode.Gaps)
            {
                for (int offset = 1; offset <= Grid.SpreadRange; offset++)
                {
                    for (int n = 0; n < TileArms.SpreadOrderV2.Length; n++)
                    {
                        Dir dir = TileArms.SpreadOrderV2[n];
                        if (!spec.Has(dir)) continue;
                        int i = pi + TileArms.Di(dir) * offset;
                        int j = pj + TileArms.Dj(dir) * offset;
                        if (!Grid.InBounds(i, j)) continue;
                        if (spec.ReachOf(dir) != 0 && offset > spec.ReachOf(dir)) continue;
                        if (rng.Next(20) < carve.ChanceAt(offset))
                        {
                            board[Grid.Loc(i, j)] = Cell.Active;
                            carved?.Add(Grid.Loc(i, j));
                        }
                    }
                }
                return;
            }
            for (int n = 0; n < TileArms.SpreadOrderV2.Length; n++)
            {
                Dir dir = TileArms.SpreadOrderV2[n];
                if (!spec.Has(dir)) continue;
                int run = carve.MinRun + (carve.MaxRun > carve.MinRun ? rng.Next(carve.MaxRun - carve.MinRun + 1) : 0);
                if (spec.ReachOf(dir) != 0 && run > spec.ReachOf(dir)) run = spec.ReachOf(dir);   // a short arm carves no further than it reaches
                int offset = 1;
                for (; offset <= run; offset++)
                {
                    int i = pi + TileArms.Di(dir) * offset;
                    int j = pj + TileArms.Dj(dir) * offset;
                    if (!Grid.InBounds(i, j)) break;
                    board[Grid.Loc(i, j)] = Cell.Active;
                    carved?.Add(Grid.Loc(i, j));
                }
                int ei = pi + TileArms.Di(dir) * offset, ej = pj + TileArms.Dj(dir) * offset;
                if (Grid.InBounds(ei, ej)) endWalls.Add(Grid.Loc(ei, ej));
            }
        }

        static bool Covers(LineMap map, int[] set, int wall)
        {
            var covered = CellMask.None;
            foreach (int p in set)
            {
                int piece = p / Grid.Cells, cell = p % Grid.Cells;
                if (cell == wall) return false;
                covered |= map.Coverage(map.Def.Specs[piece], cell);
            }
            return covered.Contains(map.ActiveMask);
        }
    }
}
