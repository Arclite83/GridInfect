using Bloodhound.Engine;

namespace GridInfect.Core
{
    /// <summary>
    /// freeplay.begin — the BEGIN button: starts the run clock. Wall-clock
    /// milliseconds arrive as input so the log replays deterministically
    /// (MODES.md §2.2: wall clock, backgrounding does not pause it).
    /// </summary>
    public sealed class BeginFreePlayAction : GameAction<GameState>
    {
        public override string Name => "freeplay.begin";

        public override string Validate(GameState state, ActionInput input)
        {
            if (state.Mode != GameMode.FreePlay) return "not in free play";
            if (state.FreePlayRun != null) return "run already begun";
            input.Long("nowMs");
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            state.FreePlayRun = new FreePlayRun { StartedMs = input.Long("nowMs") };
        }
    }

    /// <summary>
    /// freeplay.advance — after solving levels 1–4 of a run, bind the next
    /// generated level. The clock keeps running; there is no pause between
    /// levels.
    /// </summary>
    public sealed class AdvanceFreePlayAction : GameAction<GameState>
    {
        public override string Name => "freeplay.advance";

        public override string Validate(GameState state, ActionInput input)
        {
            if (state.Mode != GameMode.FreePlay) return "not in free play";
            if (state.Session == null || !state.Session.Solved) return "current level not solved";
            if (state.FreePlayDefs == null || state.FreePlayIndex >= state.FreePlayDefs.Length - 1)
                return "no next level (run finished)";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            state.FreePlayIndex++;
            state.SetSession(new LevelSession(state.FreePlayDefs[state.FreePlayIndex]));
        }
    }

    /// <summary>
    /// freeplay.complete — the 5th solve stops the clock and records the
    /// result: best time overwritten iff lower or none, completion count
    /// incremented (drives the difficulty unlock ladder, MODES.md §2.3–2.4).
    /// A backward-moving clock is rejected here; the adapter's running
    /// display also aborts the run on a negative duration (cheat guard).
    /// </summary>
    public sealed class CompleteFreePlayAction : GameAction<GameState>
    {
        public override string Name => "freeplay.complete";

        public override string Validate(GameState state, ActionInput input)
        {
            if (state.Mode != GameMode.FreePlay) return "not in free play";
            var run = state.FreePlayRun;
            if (run == null) return "run not begun";
            if (run.Completed) return "run already completed";
            if (state.Session == null || !state.Session.Solved) return "current level not solved";
            if (state.FreePlayDefs == null || state.FreePlayIndex != state.FreePlayDefs.Length - 1)
                return "not on the final level of the run";
            if (input.Long("nowMs") < run.StartedMs) return "clock moved backward";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            var run = state.FreePlayRun;
            run.CompletedMs = input.Long("nowMs");
            long duration = run.CompletedMs - run.StartedMs;

            int d = (int)state.Difficulty;
            var profile = state.Profile;
            if (profile.BestTimesMs[d] == 0 || duration < profile.BestTimesMs[d])
            {
                profile.BestTimesMs[d] = duration;
            }
            profile.FreePlayCounts[d]++;
            profile.Dirty = true;
        }
    }

    /// <summary>
    /// freeplay.abort — leave a run (back-out or the cheat guard tripping).
    /// Nothing is recorded.
    /// </summary>
    public sealed class AbortFreePlayAction : GameAction<GameState>
    {
        public override string Name => "freeplay.abort";

        public override string Validate(GameState state, ActionInput input)
        {
            if (state.Mode != GameMode.FreePlay) return "not in free play";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            state.Mode = GameMode.Classic;
            state.FreePlayDefs = null;
            state.FreePlayRun = null;
            state.SetSession(null);
        }
    }
}
