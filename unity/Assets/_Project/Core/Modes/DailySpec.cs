using System;
using System.Globalization;
using GridInfect.Core.Generation;
using GridInfect.Core.Solving;

namespace GridInfect.Core
{
    // The Daily's board is a pure function of the UTC date: the weekday's
    // spec (the ramp below) at the date's seed range (SeedFor), first
    // accepted seed wins. Nothing is pregenerated; the level cache runs that
    // math ahead of time in the background and the loader covers a miss
    // (MODES.md §5.1). Endless works the same way from its logged run seed.
    public static class DailySpec
    {
        public const string DateFormat = "yyyy-MM-dd";

        // A date's seed range: day n since DailyCalendar.Epoch scans from
        // CalendarSeedBase + n * DayStride; the stride leaves the cache's
        // MaxSeedTries room, so two dates never share a seed.
        public const ulong CalendarSeedBase = 2_000_000;
        public const ulong DayStride = 10_000;

        public static ulong SeedFor(DateTime date) => CalendarSeedBase + (ulong)DailyCalendar.DayIndex(date) * DayStride;

        // The UTC date a clock stamp falls on: what "today" means to an action.
        public static DateTime DateOf(long nowMs) => DateTimeOffset.FromUnixTimeMilliseconds(nowMs).UtcDateTime.Date;

        public static bool TryParseDate(string dateUtc, out DateTime date) =>
            DateTime.TryParseExact(dateUtc, DateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date);

        public static string Format(DateTime date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

        // The element set rotates with the weekday (one element per day as
        // the stages land): Monday and Tuesday are plain walls (Tuesday one
        // grade up), the weekend stacks them.
        public static Element ElementsFor(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Wednesday: return Element.Walls | Element.Area;
                case DayOfWeek.Thursday: return Element.Walls | Element.Forbidden;
                case DayOfWeek.Friday: return Element.Walls | Element.Diagonals;
                case DayOfWeek.Saturday: return Element.Walls | Element.Relays;
                case DayOfWeek.Sunday: return Element.Walls | Element.Forbidden | Element.Diagonals;
                default: return Element.Walls;
            }
        }

        public static GenSpec For(DateTime date) => For(date.DayOfWeek);

        // The week ramps: Monday is a warm-up, the weekend is the hard one.
        public static GenSpec For(DayOfWeek day)
        {
            var spec = new GenSpec { Elements = ElementsFor(day) };
            switch (day)
            {
                case DayOfWeek.Monday: spec.MinPieces = 3; spec.MaxPieces = 3; spec.MinGrade = Grade.G1; spec.MaxGrade = Grade.G2; break;
                case DayOfWeek.Tuesday: spec.MinPieces = 3; spec.MaxPieces = 4; spec.MinGrade = Grade.G2; spec.MaxGrade = Grade.G2; break;
                case DayOfWeek.Wednesday: spec.MinPieces = 4; spec.MaxPieces = 4; spec.MinGrade = Grade.G2; spec.MaxGrade = Grade.G3; break;
                case DayOfWeek.Thursday: spec.MinPieces = 4; spec.MaxPieces = 5; spec.MinGrade = Grade.G3; spec.MaxGrade = Grade.G3; break;
                case DayOfWeek.Friday: spec.MinPieces = 5; spec.MaxPieces = 5; spec.MinGrade = Grade.G3; spec.MaxGrade = Grade.G4; break;
                case DayOfWeek.Saturday: spec.MinPieces = 5; spec.MaxPieces = 5; spec.MinGrade = Grade.G4; spec.MaxGrade = Grade.G4; break;
                default: spec.MinPieces = 5; spec.MaxPieces = 5; spec.MinGrade = Grade.G4; spec.MaxGrade = Grade.G5; break;
            }
            return spec;
        }

        // The board for a date (DailyCalendar.For); null for a malformed date.
        public static PlayableLevel Build(string dateUtc)
        {
            if (!TryParseDate(dateUtc, out DateTime date)) return null;
            return DailyCalendar.For(date);
        }

        // The first seed at or after `seed` the generator accepts, through the
        // cache: at once when it is there, otherwise generated now.
        public static PlayableLevel FirstAccepted(GenSpec spec, ulong seed) => LevelCache.Shared.Get(spec, seed);

        // Par: a deduction step every fifteen seconds, more for the harder
        // grades, plus a look at the board.
        public static long ParMs(int traceLength, Grade grade) =>
            10_000 + traceLength * 15_000L * (4 + (int)grade - 1) / 4;

        // The Endless spec per grade: the same piece bands the worlds use.
        public static GenSpec Endless(Grade grade)
        {
            var spec = new GenSpec { Elements = Element.Walls, MinGrade = grade, MaxGrade = grade };
            switch (grade)
            {
                case Grade.G1: spec.MinPieces = 2; spec.MaxPieces = 3; break;
                case Grade.G2: spec.MinPieces = 3; spec.MaxPieces = 4; break;
                case Grade.G3: spec.MinPieces = 4; spec.MaxPieces = 5; break;
                case Grade.G4: spec.MinPieces = 5; spec.MaxPieces = 5; break;
                default: spec.MinPieces = 5; spec.MaxPieces = 5; break;
            }
            return spec;
        }
    }

    public sealed class DailyRun
    {
        public string DateUtc;
        public ulong Seed;             // the level's accepted generator seed
        public int Day;                // days since DailyCalendar.Epoch
        public long StartedMs;
        public long CompletedMs;       // 0 while running
        public int TraceLength;
        public Solving.Grade Grade;
        public long ParMs;
        public bool StreakGrantDue;    // set by daily.complete when the streak hit a multiple of 7
        public bool Completed => CompletedMs != 0;
    }

    public sealed class EndlessRun
    {
        public Solving.Grade Grade;
        public ulong Seed;             // run seed; level n starts its seed search at Seed + n * stride
        public int Index;              // levels solved so far in the run
        public int Streak;             // solves in a row without a reset
        public ulong LevelSeed;        // the accepted seed of the current level
    }
}
