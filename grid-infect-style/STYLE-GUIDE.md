# Grid Infect visual style guide (locked 2026-09-04, chrome retuned 2026-09-05, logo and breadboard infection 2026-09-09)

Everything below is the decision. Regenerate vector assets with `node gen-assets.mjs out`.

## 1. Concept
Bugs on a printed circuit board. The board is the substrate, components are tiles, the infection is light inside the components, the bug is a component-shaped glyph. The infection glow is the only strong emissive element on screen.

## 2. Palette and skins
Mask color plus infection hue is a skin layer. Everything else is constant. Values in `out/tokens.json`.

| skin | mask | copper | infection | note |
|---|---|---|---|---|
| default | green #7fae66 | gold #c9a648 | deep rose red #d9204f | ship default. Red is infection, and infection is success |
| blue | #2e5aa8 | gold | amber #ff8a00 | unlockable |
| breadboard | cream #e9dcb8 | bare copper #c46a3a | lime #7fd100 | unlockable. Lime over red (2026-09-09): on a cream mask the red read as paint, and a light lime read as a stain. This lime keeps enough depth in the core that the white tips still register as the bright element. Edge #1e2e00, wire #141f00 |

Neutrals: lit tips and highlights #ffffff, blocker body #cfd8e0, blocker edge #4d565f, board well rgba(0,0,0,.36).

Conflict (a tripped trap, a warned arm, a refused drop): ice #4de3ff, never red. Red is the infection, so a warning is the cold opposite of it. The repel switch is indigo #4f5bff: cool and far from every red, and still not the ice, so a switch is never a cousin of the infection or of a warning.

Rule: gold appears only as points (pads, vias, mounting holes, HUD chip pads), never as lines.

## 3. Substrate (background)
`out/board_background.svg`, 390×844, layers in order:
1. Mask: linear gradient maskHi → mask (70%) → maskLo, 160°.
2. 24px grid, white 7%. 3. 12px grid, black 5%.
4. Sheen: radial, white 18% at (50%, 12%), r 50%.
5. Traces: margins only, tone-on-tone. 3px black 14% with a 1px white 10% highlight offset -1.5px. 45° bends. Trace ends: 4px black 18% dot with 1.6px white 35% center.
6. Mounting holes: four corners, r9 copper 70% over r5 #2a3a24.
7. Silkscreen: Share Tech Mono 9px, white 55%, letter-spacing 1.5. Top-left studio name, bottom-left copyright, bottom-right `GI-{LEVEL} REV B`.
8. Vignette: radial black 0% to 28% from r60% to r75%.

Trace routing is placeholder art. Production generates routing per level.

## 4. Board well
Centered, top 88px. Grid of 54px cells, 5px gap, 14px padding, radius 12. Fill rgba(0,0,0,.36). Inset 1px white 14%, inset 60px black 50% blur, outer 3px black 18% ring.

54px is the design cell, not a ceiling: the board takes whatever the band between the HUD and the tray allows, up to 72px. Everything else here scales with it.

## 5. Tile states (frosted glass)
All tiles radius 6. Material: backlit frosted glass. Light lives inside the tile.

| state | fill | shadow |
|---|---|---|
| out of bounds | black 5% | inset 1px black 10% |
| empty pad | radial copperHi r4.5 → copper r5.5 → transparent | none |
| component (placed, dormant) | linear 160°: white 34% → white 8% (55%) → white 16% | inset 0 1px white 60%; inset 1px white 25%; 0 7px 16px black 38% |
| component dot | 9px circle copperHi, glow 7px copperHi 90% | |
| infected | linear 160°: infectHi-tint white 90% → infect (55%) → infectLo | inset 0 1px white 85%; inset 1px white 40%; glow 26px infect; glow 64px infectGlow; 0 7px 16px black 38% |
| infecting (mid-spread) | component fill + radial infect from the entry edge, r16 solid → r26 glow → r38 clear | component shadow |
| pending trace (preview) | radial infectGlow → transparent 70% | inset 1px infect 50% |
| blocker | white 60% → white 20% | inset 0 1px white; inset 2px white 75%; 0 7px 16px black 38% |

Bleed direction: the infection enters a tile from the edge facing its source and pools across.

## 6. Bug glyph (nucleus grammar)
40×40 viewBox, center (20,20). Rendered at 44px on a 54px tile, 58px next-piece, 40px queued.

Core: hexagon 20,9 30,14.5 30,25.5 20,31 10,25.5 10,14.5. Fill infect, stroke glyphEdge 1.6. Gloss quad 20,11 28,15.5 20,20 12,15.5 white 40%. Center dot r3 white.

Orthogonal lead (N, E, S, W): rect 6×12 at (17,2) rx1 glyphEdge, tip rect 4×5 at (18,2) white. Two bond wires glyphWire 1px at x 14.5 and 25.5, straight y14→7, quadratic hook to y4.5 at x17/23. One branch stub per side leaving each bond wire outward at y7.5, length 3, pad r1.1.

Diagonal lead (NE, SE, SW, NW): line x20 y14→6, glyphEdge 2.4; tip circle r3 glyphEdge with r2 white center. The lit tip is the arm's only bright element and has to read at 40 px, so the white carries the disc and the edge is a thin ring around it.

Body stubs: every inactive orthogonal edge gets three stubs from y10.5 at offsets -4/0/+4, lengths 3.5/2.5/3.5, pads on the outer two. Each hex vertex between two inactive edges and not occupied by a diagonal lead gets one stub, length 2.5, no pad.

Area bug: no leads. Four outer arcs `M12 8 Q20 3 28 8` glyphEdge 2px and four inner arcs `M14.5 12 Q20 9 25.5 12` glyphWire 1.2px, rotated 0/90/180/270. Core unchanged. No rim dots: white belongs to lit lead tips, and outside the body it read as four stray highlights — the arcs already describe the 3×3 footprint.

Blocker: no leads, all body stubs, core fill blockerBody, stroke blockerEdge, shield mark replacing the center dot.

Invariant: lit tips are the only long bright elements. No stub or wire exceeds length 3.5.

Files: `out/glyphs/bug_<DIRS>.svg` (DIRS canonical order N E S W NE SE SW NW), `bug_AREA.svg`, `tile_BLOCKER.svg`, `glyph_sheet.svg` (8 per row, 44px pitch, each in `<g id>`).

## 7. HUD
Height 56px, items bottom-aligned: the two chips and the level label, nothing else. Level label Chakra Petch 26px ink color, 0.06em tracking. A second row at 52px from the top carries the Share Tech Mono 11px caption (`GI-REV B`) left-aligned under MENU and the counter badge right-aligned under RESET; the caption sits there rather than above the label so the HUD band costs one text line, not two. Buttons are glass chips: 12px 0.1em uppercase, padding 8×14, radius 7, glass fill white 42%→14%, one 5px copperHi pad on each side outside the chip. Solve counter: Share Tech Mono 13px copperHi on black 35%, radius 7, inset 1px copperHi 35%, sized for eight characters (`SOLVE 03`, `+1 SOLVE`).

## 8. Tray
Bottom 96px. Three slots, 30px gap. Next slot 74px radius 12, black 30% with inset 24px black 50%, plus infect glow 22px 45%, glyph at 58px, caption `NEXT` Share Tech Mono 10px ink 75%. Queued slots 54px at 75% opacity, glyph at 40px.

## 9. Motion
- Placement: bug lands, lead tips light in sequence, then infection propagates out of the tips.
- Spread: per-tile bleed from entry edge, one tile delay per hop along the wavefront. Sparks at the wavefront.
- Area bug: outer arcs expand on placement, then neighbors light.
- Undo: brief desaturation, reverse the bleed.
- Win: board powers up in placement order, glow intensity rises, then settles.
- Board shake on placement: 2px, 80ms.

## 10. Rendered sprite pipeline
Tiles and bug bodies share one frosted-glass material with an emissive channel. Leads, wires, stubs are opaque dark. Export two LODs from the same source: board sprite (44px glyph) and tray/hero sprite (58px+). Skin swap changes material color inputs only.

## 11. Logo (locked 2026-09-09)
Regenerate with `node gen-logo.mjs` (add `--png` for raster exports; needs playwright, see `tools/style-bench/README.md`). Source of truth is `gen-logo.mjs`; Chakra Petch Bold is vendored in `fonts/` (OFL) and outlined to paths, so the SVGs have no font dependency.

Concept: the wordmark is two components in two states on the board. GRID is dormant glass, INFECT is lit, and the bug between them is the source with its lit tip on the I. The logo is green-skin only; it does not reskin.

Wordmark (`out/logo/wordmark.svg`, transparent; `wordmark_board.svg` framed on the substrate for banners):
- Chakra Petch 700, 92 px, letter-spacing 2 px. GRID, a 96 px gap, INFECT. Bug is bug_E at 56 px centred in the gap on the x-height midline.
- GRID: dense glass. Linear 160°: white 66% → white 30% (55%) → white 44%. Rim white 90% 1.4 px. Drop shadow 0 5px blur 6 black 38%. Dense, not component density: the component fill (§5) vanishes below 48 px and a mark has to hold at 48.
- INFECT: lit. Linear 160°: white 92% → infectHi (12%) → infect (55%) → infectLo. Rim white 60% 1.3 px. Glow two passes, both infect: blur 14 at 75% and blur 7 at 50%. Tight passes, so the glow stays on the type.
- Bug: the glyph from §6 with a 35% infect backing glow, exactly as on a tile.

Monogram (`out/logo/monogram.svg`, 1024 square, no corner rounding; platforms mask it): the wordmark's first and fifth letters under the same rules. G dense glass at 70% of the side, lit I, bug_E between them at 27% of the side with its tip on the I. `monogram_adaptive.svg` is the same mark at 62% for Android adaptive-icon foregrounds. Raster sizes: 1024, 512, 192, 96, 48 and adaptive 1024, 432.

Title screen: the wordmark composites on the live substrate. Motion: GRID present at rest, the bug lands, tip lights, INFECT fills one letter per hop left to right, the same hop timing as board spread. Half-lit frames are animation only; the static mark is fully lit.

Rejected and why (all three rounds are in `reference/logo-rounds.html`): trace-drawn letters in the 2013 manner, disciplined to 45° routing (loses the glass system and reads as etched circuitry, not a mark); pixel-font letters as tiles on a board well (needs two lines, reads as a menu); silkscreen Chakra Petch (interchangeable with any title); dark-glass "well" G and solid-ink G for the monogram (dark glass reads solid but changes the concept from components to well, solid ink is paint).

