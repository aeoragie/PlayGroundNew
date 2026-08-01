// 에이전트 flag OFF/ON 게이팅 — 알림 패널의 "열람 승인 만료 임박"(AgentGrantExpiring) 행 노출 여부.
//  OFF: 미노출(0건) · ON: 노출(violet). 사용: node shot-agent-flag.js <off|on>
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9445;
const BASE = 'http://localhost:5000';
const MODE = (process.argv[2] ?? 'off').toLowerCase();
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-flag-' + Date.now();

let failed = false;
function check(name, cond, detail) {
    console.log(`${cond ? 'PASS' : 'FAIL'} ${name}${detail ? ' — ' + detail : ''}`);
    if (!cond) failed = true;
}
function waitCdp() {
    return new Promise((resolve, reject) => {
        let tries = 0;
        const tick = () => {
            http.get(`http://localhost:${PORT}/json/version`, res => {
                let d = ''; res.on('data', c => d += c);
                res.on('end', () => resolve(JSON.parse(d).webSocketDebuggerUrl));
            }).on('error', () => { if (++tries > 40) reject(new Error('CDP timeout')); else setTimeout(tick, 250); });
        };
        tick();
    });
}
async function login(email) {
    const r = await fetch(`${BASE}/api/auth/login/email`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json()).data.accessToken;
}

(async () => {
    const token = await login('verify-player-u15@test.local');
    const edge = spawn(EDGE, [
        '--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
        `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`, 'about:blank',
    ], { stdio: 'ignore', detached: false });
    try {
        const wsUrl = await waitCdp();
        const browser = await puppeteer.connect({ browserWSEndpoint: wsUrl, defaultViewport: null });
        const page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 1050 });
        await page.goto(`${BASE}/`, { waitUntil: 'domcontentloaded' });
        await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
        await page.goto(`${BASE}/dashboard/player/profile`, { waitUntil: 'networkidle0', timeout: 40000 });
        await page.waitForFunction(() => document.body.innerText.includes('항목별 공개 설정'), { timeout: 40000, polling: 300 });
        await new Promise(r => setTimeout(r, 1000));
        await page.evaluate(() => {
            const b = [...document.querySelectorAll('button[aria-label="알림"]')].find(x => x.offsetParent);
            if (b) b.click();
        });
        await new Promise(r => setTimeout(r, 2500));
        const rows = await page.evaluate(() =>
            (document.body.innerText.match(/열람 승인 만료 임박/g) || []).length);
        const expect = MODE === 'on';
        check(`[${MODE}] expiring row ${expect ? 'shown' : 'hidden'}`,
            expect ? rows > 0 : rows === 0, `count=${rows}`);
        await page.screenshot({ path: `ag-flag-${MODE}-panel.png` });
        await browser.disconnect();
    } catch (e) {
        console.error('FAILED:', e.message); failed = true;
    } finally {
        edge.kill();
    }
    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})();
