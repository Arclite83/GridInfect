using Bloodhound.Engine;

namespace GridInfect.Core
{
    // Unlock gating is presentation policy (as for level.load); tests and
    // tools load anything.
    public sealed class LoadWorldLevelAction : GameAction<GameState>
    {
        public override string Name => "world.load";

        public override string Validate(GameState state, ActionInput input)
        {
            string worldId = input.Str("worldId");
            World w = Worlds.Get(worldId);
            if (w == null) return $"unknown world '{worldId}'";
            int index = input.Int("index");
            if (index < 0 || index >= w.Count) return $"index {index} out of range for world '{worldId}'";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            string worldId = input.Str("worldId");
            int index = input.Int("index");
            state.Mode = GameMode.World;
            state.ClassicLevelId = -1;
            state.FreePlayDefs = null;
            state.FreePlayRun = null;
            state.WorldId = worldId;
            state.WorldIndex = index;
            state.Solution = Worlds.Solution(worldId, index);
            state.SetSession(new LevelSession(Worlds.Level(worldId, index)));
        }
    }

    // A world becomes playable (its first level opens). Dispatched by the
    // adapter when the previous world's last level is solved, so replay
    // reproduces progression.
    public sealed class UnlockWorldAction : GameAction<GameState>
    {
        public override string Name => "progress.unlockWorld";

        public override string Validate(GameState state, ActionInput input)
        {
            string worldId = input.Str("worldId");
            if (Worlds.Get(worldId) == null) return $"unknown world '{worldId}'";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            string worldId = input.Str("worldId");
            var unlocked = state.Profile.WorldUnlocked;
            if (!unlocked.TryGetValue(worldId, out int n) || n < 1)
            {
                unlocked[worldId] = 1;
                state.Profile.Dirty = true;
            }
        }
    }

    // Level `index` of a world becomes playable; index == Count marks the
    // world finished. Solving level N dispatches this for N+1.
    public sealed class UnlockWorldLevelAction : GameAction<GameState>
    {
        public override string Name => "progress.unlockWorldLevel";

        public override string Validate(GameState state, ActionInput input)
        {
            string worldId = input.Str("worldId");
            World w = Worlds.Get(worldId);
            if (w == null) return $"unknown world '{worldId}'";
            int index = input.Int("index");
            if (index < 0 || index > w.Count) return $"index {index} out of range for world '{worldId}'";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            string worldId = input.Str("worldId");
            int index = input.Int("index");
            var unlocked = state.Profile.WorldUnlocked;
            int current = unlocked.TryGetValue(worldId, out int n) ? n : 0;
            if (index + 1 > current)
            {
                unlocked[worldId] = index + 1;
                state.Profile.Dirty = true;
            }
        }
    }
}
