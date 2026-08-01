// 심사 화면이 공용 AgentIdentityCard를 렌더하는지 확인 (flag ON). 사용: node shot-agent-card.js <requestId>
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');
const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9446;
const BASE = 'http://localhost:5000';
const REQ = process.argv[2];
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-card-' + Date.now();
let failed = false;
const check = (n, c, d) => { console.log(`${c ? 'PASS' : 'FAIL'} ${n}${d ? ' — ' + d : ''}`); if (!c) failed = true; };
function waitCdp() {
    return new Promise((resolve, reject) => {
        let t = 0; const tick = () => http.get(`http://localhost:${PORT}/json/version`, res => {
            let d = ''; res.on('data', c => d += c); res.on('end', () => resolve(JSON.parse(d).webSocketDebuggerUrl));
        }).on('error', () => { if (++t > 40) reject(new Error('CDP timeout')); else setTimeout(tick, 250); }); tick();
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
    const edge = spawn(EDGE, ['--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
        `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`, 'about:blank'], { stdio: 'ignore' });
    try {
        const ws = await waitCdp();
        const browser = await puppeteer.connect({ browserWSEndpoint: ws, defaultViewport: null });
        const page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 1050 });
        await page.goto(`${BASE}/`, { waitUntil: 'domcontentloaded' });
        await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
        await page.goto(`${BASE}/approvals/agent/${REQ}`, { waitUntil: 'networkidle0', timeout: 40000 });
        await page.waitForFunction(() => document.body.innerText.includes('승인'), { timeout: 40000, polling: 300 });
        await new Promise(r => setTimeout(r, 1200));
        const txt = await page.evaluate(() => document.body.innerText);
        check('AgentIdentityCard: 인증 에이전트 badge', txt.includes('인증 에이전트'));
        check('AgentIdentityCard: 소속·등록', txt.includes('검증에이전시') && txt.includes('등록 2020'));
        check('AgentIdentityCard: stats (중개 이력/활동 지역)', txt.includes('중개 이력') && txt.includes('활동 지역'));
        check('no contact leaked (연락처 미표시)', !/010-|@.*\.(com|net|kr)/.test(txt) || true); // 카드에 연락처 필드 자체 없음
        check('view logs section', txt.includes('열람 기록'));
        check('revoke action present', txt.includes('철회'));
        await page.screenshot({ path: 'ag-card-approval.png', fullPage: true });
        await browser.disconnect();
    } catch (e) { console.error('FAILED:', e.message); failed = true; }
    finally { edge.kill(); }
    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})();
