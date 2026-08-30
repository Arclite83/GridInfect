using UnityEngine;

namespace GridInfect.Game
{
    // Procedural primitives: one white texture and a built-in font, zero assets.
    // 1 world unit = 1 screen pixel, origin at screen center.
    public static class Ui
    {
        static Sprite _whiteSprite;
        static Font _font;

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

        public static Font UiFont
        {
            get
            {
                if (_font == null)
                {
                    // Unity 2022.2+ ships LegacySans; older editors ship Arial.
                    try { _font = Resources.GetBuiltinResource<Font>("LegacySans.ttf"); }
                    catch (System.Exception) { }
                    if (_font == null)
                    {
                        try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                        catch (System.Exception) { }
                    }
                    if (_font == null)
                    {
                        _font = Font.CreateDynamicFontFromOSFont("Helvetica", 64);
                    }
                }
                return _font;
            }
        }

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

        public static TextMesh MakeText(string name, Transform parent, string text, float heightPx, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = go.AddComponent<TextMesh>();
            mesh.font = UiFont;
            mesh.text = text;
            mesh.fontSize = 64;
            mesh.characterSize = heightPx * 10f / 64f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingOrder = sortingOrder;
            // TextMesh needs the font material to render.
            renderer.sharedMaterial = UiFont.material;
            return mesh;
        }

        public static void SetPos(GameObject go, float x, float y)
        {
            go.transform.localPosition = new Vector3(x, y, 0f);
        }
    }

    public sealed class UiButton
    {
        public GameObject Root;
        public Rect Bounds;               // world coords (pixels, origin center-screen)
        public System.Action OnClick;
        public bool Enabled = true;

        public bool HitTest(Vector2 worldPoint) => Enabled && Bounds.Contains(worldPoint);

        public static UiButton Make(Transform parent, string label, Vector2 center, Vector2 sizePx,
            Color background, Color textColor, System.Action onClick, int sortingOrder = 20)
        {
            var root = Ui.MakeRect("btn:" + label, parent, sizePx, background, sortingOrder);
            Ui.SetPos(root, center.x, center.y);
            var text = Ui.MakeText("label", root.transform, label, sizePx.y * 0.42f, textColor, sortingOrder + 1);
            // Text is a child of a scaled rect; neutralize the parent scale.
            text.transform.localScale = new Vector3(1f / sizePx.x, 1f / sizePx.y, 1f);
            return new UiButton
            {
                Root = root,
                Bounds = new Rect(center.x - sizePx.x / 2f, center.y - sizePx.y / 2f, sizePx.x, sizePx.y),
                OnClick = onClick,
            };
        }
    }
}
