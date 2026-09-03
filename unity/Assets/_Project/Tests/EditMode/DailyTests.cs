using GridInfect.Core.Generation;
using GridInfect.Core.Solving;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Stage 4 acceptance: two fresh states given the same date build the
    // same board (same LevelDef hash); a full daily run replays from its
    // log; a backward clock is refused; the streak counts consecutive
    // dates; Endless runs replay from their seed and score streaks; the
    // save schema bump migrates.
    [TestFixture]
    public class DailyTests
    {
        const string Monday = "2026-09-07";

        [Test]
        public void SameDateSameBoardOnTwoDevices()
        {
            var a = GridInfectActions.CreateDispatcher();
            var b = GridInfectActions.CreateDispatcher();
            Assert.That(a.Dispatch(GridInfectActions.DailyBegin, Inputs.DailyBegin(Monday, 1_000)).Applied);
            Assert.That(b.Dispatch(GridInfectActions.DailyBegin, Inputs.DailyBegin(Monday, 999_000)).Applied);
            Assert.That(Canonical.Hash(a.State.Session.Def), Is.EqualTo(Canonical.Hash(b.State.Session.Def)));
            Assert.That(a.State.Session.Board, Is.EqualTo(b.State.Session.Board));
            Assert.That(a.State.DailyRun.Seed, Is.EqualTo(b.State.DailyRun.Seed));
            Assert.That(a.State.Mode, Is.EqualTo(GameMode.Daily));
            Assert.That(a.Dispatch(GridInfectActions.DailyBegin, Inputs.DailyBegin("07/09/2026", 1)).Applied, Is.False, "date format");
        }

        [Test]
        public void FullDailyRunReplaysFromItsLog()
        {
            var d = GridInfectActions.CreateDispatcher();
            Assert.That(d.Dispatch(GridInfectActions.DailyBegin, Inputs.DailyBegin(Monday, 10_000)).Applied);
            Assert.That(d.Dispatch(GridInfectActions.DailyComplete, Inputs.Now(20_000)).Applied, Is.False, "not solved yet");
            Solve(d);
            Assert.That(d.Dispatch(GridInfectActions.DailyComplete, Inputs.Now(9_000)).Applied, Is.False, "backward clock");
            Assert.That(d.Dispatch(GridInfectActions.DailyComplete, Inputs.Now(70_000)).Applied);
            Assert.That(d.Dispatch(GridInfectActions.DailyComplete, Inputs.Now(80_000)).Applied, Is.False, "already completed");
            Assert.That(d.State.Profile.DailyBestMs[Monday], Is.EqualTo(60_000));
            Assert.That(d.State.Profile.DailyStreak, Is.EqualTo(1));
            Assert.That(d.State.DailyRun.ParMs, Is.EqualTo(DailySpec.ParMs(d.State.DailyRun.TraceLength, d.State.DailyRun.Grade)));

            var replayed = GridInfectActions.CreateDispatcher();
            replayed.Replay(d.Log.Entries);
            Assert.That(replayed.State.Session.Solved, Is.True);
            Assert.That(replayed.State.Profile.DailyBestMs[Monday], Is.EqualTo(60_000));
            Assert.That(replayed.State.DailyRun.CompletedMs, Is.EqualTo(70_000));
        }

        [Test]
        public void StreakCountsConsecutiveDatesAndGrantsEverySeventh()
        {
            var d = GridInfectActions.CreateDispatcher();
            string[] days = { "2026-09-07", "2026-09-08", "2026-09-09", "2026-09-10", "2026-09-11", "2026-09-12", "2026-09-13" };
            for (int n = 0; n < days.Length; n++)
            {
                Assert.That(d.Dispatch(GridInfectActions.DailyBegin, Inputs.DailyBegin(days[n], 1)).Applied, days[n]);
                Solve(d);
                Assert.That(d.Dispatch(GridInfectActions.DailyComplete, Inputs.Now(5_000)).Applied, days[n]);
                Assert.That(d.State.Profile.DailyStreak, Is.EqualTo(n + 1), days[n]);
                Assert.That(d.State.DailyRun.StreakGrantDue, Is.EqualTo(n == 6), days[n]);
            }
            // The same date again: a better time, streak untouched.
            d.Dispatch(GridInfectActions.DailyBegin, Inputs.DailyBegin(days[6], 1));
            Solve(d);
            d.Dispatch(GridInfectActions.DailyComplete, Inputs.Now(3_000));
            Assert.That(d.State.Profile.DailyStreak, Is.EqualTo(7));
            Assert.That(d.State.Profile.DailyBestMs[days[6]], Is.EqualTo(2_999));
            Assert.That(d.State.DailyRun.StreakGrantDue, Is.False);
            Assert.That(Queries.DailyStreakOn(d.State.Profile, "2026-09-14"), Is.EqualTo(7), "still intact tomorrow");
            Assert.That(Queries.DailyStreakOn(d.State.Profile, "2026-09-16"), Is.EqualTo(0), "broken after a missed day");
            // A gap resets to 1.
            d.Dispatch(GridInfectActions.DailyBegin, Inputs.DailyBegin("2026-09-20", 1));
            Solve(d);
            d.Dispatch(GridInfectActions.DailyComplete, Inputs.Now(5_000));
            Assert.That(d.State.Profile.DailyStreak, Is.EqualTo(1));
        }

        [Test]
        public void EndlessRunReplaysAndScoresStreaks()
        {
            var d = GridInfectActions.CreateDispatcher();
            Assert.That(d.Dispatch(GridInfectActions.EndlessBegin, Inputs.EndlessBegin(Grade.G1, 77)).Applied);
            Assert.That(d.State.Mode, Is.EqualTo(GameMode.Endless));
            Assert.That(d.Dispatch(GridInfectActions.EndlessAdvance).Applied, Is.False, "not solved");
            Solve(d);
            Assert.That(d.Dispatch(GridInfectActions.EndlessAdvance).Applied);
            Assert.That(d.State.EndlessRun.Streak, Is.EqualTo(1));
            Assert.That(d.State.EndlessRun.Index, Is.EqualTo(1));
            // A reset on this board ends the streak at the next solve.
            Assert.That(d.Dispatch(GridInfectActions.LevelReset).Applied);
            Solve(d);
            Assert.That(d.Dispatch(GridInfectActions.EndlessAdvance).Applied);
            Assert.That(d.State.EndlessRun.Streak, Is.EqualTo(1), "reset broke the streak");
            Solve(d);
            Assert.That(d.Dispatch(GridInfectActions.EndlessAdvance).Applied);
            Assert.That(d.State.EndlessRun.Streak, Is.EqualTo(2));
            Assert.That(d.State.Profile.EndlessBest[0], Is.EqualTo(2));

            var replayed = GridInfectActions.CreateDispatcher();
            replayed.Replay(d.Log.Entries);
            Assert.That(replayed.State.Session.Board, Is.EqualTo(d.State.Session.Board));
            Assert.That(replayed.State.EndlessRun.LevelSeed, Is.EqualTo(d.State.EndlessRun.LevelSeed));
            Assert.That(replayed.Dispatch(GridInfectActions.EndlessAbort).Applied);
            Assert.That(replayed.State.Session, Is.Null);
        }

        [Test]
        public void SaveMigratesFromV2AndRoundTripsV3()
        {
            string v2 = "{\"v\":2,\"unlocked\":[1],\"bestMs\":[0,0,0,0,0],\"counts\":[0,0,0,0,0],\"muted\":false,\"worlds\":{\"w01\":3}}";
            var migrated = SaveCodec.Load(v2);
            Assert.That(migrated.WorldUnlocked["w01"], Is.EqualTo(3));
            Assert.That(migrated.DailyStreak, Is.EqualTo(0));
            Assert.That(migrated.DailyBestMs, Is.Empty);

            migrated.DailyBestMs["2026-09-07"] = 61_000;
            migrated.DailyStreak = 3;
            migrated.DailyLastDate = "2026-09-07";
            migrated.EndlessBest[2] = 9;
            string json = SaveCodec.Save(migrated);
            Assert.That(json, Does.Contain("\"v\":" + SaveCodec.Version));
            var loaded = SaveCodec.Load(json);
            Assert.That(loaded.DailyBestMs, Is.EqualTo(migrated.DailyBestMs));
            Assert.That(loaded.DailyStreak, Is.EqualTo(3));
            Assert.That(loaded.DailyLastDate, Is.EqualTo("2026-09-07"));
            Assert.That(loaded.EndlessBest, Is.EqualTo(migrated.EndlessBest));
            Assert.That(SaveCodec.Save(loaded), Is.EqualTo(json));
        }

        // Every generated board carries its solution; play it through the actions.
        static void Solve(Bloodhound.Engine.Dispatcher<GameState> d)
        {
            var def = d.State.Session.Def;
            var order = SolutionCounter.FirstSolution(def);
            Assert.That(order, Is.Not.Null);
            foreach (var (piece, cell) in order)
            {
                var place = d.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(piece, cell / Grid.Width, cell % Grid.Width));
                Assert.That(place.Applied, Is.True, place.Rejection);
                Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied);
            }
            Assert.That(d.State.Session.Solved, Is.True);
        }
    }
}
