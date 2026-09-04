using UnityEngine;

namespace GridInfect.Game
{
    // Every colour the presentation draws. Nothing in a shader or a view
    // samples a literal colour: a skin is this asset and nothing else
    // (docs/infection-vfx-spec.md, acceptance criterion 8; STYLE-GUIDE §2).
    //
    // The guide's rule: mask colour plus infection hue is the skin layer,
    // everything else is constant. The token names below are the guide's own
    // (grid-infect-style/out/tokens.json) so a value can be checked against
    // it by eye. The defaults are the ship skin; Skins.Apply swaps in blue or
    // breadboard. CreateInstance gives the defaults, so the game still boots
    // on a fresh clone with no asset in Resources.
    [CreateAssetMenu(menuName = "Grid Infect/Board Palette", fileName = "BoardPalette")]
    public sealed class BoardPalette : ScriptableObject
    {
        public enum SkinId { Default, Blue, Breadboard }

        // ---- skin: solder mask ----
        public Color Mask = Hex("#7FAE66");
        public Color MaskHi = Hex("#97C27C");
        public Color MaskLo = Hex("#5F8B4A");
        public Color Ink = Hex("#1D3316");          // silkscreen-adjacent type on the mask

        // ---- skin: copper. Points only (pads, vias, holes), never lines ----
        public Color Copper = Hex("#C9A648");
        public Color CopperHi = Hex("#F3E2A8");
        public Color CopperLo = Hex("#7D6120");

        // ---- skin: infection. The only strong emissive element on screen ----
        public Color Infect = Hex("#FF2D95");
        public Color InfectHi = Hex("#FF7CC4");
        public Color InfectLo = Hex("#B3086A");
        public Color InfectGlow = Alpha(Hex("#FF2D95"), 0.55f);
        public Color GlyphEdge = Hex("#5A0033");    // leads, body outline
        public Color GlyphWire = Hex("#3A0B22");    // bond wires, stubs, pads

        // ---- neutrals, constant across skins ----
        public Color Tip = Hex("#FFFFFF");          // lit lead tips, highlights, core dot
        public Color BlockerBody = Hex("#CFD8E0");
        public Color BlockerEdge = Hex("#4D565F");
        public Color WellBg = Alpha(Hex("#000000"), 0.36f);
        public Color Shade = Hex("#000000");        // shadows, insets, the well's ring

        // ---- states the guide does not draw ----
        // Grid Infect ships a repel switch, a reset trap and a forbidden cell
        // alongside empty / infected / blocker. They are tints on the guide's
        // component glass plus a shape glyph each (R-1001), never a literal.
        public Color RepelSwitch = Hex("#8A5CFF");  // violet tint, diamond glyph
        public Color ResetTrap = Hex("#0D0D12");    // near-black tint, X glyph
        public Color Conflict = Hex("#FF3B30");     // red overprint on a tripped ray

        // ---- emission / bloom ----
        // Colours are sRGB hex as authored; Unity converts on SetColor. An
        // infected tile rests slightly above 1 so the bloom gives it the
        // guide's 64 px halo; a freshly lit one is pushed to HotEmission and
        // cools back to rest over GlowHold + GlowFade.
        [Min(1f)] public float HotEmission = 2.2f;
        [Min(1f)] public float RestEmission = 1.15f;
        [Min(0f)] public float BloomThreshold = 1.0f;
        [Min(0f)] public float BloomIntensity = 0.9f;
        [Range(0f, 1f)] public float BloomScatter = 0.62f;

        // ---- board furniture (reference px, STYLE-GUIDE §4-§5) ----
        [Min(0f)] public float GlowPx = 26f;        // near glow around an infected tile
        [Min(0f)] public float TraceWidthPx = 2.5f; // the beam between cells during a wave
        [Range(0f, 1f)] public float BlotAmp = 0.18f;   // how far the blot noise ripples the pool front

        public static Color Hex(string hex)
        {
            int rgb = System.Convert.ToInt32(hex.Substring(1), 16);
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        public static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // The three skins of STYLE-GUIDE §2. Mask and infection move; copper
        // is gold on the two green/blue masks and bare copper on breadboard.
        public static class Skins
        {
            public static void Apply(BoardPalette p, SkinId skin)
            {
                switch (skin)
                {
                    case SkinId.Blue:
                        p.Mask = Hex("#2E5AA8"); p.MaskHi = Hex("#3F70C4"); p.MaskLo = Hex("#1F3F7A"); p.Ink = Hex("#E6EFFF");
                        p.Copper = Hex("#D9A441"); p.CopperHi = Hex("#FFE08A"); p.CopperLo = Hex("#7A5410");
                        p.Infect = Hex("#FF8A00"); p.InfectHi = Hex("#FFB347"); p.InfectLo = Hex("#C25A00");
                        p.InfectGlow = Alpha(Hex("#FF8A00"), 0.55f);
                        p.GlyphEdge = Hex("#4A2600"); p.GlyphWire = Hex("#3A1D00");
                        break;
                    case SkinId.Breadboard:
                        p.Mask = Hex("#E9DCB8"); p.MaskHi = Hex("#F4EAD0"); p.MaskLo = Hex("#CDBB8C"); p.Ink = Hex("#3C2E12");
                        p.Copper = Hex("#C46A3A"); p.CopperHi = Hex("#F0A878"); p.CopperLo = Hex("#7A3A18");
                        p.Infect = Hex("#FF2D3A"); p.InfectHi = Hex("#FF6B6B"); p.InfectLo = Hex("#B3101C");
                        p.InfectGlow = Alpha(Hex("#FF2D3A"), 0.5f);
                        p.GlyphEdge = Hex("#5A0008"); p.GlyphWire = Hex("#3A0008");
                        break;
                    default:
                        p.Mask = Hex("#7FAE66"); p.MaskHi = Hex("#97C27C"); p.MaskLo = Hex("#5F8B4A"); p.Ink = Hex("#1D3316");
                        p.Copper = Hex("#C9A648"); p.CopperHi = Hex("#F3E2A8"); p.CopperLo = Hex("#7D6120");
                        p.Infect = Hex("#FF2D95"); p.InfectHi = Hex("#FF7CC4"); p.InfectLo = Hex("#B3086A");
                        p.InfectGlow = Alpha(Hex("#FF2D95"), 0.55f);
                        p.GlyphEdge = Hex("#5A0033"); p.GlyphWire = Hex("#3A0B22");
                        break;
                }
            }
        }

        // A cheap fingerprint of the colours that reach a rasterised glyph, so
        // the glyph cache can tell one skin from another.
        public int GlyphKey =>
            Quantise(Infect) * 31 + Quantise(GlyphEdge) * 17 + Quantise(GlyphWire) * 7 + Quantise(BlockerBody);

        static int Quantise(Color c) => ((int)(c.r * 255) << 16) | ((int)(c.g * 255) << 8) | (int)(c.b * 255);

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
