using System.Collections.Generic;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Two rules the adapter leans on and could not see before:
    //
    //  1. A level's givens are on the board by the time the session is
    //     published. SessionChanged is what builds the board view, so a lock
    //     applied after it left the view drawing a board the rules had already
    //     infected — cells lit with the piece stranded in the tray.
    //  2. A hint on an already-beaten level is free. The wallet pays for
    //     progress, and a replay is not progress.
    [TestFixture]
    public class GivensAndHintsTests
    {
        // The state of a session at the instant SessionChanged fired: what an
        // adapter binding to the event can actually see.
        struct Snapshot
        {
            public int PlacedPieces;
            public int InfectedCells;
        }

        static Snapshot OnPublish(Bloodhound.Engine.Dispatcher<GameState> d, string action,
            Dictionary<string, object> input)
        {
            var seen = new Snapshot();
            void Capture(LevelSession s)
            {
                if (s == null) return;
                foreach (PieceState p in s.Pieces) if (p.Placed) seen.PlacedPieces++;
                foreach (byte v in s.Board) if (v == Cell.Infected) seen.InfectedCells++;
            }
            d.State.SessionChanged += Capture;
            Assert.That(d.Dispatch(action, input).Applied, Is.True, action);
            d.State.SessionChanged -= Capture;
            return seen;
        }

        [Test]
        public void WorldGivensAreOnTheBoardWhenTheSessionIsPublished()
        {
            int levelsWithGivens = 0;
            foreach (World w in Worlds.All)
            {
                for (int n = 0; n < w.Count; n++)
                {
                    var locks = Worlds.Locks(w.Id, n);
                    if (locks.Length == 0) continue;
                    levelsWithGivens++;

                    var d = GridInfectActions.CreateDispatcher();
                    Snapshot seen = OnPublish(d, GridInfectActions.WorldLoad, Inputs.WorldLoad(w.Id, n));
                    Assert.That(seen.PlacedPieces, Is.EqualTo(locks.Length),
                        $"{w.Id}/{n}: givens must be placed before SessionChanged");
                    Assert.That(seen.InfectedCells, Is.GreaterThan(0),
                        $"{w.Id}/{n}: a given infects at least its own cell");

                    var s = d.State.Session;
                    foreach (var (piece, cell) in locks)
                    {
                        Assert.That(s.Pieces[piece].Locked, Is.True, $"{w.Id}/{n}: piece {piece} locked");
                        Assert.That(Grid.Loc(s.Pieces[piece].I, s.Pieces[piece].J), Is.EqualTo(cell));
                    }
                }
            }
            Assert.That(levelsWithGivens, Is.GreaterThan(0), "the baked worlds ship levels with givens");
        }

        [Test]
        public void TheFirstLevelOfTheFirstWorldShipsAGiven()
        {
            // The one a player meets first, and the one the tray bug showed up
            // on: piece 0 is already on the board, so only the rest can move.
            var d = GridInfectActions.CreateDispatcher();
            Assert.That(d.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(Worlds.First.Id, 0)).Applied);
            var s = d.State.Session;
            int placed = 0;
            for (int k = 0; k < s.Pieces.Length; k++)
            {
                if (!s.Pieces[k].Placed) continue;
                placed++;
                Assert.That(s.Pieces[k].Locked, Is.True, "a piece placed before play is a locked given");
            }
            Assert.That(placed, Is.EqualTo(1));
        }

        [Test]
        public void DailyAndEndlessGivensAreOnTheBoardWhenTheSessionIsPublished()
        {
            var daily = GridInfectActions.CreateDispatcher();
            var level = DailySpec.Build("2026-01-05");   // a Monday
            Snapshot seenDaily = OnPublish(daily, GridInfectActions.DailyBegin, Inputs.DailyBegin("2026-01-05", 0L));
            Assert.That(seenDaily.PlacedPieces, Is.EqualTo(level.Locks.Length));
            if (level.Locks.Length > 0) Assert.That(seenDaily.InfectedCells, Is.GreaterThan(0));

            var endless = GridInfectActions.CreateDispatcher();
            Snapshot seenEndless = OnPublish(endless, GridInfectActions.EndlessBegin,
                Inputs.EndlessBegin(Solving.Grade.G1, 4242L));
            int locked = 0;
            foreach (PieceState p in endless.State.Session.Pieces) if (p.Locked) locked++;
            Assert.That(seenEndless.PlacedPieces, Is.EqualTo(locked));
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
