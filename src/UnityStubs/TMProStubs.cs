// Compile-time stubs for the TextMeshPro APIs the adapter uses (see
// UnityEngineStubs.cs). Inert; never shipped.
#pragma warning disable IDE0060, CA1822
using UnityEngine;

namespace UnityEngine.TextCore.LowLevel
{
    public enum GlyphRenderMode { SDFAA }
}

namespace TMPro
{
    public enum TextAlignmentOptions { Left, Center, Right }
    public enum FontStyles { Normal, Bold }
    public enum TextWrappingModes { NoWrap, Normal }
    public enum TextOverflowModes { Overflow }
    public enum AtlasPopulationMode { Static, Dynamic }

    public class TMP_FontAsset : Object
    {
        public System.Collections.Generic.List<TMP_FontAsset> fallbackFontAssetTable { get; set; }
        public static TMP_FontAsset CreateFontAsset(Font font, int samplingPointSize, int atlasPadding,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode renderMode, int atlasWidth, int atlasHeight,
            AtlasPopulationMode atlasPopulationMode, bool enableMultiAtlasSupport) => null;
    }

    public class TMP_Text : Component
    {
        public virtual string text { get; set; }
        public float fontSize { get; set; }
        public TMP_FontAsset font { get; set; }
        public FontStyles fontStyle { get; set; }
        public TextAlignmentOptions alignment { get; set; }
        public Color color { get; set; }
        public bool richText { get; set; }
        public bool isRightToLeftText { get; set; }
        public TextWrappingModes textWrappingMode { get; set; }
        public TextOverflowModes overflowMode { get; set; }
        public bool enableAutoSizing { get; set; }
        public float fontSizeMin { get; set; }
        public float fontSizeMax { get; set; }
        public RectTransform rectTransform => null;
        public Vector2 GetPreferredValues(string text) => default;
    }

    public class TextMeshPro : TMP_Text
    {
        public int sortingOrder { get; set; }
    }
}
