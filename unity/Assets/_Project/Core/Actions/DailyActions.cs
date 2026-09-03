using System;
using Bloodhound.Engine;
using GridInfect.Core.Solving;

namespace GridInfect.Core
{
    // daily.begin { dateUtc, nowMs }: the board is a pure function of the
    // date, the clock is a stat. The adapter supplies both (wall clock via
    // input), so a log replays to the same board and the same times.
    public sealed class BeginDailyAction : GameAction<GameState>
    {
        public override string Name => "daily.begin";

        public override string Validate(GameState state, ActionInput input)
        {
            string dateUtc = input.Str("dateUtc");
            if (!DailySpec.TryParseDate(dateUtc, out _)) return $"dateUtc '{dateUtc}' is not {DailySpec.DateFormat}";
            input.Long("nowMs");
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            string dateUtc = input.Str("dateUtc");
            var level = DailySpec.Build(dateUtc) ?? throw new InvalidOperationException($"no daily board for {dateUtc}");
            state.Mode = GameMode.Daily;
            state.ClassicLevelId = -1;
            state.FreePlayDefs = null;
            state.FreePlayRun = null;
            state.EndlessRun = null;
            state.DailyRun = new DailyRun
            {
                DateUtc = dateUtc,
                Seed = level.Seed,
                StartedMs = input.Long("nowMs"),
                TraceLength = level.Trace.Length,
                Grade = level.Grade,
                ParMs = DailySpec.ParMs(level.Trace.Length, level.Grade),
            };
            state.SetSession(new LevelSession(level.Def));
        }
    }

    // daily.complete { nowMs }: elapsed, personal best per date, streak.
    // Rejects a backward clock. Completing the same date again records a
    // better time but never moves the streak.
    public sealed class CompleteDailyAction : GameAction<GameState>
    {
        public override string Name => "daily.complete";

        public override string Validate(GameState state, ActionInput input)
        {
            if (state.Mode != GameMode.Daily || state.DailyRun == null) return "no daily in progress";
            if (state.DailyRun.Completed) return "daily already completed";
            if (state.Session == null || !state.Session.Solved) return "board not solved";
            if (input.Long("nowMs") < state.DailyRun.StartedMs) return "clock moved backward";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            var run = state.DailyRun;
            var profile = state.Profile;
            run.CompletedMs = input.Long("nowMs");
            long elapsed = run.CompletedMs - run.StartedMs;

            if (!profile.DailyBestMs.TryGetValue(run.DateUtc, out long best) || elapsed < best)
            {
                profile.DailyBestMs[run.DateUtc] = elapsed;
            }

            if (profile.DailyLastDate != run.DateUtc)
            {
                DailySpec.TryParseDate(run.DateUtc, out DateTime today);
                bool consecutive = DailySpec.TryParseDate(profile.DailyLastDate, out DateTime last)
                                   && last.AddDays(1) == today;
                profile.DailyStreak = consecutive ? profile.DailyStreak + 1 : 1;
                profile.DailyLastDate = run.DateUtc;
                run.StreakGrantDue = profile.DailyStreak % 7 == 0;
            }
            profile.Dirty = true;
        }
    }

    // endless.begin { grade, seed }: no clock; level n of the run is the
    // first accepted seed from seed + n * EndlessRun stride, so the whole
    // run replays from the log.
    public sealed class BeginEndlessAction : GameAction<GameState>
    {
        public const ulong Stride = 100_000;

        public override string Name => "endless.begin";

        public override string Validate(GameState state, ActionInput input)
        {
            int grade = input.Int("grade");
            if (grade < (int)Grade.G1 || grade > (int)Grade.G5) return $"grade {grade} out of range";
            input.Long("seed");
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            var grade = (Grade)input.Int("grade");
            ulong seed = (ulong)input.Long("seed");
            var level = DailySpec.FirstAccepted(DailySpec.Endless(grade), seed)
                        ?? throw new InvalidOperationException("no endless board from this seed");
            state.Mode = GameMode.Endless;
            state.ClassicLevelId = -1;
            state.FreePlayDefs = null;
            state.FreePlayRun = null;
            state.DailyRun = null;
            state.EndlessRun = new EndlessRun { Grade = grade, Seed = seed, Index = 0, Streak = 0, LevelSeed = level.Seed };
            state.SetSession(new LevelSession(level.Def));
        }
    }

    // endless.advance: the current board is solved; count it (a solve with
    // no reset extends the streak, a reset ends it) and load the next.
    public sealed class AdvanceEndlessAction : GameAction<GameState>
    {
        public override string Name => "endless.advance";

        public override string Validate(GameState state, ActionInput input)
        {
            if (state.Mode != GameMode.Endless || state.EndlessRun == null) return "no endless run";
            if (state.Session == null || !state.Session.Solved) return "board not solved";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            var run = state.EndlessRun;
            run.Streak = state.Session.Resets == 0 ? run.Streak + 1 : 1;
            run.Index++;
            int g = (int)run.Grade - 1;
            if (run.Streak > state.Profile.EndlessBest[g])
            {
                state.Profile.EndlessBest[g] = run.Streak;
                state.Profile.Dirty = true;
            }
            var level = DailySpec.FirstAccepted(DailySpec.Endless(run.Grade), run.Seed + (ulong)run.Index * BeginEndlessAction.Stride)
                        ?? throw new InvalidOperationException("no endless board from this seed");
            run.LevelSeed = level.Seed;
            state.SetSession(new LevelSession(level.Def));
        }
    }

    public sealed class AbortEndlessAction : GameAction<GameState>
    {
        public override string Name => "endless.abort";

        public override string Validate(GameState state, ActionInput input)
        {
            if (state.Mode != GameMode.Endless) return "no endless run";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            state.Mode = GameMode.Classic;
            state.EndlessRun = null;
            state.SetSession(null);
        }
    }
}
