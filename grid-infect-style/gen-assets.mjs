#!/usr/bin/env node
// Grid Infect visual assets generator (style lock 2026-09-04).
// node gen-assets.mjs [outDir]  ->  glyph SVGs, board background SVG, tokens.json
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
const OUT = process.argv[2] ?? "out";

// ---------- tokens ----------
export const tokens = {
  skins: {
    default: { mask:"#7fae66", maskHi:"#97c27c", maskLo:"#5f8b4a", ink:"#1d3316",
               copper:"#c9a648", copperHi:"#f3e2a8", copperLo:"#7d6120",
               infect:"#ff2d95", infectHi:"#ff7cc4", infectLo:"#b3086a", infectGlow:"rgba(255,45,149,.55)",
               glyphEdge:"#5a0033", glyphWire:"#3a0b22" },
    blue:    { mask:"#2e5aa8", maskHi:"#3f70c4", maskLo:"#1f3f7a", ink:"#e6efff",
               copper:"#d9a441", copperHi:"#ffe08a", copperLo:"#7a5410",
               infect:"#ff8a00", infectHi:"#ffb347", infectLo:"#c25a00", infectGlow:"rgba(255,138,0,.55)",
               glyphEdge:"#4a2600", glyphWire:"#3a1d00" },
    breadboard: { mask:"#e9dcb8", maskHi:"#f4ead0", maskLo:"#cdbb8c", ink:"#3c2e12",
               copper:"#c46a3a", copperHi:"#f0a878", copperLo:"#7a3a18",
               infect:"#ff2d3a", infectHi:"#ff6b6b", infectLo:"#b3101c", infectGlow:"rgba(255,45,58,.5)",
               glyphEdge:"#5a0008", glyphWire:"#3a0008" },
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
const diag =a=>`<g ${rot(a)}><line x1="20" y1="14" x2="20" y2="6" stroke="${E}" stroke-width="2.4"/><circle cx="20" cy="5" r="2.6" fill="${E}"/><circle cx="20" cy="5" r="1.3" fill="${T}"/></g>`;
const pin=(a,t,len,pad)=>`<g transform="rotate(${a} 20 20) translate(${t} 0)" stroke="${W}" stroke-width="1.1" fill="${W}"><line x1="20" y1="10.5" x2="20" y2="${10.5-len}"/>${pad?`<circle cx="20" cy="${10.5-len-0.6}" r="1.1"/>`:""}</g>`;
const core=(fill=F,edge=E,center=`<circle cx="20" cy="20" r="3" fill="${T}"/>`)=>`<polygon points="20,9 30,14.5 30,25.5 20,31 10,25.5 10,14.5" fill="${fill}" stroke="${edge}" stroke-width="1.6" stroke-linejoin="round"/><polygon points="20,11 28,15.5 20,20 12,15.5" fill="rgba(255,255,255,.4)"/>${center}`;

function body(active){
  let o="";
  for(const d of ORTH){ if(active.has(d)) continue; o+=pin(ANG[d],-4,3.5,true)+pin(ANG[d],0,2.5,false)+pin(ANG[d],4,3.5,true); }
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
   + arcs.map(a=>`<g ${rot(a)}><circle cx="20" cy="3.5" r="1.6" fill="${T}"/></g>`).join("") + core();
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
