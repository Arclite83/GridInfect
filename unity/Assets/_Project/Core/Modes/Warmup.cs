using System;
using GridInfect.Core.Solving;

namespace GridInfect.Core
{
    // What the level cache should be working on, and in what order: the
    // boards the player is most likely to open next. Lower priority runs
    // first; the worker is a single thread, so the order is the budget.
    public static class Warmup
    {
        const int RecentDays = 6;

        // How many Endless boards to keep ahead of the player. One was not
        // enough: a fast solve outruns a one-deep queue, and then the next
        // board is a wait rather than an instant.
        public const int EndlessLookahead = 3;

        // At boot: today's daily, the recent unsolved dailies (the archive
        // the player reaches for), then Endless's opening board per grade
        // for the run seed the adapter has picked.
        public static void AtBoot(LevelCache cache, DateTime todayUtc, Profile profile, ulong endlessSeed)
        {
            DateTime today = todayUtc.Date;
            if (today >= DailyCalendar.Epoch) cache.Prefetch(DailySpec.For(today.DayOfWeek), DailySpec.SeedFor(today), 0);
            for (int back = 1; back <= RecentDays; back++)
            {
                DateTime date = today.AddDays(-back);
                if (date < DailyCalendar.Epoch) break;
                if (Queries.IsDailySolved(profile, DailySpec.Format(date))) continue;
                cache.Prefetch(DailySpec.For(date.DayOfWeek), DailySpec.SeedFor(date), back);
            }
            EndlessOpeners(cache, endlessSeed, 10);
        }

        // Endless's first board at each grade from a run seed, cheapest grade first.
        public static void EndlessOpeners(LevelCache cache, ulong endlessSeed, int priority)
        {
            for (int g = (int)Grade.G1; g <= (int)Grade.G5; g++)
            {
                cache.Prefetch(DailySpec.Endless((Grade)g), endlessSeed, priority + g);
            }
        }

        // The calendar's visible month: its unsolved, playable days, newest
        // first, behind everything above.
        public static void ForMonth(LevelCache cache, int year, int month, DateTime todayUtc, Profile profile)
        {
            DateTime today = todayUtc.Date;
            int days = DateTime.DaysInMonth(year, month);
            int priority = 20;
            for (int d = days; d >= 1; d--)
            {
                var date = new DateTime(year, month, d, 0, 0, 0, DateTimeKind.Utc);
                if (date > today || date < DailyCalendar.Epoch) continue;
                if (Queries.IsDailySolved(profile, DailySpec.Format(date))) continue;
                cache.Prefetch(DailySpec.For(date.DayOfWeek), DailySpec.SeedFor(date), priority++);
            }
        }

        // A run the player is about to start: its opening board first, then
        // the ones after it. Called when the tier is picked, so the worker is
        // on the board the loading card is waiting for rather than on the
        // openers of four tiers nobody chose.
        public static void ForEndlessRun(LevelCache cache, Grade grade, ulong seed)
        {
            for (int ahead = 0; ahead < EndlessLookahead; ahead++)
            {
                cache.Prefetch(DailySpec.Endless(grade), seed + (ulong)ahead * EndlessRun.Stride, ahead - 1);
            }
        }

        // After any applied action: in Endless, the boards the player is
        // certainly about to need.
        public static void AfterAction(LevelCache cache, GameState state)
        {
            if (state.Mode != GameMode.Endless || state.EndlessRun == null) return;
            var run = state.EndlessRun;
            for (int ahead = 1; ahead <= EndlessLookahead; ahead++)
            {
                cache.Prefetch(DailySpec.Endless(run.Grade), run.SeedAt(run.Index + ahead), ahead - 1);
            }
        }
    }
}
