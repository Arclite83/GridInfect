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
                float y = h * 0.28f - n * h * 0.14f;

                var captured = difficulty;
                Buttons.Add(UiButton.Make(Root.transform, difficulty.ToString().ToUpperInvariant(),
                    new Vector2(-w * 0.12f, y), new Vector2(w * 0.44f, h * 0.11f),
                    BoardTheme.ButtonBg, BoardTheme.Text,
                    () => StartRun(captured)));

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
