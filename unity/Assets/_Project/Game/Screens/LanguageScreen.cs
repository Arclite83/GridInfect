using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;
using S = GridInfect.Game.PresentationConfig.Style;

namespace GridInfect.Game
{
    // Language: a grid of glass chips off SETTINGS, one per shipped tag plus
    // AUTO for following the device (docs/I18N.md 3). A chip carries its tag
    // in the mono face — a part number in the silkscreen idiom the HUD
    // already speaks — and the one in force is the lit chip. A non-Latin
    // language will additionally carry a drawn mark of its own script beside
    // the tag, for a reader who cannot read the chip; that lands with the
    // first such language (SelectorGlyphs), not before it.
    //
    // The pseudolocales (docs/I18N.md 9) show in the editor and in a
    // development build only. They are how the layout is tested, not
    // something a player picks.
    public sealed class LanguageScreen : AppScreen
    {
        const int Columns = 3;

        protected override void Build()
        {
            var title = Ui.MakeText("title", Root.transform, Str.LanguageTitle, L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            Buttons.Add(UiButton.Make(Root.transform, Str.NavSettings, L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new SettingsScreen())));

            bool dev = Debug.isDebugBuild || Application.isEditor;
            string stored = App.State.Profile.Lang ?? "";

            float w = (L.ContentWidth - L.Gap * (Columns - 1)) / Columns;
            var size = new Vector2(w, L.BarHeight);
            float pitch = L.BarHeight + L.Gap;
            float top = L.TopBarY - L.BarHeight - L.Gap * 2f;

            int n = 0;
            Chip("", Str.LanguageAuto, stored.Length == 0, size, Slot(n++, w, pitch, top));
            for (int i = 0; i < Str.Tags.Length; i++)
            {
                if (Str.IsPseudo[i] && !dev) continue;
                string tag = Str.Tags[i];
                Chip(tag, tag.ToUpperInvariant(), stored == tag, size, Slot(n++, w, pitch, top));
            }
        }

        static Vector2 Slot(int n, float w, float pitch, float top)
        {
            int col = n % Columns, row = n / Columns;
            return new Vector2(L.ColumnX(col, Columns, w + L.Gap), top - row * pitch);
        }

        // The tag is ASCII by construction (SetLanguageAction.IsWellFormed),
        // so uppercasing it for the chip is not a casing decision the string
        // table has to make.
        void Chip(string tag, string label, bool lit, Vector2 size, Vector2 centre)
        {
            var background = lit ? BoardTheme.Primary : BoardTheme.ButtonBg;
            var ink = lit ? BoardTheme.TextOnAccent : BoardTheme.Text;
            var chip = UiButton.Make(Root.transform, label, centre, size,
                BoardTheme.Chip(background), ink, () => Choose(tag), 20,
                pads: true, padAlpha: background.a, mono: true);
            chip.Root.name = "lang:" + (tag.Length == 0 ? "auto" : tag);
            Buttons.Add(chip);
        }

        void Choose(string tag)
        {
            if ((App.State.Profile.Lang ?? "") == tag) return;
            if (!App.Do(GridInfectActions.SettingsLanguage, Inputs.Language(tag)).Applied) return;
            App.ApplyLanguage();
            // Every word on screen was baked at construction, so the screen
            // is built again rather than relabelled — the same repaint the
            // skin swatches do, and no fade for the same reason.
            App.Screens.Show(new LanguageScreen(), instant: true);
        }
    }
}
