using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;

namespace GridInfect.Game
{
    // Portrait: five full-width rows centred in the screen, each carrying its
    // own best time on the right rather than in a second column.
    public sealed class FreePlayMenuScreen : AppScreen
    {
        static readonly Difficulty[] Order =
        {
            Difficulty.Beginner, Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Challenging
        };

        protected override void Build()
        {
            var title = Ui.MakeText("title", Root.transform, "FREE PLAY", L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            Buttons.Add(UiButton.Make(Root.transform, "MENU", L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));

            var profile = App.State.Profile;
            var size = new Vector2(L.ContentWidth, L.ButtonHeight);

            for (int n = 0; n < Order.Length; n++)
            {
                var difficulty = Order[n];
                float y = L.StackRowY(n, Order.Length, L.ButtonHeight, 0f);

                var captured = difficulty;
                Buttons.Add(UiButton.Make(Root.transform, difficulty.ToString().ToUpperInvariant(),
                    new Vector2(0f, y), size, BoardTheme.ButtonBg, BoardTheme.Text, () => StartRun(captured)));

                var best = Ui.MakeText($"best:{difficulty}", Root.transform,
                    Queries.FormatBestTime(profile.BestTimesMs[n]), L.LabelText, BoardTheme.Accent, 2);
                Ui.SetPos(best.gameObject, L.ContentWidth / 2f - L.Gap * 2.5f, y);
            }
        }

        void StartRun(Difficulty difficulty)
        {
            // The seed is the adapter's pick (wall clock); it enters the log,
            // so the run — boards included — replays deterministically. Five
            // levels are generated up front, so the dispatch goes behind the
            // transition's LOADING card.
            App.Screens.Show(new BoardScreen(), prepare: () => App.Do(GridInfectActions.LevelGenerate,
                Inputs.LevelGenerate(difficulty, GameApp.NowMs())).Applied);
        }
    }
}
