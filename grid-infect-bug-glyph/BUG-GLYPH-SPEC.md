# Grid Infect bug glyph v1b (locked 2026-09-04)

## Decision
The bug is the "nucleus" glyph: a hexagonal IC body with one lit squared lead per spread direction. Wire detailing: two bond wires alongside each active lead, one branch stub with pad per side leaving each bond wire near its top, three short stubs on every inactive body edge (outer two with pads), and one stub at each hex vertex that sits between two inactive edges. Lit lead tips are the only long, bright elements.

## Files
- `gen-bug-glyph.mjs` source of truth. Regenerate with `node gen-bug-glyph.mjs <outDir>`.
- `out/bug_<DIRS>.svg` one file per direction set, `DIRS` in canonical order `N E S W` (e.g. `bug_NE.svg`, `bug_NESW.svg`). All 15 non-empty sets are emitted; use the ones the ruleset needs.
- `out/bug_sheet.svg` all 15 in a 5x3 grid, 44px pitch, each in `<g id="bug_DIRS">`.

## Geometry (40x40 viewBox, center 20,20)
- Active lead: rect 6x12 at (17,2), rx 1. Tip: rect 4x5 at (18,2).
- Bond wires: x 14.5 and 25.5, straight 14 to 7, quadratic hook to y 4.5 at x 17 / 23.
- Lead branch stubs: leave bond wire outward at y 7.5, length 3, pad r 1.1.
- Body: hexagon 20,9 30,14.5 30,25.5 20,31 10,25.5 10,14.5. Core dot r 3.
- Inactive-edge stubs: base y 10.5, offsets -4 / 0 / +4, lengths 3.5 / 2.5 / 3.5, pads on the outer two.
- Vertex stubs: at 45/135/225/315 deg, length 2.5, no pad, only where both adjacent edges are inactive.
- Strokes: body outline 1.6, wires 1.0, pins 1.1.

## Colors (default skin)
| role | hex |
|---|---|
| body fill | #ff2d95 |
| lead / outline | #5a0033 |
| wires, stubs, pads | #3a0b22 |
| tips, core dot | #ffffff |
| body gloss | rgba(255,255,255,.4) |

Skins swap the four colors in the `C` table of the generator. Blue mask skin uses amber for body fill.

## Rendering
- Board sprite: 38x38 on the 46x46 G1 glass tile. Tray: 52x52 next piece, 36x36 queued.
- Rendered-sprite pipeline: body uses the same frosted-glass material as tiles, lit from inside. Leads, wires, stubs are opaque dark.
- Placement: tips light in sequence, then infection propagates out of the tips.
- No glyph element other than an active lead may exceed length 3.5.
