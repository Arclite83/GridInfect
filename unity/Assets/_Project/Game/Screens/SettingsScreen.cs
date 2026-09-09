using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;

namespace GridInfect.Game
{
    // Settings: the preferences that used to hang off the bottom of the main
    // menu, plus the one destructive control in the game, kept at the back
    // where nothing else is.
    //
    // Erasing progress arms on the first tap and fires on the second. There
    // is no confirm popup because there is no need for one: the button says
    // what it is about to do and a stray tap only ever arms it.
    public sealed class SettingsScreen : AppScreen
    {
        UiButton _sound;
        UiButton _erase;
        bool _armed;

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;

            var title = Ui.MakeText("title", Root.transform, "SETTINGS", L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            Buttons.Add(UiButton.Make(Root.transform, "MENU", L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));

            var size = new Vector2(L.ContentWidth, L.ButtonHeight);
            int row = 0;
            _sound = UiButton.Make(Root.transform, "", new Vector2(0f, L.StackRowY(row++, 3, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, ToggleSound);
            Buttons.Add(_sound);
            RefreshSound();

            // R-802: the privacy options entry, whenever the consent SDK says
            // one is required.
            if (App.Ads.PrivacyOptionsAvailable)
            {
                Buttons.Add(UiButton.Make(Root.transform, "PRIVACY OPTIONS",
                    new Vector2(0f, L.StackRowY(row++, 3, L.ButtonHeight, 0f)), size,
                    BoardTheme.ButtonBg, BoardTheme.Text, () => App.Ads.ShowPrivacyOptions(null)));
            }

            var caption = Ui.MakeText("caption", Root.transform,
                "ERASES EVERY LEVEL BEATEN AND EVERY SCORE. LOCKS ARE KEPT.",
                L.BodyText * 0.8f, BoardTheme.TextDim, 2);
            Ui.SetPos(caption.gameObject, 0f, -h * 0.36f + L.ButtonHeight);
            _erase = UiButton.Make(Root.transform, "", new Vector2(0f, -h * 0.36f),
                new Vector2(L.ContentWidth, L.BarHeight),
                BoardTheme.ButtonBgDisabled, BoardTheme.TextDim, Erase);
            Buttons.Add(_erase);
            RefreshErase();
        }

        void ToggleSound()
        {
            App.Do(GridInfectActions.SettingsMute, Inputs.Muted(!App.State.Profile.Muted));
            RefreshSound();
        }

        void RefreshSound()
        {
            _sound.Label.text = App.State.Profile.Muted ? "SOUND: OFF" : "SOUND: ON";
        }

        void Erase()
        {
            if (!_armed)
            {
                _armed = true;
                RefreshErase();
                return;
            }
            App.Do(GridInfectActions.ProgressReset);
            _armed = false;
            RefreshErase();
        }

        void RefreshErase()
        {
            _erase.Label.text = _armed ? "TAP AGAIN TO ERASE" : "RESET PROGRESS";
            _erase.Label.color = _armed ? BoardTheme.Primary : BoardTheme.TextDim;
        }
    }
}
