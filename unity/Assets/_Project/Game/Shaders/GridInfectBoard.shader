// Grid Infect board shader — docs/infection-vfx-spec.md.
//
// The whole board is one quad, one material, one draw call. Cell state lives
// in _StateTex (point-filtered RGBAFloat, one texel per cell); the simulation
// writes it, this shader only reads it. Nothing here gates input and nothing
// writes back, so a placement landing mid-bleed just changes texels and the
// cells already in flight keep running off their own start times.
//
// _StateTex channels (BoardStateTexture.cs owns the writes):
//   R  cell value, the game's own wire vocabulary (0 void, 1 active, 2 wall,
//      3 repel switch, 4 infected, 5 reset trap)
//   G  transition start time, seconds on the board clock
//   B  entry direction packed as (dr + 1) * 3 + (dc + 1), grid deltas, 4 = seed
//   A  transition kind: 0 none, 1 infecting, 2 receding, 3 conflict flash
Shader "GridInfect/Board"
{
    Properties
    {
        [NoScaleOffset] _StateTex ("Cell state (RGBAFloat)", 2D) = "black" {}
        [NoScaleOffset] _NoiseTex ("Blot noise", 2D) = "gray" {}

        // Locked parameters (spec table). Blocks/bias/hop never move; trace
        // and bleed are the two tunables.
        _Cols ("Columns", Float) = 11
        _Rows ("Rows", Float) = 6
        _Blocks ("Blocks per cell", Float) = 16
        _Bias ("Direction bias", Range(0, 1)) = 0.3
        _TraceDur ("Trace pulse (s)", Float) = 0.09
        _BleedDur ("Bleed dissolve (s)", Float) = 0.26
        _GlowHold ("Glow hold (s)", Float) = 0.15
        _GlowFade ("Glow fade (s)", Float) = 0.30
        _BoardTime ("Board clock (s)", Float) = 0

        // Layout, in screen pixels (1 world unit = 1 screen pixel here).
        _BoardPx ("Board size (px)", Vector) = (1155, 630, 0, 0)
        _CellFrac ("Cell size / pitch", Float) = 0.952381
        _GridLinePx ("Grid line (px)", Float) = 1
        _BorderPx ("Cell border (px)", Float) = 1
        _HatchPitchPx ("Immune hatch pitch (px)", Float) = 7
        _TracePx ("Trace width (px)", Float) = 2.5

        _HotEmission ("Hot emission (HDR gain)", Float) = 2.2
        _EdgeBand ("Edge band width", Float) = 0.12
        _GlitchBand ("Glitch band width", Float) = 0.15
        _GlitchHz ("Glitch resample (Hz)", Float) = 20
        _GhostAlpha ("Ghost alpha", Range(0, 1)) = 0.45
        _ConflictDur ("Conflict flash (s)", Float) = 0.5

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
        _ColBackground ("Board background", Color) = (0, 0, 0, 1)
        _ColCellPlate ("Cell plate", Color) = (0, 0, 0, 1)
        _ColGridLine ("Grid line", Color) = (0, 0, 0, 1)
        _ColCellBorder ("Cell border", Color) = (0, 0, 0, 1)
        _ColInfected ("Infected fill", Color) = (0, 0, 0, 1)
        _ColCooled ("Cooled fill", Color) = (0, 0, 0, 1)
        _ColBleedEdge ("Bleed edge band", Color) = (0, 0, 0, 1)
        _ColGhost ("Glitch ghost", Color) = (0, 0, 0, 1)
        _ColSeed ("Seed marker", Color) = (0, 0, 0, 1)
        _ColImmuneHatch ("Immune hatch", Color) = (0, 0, 0, 1)
        _ColSwitch ("Repel switch", Color) = (0, 0, 0, 1)
        _ColTrap ("Reset trap", Color) = (0, 0, 0, 1)
        _ColConflict ("Conflict overprint", Color) = (0, 0, 0, 1)
        _ColGlyph ("Glyph", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "BoardInfection"
            // No LightMode tag: Unity files the pass under SRPDefaultUnlit,
            // which both the Forward and the 2D renderer draw.
            ZWrite On
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
                float4 _BoardPx;
                float _CellFrac, _GridLinePx, _BorderPx, _HatchPitchPx, _TracePx;
                float _HotEmission, _EdgeBand, _GlitchBand, _GlitchHz, _GhostAlpha, _ConflictDur;
                float _ArrivalPulse, _PulseGain, _PulseDur;
                float _EdgeSparks, _SparkLife;
                float _TraceDim, _TraceDimLevel;
                float _GhostTrail, _GhostTrailDur;
                float4 _ColBackground, _ColCellPlate, _ColGridLine, _ColCellBorder, _ColInfected, _ColCooled;
                float4 _ColBleedEdge, _ColGhost, _ColSeed, _ColImmuneHatch;
                float4 _ColSwitch, _ColTrap, _ColConflict, _ColGlyph;
            CBUFFER_END

            #define CELL_VOID    0
            #define CELL_ACTIVE  1
            #define CELL_WALL    2
            #define CELL_SWITCH  3
            #define CELL_INFECT  4
            #define CELL_TRAP    5

            #define TR_NONE      0
            #define TR_INFECT    1
            #define TR_RECEDE    2
            #define TR_CONFLICT  3

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

            // ---- state access -------------------------------------------------

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

            // ---- blot ---------------------------------------------------------

            float Hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            // The noise texture is exactly (_Cols * _Blocks) x (_Rows * _Blocks),
            // so a quantised board UV lands on its own texel: one noise value per
            // block, generated as one continuous field across the whole board.
            float NoiseAtBlock(float2 blockGlobal)
            {
                float2 uv = (blockGlobal + 0.5) / (float2(_Cols, _Rows) * _Blocks);
                return SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, uv, 0).r;
            }

            // 0 at the edge the infection entered from, 1 at the opposite edge.
            // The seed has no entry edge, so it uses radial distance instead.
            float EntryDistance(float2 drdc, float2 cellUv)
            {
                if (IsSeedDir(drdc)) return saturate(length(cellUv - 0.5) * 2.0);
                float2 travel = TravelUv(drdc);
                float2 entry = 0.5 - travel * 0.5;
                return saturate(dot(cellUv - entry, travel));
            }

            // t = lerp(noise, entryDistance, bias): noise-dominant with a lean
            // toward the entry edge, so it reads as ink soaking in.
            float BlotT(float2 blockGlobal, float2 blockInCell, float2 drdc)
            {
                float n = NoiseAtBlock(blockGlobal);
                float e = EntryDistance(drdc, (blockInCell + 0.5) / _Blocks);
                return lerp(n, e, _Bias);
            }

            // ---- traces ---------------------------------------------------------

            float SegDistance(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float2 ap = p - a;
                float h = saturate(dot(ap, ab) / max(dot(ab, ab), 1e-6));
                return length(ap - ab * h);   // round caps fall out of the distance
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

            float TraceCoverage(float2 cellUv, float2 a, float2 b, float pitchPx)
            {
                float d = SegDistance(cellUv, a, b) * pitchPx;
                return 1.0 - smoothstep(_TracePx * 0.5 - 0.5, _TracePx * 0.5 + 0.5, d);
            }

            // ---- fragment -------------------------------------------------------

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = saturate(input.uv);
                float2 grid = uv * float2(_Cols, _Rows);
                float2 cellXY = min(floor(grid), float2(_Cols - 1, _Rows - 1));
                float2 cellUv = grid - cellXY;                      // 0..1 across the pitch tile
                float2 cell = float2(cellXY.x, (_Rows - 1) - cellXY.y);   // (j, i)

                float2 blockGlobal = min(floor(uv * float2(_Cols, _Rows) * _Blocks),
                                         float2(_Cols, _Rows) * _Blocks - 1);
                float2 blockInCell = blockGlobal - float2(cellXY.x, cellXY.y) * _Blocks;

                float pitchPx = _BoardPx.x / _Cols;
                float2 px = uv * _BoardPx.xy;

                float4 state = LoadState(cell);
                int value = (int)round(state.r);
                float startTime = state.g;
                float2 drdc = UnpackDir(state.b);
                int kind = (int)round(state.a);

                // Tile geometry: gutter, hairline grid line, 1 px cell border.
                float2 m = abs(cellUv - 0.5);
                float halfCell = _CellFrac * 0.5;
                float2 toTileEdgePx = (0.5 - m) * pitchPx;
                float2 insideCellPx = (halfCell - m) * pitchPx;
                float edgeDistPx = min(toTileEdgePx.x, toTileEdgePx.y);
                float cellDistPx = min(insideCellPx.x, insideCellPx.y);
                bool inCell = cellDistPx > 0.0;
                bool onBorder = inCell && cellDistPx <= _BorderPx;

                float3 col = (edgeDistPx <= _GridLinePx * 0.5) ? _ColGridLine.rgb : _ColBackground.rgb;

                if (value != CELL_VOID && inCell)
                {
                    // Every cell that exists sits on a plate. A 1 px border on
                    // the board background was not enough to tell an empty cell
                    // from a hole at thumb size (criterion 5) — the fill is what
                    // carries it, and the border sharpens the edge.
                    col = _ColCellPlate.rgb;

                    if (value == CELL_ACTIVE)
                    {
                        if (onBorder) col = _ColCellBorder.rgb;

                        if (kind == TR_RECEDE)
                        {
                            // A repel walking infection back off a cell, or an
                            // undo lifting it: the same blot, pulled back toward
                            // the edge it came in from.
                            float r = saturate((_BoardTime - startTime) / max(_BleedDur, 1e-4));
                            if (r < 1.0)
                            {
                                float back = 1.0 - r;
                                float bt = BlotT(blockGlobal, blockInCell, drdc);
                                if (bt <= back) col = _ColCooled.rgb;
                                if (bt > back - _EdgeBand && bt <= back)
                                    col = _ColBleedEdge.rgb * _HotEmission;
                            }
                        }
                    }
                    else if (value == CELL_WALL)
                    {
                        // Immune: 45 degree hatch, constant on-screen pitch.
                        float hatch = frac((px.x + px.y) / (_HatchPitchPx * 1.41421356));
                        col = (hatch < 0.5) ? _ColImmuneHatch.rgb : _ColCellPlate.rgb;
                        if (onBorder) col = _ColCellBorder.rgb;
                    }
                    else if (value == CELL_SWITCH)
                    {
                        col = _ColSwitch.rgb;
                        if (abs(cellUv.x - 0.5) + abs(cellUv.y - 0.5) < 0.19) col = _ColGlyph.rgb;
                    }
                    else if (value == CELL_TRAP)
                    {
                        col = _ColTrap.rgb;
                        float2 d = abs(cellUv - 0.5);
                        if (abs(d.x - d.y) < 0.07 && max(d.x, d.y) < 0.28) col = _ColConflict.rgb;
                    }
                    else if (value == CELL_INFECT)
                    {
                        float p = saturate((_BoardTime - startTime - _TraceDur) / max(_BleedDur, 1e-4));
                        if (p > 0.0)
                        {
                            float settle = startTime + _TraceDur + _BleedDur;
                            float k = saturate((_BoardTime - settle - _GlowHold) / max(_GlowFade, 1e-4));

                            float pulse = 1.0;
                            if (_ArrivalPulse > 0.5)
                            {
                                float age = _BoardTime - settle;
                                if (age >= 0.0 && age <= _PulseDur)
                                    pulse = lerp(_PulseGain, 1.0, age / max(_PulseDur, 1e-4));
                            }

                            float3 hot = _ColInfected.rgb * _HotEmission * pulse;
                            float3 fill = lerp(hot, _ColCooled.rgb, k);

                            float t = BlotT(blockGlobal, blockInCell, drdc);

                            // Ghost: the fill mask displaced one block along the
                            // entry direction, so a magenta fringe leads the front.
                            bool ghostOn = (p < 1.0) ||
                                (_GhostTrail > 0.5 && _BoardTime < startTime + _TraceDur + _BleedDur + _GhostTrailDur);
                            if (ghostOn && !IsSeedDir(drdc))
                            {
                                float2 back = TravelUv(drdc);
                                float2 gBlock = blockGlobal - back;
                                float2 gInCell = clamp(blockInCell - back, 0.0, _Blocks - 1.0);
                                if (BlotT(gBlock, gInCell, drdc) <= p)
                                    col = lerp(col, _ColGhost.rgb, _GhostAlpha);
                            }

                            if (t <= p) col = fill;

                            // Edge band and the 20 Hz glitch band straddle the
                            // front; both belong to the dissolve only, so they
                            // fade out as the cell locks down hard-edged.
                            if (p < 1.0)
                            {
                                float bandFade = saturate((1.0 - p) * 8.0);
                                bool edge = t > p - _EdgeBand && t <= p;
                                bool glitch = t > p - _EdgeBand && t <= p + _GlitchBand &&
                                    Hash21(blockGlobal + floor(_BoardTime * _GlitchHz) * 37.0) > 0.5;
                                if (edge || glitch)
                                    col = lerp(col, _ColBleedEdge.rgb * _HotEmission, bandFade);
                            }

                            // Seed marker: an emissive ring one block thick, so it
                            // still reads with the piece sprite sitting on the cell.
                            if (IsSeedDir(drdc))
                            {
                                float ringPx = pitchPx / _Blocks;
                                if (cellDistPx <= ringPx) col = _ColSeed.rgb * _HotEmission;
                            }
                            else if (t <= p)
                            {
                                // Shape glyph so infected never reads by colour
                                // alone; it shows as a hole in the ink.
                                if (max(abs(cellUv.x - 0.5), abs(cellUv.y - 0.5)) < 0.09) col = _ColGlyph.rgb;
                            }

                            // Edge sparks: single-block particles thrown off the
                            // band, confined to the cell's own tile.
                            if (_EdgeSparks > 0.5 && p < 1.0)
                            {
                                bool radial = IsSeedDir(drdc);
                                float2 travel = radial ? float2(0, 0) : TravelUv(drdc);
                                float2 perp = float2(-travel.y, travel.x);
                                float best = 0.0;
                                for (int s = 0; s < SPARK_COUNT; s++)
                                {
                                    float2 seed = cell * 17.0 + float2(s * 7.13, s * 13.71);
                                    float h0 = Hash21(seed);
                                    float h1 = Hash21(seed + 3.31);
                                    float h2 = Hash21(seed + 9.77);
                                    float launch = startTime + _TraceDur + _BleedDur * h0;
                                    float age = _BoardTime - launch;
                                    if (age < 0.0 || age > _SparkLife) continue;

                                    // Launched from wherever the front is: along
                                    // the ray for a ray cell, out of the centre
                                    // for the seed, which has no entry edge.
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
                                if (best > 0.0) col = _ColInfected.rgb * _HotEmission * best;
                            }
                        }
                    }
                    // Conflict is an overprint plus the X glyph the trap already
                    // carries, never a colour shift of the cell's own state.
                    if (kind == TR_CONFLICT)
                    {
                        float flash = saturate(1.0 - (_BoardTime - startTime) / max(_ConflictDur, 1e-4));
                        col = lerp(col, _ColConflict.rgb * _HotEmission, flash * 0.85);
                    }
                }

                // Traces last, so a beam crosses gutters and grid lines. Each
                // trace runs parent centre -> cell centre over _TraceDur; this
                // cell draws its own inbound half and the outbound half of every
                // neighbour that was entered from here.
                float trace = 0.0;
                if (value == CELL_INFECT && !IsSeedDir(drdc))
                {
                    float q = (_BoardTime - startTime) / max(_TraceDur, 1e-4);
                    if (q > 0.5)
                    {
                        float2 travel = TravelUv(drdc);
                        float2 a = 0.5 - travel * 0.5;
                        float2 b = lerp(a, float2(0.5, 0.5), saturate((q - 0.5) * 2.0));
                        trace = max(trace, TraceCoverage(cellUv, a, b, pitchPx) * TraceBrightness(startTime));
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
                        float q = (_BoardTime - ns.g) / max(_TraceDur, 1e-4);
                        if (q <= 0.0) continue;
                        float2 travel = TravelUv(nd);
                        float2 a = float2(0.5, 0.5);
                        float2 b = lerp(a, a + travel * 0.5, saturate(q * 2.0));
                        trace = max(trace, TraceCoverage(cellUv, a, b, pitchPx) * TraceBrightness(ns.g));
                    }
                }
                if (trace > 0.0) col = lerp(col, _ColInfected.rgb * _HotEmission, trace);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
