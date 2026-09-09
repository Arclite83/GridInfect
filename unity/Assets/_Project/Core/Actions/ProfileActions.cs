using Bloodhound.Engine;

namespace GridInfect.Core
{
    public sealed class UnlockLevelAction : GameAction<GameState>
    {
        public override string Name => "progress.unlock";

        public override string Validate(GameState state, ActionInput input)
        {
            int id = input.Int("levelId");
            if (id < 0 || id >= ClassicLevels.Count) return $"levelId {id} out of range";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            if (state.Profile.Unlocked.Add(input.Int("levelId")))
            {
                state.Profile.Dirty = true;
            }
        }
    }

    // progress.unlockAll { }: open every Legacy level, every world and every
    // world level at once. A development affordance (the adapter only offers
    // it in a debug build), but it writes progression, so it is an action
    // like any other and replays from the log.
    public sealed class UnlockEverythingAction : GameAction<GameState>
    {
        public override string Name => "progress.unlockAll";

        public override string Validate(GameState state, ActionInput input) => null;

        public override void Execute(GameState state, ActionInput input)
        {
            var profile = state.Profile;
            for (int id = 0; id < ClassicLevels.Count; id++) profile.Unlocked.Add(id);
            foreach (World world in Worlds.All)
            {
                // Count + 1 is the "finished" marker unlockWorldLevel writes
                // when the last level is solved (Queries.IsWorldFinished).
                profile.WorldUnlocked[world.Id] = world.Count + 1;
            }
            profile.Dirty = true;
        }
    }

    // progress.solved { levelId }: a Legacy level has been beaten. Nothing
    // is gated on it — every level is open — so this is the whole record of
    // progress, and what paints its select tile infected.
    public sealed class SolveLevelAction : GameAction<GameState>
    {
        public override string Name => "progress.solved";

        public override string Validate(GameState state, ActionInput input)
        {
            int id = input.Int("levelId");
            if (id < 0 || id >= ClassicLevels.Count) return $"levelId {id} out of range";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            if (state.Profile.SolvedClassic.Add(input.Int("levelId")))
            {
                state.Profile.Dirty = true;
            }
        }
    }

    // progress.reset { }: forget every level beaten and every score, the way
    // a fresh install starts. Offered at the bottom of settings. The lock
    // wallet survives: locks are earned or paid for, and clearing a board
    // record is not a reason to take them.
    public sealed class ResetProgressAction : GameAction<GameState>
    {
        public override string Name => "progress.reset";

        public override string Validate(GameState state, ActionInput input) => null;

        public override void Execute(GameState state, ActionInput input)
        {
            var profile = state.Profile;
            profile.SolvedClassic.Clear();
            profile.SolvedWorld.Clear();
            profile.Unlocked.Clear();
            profile.WorldUnlocked.Clear();
            profile.DailyBestMs.Clear();
            profile.DailyStreak = 0;
            profile.DailyLastDate = "";
            for (int d = 0; d < 5; d++)
            {
                profile.BestTimesMs[d] = 0;
                profile.FreePlayCounts[d] = 0;
                profile.EndlessBest[d] = 0;
            }
            profile.Dirty = true;
        }
    }

    public sealed class SetMutedAction : GameAction<GameState>
    {
        public override string Name => "settings.mute";

        public override string Validate(GameState state, ActionInput input)
        {
            input.Bool("muted");
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            bool muted = input.Bool("muted");
            if (state.Profile.Muted != muted)
            {
                state.Profile.Muted = muted;
                state.Profile.Dirty = true;
            }
        }
    }
}
