using TMPro;
using UnityEngine;
using S = GridInfect.Game.PresentationConfig.Style;

namespace GridInfect.Game
{
    // Procedural primitives: one white texture, glass panels, OS fonts, zero
    // assets. 1 world unit = 1 screen pixel, origin at screen center.
    //
    // Text is TextMeshPro's worldspace component over a font asset built at
    // runtime from the same OS faces FindFont always chose, so the swap from
    // TextMesh changed the renderer and nothing about which face is used.
    // What TMP brings: a signed-distance face that stays crisp at every size,
    // measurement (GetPreferredValues), wrapping and auto-size when asked,
    // and a fallback chain and bidi flag for the languages that need them
    // (docs/I18N.md).
    public static class Ui
    {
        static Sprite _whiteSprite;
        static Font _font;
        static Font _bold;
        static Font _mono;
        static TMP_FontAsset _fontAsset;
        static TMP_FontAsset _boldAsset;
        static TMP_FontAsset _monoAsset;

        // TextMesh drew an em `heightPx` units tall from fontSize 64 and a
        // characterSize of heightPx * 10 / 64; TMP's worldspace component
        // draws an em of fontSize / 10 units. So fontSize = heightPx * 10.
        // If every piece of text in the game comes up ten times too big or
        // too small, this is the number that is wrong, and nothing else is.
        const float TmpPointsPerPx = 10f;

        // The SDF is sampled at this point size; glyphs are added to the
        // atlas on demand, so a language's charset costs nothing until it
        // is drawn.
        const int SamplingPointSize = 90;

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

        // The guide's display face is Chakra Petch at 500 and 700 and its
        // mono face Share Tech Mono (STYLE-GUIDE §7). All three ship under
        // Resources/Fonts (OFL, licences beside them), so the face is a
        // given rather than probed for; the OS chain behind it is for a
        // build where the resource failed to import, and logs when used.
        static Font Vendored(string resource)
        {
            Font font = TryVendored(resource);
            if (font == null) Debug.LogWarning($"[text] Resources/Fonts/{resource} did not load; an OS face stands in");
            return font;
        }

        static Font TryVendored(string resource)
        {
            try { return Resources.Load<Font>("Fonts/" + resource); }
            catch (System.Exception) { return null; }
        }

        // The fallback faces (tools/subset_fonts.py): Noto, cut to the code
        // points the string files use that the design faces do not carry —
        // kana and kanji, Cyrillic, the odd Latin letter. Each design face
        // falls through to these, in this order, for a glyph it lacks. A
        // fallback that is not there is not an error: the tool has not been
        // run for that script yet, and the OS face is what draws.
        static readonly string[] FallbackFiles = { "Fallback-Sans", "Fallback-JP" };
        static System.Collections.Generic.List<TMP_FontAsset> _fallbacks;

        static System.Collections.Generic.List<TMP_FontAsset> Fallbacks
        {
            get
            {
                if (_fallbacks == null)
                {
                    _fallbacks = new System.Collections.Generic.List<TMP_FontAsset>();
                    foreach (string file in FallbackFiles)
                    {
                        var asset = MakeFontAsset(TryVendored(file));
                        if (asset != null) _fallbacks.Add(asset);
                    }
                }
                return _fallbacks;
            }
        }

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
                    _font = Vendored("ChakraPetch-Medium")
                        ?? FindFont(new[] { "Chakra Petch Medium", "Chakra Petch", "Arial", "Helvetica", "Segoe UI", "Liberation Sans", "DejaVu Sans", "Roboto" });
                }
                return _font;
            }
        }

        public static Font UiBoldFont
        {
            get
            {
                if (_bold == null) _bold = Vendored("ChakraPetch-Bold") ?? UiFont;
                return _bold;
            }
        }

        public static Font MonoFont
        {
            get
            {
                if (_mono == null)
                {
                    _mono = Vendored("ShareTechMono-Regular")
                        ?? FindFont(new[] { "Share Tech Mono", "Menlo", "Consolas", "Courier New", "Liberation Mono", "DejaVu Sans Mono", "Roboto Mono" });
                    if (_mono == null) _mono = UiFont;
                }
                return _mono;
            }
        }

        // A dynamic SDF font asset over a Font: glyphs rasterised into the
        // atlas as they are first drawn. Null if TMP cannot read the face
        // (it goes through FreeType from the font's file), in which case
        // the other face stands in rather than nothing drawing.
        static TMP_FontAsset MakeFontAsset(Font font)
        {
            if (font == null) return null;
            try
            {
                var asset = TMP_FontAsset.CreateFontAsset(font, SamplingPointSize, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                if (asset != null) asset.hideFlags = HideFlags.HideAndDontSave;
                return asset;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[text] no font asset for {font.name}: {e.Message}");
                return null;
            }
        }

        static TMP_FontAsset WithFallbacks(TMP_FontAsset asset)
        {
            if (asset != null) asset.fallbackFontAssetTable = Fallbacks;
            return asset;
        }

        public static TMP_FontAsset UiFontAsset
        {
            get
            {
                if (_fontAsset == null) _fontAsset = WithFallbacks(MakeFontAsset(UiFont) ?? MakeFontAsset(MonoFont));
                return _fontAsset;
            }
        }

        // The 700 weight is its own face, not TMP's synthetic bold over the
        // 500: the synthetic one thickens strokes without the letterforms
        // that were drawn for the weight. It falls back to that synthetic
        // bold only when the bold face itself did not load.
        public static TMP_FontAsset UiBoldFontAsset
        {
            get
            {
                if (_boldAsset == null) _boldAsset = UiBoldFont == UiFont ? null : WithFallbacks(MakeFontAsset(UiBoldFont));
                return _boldAsset ?? UiFontAsset;
            }
        }

        public static TMP_FontAsset MonoFontAsset
        {
            get
            {
                if (_monoAsset == null) _monoAsset = WithFallbacks(MakeFontAsset(MonoFont)) ?? UiFontAsset;
                return _monoAsset;
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
        // How wide a line would draw, estimated: characters times size times
        // the face's average advance. Only the fallback now, for a text with
        // no font asset behind it; MeasureWidth is the real thing.
        static float EstimateWidth(string text, float heightPx, bool mono)
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

        // One hidden text, kept for measuring: how wide `text` draws in a
        // face at a size, before the object that will show it exists. It is
        // what lets a chip be sized to its label and a badge to its widest
        // reading, in whatever language, instead of to a character count.
        static TextMeshPro _measure;

        public static float MeasureWidth(string text, float heightPx, bool mono, bool bold)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            if (_measure == null)
            {
                var go = new GameObject("measure");
                go.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(go);
                _measure = go.AddComponent<TextMeshPro>();
                _measure.richText = false;
                _measure.textWrappingMode = TextWrappingModes.NoWrap;
                _measure.overflowMode = TextOverflowModes.Overflow;
                _measure.color = Color.clear;
                go.transform.localPosition = new Vector3(0f, 1e6f, 0f);
            }
            bool realBold = bold && !mono && UiBoldFontAsset != UiFontAsset;
            var font = mono ? MonoFontAsset : realBold ? UiBoldFontAsset : UiFontAsset;
            if (font == null) return EstimateWidth(text, heightPx, mono);
            _measure.font = font;
            _measure.fontStyle = bold && !mono && !realBold ? FontStyles.Bold : FontStyles.Normal;
            _measure.fontSize = heightPx * TmpPointsPerPx;
            return _measure.GetPreferredValues(text).x;
        }

        // Below this the type is not legible over the board (STYLE-GUIDE 7,
        // PresentationConfig.Style), so a text stops shrinking here and is
        // allowed to run over its box instead: clipped is a layout bug you
        // can see, unreadable is one you cannot.
        static float MinTextPx => S.Px(11f);

        // Size the text at `heightPx`, or smaller if that would draw wider
        // than `maxWidthPx` (0 = no limit), down to the legibility floor.
        public static void FitText(TMP_Text mesh, string text, float heightPx, float maxWidthPx, bool mono)
        {
            mesh.fontSize = heightPx * TmpPointsPerPx;
            if (maxWidthPx <= 0f || string.IsNullOrEmpty(text)) return;
            float width = mesh.font != null ? mesh.GetPreferredValues(text).x : EstimateWidth(text, heightPx, mono);
            if (width > maxWidthPx)
            {
                float fitted = Mathf.Max(MinTextPx, heightPx * maxWidthPx / width);
                mesh.fontSize = fitted * TmpPointsPerPx;
            }
        }

        // A chip's label size from its box: 42% of the height, capped at
        // 1.4x the chip type (STYLE-GUIDE 7). One definition, so the box a
        // label is measured for is the box it is drawn in.
        public static float LabelPx(Vector2 chipSize) =>
            Mathf.Min(chipSize.y * 0.42f, S.Px(S.ChipText) * 1.4f);

        // A chip's box from its label: 12 px type, 8 x 14 padding (§7), the
        // width measured in the face the label draws in.
        public static Vector2 ChipBox(string label, bool mono = false, bool bold = true)
        {
            float height = S.Px(S.ChipPadY * 2f + S.ChipText * 1.25f);
            float textPx = LabelPx(new Vector2(0f, height));
            return new Vector2(S.Px(S.ChipPadX * 2f) + MeasureWidth(label, textPx, mono, bold), height);
        }

        // A badge's box from its widest reading: mono 13 px on 6 x 12 padding.
        public static Vector2 BadgeBox(params string[] readings)
        {
            float height = S.Px(S.BadgePadY * 2f + S.BadgeText * 1.25f);
            float textPx = LabelPx(new Vector2(0f, height));
            float widest = 0f;
            foreach (string r in readings) widest = Mathf.Max(widest, MeasureWidth(r, textPx, mono: true, bold: true));
            return new Vector2(S.Px(S.BadgePadX * 2f) + widest, height);
        }

        public static TMP_Text MakeText(string name, Transform parent, string text, float heightPx, Color color, int sortingOrder,
            bool mono = false, TextAnchor anchor = TextAnchor.MiddleCenter, bool bold = false, float maxWidthPx = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = go.AddComponent<TextMeshPro>();
            bool realBold = bold && !mono && UiBoldFontAsset != UiFontAsset;
            mesh.font = mono ? MonoFontAsset : realBold ? UiBoldFontAsset : UiFontAsset;
            // Strings come from translators, not markup: a '<' in a label is
            // a '<'. TextMesh parsed tags by default; this does not.
            mesh.richText = false;
            mesh.textWrappingMode = TextWrappingModes.NoWrap;   // a hard break is authored (\n), never found
            mesh.overflowMode = TextOverflowModes.Overflow;
            // Not TMP's RTL flag, even for a right-to-left tag: that flag
            // reverses glyph order, which a real RTL language needs and the
            // mirrored pseudolocale has already done to itself (docs/I18N.md).
            // It is set with the first shaped language, not before.
            mesh.isRightToLeftText = false;
            mesh.fontStyle = bold && !mono && !realBold ? FontStyles.Bold : FontStyles.Normal;
            mesh.text = text;
            FitText(mesh, text, heightPx, maxWidthPx, mono);
            // The rect is a point at the object's origin; the pivot and the
            // alignment together put the text where TextMesh's anchor did.
            var rect = mesh.rectTransform;
            rect.sizeDelta = Vector2.zero;
            if (anchor == TextAnchor.MiddleLeft)
            {
                rect.pivot = new Vector2(0f, 0.5f);
                mesh.alignment = TextAlignmentOptions.Left;
            }
            else if (anchor == TextAnchor.MiddleRight)
            {
                rect.pivot = new Vector2(1f, 0.5f);
                mesh.alignment = TextAlignmentOptions.Right;
            }
            else
            {
                rect.pivot = new Vector2(0.5f, 0.5f);
                mesh.alignment = TextAlignmentOptions.Center;
            }
            mesh.color = color;
            mesh.sortingOrder = sortingOrder;
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
        public TMP_Text Label;
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

            float textPx = Ui.LabelPx(sizePx);
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
