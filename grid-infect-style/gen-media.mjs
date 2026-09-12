#!/usr/bin/env node
// Grid Infect store and press media (STYLE-GUIDE §12, §12.1 for the icons).
// node gen-media.mjs              renders every entry of media.manifest.json
// node gen-media.mjs --only <id>  renders one
// node gen-media.mjs --check      reads the manifest, exits 1 if any output is
//                                 missing or has the wrong size, alpha or bytes
// node gen-media.mjs --install    copies the entries with an install name into
//                                 the unity icon folder (after rendering, or
//                                 alone from what is already in out/store)
// Output: out/store/<platform>/<id>.<ext>, git-ignored. Rendering is
// deterministic: the same manifest and inputs give byte-identical files.
// Raster through Chromium (playwright devDependency; PLAYWRIGHT= and
// CHROMIUM= as in tools/style-bench/run.mjs). Every dimension comes from the
// manifest; this file only lays out relative to w and h.
import { mkdirSync, readFileSync, existsSync, copyFileSync } from "node:fs";
import { join, dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { bug, tokens } from "./gen-assets.mjs";
import { S, FS, LS, glyphs, advance, defs, lit, substrate, wordmark, monogramBackground } from "./gen-logo.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const args = process.argv.slice(2);
const CHECK = args.includes("--check");
const INSTALL = args.includes("--install");
const ONLY = args.includes("--only") ? args[args.indexOf("--only") + 1] : null;

// ---------- manifest ----------
const manifest = JSON.parse(readFileSync(join(here, "media.manifest.json"), "utf8"));
const OUT = resolve(here, manifest.outDir);
const RAW = resolve(here, manifest.rawDir);
const INSTALL_DIR = resolve(here, manifest.installDir);
const outPath = e => join(OUT, e.platform, `${e.id}.${e.format}`);
const rawPath = e => join(RAW, `${e.raw}.png`);

// ---------- shared pieces ----------
const REF_W = tokens.layout.screen[0];        // 390: the screen's reference width, portrait scale base
const BOARD_W = wordmark({ framed: true }).W;  // 880: the framed wordmark's width, landscape scale base
const INK = S.ink;
const label = e => e.label ?? `GI-${e.id.toUpperCase().replace(/_/g, "-")}`;
const svgDoc = (W, H, body) => `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}">\n${body}\n</svg>`;

// The board substrate at reference scale sc (holes, silkscreen and grid
// pitch scale with it; the mask gradient and sheen stretch to the frame).
function board(W, H, sc, text) {
  return defs("s", 1) + `<g transform="scale(${sc})">${substrate("s", W / sc, H / sc, 24, true, text)}</g>`;
}
// A mark's inner content placed at (x, y) with width w; overflow visible so
// glows and shadows are not clipped at the mark's own box.
function place(mark, x, y, w) {
  const h = w * mark.H / mark.W;
  return `<svg x="${x}" y="${y}" width="${w}" height="${h}" viewBox="0 0 ${mark.W} ${mark.H}" overflow="visible">${mark.inner}</svg>`;
}
// Chakra Petch Bold outlined, centred on cx, one or more lines. Sizes are
// the STYLE-GUIDE reference sizes times the frame's scale; letter-spacing
// keeps the wordmark's ratio (§12: 2 px at 92 px).
function caption(text, cx, top, fs, fill = INK, lineHeight = 1.2) {
  const ls = fs * LS / FS;
  return text.split("\n").map((line, i) => {
    const w = advance(line, fs) + ls * (line.length - 1);
    return `<path d="${glyphs(line, cx - w / 2, top + fs + i * fs * lineHeight, fs, ls)}" fill="${fill}"/>`;
  }).join("");
}

// ---------- icon-only materials (STYLE-GUIDE §12.1) ----------
// The monogram at 48 px on a launcher: the dense G loses its rim and its
// glass reads as a stain on the mask. Icons only: a heavier, fully opaque
// rim, a denser fill, a stronger backing glow under the bug. Nothing else
// changes, and the wordmark and the 1024 monogram reference keep §12.
const ICON = { rim: 0.014, denseStops: [".78", ".42", ".56"], glow: ".5", mark: 0.62, offset: 0.19 };
function iconDefs(k, sc) {
  const [a, b, c] = ICON.denseStops;
  return defs(k, sc) + `<defs><linearGradient id="${k}denseIcon" x1="0" y1="0" x2=".3" y2="1"><stop offset="0" stop-color="#fff" stop-opacity="${a}"/><stop offset=".55" stop-color="#fff" stop-opacity="${b}"/><stop offset="1" stop-color="#fff" stop-opacity="${c}"/></linearGradient></defs>`;
}
const denseIcon = (k, d, sw) => `<g filter="url(#${k}drop)"><path d="${d}" fill="url(#${k}denseIcon)" stroke="#fff" stroke-width="${sw}" stroke-linejoin="round"/></g>`;
const bugIconAt = (k, x, y, size) => `<g transform="translate(${x - size/2} ${y - size/2}) scale(${size/40})"><rect x="6" y="6" width="28" height="28" rx="6" fill="${S.infect}" opacity="${ICON.glow}" filter="url(#${k}bugGlow)"/>${bug(["E"])}</g>`;
// The monogram's mark (§12 geometry) with the icon materials.
function iconMark(k, s0) {
  const fs = s0 * 0.7, base = s0 * 0.5 + fs * 0.36;
  return denseIcon(k, glyphs("G", s0 * 0.03, base, fs, 0), s0 * ICON.rim)
       + lit(k, glyphs("I", s0 * 0.745, base, fs, 0), s0 * 0.009)
       + bugIconAt(k, s0 * 0.615, s0 * 0.5, s0 * 0.27);
}
const adaptive = (s0, mark) => `<g transform="translate(${s0 * ICON.offset} ${s0 * ICON.offset}) scale(${ICON.mark})">${mark}</g>`;

// ---------- compositions ----------
// Each takes the manifest entry and returns an SVG document of exactly w×h.
const COMPOSE = {
  // Play feature graphic: substrate, wordmark centred at 76% of the width so
  // the store's overlays at the edges never touch it.
  feature_graphic(e) {
    const { w: W, h: H } = e, sc = W / BOARD_W, m = wordmark();
    const mw = W * 0.76, mh = mw * m.H / m.W;
    return svgDoc(W, H, board(W, H, sc, label(e)) + place(m, (W - mw) / 2, (H - mh) / 2, mw));
  },
  // Press banner: substrate, wordmark at 58% of the width, an optional
  // caption under it at the HUD level size.
  banner_wide(e) {
    const { w: W, h: H } = e, sc = W / BOARD_W, m = wordmark();
    const mw = W * 0.58, mh = mw * m.H / m.W;
    const fs = tokens.type.hudLevel * sc, lines = e.caption ? e.caption.split("\n").length : 0;
    const block = mh + (lines ? fs * 1.2 * lines + 12 * sc : 0), top = (H - block) / 2;
    let s = board(W, H, sc, label(e)) + place(m, (W - mw) / 2, top, mw);
    if (lines) s += caption(e.caption, W / 2, top + mh + 12 * sc, fs);
    return svgDoc(W, H, s);
  },
  // Social header (X/Twitter 1500×500 and the like): the avatar covers the
  // bottom-left, so the wordmark sits centred at 52% and a touch high.
  social_header(e) {
    const { w: W, h: H } = e, sc = W / BOARD_W, m = wordmark();
    const mw = W * 0.52, mh = mw * m.H / m.W;
    let s = board(W, H, sc, label(e)) + place(m, (W - mw) / 2, H * 0.46 - mh / 2, mw);
    if (e.caption) s += caption(e.caption, W / 2, H * 0.46 + mh / 2 + 8 * sc, tokens.type.hudChip * sc);
    return svgDoc(W, H, s);
  },
  // Store icon (App Store 1024, Play 512, unity icon_1024): §12 substrate
  // under the §12.1 mark. No corner rounding; the platforms mask it.
  icon_main(e) {
    const s0 = e.w, k = "im";
    return svgDoc(e.w, e.h, iconDefs(k, s0 / 168) + substrate(k, s0, s0, s0 / 8, false) + iconMark(k, s0));
  },
  // Adaptive foreground: the mark alone at 62% inside the safe zone on a
  // transparent ground, so the launcher's parallax shows the background.
  icon_fg(e) {
    const s0 = e.w, k = "if";
    return svgDoc(e.w, e.h, iconDefs(k, s0 / 168) + adaptive(s0, iconMark(k, s0)));
  },
  // Adaptive background: the substrate alone, same numbers as monogram_bg.
  icon_bg(e) {
    const m = monogramBackground({ side: e.w });
    return svgDoc(e.w, e.h, m.inner);
  },
  // Themed-icon monochrome layer (Android 13+): the mark as one flat white
  // silhouette on transparent, no gradient, glow or shadow (the launcher
  // tints it), at the adaptive 62%.
  icon_mono(e) {
    const s0 = e.w, k = "iz", fs = s0 * 0.7, base = s0 * 0.5 + fs * 0.36;
    const flat = `<filter id="${k}flat" color-interpolation-filters="sRGB"><feColorMatrix type="matrix" values="0 0 0 0 1  0 0 0 0 1  0 0 0 0 1  0 0 0 1 0"/></filter>`;
    const size = s0 * 0.27, bx = s0 * 0.615, by = s0 * 0.5;
    const mark = `<path d="${glyphs("G", s0 * 0.03, base, fs, 0)}" fill="#fff" stroke="#fff" stroke-width="${s0 * ICON.rim}" stroke-linejoin="round"/>`
      + `<path d="${glyphs("I", s0 * 0.745, base, fs, 0)}" fill="#fff" stroke="#fff" stroke-width="${s0 * 0.009}" stroke-linejoin="round"/>`
      + `<g filter="url(#${k}flat)" transform="translate(${bx - size/2} ${by - size/2}) scale(${size/40})">${bug(["E"])}</g>`;
    return svgDoc(e.w, e.h, `<defs>${flat}</defs>` + adaptive(s0, mark));
  },
  // A device capture from out/raw/<device>/<n>.png on the substrate, caption
  // above it in Chakra Petch at the HUD level size times the frame's scale
  // (§11: type scales with the short edge), the capture framed like the
  // board well (§4): rounded to the well radius, outer 3 px black 18% ring,
  // inset 1 px white 14% rim, the tile shadow under it.
  screenshot_frame(e) {
    const { w: W, h: H } = e, sc = W / REF_W;
    const raw = rawPath(e);
    if (!existsSync(raw)) throw new Error(`raw capture missing: ${raw}`);
    const buf = readFileSync(raw), dim = pngInfo(buf);
    if (!dim) throw new Error(`raw capture is not a PNG: ${raw}`);
    const fs = tokens.type.hudLevel * sc, lines = e.caption ? e.caption.split("\n").length : 0;
    const margin = 44 * sc, capTop = 40 * sc, capH = lines ? fs * 1.2 * lines : 0;
    const top = capTop + capH + (lines ? 28 * sc : 0), bottom = H - 48 * sc;
    const fit = Math.min((W - 2 * margin) / dim.w, (bottom - top) / dim.h);
    const iw = dim.w * fit, ih = dim.h * fit, ix = (W - iw) / 2, iy = top + (bottom - top - ih) / 2;
    const r = tokens.layout.wellRadius * sc, k = "sf";
    let s = board(W, H, sc, label(e));
    if (lines) s += caption(e.caption, W / 2, capTop, fs);
    s += `<defs><clipPath id="${k}clip"><rect x="${ix}" y="${iy}" width="${iw}" height="${ih}" rx="${r}"/></clipPath>`
       + `<filter id="${k}shadow" x="-20%" y="-20%" width="140%" height="140%"><feDropShadow dx="0" dy="${7 * sc}" stdDeviation="${8 * sc}" flood-color="#000" flood-opacity=".38"/></filter></defs>`
       + `<rect x="${ix}" y="${iy}" width="${iw}" height="${ih}" rx="${r}" fill="#000" filter="url(#${k}shadow)"/>`
       + `<rect x="${ix - 1.5 * sc}" y="${iy - 1.5 * sc}" width="${iw + 3 * sc}" height="${ih + 3 * sc}" rx="${r + 1.5 * sc}" fill="none" stroke="rgba(0,0,0,.18)" stroke-width="${3 * sc}"/>`
       + `<image href="data:image/png;base64,${buf.toString("base64")}" x="${ix}" y="${iy}" width="${iw}" height="${ih}" preserveAspectRatio="none" clip-path="url(#${k}clip)"/>`
       + `<rect x="${ix + 0.5 * sc}" y="${iy + 0.5 * sc}" width="${iw - sc}" height="${ih - sc}" rx="${r - 0.5 * sc}" fill="none" stroke="rgba(255,255,255,.14)" stroke-width="${sc}"/>`;
    return svgDoc(W, H, s);
  },
};

// ---------- image headers (for --check and the raw captures) ----------
function pngInfo(buf) {
  if (buf.length < 33 || buf.toString("latin1", 1, 4) !== "PNG") return null;
  const w = buf.readUInt32BE(16), h = buf.readUInt32BE(20), type = buf[25];
  let alpha = (type & 4) !== 0;
  if (type === 3) {                              // palette: alpha only through a tRNS chunk
    for (let p = 8; p + 8 <= buf.length;) {
      const len = buf.readUInt32BE(p), name = buf.toString("latin1", p + 4, p + 8);
      if (name === "tRNS") { alpha = true; break; }
      if (name === "IDAT" || name === "IEND") break;
      p += 12 + len;
    }
  }
  return { w, h, alpha };
}
function jpgInfo(buf) {
  if (buf[0] !== 0xff || buf[1] !== 0xd8) return null;
  for (let p = 2; p + 9 < buf.length;) {
    if (buf[p] !== 0xff) return null;
    const m = buf[p + 1], len = buf.readUInt16BE(p + 2);
    if (m >= 0xc0 && m <= 0xcf && m !== 0xc4 && m !== 0xc8 && m !== 0xcc) return { h: buf.readUInt16BE(p + 5), w: buf.readUInt16BE(p + 7), alpha: false };
    p += 2 + len;
  }
  return null;
}
function problems(e) {
  const f = outPath(e), out = [];
  if (!existsSync(f)) return [`missing ${f}`];
  const buf = readFileSync(f), info = e.format === "png" ? pngInfo(buf) : jpgInfo(buf);
  if (!info) return [`${f}: not a ${e.format}`];
  if (info.w !== e.w || info.h !== e.h) out.push(`${f}: ${info.w}x${info.h}, manifest says ${e.w}x${e.h}`);
  if (info.alpha !== e.alpha) out.push(`${f}: ${info.alpha ? "has" : "no"} alpha channel, manifest says alpha ${e.alpha}`);
  if (e.maxBytes && buf.length > e.maxBytes) out.push(`${f}: ${buf.length} bytes, manifest caps at ${e.maxBytes}`);
  return out;
}

// The manifest is validated once COMPOSE exists.
function validate() {
  const PLATFORMS = new Set(["play", "appstore", "press", "social"]);
  const FORMATS = new Set(["png", "jpg"]);
  for (const e of manifest.entries) {
    const bad = m => { throw new Error(`manifest ${e.id ?? "?"}: ${m}`); };
    if (!e.id || !PLATFORMS.has(e.platform)) bad("id and platform (play|appstore|press|social) required");
    if (!Number.isInteger(e.w) || !Number.isInteger(e.h) || e.w < 1 || e.h < 1) bad("w and h must be positive integers");
    if (!FORMATS.has(e.format)) bad("format must be png or jpg");
    if (typeof e.alpha !== "boolean") bad("alpha must be a boolean");
    if (e.format === "jpg" && e.alpha) bad("jpg cannot carry alpha");
    if (typeof COMPOSE[e.compose] !== "function") bad(`unknown compose ${e.compose}`);
    if (e.compose === "screenshot_frame" && !e.raw) bad("screenshot_frame needs raw (<device>/<n>)");
  }
  const ids = new Set(manifest.entries.map(e => e.id));
  if (ids.size !== manifest.entries.length) throw new Error("manifest: duplicate id");
  if (ONLY && !ids.has(ONLY)) throw new Error(`--only ${ONLY}: not in media.manifest.json`);
}

// ---------- modes ----------
function check(entries) {
  let bad = 0;
  for (const e of entries) {
    const p = problems(e);
    bad += p.length;
    console.log(p.length ? p.map(m => `  FAIL ${m}`).join("\n") : `  ok   ${e.platform}/${e.id}.${e.format} ${e.w}x${e.h}${e.alpha ? " alpha" : ""}`);
  }
  console.log(bad ? `${bad} problem(s)` : `${entries.length} outputs match media.manifest.json`);
  return bad === 0;
}

function install(entries) {
  const set = entries.filter(e => e.install);
  const missing = set.filter(e => !existsSync(outPath(e)));
  if (missing.length) throw new Error(`--install: render first, missing ${missing.map(e => e.id).join(", ")}`);
  const bad = set.flatMap(problems);
  if (bad.length) throw new Error(`--install: not copying, ${bad.join("; ")}`);
  mkdirSync(INSTALL_DIR, { recursive: true });
  for (const e of set) {
    const dst = join(INSTALL_DIR, e.install);
    copyFileSync(outPath(e), dst);
    console.log(`  ${e.id} -> ${dst}${existsSync(dst + ".meta") ? "" : "  (no .meta yet: let the editor import it, then commit the .meta)"}`);
  }
}

async function render(entries) {
  const pw = await import(process.env.PLAYWRIGHT || "playwright");
  const chromium = pw.chromium ?? pw.default.chromium;
  const b = await chromium.launch({ executablePath: process.env.CHROMIUM || undefined });
  const failed = [];
  try {
    for (const e of entries) {
      let doc;
      try { doc = COMPOSE[e.compose](e); }
      catch (err) { failed.push(`${e.id}: ${err.message}`); console.log(`  skip ${e.id}: ${err.message}`); continue; }
      const p = await b.newPage({ viewport: { width: e.w, height: e.h }, deviceScaleFactor: 1 });
      await p.setContent(`<!doctype html><html><head><style>html,body{margin:0;padding:0;overflow:hidden;background:transparent}svg{display:block}</style></head><body>${doc}</body></html>`, { waitUntil: "load" });
      const f = outPath(e);
      mkdirSync(dirname(f), { recursive: true });
      await p.screenshot({ path: f, omitBackground: e.alpha, type: e.format === "jpg" ? "jpeg" : "png", ...(e.format === "jpg" ? { quality: 92 } : {}) });
      await p.close();
      const bad = problems(e);
      if (bad.length) failed.push(...bad);
      console.log(`  ${bad.length ? "FAIL" : "wrote"} ${e.platform}/${e.id}.${e.format} ${e.w}x${e.h}${bad.length ? "  " + bad.join("; ") : ""}`);
    }
  } finally { await b.close(); }
  return failed;
}

validate();
const entries = ONLY ? manifest.entries.filter(e => e.id === ONLY) : manifest.entries;
let ok = true;
if (CHECK) {
  ok = check(entries);
} else if (INSTALL && args.length === 1) {
  install(entries);                      // --install alone: copy what is already rendered
} else {
  const failed = await render(entries);
  if (failed.length) { ok = false; console.log(`${failed.length} not rendered:\n  ${failed.join("\n  ")}`); }
  if (INSTALL) install(entries);
}
process.exit(ok ? 0 : 1);
