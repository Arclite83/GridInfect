using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Stage 7: RulesV2 reproduces the classic placement path on all 128
    // boards, undo is "restore the initial board and re-propagate placed
    // pieces in index order" with no repel-queue accumulation, Legacy stays
    // on the frozen rules, and PieceSpec text round-trips.
    [TestFixture]
    public class RulesV2Tests
    {
        static LevelDef AsV2(LevelDef v1)
        {
            var board = new byte[Grid.Cells];
            v1.CopyBoardTo(board);
            return new LevelDef(board, v1.Specs);
        }

        [Test]
        public void LegacyStaysOnTheClassicRulesAndNewContentRunsOnV2()
        {
            Assert.That(new LevelSession(ClassicLevels.Get(0)).Rules, Is.InstanceOf<RulesV1>());
            Assert.That(ClassicLevels.Get(0).Version, Is.EqualTo(1));
            Assert.That(new LevelSession(Worlds.Level(Worlds.First.Id, 0)).Rules, Is.InstanceOf<RulesV2>());
            Assert.That(Worlds.Level(Worlds.First.Id, 0).Version, Is.EqualTo(2));
        }

        [Test]
        public void PlacementPathMatchesTheClassicRulesOnAll128Boards()
        {
            for (int id = 0; id < ClassicLevels.Count; id++)
            {
                var v1 = new LevelSession(ClassicLevels.Get(id));
                var v2 = new LevelSession(AsV2(ClassicLevels.Get(id)));
                foreach (var (piece, cell) in ClassicLevels.Solution(id))
                {
                    int i = cell / Grid.Width, j = cell % Grid.Width;
                    Assert.That(v2.Rules.CanPlace(v2, piece, i, j), Is.EqualTo(v1.Rules.CanPlace(v1, piece, i, j)), $"level {id}");
                    v1.Rules.SetPiece(v1, piece, i, j);
                    v2.Rules.SetPiece(v2, piece, i, j);
                    Assert.That(v2.RepelQueue.Count, Is.EqualTo(v1.RepelQueue.Count), $"level {id}: queue after placement");
                    Assert.That(v2.ResetTripped, Is.EqualTo(v1.ResetTripped), $"level {id}: trip flag");
                    v1.Rules.Resolve(v1);
                    v2.Rules.Resolve(v2);
                    Assert.That(v2.Board, Is.EqualTo(v1.Board), $"level {id}: board after resolve");
                    Assert.That(v2.Solved, Is.EqualTo(v1.Solved), $"level {id}: solved");
                }
                Assert.That(v2.Solved, Is.True, $"level {id}");
            }
        }

        [Test]
        public void UndoRestoresTheBoardAndRePropagatesInIndexOrder()
        {
            foreach (var scenario in UndoFixtures.All)
            {
                var def = AsV2(UndoTests.ParseDef(scenario.Board, scenario.Pieces));
                var s = new LevelSession(def);
                foreach (string op in scenario.Script.Split(';'))
                {
                    if (op[0] == 'P')
                    {
                        string[] head = op.Substring(1).Split('@');
                        string[] pos = head[1].Split(',');
                        int k = int.Parse(head[0]), i = int.Parse(pos[0]), j = int.Parse(pos[1]);
                        if (!s.Rules.CanPlace(s, k, i, j)) continue;
                        s.Rules.SetPiece(s, k, i, j);
                        s.Rules.Resolve(s);
                    }
                    else
                    {
                        s.Rules.ClearPiece(s, int.Parse(op.Substring(1)));
                    }
                    if (s.Solved) break;
                }
                Assert.That(s.RepelQueue.Count, Is.EqualTo(0), $"{scenario.Name}: V2 never leaves repels queued");

                // The V2 board equals the placed pieces' spreads replayed on a
                // fresh session in index order, resolved once.
                var fresh = new LevelSession(def);
                for (int k = 0; k < s.Pieces.Length; k++)
                {
                    if (!s.Pieces[k].Placed) continue;
                    fresh.Pieces[k].Placed = true;
                    fresh.Pieces[k].I = s.Pieces[k].I;
                    fresh.Pieces[k].J = s.Pieces[k].J;
                }
                if (s.Solved) continue; // a win mid-script ends the comparison
                Assert.That(RulesV2.Rebuilt(fresh), Is.EqualTo(s.Board), $"{scenario.Name}: board is the union of the remaining spreads");
            }
        }

        [Test]
        public void RepelQueueDoesNotAccumulateAcrossUndo()
        {
            // The V1 quirk fixture: two undos leave two repels queued in V1.
            var def = AsV2(UndoTests.ParseDef(
                "..3..." + "..1..." + "..1..." + "..1..." + "..1..." + "..1..." +
                "..1..." + "..1..." + "..1..." + "..1..." + "......", "U,D"));
            var s = new LevelSession(def);
            s.Rules.SetPiece(s, 0, 9, 2);
            s.Rules.Resolve(s);
            Assert.That(s.RepelQueue.Count, Is.EqualTo(0), "queue emptied after it ran");
            s.Rules.SetPiece(s, 1, 5, 2);
            s.Rules.Resolve(s);
            s.Rules.ClearPiece(s, 1);
            s.Rules.ClearPiece(s, 0);
            Assert.That(s.RepelQueue.Count, Is.EqualTo(0));
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                Assert.That(s.Board[loc], Is.EqualTo(def.BoardAt(loc)), "empty board after clearing everything");
            }
        }

        [TestCase("L")]
        [TestCase("LRUD")]
        [TestCase("LR+U1")]
        [TestCase("D2")]
        [TestCase("ul+dr")]
        [TestCase("L+ur2")]
        [TestCase("A")]
        [TestCase("LD+A")]
        public void PieceSpecTextRoundTrips(string text)
        {
            var spec = PieceSpec.Parse(text);
            Assert.That(spec.Encode(), Is.EqualTo(text));
            Assert.That(PieceSpec.Parse(spec.Encode()), Is.EqualTo(spec));
        }

        [Test]
        public void ClassicTilesAreExactlyTheUnlimitedCardinalSpecs()
        {
            for (int t = 0; t <= (int)Tile.LRUD; t++)
            {
                var spec = PieceSpec.FromTile((Tile)t);
                Assert.That(spec.IsTile, Is.True);
                Assert.That(spec.ToTile(), Is.EqualTo((Tile)t));
                Assert.That(spec.Encode(), Is.EqualTo(((Tile)t).ToString()));
            }
            Assert.That(PieceSpec.Parse("L1").IsTile, Is.False);
            Assert.That(PieceSpec.Parse("ul").IsTile, Is.False);
            Assert.That(PieceSpec.Parse("A").IsTile, Is.False);
        }
    }
}
