using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;
using S = GridInfect.Game.PresentationConfig.Style;

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

            BuildSkins(L.StackRowY(row++, 3, L.ButtonHeight, 0f));

            // R-802: the privacy options entry, whenever the consent SDK says
            // one is required.
            if (App.Ads.PrivacyOptionsAvailable)
            {
                Buttons.Add(UiButton.Make(Root.transform, "PRIVACY OPTIONS",
                    new Vector2(0f, L.StackRowY(row++, 3, L.ButtonHeight, 0f)), size,
                    BoardTheme.ButtonBg, BoardTheme.Text, () => App.Ads.ShowPrivacyOptions(null)));
            }

            // Two lines, and in ink: it was one dimmed line long enough to
            // run off both edges of the screen, which is a warning nobody
            // reads. TextMesh does not wrap, so the break is explicit.
            var caption = Ui.MakeText("caption", Root.transform,
                "ERASES EVERY LEVEL BEATEN\nAND EVERY SCORE. LOCKS ARE KEPT.",
                L.BodyText * 0.85f, BoardTheme.Text, 2);
            Ui.SetPos(caption.gameObject, 0f, -h * 0.36f + L.BarHeight * 1.6f);
            _erase = UiButton.Make(Root.transform, "", new Vector2(0f, -h * 0.36f),
                new Vector2(L.ContentWidth, L.BarHeight),
                BoardTheme.ButtonBg, BoardTheme.Text, Erase);
            Buttons.Add(_erase);
            RefreshErase();
        }

        // The three board skins, each chip wearing its own solder mask with a
        // bead of its own infection under the name — the two colours that
        // actually move between skins (STYLE-GUIDE §2). The live one takes a
        // lit plate behind it, the same mark the calendar puts on the day it
        // is pointing at.
        static readonly (BoardPalette.SkinId id, string name)[] Palettes =
        {
            (BoardPalette.SkinId.Default, "GREEN"),
            (BoardPalette.SkinId.Blue, "BLUE"),
            (BoardPalette.SkinId.Breadboard, "TAN"),
        };

        void BuildSkins(float y)
        {
            float w = (L.ContentWidth - L.Gap * 2f) / 3f;
            var box = new Vector2(w, L.ButtonHeight);
            for (int k = 0; k < Palettes.Length; k++)
            {
                var (id, name) = Palettes[k];
                BoardPalette skin = BoardPalette.Preview(id);
                float x = (k - 1) * (w + L.Gap);

                if (BoardPalette.Skin == id)
                {
                    var plate = Ui.MakeGlass("live", Root.transform,
                        new Vector2(w + S.Px(5f), L.ButtonHeight + S.Px(5f)), LiveStyle(), 8);
                    Ui.SetPos(plate, x, y);
                }

                var captured = id;
                var chip = UiButton.Make(Root.transform, name, new Vector2(x, y), box,
                    SwatchStyle(skin), skin.Ink, () => Choose(captured), 20,
                    pads: false, padAlpha: 1f, mono: false);
                chip.Label.transform.localPosition = new Vector3(0f, L.ButtonHeight * 0.12f, 0f);
                Buttons.Add(chip);

                var bead = Ui.MakeGlass("infect", chip.Root.transform,
                    new Vector2(S.Px(9f), S.Px(9f)), BeadStyle(skin), 22);
                Ui.SetPos(bead, 0f, -L.ButtonHeight * 0.22f);
            }
        }

        void Choose(BoardPalette.SkinId skin)
        {
            if (BoardPalette.Skin == skin) return;
            if (!App.Do(GridInfectActions.SettingsSkin, Inputs.Skin((int)skin)).Applied) return;
            App.ApplySkin();
            // Every piece of glass on screen baked its colours when it was
            // made, so the screen is built again rather than repainted. No
            // fade: this is a repaint, not a navigation.
            App.Screens.Show(new SettingsScreen(), instant: true);
        }

        static GlassStyle SwatchStyle(BoardPalette skin) => new GlassStyle
        {
            FillTop = skin.MaskHi, FillMid = skin.Mask, MidStop = 0.5f, FillBottom = skin.MaskLo,
            Radius = S.ChipRadius, Border = BoardPalette.Alpha(skin.Ink, 0.3f), BorderPx = 1f,
            TopLight = BoardPalette.Alpha(skin.Tip, 0.45f),
        };

        static GlassStyle BeadStyle(BoardPalette skin) => new GlassStyle
        {
            FillTop = skin.InfectHi, FillBottom = skin.Infect, Radius = S.Px(5f),
            Glow = BoardPalette.Alpha(skin.Infect, 0.6f), GlowPx = 7f,
        };

        static GlassStyle LiveStyle() => new GlassStyle
        {
            FillTop = BoardTheme.GlyphLight, FillBottom = BoardTheme.GlyphLight, Radius = S.ChipRadius + 2f,
            Glow = BoardPalette.Alpha(BoardPalette.Default.Infect, 0.5f), GlowPx = 12f,
        };

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
            // The chip is plain glass either way — legible is the point —
            // and arming turns the type to the infection, which is the one
            // colour on this screen that means anything.
            _erase.Label.text = _armed ? "TAP AGAIN TO ERASE" : "RESET PROGRESS";
            _erase.Label.color = _armed ? BoardTheme.Primary : BoardTheme.Text;
        }
    }
}
