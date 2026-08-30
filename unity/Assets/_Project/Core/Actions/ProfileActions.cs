using Bloodhound.Engine;

namespace GridInfect.Core
{
    /// <summary>
    /// progress.unlock — record a classic level as unlocked. Dispatched by the
    /// adapter when a solve unlocks its successor (solving id N unlocks id
    /// N+1 and nothing else, MODES.md §1.1); logged so replay reproduces
    /// progression.
    /// </summary>
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

    /// <summary>settings.mute — audio preference, persisted.</summary>
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
