using System;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // The free-solve faucets (Rewards): the daily streak's ladder through
    // daily.complete, the world and Legacy first-clear rules, and the save
    // field the ladder's one-off rung reads.
    [TestFixture]
    public class RewardTests
    {
        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        static long NoonMs(DateTime date) => (long)(date - Epoch).TotalMilliseconds + 12L * 3600_000L;

        // A dispatcher whose session is a solved board: the wallet is filled
        // and the first world level is locked through. The daily run is then
        // hung on that session directly, so the ladder is exercised without
        // generating a board per date.
        static Bloodhound.Engine.Dispatcher<GameState> Solved()
        {
            var d = GridInfectActions.CreateDispatcher();
            Assert.That(d.Dispatch(GridInfectActions.LocksGrant, Inputs.LocksGrant(50, GrantLocksAction.Rewarded)).Applied);
            Assert.That(d.Dispatch(GridInfectActions.WorldLoad, Inputs.WorldLoad(Worlds.First.Id, 0)).Applied);
            while (!d.State.Session.Solved)
            {
                Assert.That(d.Dispatch(GridInfectActions.PieceLock).Applied);
                Assert.That(d.Dispatch(GridInfectActions.BoardResolve).Applied);
            }
            return d;
        }

        // Complete `date` on the day; reports whether the run flagged a grant.
        static bool CompleteOn(Bloodhound.Engine.Dispatcher<GameState> d, DateTime date)
        {
            string dateUtc = DailySpec.Format(date);
            d.State.Mode = GameMode.Daily;
            d.State.DailyRun = new DailyRun { DateUtc = dateUtc, StartedMs = NoonMs(date) - 60_000L };
            var result = d.Dispatch(GridInfectActions.DailyComplete, Inputs.Now(NoonMs(date)));
            Assert.That(result.Applied, Is.True, $"{dateUtc}: {result.Rejection}");
            return d.State.DailyRun.StreakGrantDue;
        }

        [Test]
        public void StreakLadderGrantsTheFirstThreeThenEverySeventh()
        {
            var d = Solved();
            var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            for (int day = 1; day <= 21; day++)
            {
                bool granted = CompleteOn(d, start.AddDays(day - 1));
                bool expected = day == 3 || day == 7 || day == 14 || day == 21;
                Assert.That(granted, Is.EqualTo(expected), $"day {day}");
                Assert.That(d.State.Profile.DailyStreak, Is.EqualTo(day));
                Assert.That(d.State.Profile.DailyStreakBest, Is.EqualTo(day));
            }
        }

        [Test]
        public void FirstRungIsTakenOnceOnly()
        {
            var d = Solved();
            var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.That(CompleteOn(d, start), Is.False);
            Assert.That(CompleteOn(d, start.AddDays(1)), Is.False);
            Assert.That(CompleteOn(d, start.AddDays(2)), Is.True, "the first 3");
            Assert.That(Rewards.StreakFirstTaken(d.State.Profile), Is.True);

            // A day missed: the streak restarts and 3 is no longer a rung.
            Assert.That(CompleteOn(d, start.AddDays(4)), Is.False);
            Assert.That(d.State.Profile.DailyStreak, Is.EqualTo(1));
            Assert.That(CompleteOn(d, start.AddDays(5)), Is.False);
            Assert.That(CompleteOn(d, start.AddDays(6)), Is.False, "3 again, already taken");
            for (int day = 4; day <= 6; day++) Assert.That(CompleteOn(d, start.AddDays(3 + day)), Is.False, $"day {day}");
            Assert.That(CompleteOn(d, start.AddDays(10)), Is.True, "7 is always a rung");
            Assert.That(d.State.Profile.DailyStreakBest, Is.EqualTo(7));
        }

        [Test]
        public void ArchiveAndRepeatMoveNothing()
        {
            var d = Solved();
            var today = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
            Assert.That(CompleteOn(d, today), Is.False);

            // The same date again: a better time at most, never the streak.
            Assert.That(CompleteOn(d, today), Is.False);
            Assert.That(d.State.Profile.DailyStreak, Is.EqualTo(1));

            // A past date solved from the calendar (the clock says today).
            d.State.DailyRun = new DailyRun { DateUtc = DailySpec.Format(today.AddDays(-3)), StartedMs = NoonMs(today) - 60_000L };
            Assert.That(d.Dispatch(GridInfectActions.DailyComplete, Inputs.Now(NoonMs(today))).Applied);
            Assert.That(d.State.DailyRun.StreakGrantDue, Is.False);
            Assert.That(d.State.Profile.DailyStreak, Is.EqualTo(1));
            Assert.That(d.State.Profile.DailyStreakBest, Is.EqualTo(1));
        }

        [Test]
        public void StreakCycleIsThePositionInTheWeek()
        {
            Assert.That(Rewards.StreakCycle(0), Is.EqualTo(0));
            Assert.That(Rewards.StreakCycle(1), Is.EqualTo(1));
            Assert.That(Rewards.StreakCycle(7), Is.EqualTo(7));
            Assert.That(Rewards.StreakCycle(8), Is.EqualTo(1));
            Assert.That(Rewards.StreakCycle(9), Is.EqualTo(2));
            Assert.That(Rewards.StreakCycle(14), Is.EqualTo(7));
        }

        [Test]
        public void EveryWorldGrantsAtItsMidpointAndItsLastLevel()
        {
            foreach (World w in Worlds.All)
            {
                int grants = 0;
                for (int n = 0; n < w.Count; n++) if (Rewards.WorldLevelGrant(w, n)) grants++;
                Assert.That(grants, Is.EqualTo(2), w.Id);
                Assert.That(Rewards.WorldLevelGrant(w, w.Count - 1), Is.True, $"{w.Id}: last");
                Assert.That(Rewards.WorldLevelGrant(w, w.Count / 2 - 1), Is.True, $"{w.Id}: midpoint");
                Assert.That(Rewards.WorldLevelGrant(w, 0), Is.False, $"{w.Id}: first");
            }
        }

        [Test]
        public void LegacyGrantsEveryTenth()
        {
            int grants = 0;
            for (int id = 0; id < ClassicLevels.Count; id++) if (Rewards.LegacyGrant(id)) grants++;
            Assert.That(grants, Is.EqualTo(ClassicLevels.Count / Rewards.LegacyEvery));
            Assert.That(Rewards.LegacyGrant(0), Is.False);
            Assert.That(Rewards.LegacyGrant(9), Is.True, "level 10");
            Assert.That(Rewards.LegacyGrant(10), Is.False);
        }

        [Test]
        public void SaveV9CarriesTheBestStreakAndNeverBelowTheStreak()
        {
            var profile = new Profile { DailyStreak = 2, DailyStreakBest = 8, DailyLastDate = "2026-09-10" };
            var loaded = SaveCodec.Load(SaveCodec.Save(profile));
            Assert.That(loaded.DailyStreakBest, Is.EqualTo(8));
            Assert.That(loaded.DailyStreak, Is.EqualTo(2));

            // A v8 save has no best: the streak it does carry is the floor,
            // so a returning player at 5 is not offered the rung at 3.
            var v8 = SaveCodec.Load("{\"v\":8,\"dailyStreak\":5,\"dailyLast\":\"2026-09-10\"}");
            Assert.That(v8.DailyStreakBest, Is.EqualTo(5));
            Assert.That(Rewards.StreakFirstTaken(v8), Is.True);

            var lagging = SaveCodec.Load("{\"v\":9,\"dailyStreak\":4,\"dailyStreakBest\":2}");
            Assert.That(lagging.DailyStreakBest, Is.EqualTo(4));
        }

        [Test]
        public void ResetForgetsTheBestStreak()
        {
            var d = GridInfectActions.CreateDispatcher();
            d.State.Profile.DailyStreak = 3;
            d.State.Profile.DailyStreakBest = 9;
            Assert.That(d.Dispatch(GridInfectActions.ProgressReset).Applied);
            Assert.That(d.State.Profile.DailyStreakBest, Is.EqualTo(0));
            Assert.That(Rewards.StreakFirstTaken(d.State.Profile), Is.False);
        }
    }
}
