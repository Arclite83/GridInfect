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
                        if (!spec.AllowDuplicateTiles && tiles[k] == tile) clash = true;
                        int ki = cells[k] / Grid.Width, kj = cells[k] % Grid.Width;
                        int ci = cell / Grid.Width, cj = cell % Grid.Width;
                        if (spec.ExclusiveLines && (ki == ci || kj == cj)) clash = true;
                        if (Math.Abs(ki - ci) + Math.Abs(kj - cj) < spec.MinPieceDistance) clash = true;
                    }
                    if (!clash) { tiles[n] = tile; specs[n] = PieceSpec.FromTile(tile); cells[n] = cell; break; }
                    if (++tries > 200) { rejection = Rejection.Tiles; return null; }
                }
            }
            var board = new byte[Grid.Cells];
            var sampled = new int[count];
            for (int n = 0; n < count; n++) sampled[n] = n * Grid.Cells + cells[n];
            var endWalls = new List<int>();
            for (int n = 0; n < count; n++) Carve(board, specs[n], cells[n], spec.Carve, ref rng, endWalls);

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
                    var map = new LineMap(new LevelDef(board, specs));
                    if (Covers(map, sampled, w) && ArmsUseful(map, sampled)) walls++;
                    else board[w] = Cell.Void;
                }
            }

            var def = new LevelDef(board, specs);

            // 2. Prune with walls until unique.
            int steps = 0;
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
                if (steps >= spec.MaxPruneSteps || walls >= spec.MaxWalls || (spec.Elements & Element.Walls) == 0)
                {
                    rejection = Rejection.NotUnique;
                    return null;
                }
                int wall = ChooseWall(def, sampled, fast, out int after);
                if (wall < 0) { log?.Add("  no wall reduces the count"); rejection = Rejection.NotUnique; return null; }
                board[wall] = Cell.Wall;
                def = new LevelDef(board, specs);
                walls++;
                steps++;
                fast = after;
                log?.Add($"  wall {steps} at ({wall / Grid.Width},{wall % Grid.Width}) -> {fast} covers [{watch.ElapsedMilliseconds} ms]");
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
                PruneSteps = steps,
            };
        }

        // Gaps: as LevelGenerator does — walk each arm outward, one draw per
        // in-bounds cell, gaps allowed — with the chance curve from CarveParams.
        // Runs: each arm draws a length and activates that many in-bounds
        // cells; one more draw decides an end wall (recorded, applied later).
        static void Carve(byte[] board, PieceSpec spec, int cell, CarveParams carve, ref Pcg32 rng, List<int> endWalls)
        {
            board[cell] = Cell.Active;
            int pi = cell / Grid.Width, pj = cell % Grid.Width;
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
                        if (rng.Next(20) < carve.ChanceAt(offset)) board[Grid.Loc(i, j)] = Cell.Active;
                    }
                }
                return;
            }
            for (int n = 0; n < TileArms.SpreadOrderV2.Length; n++)
            {
                Dir dir = TileArms.SpreadOrderV2[n];
                if (!spec.Has(dir)) continue;
                int run = carve.MinRun + (carve.MaxRun > carve.MinRun ? rng.Next(carve.MaxRun - carve.MinRun + 1) : 0);
                int offset = 1;
                for (; offset <= run; offset++)
                {
                    int i = pi + TileArms.Di(dir) * offset;
                    int j = pj + TileArms.Dj(dir) * offset;
                    if (!Grid.InBounds(i, j)) break;
                    board[Grid.Loc(i, j)] = Cell.Active;
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

        static int ChooseWall(LevelDef def, int[] sampled, int current, out int best)
        {
            var sets = SolutionCounter.Sets(def, Math.Min(current, EstimateSets) + 1, out _);
            var board = new byte[Grid.Cells];
            def.CopyBoardTo(board);

            var ranked = new List<(int estimate, int order, int wall, LevelDef walled)>();
            for (int w = 0; w < Grid.Cells; w++)
            {
                byte v = board[w];
                if (v != Cell.Void && v != Cell.Active) continue;
                bool onPiece = false;
                foreach (int p in sampled) if (p % Grid.Cells == w) onPiece = true;
                if (onPiece) continue;

                board[w] = Cell.Wall;
                var walled = new LevelDef(board, def.Specs);
                board[w] = v;
                var walledMap = new LineMap(walled);
                if (!Covers(walledMap, sampled, w) || !ArmsUseful(walledMap, sampled)) continue;

                int survivors = 0;
                foreach (int[] set in sets) if (Covers(walledMap, set, w)) survivors++;
                if (survivors >= current) continue;
                ranked.Add((survivors, (v == Cell.Void ? 0 : Grid.Cells) + w, w, walled));
            }
            ranked.Sort((x, y) => x.estimate != y.estimate ? x.estimate.CompareTo(y.estimate) : x.order.CompareTo(y.order));

            int bestWall = -1;
            best = current;
            for (int n = 0; n < ranked.Count && n < ExactCandidates; n++)
            {
                int count = SolutionCounter.CountFast(ranked[n].walled, best);
                if (count >= best) continue;
                bestWall = ranked[n].wall;
                best = count;
                if (best == 1) break;
            }
            return bestWall;
        }

        // Every arm of every sampled piece still reaches at least one active
        // cell: a wall that blinds an arm turns the tile into a smaller one
        // and hands the level a swap ambiguity nothing can prune.
        static bool ArmsUseful(LineMap map, int[] set)
        {
            foreach (int p in set)
            {
                var spec = map.Def.Specs[p / Grid.Cells];
                int cell = p % Grid.Cells;
                for (int d = 0; d < 8; d++)
                {
                    var dir = (Dir)d;
                    if (!spec.Has(dir)) continue;
                    int f = map.Families.Length;
                    bool reaches = false;
                    for (int fam = 0; fam < f && !reaches; fam++)
                    {
                        var family = map.Families[fam];
                        if (family.Pos != dir && family.Neg != dir) continue;
                        int id = map.LineOf[fam][cell];
                        if (id < 0) break;
                        var line = map.Lines[fam][id];
                        int at = map.IndexIn[fam][cell];
                        if (family.Pos == dir)
                        {
                            for (int n = at + 1; n < line.Cells.Length; n++) if (map.Def.BoardAt(line.Cells[n]) == Cell.Active) { reaches = true; break; }
                        }
                        else
                        {
                            for (int n = at - 1; n >= 0; n--) if (map.Def.BoardAt(line.Cells[n]) == Cell.Active) { reaches = true; break; }
                        }
                    }
                    if (!reaches) return false;
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
