using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Bloodhound.Engine;
using GridInfect.Core.Solving;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Stage 1 acceptance (docs/EXECUTION_PLAN.md): the C# counter equals the
    // Python oracle on all 128 classic levels, the deducer never claims a
    // solve on a non-unique level, it solves most unique levels without a
    // contradiction step, and the whole run stays under two seconds.
    [TestFixture]
    public class SolverTests
    {
        static string GoldenPath => Path.Combine(TestPaths.RepoRoot, "docs", "level_metrics_classic.json");

        static Dictionary<int, Dictionary<string, object>> Golden()
        {
            var root = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(GoldenPath));
            var rows = new Dictionary<int, Dictionary<string, object>>();
            foreach (object item in (List<object>)root["levels"])
            {
                var row = (Dictionary<string, object>)item;
                rows[(int)(long)row["id"]] = row;
            }
            return rows;
        }

        [Test]
        public void CountMatchesThePythonOracleOnAll128Levels()
        {
            var golden = Golden();
            Assert.That(golden.Count, Is.EqualTo(ClassicLevels.Count));
            for (int id = 0; id < ClassicLevels.Count; id++)
            {
                var r = SolutionCounter.Analyse(ClassicLevels.Get(id));
                Assert.That(r.Capped, Is.False, $"level {id}: capped");
                Assert.That(r.Solutions, Is.EqualTo((int)(long)golden[id]["solutions"]), $"level {id}: solutions");
                Assert.That(r.Static, Is.EqualTo((int)(long)golden[id]["static"]), $"level {id}: static");
                Assert.That(r.MinPieces, Is.EqualTo((int)(long)golden[id]["min_pieces"]), $"level {id}: min pieces");
            }
        }

        [Test]
        public void SolvedIsOnlyReportedOnUniqueLevels()
        {
            var golden = Golden();
            for (int id = 0; id < ClassicLevels.Count; id++)
            {
                var r = Deducer.Solve(ClassicLevels.Get(id));
                if (r.Solved)
                {
                    Assert.That((long)golden[id]["solutions"], Is.EqualTo(1), $"level {id}: Solved on a non-unique level");
                    Assert.That(r.Guesses, Is.EqualTo(0), $"level {id}");
                }
            }
        }

        [Test]
        public void MostUniqueLevelsSolveWithoutAContradictionStep()
        {
            var golden = Golden();
            int unique = 0, solved = 0;
            var failed = new StringBuilder();
            for (int id = 0; id < ClassicLevels.Count; id++)
            {
                if ((long)golden[id]["solutions"] != 1) continue;
                unique++;
                var r = Deducer.Solve(ClassicLevels.Get(id));
                if (r.Solved && r.MaxTier < Tier.Contradiction1) solved++;
                else failed.Append($" {id}(solved={r.Solved} max={r.MaxTier} guesses={r.Guesses})");
            }
            TestContext.Out.WriteLine($"unique {unique}, solved without contradiction {solved}, failing:{failed}");
            // The plan counted 27 unique levels with the oracle's order check
            // dead (tools/level_metrics.py, fixed in stage 1); it is 31.
            Assert.That(unique, Is.EqualTo(31));
            Assert.That(solved, Is.GreaterThanOrEqualTo(20), $"failing:{failed}");
        }

        [Test]
        public void EveryLevelHasAWinningFirstSolutionThroughTheRealRules()
        {
            for (int id = 0; id < ClassicLevels.Count; id++)
            {
                var def = ClassicLevels.Get(id);
                var order = SolutionCounter.FirstSolution(def);
                Assert.That(order, Is.Not.Null, $"level {id}");
                Assert.That(SolutionCounter.Wins(def, order), Is.True, $"level {id}");
                var r = Deducer.Solve(def);
                Assert.That(r.Complete, Is.True, $"level {id}: deducer found no assignment");
                Assert.That(SolutionCounter.Wins(def, r.Placements), Is.True, $"level {id}: deducer assignment does not win");
            }
        }

        [Test]
        public void SolveRespectsPlacedPieces()
        {
            var def = ClassicLevels.Get(0);
            var full = Deducer.Solve(def);
            Assert.That(full.Solved, Is.True);
            var placed = new PieceState[def.Pieces.Length];
            var first = full.Placements[0];
            placed[first.piece] = new PieceState
            {
                Tile = def.Pieces[first.piece], Placed = true,
                I = (sbyte)(first.cell / Grid.Width), J = (sbyte)(first.cell % Grid.Width),
            };
            var partial = Deducer.Solve(def, placed);
            Assert.That(partial.Solved, Is.True);
            Assert.That(partial.Trace.Length, Is.EqualTo(full.Trace.Length - 1));
            foreach (var d in partial.Trace) Assert.That(d.Piece, Is.Not.EqualTo(first.piece));
        }

        [Test]
        public void WholeClassicRunStaysUnderTwoSeconds()
        {
            var watch = Stopwatch.StartNew();
            for (int id = 0; id < ClassicLevels.Count; id++)
            {
                SolutionCounter.Count(ClassicLevels.Get(id));
                Deducer.Solve(ClassicLevels.Get(id));
            }
            watch.Stop();
            TestContext.Out.WriteLine($"128 levels counted and solved in {watch.ElapsedMilliseconds} ms");
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(2000));
        }

        // The grade of every unique classic level, locked: a change here is a
        // change to every generated world's difficulty ramp.
        [Test]
        public void ClassicGradesAreStable()
        {
            var sb = new StringBuilder();
            for (int id = 0; id < ClassicLevels.Count; id++)
            {
                var r = Deducer.Solve(ClassicLevels.Get(id));
                if (!r.Solved) continue;
                sb.Append(id).Append(':').Append(Grader.Grade(r)).Append(' ');
            }
            TestContext.Out.WriteLine(sb.ToString().TrimEnd());
            Assert.That(sb.ToString().TrimEnd(), Is.EqualTo(SolverGoldens.ClassicGrades));
        }
    }

    internal static class SolverGoldens
    {
        public const string ClassicGrades =
            "0:G1 3:G1 5:G1 6:G1 8:G1 18:G2 19:G2 25:G1 27:G1 34:G1 35:G2 36:G2 38:G3 41:G2 42:G3 46:G3 49:G2 " +
            "50:G5 57:G3 58:G3 59:G2 61:G2 62:G3 63:G2 65:G3 67:G3 71:G2 80:G3 86:G5 105:G3 117:G3";
    }
}
