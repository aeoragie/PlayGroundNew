// Edge를 직접 헤드리스로 띄우고 puppeteer.connect로 붙어 스크린샷 (launch 핸드셰이크 우회)
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9444;
const BASE = 'http://localhost:5000';
const URL = `${BASE}/team/${encodeURIComponent('검증fc')}/record`;
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-connect-' + Date.now();

function waitCdp() {
    return new Promise((resolve, reject) => {
        let tries = 0;
        const tick = () => {
            http.get(`http://localhost:${PORT}/json/version`, res => {
                let d = '';
                res.on('data', c => d += c);
                res.on('end', () => resolve(JSON.parse(d).webSocketDebuggerUrl));
            }).on('error', () => {
                if (++tries > 40) { reject(new Error('CDP timeout')); }
                else { setTimeout(tick, 250); }
            });
        };
        tick();
    });
}

(async () => {
    const edge = spawn(EDGE, [
        '--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
        `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`, 'about:blank',
    ], { stdio: 'ignore', detached: false });

    try {
        const wsUrl = await waitCdp();
        const browser = await puppeteer.connect({ browserWSEndpoint: wsUrl, defaultViewport: null });
        const page = await browser.newPage();
        page.on('pageerror', e => console.log('PAGE ERROR:', e.message));

        await page.setViewport({ width: 1440, height: 1100 });
        await page.goto(URL, { waitUntil: 'networkidle0', timeout: 30000 });
        await page.waitForFunction(() => document.body.innerText.includes('최근 경기'), { timeout: 15000 });
        await new Promise(r => setTimeout(r, 400));
        await page.screenshot({ path: 'public-record-pc.png', fullPage: true });
        console.log('pc OK');

        await page.setViewport({ width: 390, height: 900, isMobile: true });
        await page.goto(URL, { waitUntil: 'networkidle0' });
        await page.waitForFunction(() => document.body.innerText.includes('최근 경기'), { timeout: 15000 });
        await new Promise(r => setTimeout(r, 400));
        await page.screenshot({ path: 'public-record-mobile.png', fullPage: true });
        console.log('mobile OK');

        await browser.disconnect();
    } catch (e) {
        console.error('FAILED:', e.message);
        process.exitCode = 1;
    } finally {
        edge.kill();
    }
})();
