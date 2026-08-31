using Bloodhound.Engine;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Two load-bearing checks: generated levels are winnable through the real
    // rules (GENERATOR.md §5 by construction), and golden seeds lock the
    // generator against drift — a diff here changes every player's boards.
    // These values were recaptured when the board was transposed to portrait
    // (Grid): a different board shape is a different generator domain, so the
    // same seed legitimately yields a different level.
    [TestFixture]
    public class GeneratorTests
    {
        static readonly Difficulty[] AllDifficulties =
        {
            Difficulty.Beginner, Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Challenging
        };

        [Test]
        public void SampledSolutionAlwaysWinsThroughTheRealRules()
        {
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

        [TestCase(Difficulty.Beginner, ".........................1111....1.....1.....1....................|D,L")]
        [TestCase(Difficulty.Easy, "..1.1...1.....1.1....11...111.11111...1.....1.....1......11.......|D,LUD,U")]
        [TestCase(Difficulty.Medium, "............111.1..111111..111.....1...1.....1.....1.1............|D,L,R,LD")]
        [TestCase(Difficulty.Hard, ".....1..1..1..11....11.1...1.1..1111....11..1111..1111..1.11..1.1.|D,RUD,LD,UD")]
        [TestCase(Difficulty.Challenging, ".1.....11..1..11...111...11111..11111.1111..111...111...1.1...1.1.|D,RUD,LD,RU,LU")]
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
