# Grid Infect visual style guide

Everything below is the decision. Regenerate vector assets with `node gen-assets.mjs out`, the logo with `node gen-logo.mjs`, store media with `node gen-media.mjs`. Values live in `out/tokens.json`; the game mirrors them in `View/BoardPalette.cs` (colour) and `PresentationConfig.Style` (sizes).

## 1. Concept
Bugs on a printed circuit board. The board is the substrate, components are tiles, the infection is light inside the components, the bug is a component-shaped glyph. The infection glow is the only strong emissive element on screen.

## 2. Palette and skins
A skin is the mask colour, the copper and the infection hue. Everything else is constant. Three skins ship; `reference/skins.html` renders them on the same board.

| skin | mask | copper | infection | ink |
|---|---|---|---|---|
| default | green #7fae66 | gold #c9a648 | deep rose red #d9204f | #0c190a |
| blue | #2e5aa8 | gold #d9a441 | amber #ff8a00 | #e6efff |
| breadboard | cream #e9dcb8 | bare copper #c46a3a | lime #7fd100 | #3c2e12 |

Each of mask, copper and infection has a highlight and a shadow stop (`tokens.json`). Red is the infection, and infection is success: the default skin's red is the one hue with that meaning, so nothing else on screen is red. Breadboard uses lime because red on cream reads as paint.

The glass of a dormant component is white at low alpha on the two dark masks. Breadboard's mask is lighter than white glass would be, so its component glass is a dark recess (#4a3a22) instead. `BoardPalette.Space` carries this.

Neutrals: lit tips and highlights #ffffff, blocker body #cfd8e0, blocker edge #4d565f, board well black 36%, shadows black.

Warning and switch colours, the same on every skin:
- Conflict (a tripped trap, a warned arm, a refused drop): ice #4de3ff. A warning is the cold opposite of the infection, never red.
- Repel switch: indigo #4f5bff. Cool, far from red, and not the ice, so a switch is never mistaken for infection or for a warning.
- Reset trap: near-black #0d0d12.

Each of these is a tint on the component glass plus a shape glyph (diamond, X). Colour is never the only cue.

Rule: copper appears only as points (pads, vias, mounting holes, HUD chip pads), never as lines.

## 3. Substrate (background)
`out/board_background.svg`, 390×844, layers in order:
1. Mask: linear gradient maskHi → mask (70%) → maskLo, 160°.
2. 24px grid, white 7%. 3. 12px grid, black 5%.
4. Sheen: radial, white 18% at (50%, 12%), r 50%.
5. Traces: margins only, tone-on-tone. 3px black 14% with a 1px white 10% highlight offset -1.5px. 45° bends. Trace ends: 4px black 18% dot with 1.6px white 35% centre.
6. Mounting holes: four corners, r9 copper 70% over r5 dark.
7. Silkscreen: Share Tech Mono 9px, white 55%, letter-spacing 1.5. Top-left studio name, bottom-left copyright, bottom-right `GI-{LEVEL}`, the board's part number. Blank on a screen with no board.
8. Vignette: radial black 0% to 28% from r60% to r75%.

Trace routing in the SVG is placeholder art. The game generates routing per level.

## 4. Board well
Centred, top 88px. Grid of 54px cells, 5px gap, 14px padding, radius 12. Fill black 36%. Inset 1px white 14%, inset 60px black 50% blur, outer 3px black 18% ring.

54px is the design cell, not a ceiling: the board takes whatever the band between the HUD and the tray allows, up to 72px. Everything else scales with it.

## 5. Tile states (frosted glass)
All tiles radius 6. Material: backlit frosted glass. Light lives inside the tile.

| state | fill | shadow |
|---|---|---|
| out of bounds | black 5% | inset 1px black 10% |
| empty pad | radial copperHi r4.5 → copper r5.5 → transparent | none |
| component (placed, dormant) | linear 160°: glass 34% → glass 8% (55%) → glass 16%, glass being white or the breadboard recess | inset 0 1px white 60%; inset 1px white 25%; 0 7px 16px black 38% |
| component dot | 9px circle copperHi, glow 7px copperHi 90% | |
| infected | linear 160°: infectHi leaned 45% toward white at 90% → infect (55%) → infectLo | inset 0 1px white 85%; inset 1px white 40%; glow 26px infect; glow 64px infectGlow; 0 7px 16px black 38% |
| infecting (mid-spread) | component fill + radial infect from the entry edge, r16 solid → r26 glow → r38 clear | component shadow |
| pending trace (preview) | radial infectGlow → transparent 70% | inset 1px infect 50% |
| blocker | white 60% → white 20% | inset 0 1px white; inset 2px white 75%; 0 7px 16px black 38% |

Bleed direction: the infection enters a tile from the edge facing its source and pools across.

## 6. Bug glyph
40×40 viewBox, centre (20,20). Rendered at 44px on a 54px tile, 58px next-piece, 40px queued.

Core: hexagon 20,9 30,14.5 30,25.5 20,31 10,25.5 10,14.5. Fill infect, stroke glyphEdge 1.6. Gloss quad 20,11 28,15.5 20,20 12,15.5 white 40%. Centre dot r3 white.

Orthogonal lead (N, E, S, W): rect 6×12 at (17,2) rx1 glyphEdge, tip rect 4×5 at (18,2) white. Two bond wires glyphWire 1px at x 14.5 and 25.5, straight y14→7, quadratic hook to y4.5 at x17/23. One branch stub per side leaving each bond wire outward at y7.5, length 3, pad r1.1.

Diagonal lead (NE, SE, SW, NW): line x20 y14.5→6.5, glyphEdge 2.6, wired like an orthogonal lead: two bond wires glyphWire 1px at x 17.6 and 22.4, straight y13.5→8.5, quadratic hook to y6.6 at x18.6/21.4; one branch stub per side leaving each wire outward at y9.5, length 3, pad r1.1. Tip circle r3.2 glyphEdge with r2.1 white centre, drawn last. The white carries the tip so it reads at 40px; the wiring is what makes it a lead rather than a stray highlight.

Body stubs: every inactive orthogonal edge gets three stubs from y10.5 at offsets -4/0/+4, lengths 3.5/2.5/3.5, pads on the outer two. An outer stub is dropped where an active diagonal lead sits beside it (the +4 stub for the lead 45° clockwise, the -4 stub for the one anticlockwise): the lead's branch pad lands on that spot. Each hex vertex between two inactive edges and not occupied by a diagonal lead gets one stub, length 2.5, no pad.

Area bug: no leads. Four outer arcs `M12 8 Q20 3 28 8` glyphEdge 2px and four inner arcs `M14.5 12 Q20 9 25.5 12` glyphWire 1.2px, rotated 0/90/180/270. Core unchanged. No rim dots: white belongs to lit lead tips.

Blocker: no leads, all body stubs, core fill blockerBody, stroke blockerEdge, shield mark replacing the centre dot.

Invariant: lit tips are the only long bright elements. No stub or wire exceeds length 3.5.

Files: `out/glyphs/bug_<DIRS>.svg` (DIRS canonical order N E S W NE SE SW NW), `bug_AREA.svg`, `tile_BLOCKER.svg`, `glyph_sheet.svg` (8 per row, 44px pitch, each in `<g id>`).

## 7. HUD
Height 56px, items bottom-aligned: the two chips and the level label. The band is measured from the top of the device's safe area rather than the screen edge, and the badge row and board well keep their distances below it, so the HUD clears a camera cutout; every other screen's top bar (0.08 of the height from the top) drops below the safe area too when a cutout is deeper than that. Level label Chakra Petch 26px in ink, 0.06em tracking. A second row at 52px from the top carries the Share Tech Mono 12px caption (the mode readout: `LEGACY`, `DAILY`, `ENDLESS T3`) left-aligned under MENU and the counter badge right-aligned under RESET.

Buttons are glass chips: 12px 0.1em uppercase at weight 700, padding 8×14, radius 7, glass fill white 42%→14%, one 5px copperHi pad on each side outside the chip. The one lit chip on a screen (BEGIN, PLAY) is the infection with its top stop leaned 12% toward white. Solve counter: Share Tech Mono 13px copperHi on black 60%, radius 7, inset 1px copperHi 50%, sized for eight characters (`SOLVE 03`, `+1 SOLVE`). Icon chips (gear, help, pager chevrons) are 39px squares carrying a drawn mark at 72% of the chip, never a font glyph.

## 8. Tray
Bottom 96px. Three slots, 30px gap. Next slot 74px radius 12, black 30% with inset 24px black 50%, plus infect glow 22px 45%, glyph at 58px, caption `NEXT` Share Tech Mono 11px ink. Queued slots 54px at 75% opacity, glyph at 40px.

## 9. Motion
- Placement: bug lands, lead tips light in sequence, then infection propagates out of the tips.
- Spread: per-tile bleed from entry edge, one tile delay per hop along the wavefront. Sparks at the wavefront.
- Area bug: outer arcs expand on placement, then neighbours light.
- Undo: brief desaturation, reverse the bleed.
- Win: board powers up in placement order, glow intensity rises, then settles.
- Board shake on placement: 2px, 80ms.

## 10. Rendered sprite pipeline
Tiles and bug bodies share one frosted-glass material with an emissive channel. Leads, wires, stubs are opaque dark. Two sizes from the same source: board sprite (44px glyph) and tray/hero sprite (58px+). A skin swap changes material colour inputs only.

## 11. Legibility floor
The board is meant to be read on a phone in daylight, in a moving vehicle, by someone who does not see colour the way the palette assumes. Contrast is WCAG 2.x, measured against the composited background the element sits on (mask gradient, chip glass, well).

| rule | value |
|---|---|
| Informational type | ≥ 4.5:1 under 24px; ≥ 3:1 at 24px and up. Ink clears 4.5:1 on the mask shadow of every skin |
| Dimmed type | none. Hierarchy is size and the mono face, never alpha |
| Disabled controls | ink 45%, the one exempt state (a pager at its end, TODAY while today shows) |
| Decorative silkscreen | 9px white 55%: studio name, copyright, part number. Decorative, exempt |
| Type size | ≥ 11px reference for anything that says something; body 14.4px (0.037 of the short edge); silkscreen alone at 9 |
| Chip labels | weight 700. Thin uppercase on glass is the first thing to go in glare |
| Copper on the badge | black 60% under it: 8:1 for the 13px counter |
| White on the lit chip | top stop leaned 12% toward white, which keeps the label above 4.5:1 |
| Colour is never the only cue | infected carries the white dot and the fill, repel the diamond, trap the X, avoid the copper ring, wall the shield, solved the tick, locked the padlock |
| Touch targets | every pressable chip answers a 44×44px reference square about its centre (`UiButton.HitBounds`); top-bar chips 33px tall, icon chips 39px |
| UI marks | drawn shapes, never font glyphs; stroke ≥ 4.4 viewBox units in a chip, ≥ 2.6 over a glyph; the relay's leads 3.0 with a pad |

Not done: reduce-motion switch, text scaling with the OS setting, screen-reader labels. Each is a settings or platform pass.

## 12. Logo
Regenerate with `node gen-logo.mjs` (add `--png` for raster exports; needs playwright, see `tools/style-bench/README.md`). Source of truth is `gen-logo.mjs`; Chakra Petch Bold is vendored in `fonts/` (OFL) and outlined to paths, so the SVGs have no font dependency.

Concept: the wordmark is two components in two states on the board. GRID is dormant glass, INFECT is lit, and the bug between them is the source with its lit tip on the I. The logo is green-skin only; it does not reskin.

Wordmark (`out/logo/wordmark.svg`, transparent; `wordmark_board.svg` framed on the substrate for banners):
- Chakra Petch 700, 92px, letter-spacing 2px. GRID, a 96px gap, INFECT. Bug is bug_E at 56px centred in the gap on the x-height midline.
- GRID: dense glass. Linear 160°: white 66% → white 30% (55%) → white 44%. Rim white 90% 1.4px. Drop shadow 0 5px blur 6 black 38%. Denser than a component tile so the mark holds at 48px.
- INFECT: lit. Linear 160°: white 92% → infectHi (12%) → infect (55%) → infectLo. Rim white 60% 1.3px. Glow two passes, both infect: blur 14 at 75% and blur 7 at 50%.
- Bug: the glyph from §6 with a 35% infect backing glow, exactly as on a tile.

Monogram (`out/logo/monogram.svg`, 1024 square): the wordmark's first and fifth letters under the same rules. G dense glass at 70% of the side, lit I, bug_E between them at 27% of the side with its tip on the I. A press mark, not the app icon. Raster sizes: 1024, 512.

Title screen: the wordmark composites on the live substrate. Motion, once per launch: GRID present at rest, the bug lands 0.25s in over 0.12s from 1.6× its size, then each INFECT letter fades from dense to lit over two hops, one hop apart, the same hop timing as board spread. The static mark is fully lit.

In the build: `gen-logo.mjs --cs` emits the letter outlines to `unity/.../View/LogoGlyphs.g.cs`; `View/TitleRaster.cs` draws each letter through the glyph rasteriser and `View/TitleView.cs` lays them out to the content width. No font and no bitmap. `tools/style-bench/glyphs` renders the same mark headlessly (`dotnet run -- title <scale> out.png [dormant]`) for checking against `out/logo/wordmark.svg`.

App icon: a board tile with bug_E on it. The game's most distinctive shape and nothing else, so it reads as the game rather than as a name, and it holds at 29px.
- `icon.svg`: tile at 72% of the side, corner radius 8%, dense glass with the 90% white rim and the drop shadow; bug_E at 58% of the side with its 35% infect backing glow, as on a board. Mask one stop darker than the board's for the icon only (`ICON_SKIN`: maskHi = mask, mask = maskLo, maskLo = #456a35), so the glass separates on a light wallpaper. Raster 1024 (App Store, no alpha), 512 (Play), 192, 96, 48.
- `icon_adaptive.svg`: the same mark at 62% for Android's foreground; `icon_bg.svg` is the substrate alone. Raster 1024 and 432.
- `icon_mono.svg`: the bug as a flat white silhouette at the adaptive inset (hexagon with a hollow core, the E lead, three pins on each other side) on transparent, for Android themed icons and iOS tinted mode. The launcher supplies the colour. Raster 1024 and 432.
- The copies under `unity/Assets/_Project/Art/Icon/` (`icon_1024`, `icon_adaptive_fg_432`, `icon_adaptive_bg_432`, `icon_adaptive_mono_432`) are what `Editor/MobileBuild.cs` assigns; regenerate here and copy (`gen-media.mjs --install`).

Earlier logo and icon candidates are kept in `reference/logo-rounds.html` and `out/icon-rounds/` for the record.

### 12.1 Store and press media
Regenerate with `node gen-media.mjs` (needs the playwright devDependency and a Chromium; `CHROMIUM=` and `PLAYWRIGHT=` as in `tools/style-bench/run.mjs`). `media.manifest.json` is the only place a store dimension, format, alpha rule or upload cap is written; `--check` reads it back against `out/store/` and fails on any missing file, wrong size, wrong alpha channel or oversize file; `--only <id>` renders one; `--install` copies the four icon files into `unity/Assets/_Project/Art/Icon/`. Output is deterministic and git-ignored; raw device captures for screenshot composites go under `out/raw/<device>/<n>.png`, also ignored. Captures come from a device or the editor with the game's bloom and tonemap on, never from `tools/style-bench`, which has neither, so its lit tiles clip brighter than the game draws them.

Icons are drawn through `gen-logo.mjs`'s `icon()`, `iconBackground()` and `tileMark()`/`tileMonoMark()`, so a change of numbers there changes both the reference rasters and the store files:
- `icon_main`: `icon()`, 1024 for the App Store and Unity's `icon_1024.png`; `icon_play` is the same at 512 (Play caps it at 1 MB, and both stores reject an alpha channel).
- `icon_fg`: the tile at the adaptive 62% inset on a transparent ground, 432px. The store file carries alpha so the launcher's parallax shows the background layer.
- `icon_bg`: `iconBackground()`, 432px, opaque.
- `icon_mono`: `iconMono()`, 432px, alpha.

Media compositions: `feature_graphic` (Play 1024×500, wordmark at 76% of the width, clear of the store's edge overlays), `banner_wide` (press 1920×640, wordmark at 58%), `social_header` (1500×500, wordmark at 52%, sitting high so a profile avatar clears it), all on the substrate at the framed wordmark's scale. `screenshot_frame` takes a raw capture and composites it on the substrate at the screen's scale, framed like the board well (§4), under a caption in Chakra Petch Bold at the HUD level size in ink, letter-spaced as the wordmark. Captions in the manifest are placeholders until the store copy is written.
