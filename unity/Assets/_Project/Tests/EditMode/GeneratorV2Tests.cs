using System.Collections.Generic;
using System.Text;
using GridInfect.Core.Generation;
using GridInfect.Core.Solving;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // The constructor's acceptance: every accepted board has exactly one
    // solution, the deducer solves it without a guess inside the depth cap,
    // all its pieces are needed, the stored solution wins through the real
    // rules, every given left on the board is load-bearing, and the
    // canonical hash is the same for the board's mirror images. Golden
    // seeds lock the pipeline against drift, as GeneratorTests does for v1;
    // the 128 classic solutions run through the constructor as a fixture.
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
                var placed = Locked.Placed(def, level.Locks);
                Assert.That(SolutionCounter.Count(def, placed), Is.EqualTo(1), $"seed {level.Seed}: solution count");
                var solve = Deducer.Solve(def, placed);
                Assert.That(solve.Solved, Is.True, $"seed {level.Seed}: not solved by deduction");
                Assert.That(solve.Guesses, Is.EqualTo(0), $"seed {level.Seed}: guessed");
                Assert.That(Grader.EffectiveDepth(solve, def), Is.LessThanOrEqualTo(Depth.Max), $"seed {level.Seed}: depth");
                Assert.That(solve.Placements.Length, Is.EqualTo(def.Pieces.Length), $"seed {level.Seed}: decoy piece");
                Assert.That(level.Solution.Length, Is.EqualTo(def.Pieces.Length), $"seed {level.Seed}: stored solution size");
                for (int n = 0; n < level.Locks.Length; n++)
                {
                    Assert.That(level.Solution[n], Is.EqualTo(level.Locks[n]), $"seed {level.Seed}: locked pieces lead the solution");
                }
                Assert.That(SolutionCounter.Wins(def, level.Solution), Is.True, $"seed {level.Seed}: stored solution does not win");
                Assert.That(Grader.Grade(solve, def), Is.EqualTo(level.Grade), $"seed {level.Seed}: grade");
                Assert.That(level.Hash, Is.EqualTo(Canonical.Hash(def, level.Locks)), $"seed {level.Seed}: hash");
            }
        }

        // Minimality: withdraw any wall, forbidden cell or lock and the level
        // is no longer unique (or the stored solution no longer wins).
        [Test]
        public void EveryGivenIsLoadBearing()
        {
            var spec = new GenSpec { MinPieces = 3, MaxPieces = 5 };
            int walls = 0, locks = 0;
            foreach (var level in FirstAccepted(spec, 12))
            {
                var board = new byte[Grid.Cells];
                level.Def.CopyBoardTo(board);
                for (int loc = 0; loc < Grid.Cells; loc++)
                {
                    byte v = board[loc];
                    if (v != Cell.Wall && v != Cell.Forbidden) continue;
                    board[loc] = Cell.Void;
                    var without = level.Def.WithBoard(board);
                    board[loc] = v;
                    bool stillUnique = SolutionCounter.Count(without, Locked.Placed(without, level.Locks)) == 1
                                       && SolutionCounter.Wins(without, level.Solution);
                    Assert.That(stillUnique, Is.False, $"seed {level.Seed}: cell {loc} carries no information");
                    walls++;
                }
                if (level.Locks.Length > 0)
                {
                    Assert.That(SolutionCounter.Count(level.Def), Is.Not.EqualTo(1), $"seed {level.Seed}: the lock carries no information");
                    locks++;
                }
            }
            TestContext.Out.WriteLine($"checked {walls} blockers and {locks} locks");
            Assert.That(walls, Is.GreaterThan(0));
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
            foreach (var level in FirstAccepted(new GenSpec { MaxLocks = 0 }, 5))
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
        public void ALockedPieceChangesTheHash()
        {
            var level = FirstAccepted(new GenSpec { MaxLocks = 0 }, 1)[0];
            var locks = new[] { level.Solution[0] };
            Assert.That(Canonical.Hash(level.Def, locks), Is.Not.EqualTo(Canonical.Hash(level.Def)));
            Assert.That(Canonical.Hash(level.Def, locks), Is.EqualTo(Canonical.Hash(level.Def, locks)));
        }

        [Test]
        public void GapsCarveModeAlsoProducesAcceptedBoards()
        {
            var spec = new GenSpec { MinPieces = 2, MaxPieces = 3 };
            spec.Carve.Mode = CarveMode.Gaps;
            var levels = FirstAccepted(spec, 3);
            foreach (var level in levels) Assert.That(SolutionCounter.Count(level.Def, Locked.Placed(level.Def, level.Locks)), Is.EqualTo(1));
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

        // The 128 classic solutions as samples: the constructor turns most
        // of them into unique, minimal, graded levels; the outcome per level
        // is pinned so a change to any step shows here.
        [Test]
        public void ClassicSolutionsConstructToUniqueMinimalLevels()
        {
            // MaxLocks = 1 explicitly: this is a capability fixture for the
            // constructor against a foreign corpus, not shipped content, and
            // the pinned table is only comparable across versions if the lock
            // pool stays open. Shipped levels use the GenSpec default (0).
            var spec = new GenSpec
            {
                Elements = Element.Walls | Element.Traps, RequireAllPieces = false, RequireUsefulArms = false,
                MinGrade = Grade.G1, MaxGrade = Grade.G5, MaxLocks = 1,
            };
            var sb = new StringBuilder();
            int constructed = 0;
            for (int id = 0; id < ClassicLevels.Count; id++)
            {
                var sample = Sample.FromLevel(ClassicLevels.Get(id), ClassicLevels.Solution(id));
                var level = Constructor.Build(sample, spec, (ulong)id, out Rejection why);
                if (level == null)
                {
                    sb.Append(id).Append(':').Append(why).Append(' ');
                    continue;
                }
                constructed++;
                Assert.That(SolutionCounter.Count(level.Def, Locked.Placed(level.Def, level.Locks)), Is.EqualTo(1), $"classic {id}");
                Assert.That(SolutionCounter.Wins(level.Def, level.Solution), Is.True, $"classic {id}");
                sb.Append(id).Append(':').Append(level.Grade).Append('/').Append(level.Givens).Append(level.Locks.Length > 0 ? "L " : " ");
            }
            string table = sb.ToString().TrimEnd();
            TestContext.Out.WriteLine($"constructed {constructed} of {ClassicLevels.Count}: {table}");
            Assert.That(constructed, Is.GreaterThanOrEqualTo(80));
            Assert.That(Canonical.Fnv1a64(table).ToString("x16"), Is.EqualTo(GeneratorV2Goldens.ClassicConstruction), table);
        }

        // Seed 1 is pinned as a *rejection*: its ambiguity was the kind only a
        // pre-placed piece could break, so at MaxLocks 0 it is refused. The
        // other four pin real boards, up to a five-piece G4.
        [TestCase(1ul, GeneratorV2Goldens.Seed1)]
        [TestCase(2ul, GeneratorV2Goldens.Seed2)]
        [TestCase(3ul, GeneratorV2Goldens.Seed3)]
        [TestCase(5ul, GeneratorV2Goldens.Seed5)]
        [TestCase(7ul, GeneratorV2Goldens.Seed7)]
        public void GoldenSeedsAreStable(ulong seed, string expected)
        {
            var level = GeneratorV2.Generate(new GenSpec(), seed);
            Assert.That(Encode(level), Is.EqualTo(expected));
        }

        [Test, Explicit("run once to (re)capture the golden values above")]
        public void CaptureGoldenSeeds()
        {
            for (ulong seed = 1; seed <= 8; seed++)
            {
                TestContext.Out.WriteLine($"GOLDEN {seed}: {Encode(GeneratorV2.Generate(new GenSpec(), seed))}");
            }
        }

        // board|pieces|grade|solution[|locks] or "rejected".
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
            if (level.Locks.Length > 0)
            {
                sb.Append('|');
                for (int n = 0; n < level.Locks.Length; n++)
                {
                    if (n > 0) sb.Append(' ');
                    sb.Append(level.Locks[n].piece).Append('@').Append(level.Locks[n].cell);
                }
            }
            return sb.ToString();
        }
    }

    internal static class GeneratorV2Goldens
    {
        public const string Seed1 = "rejected";
        public const string Seed2 = "1.....1111............................11....1.....1...............|RD,RU|G1|0@38 1@6";
        public const string Seed3 = "............1111..1.....1.....1112.11...111....1..................|RD,L,LUD|G1|0@12 1@32 2@41";
        public const string Seed5 = "1.....1.....1...1.1...1.1...1.2.1111..................111.....1...|U,LD,LRU|G1|0@24 1@56 2@34";
        public const string Seed7 = ".......11111..1..1.11111.112.1.111.1.111.11112.2.11....11111....1.|RD,LRD,D,LUD,LD|G4|0@19 1@58 2@33 3@44 4@11";
        public const string ClassicConstruction = "32a62e239bf620ad";
    }
}
