using UnityEngine;

namespace GridInfect.Game
{
    // UI chrome only. Board colours are not here: every colour the board draws
    // comes from BoardPalette, so there is exactly one place to restyle a board
    // (docs/infection-vfx-spec.md, acceptance criterion 8).
    public static class BoardTheme
    {
        // The screen behind the board has to be the board's own background, or
        // the quad reads as a panel sitting on a different surface.
        public static Color Background => BoardPalette.Default.Background;

        public static readonly Color PanelDim = new Color(0f, 0f, 0f, 0.65f);

        public static readonly Color GlyphDark = new Color(0.043f, 0.063f, 0.125f);
        public static readonly Color GlyphLight = new Color(0.92f, 0.92f, 0.95f);

        public static readonly Color PieceBody = new Color(0.17f, 0.62f, 0.36f);    // green
        public static readonly Color PieceArm = new Color(0.34f, 0.82f, 0.52f);

        public static readonly Color ButtonBg = new Color(0.11f, 0.16f, 0.28f);
        public static readonly Color ButtonBgDisabled = new Color(0.08f, 0.11f, 0.19f);
        public static readonly Color Text = new Color(0.92f, 0.92f, 0.95f);
        public static readonly Color TextDim = new Color(0.42f, 0.50f, 0.64f);
        public static readonly Color Accent = new Color(0f, 0.85f, 1f);
    }
}
