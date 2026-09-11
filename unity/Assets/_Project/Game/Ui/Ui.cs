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

        // `bold` is the display face's 700 weight (the bench loads Chakra
        // Petch 500 and 700). Chip labels take it: 12-13 px uppercase on
        // glass over a photographed-looking board is where thin type goes
        // first in glare. The mono face has no bold and never asks for one.
        // How wide a line draws, estimated: characters times size times the
        // face's average advance (0.66 for the display face in caps, 0.62 for
        // the mono). It is the estimate ChipSize always used, in one place.
        // TextMesh cannot measure; this is where GetPreferredValues goes when
        // the renderer can (docs/I18N.md, remaining).
        public static float EstimateWidth(string text, float heightPx, bool mono)
        {
            int longest = 0, run = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') { if (run > longest) longest = run; run = 0; }
                else run++;
            }
            if (run > longest) longest = run;
            return longest * heightPx * (mono ? 0.62f : 0.66f);
        }

        // Size the text at `heightPx`, or smaller if that would draw wider
        // than `maxWidthPx` (0 = no limit). A translation that is longer than
        // the English it replaces shrinks to its box rather than leaving it.
        public static void FitText(TextMesh mesh, string text, float heightPx, float maxWidthPx, bool mono)
        {
            if (maxWidthPx > 0f)
            {
                float width = EstimateWidth(text ?? "", heightPx, mono);
                if (width > maxWidthPx) heightPx *= maxWidthPx / width;
            }
            mesh.characterSize = heightPx * 10f / 64f;
        }

        public static TextMesh MakeText(string name, Transform parent, string text, float heightPx, Color color, int sortingOrder,
            bool mono = false, TextAnchor anchor = TextAnchor.MiddleCenter, bool bold = false, float maxWidthPx = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = go.AddComponent<TextMesh>();
            var font = mono ? MonoFont : UiFont;
            mesh.font = font;
            mesh.text = text;
            mesh.fontStyle = bold && !mono ? FontStyle.Bold : FontStyle.Normal;
            mesh.fontSize = 64;
            FitText(mesh, text, heightPx, maxWidthPx, mono);
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

        // The solved mark on a select tile: the same lit tick the calendar
        // puts on a solved day, in the same corner. Infected glass says
        // "beaten" at a glance and the tick says it again without colour
        // (R-1001), which is the whole reason the calendar has one.
        public static void MarkSolved(Transform tile, float tilePx)
        {
            var check = MakeSprite("solved", tile, BugGlyph.Check(BoardPalette.Default,
                Mathf.RoundToInt(tilePx * 0.36f)), 24);
            check.transform.localPosition = new Vector3(tilePx * 0.27f, -tilePx * 0.27f, 0f);
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
        public SpriteRenderer Icon;       // an icon chip's mark, when it has one instead of a label
        public Rect Bounds;               // world coords (pixels, origin center-screen): the drawn box
        public System.Action OnClick;
        public bool Enabled = true;
        // Seconds no chip answers after this one's handler returns. The
        // default covers a double-tap; a control whose handler does real
        // work (the hint) asks for more.
        public float Cooldown = PresentationConfig.ButtonDebounce;

        // What the finger gets: the drawn box grown to at least MinTouch
        // (44 reference px) on each axis, centred. A 29 px pager chip or a
        // 31 px HUD chip stays the size the guide drew it and still takes a
        // thumb. Neighbours can overlap in the slop; GameApp's last-wins
        // pick settles a press in the seam the way z-order would.
        public Rect HitBounds
        {
            get
            {
                float min = S.Px(S.MinTouch);
                float w = Mathf.Max(Bounds.width, min), h = Mathf.Max(Bounds.height, min);
                return new Rect(Bounds.x + Bounds.width / 2f - w / 2f, Bounds.y + Bounds.height / 2f - h / 2f, w, h);
            }
        }

        public bool HitTest(Vector2 worldPoint) => Enabled && HitBounds.Contains(worldPoint);

        // Dim is for a control that cannot act right now (a pager at its end,
        // TODAY while today is showing). It is the one place type is allowed
        // under the contrast floor, because it is telling you not to press.
        public void SetDim(bool dim)
        {
            if (Label != null) Label.color = dim ? BoardTheme.TextDisabled : BoardTheme.Text;
            if (Icon != null) Icon.color = dim ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
        }

        // A chip carrying a drawn mark instead of a word: the gear, the help
        // mark, a pager chevron. A glyph is a shape that the OS font cannot
        // take away, and it is drawn at a stroke that survives glare, which
        // a 12 px "?" or a "◀" from whatever face is installed did not.
        public static UiButton MakeIcon(Transform parent, string name, Sprite icon, Vector2 center, Vector2 sizePx,
            System.Action onClick, int sortingOrder = 20)
        {
            var button = Make(parent, "", center, sizePx, BoardTheme.ButtonBg, BoardTheme.Text, onClick, sortingOrder);
            button.Root.name = "btn:" + name;
            button.Icon = Ui.MakeSprite("icon", button.Root.transform, icon, sortingOrder + 2);
            return button;
        }

        // The mark inside an icon chip: 72% of the chip's short side.
        public static int IconPx(Vector2 chipSize) => Mathf.RoundToInt(Mathf.Min(chipSize.x, chipSize.y) * 0.72f);

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
            // The label fits inside the chip's padding, or shrinks until it does.
            var text = Ui.MakeText("label", root.transform, label, textPx, textColor, sortingOrder + 1, mono, bold: true,
                maxWidthPx: sizePx.x - S.Px(S.ChipPadX * 2f));
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
