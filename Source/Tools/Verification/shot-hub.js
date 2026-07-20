// 허브 화면 + 라우팅 3분기 검증 (Design.DashboardHub).
// 핵심: 팀 관리자이면서 보호자인 계정이 /dashboard/player로 갈 수 있는가
//       (역할 가드였다면 허브 ↔ 대시보드를 무한히 오간다 — 이번 3단계의 진짜 위험).
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9531;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-hub-' + Date.now();

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

// 최종 도착지 — 리다이렉트가 끝난 뒤의 경로. 튕김이 남아 있으면 여기서 드러난다.
async function landOn(page, token, path) {
    await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), token);
    await page.goto(BASE + path, { waitUntil: 'networkidle2' });
    await page.waitForFunction(() => !document.body.innerText.includes('확인 중'),
        { polling: 300, timeout: 15000 }).catch(() => {});

    // 리다이렉트는 await 뒤에 일어난다 — URL이 멈출 때까지 기다리지 않으면 중간 상태를 잡는다(실제로 겪음)
    let last = page.url();
    for (let stable = 0; stable < 3;) {
        await new Promise(r => setTimeout(r, 400));
        const now = page.url();
        stable = now === last ? stable + 1 : 0;
        last = now;
    }
    return new URL(last).pathname + new URL(last).search;
}

(async () => {
    const edge = spawn(EDGE, [
        '--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank',
    ], { stdio: 'ignore' });
    const ws = await waitCdp();
    const browser = await puppeteer.connect({ browserWSEndpoint: ws, defaultViewport: null });
    const kill = () => edge.kill();

    const cases = [
        ['팀만 1개 → 팀 대시보드', 'verify-u15-1@test.local', '/dashboard', '/dashboard/team'],
        ['자녀 1명 → 선수 대시보드', 'verify-player-u12@test.local', '/dashboard', '/dashboard/player'],
        ['자녀 2명 → 허브 유지', 'verify-player-u15@test.local', '/dashboard', '/dashboard'],
        ['팀1+자녀1 → 허브 유지', 'verify-teamadmin-0713@test.local', '/dashboard', '/dashboard'],
        // 역할은 TeamAdmin 하나뿐인 계정이 선수 대시보드에 머무를 수 있어야 한다
        ['팀 관리자가 선수 대시보드 진입', 'verify-teamadmin-0713@test.local', '/dashboard/player', '/dashboard/player'],
        // 반대로 팀이 없는 계정은 팀 대시보드에서 허브로 되돌아간다
        ['자녀만 있는 계정이 팀 대시보드 진입', 'verify-player-u15@test.local', '/dashboard/team', '/dashboard'],
    ];

    for (const [label, email, from, expect] of cases) {
        const page = await browser.newPage();
        await page.setViewport({ width: 1280, height: 900 });
        const token = await login(email);
        const landed = await landOn(page, token, from);
        // 정확 비교 — startsWith면 '/dashboard/team'이 '/dashboard' 기대를 통과해 버린다(실제로 겪음)
        const ok = landed.split('?')[0] === expect;
        console.log(`${ok ? 'OK  ' : '실패'} ${label}: ${from} → ${landed} (기대 ${expect})`);
        await page.close();
    }

    //.// 허브 화면 내용 — PC
    const page = await browser.newPage();
    await page.setViewport({ width: 1280, height: 1000 });
    const token = await login('verify-teamadmin-0713@test.local');
    await landOn(page, token, '/dashboard');

    const seen = await page.evaluate(() => {
        const txt = document.body.innerText;
        const has = s => txt.includes(s);
        const emoji = (txt.match(/\p{Extended_Pictographic}/gu) || []).length;
        return {
            인사: has('안녕하세요'),
            처리섹션: has('처리가 필요해요'),
            내팀: has('내 팀'), 팀관리자라벨: has('팀 관리자'),
            내자녀: has('내 자녀'), 보호자라벨: has('보호자'),
            바로가기: has('바로가기'), 경기기록: has('경기기록'),
            오렌지버튼수: document.querySelectorAll('a.bg-orange').length,
            이모지수: emoji,
        };
    });
    console.log('\n[PC 허브 내용]', JSON.stringify(seen, null, 1));

    await page.screenshot({ path: 'hub-pc.png', fullPage: true });

    //.// 모바일 — 하단 탭바가 없어야 한다(진입 화면)
    await page.setViewport({ width: 390, height: 844 });
    await page.reload({ waitUntil: 'networkidle2' });
    await page.waitForFunction(() => document.body.innerText.includes('안녕하세요'), { polling: 300 });
    const mobile = await page.evaluate(() => ({
        하단탭바: !!document.querySelector('nav.fixed.bottom-0'),
        가로스크롤: document.documentElement.scrollWidth > window.innerWidth,
        주버튼높이: (() => {
            const b = [...document.querySelectorAll('a')].find(a => a.innerText.trim() === '팀 대시보드');
            return b ? Math.round(b.getBoundingClientRect().height) : 0;
        })(),
    }));
    console.log('[모바일]', JSON.stringify(mobile));
    await page.screenshot({ path: 'hub-mobile.png', fullPage: true });

    await browser.disconnect();
    kill();
})();
