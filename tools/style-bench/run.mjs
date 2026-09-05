import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
const here = dirname(fileURLToPath(import.meta.url));
// PLAYWRIGHT=/path/to/playwright/index.mjs when it is not installed next to this file.
const { chromium } = await import(process.env.PLAYWRIGHT || 'playwright');
const b = await chromium.launch({ executablePath: process.env.CHROMIUM || undefined, args: ['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader'] });
const p = await b.newPage({ viewport: { width: 780, height: 1688 } });
p.on('console', m => { if (m.type() === 'error' || m.type() === 'warning') console.log('[console]', m.text().slice(0, 3000)); });
p.on('pageerror', e => console.log('[pageerror]', e.message));

await p.goto('file://' + join(here, 'bench.html'), { waitUntil: 'load' });
await p.waitForTimeout(1500);
for (const t of [0.22, 3.0]) {
  await p.evaluate(t => window.render(t), t);
  await p.waitForTimeout(200);
  await p.screenshot({ path: join(here, `shot_${t}.png`) });
}
await b.close(); console.log('done');
