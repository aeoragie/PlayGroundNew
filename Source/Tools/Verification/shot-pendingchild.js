// 허브 승인 대기 자녀 카드 UI (Design.DashboardHub). sql-pendingchild.sql 실행 후.
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9561;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-pending-' + Date.now();

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});
const ready = (page, text) => page.waitForFunction(t => document.body.innerText.includes(t), { timeout: 40000, polling: 300 }, text);

async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json())?.data?.accessToken ?? null;
}

let pass = 0, fail = 0;
const check = (name, ok) => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}`); ok ? pass++ : fail++; };

(async () => {
    const edge = spawn(EDGE, ['--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank'], { stdio: 'ignore' });
    const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1280, height: 1000 });
        const token = await login('verify-player-u15@test.local');
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), token);
        await page.goto(BASE + '/dashboard', { waitUntil: 'networkidle2' });
        await ready(page, '내 자녀');
        // 리다이렉트 정착 대기
        let last = page.url();
        for (let s = 0; s < 3;) { await new Promise(r => setTimeout(r, 400)); const n = page.url(); s = n === last ? s + 1 : 0; last = n; }

        const view = await page.evaluate(() => {
            const txt = document.body.innerText;
            return {
                onHub: location.pathname === '/dashboard',
                pendingBadge: txt.includes('승인 대기'),
                connectedBadge: txt.includes('연결됨'),
                dash: txt.match(/–/g)?.length ?? 0,          // Pending 스탯 3칸
                waitNote: /관리자의 승인을 기다리고 있어요/.test(txt),
                requestBtn: [...document.querySelectorAll('a')].some(a => a.innerText.trim() === '요청 상태 보기'),
                requestHref: [...document.querySelectorAll('a')].find(a => a.innerText.trim() === '요청 상태 보기')?.getAttribute('href'),
                playerDashBtn: [...document.querySelectorAll('a')].some(a => a.innerText.trim() === '선수 대시보드'),
            };
        });
        console.log(JSON.stringify(view, null, 1));

        check('허브 표시(리다이렉트 안 됨)', view.onHub);
        check('승인 대기 뱃지', view.pendingBadge);
        check('연결됨 뱃지(Claimed 자녀)', view.connectedBadge);
        check('Pending 스탯 "–" 3칸', view.dash >= 3);
        check('대기 안내 문구', view.waitNote);
        check('요청 상태 보기 버튼 → /claim', view.requestBtn && view.requestHref === '/claim');
        check('Claimed 자녀는 선수 대시보드 버튼', view.playerDashBtn);
        await page.screenshot({ path: 'pendingchild-pc.png', fullPage: true });

        console.log(`\n${pass} PASS / ${fail} FAIL`);
    } finally {
        await browser.disconnect();
        edge.kill();
    }
})();
