// Optional documentation renderer: npm install --no-save playwright
// Use Playwright's Chromium, or set JEI_CHROME_PATH to an installed Chrome binary.
const { chromium } = require('playwright');
const path = require('path');
const { pathToFileURL } = require('url');

(async () => {
  const options = { headless: true };
  if (process.env.JEI_CHROME_PATH) options.executablePath = process.env.JEI_CHROME_PATH;
  const browser = await chromium.launch(options);
  try {
    const page = await browser.newPage({ viewport: { width: 1680, height: 1120 }, deviceScaleFactor: 1.5 });
    await page.goto(pathToFileURL(path.join(__dirname, 'Floor01_Plan.svg')).href);
    await page.locator('svg').screenshot({ path: path.join(__dirname, 'Floor01_Plan.png') });
    await page.goto(pathToFileURL(path.join(__dirname, 'Floor01_Plan.html')).href);
    await page.locator('#room-N01').click();
    const detail = await page.locator('#detail').innerText();
    if (!detail.includes('55.6 m²') || !detail.includes('8.30 × 6.70')) throw Error('room information check failed');
    await page.locator('#detail .close').click();
    await page.locator('#route-toggle').click();
    if (await page.locator('#route-layer').evaluate(e => getComputedStyle(e).display) !== 'block') throw Error('route toggle failed');
    await page.screenshot({ path: path.join(__dirname, 'Floor01_Preview_QA.png'), fullPage: true });
    console.log(JSON.stringify({ svgRendered: true, roomClick: true, netAreaAndDimensions: true, routeToggle: true, rooms: await page.locator('.room').count() }));
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exit(1); });
