using Bloodhound.Engine;

namespace GridInfect.Core
{
    // tutorial.load { index }: enter a tutorial step. The stored solution is
    // the marks the board screen draws; nothing is gated, so a test or a
    // tool can load any step, and the forward chevron's gate is the
    // adapter's (progress gating is presentation policy throughout).
    public sealed class LoadTutorialAction : GameAction<GameState>
    {
        public override string Name => "tutorial.load";

        public override string Validate(GameState state, ActionInput input)
        {
            int index = input.Int("index");
            if (index < 0 || index >= TutorialLevels.Count) return $"index {index} out of range for the tutorial";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            int index = input.Int("index");
            var step = TutorialLevels.Get(index);
            state.Mode = GameMode.Tutorial;
            state.ClassicLevelId = -1;
            state.FreePlayDefs = null;
            state.FreePlayRun = null;
            state.TutorialIndex = index;
            state.Solution = step.Solution;
            state.SetSession(new LevelSession(step.Def));
        }
    }

    // tutorial.solved { index }: a step has been beaten. Keeps the highest
    // step beaten plus one, which is what the series resumes at and how far
    // its forward chevron opens.
    public sealed class SolveTutorialStepAction : GameAction<GameState>
    {
        public override string Name => "tutorial.solved";

        public override string Validate(GameState state, ActionInput input)
        {
            int index = input.Int("index");
            if (index < 0 || index >= TutorialLevels.Count) return $"index {index} out of range for the tutorial";
            return null;
        }

        public override void Execute(GameState state, ActionInput input)
        {
            int index = input.Int("index");
            if (index + 1 > state.Profile.TutorialStep)
            {
                state.Profile.TutorialStep = index + 1;
                state.Profile.Dirty = true;
            }
        }
    }

    // tutorial.seen: the first-open offer has been answered, taken or
    // skipped. It is asked once.
    public sealed class SeeTutorialAction : GameAction<GameState>
    {
        public override string Name => "tutorial.seen";

        public override string Validate(GameState state, ActionInput input) => null;

        public override void Execute(GameState state, ActionInput input)
        {
            if (state.Profile.TutorialSeen) return;
            state.Profile.TutorialSeen = true;
            state.Profile.Dirty = true;
        }
    }
}
