# Grid Infect visual style guide (locked 2026-09-04, chrome retuned 2026-09-05, legibility floor 2026-09-09)

Everything below is the decision. Regenerate vector assets with `node gen-assets.mjs out`.

## 1. Concept
Bugs on a printed circuit board. The board is the substrate, components are tiles, the infection is light inside the components, the bug is a component-shaped glyph. The infection glow is the only strong emissive element on screen.

## 2. Palette and skins
Mask color plus infection hue is a skin layer. Everything else is constant. Values in `out/tokens.json`.

| skin | mask | copper | infection | note |
|---|---|---|---|---|
| default | green #7fae66 | gold #c9a648 | deep rose red #d9204f | ship default. Red is infection, and infection is success. Ink #0c190a (was #1d3316: 3.4:1 on the mask shadow; now 4.5:1, §11) |
| blue | #2e5aa8 | gold | amber #ff8a00 | unlockable |
| breadboard | cream #e9dcb8 | bare copper #c46a3a | red #ff2d3a | unlockable |

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
Height 56px, items bottom-aligned: the two chips and the level label, nothing else. Level label Chakra Petch 26px ink color, 0.06em tracking. A second row at 52px from the top carries the Share Tech Mono 12px caption (`GI-REV B`) left-aligned under MENU and the counter badge right-aligned under RESET; the caption sits there rather than above the label so the HUD band costs one text line, not two. Buttons are glass chips: 12px 0.1em uppercase at weight 700, padding 8×14, radius 7, glass fill white 42%→14%, one 5px copperHi pad on each side outside the chip. The one lit chip (BEGIN, PLAY) is the infection with its top stop leaned 12% toward white, not 35% (§11). Solve counter: Share Tech Mono 13px copperHi on black 60%, radius 7, inset 1px copperHi 50%, sized for eight characters (`SOLVE 03`, `+1 SOLVE`). Icon chips (gear, help, pager chevrons) are 39px squares carrying a drawn mark at 72% of the chip, never a font glyph.

## 8. Tray
Bottom 96px. Three slots, 30px gap. Next slot 74px radius 12, black 30% with inset 24px black 50%, plus infect glow 22px 45%, glyph at 58px, caption `NEXT` Share Tech Mono 11px ink. Queued slots 54px at 75% opacity, glyph at 40px.

## 9. Motion
- Placement: bug lands, lead tips light in sequence, then infection propagates out of the tips.
- Spread: per-tile bleed from entry edge, one tile delay per hop along the wavefront. Sparks at the wavefront.
- Area bug: outer arcs expand on placement, then neighbors light.
- Undo: brief desaturation, reverse the bleed.
- Win: board powers up in placement order, glow intensity rises, then settles.
- Board shake on placement: 2px, 80ms.

## 10. Rendered sprite pipeline
Tiles and bug bodies share one frosted-glass material with an emissive channel. Leads, wires, stubs are opaque dark. Export two LODs from the same source: board sprite (44px glyph) and tray/hero sprite (58px+). Skin swap changes material color inputs only.

## 11. Legibility floor (2026-09-09)
The board is meant to be read on a phone in daylight, in a moving vehicle, by someone who does not see colour the way the palette assumes. Everything above stays; these are the limits it has to clear. Measured as WCAG 2.x contrast against the composited background the element actually sits on (the mask gradient, the chip glass over it, the well), sRGB blend as the glass shader does it.

| rule | value | why |
|---|---|---|
| Informational type | ≥ 4.5:1 at any size under 24px; ≥ 3:1 at 24px and up | ink #1d3316 was 5.3:1 on the mask and 3.4:1 on the mask shadow; #0c190a is 7.0 and 4.5 |
| Dimmed type | none. Hierarchy is size and the mono face, never alpha | ink at 70% was 3.1:1 on the mask, 2.4 on its shadow; it carried the HUD mode readout and every page counter |
| Disabled controls | ink 45%, the one exempt state (a pager at its end, TODAY while today shows) | WCAG exempts an inactive control; the dim is the message |
| Decorative silkscreen | 9px white 55% stays | studio name, copyright, `GI-06 REV B`: decorative, exempt |
| Type size | ≥ 11px reference for anything that says something; body 14.4px (0.037 of the short edge) | 10px weekday names, 10px tray caption and the 11px HUD caption all rise; the silkscreen alone stays at 9 |
| Chip labels | weight 700 | 12px uppercase on glass is where thin type goes first in glare |
| Copper on the badge | black 60% under it | black 35% put 13px copperHi at 4.3:1 (3.5 on the mask highlight); 60% is 8:1. A zero streak is no longer dimmed to 2:1 |
| White on the lit chip | top stop lerp(infect, white, .12), not .35 | .35 was 3.0:1 at the top of the chip and 4.0 at the label; .12 clears 4.5 |
| Colour is never the only cue (R-1001) | unchanged: infected carries the white dot and the fill, repel the diamond, trap the X, avoid the copper ring, wall the shield, solved the tick, locked the padlock | |
| Touch targets | every pressable chip answers a 44×44px reference square about its centre (`UiButton.HitBounds`), whatever it draws; top-bar chips 33px tall, icon chips 39px | 29px chips were under both the Android 48dp and iOS 44pt guidance |
| UI marks | drawn shapes, never font glyphs; stroke ≥ 4.4 viewBox units in a chip, ≥ 2.6 over a glyph; the relay's leads 3.0 with a pad, copper points in the hub and pads | "?" was the label face at 12px; "◀" is not in Chakra Petch; the relay's 1.4-unit stubs were a 1.5px line on the board |

Not done, and known: no reduce-motion switch (R-1004), no text scaling with the OS setting (TextMesh, R-1003), no screen-reader labels. Each is a settings or platform pass, not a chrome pass.

