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

        // Called when the skin changes: every cached glyph was rasterised
        // against the old colours. The sprites and their textures are
        // HideAndDontSave, which is neither garbage collected nor reached by
        // UnloadUnusedAssets, so dropping the references alone would leak the
        // whole set on every switch. Anything still on screen dies with the
        // screen that is rebuilt straight after.
        public static void ClearCache()
        {
            foreach (Sprite sprite in Cache.Values)
            {
                if (sprite == null) continue;
                if (sprite.texture != null) UnityEngine.Object.Destroy(sprite.texture);
                UnityEngine.Object.Destroy(sprite);
            }
            Cache.Clear();
        }

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
        //
        // Every mark from here down is a UI mark, not a bug: it has to read
        // on its own at the size it is drawn, in sunlight, so the floor is
        // a 2.6 stroke for a mark over a glyph and 4.4 for one in a chip
        // (viewBox units; on a 28 px chip icon that is a 3 px line).
        public static Sprite Lock(BoardPalette p, int sizePx)
        {
            return Cached($"lock:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                c.Quad(15.5f, 21f, 15.5f, 13.5f, 20f, 13.5f, 2.6f, p.GlyphEdge, false);
                c.Quad(20f, 13.5f, 24.5f, 13.5f, 24.5f, 21f, 2.6f, p.GlyphEdge, false);
                c.Rect(13.5f, 19.5f, 13f, 9.5f, 1.8f, p.GlyphEdge);
                c.Circle(20f, 23.2f, 1.6f, p.Tip);
                c.Rect(19.25f, 23.2f, 1.5f, 3f, 0f, p.Tip);
                return c.ToSprite($"mark_LOCK_{sizePx}");
            });
        }

        // The solved mark on a calendar day (R-1001: a shape, never colour
        // alone): a lit tick. Drawn at 0.36 of a tile, so the 5-unit stroke
        // is a 2.4 px line on a 54 px tile.
        public static Sprite Check(BoardPalette p, int sizePx)
        {
            return Cached($"check:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                c.Stroke(new[] { 8f, 21f, 16f, 29f, 32f, 11f }, 5f, p.Tip, true);
                return c.ToSprite($"mark_CHECK_{sizePx}");
            });
        }

        // The help mark. It was the "?" of whichever face the OS had, at the
        // chip's 12 px label size: a hairline in a 29 px box. Now it is a
        // stroked shape at the chip stroke, like the gear beside it.
        public static Sprite Question(BoardPalette p, int sizePx)
        {
            return Cached($"question:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                const float w = 4.4f;
                c.Quad(13.5f, 15.5f, 13.5f, 7.5f, 20f, 7.5f, w, p.Ink, true);
                c.Quad(20f, 7.5f, 26.5f, 7.5f, 26.5f, 14f, w, p.Ink, true);
                c.Quad(26.5f, 14f, 26.5f, 20.5f, 20f, 22f, w, p.Ink, true);
                c.Stroke(new[] { 20f, 22f, 20f, 26.5f }, w, p.Ink, true);
                c.Circle(20f, 32.5f, 2.8f, p.Ink);
                return c.ToSprite($"mark_QUESTION_{sizePx}");
            });
        }

        // A pager chevron. The pagers were "◀" and "▶" as chip labels,
        // which is a glyph the display face does not carry and the fallback
        // draws small; a drawn chevron is the same on every phone.
        // The pager's two marks. "Previous" points against the reading
        // direction and "next" along it, so under a right-to-left language
        // the pair swaps — the one mark in the chrome that mirrors, because
        // it is about progress through a sequence rather than a thing
        // (docs/I18N.md 7).
        public static Sprite Prev(BoardPalette p, int sizePx) => Chevron(p, sizePx, left: !Str.IsRtl);
        public static Sprite Next(BoardPalette p, int sizePx) => Chevron(p, sizePx, left: Str.IsRtl);

        public static Sprite Chevron(BoardPalette p, int sizePx, bool left)
        {
            return Cached($"chevron:{left}:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                c.SetTransform(left ? 0f : 180f);
                c.Stroke(new[] { 24f, 10.5f, 14.5f, 20f, 24f, 29.5f }, 4.6f, p.Ink, true);
                return c.ToSprite($"mark_CHEVRON_{(left ? "L" : "R")}_{sizePx}");
            });
        }

        // The settings mark: a toothed ring. Drawn rather than typed — the
        // display face has no gear in it and a dynamic OS font is not a
        // promise across phones, so this joins the lock and the tick as one
        // more primitive shape. Hollow, because there is no way to cut a hole
        // in a canvas that only paints over: the rim is a stroked circle.
        public static Sprite Gear(BoardPalette p, int sizePx)
        {
            return Cached($"gear:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                for (int k = 0; k < 8; k++)
                {
                    c.SetTransform(k * 45f);
                    c.Rect(17.5f, 6.5f, 5f, 6f, 1f, p.Ink);
                }
                c.ClearTransform();
                c.Stroke(Ring(8f), 4.4f, p.Ink, false, true);
                return c.ToSprite($"mark_GEAR_{sizePx}");
            });
        }

        // ---- cell marks, for the legend on the rules sheet ----
        //
        // The board itself draws these in the shader (GridInfectBoard, the
        // per-cell branch); these are the same shapes at the same fractions
        // of a tile, so a swatch and a cell read as the same thing.

        // Repel: the diamond, half-diagonal 0.19 of a tile.
        public static Sprite Repel(BoardPalette p, int sizePx)
        {
            return Cached($"repel:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                c.Polygon(new[] { 20f, 12.4f, 27.6f, 20f, 20f, 27.6f, 12.4f, 20f }, p.GlyphEdge);
                return c.ToSprite($"mark_REPEL_{sizePx}");
            });
        }

        // Trap: the ice cross, arms to 0.28 of a tile.
        public static Sprite Trap(BoardPalette p, int sizePx)
        {
            return Cached($"trap:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                c.Stroke(new[] { 9f, 9f, 31f, 31f }, 3.2f, p.Conflict);
                c.Stroke(new[] { 31f, 9f, 9f, 31f }, 3.2f, p.Conflict);
                return c.ToSprite($"mark_TRAP_{sizePx}");
            });
        }

        // Avoid (Cell.Forbidden): no chip at all. Bare copper — the ring and
        // the pad inside it — which is the whole of why it cannot be hit.
        public static Sprite Avoid(BoardPalette p, int sizePx)
        {
            return Cached($"avoid:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                c.Stroke(Ring(8.8f), 4f, p.CopperLo, false, true);
                c.Circle(20f, 20f, 4.1f, p.Copper);
                c.Circle(20f, 20f, 3.3f, p.CopperHi);
                return c.ToSprite($"mark_AVOID_{sizePx}");
            });
        }

        // The core dot every component cell carries: gold dormant, white lit
        // (R-1001 — the shape that stays while the colour changes).
        public static Sprite CellDot(BoardPalette p, bool lit, int sizePx)
        {
            return Cached($"dot:{lit}:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                c.Circle(20f, 20f, 3.3f, lit ? p.Tip : p.CopperHi);
                return c.ToSprite($"mark_DOT_{lit}_{sizePx}");
            });
        }

        // A closed polyline circle: the canvas paints over, never through, so
        // an annulus is a stroke rather than a disc with a hole in it.
        static float[] Ring(float radius, int steps = 32)
        {
            var xy = new float[steps * 2];
            for (int i = 0; i < steps; i++)
            {
                float a = i * Mathf.PI * 2f / steps;
                xy[i * 2] = 20f + Mathf.Cos(a) * radius;
                xy[i * 2 + 1] = 20f + Mathf.Sin(a) * radius;
            }
            return xy;
        }

        // Relay cells (RULES_V2 §12): a hub with one lead and pad per arm.
        //
        // It was drawn in the bug's wire vocabulary — 1.4-unit stubs with
        // 1.6 pads — which is a 1.5 px line on a 44 px glyph: the one board
        // mark that carries a rule (which way it fires) was the thinnest
        // thing on the board. Now it is drawn at the lead weight: a 3-unit
        // lead in the edge colour, a pad at the end, and copper points in
        // the pad and the hub (§2: copper is points, and a pad is a point).
        // Copper rather than white so a relay never reads as already lit.
        public static Sprite Relay(byte arms, BoardPalette p, int sizePx)
        {
            return Cached($"relay:{arms}:{sizePx}:{p.GlyphKey}", () =>
            {
                var c = new GlyphCanvas(sizePx);
                for (int d = 0; d < 8; d++)
                {
                    if ((arms & (1 << d)) == 0) continue;
                    c.SetTransform(Angle((Dir)d));
                    c.Stroke(new[] { 20f, 20f, 20f, 9.5f }, 3f, p.GlyphEdge);
                    c.Circle(20f, 8.5f, 3f, p.GlyphEdge);
                    c.Circle(20f, 8.5f, 1.6f, p.CopperHi);
                }
                c.ClearTransform();
                c.Circle(20f, 20f, 5.2f, p.GlyphEdge);
                c.Circle(20f, 20f, 2.6f, p.CopperHi);
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
                }
            }

            // The area bug has no leads and no body stubs: the arcs are its
            // whole outside. Arms on an area piece (the schema allows both)
            // keep their leads, and the stubs return only for the edges they leave.
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

        // Diagonal lead: line 20,14.5 -> 20,6.5 at 2.6, and the same wiring
        // a squared lead gets — two bond wires at x 17.6 / 22.4 running
        // 13.5 -> 8.5 and hooking in under the tip, a branch stub with a pad
        // leaving each wire outward at y 9.5 — under a tip circle r 3.2
        // with a r 2.1 white centre. A bare line and a dot read as a stray
        // highlight next to a wired squared lead; the wiring is what says
        // "this is a lead too". The hooks end inside the tip ring, so the
        // tip is drawn last and the wires go under it.
        static void Diag(GlyphCanvas c, float a, BoardPalette p)
        {
            c.SetTransform(a);
            c.Stroke(new[] { 20f, 14.5f, 20f, 6.5f }, 2.6f, p.GlyphEdge);
            c.Stroke(new[] { 17.6f, 13.5f, 17.6f, 8.5f }, 1f, p.GlyphWire);
            c.Quad(17.6f, 8.5f, 17.6f, 6.6f, 18.6f, 6.6f, 1f, p.GlyphWire, false);
            c.Stroke(new[] { 22.4f, 13.5f, 22.4f, 8.5f }, 1f, p.GlyphWire);
            c.Quad(22.4f, 8.5f, 22.4f, 6.6f, 21.4f, 6.6f, 1f, p.GlyphWire, false);
            c.Stroke(new[] { 17.6f, 9.5f, 14.6f, 9.5f }, 1f, p.GlyphWire);
            c.Circle(14f, 9.5f, 1.1f, p.GlyphWire);
            c.Stroke(new[] { 22.4f, 9.5f, 25.4f, 9.5f }, 1f, p.GlyphWire);
            c.Circle(26f, 9.5f, 1.1f, p.GlyphWire);
            c.Circle(20f, 5.5f, 3.2f, p.GlyphEdge);
            c.Circle(20f, 5.5f, 2.1f, p.Tip);
        }

        // The diagonal lead that sits clockwise (+45) or anticlockwise (-45)
        // of an orthogonal edge, for the stubs that give way to it.
        static Dir DiagAt(float angle)
        {
            switch (((int)angle % 360 + 360) % 360)
            {
                case 45: return Dir.UR;
                case 135: return Dir.DR;
                case 225: return Dir.DL;
                default: return Dir.UL;
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
        // between two inactive edges that no diagonal lead occupies. An
        // edge's outer stub gives way to an active diagonal lead beside it:
        // the lead's own branch pad lands exactly where that stub was, and
        // two pads on top of each other read as a smudge.
        static void Body(GlyphCanvas c, HashSet<Dir> active, BoardPalette p)
        {
            foreach (var dir in Orth)
            {
                if (active.Contains(dir)) continue;
                float a = Angle(dir);
                if (!active.Contains(DiagAt(a - 45f))) Pin(c, a, -4f, 3.5f, true, p);
                Pin(c, a, 0f, 2.5f, false, p);
                if (!active.Contains(DiagAt(a + 45f))) Pin(c, a, 4f, 3.5f, true, p);
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

        // Area bug: four outer arcs, four inner arcs, core unchanged. The
        // spec's four white rim dots are gone: on the blot they sat outside
        // the body reading as four stray highlights, and the arcs already
        // describe the 3x3 footprint. White stays for the lit lead tips,
        // where it means something.
        static void AreaArcs(GlyphCanvas c, BoardPalette p)
        {
            for (int k = 0; k < 4; k++)
            {
                c.SetTransform(k * 90f);
                c.Quad(12f, 8f, 20f, 3f, 28f, 8f, 2f, p.GlyphEdge, true);
                c.Quad(14.5f, 12f, 20f, 9f, 25.5f, 12f, 1.2f, p.GlyphWire, true);
            }
        }
    }
}
