using UnityEngine;
using S = GridInfect.Game.PresentationConfig.Style;

namespace GridInfect.Game
{
    // One glass element (STYLE-GUIDE §7-§8): the parameters of the
    // GridInfect/Glass shader in reference px. The presets are the guide's
    // CSS stacks transcribed; Ui.MakeGlass scales them to the device.
    public struct GlassStyle
    {
        public Color FillTop, FillMid, FillBottom;
        public float MidStop;          // 0 = two stops
        public float Radius;
        public Color Border;           // inset 0 0 0 BorderPx
        public float BorderPx;
        public Color TopLight;         // inset 0 1px 0
        public Color Glow;             // outer 0 0 GlowPx
        public float GlowPx;
        public Color Shadow;           // drop shadow colour and alpha
        public Vector2 ShadowOffset;
        public float ShadowBlur;
        public Color InsetShadow;      // inset 0 0 InsetPx
        public float InsetPx;

        // The room the quad needs around the box for glow and shadow.
        public float Margin => Mathf.Max(GlowPx, ShadowBlur + Mathf.Abs(ShadowOffset.y)) + 2f;

        static Color White(BoardPalette p, float a) => BoardPalette.Alpha(p.Tip, a);
        static Color Black(BoardPalette p, float a) => BoardPalette.Alpha(p.Shade, a);

        // HUD chip: white 42% -> 14%, top light 70%, ring 30%, 0 4px 10px black 25%.
        public static GlassStyle Chip(BoardPalette p) => new GlassStyle
        {
            FillTop = White(p, 0.42f), FillBottom = White(p, 0.14f), Radius = S.ChipRadius,
            Border = White(p, 0.3f), BorderPx = 1f, TopLight = White(p, 0.7f),
            Shadow = Black(p, 0.25f), ShadowOffset = new Vector2(0f, -4f), ShadowBlur = 10f,
        };

        // The copper pad either side of a chip: 5 px, glow 4 px at 90%.
        public static GlassStyle Pad(BoardPalette p) => new GlassStyle
        {
            FillTop = p.CopperHi, FillBottom = p.CopperHi, Radius = S.ChipPadDot,
            Glow = BoardPalette.Alpha(p.CopperHi, 0.9f), GlowPx = 4f,
        };

        // Lock counter: mono on black 35%, inset 1 px copperHi 35%.
        public static GlassStyle Badge(BoardPalette p) => new GlassStyle
        {
            FillTop = Black(p, 0.35f), FillBottom = Black(p, 0.35f), Radius = S.ChipRadius,
            Border = BoardPalette.Alpha(p.CopperHi, 0.35f), BorderPx = 1f,
        };

        // Tray slot: black 30%, ring white 14%, inset 24 px black 50%.
        public static GlassStyle TraySlot(BoardPalette p, bool lit) => new GlassStyle
        {
            FillTop = Black(p, 0.3f), FillBottom = Black(p, 0.3f), Radius = S.TraySlotRadius,
            Border = White(p, 0.14f), BorderPx = 1f,
            InsetShadow = Black(p, 0.5f), InsetPx = 24f,
            Glow = lit ? BoardPalette.Alpha(p.Infect, 0.45f) : Color.clear, GlowPx = lit ? 22f : 0f,
        };

        // A popup panel: chip glass at the well's radius with the tile shadow.
        public static GlassStyle Panel(BoardPalette p) => new GlassStyle
        {
            FillTop = White(p, 0.42f), FillBottom = White(p, 0.14f), Radius = S.PanelRadius,
            Border = White(p, 0.3f), BorderPx = 1f, TopLight = White(p, 0.7f),
            Shadow = Black(p, 0.38f), ShadowOffset = new Vector2(0f, -7f), ShadowBlur = 16f,
        };

        // A recessed cover over the board area: the well's own material.
        public static GlassStyle Well(BoardPalette p) => new GlassStyle
        {
            FillTop = p.WellBg, FillBottom = p.WellBg, Radius = S.WellRadius,
            Border = White(p, 0.14f), BorderPx = 1f,
            InsetShadow = Black(p, 0.5f), InsetPx = 60f,
        };
    }

    public static class Glass
    {
        const string ShaderName = "GridInfect/Glass";
        static Mesh _unitQuad;
        static Shader _shader;

        // A quad from -0.5 to 0.5 with UVs 0..1; scaled by the transform.
        public static Mesh UnitQuad
        {
            get
            {
                if (_unitQuad == null)
                {
                    _unitQuad = new Mesh { name = "unit-quad", hideFlags = HideFlags.HideAndDontSave };
                    _unitQuad.vertices = new[]
                    {
                        new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                        new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f),
                    };
                    _unitQuad.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
                    _unitQuad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                    _unitQuad.RecalculateBounds();
                }
                return _unitQuad;
            }
        }

        static Shader GlassShader
        {
            get
            {
                if (_shader == null)
                {
                    _shader = Shader.Find(ShaderName);
                    if (_shader == null) Debug.LogWarning($"[glass] shader '{ShaderName}' not found — chrome will not draw");
                }
                return _shader;
            }
        }

        // A glass box of `boxPx` (device px) centred on the returned object.
        // The quad carries the glow and shadow margin; the box is what the
        // style describes. Style dimensions are reference px and scale here.
        public static GameObject Make(string name, Transform parent, Vector2 boxPx, GlassStyle style, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var shader = GlassShader;
            if (shader == null) return go;

            float margin = S.Px(style.Margin);
            var quad = new Vector2(boxPx.x + margin * 2f, boxPx.y + margin * 2f);

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = UnitQuad;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sortingOrder = sortingOrder;
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            renderer.sharedMaterial = material;
            go.AddComponent<MaterialOwner>().Material = material;
            go.transform.localScale = new Vector3(quad.x, quad.y, 1f);

            material.SetVector("_QuadPx", new Vector4(quad.x, quad.y, 0f, 0f));
            material.SetVector("_BoxPx", new Vector4(boxPx.x, boxPx.y, 0f, 0f));
            material.SetFloat("_RadiusPx", S.Px(style.Radius));
            material.SetColor("_FillTop", style.FillTop);
            material.SetColor("_FillMid", style.FillMid);
            material.SetColor("_FillBottom", style.FillBottom);
            material.SetFloat("_MidStop", style.MidStop);
            material.SetColor("_Border", style.Border);
            material.SetFloat("_BorderPx", S.Px(style.BorderPx));
            material.SetColor("_TopLight", style.TopLight);
            material.SetColor("_Glow", style.Glow);
            material.SetFloat("_GlowPx", S.Px(style.GlowPx));
            material.SetColor("_Shadow", style.Shadow);
            material.SetVector("_ShadowOffset", new Vector4(S.Px(style.ShadowOffset.x), S.Px(style.ShadowOffset.y), 0f, 0f));
            material.SetFloat("_ShadowBlurPx", S.Px(style.ShadowBlur));
            material.SetColor("_InsetShadow", style.InsetShadow);
            material.SetFloat("_InsetPx", S.Px(style.InsetPx));
            return go;
        }
    }

    // Frees a per-instance material with the object that owns it.
    public sealed class MaterialOwner : MonoBehaviour
    {
        public Material Material;

        void OnDestroy()
        {
            if (Material != null) Object.Destroy(Material);
        }
    }
}
