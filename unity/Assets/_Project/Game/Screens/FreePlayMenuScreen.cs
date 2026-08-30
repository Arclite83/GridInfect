using GridInfect.Core;
using UnityEngine;

namespace GridInfect.Game
{
    public sealed class FreePlayMenuScreen : AppScreen
    {
        static readonly Difficulty[] Order =
        {
            Difficulty.Beginner, Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Challenging
        };

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;
            float w = UnityEngine.Screen.width;

            var title = Ui.MakeText("title", Root.transform, "FREE PLAY", h * 0.05f, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, h * 0.44f);
            Buttons.Add(UiButton.Make(Root.transform, "MENU",
                new Vector2(-w * 0.42f, h * 0.44f), new Vector2(w * 0.12f, h * 0.07f),
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));

            var profile = App.State.Profile;
            for (int n = 0; n < Order.Length; n++)
            {
                var difficulty = Order[n];
                bool unlocked = Queries.IsDifficultyUnlocked(profile, difficulty);
                float y = h * 0.28f - n * h * 0.14f;

                string label = difficulty.ToString().ToUpperInvariant();
                if (!unlocked)
                {
                    int remaining = Queries.RunsRemainingToUnlock(profile, difficulty);
                    string previous = Order[n - 1].ToString().ToUpperInvariant();
                    label = Queries.IsDifficultyUnlocked(profile, Order[n - 1])
                        ? $"PLAY {previous} {remaining} MORE TIME{(remaining == 1 ? "" : "S")}"
                        : label + " LOCKED";
                }

                var captured = difficulty;
                var button = UiButton.Make(Root.transform, label,
                    new Vector2(-w * 0.12f, y), new Vector2(w * 0.44f, h * 0.11f),
                    unlocked ? BoardTheme.ButtonBg : BoardTheme.ButtonBgDisabled,
                    unlocked ? BoardTheme.Text : BoardTheme.TextDim,
                    () => StartRun(captured));
                button.Enabled = unlocked;
                Buttons.Add(button);

                var best = Ui.MakeText($"best:{difficulty}", Root.transform,
                    Queries.FormatBestTime(profile.BestTimesMs[n]), h * 0.04f, BoardTheme.Accent, 2);
                Ui.SetPos(best.gameObject, w * 0.26f, y);
            }
        }

        void StartRun(Difficulty difficulty)
        {
            // The seed is the adapter's pick (wall clock); it enters the log,
            // so the run — boards included — replays deterministically.
            var generate = App.Do(GridInfectActions.LevelGenerate,
                Inputs.LevelGenerate(difficulty, GameApp.NowMs()));
            if (generate.Applied)
            {
                App.Screens.Show(new BoardScreen());
            }
        }
    }
}
