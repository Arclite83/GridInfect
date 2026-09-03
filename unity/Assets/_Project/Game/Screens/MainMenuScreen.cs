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

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;

            var title = Ui.MakeText("title", Root.transform, "GRID INFECT", L.TitleText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, h * 0.28f);
            var subtitle = Ui.MakeText("subtitle", Root.transform, "infect every cell",
                L.BodyText, BoardTheme.TextDim, 2);
            Ui.SetPos(subtitle.gameObject, 0f, h * 0.28f - L.TitleText * 0.9f - L.BodyText);

            // PLAY is the worlds (stage 3); the 128 classic levels live on as
            // LEGACY: unchanged rules, no hints.
            var size = new Vector2(L.ContentWidth, L.ButtonHeight);
            Buttons.Add(UiButton.Make(Root.transform, "PLAY",
                new Vector2(0f, L.StackRowY(0, 3, L.ButtonHeight, 0f)), size,
                BoardTheme.Accent, BoardTheme.GlyphDark, () => App.Screens.Show(new WorldSelectScreen())));
            Buttons.Add(UiButton.Make(Root.transform, "FREE PLAY",
                new Vector2(0f, L.StackRowY(1, 3, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new FreePlayMenuScreen())));
            Buttons.Add(UiButton.Make(Root.transform, "LEGACY",
                new Vector2(0f, L.StackRowY(2, 3, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new ClassicSelectScreen())));

            _soundButton = UiButton.Make(Root.transform, "", new Vector2(0f, -h * 0.38f),
                new Vector2(L.ContentWidth, L.BarHeight),
                BoardTheme.ButtonBgDisabled, BoardTheme.TextDim, ToggleSound);
            _soundLabel = _soundButton.Root.GetComponentInChildren<TextMesh>();
            Buttons.Add(_soundButton);
            RefreshSoundLabel();
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
