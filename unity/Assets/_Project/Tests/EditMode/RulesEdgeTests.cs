using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    /// <summary>
    /// Placement legality and resolution-ordering edges from RULES.md that the
    /// shipped vectors exercise only implicitly.
    /// </summary>
    [TestFixture]
    public class RulesEdgeTests
    {
        // Row 2: void, active, wall, switch, trap, active x6; row 4 active (never solved).
        const string EdgeBoard =
            "..........." +
            "..........." +
            ".1235111111" +
            "..........." +
            "11111111111" +
            "...........";

        static Bloodhound.Engine.Dispatcher<GameState> NewDispatcher(string board = EdgeBoard, string pieces = "R,D")
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            dispatcher.State.SetSession(new LevelSession(UndoTests.ParseDef(board, pieces)));
            return dispatcher;
        }

        [TestCase(2, 0, Description = "void")]
        [TestCase(2, 2, Description = "wall")]
        [TestCase(2, 3, Description = "switch")]
        [TestCase(2, 4, Description = "trap")]
        [TestCase(0, 0, Description = "void elsewhere")]
        [TestCase(-1, 5, Description = "off-board row")]
        [TestCase(2, 11, Description = "off-board column")]
        public void PlacementOnIllegalCellIsRejected(int i, int j)
        {
            var d = NewDispatcher();
            var result = d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, i, j));
            Assert.That(result.Applied, Is.False);
        }

        [Test]
        public void PlacementOnInfectedCellIsLegal()
        {
            var d = NewDispatcher();
            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 2, 5)).Applied);
            Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied);
            // (2,6) was infected by piece 0's R arm; dropping piece 1 there is legal.
            Assert.That(d.State.Session.Board[Grid.Loc(2, 6)], Is.EqualTo(Cell.Infected));
            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(1, 2, 6)).Applied);
        }

        [Test]
        public void PlacementOnOccupiedCellIsRejected()
        {
            var d = NewDispatcher();
            d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 2, 5));
            d.Dispatch(GridInfectActions.BoardResolve);
            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(1, 2, 5)).Applied, Is.False);
        }

        [Test]
        public void BoardActionsAreBlockedWhilePendingAndResolveIsRequired()
        {
            var d = NewDispatcher();
            Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied, Is.False, "resolve without pending");

            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 2, 5)).Applied);
            Assert.That(d.State.Session.ResolutionPending, Is.True);

            // The beat is never cancellable and nothing may run inside it (R-107).
            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(1, 4, 0)).Applied, Is.False);
            Assert.That(d.Dispatch(GridInfectActions.PieceClear, Inputs.PieceClear(0)).Applied, Is.False);
            Assert.That(d.Dispatch(GridInfectActions.LevelReset).Applied, Is.False);

            Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied, Is.True);
            Assert.That(d.State.Session.ResolutionPending, Is.False);
        }

        [Test]
        public void ClearingAnUnplacedPieceIsRejected()
        {
            var d = NewDispatcher();
            Assert.That(d.Dispatch(GridInfectActions.PieceClear, Inputs.PieceClear(0)).Applied, Is.False);
        }

        [Test]
        public void WinningPlacementIgnoresTrippedTrapAndQueuedRepels()
        {
            // Row 2: trap at col 0, switch at col 10, active between; a single
            // LR piece infects everything, hitting both. Win check runs first:
            // solved, no reset, no repel (RULES.md §4.1 order).
            const string board =
                "..........." + "..........." + "51111111113" + "..........." + "..........." + "...........";
            var d = NewDispatcher(board, "LR");
            var session = d.State.Session;
            bool unbound = false;
            session.PiecesUnbound += () => unbound = true;

            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 2, 5)).Applied);
            Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied);

            Assert.That(session.Solved, Is.True);
            Assert.That(session.ResetTripped, Is.True, "flag stays set until the next placement");
            Assert.That(unbound, Is.False, "no reset ran");
            Assert.That(session.RepelQueue.Count, Is.EqualTo(1), "repel stayed queued, never ran");
            for (int j = 1; j <= 9; j++)
            {
                Assert.That(session.Board[Grid.Loc(2, j)], Is.EqualTo(Cell.Infected));
            }
        }

        [Test]
        public void NonWinningTrapTripFullResetsAndReturnsPieces()
        {
            const string board =
                "..........." + "..........." + "51111111111" + "..........." + "11111111111" + "...........";
            var d = NewDispatcher(board, "LR");
            var session = d.State.Session;
            bool unbound = false;
            session.PiecesUnbound += () => unbound = true;

            d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 2, 5));
            d.Dispatch(GridInfectActions.BoardResolve);

            Assert.That(session.Solved, Is.False);
            Assert.That(unbound, Is.True);
            Assert.That(session.Pieces[0].Placed, Is.False);
            for (int j = 1; j <= 10; j++)
            {
                Assert.That(session.Board[Grid.Loc(2, j)], Is.EqualTo(Cell.Active), $"col {j} reverted");
            }
        }

        [Test]
        public void SpreadJumpsGapsAndEdgesButStopsAtWalls()
        {
            // Row 2: active at cols 1,5,9 with voids between; wall at col 3.
            const string board =
                "..........." + "..........." + ".1.2.1...1." + "..........." + "11111111111" + "...........";
            var d = NewDispatcher(board, "LR");
            var session = d.State.Session;

            d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 2, 5));
            d.Dispatch(GridInfectActions.BoardResolve);

            Assert.That(session.Board[Grid.Loc(2, 9)], Is.EqualTo(Cell.Infected), "jumped voids rightward");
            Assert.That(session.Board[Grid.Loc(2, 1)], Is.EqualTo(Cell.Active), "wall stopped leftward spread");
        }

        [Test]
        public void LevelResetRestoresUnsolvedBoard()
        {
            var d = NewDispatcher();
            d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 2, 5));
            d.Dispatch(GridInfectActions.BoardResolve);
            Assert.That(d.Dispatch(GridInfectActions.LevelReset).Applied);
            Assert.That(d.State.Session.Pieces[0].Placed, Is.False);
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                Assert.That(d.State.Session.Board[loc], Is.Not.EqualTo(Cell.Infected));
            }
        }
    }
}
