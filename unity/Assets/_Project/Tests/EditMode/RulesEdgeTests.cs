using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // The resolution-order rules the vectors only cover implicitly:
    // win-before-reset (RULES §4.1), reset-on-trip (§6), and the R-107
    // invariant that nothing runs inside a pending resolution.
    [TestFixture]
    public class RulesEdgeTests
    {
        static Bloodhound.Engine.Dispatcher<GameState> NewDispatcher(string board, string pieces)
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            dispatcher.State.SetSession(new LevelSession(UndoTests.ParseDef(board, pieces)));
            return dispatcher;
        }

        [Test]
        public void BoardActionsAreBlockedWhilePendingAndResolveIsRequired()
        {
            var d = NewDispatcher(
                "..........." + "..........." + ".1235111111" + "..........." + "11111111111" + "...........",
                "R,D");
            Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied, Is.False, "resolve without pending");

            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 2, 5)).Applied);
            Assert.That(d.State.Session.ResolutionPending, Is.True);

            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(1, 4, 0)).Applied, Is.False);
            Assert.That(d.Dispatch(GridInfectActions.PieceClear, Inputs.PieceClear(0)).Applied, Is.False);
            Assert.That(d.Dispatch(GridInfectActions.LevelReset).Applied, Is.False);

            Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied, Is.True);
            Assert.That(d.State.Session.ResolutionPending, Is.False);
        }

        [Test]
        public void WinningPlacementIgnoresTrippedTrapAndQueuedRepels()
        {
            var d = NewDispatcher(
                "..........." + "..........." + "51111111113" + "..........." + "..........." + "...........",
                "LR");
            var session = d.State.Session;
            bool unbound = false;
            session.PiecesUnbound += () => unbound = true;

            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 2, 5)).Applied);
            Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied);

            Assert.That(session.Solved, Is.True);
            Assert.That(unbound, Is.False, "no reset ran");
            Assert.That(session.RepelQueue.Count, Is.EqualTo(1), "repel stayed queued, never ran");
        }

        [Test]
        public void NonWinningTrapTripFullResetsAndReturnsPieces()
        {
            var d = NewDispatcher(
                "..........." + "..........." + "51111111111" + "..........." + "11111111111" + "...........",
                "LR");
            var session = d.State.Session;

            d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 2, 5));
            d.Dispatch(GridInfectActions.BoardResolve);

            Assert.That(session.Solved, Is.False);
            Assert.That(session.Pieces[0].Placed, Is.False);
            for (int j = 1; j <= 10; j++)
            {
                Assert.That(session.Board[Grid.Loc(2, j)], Is.EqualTo(Cell.Active), $"col {j} reverted");
            }
        }
    }
}
