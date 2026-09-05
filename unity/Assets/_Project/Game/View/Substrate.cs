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
        static TextMesh _legend;

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
                var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                material.SetVector("_ScreenPx", new Vector4(w, h, 0f, 0f));
                material.SetFloat("_RefScale", S.Scale);
                material.SetColor("_ColMask", palette.Mask);
                material.SetColor("_ColMaskHi", palette.MaskHi);
                material.SetColor("_ColMaskLo", palette.MaskLo);
                material.SetColor("_ColCopper", palette.Copper);
                material.SetColor("_ColTip", palette.Tip);
                material.SetColor("_ColShade", palette.Shade);
                renderer.sharedMaterial = material;
                quad.AddComponent<MaterialOwner>().Material = material;
            }

            // Silkscreen: mono 9 px, white 55%, studio top-left, copyright
            // bottom-left, the board legend bottom-right.
            var silk = BoardPalette.Alpha(palette.Tip, 0.55f);
            float size = S.Px(S.Silkscreen);
            var studio = Ui.MakeText("silk:studio", _root.transform, "BLOODHOUND STUDIOS", size, silk, 1, mono: true, anchor: TextAnchor.MiddleLeft);
            Ui.SetPos(studio.gameObject, -w / 2f + S.Px(44f), h / 2f - S.Px(27f));
            var copyright = Ui.MakeText("silk:copyright", _root.transform, "© 2026", size, silk, 1, mono: true, anchor: TextAnchor.MiddleLeft);
            Ui.SetPos(copyright.gameObject, -w / 2f + S.Px(44f), -h / 2f + S.Px(12f));
            _legend = Ui.MakeText("silk:legend", _root.transform, "", size, silk, 1, mono: true, anchor: TextAnchor.MiddleRight);
            Ui.SetPos(_legend.gameObject, w / 2f - S.Px(20f), -h / 2f + S.Px(12f));
            SetLevel(null);
        }

        // `GI-{LEVEL} REV B`; the menus carry the bare revision.
        public static void SetLevel(string level)
        {
            if (_legend == null) return;
            _legend.text = string.IsNullOrEmpty(level) ? "GI-REV B" : $"GI-{level} REV B";
        }
    }
}
