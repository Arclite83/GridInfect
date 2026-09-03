using Bloodhound.Engine;

namespace GridInfect.Core
{
    // Unlock gating is presentation policy (as in the original); tests and tools load anything.
    public sealed class LoadLevelAction : GameAction<GameState>
    {
        public override string Name => "level.load";

        public override string Validate(GameState state, ActionInput input)
        {
            int id = input.Int("levelId");
            if (id < 0 || id >= ClassicLevels.Count) return $"levelId {id} out of range";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            int id = input.Int("levelId");
            state.Mode = GameMode.Classic;
            state.ClassicLevelId = id;
            state.FreePlayDefs = null;
            state.FreePlayRun = null;
            state.Solution = ClassicLevels.Solution(id);
            state.SetSession(new LevelSession(ClassicLevels.Get(id)));
        }
    }

    public sealed class GenerateLevelsAction : GameAction<GameState>
    {
        public const int LevelsPerRun = 5;

        public override string Name => "level.generate";

        public override string Validate(GameState state, ActionInput input)
        {
            int difficulty = input.Int("difficulty");
            if (difficulty < 0 || difficulty > (int)Difficulty.Challenging)
                return $"difficulty {difficulty} out of range";
            input.Long("seed"); // presence + shape
            int count = input.IntOr("count", LevelsPerRun);
            if (count < 1 || count > 32) return $"count {count} out of range";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            var difficulty = (Difficulty)input.Int("difficulty");
            int count = input.IntOr("count", LevelsPerRun);
            var rng = new Pcg32((ulong)input.Long("seed"));

            var defs = new LevelDef[count];
            var solutions = new (int piece, int cell)[count][];
            for (int n = 0; n < count; n++)
            {
                defs[n] = LevelGenerator.Generate(difficulty, ref rng, out var sampled);
                solutions[n] = new (int, int)[sampled.Length];
                for (int k = 0; k < sampled.Length; k++) solutions[n][k] = (k, Grid.Loc(sampled[k].i, sampled[k].j));
            }

            state.Mode = GameMode.FreePlay;
            state.Difficulty = difficulty;
            state.ClassicLevelId = -1;
            state.FreePlayDefs = defs;
            state.FreePlaySolutions = solutions;
            state.FreePlayIndex = 0;
            state.FreePlayRun = null; // timer starts at freeplay.begin (BEGIN button)
            state.Solution = solutions[0];
            state.SetSession(new LevelSession(defs[0]));
        }
    }

    public sealed class ResetLevelAction : GameAction<GameState>
    {
        public override string Name => "level.reset";

        public override string Validate(GameState state, ActionInput input)
        {
            var s = state.Session;
            if (s == null) return "no level loaded";
            if (s.ResolutionPending) return "resolution pending — dispatch board.resolve first";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            Rules.FullReset(state.Session);
        }
    }
}
