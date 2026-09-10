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

        // Sound, skins, language: the stack the privacy row joins when the
        // consent SDK asks for one. The count sizes the stack's centre, so
        // the optional row is not in it and hangs below.
        const int Rows = 3;

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;

            var title = Ui.MakeText("title", Root.transform, Str.SettingsTitle, L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            Buttons.Add(UiButton.Make(Root.transform, Str.NavMenu, L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));

            var size = new Vector2(L.ContentWidth, L.ButtonHeight);
            int row = 0;
            _sound = UiButton.Make(Root.transform, "", new Vector2(0f, L.StackRowY(row++, Rows, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, ToggleSound);
            Buttons.Add(_sound);
            RefreshSound();

            BuildSkins(L.StackRowY(row++, Rows, L.ButtonHeight, 0f));

            Buttons.Add(UiButton.Make(Root.transform, Str.SettingsLanguage,
                new Vector2(0f, L.StackRowY(row++, Rows, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new LanguageScreen())));

            // R-802: the privacy options entry, whenever the consent SDK says
            // one is required.
            if (App.Ads.PrivacyOptionsAvailable)
            {
                Buttons.Add(UiButton.Make(Root.transform, Str.SettingsPrivacy,
                    new Vector2(0f, L.StackRowY(row++, Rows, L.ButtonHeight, 0f)), size,
                    BoardTheme.ButtonBg, BoardTheme.Text, () => App.Ads.ShowPrivacyOptions(null)));
            }

            // Two lines, and in ink: it was one dimmed line long enough to
            // run off both edges of the screen, which is a warning nobody
            // reads. TextMesh does not wrap, so the break is explicit.
            var caption = Ui.MakeText("caption", Root.transform,
                Str.SettingsEraseCaption,
                L.BodyText * 0.85f, BoardTheme.Text, 2);
            Ui.SetPos(caption.gameObject, 0f, -h * 0.36f + L.BarHeight * 1.6f);
            _erase = UiButton.Make(Root.transform, "", new Vector2(0f, -h * 0.36f),
                new Vector2(L.ContentWidth, L.BarHeight),
                BoardTheme.ButtonBg, BoardTheme.Text, Erase);
            Buttons.Add(_erase);
            RefreshErase();

            BuildStamp(-h * 0.36f - L.BarHeight / 2f - L.Gap * 1.6f);
        }

        // Which build is this. The one question a screenshot from the wild
        // has to answer, and the reason the substrate's bottom-right corner
        // used to say `GI-REV B` on every screen — a revision letter from
        // the style mockups that named nothing. The answer belongs here
        // rather than on the home screen: it is settings data, like the
        // sound switch and the erase button, not something the game wears.
        //
        // buildGUID is the player's own build id and is empty in the editor,
        // where the question has a different answer.
        void BuildStamp(float y)
        {
            string guid = Application.buildGUID;
            string build = string.IsNullOrEmpty(guid) ? Str.SettingsBuildEditor : guid.Substring(0, 8).ToUpperInvariant();
            var stamp = Ui.MakeText("build", Root.transform, Str.Fmt(Str.SettingsBuild, Application.version, build),
                S.Px(S.SmallText), BoardTheme.TextDim, 2, mono: true);
            Ui.SetPos(stamp.gameObject, 0f, y);
        }

        // The three board skins, each chip wearing its own solder mask with a
        // bead of its own infection under the name — the two colours that
        // actually move between skins (STYLE-GUIDE §2). The live one takes a
        // lit plate behind it, the same mark the calendar puts on the day it
        // is pointing at.
        //
        // Two are earned: blue for every world beaten, breadboard for all 128
        // Legacy levels. They are the only thing in the game still behind
        // progress, and a locked one wears the padlock with what it wants
        // written under it — a reward nobody can see is not a reward. The
        // gate is here and not in the action: unlock gating is presentation
        // policy throughout (ARCHITECTURE §3), so a test or a tool can still
        // set any skin.
        // Ids only. The words come from Str at build time, not from a static
        // initialiser: a static readonly string would freeze at type init and
        // survive a language change that rebuilds every screen around it.
        static readonly BoardPalette.SkinId[] Palettes =
        {
            BoardPalette.SkinId.Default,
            BoardPalette.SkinId.Blue,
            BoardPalette.SkinId.Breadboard,
        };

        static string SkinName(BoardPalette.SkinId id) =>
            id == BoardPalette.SkinId.Blue ? Str.SettingsSkinBlue
            : id == BoardPalette.SkinId.Breadboard ? Str.SettingsSkinTan
            : Str.SettingsSkinGreen;

        // What solving opens the skin, or null for the one that starts open.
        static string SkinWant(BoardPalette.SkinId id) =>
            id == BoardPalette.SkinId.Blue ? Str.SettingsSkinWantWorlds
            : id == BoardPalette.SkinId.Breadboard ? Str.SettingsSkinWantLegacy
            : null;

        void BuildSkins(float y)
        {
            float w = (L.ContentWidth - L.Gap * 2f) / 3f;
            // The chips give up some height and the row rides up, because
            // what hangs under a locked one is two lines and it has to clear
            // both the chip above it and the row below.
            float height = L.ButtonHeight * 0.72f;
            float noteText = L.BodyText * 0.62f;
            var box = new Vector2(w, height);
            y += L.ButtonHeight * 0.26f;

            for (int k = 0; k < Palettes.Length; k++)
            {
                var id = Palettes[k];
                string name = SkinName(id), want = SkinWant(id);
                BoardPalette skin = BoardPalette.Preview(id);
                bool earned = App.SkinEarned(id);
                float x = L.ColumnX(k, Palettes.Length, w + L.Gap);

                if (BoardPalette.Skin == id)
                {
                    var plate = Ui.MakeGlass("live", Root.transform,
                        new Vector2(w + S.Px(5f), height + S.Px(5f)), LiveStyle(), 8);
                    Ui.SetPos(plate, x, y);
                }

                var captured = id;
                var chip = UiButton.Make(Root.transform, earned ? name : "", new Vector2(x, y), box,
                    SwatchStyle(skin, earned), skin.Ink, () => Choose(captured), 20,
                    pads: false, padAlpha: 1f, mono: false);
                chip.Enabled = earned;
                Buttons.Add(chip);

                if (earned)
                {
                    chip.Label.transform.localPosition = new Vector3(0f, height * 0.16f, 0f);
                    var bead = Ui.MakeGlass("infect", chip.Root.transform,
                        new Vector2(S.Px(9f), S.Px(9f)), BeadStyle(skin), 22);
                    Ui.SetPos(bead, 0f, -height * 0.26f);
                }
                else
                {
                    Ui.MakeSprite("locked", chip.Root.transform,
                        BugGlyph.Lock(BoardPalette.Default, Mathf.RoundToInt(height * 0.62f)), 22);
                    // TextMesh centres the whole block, so a two-line note is
                    // hung by its middle: half of it (1.2 lines) below the
                    // chip's bottom edge, not its first line.
                    var note = Ui.MakeText($"want:{id}", Root.transform, want, noteText,
                        BoardTheme.Text, 12);
                    Ui.SetPos(note.gameObject, x, y - height / 2f - S.Px(4f) - noteText * 1.2f);
                }
            }
        }

        void Choose(BoardPalette.SkinId skin)
        {
            if (BoardPalette.Skin == skin || !App.SkinEarned(skin)) return;
            if (!App.Do(GridInfectActions.SettingsSkin, Inputs.Skin((int)skin)).Applied) return;
            App.ApplySkin();
            // Every piece of glass on screen baked its colours when it was
            // made, so the screen is built again rather than repainted. No
            // fade: this is a repaint, not a navigation.
            App.Screens.Show(new SettingsScreen(), instant: true);
        }

        // A locked skin still shows its colours — muted, so the padlock over
        // them reads as the point rather than as damage.
        static GlassStyle SwatchStyle(BoardPalette skin, bool earned)
        {
            float a = earned ? 1f : 0.45f;
            return new GlassStyle
            {
                FillTop = BoardPalette.Alpha(skin.MaskHi, a), FillMid = BoardPalette.Alpha(skin.Mask, a),
                MidStop = 0.5f, FillBottom = BoardPalette.Alpha(skin.MaskLo, a),
                Radius = S.ChipRadius, Border = BoardPalette.Alpha(skin.Ink, earned ? 0.3f : 0.15f), BorderPx = 1f,
                TopLight = BoardPalette.Alpha(skin.Tip, earned ? 0.45f : 0.2f),
            };
        }

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
            _sound.Label.text = App.State.Profile.Muted ? Str.SettingsSoundOff : Str.SettingsSoundOn;
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
            // The earned skins are gone with the progress that bought them,
            // so the row has to be drawn again — and the board with it, if
            // the player was wearing one.
            App.ApplySkin();
            App.Screens.Show(new SettingsScreen(), instant: true);
        }

        void RefreshErase()
        {
            // The chip is plain glass either way — legible is the point —
            // and arming turns the type to the infection, which is the one
            // colour on this screen that means anything.
            _erase.Label.text = _armed ? Str.SettingsResetArmed : Str.SettingsReset;
            _erase.Label.color = _armed ? BoardTheme.Primary : BoardTheme.Text;
        }
    }
}
