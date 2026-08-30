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
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0f);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => default;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) =>
            new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1f, 1f, 1f);
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height) { }
        public bool Contains(Vector2 point) => false;
    }

    public static class Mathf
    {
        public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        public static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
        public static float Abs(float v) => v < 0f ? -v : v;
    }

    public enum HideFlags { None, HideAndDontSave }
    public enum TextureFormat { RGBA32 }
    public enum TextAnchor { MiddleCenter }
    public enum TextAlignment { Center }
    public enum CameraClearFlags { SolidColor }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }

    public class Object
    {
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }
        public static void Destroy(Object obj) { }
        public static void DontDestroyOnLoad(Object obj) { }
        public static T FindFirstObjectByType<T>() where T : Object => null;
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

    public sealed class Material : Object { }

    public sealed class Texture2D : Object
    {
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public void SetPixel(int x, int y, Color color) { }
        public void Apply() { }
    }

    public sealed class Sprite : Object
    {
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) => null;
    }

    public sealed class Font : Object
    {
        public Material material => null;
        public static Font CreateDynamicFontFromOSFont(string fontname, int size) => null;
    }

    public sealed class TextMesh : Component
    {
        public Font font { get; set; }
        public string text { get; set; }
        public int fontSize { get; set; }
        public float characterSize { get; set; }
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
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
    }
}
