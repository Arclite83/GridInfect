#!/usr/bin/env node
// Grid Infect visual assets generator (style lock 2026-09-04).
// node gen-assets.mjs [outDir]  ->  glyph SVGs, board background SVG, tokens.json
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { pathToFileURL } from "node:url";
const OUT = process.argv[2] ?? "out";
// gen-logo.mjs imports bug() and tokens from here; only emit when run directly.
const MAIN = process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href;

// ---------- tokens ----------
export const tokens = {
  skins: {
    default: { mask:"#7fae66", maskHi:"#97c27c", maskLo:"#5f8b4a", ink:"#0c190a",
               copper:"#c9a648", copperHi:"#f3e2a8", copperLo:"#7d6120",
               infect:"#d9204f", infectHi:"#ff6e93", infectLo:"#8f0a32", infectGlow:"rgba(217,32,79,.55)",
               glyphEdge:"#4a0018", glyphWire:"#300010" },
    blue:    { mask:"#2e5aa8", maskHi:"#3f70c4", maskLo:"#1f3f7a", ink:"#e6efff",
               copper:"#d9a441", copperHi:"#ffe08a", copperLo:"#7a5410",
               infect:"#ff8a00", infectHi:"#ffb347", infectLo:"#c25a00", infectGlow:"rgba(255,138,0,.55)",
               glyphEdge:"#4a2600", glyphWire:"#3a1d00" },
    breadboard: { mask:"#e9dcb8", maskHi:"#f4ead0", maskLo:"#cdbb8c", ink:"#3c2e12",
               copper:"#c46a3a", copperHi:"#f0a878", copperLo:"#7a3a18",
               infect:"#7fd100", infectHi:"#c8ff55", infectLo:"#3f7300", infectGlow:"rgba(127,209,0,.5)",
               glyphEdge:"#1e2e00", glyphWire:"#141f00" },
  },
  neutrals: { tip:"#ffffff", blockerBody:"#cfd8e0", blockerEdge:"#4d565f", wellBg:"rgba(0,0,0,.36)" },
  layout: { screen:[390,844], boardTop:138, cell:54, gap:5, wellPad:14, wellRadius:12, tileRadius:6,
            glyphOnTile:44, trayNext:58, trayQueued:40, traySlot:74, traySlotQueued:54, hudHeight:96 },
  type: { display:"Chakra Petch 500", mono:"Share Tech Mono", hudLevel:26, hudChip:12, silkscreen:9 },
};

// ---------- glyph grammar ----------
const S = tokens.skins.default;
const F=S.infect, E=S.glyphEdge, W=S.glyphWire, T=tokens.neutrals.tip;
const ANG={N:0,E:90,S:180,W:270,NE:45,SE:135,SW:225,NW:315};
const ORTH=["N","E","S","W"];
const VERTS=[{a:45,s:["N","E"]},{a:135,s:["E","S"]},{a:225,s:["S","W"]},{a:315,s:["W","N"]}];
const rot=a=>`transform="rotate(${a} 20 20)"`;

const lead =a=>`<g ${rot(a)}><rect x="17" y="2" width="6" height="12" rx="1" fill="${E}"/><rect x="18" y="2" width="4" height="5" fill="${T}"/></g>`;
const bond =a=>`<g ${rot(a)} fill="none" stroke="${W}" stroke-width="1"><path d="M14.5 14 L14.5 7 Q14.5 4.5 17 4.5"/><path d="M25.5 14 L25.5 7 Q25.5 4.5 23 4.5"/></g>`;
const stubs=a=>`<g ${rot(a)} stroke="${W}" stroke-width="1" fill="${W}"><line x1="25.5" y1="7.5" x2="28.5" y2="7.5"/><circle cx="29.1" cy="7.5" r="1.1"/><line x1="14.5" y1="7.5" x2="11.5" y2="7.5"/><circle cx="10.9" cy="7.5" r="1.1"/></g>`;
const diag =a=>`<g ${rot(a)}><line x1="20" y1="14.5" x2="20" y2="6.5" stroke="${E}" stroke-width="2.6"/><g fill="none" stroke="${W}" stroke-width="1"><path d="M17.6 13.5 L17.6 8.5 Q17.6 6.6 18.6 6.6"/><path d="M22.4 13.5 L22.4 8.5 Q22.4 6.6 21.4 6.6"/><line x1="17.6" y1="9.5" x2="14.6" y2="9.5"/><line x1="22.4" y1="9.5" x2="25.4" y2="9.5"/></g><circle cx="14" cy="9.5" r="1.1" fill="${W}"/><circle cx="26" cy="9.5" r="1.1" fill="${W}"/><circle cx="20" cy="5.5" r="3.2" fill="${E}"/><circle cx="20" cy="5.5" r="2.1" fill="${T}"/></g>`;
const pin=(a,t,len,pad)=>`<g transform="rotate(${a} 20 20) translate(${t} 0)" stroke="${W}" stroke-width="1.1" fill="${W}"><line x1="20" y1="10.5" x2="20" y2="${10.5-len}"/>${pad?`<circle cx="20" cy="${10.5-len-0.6}" r="1.1"/>`:""}</g>`;
const core=(fill=F,edge=E,center=`<circle cx="20" cy="20" r="3" fill="${T}"/>`)=>`<polygon points="20,9 30,14.5 30,25.5 20,31 10,25.5 10,14.5" fill="${fill}" stroke="${edge}" stroke-width="1.6" stroke-linejoin="round"/><polygon points="20,11 28,15.5 20,20 12,15.5" fill="rgba(255,255,255,.4)"/>${center}`;

// An edge's outer stub gives way to an active diagonal lead beside it (its branch pad lands where the stub was).
const CW={N:"NE",E:"SE",S:"SW",W:"NW"}, CCW={N:"NW",E:"NE",S:"SE",W:"SW"};
function body(active){
  let o="";
  for(const d of ORTH){ if(active.has(d)) continue;
    if(!active.has(CCW[d])) o+=pin(ANG[d],-4,3.5,true);
    o+=pin(ANG[d],0,2.5,false);
    if(!active.has(CW[d])) o+=pin(ANG[d],4,3.5,true); }
  for(const v of VERTS){ if(v.s.some(s=>active.has(s))||active.has(v.s.join(""))) continue; o+=pin(v.a,0,2.5,false); }
  return o;
}
/** dirs: any subset of N E S W NE SE SW NW */
export function bug(dirs){
  let o="";
  for(const d of dirs){ o += ORTH.includes(d) ? lead(ANG[d])+bond(ANG[d])+stubs(ANG[d]) : diag(ANG[d]); }
  return o+body(new Set(dirs))+core();
}
export function area(){
  const arcs=[0,90,180,270];
  return arcs.map(a=>`<g ${rot(a)} fill="none" stroke="${E}" stroke-width="2" stroke-linecap="round"><path d="M12 8 Q20 3 28 8"/></g>`).join("")
   + arcs.map(a=>`<g ${rot(a)} fill="none" stroke="${W}" stroke-width="1.2" stroke-linecap="round"><path d="M14.5 12 Q20 9 25.5 12"/></g>`).join("")
   + core();
}
export function blocker(){
  const n=tokens.neutrals;
  return body(new Set()) + core(n.blockerBody,n.blockerEdge,
    `<path d="M20 14 L25 16.5 V21 Q25 25 20 27 Q15 25 15 21 V16.5 Z" fill="${n.blockerEdge}"/><path d="M20 16 L23.5 17.8 V21 Q23.5 23.6 20 25 V16 Z" fill="${T}" opacity=".85"/>`);
}
const wrap=(inner,id)=>`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" width="40" height="40" id="${id}">\n${inner}\n</svg>\n`;

// ---------- board background (B5) ----------
export function boardBackground(skin=S){
  const traces=["12,140 12,300 26,314 26,700","378,140 378,240 364,254 364,720","60,808 150,808 164,822 240,822","230,20 300,20 314,34 350,34"];
  const ends=[[12,140],[26,700],[378,140],[364,720],[60,808],[240,822],[230,20],[350,34]];
  const holes=[[22,22],[368,22],[22,822],[368,822]];
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 390 844" width="390" height="844">
<defs>
 <linearGradient id="mask" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${skin.maskHi}"/><stop offset=".7" stop-color="${skin.mask}"/><stop offset="1" stop-color="${skin.maskLo}"/></linearGradient>
 <pattern id="grid24" width="24" height="24" patternUnits="userSpaceOnUse"><path d="M24 0 H0 V24" fill="none" stroke="rgba(255,255,255,.07)"/></pattern>
 <pattern id="grid12" width="12" height="12" patternUnits="userSpaceOnUse"><path d="M12 0 H0 V12" fill="none" stroke="rgba(0,0,0,.05)"/></pattern>
 <radialGradient id="sheen" cx=".5" cy=".12" r=".5"><stop offset="0" stop-color="rgba(255,255,255,.18)"/><stop offset="1" stop-color="rgba(255,255,255,0)"/></radialGradient>
 <radialGradient id="vig" cx=".5" cy=".5" r=".75"><stop offset=".6" stop-color="rgba(0,0,0,0)"/><stop offset="1" stop-color="rgba(0,0,0,.28)"/></radialGradient>
</defs>
<rect width="390" height="844" fill="url(#mask)"/>
<rect width="390" height="844" fill="url(#grid24)"/>
<rect width="390" height="844" fill="url(#grid12)"/>
<rect width="390" height="844" fill="url(#sheen)"/>
<g id="traces">${traces.map(p=>`<polyline points="${p}" fill="none" stroke="rgba(0,0,0,.14)" stroke-width="3" stroke-linejoin="round" stroke-linecap="round"/><polyline points="${p}" fill="none" stroke="rgba(255,255,255,.10)" stroke-width="1" stroke-linejoin="round" stroke-linecap="round" transform="translate(0 -1.5)"/>`).join("")}
${ends.map(([x,y])=>`<circle cx="${x}" cy="${y}" r="4" fill="rgba(0,0,0,.18)"/><circle cx="${x}" cy="${y}" r="1.6" fill="rgba(255,255,255,.35)"/>`).join("")}</g>
<g id="holes">${holes.map(([x,y])=>`<circle cx="${x}" cy="${y}" r="9" fill="${skin.copper}" opacity=".7"/><circle cx="${x}" cy="${y}" r="5" fill="#2a3a24"/>`).join("")}</g>
<g id="silkscreen" font-family="Share Tech Mono, monospace" font-size="9" fill="rgba(255,255,255,.55)" letter-spacing="1.5"><text x="44" y="30">BLOODHOUND STUDIOS</text><text x="290" y="835">GI-{LEVEL} REV B</text><text x="44" y="835">© 2026</text></g>
<rect width="390" height="844" fill="url(#vig)"/>
</svg>
`;
}

// ---------- emit ----------
if (MAIN) {
const gdir=join(OUT,"glyphs"); mkdirSync(gdir,{recursive:true});
const subsets=(names)=>{const r=[];for(let m=1;m<(1<<names.length);m++)r.push(names.filter((_,i)=>m&(1<<i)));return r;};
const files=[];
for(const d of subsets(ORTH)) files.push([`bug_${d.join("")}`,bug(d)]);
for(const d of subsets(["NE","SE","SW","NW"])) files.push([`bug_${d.join("")}`,bug(d)]);
for(const m of [["N","SE"],["N","SW"],["E","NW"],["E","SW"],["S","NE"],["S","NW"],["W","NE"],["W","SE"],["N","S","NE","SW"],["E","W","NE","SW"],["N","S","NW","SE"],["E","W","NW","SE"]]) files.push([`bug_${m.join("")}`,bug(m)]);
files.push(["bug_AREA",area()]);
files.push(["tile_BLOCKER",blocker()]);
let sheet=""; files.forEach(([n,g],i)=>{ writeFileSync(join(gdir,`${n}.svg`),wrap(g,n)); sheet+=`<g id="${n}" transform="translate(${(i%8)*44} ${Math.floor(i/8)*44})">${g}</g>\n`; });
const rows=Math.ceil(files.length/8);
writeFileSync(join(OUT,"glyph_sheet.svg"),`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 352 ${rows*44}" width="352" height="${rows*44}">\n${sheet}</svg>\n`);
writeFileSync(join(OUT,"board_background.svg"),boardBackground());
writeFileSync(join(OUT,"tokens.json"),JSON.stringify(tokens,null,2));
console.log(`wrote ${files.length} glyphs, glyph_sheet.svg, board_background.svg, tokens.json -> ${OUT}/`);
}
