using System.Collections.Generic;
using System.IO;
using Bloodhound.Engine;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    /// <summary>
    /// Mechanical-equivalence proof (REQUIREMENTS R-114): every one of the
    /// 128 shipped levels replays its recorded solution through the real
    /// action pipeline, asserting every per-placement golden board and the
    /// final win. A second pass replays the captured action log into fresh
    /// state — the log, not the live session, is the load-bearing record.
    /// </summary>
    [TestFixture]
    public class VectorReplayTests
    {
        static Dictionary<string, object> _levels;

        static Dictionary<string, object> Levels
        {
            get
            {
                if (_levels == null)
                {
                    var root = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(TestPaths.VectorsPath));
                    _levels = (Dictionary<string, object>)root["levels"];
                }
                return _levels;
            }
        }

        static IEnumerable<int> LevelIds()
        {
            for (int id = 0; id < ClassicLevels.Count; id++) yield return id;
        }

        [Test]
        public void VectorsCoverAll128Levels()
        {
            Assert.That(Levels.Count, Is.EqualTo(ClassicLevels.Count));
        }

        [TestCaseSource(nameof(LevelIds))]
        public void SolutionReplaysToWinWithGoldenBoards(int levelId)
        {
            var vector = (Dictionary<string, object>)Levels[levelId.ToString()];
            var dispatcher = GridInfectActions.CreateDispatcher();

            Assert.That(dispatcher.Dispatch(GridInfectActions.LevelLoad, Inputs.LevelLoad(levelId)).Applied);
            var session = dispatcher.State.Session;

            // The baked level data must equal the vector's initial board and pieces.
            AssertBoardEquals((List<object>)vector["board"], session.Board, levelId, -1);
            var pieceNames = (List<object>)vector["pieces"];
            Assert.That(session.Pieces.Length, Is.EqualTo(pieceNames.Count), $"level {levelId}: piece count");
            for (int k = 0; k < pieceNames.Count; k++)
            {
                Assert.That(session.Pieces[k].Tile,
                    Is.EqualTo(ClassicLevels.ParseTile((string)pieceNames[k])), $"level {levelId}: piece {k}");
            }

            var steps = (List<object>)vector["steps"];
            for (int n = 0; n < steps.Count; n++)
            {
                var step = (Dictionary<string, object>)steps[n];
                var input = new ActionInput(step);
                var place = dispatcher.Dispatch(GridInfectActions.PiecePlace,
                    Inputs.PiecePlace(input.Int("piece_index"), input.Int("i"), input.Int("j")));
                Assert.That(place.Applied, Is.True, $"level {levelId} step {n}: {place.Rejection}");

                var resolve = dispatcher.Dispatch(GridInfectActions.BoardResolve);
                Assert.That(resolve.Applied, Is.True, $"level {levelId} step {n}: {resolve.Rejection}");

                AssertBoardEquals((List<object>)step["board_after"], session.Board, levelId, n);
            }

            Assert.That(session.Solved, Is.True, $"level {levelId}: final step did not win");

            // Log replay: fold the captured log over fresh state; the result
            // must be indistinguishable from the live session.
            var replayed = GridInfectActions.CreateDispatcher();
            replayed.Replay(dispatcher.Log.Entries);
            Assert.That(replayed.State.Session.Board, Is.EqualTo(session.Board), $"level {levelId}: log replay board diverged");
            Assert.That(replayed.State.Session.Solved, Is.True, $"level {levelId}: log replay did not win");
        }

        [Test]
        public void LogSurvivesSerializationRoundTrip()
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            var vector = (Dictionary<string, object>)Levels["0"];
            dispatcher.Dispatch(GridInfectActions.LevelLoad, Inputs.LevelLoad(0));
            foreach (object stepObj in (List<object>)vector["steps"])
            {
                var step = new ActionInput((Dictionary<string, object>)stepObj);
                dispatcher.Dispatch(GridInfectActions.PiecePlace,
                    Inputs.PiecePlace(step.Int("piece_index"), step.Int("i"), step.Int("j")));
                dispatcher.Dispatch(GridInfectActions.BoardResolve);
            }

            string json = dispatcher.Log.ToJson();
            var entries = ActionLog.ParseEntries(json);
            var replayed = GridInfectActions.CreateDispatcher();
            replayed.Replay(entries);

            Assert.That(replayed.State.Session.Board, Is.EqualTo(dispatcher.State.Session.Board));
            Assert.That(replayed.State.Session.Solved, Is.True);
        }

        static void AssertBoardEquals(List<object> expected, byte[] actual, int levelId, int step)
        {
            Assert.That(expected.Count, Is.EqualTo(Grid.Cells));
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                if ((long)expected[loc] != actual[loc])
                {
                    Assert.Fail($"level {levelId} step {step}: board mismatch at " +
                                $"({loc / Grid.Width},{loc % Grid.Width}): expected {(long)expected[loc]}, got {actual[loc]}");
                }
            }
        }
    }
}
