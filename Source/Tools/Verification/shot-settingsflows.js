// 계정 설정 플로우 3종 UI 렌더 확인 (Design.SettingsFlows). 계정 탭 · 이름 변경 모달 · 내려받기 모달 · LINE 준비 중 행.
// 데이터 무변경 — 다이얼로그를 열기만 하고 제출하지 않는다.
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9561;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-sf-' + Date.now();

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
const clickText = (page, text) => page.evaluate(t => { const el = [...document.querySelectorAll('button')].find(x => x.innerText.trim() === t && x.getBoundingClientRect().width > 0); if (el) { el.click(); return true; } return false; }, text);

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
    const token = await login('verify-teamadmin-0713@test.local');
    if (!token) { console.log('login FAILED'); return; }

    const edge = spawn(EDGE, ['--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank'], { stdio: 'ignore' });
    const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });

    try {
        const page = await browser.newPage();
        page.on('pageerror', e => console.log('PAGE ERROR:', e.message));
        await page.setViewport({ width: 1280, height: 1000 });
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), token);
        await page.goto(BASE + '/settings/account', { waitUntil: 'networkidle2' });
        await ready(page, '연결된 로그인');
        await sleep(600);

        check('이름 변경 버튼', await has(page, '이름 변경'));
        check('연결된 로그인 섹션', await has(page, '연결된 로그인'));
        check('LINE 준비 중 행', await has(page, 'LINE') && await has(page, '준비 중'));
        check('데이터 내려받기 행', await has(page, '데이터 내려받기'));
        check('계정 삭제 행', await has(page, '계정 삭제'));
        await page.screenshot({ path: 'settingsflows-account-pc.png', fullPage: true });

        //.// 이름 변경 모달 — 반영 범위 안내
        await clickText(page, '이름 변경');
        await sleep(500);
        check('이름 변경 모달 열림', await has(page, '바뀌는 곳') && await has(page, '바뀌지 않는 곳'));
        check('반영 범위 카피', await has(page, '팀에 보이는 보호자 이름') && await has(page, '리뷰의 마스킹'));
        await page.screenshot({ path: 'settingsflows-name-pc.png', fullPage: true });
        await clickText(page, '취소');
        await sleep(400);

        //.// 데이터 내려받기 모달 — 3 체크 + 안내
        await clickText(page, '요청');
        await sleep(500);
        check('내려받기 모달 열림', await has(page, '포함 항목'));
        check('3 항목 카피', await has(page, '계정·프로필') && await has(page, '경기 기록·커리어') && await has(page, '요청·신청 내역'));
        check('24시간·7일 안내', await has(page, '24시간') && await has(page, '7일'));
        await page.screenshot({ path: 'settingsflows-export-pc.png', fullPage: true });
        await clickText(page, '취소');
        await sleep(400);

        //.// 모바일
        await page.setViewport({ width: 390, height: 900, isMobile: true });
        await page.goto(BASE + '/settings/account', { waitUntil: 'networkidle2' });
        await ready(page, '연결된 로그인');
        await sleep(600);
        check('모바일 계정 탭 렌더', await has(page, '이름 변경') && await has(page, 'LINE'));
        const noHScroll = await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1);
        check('모바일 가로 스크롤 없음', noHScroll);
        await page.screenshot({ path: 'settingsflows-account-mobile.png', fullPage: true });

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
