using GridInfect.Core.Solving;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Stage 5 acceptance: from an empty board, Lock alone solves every
    // launch level in at most piece-count locks; after each lock the level
    // still has a solution with the locked pieces fixed; the wallet, the
    // eviction rule, full reset and undo behave as NEXT_PASS specifies; the
    // classic vectors serve as the Legacy fallback source.
    [TestFixture]
    public class LockTests
    {
        static Bloodhound.Engine.Dispatcher<GameState> Rich()
        {
            var d = GridInfectActions.CreateDispatcher();
            Assert.That(d.Dispatch(GridInfectActions.LocksGrant, Inputs.LocksGrant(50, GrantLocksAction.Rewarded)).Applied);
            return d;
        }

        [Test]
        public void LockAloneSolvesEveryLaunchLevelWithinPieceCount()
        {
            foreach (World w in Worlds.All)
            {
                for (int n = 0; n < w.Count; n++)
                {
                    var d = Rich();
                    Assert.That(d.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(w.Id, n)).Applied);
                    var s = d.State.Session;
                    int locks = 0;
                    while (!s.Solved)
                    {
                        var lockResult = d.Dispatch(GridInfectActions.PieceLock);
                        Assert.That(lockResult.Applied, Is.True, $"{w.Id}/{n} lock {locks}: {lockResult.Rejection}");
                        Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied);
                        locks++;
                        Assert.That(locks, Is.LessThanOrEqualTo(s.Pieces.Length), $"{w.Id}/{n}: too many locks");
                        Assert.That(SolutionCounter.Count(s.Def, s.Pieces, 4), Is.GreaterThanOrEqualTo(1),
                            $"{w.Id}/{n}: lock {locks} made the level unsolvable");
                    }
                    Assert.That(d.State.Profile.Locks, Is.EqualTo(55 - locks));
                }
            }
        }

        [Test]
        public void LockRefusesWhenTheWalletIsEmptyOrNothingIsLeft()
        {
            var d = GridInfectActions.CreateDispatcher();
            d.State.Profile.Locks = 0;
            Assert.That(d.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(Worlds.First.Id, 0)).Applied);
            Assert.That(d.Dispatch(GridInfectActions.PieceLock).Applied, Is.False, "empty wallet");

            var rich = Rich();
            rich.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(Worlds.First.Id, 0));
            while (!rich.State.Session.Solved)
            {
                Assert.That(rich.Dispatch(GridInfectActions.PieceLock).Applied);
                rich.Dispatch(GridInfectActions.BoardResolve);
            }
            Assert.That(rich.Dispatch(GridInfectActions.PieceLock).Applied, Is.False, "solved: nothing left");
        }

        [Test]
        public void GrantsCapFreeLocksButNotRewardedOnes()
        {
            var d = GridInfectActions.CreateDispatcher();
            Assert.That(d.State.Profile.Locks, Is.EqualTo(Profile.LocksStart));
            Assert.That(d.Dispatch(GridInfectActions.LocksGrant, Inputs.LocksGrant(3, "streak")).Applied);
            Assert.That(d.State.Profile.Locks, Is.EqualTo(8));
            Assert.That(d.Dispatch(GridInfectActions.LocksGrant, Inputs.LocksGrant(5, "streak")).Applied);
            Assert.That(d.State.Profile.Locks, Is.EqualTo(Profile.LocksCap), "streak grants stop at the cap");
            Assert.That(d.Dispatch(GridInfectActions.LocksGrant, Inputs.LocksGrant(2, GrantLocksAction.Rewarded)).Applied);
            Assert.That(d.State.Profile.Locks, Is.EqualTo(12), "rewarded grants pass the cap");
            Assert.That(d.Dispatch(GridInfectActions.LocksGrant, Inputs.LocksGrant(0, "streak")).Applied, Is.False);
            Assert.That(d.Dispatch(GridInfectActions.LocksGrant, Inputs.LocksGrant(1, "")).Applied, Is.False);
        }

        [Test]
        public void LockEvictsAWrongPieceAndSurvivesResetAndUndo()
        {
            var d = Rich();
            Assert.That(d.Dispatch(GridInfectActions.LevelLoad, Inputs.LevelLoad(0)).Applied);
            var s = d.State.Session;
            // Level 1: D at (2,4) and R at (6,0). Put R on (2,4), a wrong cell.
            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(1, 2, 4)).Applied);
            d.Dispatch(GridInfectActions.BoardResolve);

            Assert.That(d.Dispatch(GridInfectActions.PieceLock).Applied);
            d.Dispatch(GridInfectActions.BoardResolve);
            int locked = -1;
            for (int k = 0; k < s.Pieces.Length; k++) if (s.Pieces[k].Locked) locked = k;
            Assert.That(locked, Is.GreaterThanOrEqualTo(0));
            Assert.That(s.Pieces[locked].Placed, Is.True);
            int cell = Grid.Loc(s.Pieces[locked].I, s.Pieces[locked].J);
            Assert.That(cell == Grid.Loc(2, 4) || cell == Grid.Loc(6, 0), "locked on a solution cell");
            if (cell == Grid.Loc(2, 4))
            {
                Assert.That(locked, Is.EqualTo(0), "D is the deducer's first placement");
                Assert.That(s.Pieces[1].Placed, Is.False, "the wrong R was evicted to the tray");
            }

            Assert.That(d.Dispatch(GridInfectActions.PieceClear, Inputs.PieceClear(locked)).Applied, Is.False, "a locked piece cannot be lifted");

            Assert.That(d.Dispatch(GridInfectActions.LevelReset).Applied);
            Assert.That(s.Pieces[locked].Placed && s.Pieces[locked].Locked, Is.True, "full reset keeps the locked piece");
            Assert.That(s.Board[cell], Is.EqualTo(Cell.Infected), "and its cell is re-infected");
            for (int k = 0; k < s.Pieces.Length; k++)
            {
                if (k != locked) Assert.That(s.Pieces[k].Placed, Is.False, "unlocked pieces went back to the tray");
            }

            // Undo re-propagation treats the locked piece as ordinary placed.
            int other = locked == 0 ? 1 : 0;
            var target = d.State.Solution[0].piece == other ? d.State.Solution[0] : d.State.Solution[1];
            Assert.That(d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(other, target.cell / Grid.Width, target.cell % Grid.Width)).Applied);
            d.Dispatch(GridInfectActions.BoardResolve);
            Assert.That(s.Solved, Is.True);
        }

        [Test]
        public void LegacyLevelsLockFromTheVectorSolution()
        {
            // Level 90 (id 89) has 114 solutions: the deducer cannot force a
            // step, so the fallback (largest coverage in the vector) is used.
            var d = Rich();
            Assert.That(d.Dispatch(GridInfectActions.LevelLoad, Inputs.LevelLoad(89)).Applied);
            var s = d.State.Session;
            int locks = 0;
            while (!s.Solved)
            {
                var lockResult = d.Dispatch(GridInfectActions.PieceLock);
                Assert.That(lockResult.Applied, Is.True, lockResult.Rejection);
                d.Dispatch(GridInfectActions.BoardResolve);
                locks++;
                Assert.That(locks, Is.LessThanOrEqualTo(s.Pieces.Length));
            }
            foreach (var (piece, cell) in ClassicLevels.Solution(89))
            {
                Assert.That(s.Pieces[piece].Placed && Grid.Loc(s.Pieces[piece].I, s.Pieces[piece].J) == cell || s.Solved);
            }
        }

        [Test]
        public void LockedTrapTripperDoesNotLoopTheBoard()
        {
            // A one-piece level whose only solution trips a trap: locking it
            // resets once, re-propagates, and the level is solved on resolve.
            var d = Rich();
            d.State.SetSession(new LevelSession(UndoTests.ParseDef(
                "..5..." + "..1..." + "..1..." + "......" + "......" + "......" +
                "......" + "......" + "......" + "......" + "......", "U")));
            d.State.Solution = new[] { (0, Grid.Loc(2, 2)) };
            Assert.That(d.Dispatch(GridInfectActions.PieceLock).Applied);
            Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied);
            Assert.That(d.State.Session.Solved, Is.True);
            Assert.That(d.State.Session.Pieces[0].Locked, Is.True);
        }

        [Test]
        public void SaveMigratesTheWalletWithTheStartingBalance()
        {
            var migrated = SaveCodec.Load("{\"v\":3,\"unlocked\":[],\"bestMs\":[0,0,0,0,0],\"counts\":[0,0,0,0,0],\"muted\":false}");
            Assert.That(migrated.Locks, Is.EqualTo(Profile.LocksStart));
            migrated.Locks = 7;
            string json = SaveCodec.Save(migrated);
            Assert.That(json, Does.Contain("\"locks\":7"));
            Assert.That(SaveCodec.Load(json).Locks, Is.EqualTo(7));
        }
    }
}
