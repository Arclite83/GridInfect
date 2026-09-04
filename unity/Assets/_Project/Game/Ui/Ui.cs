using UnityEngine;
using S = GridInfect.Game.PresentationConfig.Style;

namespace GridInfect.Game
{
    // Procedural primitives: one white texture, glass panels, OS fonts, zero
    // assets. 1 world unit = 1 screen pixel, origin at screen center.
    public static class Ui
    {
        static Sprite _whiteSprite;
        static Font _font;
        static Font _mono;

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    texture.SetPixel(0, 0, UnityEngine.Color.white);
                    texture.Apply();
                    texture.hideFlags = HideFlags.HideAndDontSave;
                    _whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                    _whiteSprite.hideFlags = HideFlags.HideAndDontSave;
                }
                return _whiteSprite;
            }
        }

        // The guide's display face is Chakra Petch and its mono face Share
        // Tech Mono (STYLE-GUIDE §7). Neither ships with a phone, so each is
        // asked for first and a system face stands in until the TTFs are
        // imported. Newer editors (6000.5+) dropped the built-in legacy
        // fonts, and probing a missing builtin logs an error — ask the OS.
        static Font FindFont(string[] preferred)
        {
            string[] installed = Font.GetOSInstalledFontNames() ?? new string[0];
            foreach (string name in preferred)
            {
                if (System.Array.IndexOf(installed, name) >= 0) return Font.CreateDynamicFontFromOSFont(name, 64);
            }
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacySans.ttf"); }
            catch (System.Exception) { }
            if (font == null)
            {
                try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch (System.Exception) { }
            }
            if (font == null && installed.Length > 0) font = Font.CreateDynamicFontFromOSFont(installed[0], 64);
            return font;
        }

        public static Font UiFont
        {
            get
            {
                if (_font == null)
                {
                    _font = FindFont(new[] { "Chakra Petch", "Chakra Petch Medium", "Arial", "Helvetica", "Segoe UI", "Liberation Sans", "DejaVu Sans", "Roboto" });
                }
                return _font;
            }
        }

        public static Font MonoFont
        {
            get
            {
                if (_mono == null)
                {
                    _mono = FindFont(new[] { "Share Tech Mono", "Menlo", "Consolas", "Courier New", "Liberation Mono", "DejaVu Sans Mono", "Roboto Mono" });
                    if (_mono == null) _mono = UiFont;
                }
                return _mono;
            }
        }

        // A flat rectangle. Still used for dims and covers; every visible
        // chrome element is glass now.
        public static GameObject MakeRect(string name, Transform parent, Vector2 sizePx, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            go.transform.localScale = new Vector3(sizePx.x, sizePx.y, 1f);
            return go;
        }

        // A sprite at its rasterised size (1 texel = 1 px).
        public static SpriteRenderer MakeSprite(string name, Transform parent, Sprite sprite, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        public static GameObject MakeGlass(string name, Transform parent, Vector2 boxPx, GlassStyle style, int sortingOrder)
            => Glass.Make(name, parent, boxPx, style, sortingOrder);

        public static TextMesh MakeText(string name, Transform parent, string text, float heightPx, Color color, int sortingOrder,
            bool mono = false, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = go.AddComponent<TextMesh>();
            var font = mono ? MonoFont : UiFont;
            mesh.font = font;
            mesh.text = text;
            mesh.fontSize = 64;
            mesh.characterSize = heightPx * 10f / 64f;
            mesh.anchor = anchor;
            mesh.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left
                : anchor == TextAnchor.MiddleRight ? TextAlignment.Right : TextAlignment.Center;
            mesh.color = color;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingOrder = sortingOrder;
            // TextMesh needs the font material to render.
            if (font != null) renderer.sharedMaterial = font.material;
            return mesh;
        }

        public static void SetPos(GameObject go, float x, float y)
        {
            go.transform.localPosition = new Vector3(x, y, 0f);
        }
    }

    // A glass chip (STYLE-GUIDE §7): 12 px 0.1em uppercase type on white
    // 42% -> 14% glass, radius 7, one 5 px copper pad each side outside the
    // chip. `background` is the glass tint: white for the plain chip, the
    // infection for the one lit control on a screen, an alpha under 1 for a
    // disabled chip.
    public sealed class UiButton
    {
        public GameObject Root;
        public TextMesh Label;
        public Rect Bounds;               // world coords (pixels, origin center-screen)
        public System.Action OnClick;
        public bool Enabled = true;

        public bool HitTest(Vector2 worldPoint) => Enabled && Bounds.Contains(worldPoint);

        public static UiButton Make(Transform parent, string label, Vector2 center, Vector2 sizePx,
            Color background, Color textColor, System.Action onClick, int sortingOrder = 20)
        {
            return Make(parent, label, center, sizePx, BoardTheme.Chip(background), textColor, onClick, sortingOrder,
                pads: true, padAlpha: background.a, mono: false);
        }

        public static UiButton Make(Transform parent, string label, Vector2 center, Vector2 sizePx,
            GlassStyle style, Color textColor, System.Action onClick, int sortingOrder, bool pads, float padAlpha, bool mono)
        {
            var palette = BoardPalette.Default;
            var root = new GameObject("btn:" + label);
            root.transform.SetParent(parent, false);
            Ui.SetPos(root, center.x, center.y);

            Ui.MakeGlass("glass", root.transform, sizePx, style, sortingOrder);
            if (pads)
            {
                float dot = S.Px(S.ChipPadDot);
                float gap = S.Px(S.ChipPadGap);
                var padStyle = GlassStyle.Pad(palette);
                if (padAlpha < 1f) padStyle.FillTop = padStyle.FillBottom = BoardPalette.Alpha(palette.CopperHi, padAlpha);
                var left = Ui.MakeGlass("pad:l", root.transform, new Vector2(dot, dot), padStyle, sortingOrder);
                Ui.SetPos(left, -sizePx.x / 2f - gap, 0f);
                var right = Ui.MakeGlass("pad:r", root.transform, new Vector2(dot, dot), padStyle, sortingOrder);
                Ui.SetPos(right, sizePx.x / 2f + gap, 0f);
            }

            float textPx = Mathf.Min(sizePx.y * 0.42f, S.Px(S.ChipText) * 1.4f);
            var text = Ui.MakeText("label", root.transform, label, textPx, textColor, sortingOrder + 1, mono);
            return new UiButton
            {
                Root = root,
                Label = text,
                Bounds = new Rect(center.x - sizePx.x / 2f, center.y - sizePx.y / 2f, sizePx.x, sizePx.y),
                OnClick = onClick,
            };
        }
    }
}
