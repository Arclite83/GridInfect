using UnityEngine;

namespace GridInfect.Game
{
    // UI chrome, all derived from the palette so a skin swap restyles the
    // menus with the board (STYLE-GUIDE §2: mask plus infection is the skin,
    // everything else is constant). Nothing here is a literal hue: the
    // constants are alphas and tints of white and black from §5 and §7.
    public static class BoardTheme
    {
        static BoardPalette P => BoardPalette.Default;

        // The camera clear: the substrate quad covers it, but a frame with
        // nothing drawn should still be the mask, not a foreign colour.
        public static Color Background => P.MaskLo;

        public static Color Text => P.Ink;
        public static Color TextDim => BoardPalette.Alpha(P.Ink, 0.7f);
        public static Color TextOnAccent => P.Tip;
        public static Color Accent => P.Ink;             // stats and readouts: ink, like the level label
        public static Color Copper => P.CopperHi;        // the lock counter's mono type on its black badge
        public static Color Primary => P.Infect;         // the one lit control on a screen

        // Kept for the lock marks and the popup dim.
        public static Color GlyphDark => P.GlyphEdge;
        public static Color GlyphLight => P.Tip;
        public static readonly Color PanelDim = new Color(0f, 0f, 0f, 0.55f);

        // Glass fills. Buttons are chips (§7): white 42% to 14%; a disabled
        // chip is the same glass at a third of the light.
        public static Color ButtonBg => P.Tip;
        public static Color ButtonBgDisabled => BoardPalette.Alpha(P.Tip, 0.35f);

        public static GlassStyle Chip(Color tint)
        {
            var g = GlassStyle.Chip(P);
            if (tint.a < 1f)
            {
                g.FillTop.a *= tint.a;
                g.FillBottom.a *= tint.a;
                g.Border.a *= tint.a;
                g.TopLight.a *= tint.a;
            }
            else if (tint.r != P.Tip.r || tint.g != P.Tip.g || tint.b != P.Tip.b)
            {
                // A coloured chip: the infection (or any accent) lit from
                // inside, the way an infected tile is.
                g.FillTop = BoardPalette.Alpha(Color.Lerp(tint, P.Tip, 0.35f), 0.95f);
                g.FillBottom = BoardPalette.Alpha(tint, 0.9f);
                g.Glow = BoardPalette.Alpha(tint, 0.45f);
                g.GlowPx = 14f;
            }
            return g;
        }
    }
}
