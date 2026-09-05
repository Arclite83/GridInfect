using System.Collections.Generic;
using UnityEngine;

namespace GridInfect.Game
{
    // A tiny vector rasteriser: the bug glyph grammar is authored as SVG
    // (grid-infect-bug-glyph/gen-bug-glyph.mjs, grid-infect-style/gen-assets.mjs)
    // and Unity has no SVG loader in the box, so the same primitives are
    // drawn here at whatever pixel size the screen needs. Every primitive is
    // a signed distance field with a half-pixel anti-aliasing ramp, painted
    // in order with premultiplied "over", which is exactly what the SVG does.
    //
    // Coordinates are the glyph's own viewBox (40 x 40, y down, centre 20,20)
    // so the numbers in the style guide can be typed in unchanged. The
    // transform stack mirrors SVG `rotate(a 20 20) translate(t 0)`.
    public sealed class GlyphCanvas
    {
        public readonly int Size;
        readonly float _viewBox;
        readonly float _scale;                 // px per viewBox unit
        readonly float[] _r, _g, _b, _a;       // premultiplied accumulation

        float _angle;                          // degrees, clockwise (y down)
        float _tx, _ty;

        public GlyphCanvas(int sizePx, float viewBox = 40f)
        {
            Size = Mathf.Max(1, sizePx);
            _viewBox = viewBox;
            _scale = Size / viewBox;
            int n = Size * Size;
            _r = new float[n]; _g = new float[n]; _b = new float[n]; _a = new float[n];
        }

        // ---- transform ----

        public void SetTransform(float angleDeg, float tx = 0f, float ty = 0f)
        {
            _angle = angleDeg;
            _tx = tx;
            _ty = ty;
        }

        public void ClearTransform() => SetTransform(0f);

        Vector2 Apply(float x, float y)
        {
            x += _tx;
            y += _ty;
            if (_angle == 0f) return new Vector2(x, y);
            float c = _viewBox * 0.5f;
            float rad = _angle * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
            float dx = x - c, dy = y - c;
            return new Vector2(c + dx * cs - dy * sn, c + dx * sn + dy * cs);
        }

        // ---- primitives ----

        public void Circle(float cx, float cy, float r, Color col)
        {
            Vector2 c = Apply(cx, cy);
            Paint(c.x - r, c.y - r, c.x + r, c.y + r, col, p => (p - c).magnitude - r);
        }

        // An SVG <rect> with rx, in the current transform. Rotation is
        // handled by measuring in the rect's own frame.
        public void Rect(float x, float y, float w, float h, float rx, Color col)
        {
            if (rx <= 0f)
            {
                Polygon(new[] { x, y, x + w, y, x + w, y + h, x, y + h }, col);
                return;
            }
            Vector2 c = Apply(x + w / 2f, y + h / 2f);
            float rad = _angle * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
            float hw = w / 2f, hh = h / 2f;
            float reach = Mathf.Sqrt(hw * hw + hh * hh);
            Paint(c.x - reach, c.y - reach, c.x + reach, c.y + reach, col, p =>
            {
                float dx = p.x - c.x, dy = p.y - c.y;
                // Rotate back into the rect's frame.
                float lx = dx * cs + dy * sn, ly = -dx * sn + dy * cs;
                float qx = Mathf.Abs(lx) - hw + rx, qy = Mathf.Abs(ly) - hh + rx;
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - rx;
            });
        }

        // A filled polygon from a flat x,y list, transformed. Concave is fine.
        public void Polygon(float[] xy, Color col)
        {
            int n = xy.Length / 2;
            var v = new Vector2[n];
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                v[i] = Apply(xy[i * 2], xy[i * 2 + 1]);
                minX = Mathf.Min(minX, v[i].x); maxX = Mathf.Max(maxX, v[i].x);
                minY = Mathf.Min(minY, v[i].y); maxY = Mathf.Max(maxY, v[i].y);
            }
            Paint(minX, minY, maxX, maxY, col, p => PolygonDistance(v, p));
        }

        // A stroked open path: segments as capsules (round joins fall out),
        // butt or round caps at the two ends.
        public void Stroke(float[] xy, float width, Color col, bool roundCaps = false, bool closed = false)
        {
            int n = xy.Length / 2;
            var v = new Vector2[n];
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                v[i] = Apply(xy[i * 2], xy[i * 2 + 1]);
                minX = Mathf.Min(minX, v[i].x); maxX = Mathf.Max(maxX, v[i].x);
                minY = Mathf.Min(minY, v[i].y); maxY = Mathf.Max(maxY, v[i].y);
            }
            float hw = width / 2f;
            int segs = closed ? n : n - 1;
            Paint(minX - hw, minY - hw, maxX + hw, maxY + hw, col, p =>
            {
                float best = float.MaxValue;
                for (int i = 0; i < segs; i++)
                {
                    Vector2 a = v[i], b = v[(i + 1) % n];
                    bool capA = !closed && i == 0 && !roundCaps;
                    bool capB = !closed && i == segs - 1 && !roundCaps;
                    best = Mathf.Min(best, SegmentDistance(p, a, b, hw, capA, capB));
                }
                return best;
            });
        }

        // A quadratic bezier, flattened; the wires and arcs are gentle enough
        // for eight pieces.
        public void Quad(float x0, float y0, float cx, float cy, float x1, float y1, float width, Color col, bool roundCaps)
        {
            const int steps = 8;
            var xy = new float[(steps + 1) * 2];
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps, u = 1f - t;
                xy[i * 2] = u * u * x0 + 2f * u * t * cx + t * t * x1;
                xy[i * 2 + 1] = u * u * y0 + 2f * u * t * cy + t * t * y1;
            }
            Stroke(xy, width, col, roundCaps);
        }

        // Flatten a quadratic into a point list, for paths that mix lines
        // and curves and then fill.
        public static void AppendQuad(List<float> xy, float x0, float y0, float cx, float cy, float x1, float y1)
        {
            const int steps = 8;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps, u = 1f - t;
                xy.Add(u * u * x0 + 2f * u * t * cx + t * t * x1);
                xy.Add(u * u * y0 + 2f * u * t * cy + t * t * y1);
            }
        }

        // ---- distance fields ----

        static float PolygonDistance(Vector2[] v, Vector2 p)
        {
            int n = v.Length;
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                float t = Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(Vector2.Dot(e, e), 1e-6f));
                Vector2 b = w - e * t;
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c0 = p.y >= v[i].y, c1 = p.y < v[j].y, c2 = e.x * w.y > e.y * w.x;
                if ((c0 && c1 && c2) || (!c0 && !c1 && !c2)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        // Distance to a stroked segment of half-width hw; a butt cap flattens
        // that end, a round cap (the default here) leaves the capsule.
        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b, float hw, bool buttA, bool buttB)
        {
            Vector2 ab = b - a;
            float len = Mathf.Max(ab.magnitude, 1e-6f);
            Vector2 dir = ab * (1f / len);
            Vector2 ap = p - a;
            float along = Vector2.Dot(ap, dir);
            float across = Mathf.Abs(ap.x * dir.y - ap.y * dir.x);
            if (!buttA && along < 0f) return (p - a).magnitude - hw;
            if (!buttB && along > len) return (p - b).magnitude - hw;
            // Past either butt end, the distance grows along the axis.
            float dx = Mathf.Max(0f, Mathf.Max(-along, along - len));
            float dy = across - hw;
            if (dx > 0f && dy > 0f) return Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Max(dx, dy);
        }

        // ---- painting ----

        void Paint(float minX, float minY, float maxX, float maxY, Color col, System.Func<Vector2, float> sdf)
        {
            if (col.a <= 0f) return;
            float pad = 1.5f / _scale;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((minX - pad) * _scale), 0, Size - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((maxX + pad) * _scale), 0, Size - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((minY - pad) * _scale), 0, Size - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt((maxY + pad) * _scale), 0, Size - 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2((x + 0.5f) / _scale, (y + 0.5f) / _scale);
                    float dPx = sdf(p) * _scale;
                    float cov = Mathf.Clamp01(0.5f - dPx) * col.a;
                    if (cov <= 0f) continue;
                    int n = y * Size + x;
                    float keep = 1f - cov;
                    _r[n] = col.r * cov + _r[n] * keep;
                    _g[n] = col.g * cov + _g[n] * keep;
                    _b[n] = col.b * cov + _b[n] * keep;
                    _a[n] = cov + _a[n] * keep;
                }
            }
        }

        // ---- output ----

        // Straight alpha, y flipped: canvas row 0 is the top, texture row 0
        // the bottom.
        public Texture2D ToTexture(string name)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                int src = (Size - 1 - y) * Size;
                for (int x = 0; x < Size; x++)
                {
                    float a = _a[src + x];
                    float inv = a > 1e-5f ? 1f / a : 0f;
                    pixels[y * Size + x] = new Color(_r[src + x] * inv, _g[src + x] * inv, _b[src + x] * inv, a);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }

        // 1 texel = 1 world unit = 1 screen px, so the sprite lands at exactly
        // the size it was rasterised for.
        public Sprite ToSprite(string name)
        {
            var texture = ToTexture(name);
            var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 1f);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
