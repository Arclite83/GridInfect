using Bloodhound.Engine;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    /// <summary>
    /// Generator port verification (GENERATOR.md): structural invariants for
    /// every difficulty, seed determinism, the by-construction solvability
    /// proof (§5) replayed through the real rules, and golden boards locked
    /// against accidental drift (REQUIREMENTS R-403).
    /// </summary>
    [TestFixture]
    public class GeneratorTests
    {
        static readonly Difficulty[] AllDifficulties =
        {
            Difficulty.Beginner, Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Challenging
        };

        static readonly int[] ExpectedPieceCounts = { 2, 3, 4, 4, 5 };

        [Test]
        public void SameSeedSameLevel()
        {
            foreach (var difficulty in AllDifficulties)
            {
                var rngA = new Pcg32(12345);
                var rngB = new Pcg32(12345);
                var a = LevelGenerator.Generate(difficulty, ref rngA);
                var b = LevelGenerator.Generate(difficulty, ref rngB);
                Assert.That(BoardText(a), Is.EqualTo(BoardText(b)), $"{difficulty}: board");
                Assert.That(a.Pieces, Is.EqualTo(b.Pieces), $"{difficulty}: pieces");
            }
        }

        [Test]
        public void StructuralInvariantsHoldAcrossSeeds()
        {
            foreach (var difficulty in AllDifficulties)
            {
                for (ulong seed = 0; seed < 25; seed++)
                {
                    var rng = new Pcg32(seed);
                    var def = LevelGenerator.Generate(difficulty, ref rng, out var solution);

                    Assert.That(def.Pieces.Length, Is.EqualTo(ExpectedPieceCounts[(int)difficulty]),
                        $"{difficulty} seed {seed}: piece count");

                    for (int a = 0; a < def.Pieces.Length; a++)
                    {
                        for (int b = a + 1; b < def.Pieces.Length; b++)
                        {
                            Assert.That(def.Pieces[a], Is.Not.EqualTo(def.Pieces[b]),
                                $"{difficulty} seed {seed}: duplicate tile");
                            Assert.That(solution[a].i, Is.Not.EqualTo(solution[b].i),
                                $"{difficulty} seed {seed}: shared row");
                            Assert.That(solution[a].j, Is.Not.EqualTo(solution[b].j),
                                $"{difficulty} seed {seed}: shared column");
                        }
                    }

                    foreach (var tile in def.Pieces)
                    {
                        if (difficulty == Difficulty.Beginner)
                            Assert.That(TileArms.Count(tile), Is.LessThanOrEqualTo(2),
                                $"Beginner seed {seed}: {tile} has too many arms");
                        if (difficulty == Difficulty.Challenging)
                            Assert.That(tile, Is.Not.EqualTo(Tile.LR).And.Not.EqualTo(Tile.UD),
                                $"Challenging seed {seed}: restricted tile {tile}");
                    }

                    for (int loc = 0; loc < Grid.Cells; loc++)
                    {
                        Assert.That(def.BoardAt(loc), Is.EqualTo(Cell.Void).Or.EqualTo(Cell.Active),
                            $"{difficulty} seed {seed}: generated boards contain only 0/1");
                    }
                }
            }
        }

        [Test]
        public void SampledSolutionAlwaysWinsThroughTheRealRules()
        {
            // GENERATOR.md §5: placing every piece at its sampled cell wins.
            // This cross-checks generator and rules against each other.
            foreach (var difficulty in AllDifficulties)
            {
                for (ulong seed = 100; seed < 120; seed++)
                {
                    var rng = new Pcg32(seed);
                    var def = LevelGenerator.Generate(difficulty, ref rng, out var solution);
                    var session = new LevelSession(def);
                    for (int k = 0; k < solution.Length; k++)
                    {
                        Assert.That(Rules.CanPlace(session, k, solution[k].i, solution[k].j), Is.True,
                            $"{difficulty} seed {seed}: sampled placement {k} illegal");
                        Rules.SetPiece(session, k, solution[k].i, solution[k].j);
                        Rules.Resolve(session);
                    }
                    Assert.That(session.Solved, Is.True, $"{difficulty} seed {seed}: sampled solution did not win");
                }
            }
        }

        [Test]
        public void GenerateActionIsDeterministicUnderLogReplay()
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            Assert.That(dispatcher.Dispatch(GridInfectActions.LevelGenerate,
                Inputs.LevelGenerate(Difficulty.Medium, seed: 777)).Applied);
            var boards = new string[dispatcher.State.FreePlayDefs.Length];
            for (int n = 0; n < boards.Length; n++) boards[n] = BoardText(dispatcher.State.FreePlayDefs[n]);

            var replayed = GridInfectActions.CreateDispatcher();
            replayed.Replay(dispatcher.Log.Entries);
            for (int n = 0; n < boards.Length; n++)
            {
                Assert.That(BoardText(replayed.State.FreePlayDefs[n]), Is.EqualTo(boards[n]), $"level {n} diverged");
            }
        }

        // Golden boards: captured once from this implementation (seed = 42,
        // one level per difficulty) and locked. A diff here means generation
        // changed for every player — bump intentionally or fix the regression.
        [TestCase(Difficulty.Beginner, ".........................1111.........1..........1..........1.....|D,L")]
        [TestCase(Difficulty.Easy, "...............11..111.....1111........1..........1..........1....|D,R,L")]
        [TestCase(Difficulty.Medium, "......1....111.1.1......1...1....111.11.......1..........1..1.....|D,UD,L,LD")]
        [TestCase(Difficulty.Hard, "....1......1.1.1.......11.1.1..1.......1..1.......1..........1..1.|D,UD,L,LD")]
        [TestCase(Difficulty.Challenging, ".1..1..1...111.11111.1.1..1.1111..1....111...1.....1....11.11.1...|D,RUD,R,LRD,LU")]
        public void GoldenBoardsAreStable(Difficulty difficulty, string expected)
        {
            var rng = new Pcg32(42);
            var def = LevelGenerator.Generate(difficulty, ref rng);
            Assert.That(BoardText(def) + "|" + PieceText(def), Is.EqualTo(expected));
        }

        [Test, Explicit("run once to (re)capture the golden values above")]
        public void CaptureGoldenBoards()
        {
            foreach (var difficulty in AllDifficulties)
            {
                var rng = new Pcg32(42);
                var def = LevelGenerator.Generate(difficulty, ref rng);
                TestContext.Out.WriteLine($"GOLDEN {difficulty}: {BoardText(def)}|{PieceText(def)}");
            }
        }

        static string BoardText(LevelDef def)
        {
            var chars = new char[Grid.Cells];
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                chars[loc] = def.BoardAt(loc) == Cell.Void ? '.' : (char)('0' + def.BoardAt(loc));
            }
            return new string(chars);
        }

        static string PieceText(LevelDef def) => string.Join(",", def.Pieces);
    }
}
