using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;

namespace GridInfect.Game
{
    // Portrait: a title block in the top third, the four ways in stacked at
    // the middle where a thumb sits, and settings pinned low. Audio, privacy
    // and the progress reset all live behind SETTINGS now rather than
    // stacking up under the menu.
    public sealed class MainMenuScreen : AppScreen
    {

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;

            var title = Ui.MakeText("title", Root.transform, "GRID INFECT", L.TitleText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, h * 0.28f);
            var subtitle = Ui.MakeText("subtitle", Root.transform, "infect every cell",
                L.BodyText, BoardTheme.TextDim, 2);
            Ui.SetPos(subtitle.gameObject, 0f, h * 0.28f - L.TitleText * 0.9f - L.BodyText);

            // PLAY is the worlds (stage 3), DAILY and ENDLESS replace timed
            // Free Play (stage 4; its actions stay for log replay), and the
            // 128 classic levels live on as LEGACY: unchanged rules, no hints.
            var size = new Vector2(L.ContentWidth, L.ButtonHeight);
            Buttons.Add(UiButton.Make(Root.transform, "PLAY",
                new Vector2(0f, L.StackRowY(0, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.Primary, BoardTheme.TextOnAccent, () => App.Screens.Show(new WorldSelectScreen())));
            Buttons.Add(UiButton.Make(Root.transform, "DAILY",
                new Vector2(0f, L.StackRowY(1, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new DailyScreen())));
            Buttons.Add(UiButton.Make(Root.transform, "ENDLESS",
                new Vector2(0f, L.StackRowY(2, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new EndlessScreen())));
            Buttons.Add(UiButton.Make(Root.transform, "LEGACY",
                new Vector2(0f, L.StackRowY(3, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new ClassicSelectScreen())));

            // The dev unlock-all row is gone with the gates it opened: every
            // level is already open, so there was nothing left for it to do.
            Buttons.Add(UiButton.Make(Root.transform, "SETTINGS", new Vector2(0f, -h * 0.38f),
                new Vector2(L.ContentWidth, L.BarHeight),
                BoardTheme.ButtonBgDisabled, BoardTheme.TextDim, () => App.Screens.Show(new SettingsScreen())));
        }
    }
}
