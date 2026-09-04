using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;

namespace GridInfect.Game
{
    // Daily: today's UTC date, the streak, the personal best, one BEGIN.
    // The board is the same for everyone (DailySpec); the clock is a stat.
    public sealed class DailyScreen : AppScreen
    {
        protected override void Build()
        {
            float h = UnityEngine.Screen.height;
            var profile = App.State.Profile;
            string today = GameApp.TodayUtc();

            var title = Ui.MakeText("title", Root.transform, "DAILY", L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            Buttons.Add(UiButton.Make(Root.transform, "MENU", L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));

            var date = Ui.MakeText("date", Root.transform, today, L.TitleText, BoardTheme.Text, 2);
            Ui.SetPos(date.gameObject, 0f, h * 0.18f);

            int streak = Queries.DailyStreakOn(profile, today);
            long best = Queries.DailyBestMs(profile, today);
            var stats = Ui.MakeText("stats", Root.transform,
                $"STREAK {streak}\nBEST {(best > 0 ? Queries.FormatDuration(best) : "--:--:---")}",
                L.LabelText, BoardTheme.Accent, 2);
            Ui.SetPos(stats.gameObject, 0f, h * 0.06f);

            Buttons.Add(UiButton.Make(Root.transform, best > 0 ? "PLAY AGAIN" : "BEGIN",
                new Vector2(0f, -h * 0.08f), new Vector2(L.ContentWidth * 0.55f, L.ButtonHeight),
                BoardTheme.Primary, BoardTheme.TextOnAccent, () =>
                {
                    // Date and clock are the adapter's inputs; both enter the
                    // log. The dispatch runs behind the transition's LOADING
                    // card (ScreenManager.Show), and a rejection cancels the
                    // navigation instead of landing on an empty board.
                    App.Screens.Show(new BoardScreen(), prepare: () =>
                        App.Do(GridInfectActions.DailyBegin, Inputs.DailyBegin(today, GameApp.NowMs())).Applied);
                }));
        }
    }

    // Endless: pick a grade, no clock, a streak of solves without a reset.
    public sealed class EndlessScreen : AppScreen
    {
        protected override void Build()
        {
            var title = Ui.MakeText("title", Root.transform, "ENDLESS", L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            Buttons.Add(UiButton.Make(Root.transform, "MENU", L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));

            var profile = App.State.Profile;
            var size = new Vector2(L.ContentWidth, L.ButtonHeight);
            for (int g = 1; g <= 5; g++)
            {
                var grade = (Core.Solving.Grade)g;
                float y = L.StackRowY(g - 1, 5, L.ButtonHeight, 0f);
                Buttons.Add(UiButton.Make(Root.transform, $"GRADE {g}", new Vector2(0f, y), size,
                    BoardTheme.ButtonBg, BoardTheme.Text, () =>
                    {
                        // The seed is the adapter's pick (wall clock); it enters
                        // the log, so the run — boards included — replays.
                        // endless.begin generates on the device and the higher
                        // grades take seconds of solver work, so it runs behind
                        // the transition's LOADING card rather than freezing
                        // this menu with its buttons still live.
                        App.Screens.Show(new BoardScreen(), prepare: () =>
                            App.Do(GridInfectActions.EndlessBegin, Inputs.EndlessBegin(grade, GameApp.NowMs())).Applied);
                    }));
                var best = Ui.MakeText($"best:{g}", Root.transform, $"BEST {profile.EndlessBest[g - 1]}",
                    L.LabelText, BoardTheme.Accent, 2);
                Ui.SetPos(best.gameObject, L.ContentWidth / 2f - L.Gap * 2.5f, y);
            }
        }
    }

    // Where a completed daily's time goes beyond the local profile. Friends
    // leaderboards (Play Games Services v2) are out of stage 4; the shipped
    // sink keeps it local.
    public interface IDailyScoreSink
    {
        void Submit(string dateUtc, long elapsedMs, long parMs);
    }

    public sealed class LocalDailyScoreSink : IDailyScoreSink
    {
        public void Submit(string dateUtc, long elapsedMs, long parMs) { }
    }
}
