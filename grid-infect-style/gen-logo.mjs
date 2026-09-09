#!/usr/bin/env node
// Grid Infect logo generator (logo lock 2026-09-09, STYLE-GUIDE §11).
// node gen-logo.mjs [outDir] [--png]
//   outDir defaults to out/logo next to this file.
//   --png also rasterises through Chromium (playwright; PLAYWRIGHT= and CHROMIUM= as in tools/style-bench/run.mjs).
// Green skin only. The logo does not reskin.
import { mkdirSync, writeFileSync, readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import opentype from "opentype.js";
import { bug, tokens } from "./gen-assets.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const args = process.argv.slice(2);
const PNG = args.includes("--png");
const OUT = args.find(a => !a.startsWith("--")) ?? join(here, "out", "logo");
mkdirSync(OUT, { recursive: true });

const S = tokens.skins.default;
const fontBuf = readFileSync(join(here, "fonts", "ChakraPetch-Bold.ttf"));
const font = opentype.parse(fontBuf.buffer.slice(fontBuf.byteOffset, fontBuf.byteOffset + fontBuf.byteLength));

// ---------- type ----------
const FS = 92, LS = 2;            // wordmark size and letter-spacing, px
const opt = { kerning: true, letterSpacing: LS / FS };
const advance = (t, fs = FS) => font.getAdvanceWidth(t, fs, { ...opt, letterSpacing: LS / FS }) ;
const glyphs  = (t, x, y, fs = FS, ls = LS) => font.getPath(t, x, y, fs, { kerning: true, letterSpacing: ls / fs }).toPathData(2);

// ---------- materials ----------
// k prefixes ids so several marks can share one HTML document.
function defs(k, sc = 1) {
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
const dense = (k, d, sw) => `<g filter="url(#${k}drop)"><path d="${d}" fill="url(#${k}dense)" stroke="rgba(255,255,255,.9)" stroke-width="${sw}" stroke-linejoin="round"/></g>`;
const lit   = (k, d, sw) => `<path d="${d}" fill="${S.infect}" filter="url(#${k}glowBig)" opacity=".75"/><path d="${d}" fill="${S.infect}" filter="url(#${k}glow2)" opacity=".5"/><path d="${d}" fill="url(#${k}lit)" stroke="rgba(255,255,255,.6)" stroke-width="${sw}" stroke-linejoin="round"/>`;
const bugAt = (k, x, y, size) => `<g transform="translate(${x - size/2} ${y - size/2}) scale(${size/40})"><rect x="6" y="6" width="28" height="28" rx="6" fill="${S.infect}" opacity=".35" filter="url(#${k}bugGlow)"/>${bug(["E"])}</g>`;

// ---------- board substrate (for the framed wordmark and the monogram) ----------
function substrate(k, W, H, pitch, holes, label) {
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
  return { svg: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${W} ${H}" id="${framed ? "wordmark_board" : "wordmark"}">\n${s}\n</svg>\n`, W, H };
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
  return { svg: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${s0} ${s0}" id="${adaptive ? "monogram_adaptive" : "monogram"}">\n${s}\n</svg>\n`, W: s0, H: s0 };
}

// ---------- emit ----------
const MAIN = process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href;
if (MAIN) {
  const files = {
    "wordmark":          wordmark(),
    "wordmark_board":    wordmark({ framed: true }),
    "monogram":          monogram(),
    "monogram_adaptive": monogram({ adaptive: true }),
  };
  for (const [n, f] of Object.entries(files)) writeFileSync(join(OUT, `${n}.svg`), f.svg);
  console.log(`wrote ${Object.keys(files).map(n => n + ".svg").join(", ")} -> ${OUT}/`);

  if (PNG) {
    const { chromium } = await import(process.env.PLAYWRIGHT || "playwright");
    const b = await chromium.launch({ executablePath: process.env.CHROMIUM || undefined });
    const jobs = [
      ["wordmark", 1, false], ["wordmark", 2, false], ["wordmark_board", 2, true],
      ...[1024, 512, 192, 96, 48].map(px => ["monogram", px / 1024, true]),
      ...[1024, 432].map(px => ["monogram_adaptive", px / 1024, true]),
    ];
    for (const [n, scale, opaque] of jobs) {
      const f = files[n], W = Math.round(f.W * scale), H = Math.round(f.H * scale);
      const p = await b.newPage({ viewport: { width: W, height: H }, deviceScaleFactor: 1 });
      // An SVG document with only a viewBox fills the viewport; the viewport matches its aspect exactly.
      await p.goto(pathToFileURL(join(OUT, `${n}.svg`)).href, { waitUntil: "load" });
      await p.waitForTimeout(150);
      const tag = n.startsWith("monogram") ? `${W}` : `${scale}x`;
      await p.screenshot({ path: join(OUT, `${n}_${tag}.png`), omitBackground: !opaque });
      await p.close();
      console.log(`  ${n}_${tag}.png ${W}x${H}`);
    }
    await b.close();
  }
}
