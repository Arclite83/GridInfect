using GridInfect.Core;
using UnityEngine;

namespace GridInfect.Game
{
    public sealed class MainMenuScreen : AppScreen
    {
        UiButton _soundButton;
        TextMesh _soundLabel;

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;
            float w = UnityEngine.Screen.width;

            var title = Ui.MakeText("title", Root.transform, "GRID INFECT", h * 0.10f, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, h * 0.28f);
            var subtitle = Ui.MakeText("subtitle", Root.transform, "infect every cell", h * 0.035f, BoardTheme.TextDim, 2);
            Ui.SetPos(subtitle.gameObject, 0f, h * 0.19f);

            var size = new Vector2(w * 0.30f, h * 0.11f);
            Buttons.Add(UiButton.Make(Root.transform, "CLASSIC", new Vector2(0f, h * 0.02f), size,
                BoardTheme.Accent, BoardTheme.GlyphDark, () => App.Screens.Show(new ClassicSelectScreen())));
            Buttons.Add(UiButton.Make(Root.transform, "FREE PLAY", new Vector2(0f, -h * 0.13f), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new FreePlayMenuScreen())));

            _soundButton = UiButton.Make(Root.transform, "", new Vector2(0f, -h * 0.28f),
                new Vector2(w * 0.30f, h * 0.08f), BoardTheme.ButtonBgDisabled, BoardTheme.TextDim, ToggleSound);
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
