// Compile-time stubs for the UnityEngine APIs the adapter layer uses.
// Signatures mirror Unity's public API; bodies are inert. This assembly
// exists so `dotnet build` can type-check unity/Assets/_Project/Game on a
// machine without Unity — it is never executed and never shipped.
#pragma warning disable IDE0060, CA1822
using System;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => default;
        public float magnitude => (float)System.Math.Sqrt(x * x + y * y);
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float f) => new Vector2(a.x * f, a.y * f);
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0f);
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => default;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) =>
            new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float f) => new Vector3(a.x * f, a.y * f, a.z * f);
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1f, 1f, 1f);
        public static Color black => new Color(0f, 0f, 0f);
        public static Color clear => new Color(0f, 0f, 0f, 0f);
        public Color linear => this;
        public Color gamma => this;
        public static Color Lerp(Color a, Color b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
        }
        public static Color operator *(Color a, float f) => new Color(a.r * f, a.g * f, a.b * f, a.a * f);
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height) { }
        public bool Contains(Vector2 point) => false;
    }

    public static class Mathf
    {
        public const float PI = 3.14159274f;
        public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        public static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
        public static float Abs(float v) => v < 0f ? -v : v;
        public static int Abs(int v) => v < 0 ? -v : v;
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static int RoundToInt(float v) => (int)System.Math.Round(v);
        public static float Sin(float v) => (float)System.Math.Sin(v);
        public static float Cos(float v) => (float)System.Math.Cos(v);
        public static float Exp(float v) => (float)System.Math.Exp(v);
        public static float Pow(float a, float b) => (float)System.Math.Pow(a, b);
        public static float Sqrt(float v) => (float)System.Math.Sqrt(v);
        public static float Atan2(float y, float x) => (float)System.Math.Atan2(y, x);
        public static float Floor(float v) => (float)System.Math.Floor(v);
        public static float Ceil(float v) => (float)System.Math.Ceiling(v);
        public static int FloorToInt(float v) => (int)System.Math.Floor(v);
        public static int CeilToInt(float v) => (int)System.Math.Ceiling(v);
        public static float Round(float v) => (float)System.Math.Round(v);
        public const float Deg2Rad = PI / 180f;
    }

    public enum HideFlags { None, HideAndDontSave }
    public enum ColorSpace { Gamma, Linear }
    public enum TextureFormat { RGBA32, RGBAFloat }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum TextAnchor { MiddleCenter, MiddleLeft, MiddleRight, UpperLeft, UpperRight, LowerLeft, LowerRight }
    public enum TextAlignment { Center, Left, Right }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum CameraClearFlags { SolidColor }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinAttribute : Attribute
    {
        public MinAttribute(float min) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string menuName { get; set; }
        public string fileName { get; set; }
        public int order { get; set; }
    }

    public class Object
    {
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }
        public static void Destroy(Object obj) { }
        public static void DontDestroyOnLoad(Object obj) { }
        public static T FindAnyObjectByType<T>() where T : Object => null;
    }

    public sealed class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public Transform transform => null;
        public string tag { get; set; }
        public T AddComponent<T>() where T : Component, new() => new T();
        public T GetComponent<T>() where T : Component => null;
        public T GetComponentInChildren<T>() where T : Component => null;
        public void SetActive(bool active) { }
    }

    public class Component : Object
    {
        public Transform transform => null;
        public GameObject gameObject => null;
    }

    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Transform parent { get; set; }
        public int childCount => 0;
        public Transform GetChild(int index) => null;
        public void SetParent(Transform parent, bool worldPositionStays) { }
    }

    public class Behaviour : Component { }

    public class MonoBehaviour : Behaviour { }

    public class Renderer : Component
    {
        public int sortingOrder { get; set; }
        public Material sharedMaterial { get; set; }
    }

    public sealed class SpriteRenderer : Renderer
    {
        public Sprite sprite { get; set; }
        public Color color { get; set; }
    }

    public sealed class MeshRenderer : Renderer { }

    public sealed class Mesh : Object
    {
        public Vector3[] vertices { get; set; }
        public Vector2[] uv { get; set; }
        public int[] triangles { get; set; }
        public void RecalculateBounds() { }
    }

    public sealed class MeshFilter : Component
    {
        public Mesh sharedMesh { get; set; }
        public Mesh mesh { get; set; }
    }

    public class Shader : Object
    {
        public static Shader Find(string name) => null;
        public static int PropertyToID(string name) => 0;
    }

    public sealed class Material : Object
    {
        public Material(Shader shader) { }
        public void SetFloat(string name, float value) { }
        public void SetFloat(int nameID, float value) { }
        public void SetColor(string name, Color value) { }
        public void SetColor(int nameID, Color value) { }
        public void SetVector(string name, Vector4 value) { }
        public void SetVector(int nameID, Vector4 value) { }
        public void SetTexture(string name, Texture value) { }
    }

    public class Texture : Object
    {
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
    }

    public sealed class Texture2D : Texture
    {
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain, bool linear) { }
        public void SetPixel(int x, int y, Color color) { }
        public void SetPixels(Color[] colors) { }
        public void Apply() { }
        public void Apply(bool updateMipmaps) { }
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new T();
    }

    public sealed class AudioClip : Object
    {
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) => null;
        public bool SetData(float[] data, int offsetSamples) => true;
    }

    public sealed class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public bool playOnAwake { get; set; }
        public float spatialBlend { get; set; }
        public float volume { get; set; }
        public float pitch { get; set; }
        public void Play() { }
    }

    public sealed class Sprite : Object
    {
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) => null;
    }

    public sealed class Font : Object
    {
        public Material material => null;
        public static Font CreateDynamicFontFromOSFont(string fontname, int size) => null;
        public static string[] GetOSInstalledFontNames() => new string[0];
    }

    public sealed class TextMesh : Component
    {
        public Font font { get; set; }
        public string text { get; set; }
        public int fontSize { get; set; }
        public float characterSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor anchor { get; set; }
        public TextAlignment alignment { get; set; }
        public Color color { get; set; }
    }

    public sealed class Camera : Behaviour
    {
        public static Camera main => null;
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public Vector3 ScreenToWorldPoint(Vector3 position) => default;
    }

    public static class Screen
    {
        public static int width => 1280;
        public static int height => 720;
    }

    public static class QualitySettings
    {
        public static ColorSpace activeColorSpace => ColorSpace.Linear;
    }

    public static class Application
    {
        public static int targetFrameRate { get; set; }
        public static string persistentDataPath => "";
    }

    public static class Time
    {
        public static float unscaledTime => 0f;
        public static float unscaledDeltaTime => 0f;
    }

    public static class Input
    {
        public static Vector3 mousePosition => default;
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButton(int button) => false;
        public static bool GetMouseButtonUp(int button) => false;
    }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object => null;
        public static T Load<T>(string path) where T : Object => null;
    }

    public static class Debug
    {
        public static bool isDebugBuild => false;
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
    }
}

namespace UnityEngine.Rendering
{
    public abstract class VolumeParameter<T>
    {
        public T value;
        public bool overrideState;
        public void Override(T newValue) { value = newValue; overrideState = true; }
    }

    public sealed class ClampedFloatParameter : VolumeParameter<float> { }

    public sealed class MinFloatParameter : VolumeParameter<float> { }

    public class VolumeComponent : ScriptableObject
    {
        public bool active = true;
    }

    public sealed class VolumeProfile : ScriptableObject
    {
        public T Add<T>(bool overrides) where T : VolumeComponent, new() => new T();
    }

    public sealed class Volume : Behaviour
    {
        public bool isGlobal { get; set; }
        public float priority { get; set; }
        public VolumeProfile profile { get; set; }
        public VolumeProfile sharedProfile { get; set; }
    }
}

namespace UnityEngine.Rendering.Universal
{
    public sealed class Bloom : VolumeComponent
    {
        public MinFloatParameter threshold = new MinFloatParameter();
        public MinFloatParameter intensity = new MinFloatParameter();
        public ClampedFloatParameter scatter = new ClampedFloatParameter();
    }

    public enum TonemappingMode { None, Neutral, ACES }

    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode> { }

    public sealed class Tonemapping : VolumeComponent
    {
        public TonemappingModeParameter mode = new TonemappingModeParameter();
    }

    public sealed class UniversalAdditionalCameraData : MonoBehaviour
    {
        public bool renderPostProcessing { get; set; }
    }

    public static class CameraExtensions
    {
        public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera camera) => null;
    }
}
