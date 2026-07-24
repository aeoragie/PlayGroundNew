// 보호자 기록 수정 신청 UI (Design.RecordCorrection). 선수 대시보드 시즌 통계.
// 공식 행 ⋯ → 기록 수정 신청 → 폼 → 토스트 + 목록 반영 + "신청 처리 중" / ⋯ 취소 → 확인 모달 → 토스트.
const puppeteer = require('puppeteer-core');
const { spawn, execFileSync } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9571;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-gcorr-' + Date.now();

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});
const sleep = ms => new Promise(r => setTimeout(r, ms));
const ready = (page, text) => page.waitForFunction(t => document.body.innerText.includes(t), { timeout: 40000, polling: 300 }, text);
const clickText = (page, text) => page.evaluate(t => {
    const el = [...document.querySelectorAll('button, a')].find(x => x.innerText.trim() === t && x.getBoundingClientRect().width > 0);
    if (el) { el.click(); return true; } return false;
}, text);

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
        await page.goto(BASE + '/dashboard/player/stats', { waitUntil: 'networkidle2' });
        await ready(page, '경기별 기록');
        await sleep(500);

        // 공식 경기 행의 ⋯ 개수(친선 행엔 없음) — 최소 1개
        const dotsCount = await page.evaluate(() =>
            [...document.querySelectorAll('button[aria-label$="추가 작업"]')].filter(b => b.getBoundingClientRect().width > 0).length);
        check('공식 경기 행에 ⋯ 있음', dotsCount >= 1);

        // 첫 ⋯ 클릭 → "기록 수정 신청"
        await page.evaluate(() => {
            const b = [...document.querySelectorAll('button[aria-label$="추가 작업"]')].find(x => x.getBoundingClientRect().width > 0);
            b?.click();
        });
        await sleep(400);
        check('⋯ 메뉴 "기록 수정 신청"', await page.evaluate(() => document.body.innerText.includes('기록 수정 신청')));

        await clickText(page, '기록 수정 신청');
        await sleep(500);
        check('폼 열림(현재→올바른 대비)', await page.evaluate(() =>
            document.body.innerText.includes('현재 기록') && document.body.innerText.includes('올바른 기록')));

        // 득점·도움 항목 선택 후 값 입력
        await clickText(page, '득점·도움');
        await sleep(200);
        await page.evaluate(() => {
            const inp = document.querySelector('input[aria-label="올바른 기록"]');
            if (inp) { const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set; setter.call(inp, '득점 3 · 도움 1'); inp.dispatchEvent(new Event('input', { bubbles: true })); }
        });
        await sleep(300);
        await clickText(page, '신청하기');
        await sleep(900);

        check('신청 토스트', await page.evaluate(() => document.body.innerText.includes('수정 신청을 보냈어요')));
        check('신청 목록 표시', await page.evaluate(() => document.body.innerText.includes('기록 수정 신청') && /\d건/.test(document.body.innerText)));
        await page.screenshot({ path: 'gcorr-pc.png', fullPage: true });

        // 같은 경기 ⋯ → "신청 처리 중" 비활성
        await page.evaluate(() => {
            const b = [...document.querySelectorAll('button[aria-label$="추가 작업"]')].find(x => x.getBoundingClientRect().width > 0);
            b?.click();
        });
        await sleep(400);
        check('중복 시 "신청 처리 중" 비활성', await page.evaluate(() => document.body.innerText.includes('신청 처리 중')));
        // 메뉴 닫기
        await page.keyboard.press('Escape').catch(() => {});
        await page.mouse.click(5, 5);
        await sleep(300);

        // 신청 목록 ⋯ → 신청 취소 → 확인 모달 → 취소
        const listDots = await page.evaluate(() => {
            // 목록 카드 안의 ⋯ (신청 요약이 있는 행)
            const all = [...document.querySelectorAll('button[aria-label$="추가 작업"]')].filter(b => b.getBoundingClientRect().width > 0);
            const last = all[all.length - 1];
            if (last) { last.click(); return true; } return false;
        });
        await sleep(400);
        await clickText(page, '신청 취소');
        await sleep(400);
        check('취소 확인 모달', await page.evaluate(() => document.body.innerText.includes('이 수정 신청을 취소할까요')));
        await clickText(page, '신청 취소');
        await sleep(900);
        check('취소 토스트', await page.evaluate(() => document.body.innerText.includes('수정 신청을 취소했어요')));

        // 모바일 — ⋯ 존재 + 가로 스크롤 없음
        await page.setViewport({ width: 390, height: 844 });
        await page.reload({ waitUntil: 'networkidle2' });
        await ready(page, '경기별 기록');
        const mobile = await page.evaluate(() => ({
            dots: [...document.querySelectorAll('button[aria-label$="추가 작업"]')].some(b => b.getBoundingClientRect().width > 0),
            hScroll: document.documentElement.scrollWidth > window.innerWidth,
        }));
        check('모바일 공식 행 ⋯ 있음', mobile.dots);
        check('모바일 가로 스크롤 없음', !mobile.hScroll);
        await page.screenshot({ path: 'gcorr-mobile.png', fullPage: true });

        console.log(`\n${pass} PASS / ${fail} FAIL`);
    } finally {
        // 검증 신청 정리(소프트 삭제만 남았을 수 있음)
        try {
            execFileSync('sqlcmd', ['-S', '.\\SQLEXPRESS', '-d', 'PlayGround_Soccer', '-E', '-b', '-Q',
                "DELETE FROM SoccerRecordCorrections WHERE RequestedByUserId='A0000000-0000-0000-0000-000000000D11' AND RequestedByRole='Guardian'"], { stdio: 'ignore' });
            console.log('cleanup: 보호자 검증 신청 물리 삭제');
        } catch {}
        await browser.disconnect();
        edge.kill();
    }
})();
