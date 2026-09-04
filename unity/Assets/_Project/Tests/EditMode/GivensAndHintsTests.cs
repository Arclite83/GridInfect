using System.Collections.Generic;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Three rules the adapter leans on and could not see before:
    //
    //  1. A loader finishes its session before it publishes it. SessionChanged
    //     is what builds the board view, so anything applied after it is a
    //     board the view never saw — which is how a locked given came to light
    //     two cells with its piece still stranded in the tray.
    //  2. No level a player can load pre-places a piece (GenSpec.MaxLocks = 0).
    //  3. A hint on an already-beaten level is free. The wallet pays for
    //     progress, and a replay is not progress.
    [TestFixture]
    public class GivensAndHintsTests
    {
        // The invariant the givens bug broke, stated without needing a level
        // that has givens: whatever a loader builds, it finishes building it
        // before it publishes. SessionChanged is what makes the adapter draw
        // the board, so anything applied after it is a board the view never
        // saw — which is exactly how a locked given came to light two cells
        // with its piece still sitting in the tray.
        [Test]
        public void EveryLoaderFinishesTheSessionBeforeItPublishesIt()
        {
            var loads = new List<(string action, Dictionary<string, object> input)>
            {
                (GridInfectActions.LevelLoad, Inputs.LevelLoad(0)),
                (GridInfectActions.LevelLoad, Inputs.LevelLoad(37)),
                (GridInfectActions.WorldLoad, Inputs.WorldLoad(Worlds.First.Id, 0)),
                (GridInfectActions.WorldLoad, Inputs.WorldLoad(Worlds.All[3].Id, 5)),
                (GridInfectActions.DailyBegin, Inputs.DailyBegin("2026-01-05", 0L)),
                (GridInfectActions.EndlessBegin, Inputs.EndlessBegin(Solving.Grade.G1, 4242L)),
                (GridInfectActions.LevelGenerate, Inputs.LevelGenerate(Difficulty.Beginner, 99L)),
            };

            foreach (var (action, input) in loads)
            {
                var d = GridInfectActions.CreateDispatcher();
                byte[] atPublish = null;
                PieceState[] piecesAtPublish = null;
                void Capture(LevelSession s)
                {
                    if (s == null) return;
                    atPublish = (byte[])s.Board.Clone();
                    piecesAtPublish = (PieceState[])s.Pieces.Clone();
                }
                d.State.SessionChanged += Capture;
                Assert.That(d.Dispatch(action, input).Applied, Is.True, action);
                d.State.SessionChanged -= Capture;

                var session = d.State.Session;
                Assert.That(atPublish, Is.Not.Null, $"{action}: never published a session");
                Assert.That(atPublish, Is.EqualTo(session.Board), $"{action}: board changed after publication");
                for (int k = 0; k < session.Pieces.Length; k++)
                {
                    Assert.That(piecesAtPublish[k].Placed, Is.EqualTo(session.Pieces[k].Placed),
                        $"{action}: piece {k} was placed after publication");
                    Assert.That(piecesAtPublish[k].Locked, Is.EqualTo(session.Pieces[k].Locked),
                        $"{action}: piece {k} was locked after publication");
                }
            }
        }

        // The policy (GENERATOR_V2 "Locks at load"): GenSpec.MaxLocks is 0, so
        // nothing the player can load hands them a piece they cannot move.
        [Test]
        public void NoShippedLevelPreplacesAPiece()
        {
            foreach (World w in Worlds.All)
            {
                for (int n = 0; n < w.Count; n++)
                {
                    Assert.That(Worlds.Locks(w.Id, n), Is.Empty, $"{w.Id}/{n} ships a locked given");
                }
            }
            foreach (System.DayOfWeek day in System.Enum.GetValues(typeof(System.DayOfWeek)))
            {
                for (int n = 0; n < DailyPool.Count(day); n++)
                {
                    Assert.That(DailyPool.Get(day, n).Locks, Is.Empty, $"daily {day}/{n} ships a locked given");
                }
                Assert.That(DailySpec.For(day).MaxLocks, Is.Zero, $"daily spec {day}");
            }

            // Endless has no baked pool to check — it generates on the device,
            // so the guarantee there is the spec it generates from.
            for (int g = (int)Solving.Grade.G1; g <= (int)Solving.Grade.G5; g++)
            {
                Assert.That(DailySpec.Endless((Solving.Grade)g).MaxLocks, Is.Zero, $"endless spec G{g}");
            }
        }

        // ... and the loaders agree, board by board.
        [Test]
        public void EveryBoardAPlayerCanLoadStartsWithAnEmptyTray()
        {
            var d = GridInfectActions.CreateDispatcher();
            foreach (World w in Worlds.All)
            {
                for (int n = 0; n < w.Count; n++)
                {
                    Assert.That(d.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(w.Id, n)).Applied);
                    foreach (PieceState piece in d.State.Session.Pieces)
                    {
                        Assert.That(piece.Placed, Is.False, $"{w.Id}/{n} starts with a piece on the board");
                    }
                }
            }
            Assert.That(d.Dispatch(GridInfectActions.DailyBegin, Inputs.DailyBegin("2026-01-05", 0L)).Applied);
            foreach (PieceState piece in d.State.Session.Pieces) Assert.That(piece.Placed, Is.False, "daily");
            Assert.That(d.Dispatch(GridInfectActions.EndlessBegin, Inputs.EndlessBegin(Solving.Grade.G3, 31337L)).Applied);
            foreach (PieceState piece in d.State.Session.Pieces) Assert.That(piece.Placed, Is.False, "endless");
        }

        // The mechanism stays budgeted out, not deleted: a level that does
        // carry a lock still loads the way GENERATOR_V2 says it does, so
        // raising MaxLocks stays a one-field change.
        [Test]
        public void LockedApplyStillPlacesInfectsAndSurvivesAFullReset()
        {
            var def = UndoTests.ParseDef(
                "......" + "..1..." + "..1..." + "..1..." + "......" +
                "......" + "......" + "......" + "......" + "......" + "......", "D");
            var session = new LevelSession(def);
            Locked.Apply(session, new[] { (0, Grid.Loc(1, 2)) });

            Assert.That(session.Pieces[0].Placed && session.Pieces[0].Locked, Is.True);
            Assert.That(session.Board[Grid.Loc(1, 2)], Is.EqualTo(Cell.Infected), "its own cell");
            Assert.That(session.Board[Grid.Loc(3, 2)], Is.EqualTo(Cell.Infected), "and down its arm");
            Assert.That(session.ResolutionPending, Is.False, "Apply resolves once");

            session.Rules.FullReset(session);
            Assert.That(session.Pieces[0].Placed && session.Pieces[0].Locked, Is.True, "a lock survives a full reset");
            Assert.That(session.Board[Grid.Loc(3, 2)], Is.EqualTo(Cell.Infected), "and re-infects");
        }

        // ---- free hints on a replay ----

        [Test]
        public void ReplayIsProgressionMinusTheLevelInPlay()
        {
            var d = GridInfectActions.CreateDispatcher();
            World w = Worlds.First;

            Assert.That(d.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(w.Id, 0)).Applied);
            Assert.That(Queries.IsReplay(d.State), Is.False, "the frontier level is not a replay");

            Assert.That(d.Dispatch(GridInfectActions.ProgressUnlockWorldLevel, Inputs.UnlockWorldLevel(w.Id, 1)).Applied);
            Assert.That(Queries.IsReplay(d.State), Is.True, "level 1 solved: revisiting level 0 is a replay");

            Assert.That(d.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(w.Id, 1)).Applied);
            Assert.That(Queries.IsReplay(d.State), Is.False, "the new frontier is not");

            // The last level of a world only ever records the finished marker.
            Assert.That(d.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(w.Id, w.Count - 1)).Applied);
            Assert.That(Queries.IsReplay(d.State), Is.False);
            Assert.That(d.Dispatch(GridInfectActions.ProgressUnlockWorldLevel, Inputs.UnlockWorldLevel(w.Id, w.Count)).Applied);
            Assert.That(Queries.IsReplay(d.State), Is.True, "a finished world replays its last level");

            // Legacy reads the same way, off the classic unlock set.
            Assert.That(d.Dispatch(GridInfectActions.LevelLoad, Inputs.LevelLoad(0)).Applied);
            Assert.That(Queries.IsReplay(d.State), Is.False);
            Assert.That(d.Dispatch(GridInfectActions.ProgressUnlock, Inputs.Unlock(1)).Applied);
            Assert.That(Queries.IsReplay(d.State), Is.True);

            // Endless generates a fresh board every time; there is nothing to replay.
            Assert.That(d.Dispatch(GridInfectActions.EndlessBegin, Inputs.EndlessBegin(Solving.Grade.G1, 7L)).Applied);
            Assert.That(Queries.IsReplay(d.State), Is.False);
        }

        [Test]
        public void ADailyAlreadyBeatenReplaysAndItsHintsAreFree()
        {
            var d = GridInfectActions.CreateDispatcher();
            const string date = "2026-01-05";
            Assert.That(d.Dispatch(GridInfectActions.DailyBegin, Inputs.DailyBegin(date, 0L)).Applied);
            Assert.That(Queries.IsReplay(d.State), Is.False, "first attempt");

            d.State.Profile.DailyBestMs[date] = 30_000;
            Assert.That(Queries.IsReplay(d.State), Is.True, "a recorded best is a solve");

            int before = d.State.Profile.Locks;
            Assert.That(d.Dispatch(GridInfectActions.PieceLock).Applied);
            Assert.That(d.State.Profile.Locks, Is.EqualTo(before), "a replayed daily does not charge");
        }

        [Test]
        public void AReplayedLevelDoesNotSpendLocksAndWorksAtAnEmptyWallet()
        {
            var d = GridInfectActions.CreateDispatcher();
            World w = Worlds.First;
            d.State.Profile.Locks = 0;

            Assert.That(d.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(w.Id, 0)).Applied);
            Assert.That(d.Dispatch(GridInfectActions.PieceLock).Applied, Is.False, "unbeaten: the wallet still rules");

            Assert.That(d.Dispatch(GridInfectActions.ProgressUnlockWorldLevel, Inputs.UnlockWorldLevel(w.Id, 1)).Applied);
            Assert.That(d.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(w.Id, 0)).Applied);

            var s = d.State.Session;
            int locks = 0;
            while (!s.Solved)
            {
                var result = d.Dispatch(GridInfectActions.PieceLock);
                Assert.That(result.Applied, Is.True, result.Rejection);
                Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied);
                locks++;
                Assert.That(locks, Is.LessThanOrEqualTo(s.Pieces.Length));
            }
            Assert.That(locks, Is.GreaterThan(0), "the level took at least one hint");
            Assert.That(d.State.Profile.Locks, Is.EqualTo(0), "and charged for none of them");
        }

        // ---- the dev unlock ----

        [Test]
        public void UnlockAllOpensEverythingAndSurvivesASaveRoundTrip()
        {
            var d = GridInfectActions.CreateDispatcher();
            Assert.That(Queries.EverythingUnlocked(d.State.Profile), Is.False);

            Assert.That(d.Dispatch(GridInfectActions.ProgressUnlockAll).Applied);
            Profile profile = d.State.Profile;
            Assert.That(Queries.EverythingUnlocked(profile), Is.True);
            Assert.That(Queries.IsUnlocked(profile, ClassicLevels.Count - 1), Is.True);
            foreach (World w in Worlds.All)
            {
                Assert.That(Queries.IsWorldUnlocked(profile, w.Id), Is.True, w.Id);
                Assert.That(Queries.IsWorldLevelUnlocked(profile, w.Id, w.Count - 1), Is.True, w.Id);
                Assert.That(Queries.IsWorldFinished(profile, w.Id), Is.True, w.Id);
            }
            Assert.That(profile.Dirty, Is.True);

            Assert.That(Queries.EverythingUnlocked(SaveCodec.Load(SaveCodec.Save(profile))), Is.True);
        }
    }
}
