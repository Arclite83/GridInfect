// Grid Infect board shader — STYLE-GUIDE §4-§5 on the infection machinery of
// docs/infection-vfx-spec.md.
//
// The whole board is one quad, one material, one draw call: the recessed well,
// every tile, the infection bleed, the beam between cells and the sparks. Cell
// state lives in _StateTex (point-filtered RGBAFloat, one texel per cell); the
// simulation writes it, this shader only reads it. Nothing here gates input
// and nothing writes back, so a placement landing mid-bleed just changes
// texels and the cells already in flight keep running off their own start
// times.
//
// The material is backlit frosted glass: tiles are translucent white over the
// dark well, and the infection is light inside the glass. The quad is drawn
// transparent over the substrate, premultiplied, so a dormant tile really is
// the well showing through 34% white.
//
// _StateTex channels (BoardStateTexture.cs owns the writes):
//   R  cell value, the game's own wire vocabulary (0 void, 1 active, 2 wall,
//      3 repel switch, 4 infected, 5 reset trap, 6 forbidden)
//   G  transition start time, seconds on the board clock
//   B  entry direction packed as (dr + 1) * 3 + (dc + 1), grid deltas, 4 = seed
//   A  transition kind: 0 none, 1 infecting, 2 receding, 3 conflict flash,
//      4 pending (the drop preview)
Shader "GridInfect/Board"
{
    Properties
    {
        [NoScaleOffset] _StateTex ("Cell state (RGBAFloat)", 2D) = "black" {}
        [NoScaleOffset] _NoiseTex ("Blot noise", 2D) = "gray" {}

        // Locked parameters (spec table). Blocks/bias/hop never move; trace
        // and bleed are the two tunables.
        _Cols ("Columns", Float) = 6
        _Rows ("Rows", Float) = 11
        _Blocks ("Blocks per cell", Float) = 16
        _Bias ("Direction bias", Range(0, 1)) = 0.3
        _TraceDur ("Trace pulse (s)", Float) = 0.09
        _BleedDur ("Bleed dissolve (s)", Float) = 0.26
        _GlowHold ("Glow hold (s)", Float) = 0.15
        _GlowFade ("Glow fade (s)", Float) = 0.30
        _BoardTime ("Board clock (s)", Float) = 0

        // Layout, in screen pixels (1 world unit = 1 screen pixel here). The
        // quad is larger than the lattice: it carries the well and its ring.
        _QuadPx ("Quad size (px)", Vector) = (400, 700, 0, 0)
        _LatticeOrigin ("Lattice origin in quad (px)", Vector) = (0, 0, 0, 0)
        _PitchPx ("Cell pitch (px)", Float) = 59
        _CellFrac ("Cell size / pitch", Float) = 0.915254
        _RefScale ("Device px per style-guide px", Float) = 1
        _TileRadiusPx ("Tile radius (px)", Float) = 6
        _WellPadPx ("Well padding (px)", Float) = 14
        _WellRadiusPx ("Well radius (px)", Float) = 12
        _GlowPx ("Infected tile glow (px)", Float) = 26
        _TracePx ("Trace width (px)", Float) = 2.5
        _BlotAmp ("Blot ripple on the pool front", Range(0, 1)) = 0.18

        _HotEmission ("Hot emission (HDR gain)", Float) = 2.2
        _RestEmission ("Resting emission (HDR gain)", Float) = 1.15
        _ConflictDur ("Conflict flash (s)", Float) = 0.5
        _PreviewFade ("Pending trace fade-in (s)", Float) = 0.12

        // Juice layers — each an independent toggle on the board controller.
        _ArrivalPulse ("Juice: arrival pulse", Float) = 1
        _PulseGain ("Arrival pulse gain", Float) = 1.4
        _PulseDur ("Arrival pulse (s)", Float) = 0.06
        _EdgeSparks ("Juice: edge sparks", Float) = 1
        _SparkLife ("Spark life (s)", Float) = 0.2
        _TraceDim ("Juice: trace dim", Float) = 1
        _TraceDimLevel ("Trace dim level", Range(0, 1)) = 0.3
        _GhostTrail ("Juice: ghost trail", Float) = 0
        _GhostTrailDur ("Ghost trail (s)", Float) = 0.2

        // Palette. Every colour arrives from BoardPalette; the defaults here
        // are only what the inspector shows on a bare material.
        _ColTip ("Tip / highlight white", Color) = (1, 1, 1, 1)
        _ColShade ("Shade black", Color) = (0, 0, 0, 1)
        _ColWellBg ("Well fill (with alpha)", Color) = (0, 0, 0, 0.36)
        _ColCopper ("Copper", Color) = (0, 0, 0, 1)
        _ColCopperHi ("Copper highlight", Color) = (0, 0, 0, 1)
        _ColCopperLo ("Copper shadow", Color) = (0, 0, 0, 1)
        _ColInfect ("Infection", Color) = (0, 0, 0, 1)
        _ColInfectHi ("Infection highlight", Color) = (0, 0, 0, 1)
        _ColInfectLo ("Infection shadow", Color) = (0, 0, 0, 1)
        _ColInfectGlow ("Infection glow (with alpha)", Color) = (0, 0, 0, 0.55)
        _ColGlyphEdge ("Glyph edge (shape glyphs)", Color) = (0, 0, 0, 1)
        _ColSwitch ("Repel switch tint", Color) = (0, 0, 0, 1)
        _ColTrap ("Reset trap tint", Color) = (0, 0, 0, 1)
        _ColConflict ("Conflict overprint", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "BoardGlass"
            // No LightMode tag: Unity files the pass under SRPDefaultUnlit,
            // which both the Forward and the 2D renderer draw.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_StateTex);  SAMPLER(sampler_StateTex);
            TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float _Cols, _Rows, _Blocks, _Bias;
                float _TraceDur, _BleedDur, _GlowHold, _GlowFade, _BoardTime;
                float4 _QuadPx, _LatticeOrigin;
                float _PitchPx, _CellFrac, _RefScale, _TileRadiusPx, _WellPadPx, _WellRadiusPx, _GlowPx, _TracePx, _BlotAmp;
                float _HotEmission, _RestEmission, _ConflictDur, _PreviewFade;
                float _ArrivalPulse, _PulseGain, _PulseDur;
                float _EdgeSparks, _SparkLife;
                float _TraceDim, _TraceDimLevel;
                float _GhostTrail, _GhostTrailDur;
                float4 _ColTip, _ColShade, _ColWellBg, _ColCopper, _ColCopperHi, _ColCopperLo;
                float4 _ColInfect, _ColInfectHi, _ColInfectLo, _ColInfectGlow, _ColGlyphEdge;
                float4 _ColSwitch, _ColTrap, _ColConflict;
            CBUFFER_END

            #define CELL_VOID      0
            #define CELL_ACTIVE    1
            #define CELL_WALL      2
            #define CELL_SWITCH    3
            #define CELL_INFECT    4
            #define CELL_TRAP      5
            #define CELL_FORBIDDEN 6

            #define TR_NONE      0
            #define TR_INFECT    1
            #define TR_RECEDE    2
            #define TR_CONFLICT  3
            #define TR_PREVIEW   4

            #define SPARK_COUNT  8

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            // ---- compositing -----------------------------------------------------
            // Premultiplied "over": layers are painted in order, bottom first.

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

            // Anti-aliased coverage of the inside of a distance field.
            float Inside(float d) { return 1.0 - smoothstep(-0.5, 0.5, d); }

            // A ring `w` px wide just inside the edge (CSS inset 0 0 0 w).
            float InsetRing(float d, float w) { return Inside(d) * smoothstep(-w - 0.5, -w + 0.5, d); }

            // CSS `inset 0 0 blur` — a shadow pooling in from the edge.
            float InsetShadow(float d, float blur)
            {
                float t = saturate(1.0 + d / max(blur, 1e-3));
                return Inside(d) * t * t;
            }

            // The guide's gradients run at 160 degrees: nearly top to bottom,
            // leaning right. t is 0 at the lit corner and 1 at the far one.
            float Gradient160(float2 q, float2 size)
            {
                const float2 dir = float2(0.34202, -0.93969);
                float len = size.x * 0.34202 + size.y * 0.93969;
                return saturate(0.5 + dot(q, dir) / max(len, 1e-3));
            }

            float4 Stops3(float4 a, float4 b, float4 c, float mid, float t)
            {
                return t < mid ? lerp(a, b, t / max(mid, 1e-3)) : lerp(b, c, (t - mid) / max(1.0 - mid, 1e-3));
            }

            // ---- state access ----------------------------------------------------

            // cell = (column j, grid row i); row 0 is the top row.
            float4 LoadState(float2 cell)
            {
                float2 uv = (cell + 0.5) / float2(_Cols, _Rows);
                return SAMPLE_TEXTURE2D_LOD(_StateTex, sampler_StateTex, uv, 0);
            }

            bool CellInBounds(float2 cell)
            {
                return cell.x >= 0 && cell.x < _Cols && cell.y >= 0 && cell.y < _Rows;
            }

            // Unpack (dr + 1) * 3 + (dc + 1) back into the grid delta.
            float2 UnpackDir(float packedDir)
            {
                float p = round(packedDir);
                float dr = floor(p / 3.0) - 1.0;
                float dc = p - (dr + 1.0) * 3.0 - 1.0;
                return float2(dr, dc);
            }

            // Grid delta -> cell-UV delta. Row index grows downward, V grows up.
            float2 TravelUv(float2 drdc) { return float2(drdc.y, -drdc.x); }

            bool IsSeedDir(float2 drdc) { return abs(drdc.x) + abs(drdc.y) < 0.5; }

            // Does this cell carry a glass body (and so a shadow)?
            bool HasBody(int value)
            {
                return value == CELL_ACTIVE || value == CELL_WALL || value == CELL_SWITCH ||
                       value == CELL_INFECT || value == CELL_TRAP;
            }

            // ---- infection timeline ----------------------------------------------

            float Progress(float startTime)
            {
                return saturate((_BoardTime - startTime - _TraceDur) / max(_BleedDur, 1e-4));
            }

            // Emission gain: hot on arrival, cooling to the resting glow.
            float Emission(float startTime)
            {
                float settle = startTime + _TraceDur + _BleedDur;
                float k = saturate((_BoardTime - settle - _GlowHold) / max(_GlowFade, 1e-4));
                float pulse = 1.0;
                if (_ArrivalPulse > 0.5)
                {
                    float age = _BoardTime - settle;
                    if (age >= 0.0 && age <= _PulseDur) pulse = lerp(_PulseGain, 1.0, age / max(_PulseDur, 1e-4));
                }
                return lerp(_HotEmission * pulse, _RestEmission, k);
            }

            // ---- blot --------------------------------------------------------------

            float Hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            // The noise texture is exactly (_Cols * _Blocks) x (_Rows * _Blocks),
            // one continuous field across the whole board.
            float NoiseAtBlock(float2 blockGlobal)
            {
                float2 uv = (blockGlobal + 0.5) / (float2(_Cols, _Rows) * _Blocks);
                return SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, uv, 0).r;
            }

            // The pool: 0 at the midpoint of the edge the infection entered
            // from, 1 at the far corner. The seed has no entry edge and pools
            // out of its centre. The blot only ripples the front (STYLE-GUIDE
            // §5: the infection "pools across", it does not dissolve in).
            float PoolField(float2 cellUv, float2 drdc, float2 blockGlobal)
            {
                float e;
                if (IsSeedDir(drdc))
                {
                    e = length(cellUv - 0.5) / 0.70711;
                }
                else
                {
                    float2 travel = TravelUv(drdc);
                    float2 entry = 0.5 - travel * 0.5;
                    e = length(cellUv - entry) / 1.11803;
                }
                float n = NoiseAtBlock(blockGlobal) - 0.5;
                return e + n * (1.0 - _Bias) * _BlotAmp;
            }

            // ---- traces ----------------------------------------------------------

            float SegDistance(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float2 ap = p - a;
                float h = saturate(dot(ap, ab) / max(dot(ab, ab), 1e-6));
                return length(ap - ab * h);
            }

            // A trace cools on the same curve as the cell it feeds, and holds at
            // _TraceDimLevel from that cell's settle onward when the layer is on.
            float TraceBrightness(float startTime)
            {
                float settle = startTime + _TraceDur + _BleedDur;
                float k = saturate((_BoardTime - settle - _GlowHold) / max(_GlowFade, 1e-4));
                float dim = (_BoardTime >= settle && _TraceDim > 0.5) ? _TraceDimLevel : 1.0;
                return dim * (1.0 - k);
            }

            float TraceCoverage(float2 cellUv, float2 a, float2 b)
            {
                float d = SegDistance(cellUv, a, b) * _PitchPx;
                return 1.0 - smoothstep(_TracePx * 0.5 - 0.5, _TracePx * 0.5 + 0.5, d);
            }

            // ---- tile materials --------------------------------------------------

            // Component, placed and dormant: white 34% -> 8% (55%) -> 16% at
            // 160 degrees, a 1 px top light at 60%, a 1 px ring at 25%.
            void GlassComponent(inout float4 pm, float2 q, float d, float2 tile, float3 tint, float tintAmount, float alphaScale)
            {
                float t = Gradient160(q, tile);
                float4 fill = Stops3(float4(_ColTip.rgb, 0.34), float4(_ColTip.rgb, 0.08), float4(_ColTip.rgb, 0.16), 0.55, t);
                fill.rgb = lerp(fill.rgb, tint, tintAmount);
                fill.a = lerp(fill.a, 0.75, tintAmount) * alphaScale;
                Over(pm, fill.rgb, fill.a * Inside(d));
                Over(pm, _ColTip.rgb, 0.25 * InsetRing(d, _RefScale));
                float top = (q.y > 0.0 && abs(q.x) < tile.x * 0.5 - _TileRadiusPx) ? Inside(d) * smoothstep(-_RefScale - 0.5, -_RefScale + 0.5, d) : 0.0;
                Over(pm, _ColTip.rgb, 0.6 * top);
            }

            // Blocker: white 60% -> 20%, top light at 100%, a 2 px ring at 75%.
            void GlassBlocker(inout float4 pm, float2 q, float d, float2 tile)
            {
                float t = Gradient160(q, tile);
                Over(pm, _ColTip.rgb, lerp(0.6, 0.2, t) * Inside(d));
                Over(pm, _ColTip.rgb, 0.75 * InsetRing(d, 2.0 * _RefScale));
                float top = (q.y > 0.0 && abs(q.x) < tile.x * 0.5 - _TileRadiusPx) ? Inside(d) * smoothstep(-_RefScale - 0.5, -_RefScale + 0.5, d) : 0.0;
                Over(pm, _ColTip.rgb, top);
            }

            // Infected: the light inside the glass. Coverage masks the fill so
            // the same material draws the pool mid-bleed and the settled tile.
            void GlassInfected(inout float4 pm, float2 q, float d, float2 tile, float coverage, float emission)
            {
                float t = Gradient160(q, tile);
                float3 hi = lerp(_ColInfectHi.rgb, _ColTip.rgb, 0.45);
                float4 fill = Stops3(float4(hi, 0.9), float4(_ColInfect.rgb, 1.0), float4(_ColInfectLo.rgb, 1.0), 0.55, t);
                Over(pm, fill.rgb * emission, fill.a * Inside(d) * coverage);
                Over(pm, _ColTip.rgb * emission, 0.4 * InsetRing(d, _RefScale) * coverage);
                float top = (q.y > 0.0 && abs(q.x) < tile.x * 0.5 - _TileRadiusPx) ? Inside(d) * smoothstep(-_RefScale - 0.5, -_RefScale + 0.5, d) : 0.0;
                Over(pm, _ColTip.rgb * emission, 0.85 * top * coverage);
            }

            // A 9 px dot with a 7 px glow: gold on a dormant component, white
            // on a lit one — the shape that stays constant while the colour
            // and the light change (R-1001).
            void CoreDot(inout float4 pm, float2 q, float3 col, float glowAlpha, float emission)
            {
                float dd = length(q) - 4.5 * _RefScale;
                float g = saturate(1.0 - max(dd, 0.0) / (7.0 * _RefScale));
                Over(pm, col * emission, glowAlpha * g * g);
                Over(pm, col * emission, Inside(dd));
            }

            // ---- fragment --------------------------------------------------------

            half4 Frag(Varyings input) : SV_Target
            {
                float2 px = saturate(input.uv) * _QuadPx.xy;
                float2 lp = px - _LatticeOrigin.xy;                 // lattice-local px, y up
                float2 latticeSize = float2(_Cols, _Rows) * _PitchPx;
                float gapPx = _PitchPx * (1.0 - _CellFrac);
                float2 tile = float2(_PitchPx * _CellFrac, _PitchPx * _CellFrac);
                float2 halfTile = tile * 0.5;
                float s = _RefScale;

                float4 pm = 0;

                // ---- the well (STYLE-GUIDE §4) ----
                float2 wellHalf = latticeSize * 0.5 - gapPx * 0.5 + _WellPadPx;
                float dWell = SdRoundBox(lp - latticeSize * 0.5, wellHalf, _WellRadiusPx);
                Over(pm, _ColShade.rgb, 0.18 * (1.0 - smoothstep(3.0 * s - 0.5, 3.0 * s + 0.5, dWell)) * (1.0 - Inside(dWell)));
                Over(pm, _ColWellBg.rgb, _ColWellBg.a * Inside(dWell));
                Over(pm, _ColShade.rgb, 0.5 * InsetShadow(dWell, 60.0 * s));
                Over(pm, _ColTip.rgb, 0.14 * InsetRing(dWell, s));

                // ---- which cell, and the 3 x 3 neighbourhood ----
                float2 grid = lp / _PitchPx;
                float2 cellXY = floor(grid);
                bool inLattice = all(cellXY >= 0.0) && all(cellXY < float2(_Cols, _Rows));
                float2 own = clamp(cellXY, float2(0.0, 0.0), float2(_Cols - 1.0, _Rows - 1.0));

                // Shadows and glows spill into the gutters and over the well,
                // so every tile within a pitch of this fragment gets a say.
                for (int ny = -1; ny <= 1; ny++)
                {
                    for (int nx = -1; nx <= 1; nx++)
                    {
                        float2 nXY = own + float2(nx, ny);
                        if (any(nXY < 0.0) || any(nXY >= float2(_Cols, _Rows))) continue;
                        float2 nCell = float2(nXY.x, (_Rows - 1) - nXY.y);
                        float4 ns = LoadState(nCell);
                        int nValue = (int)round(ns.r);
                        if (!HasBody(nValue)) continue;
                        float2 nq = lp - (nXY + 0.5) * _PitchPx;
                        float nd = SdRoundBox(nq, halfTile, _TileRadiusPx);
                        float outside = 1.0 - Inside(nd);

                        // 0 7px 16px black 38%: a drop shadow below every body.
                        float ds = SdRoundBox(nq + float2(0.0, 7.0 * s), halfTile, _TileRadiusPx);
                        Over(pm, _ColShade.rgb, 0.38 * (1.0 - smoothstep(-8.0 * s, 8.0 * s, ds)) * outside);

                        // 0 0 26px infect: the near glow of a lit tile. The 64 px
                        // halo beyond it is the bloom's job.
                        if (nValue == CELL_INFECT && Progress(ns.g) > 0.0)
                        {
                            // The glow rests at the tile's resting light; the
                            // arrival flash is the bloom's to carry.
                            float cover = Progress(ns.g);
                            float e = min(Emission(ns.g), _RestEmission);
                            float g = saturate(1.0 - max(nd, 0.0) / _GlowPx);
                            Over(pm, _ColInfect.rgb * e, 0.7 * g * g * cover * outside);
                            float g2 = saturate(1.0 - max(nd, 0.0) / (2.4 * _GlowPx));
                            Over(pm, _ColInfectGlow.rgb * e, _ColInfectGlow.a * 0.4 * g2 * g2 * cover * outside);
                        }
                    }
                }

                if (!inLattice) return half4(pm);

                float2 cell = float2(cellXY.x, (_Rows - 1) - cellXY.y);   // (j, i)
                float2 cellUv = grid - cellXY;
                float2 q = lp - (cellXY + 0.5) * _PitchPx;              // px from the tile centre
                float d = SdRoundBox(q, halfTile, _TileRadiusPx);

                float2 blockGlobal = min(floor(grid * _Blocks), float2(_Cols, _Rows) * _Blocks - 1);
                float2 blockInCell = blockGlobal - cellXY * _Blocks;

                float4 state = LoadState(cell);
                int value = (int)round(state.r);
                float startTime = state.g;
                float2 drdc = UnpackDir(state.b);
                int kind = (int)round(state.a);

                if (value == CELL_VOID)
                {
                    // Out of bounds: black 5%, inset 1 px black 10%.
                    Over(pm, _ColShade.rgb, 0.05 * Inside(d));
                    Over(pm, _ColShade.rgb, 0.10 * InsetRing(d, s));
                }
                else if (value == CELL_FORBIDDEN)
                {
                    // Must stay clean (R-1001): the bare pad with nothing on
                    // it, ringed so it is a shape and not a colour.
                    float r = length(q);
                    Over(pm, _ColCopper.rgb, Inside(r - 5.5 * s));
                    Over(pm, _ColCopperHi.rgb, Inside(r - 4.5 * s));
                    float ring = Inside(r - 0.27 * tile.x) * (1.0 - Inside(r - 0.17 * tile.x));
                    Over(pm, _ColCopperLo.rgb, 0.9 * ring);
                }
                else if (value == CELL_WALL)
                {
                    GlassBlocker(pm, q, d, tile);
                }
                else if (value == CELL_SWITCH)
                {
                    GlassComponent(pm, q, d, tile, _ColSwitch.rgb, 0.6, 1.0);
                    float diamond = (abs(q.x) + abs(q.y)) - 0.19 * tile.x;
                    Over(pm, _ColGlyphEdge.rgb, Inside(diamond));
                }
                else if (value == CELL_TRAP)
                {
                    GlassComponent(pm, q, d, tile, _ColTrap.rgb, 0.7, 1.0);
                    float2 a = abs(q) / tile.x;
                    float x = (abs(a.x - a.y) < 0.07 && max(a.x, a.y) < 0.28) ? 1.0 : 0.0;
                    Over(pm, _ColConflict.rgb, x);
                }
                else if (value == CELL_ACTIVE)
                {
                    GlassComponent(pm, q, d, tile, _ColTip.rgb, 0.0, 1.0);

                    if (kind == TR_RECEDE)
                    {
                        // A repel walking the infection back off a cell, or an
                        // undo lifting it: the pool drains back toward the edge
                        // it came in from.
                        float r = saturate((_BoardTime - startTime) / max(_BleedDur, 1e-4));
                        if (r < 1.0)
                        {
                            float f = PoolField(cellUv, drdc, blockGlobal);
                            float coverage = 1.0 - smoothstep(1.0 - r - 0.06, 1.0 - r, f);
                            GlassInfected(pm, q, d, tile, coverage, _RestEmission * (1.0 - r));
                        }
                    }

                    CoreDot(pm, q, _ColCopperHi.rgb, 0.9, 1.0);

                    if (kind == TR_PREVIEW)
                    {
                        // Pending trace: where the piece under the finger would
                        // reach. Radial glow to transparent at 70%, 1 px ring.
                        float fade = saturate((_BoardTime - startTime) / max(_PreviewFade, 1e-4));
                        float r = length(q) / (halfTile.x * 1.41421356 * 0.7);
                        Over(pm, _ColInfectGlow.rgb, _ColInfectGlow.a * (1.0 - saturate(r)) * Inside(d) * fade);
                        Over(pm, _ColInfect.rgb, 0.5 * InsetRing(d, s) * fade);
                    }
                }
                else if (value == CELL_INFECT)
                {
                    GlassComponent(pm, q, d, tile, _ColTip.rgb, 0.0, 1.0);

                    float p = Progress(startTime);
                    if (p > 0.0)
                    {
                        float emission = Emission(startTime);
                        float f = PoolField(cellUv, drdc, blockGlobal);

                        // Solid where the pool has reached, then a glow band
                        // leading the front (r16 solid, r26 glow, r38 clear).
                        float coverage = p >= 1.0 ? 1.0 : 1.0 - smoothstep(p - 0.05, p + 0.02, f);
                        GlassInfected(pm, q, d, tile, coverage, emission);
                        bool trailing = _GhostTrail > 0.5 && _BoardTime < startTime + _TraceDur + _BleedDur + _GhostTrailDur;
                        if (p < 1.0 || trailing)
                        {
                            float band = (1.0 - smoothstep(p, p + 0.22, f)) * smoothstep(p - 0.02, p + 0.02, f);
                            float bandFade = p < 1.0 ? 1.0 : saturate((startTime + _TraceDur + _BleedDur + _GhostTrailDur - _BoardTime) / max(_GhostTrailDur, 1e-4));
                            Over(pm, _ColInfectGlow.rgb * emission, _ColInfectGlow.a * band * Inside(d) * bandFade);
                        }

                        CoreDot(pm, q, _ColTip.rgb, 0.6 * coverage, emission);

                        // Edge sparks: single-block particles thrown off the
                        // front, confined to the cell's own tile.
                        if (_EdgeSparks > 0.5 && p < 1.0)
                        {
                            bool radial = IsSeedDir(drdc);
                            float2 travel = radial ? float2(0, 0) : TravelUv(drdc);
                            float2 perp = float2(-travel.y, travel.x);
                            float best = 0.0;
                            for (int k = 0; k < SPARK_COUNT; k++)
                            {
                                float2 seed = cell * 17.0 + float2(k * 7.13, k * 13.71);
                                float h0 = Hash21(seed);
                                float h1 = Hash21(seed + 3.31);
                                float h2 = Hash21(seed + 9.77);
                                float launch = startTime + _TraceDur + _BleedDur * h0;
                                float age = _BoardTime - launch;
                                if (age < 0.0 || age > _SparkLife) continue;
                                float2 origin = radial
                                    ? float2(0.5, 0.5)
                                    : 0.5 - travel * 0.5 + travel * h0 + perp * (h1 - 0.5) * 0.6;
                                float2 vel = radial
                                    ? normalize(float2(h1 - 0.5, h2 - 0.5) + 1e-3) * (0.6 + h2 * 0.8)
                                    : travel * (0.9 + h2 * 0.9) + perp * (h1 - 0.5) * 0.8;
                                float2 pos = origin + vel * age;
                                if (any(pos <= 0.0) || any(pos >= 1.0)) continue;
                                if (all(abs(floor(pos * _Blocks) - blockInCell) < 0.5))
                                    best = max(best, 1.0 - age / max(_SparkLife, 1e-4));
                            }
                            if (best > 0.0) Over(pm, _ColInfectHi.rgb * _HotEmission, best);
                        }
                    }
                    else
                    {
                        CoreDot(pm, q, _ColCopperHi.rgb, 0.9, 1.0);
                    }
                }

                // Conflict is an overprint plus the X glyph the trap already
                // carries, never a colour shift of the cell's own state.
                if (kind == TR_CONFLICT)
                {
                    float flash = saturate(1.0 - (_BoardTime - startTime) / max(_ConflictDur, 1e-4));
                    Over(pm, _ColConflict.rgb * _HotEmission, flash * 0.85 * Inside(d));
                }

                // Traces last, so a beam crosses the gutters. Each trace runs
                // parent centre -> cell centre over _TraceDur; this cell draws
                // its own inbound half and the outbound half of every neighbour
                // that was entered from here.
                float trace = 0.0;
                if (value == CELL_INFECT && !IsSeedDir(drdc))
                {
                    float t = (_BoardTime - startTime) / max(_TraceDur, 1e-4);
                    if (t > 0.5)
                    {
                        float2 travel = TravelUv(drdc);
                        float2 a = 0.5 - travel * 0.5;
                        float2 b = lerp(a, float2(0.5, 0.5), saturate((t - 0.5) * 2.0));
                        trace = max(trace, TraceCoverage(cellUv, a, b) * TraceBrightness(startTime));
                    }
                }
                // Only an infected cell can be a trace's parent — or a void,
                // which the spread jumps over, and across which the beam should
                // still be seen making the jump.
                if (value == CELL_INFECT || value == CELL_VOID)
                {
                    float2 neighbours[4] = { float2(0, -1), float2(0, 1), float2(-1, 0), float2(1, 0) };
                    for (int n = 0; n < 4; n++)
                    {
                        float2 nd = neighbours[n];
                        float2 ncell = float2(cell.x + nd.y, cell.y + nd.x);
                        if (!CellInBounds(ncell)) continue;
                        float4 ns = LoadState(ncell);
                        if ((int)round(ns.r) != CELL_INFECT) continue;
                        if (any(abs(UnpackDir(ns.b) - nd) > 0.01)) continue;   // not entered from here
                        float t = (_BoardTime - ns.g) / max(_TraceDur, 1e-4);
                        if (t <= 0.0) continue;
                        float2 travel = TravelUv(nd);
                        float2 a = float2(0.5, 0.5);
                        float2 b = lerp(a, a + travel * 0.5, saturate(t * 2.0));
                        trace = max(trace, TraceCoverage(cellUv, a, b) * TraceBrightness(ns.g));
                    }
                }
                if (trace > 0.0) Over(pm, _ColInfect.rgb * _HotEmission, trace);

                return half4(pm);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
