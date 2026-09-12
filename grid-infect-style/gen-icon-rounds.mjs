#!/usr/bin/env node
// Icon round 4 (2026-09-12): the app icon at drawer sizes. The lock (STYLE-GUIDE
// §12) reads as "red bar and a dot" at 48 px because the dense-glass G is
// white at 30-66% over the mask. Candidates are built from gen-logo.mjs's
// primitives, so a pick is a change of numbers in monogram(), not new art.
// node gen-icon-rounds.mjs            writes out/icon-rounds/*.svg and sheet.html
// node gen-icon-rounds.mjs --png      also rasterises through headless Chromium
//                                     (CHROMIUM=/path/to/headless_shell or chrome)
import { mkdirSync, writeFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { execFileSync } from "node:child_process";
import { bug, tokens } from "./gen-assets.mjs";
import { S, glyphs, defs, dense, lit, bugAt, substrate, ICON_SKIN, tileMark, tileMonoMark } from "./gen-logo.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const OUT = join(here, "out", "icon-rounds");
mkdirSync(OUT, { recursive: true });
const PNG = process.argv.includes("--png");

const s0 = 1024, sc = s0 / 168;
// Chakra Petch Bold, per 1000 em: cap 700; G ink x 55..631; I ink x 70..206.
const CAP = 0.7, G_L = 0.055, G_R = 0.631, I_L = 0.07, I_R = 0.206;

// A darker mask for the icon only: the lock's gradient shifted one stop down.
const DARK = ICON_SKIN;

// Opaque glass: the dense gradient with the alpha lifted so the letter holds
// against the mask at 48 px. Rim and shadow as the lock.
const solidDefs = k => `<defs><linearGradient id="${k}solid" x1="0" y1="0" x2=".3" y2="1"><stop offset="0" stop-color="#fff" stop-opacity=".96"/><stop offset=".55" stop-color="#e8f0e0" stop-opacity=".84"/><stop offset="1" stop-color="#fff" stop-opacity=".92"/></linearGradient></defs>`;
const solid = (k, d, sw) => `<g filter="url(#${k}drop)"><path d="${d}" fill="url(#${k}solid)" stroke="rgba(255,255,255,.95)" stroke-width="${sw}" stroke-linejoin="round"/></g>`;

const svg = (id, body) => `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${s0} ${s0}" id="${id}">\n${body}\n</svg>\n`;
const adaptive = mark => `<g transform="translate(${s0 * 0.19} ${s0 * 0.19}) scale(.62)">${mark}</g>`;

// ---------- candidates: each returns { mark, ground } ----------
const C = {};

// A. The lock as built (monogram()).
C.A = { name: "Lock", note: "As built. G dense glass at 70% of the side, lit I, bug_E between. The G reads as a ghost below 96 px.", make(k) {
  const fs = s0 * 0.7, base = s0 * 0.5 + fs * 0.36;
  return { ground: defs(k, sc) + substrate(k, s0, s0, s0 / 8, false),
           mark: dense(k, glyphs("G", s0 * 0.03, base, fs, 0), s0 * 0.009) + lit(k, glyphs("I", s0 * 0.745, base, fs, 0), s0 * 0.009) + bugAt(k, s0 * 0.615, s0 * 0.5, s0 * 0.27) };
} };

// B. Contrast only: the lock's layout, opaque G, mask one stop darker.
C.B = { name: "Contrast", note: "Same layout. G opaque glass (96/84/92% white), mask shifted one stop darker for the icon only. Smallest change that separates the G.", make(k) {
  const fs = s0 * 0.7, base = s0 * 0.5 + fs * 0.36;
  return { ground: defs(k, sc, DARK) + solidDefs(k) + substrate(k, s0, s0, s0 / 8, false, undefined, DARK),
           mark: solid(k, glyphs("G", s0 * 0.03, base, fs, 0), s0 * 0.009) + lit(k, glyphs("I", s0 * 0.745, base, fs, 0), s0 * 0.009) + bugAt(k, s0 * 0.615, s0 * 0.5, s0 * 0.27) };
} };

// C. Fill the square: B's materials, type at 84% of the side, gap closed to the bug.
C.C = { name: "Fill", note: "B's materials. Type at 84% of the side (cap 59%), bug at 28%, gap closed so the mark spans 84% of the width. Uses the square instead of a band across it.", make(k) {
  const fs = s0 * 0.84, cap = fs * CAP, base = s0 * 0.5 + cap / 2;
  const gW = (G_R - G_L) * fs, iW = (I_R - I_L) * fs, bugS = s0 * 0.28, gap = s0 * 0.24;
  const x0 = (s0 - (gW + gap + iW)) / 2;
  return { ground: defs(k, sc, DARK) + solidDefs(k) + substrate(k, s0, s0, s0 / 8, false, undefined, DARK),
           mark: solid(k, glyphs("G", x0 - G_L * fs, base, fs, 0), s0 * 0.01) + lit(k, glyphs("I", x0 + gW + gap - I_L * fs, base, fs, 0), s0 * 0.01) + bugAt(k, x0 + gW + gap / 2, s0 * 0.5, bugS) };
} };

// D. One form: the G alone at 100% of the side with the bug in its counter. No I.
C.D = { name: "G + bug", note: "G opaque glass at 100% of the side (cap 70%), bug_E at 30% sitting in the counter, tip on the G's bar. One shape, so it survives 29 px. Drops the lit I.", make(k) {
  const fs = s0 * 1.0, cap = fs * CAP, base = s0 * 0.5 + cap / 2;
  const gx = (s0 - (G_R - G_L) * fs) / 2 - G_L * fs;
  return { ground: defs(k, sc, DARK) + solidDefs(k) + substrate(k, s0, s0, s0 / 8, false, undefined, DARK),
           mark: solid(k, glyphs("G", gx, base, fs, 0), s0 * 0.011) + bugAt(k, gx + 0.352 * fs, base - 0.345 * fs, s0 * 0.30) };
} };

// E. The bug on a placed tile: what a cell looks like on the board. No letters. The pick; gen-logo.mjs icon().
C.E = { name: "Tile", note: "A board tile (dense glass, tileRadius) at 72% with bug_E at 58% on it. The game's most distinctive shape and nothing else. Reads as the game, not as a name. Picked 2026-09-12.", make(k) {
  return { ground: defs(k, sc, DARK) + substrate(k, s0, s0, s0 / 8, false, undefined, DARK), mark: tileMark(k, s0) };
} };

// F. Monochrome layer (Android 13 themed icons, iOS tinted): E's bug as a silhouette.
C.F = { name: "Mono (E)", note: "Android themed-icon / iOS tinted layer for E: the bug flat, hollow core, E lead and pins, on transparent. The launcher tints it. gen-logo.mjs iconMono().", mono: true, make() {
  return { ground: "", mark: tileMonoMark(s0) };
} };

// ---------- emit ----------
const files = [];
for (const [id, c] of Object.entries(C)) {
  const k = id.toLowerCase();
  const { mark, ground } = c.make(k);
  files.push([`${id}`, svg(`icon_${id}`, ground + mark), !c.mono]);
  files.push([`${id}_adaptive`, svg(`icon_${id}_adaptive`, ground + adaptive(mark)), !c.mono]);
  if (c.mono) for (const [t, fg, bg] of [["light", "#3b5b2e", "#d7e3cf"], ["dark", "#c9dcbd", "#2b3a26"]])
    files.push([`${id}_tint_${t}`, svg(`icon_${id}_${t}`, `<rect width="${s0}" height="${s0}" fill="${bg}"/>` + adaptive(mark.replaceAll("#fff", fg))), true]);
}
for (const [n, s] of files) writeFileSync(join(OUT, `${n}.svg`), s);
console.log(`wrote ${files.length} svgs -> ${OUT}/`);

if (PNG) {
  const chromium = process.env.CHROMIUM || "/opt/pw-browsers/chromium_headless_shell-1194/chrome-linux/headless_shell";
  for (const [n, , opaque] of files) {
    execFileSync(chromium, ["--headless=new", "--no-sandbox", "--disable-gpu", "--hide-scrollbars",
      `--window-size=${s0},${s0}`, ...(opaque ? [] : ["--default-background-color=00000000"]),
      `--screenshot=${join(OUT, n + ".png")}`, pathToFileURL(join(OUT, n + ".svg")).href], { stdio: "ignore" });
    console.log(`  ${n}.png`);
  }
}

// ---------- contact sheet ----------
const ids = Object.keys(C);
const ios = (id, px) => `<img src="${id}.png" width="${px}" height="${px}" class="ios">`;
const and = (id, px) => `<img src="${id}_adaptive.png" width="${px}" height="${px}" class="and">`;
const mono = (id, px, t = "light") => `<img src="${id}_tint_${t}.png" width="${px}" height="${px}" class="and">`;
const row = id => {
  const c = C[id], m = c.mono;
  const cell = (px, cls) => m ? mono(id, px) : (cls === "and" ? and(id, px) : ios(id, px));
  return `<div class="row">
  <div class="who"><div class="eyebrow">${id}</div><h2>${c.name}</h2><p>${c.note}</p></div>
  <div class="sizes">
    <div class="sz">${cell(160)}<span class="cap">160 iOS</span></div>
    <div class="sz">${cell(108, "and")}<span class="cap">108 adaptive</span></div>
    <div class="sz">${cell(96)}<span class="cap">96</span></div>
    <div class="sz">${cell(48)}<span class="cap">48</span></div>
    <div class="sz">${cell(29)}<span class="cap">29</span></div>
  </div>
  <div class="home light">${m ? mono(id, 56) : and(id, 56)}<span>Grid Infect</span></div>
  <div class="home dark">${m ? mono(id, 56, "dark") : and(id, 56)}<span>Grid Infect</span></div>
</div>`;
};
const html = `<!doctype html><meta charset="utf-8"><title>Grid Infect icon round 4</title>
<style>
:root{--ground:#121810;--panel:#182016;--line:#2a352a;--paper:#e6e9dc;--muted:#98a38d;--dim:#6b7562;--copper:#c9a648}
html,body{background:var(--ground);color:var(--paper);font-family:"Chakra Petch",system-ui,sans-serif;font-size:15px;line-height:1.45;margin:0}
.wrap{max-width:1200px;margin:0 auto;padding:32px 28px 48px}
h1{font-size:28px;font-weight:600;margin:0 0 4px}h2{font-size:20px;font-weight:600;margin:0}
.lede{color:var(--muted);max-width:80ch;margin:0 0 8px}
.eyebrow{font-family:ui-monospace,Menlo,monospace;font-size:11px;letter-spacing:.15em;color:var(--copper)}
.row{display:grid;grid-template-columns:250px 1fr 150px 150px;gap:20px;align-items:center;padding:18px 0;border-top:1px solid var(--line)}
.who p{font-size:13.5px;color:var(--muted);margin:4px 0 0}
.sizes{display:flex;gap:22px;align-items:flex-end}
.sz{display:flex;flex-direction:column;align-items:center;gap:6px}
.cap{font-family:ui-monospace,Menlo,monospace;font-size:10px;color:var(--dim);letter-spacing:.08em}
img{display:block}
.ios{border-radius:22.5%}
.and{border-radius:50%}
.home{height:118px;border-radius:12px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:6px;font-size:11px}
.home.light{background:linear-gradient(160deg,#eef1ea,#c9d3c2 60%,#a9b8a0);color:#1a2418}
.home.dark{background:linear-gradient(160deg,#25303a,#141a20 60%,#0b0f13);color:#dfe6ec}
</style>
<div class="wrap">
<div class="eyebrow">2026-09-12 · icon round 4</div>
<h1>App icon at drawer sizes</h1>
<p class="lede">iOS squircle at 160/96/48/29, Android adaptive (mark at 62%, circle mask) at 108, and the adaptive icon on a light and a dark home screen. A and B keep the lock's layout; C to E change it; F is the themed-icon layer. Picked: E, with F.</p>
${ids.map(row).join("\n")}
</div>`;
writeFileSync(join(OUT, "sheet.html"), html);
console.log("wrote sheet.html");
