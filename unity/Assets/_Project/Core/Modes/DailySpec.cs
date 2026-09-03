using System;
using System.Globalization;
using GridInfect.Core.Generation;
using GridInfect.Core.Solving;

namespace GridInfect.Core
{
    // The Daily's generator settings and seed, both pure functions of the
    // UTC date, so every device builds the same board (MODES.md §5).
    public static class DailySpec
    {
        public const string DateFormat = "yyyy-MM-dd";
        public const int MaxSeedTries = 4000;

        public static bool TryParseDate(string dateUtc, out DateTime date) =>
            DateTime.TryParseExact(dateUtc, DateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date);

        public static string Format(DateTime date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

        // Stable across platforms: FNV-1a 64 of the date text.
        public static ulong Seed(string dateUtc) => Canonical.Fnv1a64("daily:" + dateUtc);

        // The week ramps: Monday is a warm-up, the weekend is the hard one.
        // Cardinal arms and walls only at launch; later stages rotate the
        // element set here per weekday.
        public static GenSpec For(DateTime date)
        {
            var spec = new GenSpec { Elements = Element.Walls };
            switch (date.DayOfWeek)
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

        // The board for a date: the first accepted seed at or after the
        // date's seed. Null only if MaxSeedTries seeds all reject.
        public static GeneratedLevel Build(string dateUtc)
        {
            if (!TryParseDate(dateUtc, out DateTime date)) return null;
            return FirstAccepted(For(date), Seed(dateUtc));
        }

        public static GeneratedLevel FirstAccepted(GenSpec spec, ulong seed)
        {
            for (int n = 0; n < MaxSeedTries; n++)
            {
                var level = GeneratorV2.Generate(spec, seed + (ulong)n);
                if (level != null) return level;
            }
            return null;
        }

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
        public ulong Seed;             // the accepted seed (date seed + tries)
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
