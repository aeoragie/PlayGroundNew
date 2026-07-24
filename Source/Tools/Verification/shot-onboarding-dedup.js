// 온보딩 중복 방지 UI — 이미 팀이 있는 계정이 /onboarding/team에 가면 폼 없이 대시보드로 리다이렉트.
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9601;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-onbdedup-' + Date.now();

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => { let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl)); })
        .on('error', () => { if (++t > 60) rej(new Error('to')); else setTimeout(k, 250); });
    k();
});
async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password: 'password123!' }) });
    return (await r.json())?.data?.accessToken ?? null;
}

let pass = 0, fail = 0;
const check = (n, ok) => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${n}`); ok ? pass++ : fail++; };

(async () => {
    const edge = spawn(EDGE, ['--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`, '--no-first-run', '--disable-gpu', 'about:blank'], { stdio: 'ignore' });
    const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });
    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1000, height: 800 });
        const token = await login('verify-teamadmin-0713@test.local'); // 이미 검증fc 보유
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), token);
        await page.goto(BASE + '/onboarding/team', { waitUntil: 'networkidle2' });

        // 리다이렉트 정착 대기
        let last = page.url();
        for (let s = 0; s < 3;) { await new Promise(r => setTimeout(r, 400)); const n = page.url(); s = n === last ? s + 1 : 0; last = n; }
        const path = new URL(page.url()).pathname;

        check('이미 팀 보유 → /onboarding/team에서 벗어남', path !== '/onboarding/team');
        check('팀 온보딩 폼 미표시', !(await page.evaluate(() => document.body.innerText.includes('팀 정보') && document.body.innerText.includes('선수단 등록'))));
        console.log('도착 경로:', path);

        console.log(`\n${pass} PASS / ${fail} FAIL`);
    } finally {
        await browser.disconnect();
        edge.kill();
    }
})();
