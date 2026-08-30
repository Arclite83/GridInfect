using UnityEngine;

namespace GridInfect.Game
{
    /// <summary>
    /// Baseline palette and cell glyphs. Placeholder look for the art
    /// overhaul, but the accessibility rule is structural from day one
    /// (R-1001): every special cell state carries a shape, never color alone —
    /// wall = inset block, switch = diamond, trap = X — and active vs infected
    /// differ strongly in luminance, not just hue.
    /// </summary>
    public static class BoardTheme
    {
        public static readonly Color Background = new Color(0.086f, 0.086f, 0.118f);
        public static readonly Color PanelDim = new Color(0f, 0f, 0f, 0.65f);

        public static readonly Color CellActive = new Color(0.45f, 0.71f, 0.96f);   // light blue
        public static readonly Color CellInfected = new Color(0.75f, 0.18f, 0.16f); // dark red
        public static readonly Color CellWall = new Color(0.90f, 0.76f, 0.16f);     // yellow
        public static readonly Color CellSwitch = new Color(0.58f, 0.35f, 0.75f);   // purple
        public static readonly Color CellTrap = new Color(0.05f, 0.05f, 0.05f);     // near black

        public static readonly Color GlyphDark = new Color(0.10f, 0.10f, 0.14f);
        public static readonly Color GlyphLight = new Color(0.92f, 0.92f, 0.95f);

        public static readonly Color PieceBody = new Color(0.17f, 0.62f, 0.36f);    // green
        public static readonly Color PieceArm = new Color(0.34f, 0.82f, 0.52f);

        public static readonly Color ButtonBg = new Color(0.20f, 0.22f, 0.30f);
        public static readonly Color ButtonBgDisabled = new Color(0.14f, 0.15f, 0.19f);
        public static readonly Color Text = new Color(0.92f, 0.92f, 0.95f);
        public static readonly Color TextDim = new Color(0.55f, 0.56f, 0.62f);
        public static readonly Color Accent = new Color(0.96f, 0.62f, 0.25f);
    }
}
