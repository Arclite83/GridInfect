using UnityEngine;
using S = GridInfect.Game.PresentationConfig.Style;

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

        // Glass fills. Buttons are chips (§7): white 42% to 14%. There is no
        // dimmed chip any more — nothing is locked, and the two places that
        // used one for "secondary" only made it hard to read.
        public static Color ButtonBg => P.Tip;

        // A select tile. Dormant glass unsolved, infected glass solved:
        // red is beaten, everywhere it appears — the calendar's days, the
        // Legacy rack, a world's rack. Lives here rather than on a screen so
        // the three cannot drift apart.
        public static GlassStyle TileOpen()
        {
            var p = P;
            return new GlassStyle
            {
                FillTop = White(0.62f), FillMid = White(0.28f), MidStop = 0.55f, FillBottom = White(0.4f),
                Radius = S.TileRadius, Border = White(0.35f), BorderPx = 1f, TopLight = White(0.85f),
                Shadow = Black(0.38f), ShadowOffset = new UnityEngine.Vector2(0f, -7f), ShadowBlur = 16f,
            };
        }

        public static GlassStyle TileSolved()
        {
            var p = P;
            return new GlassStyle
            {
                FillTop = BoardPalette.Alpha(p.InfectHi, 0.95f), FillMid = p.Infect, MidStop = 0.55f, FillBottom = p.InfectLo,
                Radius = S.TileRadius, Border = White(0.4f), BorderPx = 1f, TopLight = White(0.85f),
                Glow = BoardPalette.Alpha(p.Infect, 0.5f), GlowPx = 14f,
                Shadow = Black(0.38f), ShadowOffset = new UnityEngine.Vector2(0f, -7f), ShadowBlur = 16f,
            };
        }

        // ---- cell swatches, for the legend on the rules sheet ----
        //
        // The board draws its cells in GridInfectBoard.shader; these are the
        // same stops, rings and top lights at the same alphas, so a swatch on
        // the rules page and a cell on the board are the same material. If
        // one moves, move the other.

        // A dormant component: the shader's GlassComponent at tint 0.
        public static GlassStyle CellEmpty() => new GlassStyle
        {
            FillTop = White(0.34f), FillMid = White(0.08f), MidStop = 0.55f, FillBottom = White(0.16f),
            Radius = S.TileRadius, Border = White(0.25f), BorderPx = 1f, TopLight = White(0.6f),
        };

        // The same component tinted: the repel chip and the trap chip.
        public static GlassStyle CellChip(Color tint, float amount)
        {
            var g = CellEmpty();
            g.FillTop = Tinted(g.FillTop, tint, amount);
            g.FillMid = Tinted(g.FillMid, tint, amount);
            g.FillBottom = Tinted(g.FillBottom, tint, amount);
            return g;
        }

        // Infected: the light inside the glass (GlassInfected).
        public static GlassStyle CellInfected()
        {
            var p = P;
            return new GlassStyle
            {
                FillTop = BoardPalette.Alpha(Color.Lerp(p.InfectHi, p.Tip, 0.45f), 0.9f),
                FillMid = p.Infect, MidStop = 0.55f, FillBottom = p.InfectLo,
                Radius = S.TileRadius, Border = White(0.4f), BorderPx = 1f, TopLight = White(0.85f),
                Glow = BoardPalette.Alpha(p.Infect, 0.45f), GlowPx = 10f,
            };
        }

        // A wall (GlassBlocker): brighter glass, a 2 px ring, a full top light.
        public static GlassStyle CellWall() => new GlassStyle
        {
            FillTop = White(0.6f), FillBottom = White(0.2f), Radius = S.TileRadius,
            Border = White(0.75f), BorderPx = 2f, TopLight = White(1f),
        };

        // A gap: not a cell at all, just the well showing through.
        public static GlassStyle CellGap() => new GlassStyle
        {
            FillTop = Black(0.05f), FillBottom = Black(0.05f), Radius = S.TileRadius,
            Border = Black(0.1f), BorderPx = 1f,
        };

        // The shader's `lerp(fill.rgb, tint, amount)` with alpha to 0.75.
        static Color Tinted(Color fill, Color tint, float amount)
        {
            var c = Color.Lerp(fill, tint, amount);
            c.a = Mathf.Lerp(fill.a, 0.75f, amount);
            return c;
        }

        // The track and the fill of an infection meter: how much of a world
        // has gone red. The track is the well's own recess so the meter reads
        // as cut into the row rather than laid on it.
        public static GlassStyle MeterTrack() => new GlassStyle
        {
            FillTop = Black(0.3f), FillBottom = Black(0.3f), Radius = 3f,
            Border = White(0.12f), BorderPx = 1f,
        };

        public static GlassStyle MeterFill()
        {
            var p = P;
            return new GlassStyle
            {
                FillTop = p.InfectHi, FillBottom = p.Infect, Radius = 3f,
                Glow = BoardPalette.Alpha(p.Infect, 0.55f), GlowPx = 8f,
            };
        }

        static UnityEngine.Color White(float a) => BoardPalette.Alpha(P.Tip, a);
        static UnityEngine.Color Black(float a) => BoardPalette.Alpha(P.Shade, a);

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
