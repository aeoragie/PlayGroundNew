// 알림 센터 UI 검증 (DECISION.NOTIFICATIONCENTER). 딥링크 진입 · 세그먼트 URL 왕복 · 패널 "전체 보기" · 더 보기 20 · 90일 캡션.
// 계정 = verify-player-u15 (보호자, 시드 24건 · 안읽음 13).
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9563;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-noti-' + Date.now();

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});
const sleep = ms => new Promise(r => setTimeout(r, ms));
const ready = (page, text) => page.waitForFunction(t => document.body.innerText.includes(t), { timeout: 40000, polling: 300 }, text);
const has = (page, text) => page.evaluate(t => document.body.innerText.includes(t), text);

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
    const token = await login('verify-player-u15@test.local');
    if (!token) { console.log('login FAILED'); return; }

    const edge = spawn(EDGE, ['--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank'], { stdio: 'ignore' });
    const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });

    try {
        //.// 1) 비로그인 딥링크 → /login?returnUrl 로 이동
        const guest = await browser.newPage();
        await guest.setViewport({ width: 1280, height: 1000 });
        await guest.goto(BASE + '/notifications', { waitUntil: 'networkidle2' });
        await guest.waitForFunction(() => location.pathname.includes('login'), { timeout: 20000 }).catch(() => {});
        const guestUrl = guest.url();
        check('비로그인 딥링크 → 로그인 이동', guestUrl.includes('login'));
        check('returnUrl에 notifications 보존', decodeURIComponent(guestUrl).includes('/notifications'));
        await guest.close();

        //.// 2) 로그인 후 페이지 로드 (PC)
        const page = await browser.newPage();
        page.on('pageerror', e => console.log('PAGE ERROR:', e.message));
        await page.setViewport({ width: 1280, height: 1100 });
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), token);
        await page.goto(BASE + '/notifications', { waitUntil: 'networkidle2' });
        await ready(page, '알림');
        await sleep(600);
        check('헤더 "알림"', await has(page, '알림'));
        check('세그먼트 3종', await has(page, '전체') && await has(page, '처리 필요') && await has(page, '읽지 않음'));
        check('90일 캡션', await has(page, '최근 90일'));
        check('더 보기 버튼(25건>20)', await has(page, '더 보기'));
        // 안읽음 오렌지 점 존재 (첫 로드 — 서버 읽음 처리해도 이번 화면 점은 유지)
        const dot = await page.evaluate(() => [...document.querySelectorAll('span')].some(s => getComputedStyle(s).backgroundColor === 'rgb(255, 107, 53)' && s.getBoundingClientRect().width > 0 && s.getBoundingClientRect().width < 12));
        check('안읽음 오렌지 점 존재', dot);
        // 액션형(연결 요청) 우측 승인/거절 버튼
        check('액션형 승인/거절 버튼', await has(page, '승인') && await has(page, '거절'));
        await page.screenshot({ path: 'noti-page-pc.png', fullPage: true });

        //.// 3) 세그먼트 URL 왕복 — 처리 필요 클릭 → ?filter=action → 새로고침 유지
        await page.evaluate(() => { const b = [...document.querySelectorAll('button')].find(x => x.innerText.trim().startsWith('처리 필요')); b && b.click(); });
        await sleep(500);
        check('처리 필요 → URL ?filter=action', page.url().includes('filter=action'));
        await page.reload({ waitUntil: 'networkidle2' });
        await ready(page, '처리 필요');
        await sleep(500);
        check('새로고침 후 filter=action 유지', page.url().includes('filter=action'));
        check('처리 필요 필터에 액션형만', await has(page, '프로필 연결 요청'));
        await page.screenshot({ path: 'noti-page-action.png', fullPage: true });

        // 전체로 복귀 → 쿼리 제거
        await page.evaluate(() => { const b = [...document.querySelectorAll('button')].find(x => x.innerText.trim().startsWith('전체')); b && b.click(); });
        await sleep(500);
        check('전체 클릭 → filter 쿼리 제거', !page.url().includes('filter='));

        //.// 4) 더 보기 → 25건까지
        await page.evaluate(() => { const b = [...document.querySelectorAll('button')].find(x => x.innerText.trim() === '더 보기'); b && b.click(); });
        await sleep(800);
        const rowCount = await page.evaluate(() => document.querySelectorAll('main .flex.flex-col.gap-2\\.5 > *').length);
        check('더 보기 후 행 증가(>20)', rowCount > 20);

        //.// 5) 패널 "전체 보기" 행 → /notifications (허브에서 벨 열기)
        const hub = await browser.newPage();
        await hub.setViewport({ width: 1280, height: 1000 });
        await hub.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), token);
        await hub.goto(BASE + '/dashboard/player', { waitUntil: 'networkidle2' });
        await sleep(1500);
        // 벨 클릭
        await hub.evaluate(() => { const b = document.querySelector('button[aria-label="알림"]'); b && b.click(); });
        await sleep(800);
        check('패널에 "전체 보기" 행', await has(hub, '전체 보기'));
        await hub.evaluate(() => { const b = [...document.querySelectorAll('button')].find(x => x.innerText.trim() === '전체 보기'); b && b.click(); });
        await hub.waitForFunction(() => location.pathname === '/notifications', { timeout: 10000 }).catch(() => {});
        check('"전체 보기" → /notifications 이동', hub.url().includes('/notifications'));

        console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
        process.exitCode = fail > 0 ? 1 : 0;
    } catch (e) {
        console.error('FAILED:', e.message);
        process.exitCode = 1;
    } finally {
        await browser.disconnect();
        edge.kill();
    }
})();
