// Glass chrome — STYLE-GUIDE §7-§8. One rounded box per material: the HUD
// chips, the lock badge, the tray slots, the popup panel, the copper pads.
// Everything a CSS box-shadow stack expressed in the guide is a parameter
// here, in device px: a 160 degree gradient fill (two or three stops), a top
// light, an inset ring, an inset shadow, an outer glow and a drop shadow.
//
// The quad is larger than the box so the glow and the shadow have room;
// Ui.MakeGlass sizes it. Output is premultiplied over whatever is behind.
Shader "GridInfect/Glass"
{
    Properties
    {
        _QuadPx ("Quad size (px)", Vector) = (100, 40, 0, 0)
        _BoxPx ("Box size (px)", Vector) = (80, 30, 0, 0)
        _RadiusPx ("Corner radius (px)", Float) = 7
        _FillTop ("Fill, lit corner", Color) = (1, 1, 1, 0.42)
        _FillMid ("Fill, middle stop", Color) = (1, 1, 1, 0.2)
        _FillBottom ("Fill, far corner", Color) = (1, 1, 1, 0.14)
        _MidStop ("Middle stop (0 = two stops)", Range(0, 1)) = 0
        _Border ("Inset ring", Color) = (1, 1, 1, 0.3)
        _BorderPx ("Inset ring width (px)", Float) = 1
        _TopLight ("Top light (inset 0 1px)", Color) = (1, 1, 1, 0.7)
        _Glow ("Outer glow", Color) = (0, 0, 0, 0)
        _GlowPx ("Outer glow radius (px)", Float) = 0
        _Shadow ("Drop shadow", Color) = (0, 0, 0, 0.25)
        _ShadowOffset ("Drop shadow offset (px)", Vector) = (0, -4, 0, 0)
        _ShadowBlurPx ("Drop shadow blur (px)", Float) = 10
        _InsetShadow ("Inset shadow", Color) = (0, 0, 0, 0)
        _InsetPx ("Inset shadow reach (px)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Glass"
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _QuadPx, _BoxPx;
                float _RadiusPx, _MidStop, _BorderPx, _GlowPx, _ShadowBlurPx, _InsetPx;
                float4 _FillTop, _FillMid, _FillBottom, _Border, _TopLight, _Glow, _Shadow, _ShadowOffset, _InsetShadow;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            void Over(inout float4 pm, float3 rgb, float a)
            {
                a = saturate(a);
                pm.rgb = rgb * a + pm.rgb * (1.0 - a);
                pm.a = a + pm.a * (1.0 - a);
            }

            float SdRoundBox(float2 p, float2 halfSize, float r)
            {
                float2 q = abs(p) - halfSize + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            float Inside(float d) { return 1.0 - smoothstep(-0.5, 0.5, d); }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = (input.uv - 0.5) * _QuadPx.xy;
                float2 halfBox = _BoxPx.xy * 0.5;
                float r = min(_RadiusPx, min(halfBox.x, halfBox.y));
                float d = SdRoundBox(p, halfBox, r);
                float inside = Inside(d);
                float4 pm = 0;

                // Drop shadow, outside the box only.
                if (_Shadow.a > 0.0)
                {
                    float ds = SdRoundBox(p - _ShadowOffset.xy, halfBox, r);
                    float a = _Shadow.a * (1.0 - smoothstep(-_ShadowBlurPx * 0.5, _ShadowBlurPx * 0.5, ds));
                    Over(pm, _Shadow.rgb, a * (1.0 - inside));
                }

                // Outer glow.
                if (_Glow.a > 0.0 && _GlowPx > 0.0)
                {
                    float g = saturate(1.0 - max(d, 0.0) / _GlowPx);
                    Over(pm, _Glow.rgb, _Glow.a * g * g * (1.0 - inside));
                }

                // Fill at 160 degrees.
                const float2 dir = float2(0.34202, -0.93969);
                float len = _BoxPx.x * 0.34202 + _BoxPx.y * 0.93969;
                float t = saturate(0.5 + dot(p, dir) / max(len, 1e-3));
                float4 fill = _MidStop > 0.0
                    ? (t < _MidStop ? lerp(_FillTop, _FillMid, t / _MidStop) : lerp(_FillMid, _FillBottom, (t - _MidStop) / max(1.0 - _MidStop, 1e-3)))
                    : lerp(_FillTop, _FillBottom, t);
                Over(pm, fill.rgb, fill.a * inside);

                // Inset shadow pooling in from the edge.
                if (_InsetShadow.a > 0.0 && _InsetPx > 0.0)
                {
                    float k = saturate(1.0 + d / _InsetPx);
                    Over(pm, _InsetShadow.rgb, _InsetShadow.a * k * k * inside);
                }

                // Inset ring.
                float ring = inside * smoothstep(-_BorderPx - 0.5, -_BorderPx + 0.5, d);
                Over(pm, _Border.rgb, _Border.a * ring);

                // Top light: the 1 px highlight along the top edge.
                float top = (p.y > 0.0 && abs(p.x) < halfBox.x - r) ? inside * smoothstep(-1.5, -0.5, d) : 0.0;
                Over(pm, _TopLight.rgb, _TopLight.a * top);

                return half4(pm);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
