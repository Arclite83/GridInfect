using System;
using System.Collections.Generic;
using Bloodhound.Engine;
using GridInfect.Core.Solving;

namespace GridInfect.Core.Generation
{
    // Generator v2 (docs/GENERATOR_V2.md §Pipeline): sample a solution,
    // carve, prune with walls to a unique solution, accept only what the
    // deducer solves without a guess, grade, canonicalise. Deterministic:
    // every draw comes from Pcg32(seed) in a fixed order, and nothing else
    // in the pipeline depends on iteration order of a hash container.
    public static class GeneratorV2
    {

        public static GeneratedLevel Generate(GenSpec spec, ulong seed) => Generate(spec, seed, out _);

        public static GeneratedLevel Generate(GenSpec spec, ulong seed, out Rejection rejection, List<string> log = null)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            var rng = new Pcg32(seed);
            rejection = Rejection.None;

            // 1. Sample tiles and cells, carve.
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

            // Carve-time end walls, kept only where the sampled solution still
            // covers the board (a wall on a void another arm jumps would not).
            int walls = 0;
            if ((spec.Elements & Element.Walls) != 0)
            {
                foreach (int w in endWalls)
                {
                    if (board[w] != Cell.Void || walls >= spec.MaxWalls) continue;
                    board[w] = Cell.Wall;
                    var map = new LineMap(new LevelDef(board, specs, cellData));
                    if (Covers(map, sampled, w) && ArmsUseful(map, sampled)) walls++;
                    else board[w] = Cell.Void;
                }
            }

            var def = new LevelDef(board, specs, cellData);
            if (!ArmsUseful(new LineMap(def), sampled)) { rejection = Rejection.Size; return null; }

            // 2. Prune with walls (and forbidden cells, stage 10) until unique.
            int steps = 0, forbidden = 0;
            var watch = log != null ? System.Diagnostics.Stopwatch.StartNew() : null;
            var analysis = SolutionCounter.Analyse(def, spec.SolutionCap);
            log?.Add($"seed {seed}: {count} pieces, {active} active, {analysis.Solutions} solutions{(analysis.Capped ? " (capped)" : "")} [{watch.ElapsedMilliseconds} ms]");
            if (analysis.Capped) { rejection = Rejection.TooMany; return null; }
            // The loop is driven by the fast distinct count; the oracle count
            // has the last word (it can see a non-minimal cover the fast
            // search skips, in which case pruning continues on its number).
            int fast = analysis.Solutions > 1 ? SolutionCounter.CountFast(def, spec.SolutionCap) : 1;
            while (true)
            {
                if (fast <= 1)
                {
                    analysis = SolutionCounter.Analyse(def, spec.SolutionCap);
                    if (analysis.Solutions <= 1) break;
                    fast = analysis.Solutions;
                }
                bool wallsLeft = (spec.Elements & Element.Walls) != 0 && walls < spec.MaxWalls;
                bool forbiddenLeft = (spec.Elements & Element.Forbidden) != 0 && forbidden < spec.MaxForbidden;
                if (steps >= spec.MaxPruneSteps || (!wallsLeft && !forbiddenLeft))
                {
                    rejection = Rejection.NotUnique;
                    return null;
                }
                int wall = ChooseWall(def, sampled, fast, wallsLeft, forbiddenLeft, out int after, out byte kind);
                if (wall < 0) { log?.Add("  no pruner reduces the count"); rejection = Rejection.NotUnique; return null; }
                board[wall] = kind;
                def = def.WithBoard(board);
                if (kind == Cell.Wall) walls++; else forbidden++;
                steps++;
                fast = after;
                log?.Add($"  {(kind == Cell.Wall ? "wall" : "forbidden")} {steps} at ({wall / Grid.Width},{wall % Grid.Width}) -> {fast} covers [{watch.ElapsedMilliseconds} ms]");
            }
            if (analysis.Solutions != 1) { rejection = Rejection.NotUnique; return null; }

            // 3. Deduce.
            var solve = Deducer.Solve(def);
            log?.Add($"  solved={solve.Solved} guesses={solve.Guesses} grade={Grader.Grade(solve)} effort={Grader.Effort(solve)} tiers={string.Join(",", solve.TierCounts)} [{watch.ElapsedMilliseconds} ms]");
            if (!solve.Solved || solve.Guesses != 0) { rejection = Rejection.NotDeducible; return null; }
            if (spec.RequireAllPieces && solve.Placements.Length != count) { rejection = Rejection.Decoy; return null; }

            // 4. Grade.
            var grade = Grader.Grade(solve);
            if (grade < spec.MinGrade || grade > spec.MaxGrade) { rejection = Rejection.Grade; return null; }

            // 5. Emit. The sampled solution is the stored one, in an order
            // that wins (identical to the deduced set when unique).
            var solution = SolutionCounter.WinningOrder(def, sampled) ?? solve.Placements;
            return new GeneratedLevel
            {
                Def = def,
                Solution = solution,
                Trace = solve.Trace,
                Grade = grade,
                Effort = Grader.Effort(solve),
                Seed = seed,
                Hash = Canonical.Hash(def),
                Walls = walls,
                ForbiddenCells = forbidden,
                Relays = relays,
                PruneSteps = steps,
            };
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
        // cells; one more draw decides an end wall (recorded, applied later).
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
                if (rng.Next(20) < carve.EndWallChance && Grid.InBounds(ei, ej)) endWalls.Add(Grid.Loc(ei, ej));
            }
        }

        // The wall that leaves the fewest solutions while the sampled
        // solution still covers the board and keeps every arm useful.
        // Candidates are ranked by a cheap estimate (how many of the current
        // covering sets survive the wall), then the best few are counted
        // (fast distinct count) with the running best as the cap. Ties: void cells first
        // (the puzzle keeps its cells), then lowest. Returns -1 when no wall
        // strictly reduces the count.
        const int ExactCandidates = 2;   // ranked walls counted exactly per step
        const int EstimateSets = 400;    // covers enumerated for the ranking estimate

        // A forbidden cell (stage 10) goes on a void cell the sampled
        // solution never crosses: every placement whose spread would cross
        // it becomes illegal, which prunes harder than a wall there. Ties
        // between the two kinds go to the forbidden cell (it is the world's
        // element); a wall may also sit on an active cell.
        static int ChooseWall(LevelDef def, int[] sampled, int current, bool wallsLeft, bool forbiddenLeft, out int best, out byte kind)
        {
            var sets = SolutionCounter.Sets(def, Math.Min(current, EstimateSets) + 1, out _);
            var board = new byte[Grid.Cells];
            def.CopyBoardTo(board);

            var ranked = new List<(int estimate, int order, int wall, byte kind, LevelDef walled)>();
            for (int w = 0; w < Grid.Cells; w++)
            {
                byte v = board[w];
                if (v != Cell.Void && v != Cell.Active) continue;
                if (def.CellDataAt(w) != 0) continue;   // never wall a relay
                bool onPiece = false;
                foreach (int p in sampled) if (p % Grid.Cells == w) onPiece = true;
                if (onPiece) continue;

                for (int variant = 0; variant < 2; variant++)
                {
                    byte value = variant == 0 ? Cell.Forbidden : Cell.Wall;
                    if (value == Cell.Forbidden && (!forbiddenLeft || v != Cell.Void)) continue;
                    if (value == Cell.Wall && !wallsLeft) continue;

                    board[w] = value;
                    var walled = def.WithBoard(board);
                    board[w] = v;
                    var walledMap = new LineMap(walled);
                    if (!Covers(walledMap, sampled, w) || !ArmsUseful(walledMap, sampled)) continue;
                    if (value == Cell.Forbidden && Illegal(walledMap, sampled)) continue;

                    int survivors = 0;
                    foreach (int[] set in sets)
                    {
                        if (Covers(walledMap, set, w) && !(value == Cell.Forbidden && Illegal(walledMap, set))) survivors++;
                    }
                    if (survivors >= current) continue;
                    ranked.Add((survivors, (v == Cell.Void ? 0 : Grid.Cells) + w, w, value, walled));
                }
            }
            ranked.Sort((x, y) => x.estimate != y.estimate ? x.estimate.CompareTo(y.estimate)
                : x.order != y.order ? x.order.CompareTo(y.order) : y.kind.CompareTo(x.kind));

            int bestWall = -1;
            best = current;
            kind = Cell.Wall;
            for (int n = 0; n < ranked.Count && n < ExactCandidates; n++)
            {
                int count = SolutionCounter.CountFast(ranked[n].walled, best);
                if (count >= best) continue;
                bestWall = ranked[n].wall;
                kind = ranked[n].kind;
                best = count;
                if (best == 1) break;
            }
            return bestWall;
        }

        static bool Illegal(LineMap map, int[] set)
        {
            foreach (int p in set)
            {
                if (map.IsIllegal(map.Def.Specs[p / Grid.Cells], p % Grid.Cells)) return true;
            }
            return false;
        }

        // Every arm of every sampled piece still reaches at least one active
        // cell: a wall that blinds an arm turns the tile into a smaller one
        // and hands the level a swap ambiguity nothing can prune.
        static bool ArmsUseful(LineMap map, int[] set)
        {
            // Every relay's arms reach something beyond the relay itself.
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                byte relay = map.Def.CellDataAt(loc);
                if (relay == 0) continue;
                if (map.Def.BoardAt(loc) != Cell.Active) return false;
                for (int d = 0; d < 8; d++)
                {
                    if ((relay & (1 << d)) == 0) continue;
                    var arm = new PieceSpec(0).WithArm((Dir)d);
                    if (map.Coverage(arm, loc).Count < 2) return false;
                }
            }
            foreach (int p in set)
            {
                var spec = map.Def.Specs[p / Grid.Cells];
                int cell = p % Grid.Cells;
                if (spec.Area && map.Coverage(new PieceSpec(0, 0, true), cell).Count < 2) return false;
                for (int d = 0; d < 8; d++)
                {
                    var dir = (Dir)d;
                    if (!spec.Has(dir)) continue;
                    var arm = new PieceSpec(0).WithArm(dir).WithReach(dir, spec.ReachOf(dir));
                    if (map.Coverage(arm, cell).Count < 2) return false;
                }
            }
            return true;
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
