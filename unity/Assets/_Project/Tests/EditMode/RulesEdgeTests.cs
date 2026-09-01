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
                "....1." + "..1.1." + "..2.1." + "..3.1." + "..5.1." + "..1.1." +
                "..1.1." + "..1.1." + "..1.1." + "..1.1." + "..1.1.",
                "D,R");
            Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied, Is.False, "resolve without pending");

            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 5, 2)).Applied);
            Assert.That(d.State.Session.ResolutionPending, Is.True);

            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(1, 0, 4)).Applied, Is.False);
            Assert.That(d.Dispatch(GridInfectActions.PieceClear, Inputs.PieceClear(0)).Applied, Is.False);
            Assert.That(d.Dispatch(GridInfectActions.LevelReset).Applied, Is.False);

            Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied, Is.True);
            Assert.That(d.State.Session.ResolutionPending, Is.False);
        }

        [Test]
        public void WinningPlacementIgnoresTrippedTrapAndQueuedRepels()
        {
            var d = NewDispatcher(
                "..5..." + "..1..." + "..1..." + "..1..." + "..1..." + "..1..." +
                "..1..." + "..1..." + "..1..." + "..1..." + "..3...",
                "UD");
            var session = d.State.Session;
            bool unbound = false;
            session.PiecesUnbound += () => unbound = true;

            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 5, 2)).Applied);
            Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied);

            Assert.That(session.Solved, Is.True);
            Assert.That(unbound, Is.False, "no reset ran");
            Assert.That(session.RepelQueue.Count, Is.EqualTo(1), "repel stayed queued, never ran");
        }

        [Test]
        public void NonWinningTrapTripFullResetsAndReturnsPieces()
        {
            var d = NewDispatcher(
                "..5.1." + "..1.1." + "..1.1." + "..1.1." + "..1.1." + "..1.1." +
                "..1.1." + "..1.1." + "..1.1." + "..1.1." + "..1.1.",
                "UD");
            var session = d.State.Session;

            d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 5, 2));
            d.Dispatch(GridInfectActions.BoardResolve);

            Assert.That(session.Solved, Is.False);
            Assert.That(session.Pieces[0].Placed, Is.False);
            for (int i = 1; i <= 10; i++)
            {
                Assert.That(session.Board[Grid.Loc(i, 2)], Is.EqualTo(Cell.Active), $"row {i} reverted");
            }
        }
    }
}
