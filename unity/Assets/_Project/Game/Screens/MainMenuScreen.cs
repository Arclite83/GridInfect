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

            // DAILY and ENDLESS replace timed Free Play (stage 4; its actions
            // stay for log replay), and the 128 classic levels live on as
            // LEGACY: unchanged rules, no hints. Four ways in, none of them
            // louder than the others.
            var size = new Vector2(L.ContentWidth, L.ButtonHeight);
            // WORLDS, and plain glass: it says where it goes, and the
            // infection is what a beaten level wears now — a menu row in the
            // same red reads as one already done.
            Buttons.Add(UiButton.Make(Root.transform, "WORLDS",
                new Vector2(0f, L.StackRowY(0, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new WorldSelectScreen())));
            Buttons.Add(UiButton.Make(Root.transform, "DAILY",
                new Vector2(0f, L.StackRowY(1, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new DailyScreen())));
            Buttons.Add(UiButton.Make(Root.transform, "ENDLESS",
                new Vector2(0f, L.StackRowY(2, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new EndlessScreen())));
            Buttons.Add(UiButton.Make(Root.transform, "LEGACY",
                new Vector2(0f, L.StackRowY(3, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new ClassicSelectScreen())));

            // Settings is a gear in the top-right corner, where every other
            // screen puts its one right-hand chip. As a dimmed full-width row
            // at the bottom it was both hard to read and as loud as the four
            // things that actually are the game.
            //
            // Both corner chips are icon chips at the icon size (39 px on the
            // reference screen, 44 to the finger): they were BarHeight
            // squares, 29 px, and the help mark inside was the label "?" at
            // 12 px — the smallest thing on the menu, for the button a new
            // player needs first.
            var chip = new Vector2(L.IconChip, L.IconChip);
            int icon = UiButton.IconPx(chip);
            var palette = BoardPalette.Default;
            Buttons.Add(UiButton.MakeIcon(Root.transform, "settings", BugGlyph.Gear(palette, icon),
                new Vector2(-L.BackPos.x, L.BackPos.y), chip, () => App.Screens.Show(new SettingsScreen())));

            // The rules sit beside it, reachable before the first tap on
            // anything else — which is when a player wants them.
            Buttons.Add(UiButton.MakeIcon(Root.transform, "help", BugGlyph.Question(palette, icon),
                new Vector2(-L.BackPos.x - chip.x - L.Gap * 0.7f, L.BackPos.y), chip,
                () => App.Screens.Show(new RulesScreen())));
        }
    }
}
