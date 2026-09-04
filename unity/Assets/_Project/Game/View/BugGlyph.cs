using System.Collections.Generic;
using GridInfect.Core;
using UnityEngine;

namespace GridInfect.Game
{
    // The bug glyph grammar (grid-infect-bug-glyph/BUG-GLYPH-SPEC.md,
    // STYLE-GUIDE §6), ported primitive for primitive from gen-assets.mjs.
    //
    // Hexagonal IC body, one lit squared lead per orthogonal spread direction,
    // a wire lead with a round pad tip per diagonal, two bond wires alongside
    // each squared lead, a branch stub with a pad per side, three short stubs
    // on every inactive body edge (outer two with pads), one stub at each hex
    // vertex between two inactive edges. Lit tips are the only long bright
    // elements; nothing else exceeds length 3.5.
    //
    // Sprites are rasterised at the pixel size a context asks for (44 px on a
    // 54 px tile, 58 px next, 40 px queued — all scaled to the device) and
    // cached by spec, size and skin.
    public static class BugGlyph
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        // Core Dir -> glyph angle. Row index grows downward, so U is north.
        static float Angle(Dir dir)
        {
            switch (dir)
            {
                case Dir.U: return 0f;
                case Dir.R: return 90f;
                case Dir.D: return 180f;
                case Dir.L: return 270f;
                case Dir.UR: return 45f;
                case Dir.DR: return 135f;
                case Dir.DL: return 225f;
                default: return 315f;   // UL
            }
        }

        static readonly Dir[] Orth = { Dir.U, Dir.R, Dir.D, Dir.L };

        // Hex vertices at 45/135/225/315 and the two edges each sits between,
        // plus the diagonal lead that would occupy it.
        static readonly (float angle, Dir a, Dir b, Dir diag)[] Verts =
        {
            (45f, Dir.U, Dir.R, Dir.UR), (135f, Dir.R, Dir.D, Dir.DR),
            (225f, Dir.D, Dir.L, Dir.DL), (315f, Dir.L, Dir.U, Dir.UL),
        };

        public static void ClearCache() => Cache.Clear();

        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (!Cache.TryGetValue(key, out var sprite) || sprite == null)
            {
                sprite = make();
                Cache[key] = sprite;
            }
            return sprite;
        }

        // ---- public sprites ----

        public static Sprite Piece(PieceSpec spec, BoardPalette p, int sizePx)
        {
            return Cached($"piece:{spec.Encode()}:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                DrawPiece(c, spec, p);
                return c.ToSprite($"bug_{spec.Encode()}_{sizePx}");
            });
        }

        public static Sprite Blocker(BoardPalette p, int sizePx)
        {
            return Cached($"blocker:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                DrawBlocker(c, p);
                return c.ToSprite($"tile_BLOCKER_{sizePx}");
            });
        }

        // The lock mark (R-1001: a shape, never colour alone), drawn over a
        // locked piece's core.
        public static Sprite Lock(BoardPalette p, int sizePx)
        {
            return Cached($"lock:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                c.Quad(16.5f, 21f, 16.5f, 14.5f, 20f, 14.5f, 1.6f, p.GlyphEdge, false);
                c.Quad(20f, 14.5f, 23.5f, 14.5f, 23.5f, 21f, 1.6f, p.GlyphEdge, false);
                c.Rect(14.5f, 19.5f, 11f, 8f, 1.5f, p.GlyphEdge);
                c.Circle(20f, 23f, 1.3f, p.Tip);
                c.Rect(19.4f, 23f, 1.2f, 2.4f, 0f, p.Tip);
                return c.ToSprite($"mark_LOCK_{sizePx}");
            });
        }

        // Relay cells (RULES_V2 §12): a hub with one stub and pad per arm,
        // in the grammar's wire colour.
        public static Sprite Relay(byte arms, BoardPalette p, int sizePx)
        {
            return Cached($"relay:{arms}:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                for (int d = 0; d < 8; d++)
                {
                    if ((arms & (1 << d)) == 0) continue;
                    c.SetTransform(Angle((Dir)d));
                    c.Stroke(new[] { 20f, 20f, 20f, 10.5f }, 1.4f, p.GlyphWire);
                    c.Circle(20f, 10f, 1.6f, p.GlyphWire);
                }
                c.ClearTransform();
                c.Circle(20f, 20f, 3.2f, p.GlyphWire);
                c.Circle(20f, 20f, 1.2f, p.Tip);
                return c.ToSprite($"mark_RELAY_{arms}_{sizePx}");
            });
        }

        // ---- grammar ----

        static void DrawPiece(GlyphCanvas c, PieceSpec spec, BoardPalette p)
        {
            var active = new HashSet<Dir>();
            for (int d = 0; d < 8; d++) if (spec.Has((Dir)d)) active.Add((Dir)d);

            if (spec.Area) AreaArcs(c, p);

            for (int d = 0; d < 8; d++)
            {
                var dir = (Dir)d;
                if (!active.Contains(dir)) continue;
                float a = Angle(dir);
                if (TileArms.IsDiagonal(dir))
                {
                    Diag(c, a, p);
                }
                else
                {
                    Lead(c, a, p);
                    Bond(c, a, p);
                    Stubs(c, a, p);
                    int reach = spec.ReachOf(dir);
                    if (reach > 0) StopBars(c, a, reach, p);
                }
            }

            // The area bug has no leads and no body stubs: the arcs are its
            // whole outside. Arms on an area piece (RulesV2 allows both) keep
            // their leads, and the stubs return only for the edges they leave.
            if (!spec.Area || spec.Arms != 0) Body(c, active, p);
            Core(c, p.Infect, p.GlyphEdge, p);
            c.ClearTransform();
            c.Circle(20f, 20f, 3f, p.Tip);
        }

        static void DrawBlocker(GlyphCanvas c, BoardPalette p)
        {
            Body(c, new HashSet<Dir>(), p);
            Core(c, p.BlockerBody, p.BlockerEdge, p);
            c.ClearTransform();
            // The shield replaces the core dot.
            var outer = new List<float> { 20f, 14f, 25f, 16.5f, 25f, 21f };
            GlyphCanvas.AppendQuad(outer, 25f, 21f, 25f, 25f, 20f, 27f);
            GlyphCanvas.AppendQuad(outer, 20f, 27f, 15f, 25f, 15f, 21f);
            outer.Add(15f); outer.Add(16.5f);
            c.Polygon(outer.ToArray(), p.BlockerEdge);
            var inner = new List<float> { 20f, 16f, 23.5f, 17.8f, 23.5f, 21f };
            GlyphCanvas.AppendQuad(inner, 23.5f, 21f, 23.5f, 23.6f, 20f, 25f);
            c.Polygon(inner.ToArray(), BoardPalette.Alpha(p.Tip, 0.85f));
        }

        // Orthogonal lead: rect 6 x 12 at (17, 2) rx 1, tip rect 4 x 5 at (18, 2).
        static void Lead(GlyphCanvas c, float a, BoardPalette p)
        {
            c.SetTransform(a);
            c.Rect(17f, 2f, 6f, 12f, 1f, p.GlyphEdge);
            c.Rect(18f, 2f, 4f, 5f, 0f, p.Tip);
        }

        // Two bond wires at x 14.5 and 25.5: straight 14 -> 7, quadratic hook
        // to y 4.5 at x 17 / 23.
        static void Bond(GlyphCanvas c, float a, BoardPalette p)
        {
            c.SetTransform(a);
            c.Stroke(new[] { 14.5f, 14f, 14.5f, 7f }, 1f, p.GlyphWire);
            c.Quad(14.5f, 7f, 14.5f, 4.5f, 17f, 4.5f, 1f, p.GlyphWire, false);
            c.Stroke(new[] { 25.5f, 14f, 25.5f, 7f }, 1f, p.GlyphWire);
            c.Quad(25.5f, 7f, 25.5f, 4.5f, 23f, 4.5f, 1f, p.GlyphWire, false);
        }

        // One branch stub per side leaving each bond wire outward at y 7.5,
        // length 3, pad r 1.1.
        static void Stubs(GlyphCanvas c, float a, BoardPalette p)
        {
            c.SetTransform(a);
            c.Stroke(new[] { 25.5f, 7.5f, 28.5f, 7.5f }, 1f, p.GlyphWire);
            c.Circle(29.1f, 7.5f, 1.1f, p.GlyphWire);
            c.Stroke(new[] { 14.5f, 7.5f, 11.5f, 7.5f }, 1f, p.GlyphWire);
            c.Circle(10.9f, 7.5f, 1.1f, p.GlyphWire);
        }

        // Diagonal lead: line 20,14 -> 20,6 at 2.4, tip circle r 2.6 with a
        // r 1.3 white centre.
        static void Diag(GlyphCanvas c, float a, BoardPalette p)
        {
            c.SetTransform(a);
            c.Stroke(new[] { 20f, 14f, 20f, 6f }, 2.4f, p.GlyphEdge);
            c.Circle(20f, 5f, 2.6f, p.GlyphEdge);
            c.Circle(20f, 5f, 1.3f, p.Tip);
        }

        // Range-limited modifier (glyph-types M2): a stop bar under the tip,
        // one per cell of reach. Not in the locked guide; drawn in its grammar.
        static void StopBars(GlyphCanvas c, float a, int reach, BoardPalette p)
        {
            c.SetTransform(a);
            for (int n = 0; n < Mathf.Min(reach, 3); n++)
            {
                c.Rect(14f, 8f + n * 2.2f, 12f, 1.6f, 0f, p.Tip);
            }
        }

        // A body pin: rotate(a 20 20) translate(t 0), line from y 10.5 up
        // `len`, optional pad.
        static void Pin(GlyphCanvas c, float a, float t, float len, bool pad, BoardPalette p)
        {
            c.SetTransform(a, t, 0f);
            c.Stroke(new[] { 20f, 10.5f, 20f, 10.5f - len }, 1.1f, p.GlyphWire);
            if (pad) c.Circle(20f, 10.5f - len - 0.6f, 1.1f, p.GlyphWire);
        }

        // Three stubs on every inactive orthogonal edge, one on every vertex
        // between two inactive edges that no diagonal lead occupies.
        static void Body(GlyphCanvas c, HashSet<Dir> active, BoardPalette p)
        {
            foreach (var dir in Orth)
            {
                if (active.Contains(dir)) continue;
                float a = Angle(dir);
                Pin(c, a, -4f, 3.5f, true, p);
                Pin(c, a, 0f, 2.5f, false, p);
                Pin(c, a, 4f, 3.5f, true, p);
            }
            foreach (var v in Verts)
            {
                if (active.Contains(v.a) || active.Contains(v.b) || active.Contains(v.diag)) continue;
                Pin(c, v.angle, 0f, 2.5f, false, p);
            }
        }

        // Hexagon 20,9 30,14.5 30,25.5 20,31 10,25.5 10,14.5, stroke 1.6 round
        // joins, gloss quad 20,11 28,15.5 20,20 12,15.5 at white 40%.
        static void Core(GlyphCanvas c, Color fill, Color edge, BoardPalette p)
        {
            c.ClearTransform();
            var hex = new[] { 20f, 9f, 30f, 14.5f, 30f, 25.5f, 20f, 31f, 10f, 25.5f, 10f, 14.5f };
            c.Polygon(hex, fill);
            c.Stroke(hex, 1.6f, edge, true, true);
            c.Polygon(new[] { 20f, 11f, 28f, 15.5f, 20f, 20f, 12f, 15.5f }, BoardPalette.Alpha(p.Tip, 0.4f));
        }

        // Area bug: four outer arcs, four inner arcs, four rim dots, core
        // unchanged.
        static void AreaArcs(GlyphCanvas c, BoardPalette p)
        {
            for (int k = 0; k < 4; k++)
            {
                c.SetTransform(k * 90f);
                c.Quad(12f, 8f, 20f, 3f, 28f, 8f, 2f, p.GlyphEdge, true);
                c.Quad(14.5f, 12f, 20f, 9f, 25.5f, 12f, 1.2f, p.GlyphWire, true);
                c.Circle(20f, 3.5f, 1.6f, p.Tip);
            }
        }
    }
}
