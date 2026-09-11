using TMPro;
using UnityEngine;
using S = GridInfect.Game.PresentationConfig.Style;

namespace GridInfect.Game
{
    // The printed circuit board behind every screen (STYLE-GUIDE §3): one
    // full-screen quad on the substrate shader, plus the three lines of
    // silkscreen. Created once and kept across screens; the board screen
    // sets the level in the bottom-right legend.
    public static class Substrate
    {
        const string ShaderName = "GridInfect/Substrate";

        static GameObject _root;
        static TMP_Text _legend;
        static TMP_Text _copyright;
        static Material _material;

        public static void Ensure(BoardPalette palette)
        {
            if (_root != null) return;

            _root = new GameObject("substrate");
            Object.DontDestroyOnLoad(_root);

            float w = UnityEngine.Screen.width, h = UnityEngine.Screen.height;

            var quad = new GameObject("mask");
            quad.transform.SetParent(_root.transform, false);
            quad.transform.localPosition = new Vector3(0f, 0f, 1f);   // behind the board (0.5) and the sprites (0)
            quad.transform.localScale = new Vector3(w, h, 1f);
            quad.AddComponent<MeshFilter>().sharedMesh = Glass.UnitQuad;
            var renderer = quad.AddComponent<MeshRenderer>();
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[substrate] shader '{ShaderName}' not found — the background will be the camera clear");
            }
            else
            {
                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                _material.SetVector("_ScreenPx", new Vector4(w, h, 0f, 0f));
                _material.SetFloat("_RefScale", S.Scale);
                renderer.sharedMaterial = _material;
                quad.AddComponent<MaterialOwner>().Material = _material;
            }

            // Silkscreen: mono 9 px, white 55%, copyright bottom-left, the
            // board legend bottom-right. The studio mark belongs to the title
            // card, not to every screen, so the top-left corner stays clear.
            var silk = BoardPalette.Alpha(palette.Tip, 0.55f);
            float size = S.Px(S.Silkscreen);
            _copyright = Ui.MakeText("silk:copyright", _root.transform, "© 2026", size, silk, 1, mono: true, anchor: TextAnchor.MiddleLeft);
            Ui.SetPos(_copyright.gameObject, -w / 2f + S.Px(44f), -h / 2f + S.Px(12f));
            _legend = Ui.MakeText("silk:legend", _root.transform, "", size, silk, 1, mono: true, anchor: TextAnchor.MiddleRight);
            Ui.SetPos(_legend.gameObject, w / 2f - S.Px(20f), -h / 2f + S.Px(12f));
            Restyle(palette);
            SetLevel(null);
        }

        // A skin change: the same colours again, without rebuilding the quad.
        // Destroying and remaking it would leave two opaque full-screen
        // substrates fighting for a frame, since Destroy lands at the end of
        // one.
        public static void Restyle(BoardPalette palette)
        {
            if (_material != null)
            {
                _material.SetColor("_ColMask", palette.Mask);
                _material.SetColor("_ColMaskHi", palette.MaskHi);
                _material.SetColor("_ColMaskLo", palette.MaskLo);
                _material.SetColor("_ColCopper", palette.Copper);
                _material.SetColor("_ColTip", palette.Tip);
                _material.SetColor("_ColShade", palette.Shade);
            }
            var silk = BoardPalette.Alpha(palette.Tip, 0.55f);
            if (_copyright != null) _copyright.color = silk;
            if (_legend != null) _legend.color = silk;
        }

        // The board's own part number, bottom right: `GI-06`, `GI-DAILY`,
        // `GI-T01`. The designation is real, so it is there; the build the
        // player is actually running is in SETTINGS, where build data
        // belongs. No board, no part number: the corner stays clear.
        public static void SetLevel(string level)
        {
            if (_legend == null) return;
            _legend.text = string.IsNullOrEmpty(level) ? "" : $"GI-{level}";
        }
    }
}
