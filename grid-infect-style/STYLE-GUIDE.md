# Grid Infect visual style guide (locked 2026-09-04)

Everything below is the decision. Regenerate vector assets with `node gen-assets.mjs out`.

## 1. Concept
Bugs on a printed circuit board. The board is the substrate, components are tiles, the infection is light inside the components, the bug is a component-shaped glyph. The infection glow is the only strong emissive element on screen.

## 2. Palette and skins
Mask color plus infection hue is a skin layer. Everything else is constant. Values in `out/tokens.json`.

| skin | mask | copper | infection | note |
|---|---|---|---|---|
| default | green #7fae66 | gold #c9a648 | magenta #ff2d95 | ship default |
| blue | #2e5aa8 | gold | amber #ff8a00 | unlockable |
| breadboard | cream #e9dcb8 | bare copper #c46a3a | red #ff2d3a | unlockable |

Neutrals: lit tips and highlights #ffffff, blocker body #cfd8e0, blocker edge #4d565f, board well rgba(0,0,0,.36).

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
Centered, top 138px. Grid of 54px cells, 5px gap, 14px padding, radius 12. Fill rgba(0,0,0,.36). Inset 1px white 14%, inset 60px black 50% blur, outer 3px black 18% ring.

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

Diagonal lead (NE, SE, SW, NW): line x20 y14→6, glyphEdge 2.4; tip circle r2.6 glyphEdge with r1.3 white center.

Body stubs: every inactive orthogonal edge gets three stubs from y10.5 at offsets -4/0/+4, lengths 3.5/2.5/3.5, pads on the outer two. Each hex vertex between two inactive edges and not occupied by a diagonal lead gets one stub, length 2.5, no pad.

Area bug: no leads. Four outer arcs `M12 8 Q20 3 28 8` glyphEdge 2px, four inner arcs `M14.5 12 Q20 9 25.5 12` glyphWire 1.2px, four rim dots r1.6 white at (20,3.5), all rotated 0/90/180/270. Core unchanged.

Blocker: no leads, all body stubs, core fill blockerBody, stroke blockerEdge, shield mark replacing the center dot.

Invariant: lit tips are the only long bright elements. No stub or wire exceeds length 3.5.

Files: `out/glyphs/bug_<DIRS>.svg` (DIRS canonical order N E S W NE SE SW NW), `bug_AREA.svg`, `tile_BLOCKER.svg`, `glyph_sheet.svg` (8 per row, 44px pitch, each in `<g id>`).

## 7. HUD
Height 96px, items bottom-aligned. Level label Chakra Petch 26px ink color, 0.06em tracking, with a Share Tech Mono 11px caption above (`GI-REV B`). Buttons are glass chips: 12px 0.1em uppercase, padding 8×14, radius 7, glass fill white 42%→14%, one 5px copperHi pad on each side outside the chip. Lock counter: Share Tech Mono 13px copperHi on black 35%, radius 7, inset 1px copperHi 35%.

## 8. Tray
Bottom 150px. Three slots, 30px gap. Next slot 74px radius 12, black 30% with inset 24px black 50%, plus infect glow 22px 45%, glyph at 58px, caption `NEXT` Share Tech Mono 10px ink 75%. Queued slots 54px at 75% opacity, glyph at 40px.

## 9. Motion
- Placement: bug lands, lead tips light in sequence, then infection propagates out of the tips.
- Spread: per-tile bleed from entry edge, one tile delay per hop along the wavefront. Sparks at the wavefront.
- Area bug: outer arcs expand on placement, then neighbors light.
- Undo: brief desaturation, reverse the bleed.
- Win: board powers up in placement order, glow intensity rises, then settles.
- Board shake on placement: 2px, 80ms.

## 10. Rendered sprite pipeline
Tiles and bug bodies share one frosted-glass material with an emissive channel. Leads, wires, stubs are opaque dark. Export two LODs from the same source: board sprite (44px glyph) and tray/hero sprite (58px+). Skin swap changes material color inputs only.
