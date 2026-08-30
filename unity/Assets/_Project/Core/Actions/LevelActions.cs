using Bloodhound.Engine;

namespace GridInfect.Core
{
    /// <summary>
    /// level.load — enter a classic level. Deliberately does not gate on the
    /// unlock ladder: the lock is presentation policy (disabled buttons), as
    /// in the original, and tests/tools may load any level.
    /// </summary>
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
            state.SetSession(new LevelSession(ClassicLevels.Get(id)));
        }
    }

    /// <summary>
    /// level.generate — start a Free Play run: generate the levels for the
    /// chosen difficulty from an explicit seed (deterministic under replay;
    /// the adapter picks the seed) and bind the first one.
    /// </summary>
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
            for (int n = 0; n < count; n++)
            {
                defs[n] = LevelGenerator.Generate(difficulty, ref rng);
            }

            state.Mode = GameMode.FreePlay;
            state.Difficulty = difficulty;
            state.ClassicLevelId = -1;
            state.FreePlayDefs = defs;
            state.FreePlayIndex = 0;
            state.FreePlayRun = null; // timer starts at freeplay.begin (BEGIN button)
            state.SetSession(new LevelSession(defs[0]));
        }
    }

    /// <summary>
    /// level.reset — the in-level replay button while unsolved: full board
    /// reset (a solved level reloads fresh via level.load instead — adapter
    /// policy, MODES.md §1.1).
    /// </summary>
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
