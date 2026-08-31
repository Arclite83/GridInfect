# Infection VFX spec

Art direction and infection animation for the Grid Infect rebuild. Unity, URP 2D.

## Locked parameters

| Parameter | Value | Shader/field name |
|---|---|---|
| Blocks per cell | 16 | `_Blocks` |
| Hop delay | 40 ms | `_Hop` |
| Direction bias | 0.3 | `_Bias` |
| Glow hold | 150 ms | `_GlowHold` |
| Glow fade | 300 ms | `_GlowFade` |
| Trace pulse | 90 ms | `_TraceDur` |
| Bleed dissolve | 260 ms | `_BleedDur` |

Trace and bleed durations are the two remaining tunables. Everything else is fixed.

## Style

Neon circuit on near-black. Flat fills, hard edges, no gradients in the base art. All visual richness comes from the transition, not the resting state.

| Element | Colour | Notes |
|---|---|---|
| Board background | `#0B1020` | Near-black, never pure black |
| Grid lines | `#1B2A48` | Hairline, always visible |
| Cell border (empty) | `#2B3F63` | 1 px |
| Infected fill | `#00D9FF` | HDR emissive, cools on fade |
| Cooled fill | `#0B7F99` | Fade target, non-emissive |
| Bleed edge band | `#E0FFFF` | Transition only |
| Glitch ghost | `#FF3DD8` at 45% | Transition only |
| Seed marker | `#FF3DD8` | Emissive |
| Immune hatch | `#55688A` | 45 degree lines, 7 px pitch |

Palette lives in a `BoardPalette` ScriptableObject. Nothing samples a literal colour. New board types are a palette swap plus a noise texture swap.

## Architecture

One board quad, one material, one draw call. Per-cell sprites are not used for infection state.

Cell state lives in a `COLS x ROWS` point-filtered `RGBAFloat` data texture:

| Channel | Contents |
|---|---|
| R | State id (0 empty, 1 infected, 2 immune, 3 conflict) |
| G | Infection start time, seconds, board clock |
| B | Entry direction packed: `(dr + 1) * 3 + (dc + 1)` |
| A | Reserved |

The simulation writes the texture. The shader reads it. The shader never writes back and never gates input. A player placing a piece mid-bleed writes new values immediately; in-flight cells continue from their own start times.

Noise samples in **board UV**, not cell UV, so the blot pattern is continuous across cell borders. Quantise to `floor(boardUV * cellCount * _Blocks) / (cellCount * _Blocks)` so blocks align to cell boundaries at a constant on-screen size.

## Spread rule

Infection travels in **straight rays** from the seed. It does not flood-fill.

For each direction in the bug's direction set, step outward one cell at a time. Stop on immune cell, occupied-blocking cell, or board edge. Cell at step `d` gets `startTime = seedTime + d * _Hop`.

Bug movement is data, not code:

```
BugType {
  directions : Vector2Int[]   // e.g. cross = (0,1)(0,-1)(1,0)(-1,0)
  range      : int            // 0 = unlimited
}
```

Cross, row, column, and diagonal are direction sets on the same ray-cast path. Do not branch per bug type in the shader or the spread routine.

At 40 ms hop against a 90 ms trace, several traces are in flight at once. This is intended: the wavefront reads as a beam racing outward rather than a chain of discrete steps.

## Per-cell timeline

```
t+0                trace pulse starts from the parent cell centre
t+90               trace lands, bleed dissolve starts
t+350              cell fully filled, hard-edged, hot
t+500              glow starts cooling
t+800              glow at rest, colour at #0B7F99
```

Longest ray on an 8-wide board: 7 hops, so 280 + 350 = 630 ms to settle, 1080 ms to fully cool. The cell is legible and stable from 350 ms after its own trace lands, which is the number that matters under the timer.

## Shader spec

### Board shader (unlit, URP 2D)

Inputs: `_StateTex`, `_NoiseTex`, `_Blocks`, `_Bias`, `_TraceDur`, `_BleedDur`, `_GlowHold`, `_GlowFade`, `_BoardTime`, plus palette colours.

Per fragment:

1. Read cell state. Empty and immune return the base fill; immune adds the hatch pattern.
2. `p = saturate((_BoardTime - startTime - _TraceDur) / _BleedDur)`. Discard the infected fill if `p <= 0`.
3. `n = tex2D(_NoiseTex, quantisedBoardUV).r`
4. `e = entryDistance(entryDir, quantisedCellUV)` — 0 at the edge the infection entered from, 1 at the opposite edge. Seed cell uses radial distance from centre instead.
5. `t = lerp(n, e, _Bias)`
6. Fill where `t <= p`.
7. Edge band: where `t > p - 0.12`, output `#E0FFFF`.
8. Glitch band: where `t <= p + 0.15` and `hash(blockIndex, floor(_BoardTime * 20)) > 0.5`, output the edge band colour. Resamples at 20 Hz, so the band flickers.
9. Ghost: while `p < 1`, sample the fill mask offset one block along `entryDir` and output `#FF3DD8` at 45% under the main fill.
10. Glow: `k = saturate((_BoardTime - settleTime - _GlowHold) / _GlowFade)`. Multiply emission by `1 - k` and lerp fill colour toward the cooled colour by `k`.

Bias 0.3 means the pattern is noise-dominant with a directional lean. It reads as ink soaking in from the entry edge, not as a wipe.

### Trace shader

Line segment from parent cell centre to target cell centre, drawn to `saturate((_BoardTime - startTime) / _TraceDur)`. 2.5 px, round caps, emissive, same glow cooling curve as the cell it feeds.

### Bloom

URP Bloom, threshold above the cooled fill luminance and above the resting cell border. Only the hot fill, the edge band, the active trace, and the seed marker bloom. Empty and immune cells never bloom. If the threshold has to be lowered to make the effect read, the effect is wrong, not the threshold.

## Legibility rules

These override any juice decision.

- A cell is hard-edged and static within 350 ms of its trace landing. Flicker and ghost exist only during dissolve.
- State is carried by fill, hatch, and glyph. Never by glow alone. A cooled infected cell is still unambiguous against an empty cell.
- Conflict is a red overprint plus an X glyph, not a colour shift.
- Input is never blocked by animation. Placement during a bleed is legal and the new wave starts on the same frame.

## Juice layers

Each is an independent bool on the board controller, default on unless noted.

| Layer | Behaviour |
|---|---|
| Arrival pulse | Emission spikes 1.4x for 60 ms as a cell settles |
| Conflict shake | 2 px board shake, conflict only, 120 ms |
| Edge sparks | Up to 8 single-block particles ejected from the edge band, cyan, 200 ms life |
| Trace dim | Trace holds at 30% after its cell settles, then cools with it |
| Hop audio | Click per hop, pitch +1 semitone per ray depth, capped at +7 |
| Ghost trail (off) | Magenta ghost persists 200 ms after fill completes |

## Acceptance criteria

1. Whole board renders in one draw call. Infection state changes do not allocate or rebuild meshes.
2. Board holds 60 fps on the minimum target device with every cell infected and every juice layer on.
3. Spread follows the direction set exactly. Rays stop at immune cells, blocking cells, and board edges. No diagonal leakage on cross-type bugs.
4. The blot pattern is continuous across cell borders. No visible seam or pattern restart at any cell boundary.
5. Every state is identifiable at 5 cm board width on device without fixating, in both hot and cooled glow states.
6. The bleed reads as ink at hop values from 20 ms to 200 ms without parameter changes.
7. Placement input during an active wave is accepted on the frame it occurs and starts a new wave.
8. Changing a `BoardPalette` asset restyles the whole board with no code or shader edits.
9. Bloom threshold rejects the resting board. A screenshot with no active infection has no bloom.

---

## As built

Implemented in `unity/Assets/_Project/Game/`: `Shaders/GridInfectBoard.shader`
(everything the board draws), `View/BoardPalette.cs` + `Resources/BoardPalette.asset`
(the palette), `View/BoardStateTexture.cs` (the data texture), `View/BoardNoise.cs`
(the blot), `View/BoardView.cs` (quad, clock, juice switches, wave scheduling),
`View/BoardBloom.cs`, `Audio/HopClickAudio.cs`. The locked parameters live in
`PresentationConfig.Infection`; `InfectionVfxSpecTests` fails if this document
and that table stop agreeing.

Where the build deviates from the spec above, and why.

### Board size

Grid Infect's board is 11 x 6, not 8 wide (`Grid`, fixed by RULES §1). The
timeline is unchanged; the totals move: the longest ray is 10 hops, so 400 ms
of travel, 750 ms to settle, 1200 ms to fully cool.

### Cell state ids

The R channel carries the game's own wire vocabulary — `0` void, `1` active,
`2` wall, `3` repel switch, `4` infected, `5` reset trap — rather than the
spec's four-state table. Grid Infect ships two states the table does not name,
and a lossy remap would mean a second enum to keep in sync with `Rules`. The
wire value is already a superset, so it goes across unchanged. "Immune" is the
wall; "empty" is active; void is a hole in the board and draws neither border
nor fill.

The reserved A channel carries the transition kind: `0` none, `1` infecting,
`2` receding, `3` conflict flash. It is what lets one texel describe both what
a cell is and what is happening to it.

### Spread rule

Already data, not code, and already a straight-ray walk: `Rules.PropagatePiece`
steps offset-major rings 1..`Grid.SpreadRange` over the arms of a `Tile`, which
is exactly `BugType { directions, range }` under a different name. No spread
code was added.

Nothing in the view re-derives it either. `BoardView` brackets a placement,
watches `CellChanged`, and reads depth off the Manhattan distance from the seed
and the entry direction off the sign of the offset — correct precisely because
the spread is rays. One place mirrors a rule: finding which trap stopped a ray,
so the conflict overprint can light when the beam reaches it instead of when
the reset lands 300 ms later.

### Conflict

Grid Infect has no per-cell conflict state. The reset trap is the conflict
event: the trap overprints red on the ray that hit it (it already carries the X
glyph), and the board shakes when the reset lands. The replay button also full-
resets and deliberately does not shake.

### Recession

Not in the spec, needed by the game: repels walk infection back off a ray and
undo lifts a piece. Receding cells run the same blot in reverse, staggered by
hop for a repel (it walks) and simultaneous for an undo or a reset (they do
not).

### Edge sparks

Confined to the cell's own pitch tile so each fragment tests eight sparks and
never its neighbours'. They read as thrown off the band; they do not cross into
the next cell.

### Glitch band

Read as straddling the front — `p - 0.12 < t <= p + 0.15` — rather than
`t <= p + 0.15` alone, which would flicker the entire filled area rather than a
band. Band and ghost both fade out over the last of the dissolve, because a
cell must be hard-edged and static within 350 ms.

### Bloom and colour space

The project now renders in **linear** colour space, which is what
`docs/DEPENDENCIES.md` always specified and what the emission and cooling
maths here assume — the setting had simply never been flipped. `lerp(hot,
cooled, k)` is a real interpolation now, and the bloom falloff is physical.

Threshold 1.0: only what the board pushes into HDR blooms, which is the hot
fill, the edge band, the active trace and the seed marker. Everything at rest
sits well under it — cooled fill 0.32 linear, immune hatch 0.25, cell border
0.13 — and so does the UI chrome, which peaks at 0.83 for white text and
exactly 1.0 for pure white. A luminance-tuned threshold would have caught the
text; "above LDR" does not.

One conversion is manual. `Material.SetColor` hands its value straight to the
GPU, unlike a sprite tint or a camera clear colour, which Unity converts for
you. The palette is authored in sRGB hex, so `BoardView.SetPaletteColor`
converts when the active colour space is linear — and only then, so flipping
the project setting back cannot silently double-darken the board.

### Not verified here

Acceptance criteria 2, 5 and 6 are runtime judgements — frame rate on device,
legibility at 5 cm, the bleed reading as ink across hop values — and need the
editor. What is structural holds by construction: one `MeshRenderer` with one
material for the whole board, and an infection change that writes texels only.
