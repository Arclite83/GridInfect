# Style bench

Headless previews of the presentation layer, for a machine without Unity.
Both were used to verify the style pass (`grid-infect-style/STYLE-GUIDE.md`)
before the shaders had been compiled by an editor.

## Glyph sheet

Renders every bug glyph the game can draw through the real rasteriser
(`View/GlyphRaster.cs`, `View/BugGlyph.cs`) against a minimal fake
`UnityEngine`, to a PNG sheet laid out like `grid-infect-style/out/glyph_sheet.svg`.

```sh
cd tools/style-bench/glyphs
dotnet run -c Release -- 96 sheet.png            # 96 px glyphs, well-coloured background
dotnet run -c Release -- 72 glyphs72.png Blue alpha   # blue skin, transparent (for the board bench)
```

## Board bench

`port.py` ports the three `.shader` files to GLSL ES 3.00 mechanically;
`bench.html` draws a 390 x 844 screen at 2x (substrate, board, chips, badge,
tray) with a sample board state and the glyph sheet overlaid; `run.mjs`
screenshots it mid-wave and settled.

```sh
cd tools/style-bench
python3 port.py                                   # -> shaders.js
(cd glyphs && dotnet run -c Release -- 72 ../glyphs72.png Default alpha && dotnet run -c Release -- 96 ../glyphs96.png Default alpha)
node run.mjs                                      # needs playwright + a chromium; CHROMIUM=/path to override
```

No bloom and no tonemapping in the bench, so anything the board pushes past
1.0 (a freshly lit tile, the beam) clips to white here and reads as the hot
pink Unity's Neutral curve gives it.
