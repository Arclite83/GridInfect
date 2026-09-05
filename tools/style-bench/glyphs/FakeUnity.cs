// Just enough UnityEngine for GlyphRaster/BugGlyph/BoardPalette to run for real and dump PNGs.
using System;
namespace UnityEngine
{
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude => MathF.Sqrt(x * x + y * y);
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float f) => new Vector2(a.x * f, a.y * f); }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color Lerp(Color a, Color b, float t) => new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); }
    public struct Rect { public Rect(float x, float y, float w, float h) { } }
    public static class Mathf { public const float PI = MathF.PI; public const float Deg2Rad = PI / 180f;
        public static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v; public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Abs(float v) => MathF.Abs(v); public static float Max(float a, float b) => MathF.Max(a, b); public static float Min(float a, float b) => MathF.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b); public static int Min(int a, int b) => Math.Min(a, b);
        public static float Sqrt(float v) => MathF.Sqrt(v); public static float Cos(float v) => MathF.Cos(v); public static float Sin(float v) => MathF.Sin(v);
        public static int FloorToInt(float v) => (int)MathF.Floor(v); public static int CeilToInt(float v) => (int)MathF.Ceiling(v); public static int RoundToInt(float v) => (int)MathF.Round(v); }
    public enum HideFlags { None, HideAndDontSave } public enum TextureFormat { RGBA32 } public enum FilterMode { Point, Bilinear } public enum TextureWrapMode { Repeat, Clamp }
    public class Object { public string name { get; set; } public HideFlags hideFlags { get; set; } }
    public sealed class Texture2D : Object { public int W, H; public Color[] Pixels;
        public FilterMode filterMode { get; set; } public TextureWrapMode wrapMode { get; set; }
        public Texture2D(int w, int h, TextureFormat f, bool mip) { W = w; H = h; }
        public void SetPixels(Color[] c) { Pixels = c; } public void Apply(bool m) { } }
    public sealed class Sprite : Object { public Texture2D Tex; public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu) => new Sprite { Tex = t }; }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject, new() => new T(); }
    public static class Resources { public static T Load<T>(string p) where T : Object => null; }
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute { public MinAttribute(float m) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute : Attribute { public string menuName { get; set; } public string fileName { get; set; } }
}
