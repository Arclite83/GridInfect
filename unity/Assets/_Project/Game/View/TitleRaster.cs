using System.Collections.Generic;
using UnityEngine;

namespace GridInfect.Game
{
    // The wordmark, drawn at runtime (STYLE-GUIDE §12). The letters are the
    // Chakra Petch outlines gen-logo.mjs emitted (LogoGlyphs.g.cs), so the
    // title on the phone and the SVG in out/logo are the same shapes under
    // the same numbers, and the build carries neither a font nor a bitmap.
    //
    // Two materials, both from the guide: dense glass (GRID, and INFECT
    // before it lights) and lit (INFECT). Each letter is one sprite,
    // rasterised through GlyphCanvas at the device scale and cached, so the
    // title screen can light INFECT one letter at a time.
    //
    // Frame: "logo px" is the lock's frame, 92 px type. The origin is the
    // pen start of GRID on the x-height midline, y down. TitleView maps it
    // onto the screen with one scale.
    public static class TitleRaster
    {
        public enum Material { Dense, Lit }

        const float Em = LogoGlyphs.FontPx / LogoGlyphs.Em;   // logo px per outline unit
        public const float Baseline = LogoGlyphs.FontPx * LogoGlyphs.BaselineBelowMid;

        public static float GridWidth => LogoGlyphs.GridWidth * Em;
        public static float InfectWidth => LogoGlyphs.InfectWidth * Em;
        public static float InfectX => GridWidth + LogoGlyphs.GapPx;
        public static float TotalWidth => InfectX + InfectWidth;
        public static float BugX => GridWidth + LogoGlyphs.GapPx / 2f;

        // The margin a letter's canvas keeps around its box, logo px: the
        // drop shadow's reach (3 sigma + dy) for dense, the wide glow's for lit.
        const float DenseMargin = 26f;
        const float LitMargin = 44f;

        // A rasterised letter and where its sprite's centre sits, logo px.
        public struct Glyph
        {
            public Sprite Sprite;
            public float CenterX, CenterY;
        }

        static readonly Dictionary<string, Glyph> Cache = new Dictionary<string, Glyph>();

        // The logo does not reskin (§12): always the green skin's tokens.
        static BoardPalette Palette => BoardPalette.Preview(BoardPalette.SkinId.Default);

        // ---- layout ----

        static float[] Pens(string word) => word == LogoGlyphs.Grid ? LogoGlyphs.GridPen : LogoGlyphs.InfectPen;
        static float WordX(string word) => word == LogoGlyphs.Grid ? 0f : InfectX;

        // A letter's contours in logo px, in the mark's frame.
        static float[][] Outline(string word, int index)
        {
            float[][] src = LogoGlyphs.Contours(word[index]);
            float px = WordX(word) + Pens(word)[index] * Em;
            var dst = new float[src.Length][];
            for (int c = 0; c < src.Length; c++)
            {
                dst[c] = new float[src[c].Length];
                for (int i = 0; i < src[c].Length; i += 2)
                {
                    dst[c][i] = px + src[c][i] * Em;
                    dst[c][i + 1] = Baseline + src[c][i + 1] * Em;
                }
            }
            return dst;
        }

        static Rect Bounds(float[][] contours)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (float[] xy in contours)
            {
                for (int i = 0; i < xy.Length; i += 2)
                {
                    minX = Mathf.Min(minX, xy[i]); maxX = Mathf.Max(maxX, xy[i]);
                    minY = Mathf.Min(minY, xy[i + 1]); maxY = Mathf.Max(maxY, xy[i + 1]);
                }
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        // The word's box: the gradient runs over the word, not the letter,
        // the way the SVG's objectBoundingBox does on the one path per word.
        static Rect WordBounds(string word)
        {
            Rect r = Bounds(Outline(word, 0));
            for (int i = 1; i < word.Length; i++)
            {
                Rect b = Bounds(Outline(word, i));
                float minX = Mathf.Min(r.x, b.x), minY = Mathf.Min(r.y, b.y);
                float maxX = Mathf.Max(r.x + r.width, b.x + b.width), maxY = Mathf.Max(r.y + r.height, b.y + b.height);
                r = new Rect(minX, minY, maxX - minX, maxY - minY);
            }
            return r;
        }

        // ---- materials ----

        // The guide's 160° gradient: x1 0, y1 0 -> x2 .3, y2 1 over the box.
        static float GradientT(Rect box, float x, float y)
        {
            float u = (x - box.x) / Mathf.Max(box.width, 1e-3f), v = (y - box.y) / Mathf.Max(box.height, 1e-3f);
            return Mathf.Clamp01((u * 0.3f + v) / 1.09f);
        }

        static Color Stops(float t, float[] at, Color[] col)
        {
            if (t <= at[0]) return col[0];
            for (int i = 1; i < at.Length; i++)
            {
                if (t <= at[i]) return Color.Lerp(col[i - 1], col[i], (t - at[i - 1]) / (at[i] - at[i - 1]));
            }
            return col[col.Length - 1];
        }

        static Color White(float a) => new Color(1f, 1f, 1f, a);

        // Dense glass: white 66% -> 30% (55%) -> 44%, rim white 90% 1.4 px,
        // drop shadow 0 5 px blur 6 black 38%.
        static readonly float[] DenseAt = { 0f, 0.55f, 1f };
        static readonly Color[] DenseCol = { White(0.66f), White(0.30f), White(0.44f) };

        // Lit: white 92% -> infectHi (12%) -> infect (55%) -> infectLo, rim
        // white 60% 1.3 px, glow blur 14 at 75% and blur 7 at 50%, both infect.
        static readonly float[] LitAt = { 0f, 0.12f, 0.55f, 1f };

        // ---- sprites ----

        public static Glyph Letter(string word, int index, Material material, float scale)
        {
            string key = $"{word}:{index}:{material}:{scale:F4}";
            if (Cache.TryGetValue(key, out var cached) && cached.Sprite != null) return cached;

            var p = Palette;
            float[][] outline = Outline(word, index);
            Rect box = Bounds(outline);
            Rect wordBox = WordBounds(word);
            float margin = material == Material.Lit ? LitMargin : DenseMargin;
            float ox = box.x - margin, oy = box.y - margin;
            int w = Mathf.CeilToInt((box.width + margin * 2f) * scale);
            int h = Mathf.CeilToInt((box.height + margin * 2f) * scale);
            var canvas = new GlyphCanvas(w, h, scale);
            var field = canvas.PathField(outline, -ox, -oy);
            // Canvas pixel -> the mark's frame, for the gradient.
            System.Func<int, int, float> gx = (x, y) => ox + (x + 0.5f) / scale;
            System.Func<int, int, float> gy = (x, y) => oy + (y + 0.5f) / scale;

            if (material == Material.Dense)
            {
                canvas.PaintField(field, (x, y) => BoardPalette.Alpha(p.Shade, 0.38f), sigmaPx: 6f * scale, shiftY: Mathf.RoundToInt(5f * scale));
                canvas.PaintField(field, (x, y) => Stops(GradientT(wordBox, gx(x, y), gy(x, y)), DenseAt, DenseCol));
                canvas.PaintField(field, (x, y) => White(0.9f), rimPx: 1.4f * scale);
            }
            else
            {
                var litCol = new[] { White(0.92f), p.InfectHi, p.Infect, p.InfectLo };
                canvas.PaintField(field, (x, y) => BoardPalette.Alpha(p.Infect, 0.75f), sigmaPx: 14f * scale);
                canvas.PaintField(field, (x, y) => BoardPalette.Alpha(p.Infect, 0.5f), sigmaPx: 7f * scale);
                canvas.PaintField(field, (x, y) => Stops(GradientT(wordBox, gx(x, y), gy(x, y)), LitAt, litCol));
                canvas.PaintField(field, (x, y) => White(0.6f), rimPx: 1.3f * scale);
            }

            var glyph = new Glyph
            {
                Sprite = canvas.ToSprite($"title_{word[index]}_{material}_{w}"),
                CenterX = ox + w / scale / 2f,
                CenterY = oy + h / scale / 2f,
            };
            Cache[key] = glyph;
            return glyph;
        }

        // The bug's backing: the tile's 35% infect glow (28 x 28 rx 6 in the
        // 40 px glyph frame, blur 6), scaled to the 56 px bug.
        public static Glyph BugBacking(float scale)
        {
            string key = $"bugback:{scale:F4}";
            if (Cache.TryGetValue(key, out var cached) && cached.Sprite != null) return cached;

            var p = Palette;
            float k = LogoGlyphs.BugPx / 40f;
            float side = 28f * k, rx = 6f * k, sigma = 6f * k;
            float margin = sigma * 3f;
            int px = Mathf.CeilToInt((side + margin * 2f) * scale);
            var canvas = new GlyphCanvas(px, px, scale);
            var field = canvas.RoundRectField(margin, margin, side, side, rx);
            canvas.PaintField(field, (x, y) => BoardPalette.Alpha(p.Infect, 0.35f), sigmaPx: sigma * scale);
            var glyph = new Glyph { Sprite = canvas.ToSprite($"title_bugback_{px}"), CenterX = BugX, CenterY = 0f };
            Cache[key] = glyph;
            return glyph;
        }

        // The bug itself: bug_E, the same glyph a tile wears, at the lock's
        // 56 px, green skin.
        public static Sprite Bug(float scale)
        {
            var spec = new GridInfect.Core.PieceSpec((byte)(1 << (int)GridInfect.Core.Dir.R));
            return BugGlyph.Piece(spec, Palette, Mathf.RoundToInt(LogoGlyphs.BugPx * scale));
        }
    }
}
