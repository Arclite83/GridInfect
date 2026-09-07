using System;
using GridInfect.Core.Solving;

namespace GridInfect.Core
{
    // The Daily's calendar: day n is n days after the epoch, and its board
    // is the weekday's spec at the date's seed range, first accepted seed
    // wins (DailySpec.SeedFor, DailySpec.For). Nothing is pregenerated: the
    // level cache runs that math ahead of time in the background and the
    // loader covers a miss, and because the board is a pure function of the
    // date every device opens the same one (MODES.md §5.1).
    public static class DailyCalendar
    {
        // A Monday; the first daily.
        public static readonly DateTime Epoch = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        public static int DayIndex(DateTime date) => (int)(date.Date - Epoch.Date).TotalDays;

        public static DateTime DateOfDay(int day) => Epoch.AddDays(day);

        // The board for a date. Generates on the calling thread when the
        // cache has not got there yet (or waits for the worker that is on it).
        public static PlayableLevel For(DateTime date)
        {
            if (date.Date < Epoch) throw new ArgumentOutOfRangeException(nameof(date), $"no daily before {DailySpec.Format(Epoch)}");
            return DailySpec.FirstAccepted(DailySpec.For(date.DayOfWeek), DailySpec.SeedFor(date))
                   ?? throw new InvalidOperationException($"no daily board for {DailySpec.Format(date)}");
        }

        // Whether a date's board is already in the cache (a tap opens it at once).
        public static bool IsReady(DateTime date) =>
            date.Date >= Epoch && LevelCache.Shared.Has(DailySpec.For(date.DayOfWeek), DailySpec.SeedFor(date));

        // The grade band a weekday's boards fall in: the calendar's column header.
        public static (Grade min, Grade max) Band(DayOfWeek day)
        {
            var spec = DailySpec.For(day);
            return (spec.MinGrade, spec.MaxGrade);
        }
    }
}
