// The substrate — STYLE-GUIDE §3. One full-screen quad behind every screen:
// the solder mask, its two grids, the sheen, the tone-on-tone margin traces,
// the corner mounting holes and the vignette. The silkscreen type is TextMesh
// on top (Substrate.cs); everything else is here.
//
// Positions come in two frames. Grids, strokes and radii are measured in the
// guide's reference px (390 wide) via _RefScale so they keep their weight on
// any device; the trace routing and the holes are placed in normalised screen
// space so they stay in the margins whatever the aspect. Trace routing is
// placeholder art per the guide; production routes per level.
Shader "GridInfect/Substrate"
{
    Properties
    {
        _ScreenPx ("Screen size (px)", Vector) = (1080, 2340, 0, 0)
        _RefScale ("Device px per style-guide px", Float) = 2.77
        _ColMask ("Mask", Color) = (0, 0, 0, 1)
        _ColMaskHi ("Mask highlight", Color) = (0, 0, 0, 1)
        _ColMaskLo ("Mask shadow", Color) = (0, 0, 0, 1)
        _ColCopper ("Copper", Color) = (0, 0, 0, 1)
        _ColTip ("White", Color) = (1, 1, 1, 1)
        _ColShade ("Black", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Substrate"
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ScreenPx;
                float _RefScale;
                float4 _ColMask, _ColMaskHi, _ColMaskLo, _ColCopper, _ColTip, _ColShade;
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

            void Over(inout float3 col, float3 rgb, float a)
            {
                col = lerp(col, rgb, saturate(a));
            }

            float SegDistance(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float2 ap = p - a;
                float h = saturate(dot(ap, ab) / max(dot(ab, ab), 1e-6));
                return length(ap - ab * h);
            }

            float Line(float d, float w) { return 1.0 - smoothstep(w * 0.5 - 0.5, w * 0.5 + 0.5, d); }

            // One trace: 3 px black 14% with a 1 px white 10% highlight offset
            // up by 1.5 px, 45 degree bends, a via dot at each end.
            void Trace(inout float3 col, float2 p, float2 a, float2 b, float s)
            {
                float d = SegDistance(p, a, b);
                Over(col, _ColShade.rgb, 0.14 * Line(d, 3.0 * s));
                float2 lift = float2(0.0, 1.5 * s);
                float dh = SegDistance(p, a + lift, b + lift);
                Over(col, _ColTip.rgb, 0.10 * Line(dh, s));
            }

            void TraceEnd(inout float3 col, float2 p, float2 e, float s)
            {
                float d = length(p - e);
                Over(col, _ColShade.rgb, 0.18 * (1.0 - smoothstep(4.0 * s - 0.5, 4.0 * s + 0.5, d)));
                Over(col, _ColTip.rgb, 0.35 * (1.0 - smoothstep(1.6 * s - 0.5, 1.6 * s + 0.5, d)));
            }

            // Guide coordinates (390 x 844, y down) -> device px, y up, with the
            // routing stretched to this screen's margins.
            float2 Route(float2 g)
            {
                return float2(g.x / 390.0, 1.0 - g.y / 844.0) * _ScreenPx.xy;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;                       // y up
                float2 px = uv * _ScreenPx.xy;
                float s = _RefScale;
                float2 pr = px / s;                          // reference px, y up

                // 1. Mask: maskHi -> mask (70%) -> maskLo at 160 degrees.
                const float2 dir = float2(0.34202, -0.93969);
                float len = _ScreenPx.x * 0.34202 + _ScreenPx.y * 0.93969;
                float t = saturate(0.5 + dot(px - _ScreenPx.xy * 0.5, dir) / len);
                float3 col = t < 0.7 ? lerp(_ColMaskHi.rgb, _ColMask.rgb, t / 0.7)
                                     : lerp(_ColMask.rgb, _ColMaskLo.rgb, (t - 0.7) / 0.3);

                // 2. 24 px grid, white 7%.  3. 12 px grid, black 5%.
                float2 g24 = abs(frac(pr / 24.0 + 0.5) - 0.5) * 24.0 * s;
                Over(col, _ColTip.rgb, 0.07 * Line(min(g24.x, g24.y), 1.0));
                float2 g12 = abs(frac(pr / 12.0 + 0.5) - 0.5) * 12.0 * s;
                Over(col, _ColShade.rgb, 0.05 * Line(min(g12.x, g12.y), 1.0));

                // 4. Sheen: white 18% at (50%, 12%) from the top, r 50%.
                float2 sheenC = float2(0.5, 0.88);
                float sheen = 1.0 - saturate(length((uv - sheenC) / 0.5));
                Over(col, _ColTip.rgb, 0.18 * sheen);

                // 5. Traces, margins only.
                Trace(col, px, Route(float2(12, 140)), Route(float2(12, 300)), s);
                Trace(col, px, Route(float2(12, 300)), Route(float2(26, 314)), s);
                Trace(col, px, Route(float2(26, 314)), Route(float2(26, 700)), s);
                Trace(col, px, Route(float2(378, 140)), Route(float2(378, 240)), s);
                Trace(col, px, Route(float2(378, 240)), Route(float2(364, 254)), s);
                Trace(col, px, Route(float2(364, 254)), Route(float2(364, 720)), s);
                Trace(col, px, Route(float2(60, 808)), Route(float2(150, 808)), s);
                Trace(col, px, Route(float2(150, 808)), Route(float2(164, 822)), s);
                Trace(col, px, Route(float2(164, 822)), Route(float2(240, 822)), s);
                Trace(col, px, Route(float2(230, 20)), Route(float2(300, 20)), s);
                Trace(col, px, Route(float2(300, 20)), Route(float2(314, 34)), s);
                Trace(col, px, Route(float2(314, 34)), Route(float2(350, 34)), s);
                TraceEnd(col, px, Route(float2(12, 140)), s);
                TraceEnd(col, px, Route(float2(26, 700)), s);
                TraceEnd(col, px, Route(float2(378, 140)), s);
                TraceEnd(col, px, Route(float2(364, 720)), s);
                TraceEnd(col, px, Route(float2(60, 808)), s);
                TraceEnd(col, px, Route(float2(240, 822)), s);
                TraceEnd(col, px, Route(float2(230, 20)), s);
                TraceEnd(col, px, Route(float2(350, 34)), s);

                // 6. Mounting holes: four corners, r9 copper 70% over r5 dark.
                float2 corner = min(px, _ScreenPx.xy - px);
                float dh = length(corner - 22.0 * s);
                Over(col, _ColCopper.rgb, 0.7 * (1.0 - smoothstep(9.0 * s - 0.5, 9.0 * s + 0.5, dh)));
                Over(col, lerp(_ColMaskLo.rgb, _ColShade.rgb, 0.6), 1.0 - smoothstep(5.0 * s - 0.5, 5.0 * s + 0.5, dh));

                // 8. Vignette: black 0% to 28% from r 60% to r 75%.
                float v = length((uv - 0.5) / 0.75);
                Over(col, _ColShade.rgb, 0.28 * saturate((v - 0.6) / 0.4));

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
