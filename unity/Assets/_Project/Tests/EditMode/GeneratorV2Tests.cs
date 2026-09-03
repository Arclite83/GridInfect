using System.Collections.Generic;
using System.Text;
using GridInfect.Core.Generation;
using GridInfect.Core.Solving;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Stage 2 acceptance: every accepted board has exactly one solution,
    // the deducer solves it without a guess, all its pieces are needed, the
    // stored solution wins through the real rules, and the canonical hash
    // is the same for the board's mirror images. Golden seeds lock the
    // pipeline against drift, as GeneratorTests does for v1.
    [TestFixture]
    public class GeneratorV2Tests
    {
        static List<GeneratedLevel> FirstAccepted(GenSpec spec, int count, ulong from = 1)
        {
            var levels = new List<GeneratedLevel>();
            for (ulong seed = from; levels.Count < count && seed < from + 2000; seed++)
            {
                var level = GeneratorV2.Generate(spec, seed);
                if (level != null) levels.Add(level);
            }
            Assert.That(levels.Count, Is.EqualTo(count), "not enough accepted boards in the seed range");
            return levels;
        }

        [Test]
        public void AcceptedBoardsAreUniqueDeducibleAndPlayable()
        {
            foreach (var level in FirstAccepted(new GenSpec(), 20))
            {
                var def = level.Def;
                Assert.That(SolutionCounter.Count(def), Is.EqualTo(1), $"seed {level.Seed}: solution count");
                var solve = Deducer.Solve(def);
                Assert.That(solve.Solved, Is.True, $"seed {level.Seed}: not solved by deduction");
                Assert.That(solve.Guesses, Is.EqualTo(0), $"seed {level.Seed}: guessed");
                Assert.That(solve.Placements.Length, Is.EqualTo(def.Pieces.Length), $"seed {level.Seed}: decoy piece");
                Assert.That(level.Solution.Length, Is.EqualTo(def.Pieces.Length), $"seed {level.Seed}: stored solution size");
                Assert.That(SolutionCounter.Wins(def, level.Solution), Is.True, $"seed {level.Seed}: stored solution does not win");
                Assert.That(Grader.Grade(solve), Is.EqualTo(level.Grade), $"seed {level.Seed}: grade");
                Assert.That(level.Hash, Is.EqualTo(Canonical.Hash(def)), $"seed {level.Seed}: hash");
            }
        }

        [Test]
        public void SameSeedSameBytes()
        {
            var spec = new GenSpec();
            var first = FirstAccepted(spec, 3);
            foreach (var level in first)
            {
                var again = GeneratorV2.Generate(spec, level.Seed);
                Assert.That(Encode(again), Is.EqualTo(Encode(level)), $"seed {level.Seed}");
            }
        }

        [Test]
        public void CanonicalHashIgnoresFlipsAndTrayOrder()
        {
            foreach (var level in FirstAccepted(new GenSpec(), 5))
            {
                var def = level.Def;
                var board = new byte[Grid.Cells];
                var tiles = new Tile[def.Pieces.Length];
                // Mirror left-right, remap arms, reverse the tray.
                for (int i = 0; i < Grid.Height; i++)
                {
                    for (int j = 0; j < Grid.Width; j++) board[Grid.Loc(i, j)] = def.BoardAt(Grid.Loc(i, Grid.Width - 1 - j));
                }
                for (int k = 0; k < tiles.Length; k++)
                {
                    int mask = TileArms.Mask(def.Pieces[tiles.Length - 1 - k]);
                    int l = mask & 1, r = (mask >> 1) & 1;
                    mask = (mask & ~3) | (l << 1) | r;
                    tiles[k] = TileArms.FromMask(mask);
                }
                var mirrored = new LevelDef(board, tiles);
                Assert.That(Canonical.Hash(mirrored), Is.EqualTo(level.Hash), $"seed {level.Seed}");
                Assert.That(SolutionCounter.Count(mirrored), Is.EqualTo(1), $"seed {level.Seed}: mirror is still unique");
            }
        }

        [Test]
        public void GapsCarveModeAlsoProducesAcceptedBoards()
        {
            var spec = new GenSpec { MinPieces = 2, MaxPieces = 3 };
            spec.Carve.Mode = CarveMode.Gaps;
            var levels = FirstAccepted(spec, 3);
            foreach (var level in levels) Assert.That(SolutionCounter.Count(level.Def), Is.EqualTo(1));
        }

        [Test]
        public void RejectionsAreReported()
        {
            var spec = new GenSpec { MinGrade = Grade.G5, MaxGrade = Grade.G5, MinPieces = 2, MaxPieces = 2 };
            int seen = 0;
            for (ulong seed = 1; seed < 40; seed++)
            {
                GeneratorV2.Generate(spec, seed, out Rejection why);
                if (why != Rejection.None) seen++;
            }
            Assert.That(seen, Is.GreaterThan(30), "two-piece boards are never G5, so nearly every seed is rejected with a reason");
        }

        [TestCase(1ul, GeneratorV2Goldens.Seed1)]
        [TestCase(2ul, GeneratorV2Goldens.Seed2)]
        [TestCase(3ul, GeneratorV2Goldens.Seed3)]
        public void GoldenSeedsAreStable(ulong seed, string expected)
        {
            var level = GeneratorV2.Generate(new GenSpec(), seed);
            Assert.That(Encode(level), Is.EqualTo(expected));
        }

        [Test, Explicit("run once to (re)capture the golden values above")]
        public void CaptureGoldenSeeds()
        {
            for (ulong seed = 1; seed <= 3; seed++)
            {
                TestContext.Out.WriteLine($"GOLDEN {seed}: {Encode(GeneratorV2.Generate(new GenSpec(), seed))}");
            }
        }

        // board|pieces|grade|solution or "rejected".
        internal static string Encode(GeneratedLevel level)
        {
            if (level == null) return "rejected";
            var sb = new StringBuilder();
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                byte v = level.Def.BoardAt(loc);
                sb.Append(v == Cell.Void ? '.' : (char)('0' + v));
            }
            sb.Append('|').Append(string.Join(",", level.Def.Pieces));
            sb.Append('|').Append(level.Grade);
            sb.Append('|');
            for (int n = 0; n < level.Solution.Length; n++)
            {
                if (n > 0) sb.Append(' ');
                sb.Append(level.Solution[n].piece).Append('@').Append(level.Solution[n].cell);
            }
            return sb.ToString();
        }
    }

    internal static class GeneratorV2Goldens
    {
        public const string Seed1 = "rejected";
        public const string Seed2 = "1.....112.............................112...1.....1.....2.........|RD,RU|G1|0@38 1@6";
        public const string Seed3 = ".....2.....11111111....11....11112.11...111....12....2............|RD,L,LUD|G1|0@12 1@32 2@41";
    }
}
