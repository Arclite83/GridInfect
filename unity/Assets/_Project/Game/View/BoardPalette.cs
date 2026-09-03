using UnityEngine;

namespace GridInfect.Game
{
    // Every colour the board shader can output. Nothing in the shader or the
    // view samples a literal colour: a new board type is this asset plus a
    // noise swap (docs/infection-vfx-spec.md, acceptance criterion 8).
    //
    // The defaults below are the spec's palette table. They are what
    // CreateInstance gives you, so the board still boots on a fresh clone with
    // no asset in Resources; dropping a BoardPalette there overrides them.
    [CreateAssetMenu(menuName = "Grid Infect/Board Palette", fileName = "BoardPalette")]
    public sealed class BoardPalette : ScriptableObject
    {
        // ---- spec palette ----
        public Color Background = Hex("#0B1020");     // near-black, never pure black
        public Color CellPlate = Hex("#141C33");      // every cell that exists
        public Color GridLine = Hex("#1B2A48");       // hairline, always visible
        public Color CellBorder = Hex("#2B3F63");     // 1 px, empty cells
        public Color Infected = Hex("#00D9FF");       // HDR emissive, cools on fade
        public Color Cooled = Hex("#0B7F99");         // fade target, non-emissive
        public Color BleedEdge = Hex("#E0FFFF");      // transition only
        public Color Ghost = Hex("#FF3DD8");          // transition only, 45% under the fill
        public Color Seed = Hex("#FF3DD8");           // emissive
        public Color ImmuneHatch = Hex("#55688A");    // 45 degree lines, 7 px pitch

        // ---- states the spec's four-state table does not name ----
        // Grid Infect ships a repel switch and a reset trap alongside empty /
        // infected / immune, so they need palette entries of their own rather
        // than a literal in the shader.
        public Color RepelSwitch = Hex("#8A5CFF");    // violet, diamond glyph
        public Color ResetTrap = Hex("#0D0D12");      // near-black, X glyph
        public Color Forbidden = Hex("#FFB300");      // amber, ring glyph (RulesV2 stage 10: must stay clean)
        public Color Conflict = Hex("#FF3B30");       // red overprint + the X glyph
        public Color Glyph = Hex("#0B1020");          // shape glyphs read as holes in the fill

        // ---- emission / bloom ----
        // Colours here are sRGB hex, as authored; BoardView converts them for
        // linear rendering on the way to the material. Hot output is pushed
        // above 1 so it lands in the HDR buffer, and the bloom threshold sits
        // at 1, which rejects every LDR colour on the board (cooled fill, cell
        // border, immune hatch) and all of the UI chrome.
        [Min(1f)] public float HotEmission = 2.2f;
        [Min(0f)] public float BloomThreshold = 1.0f;
        [Min(0f)] public float BloomIntensity = 0.9f;
        [Range(0f, 1f)] public float BloomScatter = 0.62f;

        // ---- board furniture ----
        [Min(0f)] public float GridLinePx = 1f;
        [Min(0f)] public float CellBorderPx = 1f;
        [Min(1f)] public float ImmuneHatchPitchPx = 7f;
        [Min(0f)] public float TraceWidthPx = 2.5f;

        static Color Hex(string hex)
        {
            int rgb = System.Convert.ToInt32(hex.Substring(1), 16);
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        static BoardPalette _default;

        // Resources first so an artist can restyle without touching code; the
        // in-code defaults keep the zero-asset boot working.
        public static BoardPalette Default
        {
            get
            {
                if (_default == null) _default = Resources.Load<BoardPalette>("BoardPalette");
                if (_default == null) _default = CreateInstance<BoardPalette>();
                return _default;
            }
        }
    }
}
