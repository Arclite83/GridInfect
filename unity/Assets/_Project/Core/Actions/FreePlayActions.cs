using Bloodhound.Engine;

namespace GridInfect.Core
{
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
            state.Solution = state.FreePlaySolutions != null ? state.FreePlaySolutions[state.FreePlayIndex] : null;
            state.SetSession(new LevelSession(state.FreePlayDefs[state.FreePlayIndex]));
        }
    }

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
