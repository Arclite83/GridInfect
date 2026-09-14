using System.Collections.Generic;
using System.IO;
using Bloodhound.Engine;
using GridInfect.Core.Solving;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // The last piece: a placement that only ever wins as the final one, so
    // the Lock tool must never pin it early (docs/last_piece_classic.json,
    // written by tools/gen_last_piece_golden.py from the reference rules).
    //
    // The identification here is the oracle. What ships is the arm test in
    // Lock.ChooseTarget — "does this placement's spread reach a switch or a
    // trap" — and the test that matters is the one below that proves the
    // cheap rule covers every last piece the oracle finds.
    [TestFixture]
    public class PlacementOrderTests
    {
        static string GoldenPath => Path.Combine(TestPaths.RepoRoot, "docs", "last_piece_classic.json");

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

        static List<string> LastOnlyText(LevelDef def, (int piece, int cell)[] set)
        {
            bool[] lastOnly = PlacementOrder.LastOnly(def, set);
            var text = new List<string>();
            for (int n = 0; n < set.Length; n++)
            {
                if (lastOnly[n]) text.Add($"{set[n].piece}@{set[n].cell}");
            }
            return text;
        }

        [Test]
        public void LastPieceMatchesThePythonOracleOnAll128Levels()
        {
            var golden = Golden();
            Assert.That(golden.Count, Is.EqualTo(ClassicLevels.Count));
            int constrained = 0;
            for (int id = 0; id < ClassicLevels.Count; id++)
            {
                var expected = new List<string>();
                foreach (object entry in (List<object>)golden[id]["last_only"]) expected.Add((string)entry);
                var actual = LastOnlyText(ClassicLevels.Get(id), ClassicLevels.Solution(id));
                Assert.That(actual, Is.EquivalentTo(expected), $"level {id}: last piece");
                if (expected.Count > 0) constrained++;
            }
            Assert.That(constrained, Is.EqualTo(84), "classic levels with a last piece");
        }

        // Why the Lock tool can decide with one look at the arms: a
        // placement is forced last only because playing it early resets the
        // board or repels infection off it, and both need an arm that ends
        // on a trap or a switch. So the arm test is a superset of the last
        // pieces — it can pass over a placement that would have been fine,
        // never pin one that would not.
        [Test]
        public void EveryLastPieceReachesASwitchOrATrap()
        {
            for (int id = 0; id < ClassicLevels.Count; id++)
            {
                LevelDef def = ClassicLevels.Get(id);
                var set = ClassicLevels.Solution(id);
                bool[] lastOnly = PlacementOrder.LastOnly(def, set);
                var map = new LineMap(def);
                for (int n = 0; n < set.Length; n++)
                {
                    if (!lastOnly[n]) continue;
                    var spread = map.Spread(def.Specs[set[n].piece], set[n].cell);
                    Assert.That(spread.Trips || spread.Switches, Is.True,
                        $"level {id}: last piece {set[n].piece}@{set[n].cell} reaches neither switch nor trap");
                }
            }
        }

        // Nothing generated carries a switch or a trap (DailySpec.ElementsFor,
        // and no world was baked with them), so no baked world level and no
        // tutorial step has an order to get wrong. The day one does, this
        // test is what says so.
        [Test]
        public void NoBakedWorldOrTutorialLevelHasALastPiece()
        {
            foreach (World w in Worlds.All)
            {
                for (int n = 0; n < w.Count; n++)
                {
                    var last = PlacementOrder.LastPiece(Worlds.Level(w.Id, n), Worlds.Solution(w.Id, n));
                    Assert.That(last, Is.Null, $"{w.Id}/{n}: last piece {last}");
                }
            }
            for (int n = 0; n < TutorialLevels.Count; n++)
            {
                var step = TutorialLevels.Get(n);
                Assert.That(PlacementOrder.LastPiece(step.Def, step.Solution), Is.Null, $"tutorial {n}");
            }
        }
    }
}
