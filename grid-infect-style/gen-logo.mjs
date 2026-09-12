#!/usr/bin/env node
// Grid Infect logo generator (logo lock 2026-09-09, STYLE-GUIDE §11).
// node gen-logo.mjs [outDir] [--png] [--cs]
//   outDir defaults to out/logo next to this file.
//   --png also rasterises through Chromium: playwright if installed (PLAYWRIGHT=),
//         else the binary's own --screenshot (CHROMIUM=, default the Playwright
//         headless shell under /opt/pw-browsers).
//   --cs  also emits the letter outlines and the wordmark's layout as C#
//         (unity/Assets/_Project/Game/View/LogoGlyphs.g.cs), which is what
//         the title screen draws from at runtime: same font, same numbers,
//         no font or bitmap in the build (View/TitleRaster.cs).
// Green skin only. The logo does not reskin.
import { mkdirSync, writeFileSync, readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import opentype from "opentype.js";
import { bug, tokens } from "./gen-assets.mjs";

const here = dirname(fileURLToPath(import.meta.url));
// gen-media.mjs and gen-icon-rounds.mjs import the materials, substrate and
// marks from here; argv is only read, and out/logo only created, when this
// file is the entry.
const MAIN = process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href;
const args = MAIN ? process.argv.slice(2) : [];
const PNG = args.includes("--png");
const CS = args.includes("--cs");
const OUT = args.find(a => !a.startsWith("--")) ?? join(here, "out", "logo");
if (MAIN) mkdirSync(OUT, { recursive: true });

export const S = tokens.skins.default;
const fontBuf = readFileSync(join(here, "fonts", "ChakraPetch-Bold.ttf"));
export const font = opentype.parse(fontBuf.buffer.slice(fontBuf.byteOffset, fontBuf.byteOffset + fontBuf.byteLength));

// ---------- type ----------
export const FS = 92, LS = 2;            // wordmark size and letter-spacing, px
const opt = { kerning: true, letterSpacing: LS / FS };
export const advance = (t, fs = FS) => font.getAdvanceWidth(t, fs, { ...opt, letterSpacing: LS / FS }) ;
export const glyphs  = (t, x, y, fs = FS, ls = LS) => font.getPath(t, x, y, fs, { kerning: true, letterSpacing: ls / fs }).toPathData(2);

// ---------- materials ----------
// k prefixes ids so several marks can share one HTML document.
export function defs(k, sc = 1, S = tokens.skins.default) {
  return `<defs>
<linearGradient id="${k}dense" x1="0" y1="0" x2=".3" y2="1"><stop offset="0" stop-color="#fff" stop-opacity=".66"/><stop offset=".55" stop-color="#fff" stop-opacity=".30"/><stop offset="1" stop-color="#fff" stop-opacity=".44"/></linearGradient>
<linearGradient id="${k}lit" x1="0" y1="0" x2=".3" y2="1"><stop offset="0" stop-color="#fff" stop-opacity=".92"/><stop offset=".12" stop-color="${S.infectHi}"/><stop offset=".55" stop-color="${S.infect}"/><stop offset="1" stop-color="${S.infectLo}"/></linearGradient>
<linearGradient id="${k}mask" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${S.maskHi}"/><stop offset=".7" stop-color="${S.mask}"/><stop offset="1" stop-color="${S.maskLo}"/></linearGradient>
<filter id="${k}drop" x="-10%" y="-20%" width="120%" height="150%"><feDropShadow dx="0" dy="${5*sc}" stdDeviation="${6*sc}" flood-color="#000" flood-opacity=".38"/></filter>
<filter id="${k}glowBig" x="-20%" y="-40%" width="140%" height="180%"><feGaussianBlur stdDeviation="${14*sc}"/></filter>
<filter id="${k}glow2" x="-20%" y="-40%" width="140%" height="180%"><feGaussianBlur stdDeviation="${7*sc}"/></filter>
<filter id="${k}bugGlow" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="6"/></filter>
</defs>`;
}
export const dense = (k, d, sw) => `<g filter="url(#${k}drop)"><path d="${d}" fill="url(#${k}dense)" stroke="rgba(255,255,255,.9)" stroke-width="${sw}" stroke-linejoin="round"/></g>`;
export const lit   = (k, d, sw) => `<path d="${d}" fill="${S.infect}" filter="url(#${k}glowBig)" opacity=".75"/><path d="${d}" fill="${S.infect}" filter="url(#${k}glow2)" opacity=".5"/><path d="${d}" fill="url(#${k}lit)" stroke="rgba(255,255,255,.6)" stroke-width="${sw}" stroke-linejoin="round"/>`;
export const bugAt = (k, x, y, size) => `<g transform="translate(${x - size/2} ${y - size/2}) scale(${size/40})"><rect x="6" y="6" width="28" height="28" rx="6" fill="${S.infect}" opacity=".35" filter="url(#${k}bugGlow)"/>${bug(["E"])}</g>`;

// ---------- board substrate (for the framed wordmark and the monogram) ----------
export function substrate(k, W, H, pitch, holes, label, S = tokens.skins.default) {
  let s = `<pattern id="${k}g1" width="${pitch}" height="${pitch}" patternUnits="userSpaceOnUse"><path d="M${pitch} 0 H0 V${pitch}" fill="none" stroke="rgba(255,255,255,.07)" stroke-width="${pitch/24}"/></pattern>
<pattern id="${k}g2" width="${pitch/2}" height="${pitch/2}" patternUnits="userSpaceOnUse"><path d="M${pitch/2} 0 H0 V${pitch/2}" fill="none" stroke="rgba(0,0,0,.05)" stroke-width="${pitch/24}"/></pattern>
<radialGradient id="${k}sheen" cx=".5" cy=".1" r=".6"><stop offset="0" stop-color="#fff" stop-opacity=".16"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>`;
  s = `<defs>${s}</defs><rect width="${W}" height="${H}" fill="url(#${k}mask)"/><rect width="${W}" height="${H}" fill="url(#${k}g1)"/><rect width="${W}" height="${H}" fill="url(#${k}g2)"/><rect width="${W}" height="${H}" fill="url(#${k}sheen)"/>`;
  if (holes) s += [[22,22],[W-22,22],[22,H-22],[W-22,H-22]].map(([x,y]) => `<circle cx="${x}" cy="${y}" r="9" fill="${S.copper}" opacity=".7"/><circle cx="${x}" cy="${y}" r="5" fill="#2a3a24"/>`).join("")
    + `<g font-family="Share Tech Mono, monospace" font-size="9" fill="rgba(255,255,255,.55)" letter-spacing="1.5"><text x="44" y="30">BLOODHOUND STUDIOS</text><text x="${W-44}" y="${H-14}" text-anchor="end">${label}</text></g>`;
  return s;
}

// ---------- wordmark ----------
const GAP = 96, BUG = 56;
const wG = advance("GRID"), wI = advance("INFECT");
const total = wG + GAP + wI;
export function wordmark({ framed = false } = {}) {
  const k = framed ? "wb" : "w";
  const W = framed ? 880 : Math.ceil(total + 80), H = framed ? 260 : 160;
  const x0 = (W - total) / 2, mid = H / 2, base = mid + FS * 0.36;
  let s = defs(k);
  if (framed) s += substrate(k, W, H, 24, true, "GI-LOGO REV A");
  s += dense(k, glyphs("GRID", x0, base), 1.4);
  s += lit(k, glyphs("INFECT", x0 + wG + GAP, base), 1.3);
  s += bugAt(k, x0 + wG + GAP / 2, mid, BUG);
  // inner is the same content without the <svg> wrapper, for compositing (gen-media.mjs).
  return { svg: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${W} ${H}" id="${framed ? "wordmark_board" : "wordmark"}">\n${s}\n</svg>\n`, inner: s, W, H };
}

// ---------- monogram ----------
export function monogram({ side = 1024, adaptive = false } = {}) {
  const k = adaptive ? "ma" : "m", s0 = side;
  const sc = s0 / 168;                       // filters were tuned on the 168 px comp
  let s = defs(k, sc) + substrate(k, s0, s0, s0 / 8, false);
  const fs = s0 * 0.7, base = s0 * 0.5 + fs * 0.36;
  let mark = dense(k, glyphs("G", s0 * 0.03, base, fs, 0), s0 * 0.009)
           + lit(k, glyphs("I", s0 * 0.745, base, fs, 0), s0 * 0.009)
           + bugAt(k, s0 * 0.615, s0 * 0.5, s0 * 0.27);
  if (adaptive) mark = `<g transform="translate(${s0 * 0.19} ${s0 * 0.19}) scale(.62)">${mark}</g>`;
  s += mark;
  return { svg: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${s0} ${s0}" id="${adaptive ? "monogram_adaptive" : "monogram"}">\n${s}\n</svg>\n`, inner: s, W: s0, H: s0 };
}

// The adaptive icon's background layer: the monogram's substrate with no
// mark on it. Android composites monogram_adaptive over this and masks the
// pair, so the two are rendered from the same numbers.
export function monogramBackground({ side = 1024 } = {}) {
  const k = "mb";
  const s = defs(k, side / 168) + substrate(k, side, side, side / 8, false);
  return { svg: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${side} ${side}" id="monogram_bg">\n${s}\n</svg>\n`, inner: s, W: side, H: side };
}

// ---------- app icon (round 4, 2026-09-12: the tile) ----------
// The icon is a board tile with bug_E on it, not the monogram: at 48 px the
// monogram's dense G vanished into the mask (out/icon-rounds/sheet.png). The
// mask is one stop darker than the board's for the icon only.
export const ICON_SKIN = { ...S, maskHi: S.mask, mask: S.maskLo, maskLo: "#456a35" };
export const TILE = { side: 0.72, radius: 0.08, bug: 0.58, mono: 0.66 };

// The tile and its bug, in a side×side box. Dense glass as a placed piece.
export function tileMark(k, side) {
  const t = side * TILE.side, r = side * TILE.radius, x = (side - t) / 2;
  return `<g filter="url(#${k}drop)"><rect x="${x}" y="${x}" width="${t}" height="${t}" rx="${r}" fill="url(#${k}dense)" stroke="rgba(255,255,255,.9)" stroke-width="${side * 0.008}"/></g>`
       + bugAt(k, side / 2, side / 2, side * TILE.bug);
}
// The bug as a flat white silhouette: hexagon with a hollow core, the E lead,
// the pins. Android themed icons and iOS tinted mode recolour it.
export function tileMonoMark(side) {
  const b = side * TILE.mono;
  const pin = (a, t) => `<line transform="rotate(${a} 20 20) translate(${t} 0)" x1="20" y1="14" x2="20" y2="6" stroke="#fff" stroke-width="1.6" stroke-linecap="round"/>`;
  return `<g transform="translate(${(side - b) / 2} ${(side - b) / 2}) scale(${b / 40})">`
       + [0, 180, 270].flatMap(a => [-4, 0, 4].map(t => pin(a, t))).join("")   // N, S, W; E is the lead
       + `<rect x="28" y="16.5" width="10" height="7" rx="1.5" fill="#fff"/>`
       + `<path d="M20 9 L30 14.5 L30 25.5 L20 31 L10 25.5 L10 14.5 Z M23.2 20 A3.2 3.2 0 1 0 16.8 20 A3.2 3.2 0 1 0 23.2 20 Z" fill="#fff" fill-rule="evenodd"/></g>`;
}
export const adaptiveInset = (mark, side) => `<g transform="translate(${side * 0.19} ${side * 0.19}) scale(.62)">${mark}</g>`;
const box = (id, side, body) => ({ svg: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${side} ${side}" id="${id}">\n${body}\n</svg>\n`, inner: body, W: side, H: side });

export function icon({ side = 1024, adaptive = false } = {}) {
  const k = adaptive ? "ia" : "i";
  const mark = tileMark(k, side);
  return box(adaptive ? "icon_adaptive" : "icon", side,
    defs(k, side / 168, ICON_SKIN) + substrate(k, side, side, side / 8, false, undefined, ICON_SKIN) + (adaptive ? adaptiveInset(mark, side) : mark));
}
// Adaptive background: the icon's substrate with nothing on it.
export function iconBackground({ side = 1024 } = {}) {
  const k = "ib";
  return box("icon_bg", side, defs(k, side / 168, ICON_SKIN) + substrate(k, side, side, side / 8, false, undefined, ICON_SKIN));
}
// Monochrome layer, transparent, at the adaptive inset.
export function iconMono({ side = 1024 } = {}) {
  return box("icon_mono", side, adaptiveInset(tileMonoMark(side), side));
}

// ---------- C# outlines (title screen) ----------
// Each distinct letter of the wordmark as flattened contours in font units
// per 1000 em, y down, baseline at 0, plus each word's pen positions at the
// lock's size and spacing. Quadratics are flattened at eight steps, the
// same as GlyphCanvas.AppendQuad; Chakra Petch is nearly all straight
// segments anyway.
function contoursOf(ch) {
  const path = font.charToGlyph(ch).getPath(0, 0, 1000);
  const out = []; let cur = null, x = 0, y = 0;
  const push = (px, py) => { cur.push(+px.toFixed(1), +py.toFixed(1)); };
  for (const c of path.commands) {
    if (c.type === "M") { cur = []; out.push(cur); x = c.x; y = c.y; push(x, y); }
    else if (c.type === "L") { x = c.x; y = c.y; push(x, y); }
    else if (c.type === "Q") { for (let i = 1; i <= 8; i++) { const t = i / 8, u = 1 - t; push(u*u*x + 2*u*t*c.x1 + t*t*c.x, u*u*y + 2*u*t*c.y1 + t*t*c.y); } x = c.x; y = c.y; }
    else if (c.type === "C") { for (let i = 1; i <= 12; i++) { const t = i / 12, u = 1 - t; push(u*u*u*x + 3*u*u*t*c.x1 + 3*u*t*t*c.x2 + t*t*t*c.x, u*u*u*y + 3*u*u*t*c.y1 + 3*u*t*t*c.y2 + t*t*t*c.y); } x = c.x; y = c.y; }
    else if (c.type === "Z") { if (cur && cur.length >= 4 && cur[0] === cur[cur.length-2] && cur[1] === cur[cur.length-1]) cur.length -= 2; cur = null; }
  }
  return out.filter(c => c.length >= 6);
}
function pens(word) {
  const xs = [];
  font.forEachGlyph(word, 0, 0, FS, opt, (g, gx) => xs.push(+(gx / FS * 1000).toFixed(1)));
  return xs;
}
export function csharp() {
  const letters = [...new Set("GRIDINFECT")];
  const arr = a => a.join("f, ") + "f";
  let s = `// <auto-generated> grid-infect-style/gen-logo.mjs --cs. Do not edit; regenerate.
// The wordmark's letters (Chakra Petch Bold, OFL, grid-infect-style/fonts)
// as flattened outlines in 1/1000 em, y down, baseline 0, and the two words'
// pen positions at the lock's 92 px / 2 px letter-spacing (STYLE-GUIDE §12).
namespace GridInfect.Game
{
    public static class LogoGlyphs
    {
        public const float Em = 1000f;                 // outline units per em
        public const float FontPx = ${FS}f;                // the lock's type size
        public const float GapPx = ${GAP}f;                 // GRID | gap | INFECT
        public const float BugPx = ${BUG}f;                 // bug_E in the gap
        public const float BaselineBelowMid = 0.36f;   // baseline = mid + FontPx * this

        public static readonly string Grid = "GRID";
        public static readonly float[] GridPen = { ${arr(pens("GRID"))} };
        public const float GridWidth = ${(wG / FS * 1000).toFixed(1)}f;
        public static readonly string Infect = "INFECT";
        public static readonly float[] InfectPen = { ${arr(pens("INFECT"))} };
        public const float InfectWidth = ${(wI / FS * 1000).toFixed(1)}f;

        public static float[][] Contours(char c)
        {
            switch (c)
            {
${letters.map(l => `                case '${l}': return ${l};`).join("\n")}
                default: return null;
            }
        }

${letters.map(l => `        static readonly float[][] ${l} =\n        {\n${contoursOf(l).map(c => `            new float[] { ${arr(c)} },`).join("\n")}\n        };`).join("\n\n")}
    }
}
`;
  return s;
}

// ---------- raster ----------
// Playwright when it is installed (PLAYWRIGHT= as tools/style-bench/run.mjs);
// otherwise Chromium's own --screenshot (CHROMIUM= the chrome or
// headless_shell binary). An SVG document with only a viewBox fills the
// viewport, and the viewport matches its aspect exactly.
async function rasteriser() {
  const bin = process.env.CHROMIUM;
  let pw = null;
  try { pw = await import(process.env.PLAYWRIGHT || "playwright"); } catch { /* fall through */ }
  if (pw) {
    const chromium = pw.chromium ?? pw.default.chromium;
    const b = await chromium.launch({ executablePath: bin || undefined });
    const shoot = async (url, out, W, H, opaque) => {
      const p = await b.newPage({ viewport: { width: W, height: H }, deviceScaleFactor: 1 });
      await p.goto(url, { waitUntil: "load" });
      await p.waitForTimeout(150);
      await p.screenshot({ path: out, omitBackground: !opaque });
      await p.close();
    };
    shoot.close = () => b.close();
    return shoot;
  }
  const { execFileSync } = await import("node:child_process");
  const exe = bin || "/opt/pw-browsers/chromium_headless_shell-1194/chrome-linux/headless_shell";
  const shoot = async (url, out, W, H, opaque) => execFileSync(exe, ["--headless=new", "--no-sandbox", "--disable-gpu", "--hide-scrollbars",
    `--window-size=${W},${H}`, ...(opaque ? [] : ["--default-background-color=00000000"]), `--screenshot=${out}`, url], { stdio: "ignore" });
  shoot.close = async () => {};
  return shoot;
}

// ---------- emit ----------
if (MAIN) {
  const files = {
    "wordmark":          wordmark(),
    "wordmark_board":    wordmark({ framed: true }),
    "monogram":          monogram(),
    "icon":              icon(),
    "icon_adaptive":     icon({ adaptive: true }),
    "icon_bg":           iconBackground(),
    "icon_mono":         iconMono(),
  };
  for (const [n, f] of Object.entries(files)) writeFileSync(join(OUT, `${n}.svg`), f.svg);
  console.log(`wrote ${Object.keys(files).map(n => n + ".svg").join(", ")} -> ${OUT}/`);

  if (PNG) {
    const jobs = [
      ["wordmark", 1, false], ["wordmark", 2, false], ["wordmark_board", 2, true],
      ...[1024, 512].map(px => ["monogram", px / 1024, true]),
      ...[1024, 512, 192, 96, 48].map(px => ["icon", px / 1024, true]),
      ...[1024, 432].map(px => ["icon_adaptive", px / 1024, true]),
      ...[1024, 432].map(px => ["icon_bg", px / 1024, true]),
      ...[1024, 432].map(px => ["icon_mono", px / 1024, false]),
    ];
    const shoot = await rasteriser();
    for (const [n, scale, opaque] of jobs) {
      const f = files[n], W = Math.round(f.W * scale), H = Math.round(f.H * scale);
      const tag = n.startsWith("monogram") || n.startsWith("icon") ? `${W}` : `${scale}x`;
      await shoot(pathToFileURL(join(OUT, `${n}.svg`)).href, join(OUT, `${n}_${tag}.png`), W, H, opaque);
      console.log(`  ${n}_${tag}.png ${W}x${H}`);
    }
    await shoot.close();
  }

  if (CS) {
    const cs = join(here, "..", "unity", "Assets", "_Project", "Game", "View", "LogoGlyphs.g.cs");
    writeFileSync(cs, csharp());
    console.log(`wrote ${cs}`);
  }
}
