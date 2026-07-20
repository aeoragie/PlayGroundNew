// 나머지 분기 2종: General(역할 미설정) → 역할 선택 화면 / 자녀 2명 허브 카드 반복.
// General 계정은 시드에 없어 **일시적으로 역할을 내렸다가 되돌린다**(아래 restore).
const puppeteer = require('puppeteer-core');
const { spawn, execFileSync } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9532;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-hubroles-' + Date.now();
const EMPTY = 'verify-empty-0714@test.local';

const sql = q => execFileSync('sqlcmd',
    ['-S', '.\\SQLEXPRESS', '-d', 'PlayGround_Account', '-E', '-b', '-h', '-1', '-W', '-Q', q],
    { encoding: 'utf8' });

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});

async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json())?.data?.accessToken ?? null;
}

async function land(page, token, path) {
    await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), token);
    await page.goto(BASE + path, { waitUntil: 'networkidle2' });
    await page.waitForFunction(() => !document.body.innerText.includes('확인 중'), { polling: 300, timeout: 15000 }).catch(() => {});
    let last = page.url();
    for (let s = 0; s < 3;) {
        await new Promise(r => setTimeout(r, 400));
        const now = page.url();
        s = now === last ? s + 1 : 0;
        last = now;
    }
    return new URL(last).pathname;
}

(async () => {
    const edge = spawn(EDGE, ['--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank'], { stdio: 'ignore' });
    const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });

    try {
        //.// 1) General — 역할을 일시적으로 내린다 (JWT는 로그인 시점에 발급되므로 재로그인이 필요)
        sql(`UPDATE Users SET UserRole='General' WHERE Email='${EMPTY}'`);
        const page = await browser.newPage();
        await page.setViewport({ width: 1280, height: 900 });

        const path = await land(page, await login(EMPTY), '/dashboard');
        const roleSelect = await page.evaluate(() => ({
            머무름: location.pathname === '/dashboard',
            안내: document.body.innerText.includes('아직 역할을 선택하지 않았어요'),
            버튼: !!document.body.innerText.includes('역할 선택하기'),
        }));
        console.log(`[General] 도착 ${path} — ${JSON.stringify(roleSelect)}`);
        await page.screenshot({ path: 'hub-general.png' });

        //.// 2) 자녀 2명 — 카드가 반복되는지
        const page2 = await browser.newPage();
        await page2.setViewport({ width: 1280, height: 1000 });
        await land(page2, await login('verify-player-u15@test.local'), '/dashboard');
        const kids = await page2.evaluate(() => {
            const names = [...document.querySelectorAll('h3')].map(h => h.innerText.trim());
            return { 자녀카드수: names.length, 이름: names, 팀섹션: document.body.innerText.includes('내 팀') };
        });
        console.log(`[자녀 2명] ${JSON.stringify(kids)} (팀섹션은 false 기대 — 팀이 없으면 섹션을 그리지 않는다)`);
        await page2.screenshot({ path: 'hub-children.png', fullPage: true });
    } finally {
        //.// restore — 검증에 쓴 상태는 반드시 되돌린다
        sql(`UPDATE Users SET UserRole='TeamAdmin' WHERE Email='${EMPTY}'`);
        console.log('\nrestore: verify-empty-0714 역할 → TeamAdmin 복구');
        await browser.disconnect();
        edge.kill();
    }
})();
