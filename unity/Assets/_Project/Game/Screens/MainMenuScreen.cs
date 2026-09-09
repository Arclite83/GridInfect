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
            // anything else — which is when a player wants them. ChipPitch,
            // not a bare gap: at chip.x + Gap * 0.7 the two chips cleared
            // each other but their copper pads did not, so the help mark and
            // the gear looked joined by one smear of yellow dots.
            Buttons.Add(UiButton.MakeIcon(Root.transform, "help", BugGlyph.Question(palette, icon),
                new Vector2(-L.BackPos.x - L.ChipPitch(chip.x), L.BackPos.y), chip,
                () => App.Screens.Show(new RulesScreen())));

            // The tutorial, under the four ways in: a shorter chip, the same
            // glass and the same ink as the rows above it (nothing on this
            // screen is dimmed; hierarchy is size). It is the way back into
            // the series for anyone who skipped it or wants a step again.
            float below = L.StackRowY(3, 4, L.ButtonHeight, 0f) - L.ButtonHeight / 2f - L.Gap - L.BarHeight / 2f;
            Buttons.Add(UiButton.Make(Root.transform, "TUTORIAL",
                new Vector2(0f, below), new Vector2(L.ContentWidth * 0.5f, L.BarHeight),
                BoardTheme.ButtonBg, BoardTheme.Text, OpenTutorial));

            if (!App.State.Profile.TutorialSeen) OfferTutorial();
        }

        // Where the series is picked up: the step after the last one beaten,
        // or the first again once every step has been.
        void OpenTutorial()
        {
            int step = App.State.Profile.TutorialStep;
            int index = step >= TutorialLevels.Count ? 0 : step;
            App.Screens.Show(new BoardScreen(),
                prepare: () => App.Do(GridInfectActions.TutorialLoad, Inputs.Tutorial(index)).Applied);
        }

        // The first open: one offer, answered once either way (tutorial.seen).
        // A solid plate, not the board popup's glass: here it sits over the
        // title and the four rows, and through glass they were the
        // message's background. The menu under it is shut off until it is
        // answered, as the board is under the COMPLETE popup.
        GameObject _offer;
        GameObject _offerPanel;

        void OfferTutorial()
        {
            foreach (var button in Buttons) button.Enabled = false;

            _offer = new GameObject("offer");
            _offer.transform.SetParent(Root.transform, false);
            Ui.MakeRect("dim", _offer.transform,
                new Vector2(UnityEngine.Screen.width, UnityEngine.Screen.height), BoardTheme.PanelDim, 40);

            float short_ = PresentationConfig.ShortEdge;
            var panel = new GameObject("panel");
            panel.transform.SetParent(_offer.transform, false);
            _offerPanel = panel;
            Ui.MakeGlass("bg", panel.transform, new Vector2(L.ContentWidth, short_ * 0.40f), GlassStyle.Plate(BoardPalette.Default), 41);
            var title = Ui.MakeText("title", panel.transform, "FIRST TIME?", L.HeadingText, BoardTheme.Text, 42, bold: true);
            Ui.SetPos(title.gameObject, 0f, short_ * 0.10f);
            var line = Ui.MakeText("line", panel.transform, "Learn the basics in a minute.", L.BodyText, BoardTheme.Text, 42);
            Ui.SetPos(line.gameObject, 0f, short_ * 0.03f);

            float y = -short_ * 0.08f;
            float step = L.ContentWidth / 4f;
            var size = new Vector2(step * 1.6f, L.BarHeight);
            Buttons.Add(UiButton.Make(panel.transform, "TUTORIAL", new Vector2(-step, y), size,
                BoardTheme.Primary, BoardTheme.TextOnAccent, () =>
                {
                    App.Do(GridInfectActions.TutorialSeen);
                    App.Screens.Show(new BoardScreen(),
                        prepare: () => App.Do(GridInfectActions.TutorialLoad, Inputs.Tutorial(0)).Applied);
                }, 43));
            Buttons.Add(UiButton.Make(panel.transform, "SKIP", new Vector2(step, y), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () =>
                {
                    App.Do(GridInfectActions.TutorialSeen);
                    CloseOffer();
                }, 43));

            panel.transform.localPosition = new Vector3(0f, -UnityEngine.Screen.height, 0f);
            App.Tweens.MoveTo(panel.transform, Vector3.zero, PresentationConfig.PopupSlide);
        }

        void CloseOffer()
        {
            if (_offer == null) return;
            Buttons.RemoveAll(b => b.Root == null || b.Root.transform.parent == _offerPanel.transform);
            Object.Destroy(_offer);
            _offer = null;
            _offerPanel = null;
            foreach (var button in Buttons) button.Enabled = true;
        }
    }
}
