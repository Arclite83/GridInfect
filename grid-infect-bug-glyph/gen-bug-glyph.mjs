#!/usr/bin/env node
// Grid Infect bug glyph v1b generator. Emits one SVG per direction set into ./out.
// Usage: node gen-bug-glyph.mjs [outDir]
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const OUT = process.argv[2] ?? "out";

// Palette. Skin-swappable: these are the default (green mask) values.
const C = {
  body: "#ff2d95",   // infection fill
  edge: "#5a0033",   // lead + outline
  wire: "#3a0b22",   // bond wires, branch stubs, pads
  tip:  "#ffffff",   // lit lead tip, core highlight
  gloss:"rgba(255,255,255,.4)",
};

// Geometry, 40x40 viewBox, center (20,20).
const G = {
  lead:      { x: 17, y: 2, w: 6, h: 12, rx: 1 },     // active lead body
  tip:       { x: 18, y: 2, w: 4, h: 5 },             // lit tip
  bondX:     [14.5, 25.5],                            // bond wire x, either side of lead
  bondTop:   7, bondBase: 14, bondHook: 4.5,          // straight run 14->7, hook to y=4.5 at x=17/23
  stubY:     7.5, stubLen: 3, padR: 1.1,              // lead branch stubs, leave bond wire outward
  hex:       "20,9 30,14.5 30,25.5 20,31 10,25.5 10,14.5",
  hexGloss:  "20,11 28,15.5 20,20 12,15.5",
  coreDot:   3,
  sidePins:  [ [-4, 3.5, true], [0, 2.5, false], [4, 3.5, true] ], // [offset, length, pad] per inactive side
  vertexPin: [0, 2.5, false],                          // one per hex vertex between two inactive sides
  pinBase:   10.5,
  strokeLead: 1.6, strokeWire: 1, strokePin: 1.1,
};

const ANG = { N: 0, E: 90, S: 180, W: 270 };
const ALL = ["N", "E", "S", "W"];
const VERTS = [
  { a: 45,  s: ["N", "E"] }, { a: 135, s: ["E", "S"] },
  { a: 225, s: ["S", "W"] }, { a: 315, s: ["W", "N"] },
];

const rot = (d) => `transform="rotate(${ANG[d]} 20 20)"`;

const lead = (d) => `<g ${rot(d)}>
  <rect x="${G.lead.x}" y="${G.lead.y}" width="${G.lead.w}" height="${G.lead.h}" rx="${G.lead.rx}" fill="${C.edge}"/>
  <rect x="${G.tip.x}" y="${G.tip.y}" width="${G.tip.w}" height="${G.tip.h}" fill="${C.tip}"/>
</g>`;

const bond = (d) => {
  const [l, r] = G.bondX;
  return `<g ${rot(d)} fill="none" stroke="${C.wire}" stroke-width="${G.strokeWire}">
  <path d="M${l} ${G.bondBase} L${l} ${G.bondTop} Q${l} ${G.bondHook} 17 ${G.bondHook}"/>
  <path d="M${r} ${G.bondBase} L${r} ${G.bondTop} Q${r} ${G.bondHook} 23 ${G.bondHook}"/>
</g>`;
};

const leadStubs = (d) => {
  const [l, r] = G.bondX, y = G.stubY, n = G.stubLen, pr = G.padR;
  return `<g ${rot(d)} stroke="${C.wire}" stroke-width="${G.strokeWire}" fill="${C.wire}">
  <line x1="${r}" y1="${y}" x2="${r + n}" y2="${y}"/><circle cx="${r + n + pr * 0.55}" cy="${y}" r="${pr}"/>
  <line x1="${l}" y1="${y}" x2="${l - n}" y2="${y}"/><circle cx="${l - n - pr * 0.55}" cy="${y}" r="${pr}"/>
</g>`;
};

const pin = (a, t, len, pad) => {
  const y0 = G.pinBase, y1 = y0 - len;
  return `<g transform="rotate(${a} 20 20) translate(${t} 0)" stroke="${C.wire}" stroke-width="${G.strokePin}" fill="${C.wire}">
  <line x1="20" y1="${y0}" x2="20" y2="${y1}"/>${pad ? `<circle cx="20" cy="${y1 - 0.6}" r="${G.padR}"/>` : ""}
</g>`;
};

const core = () => `<polygon points="${G.hex}" fill="${C.body}" stroke="${C.edge}" stroke-width="${G.strokeLead}" stroke-linejoin="round"/>
<polygon points="${G.hexGloss}" fill="${C.gloss}"/>
<circle cx="20" cy="20" r="${G.coreDot}" fill="${C.tip}"/>`;

export function glyph(dirs) {
  let o = "";
  for (const d of dirs) o += lead(d) + bond(d) + leadStubs(d);
  for (const d of ALL) {
    if (dirs.includes(d)) continue;
    for (const [t, len, pad] of G.sidePins) o += pin(ANG[d], t, len, pad);
  }
  for (const v of VERTS) {
    if (v.s.some((s) => dirs.includes(s))) continue;
    o += pin(v.a, ...G.vertexPin);
  }
  return o + core();
}

export const svg = (dirs, id) =>
`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" width="40" height="40" id="${id}">
${glyph(dirs)}
</svg>
`;

// All 15 non-empty direction subsets, canonical order N E S W.
const subsets = [];
for (let m = 1; m < 16; m++) subsets.push(ALL.filter((_, i) => m & (1 << i)));

mkdirSync(OUT, { recursive: true });
let sheet = "";
subsets.forEach((dirs, i) => {
  const name = `bug_${dirs.join("")}`;
  writeFileSync(join(OUT, `${name}.svg`), svg(dirs, name));
  const x = (i % 5) * 44, y = Math.floor(i / 5) * 44;
  sheet += `<g id="${name}" transform="translate(${x} ${y})">${glyph(dirs)}</g>\n`;
});
writeFileSync(join(OUT, "bug_sheet.svg"),
`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 220 132" width="220" height="132">
${sheet}</svg>
`);
console.log(`wrote ${subsets.length} glyphs + bug_sheet.svg to ${OUT}/`);
