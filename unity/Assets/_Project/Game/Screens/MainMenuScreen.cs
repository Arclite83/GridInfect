using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;

namespace GridInfect.Game
{
    // Portrait: a title block in the top third, the two ways in stacked at the
    // middle where a thumb sits, and the sound toggle pinned low.
    public sealed class MainMenuScreen : AppScreen
    {
        UiButton _soundButton;
        TextMesh _soundLabel;
        UiButton _devButton;
        TextMesh _devLabel;

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
                BoardTheme.Accent, BoardTheme.GlyphDark, () => App.Screens.Show(new WorldSelectScreen())));
            Buttons.Add(UiButton.Make(Root.transform, "DAILY",
                new Vector2(0f, L.StackRowY(1, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new DailyScreen())));
            Buttons.Add(UiButton.Make(Root.transform, "ENDLESS",
                new Vector2(0f, L.StackRowY(2, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new EndlessScreen())));
            Buttons.Add(UiButton.Make(Root.transform, "LEGACY",
                new Vector2(0f, L.StackRowY(3, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new ClassicSelectScreen())));

            _soundButton = UiButton.Make(Root.transform, "", new Vector2(0f, -h * 0.38f),
                new Vector2(L.ContentWidth, L.BarHeight),
                BoardTheme.ButtonBgDisabled, BoardTheme.TextDim, ToggleSound);
            _soundLabel = _soundButton.Root.GetComponentInChildren<TextMesh>();
            Buttons.Add(_soundButton);
            RefreshSoundLabel();

            // R-802: the privacy options entry, whenever the consent SDK says
            // one is required; R-702: restore purchases beside it.
            if (App.Ads.PrivacyOptionsAvailable)
            {
                Buttons.Add(UiButton.Make(Root.transform, "PRIVACY OPTIONS", new Vector2(0f, -h * 0.44f),
                    new Vector2(L.ContentWidth, L.BarHeight),
                    BoardTheme.ButtonBgDisabled, BoardTheme.TextDim, () => App.Ads.ShowPrivacyOptions(null)));
            }

            // Testing affordance: every level open, one press. Debug builds and
            // the editor only — isDebugBuild is false in a release player, so
            // this row does not exist for a player.
            if (Debug.isDebugBuild)
            {
                // One bar above SOUND, measured rather than guessed, so the
                // two do not collide on a squarer aspect than a phone's.
                _devButton = UiButton.Make(Root.transform, "",
                    new Vector2(0f, -h * 0.38f + L.BarHeight + L.Gap),
                    new Vector2(L.ContentWidth, L.BarHeight),
                    BoardTheme.ButtonBgDisabled, BoardTheme.TextDim, UnlockEverything);
                _devLabel = _devButton.Root.GetComponentInChildren<TextMesh>();
                Buttons.Add(_devButton);
                RefreshDevLabel();
            }
        }

        void UnlockEverything()
        {
            App.Do(GridInfectActions.ProgressUnlockAll);
            RefreshDevLabel();
        }

        void RefreshDevLabel()
        {
            if (_devLabel == null) return;
            bool all = Queries.EverythingUnlocked(App.State.Profile);
            _devLabel.text = all ? "DEV: ALL LEVELS UNLOCKED" : "DEV: UNLOCK ALL LEVELS";
            _devButton.Enabled = !all;
        }

        void ToggleSound()
        {
            App.Do(GridInfectActions.SettingsMute, Inputs.Muted(!App.State.Profile.Muted));
            RefreshSoundLabel();
        }

        void RefreshSoundLabel()
        {
            _soundLabel.text = App.State.Profile.Muted ? "SOUND: OFF" : "SOUND: ON";
        }
    }
}
