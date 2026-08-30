using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Undo has no shipped vectors; scenarios are cross-checked against an
    // independent second port (tools/gen_undo_fixtures.py).
    [TestFixture]
    public class UndoTests
    {
        static UndoFixtures.Scenario[] Scenarios() => UndoFixtures.All;

        [TestCaseSource(nameof(Scenarios))]
        public void ScenarioMatchesReferenceImplementation(UndoFixtures.Scenario scenario)
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            dispatcher.State.SetSession(new LevelSession(ParseDef(scenario.Board, scenario.Pieces)));
            var session = dispatcher.State.Session;

            int solvedEvents = 0, unboundEvents = 0;
            session.LevelSolved += () => solvedEvents++;
            session.PiecesUnbound += () => unboundEvents++;

            foreach (string op in scenario.Script.Split(';'))
            {
                if (op[0] == 'P')
                {
                    string[] head = op.Substring(1).Split('@');
                    string[] pos = head[1].Split(',');
                    var place = dispatcher.Dispatch(GridInfectActions.PiecePlace,
                        Inputs.PiecePlace(int.Parse(head[0]), int.Parse(pos[0]), int.Parse(pos[1])));
                    Assert.That(place.Applied, Is.True, $"{scenario.Name} '{op}': {place.Rejection}");
                    var resolve = dispatcher.Dispatch(GridInfectActions.BoardResolve);
                    Assert.That(resolve.Applied, Is.True, $"{scenario.Name} '{op}': {resolve.Rejection}");
                }
                else if (op[0] == 'C')
                {
                    var clear = dispatcher.Dispatch(GridInfectActions.PieceClear,
                        Inputs.PieceClear(int.Parse(op.Substring(1))));
                    Assert.That(clear.Applied, Is.True, $"{scenario.Name} '{op}': {clear.Rejection}");
                }
                else
                {
                    Assert.Fail($"{scenario.Name}: bad op '{op}'");
                }
            }

            Assert.That(BoardText(session.Board), Is.EqualTo(scenario.ExpectedBoard), $"{scenario.Name}: final board");
            Assert.That(session.RepelQueue.Count, Is.EqualTo(scenario.ExpectedRepelQueue), $"{scenario.Name}: repel queue");
            Assert.That(PlacedMask(session), Is.EqualTo(scenario.ExpectedPlacedMask), $"{scenario.Name}: placed pieces");
            Assert.That(session.Solved, Is.EqualTo(scenario.ExpectedSolved), $"{scenario.Name}: solved flag");
            Assert.That(solvedEvents, Is.EqualTo(scenario.ExpectedSolvedEvents), $"{scenario.Name}: solved events");
            Assert.That(unboundEvents, Is.EqualTo(scenario.ExpectedUnboundEvents), $"{scenario.Name}: unbound events");
        }

        internal static LevelDef ParseDef(string boardText, string pieceNames)
        {
            var board = new byte[Grid.Cells];
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                board[loc] = boardText[loc] == '.' ? Cell.Void : (byte)(boardText[loc] - '0');
            }
            string[] names = pieceNames.Split(',');
            var tiles = new Tile[names.Length];
            for (int k = 0; k < names.Length; k++) tiles[k] = ClassicLevels.ParseTile(names[k]);
            return new LevelDef(board, tiles);
        }

        static string BoardText(byte[] board)
        {
            var chars = new char[Grid.Cells];
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                chars[loc] = board[loc] == Cell.Void ? '.' : (char)('0' + board[loc]);
            }
            return new string(chars);
        }

        static int PlacedMask(LevelSession session)
        {
            int mask = 0;
            for (int k = 0; k < session.Pieces.Length; k++)
            {
                if (session.Pieces[k].Placed) mask |= 1 << k;
            }
            return mask;
        }
    }
}
