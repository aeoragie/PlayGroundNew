// InfoPopover(ⓘ 인포 팝오버) 검증 — Design.TooltipHelp §2 첫 소비.
// 부착 4개념 중 3곳 UI 확인: Records 집계 기준(목록 푸터 — 위로 열림) · 인증팀(공개홈 히어로) ·
// Claim(스텝 ② — Pending 초대코드로 진입). 에이전트 열람 승인 ⓘ는 feature flag off라 코드 검토로 갈음.
// 탭 토글 → 제목·본문 → 바깥 탭 닫힘 왕복. 데이터 변경 없음(읽기 전용 — Claim은 신청 전 단계까지만).
const puppeteer = require('puppeteer-core');
const { spawn, execSync } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9575;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-tip-' + Date.now();
const SHOT = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\claude\\d--Study-Workspace-PlayGroundNew\\c91a78a4-3845-419f-bf82-306440282945\\scratchpad\\tip-';

const sql = (q) => execSync(
    `sqlcmd -S .\\SQLEXPRESS -d PlayGround_Soccer -E -b -f 65001 -h -1 -W -Q "SET NOCOUNT ON; ${q.replace(/\s+/g, ' ').replace(/"/g, '\\"')}"`,
    { encoding: 'utf8' }).trim();

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

let pass = 0, fail = 0;
const check = (name, ok, detail) => {
    console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${detail ? ' — ' + detail : ''}`);
    ok ? pass++ : fail++;
};

// 보이는 ⓘ 트리거 클릭
const clickInfo = (page) => page.evaluate(() => {
    const trigger = [...document.querySelectorAll('button[aria-label$=" 설명"]')]
        .find(b => b.getBoundingClientRect().width > 0);
    trigger?.click();
    return !!trigger;
});

(async () => {
    const edge = spawn(EDGE, [
        '--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank',
    ], { stdio: 'ignore' });
    const ws = await waitCdp();
    const browser = await puppeteer.connect({ browserWSEndpoint: ws, defaultViewport: null });

    try {
        //.// 1. Records 목록 푸터 — 위로 열림 + 토글·바깥 닫힘 왕복
        let page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await page.goto(BASE + '/records', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));

        const hasTrigger = await clickInfo(page);
        await new Promise(r => setTimeout(r, 500));
        const opened = await page.evaluate(() => {
            const card = [...document.querySelectorAll('span')]
                .find(e => e.innerText.startsWith('기록은 어떻게 집계되나요?') && e.getBoundingClientRect().width > 200);
            if (!card) { return null; }
            const trigger = [...document.querySelectorAll('button[aria-label$=" 설명"]')]
                .find(b => b.getBoundingClientRect().width > 0);
            return {
                text: card.innerText,
                aboveTrigger: card.getBoundingClientRect().bottom <= trigger.getBoundingClientRect().top + 2,
            };
        });
        check('Records ⓘ: 탭 → 팝오버 (제목+본문)', hasTrigger && opened !== null
            && opened.text.includes('주최측이 입력하고') && opened.text.includes('친선경기는 별도로'));
        check('Records ⓘ: 푸터라 위로 열림', opened?.aboveTrigger === true);
        await page.screenshot({ path: SHOT + 'records.png' });

        // 바깥 탭 닫힘
        await page.mouse.click(400, 300);
        await new Promise(r => setTimeout(r, 400));
        const closed = await page.evaluate(() => ![...document.querySelectorAll('span')]
            .some(e => e.innerText.startsWith('기록은 어떻게 집계되나요?') && e.getBoundingClientRect().width > 200));
        check('Records ⓘ: 바깥 탭 → 닫힘', closed);
        await page.close();

        //.// 2. 공개홈 히어로 — 인증팀 ⓘ
        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await page.goto(BASE + '/team/gwangju-fc-u15', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));
        await clickInfo(page);
        await new Promise(r => setTimeout(r, 500));
        const teamPop = await page.evaluate(() => document.body.innerText);
        check('인증팀 ⓘ: 팝오버 (직접 붙일 수 없음 고지)', teamPop.includes('인증팀이란?')
            && teamPop.includes('팀이 직접 붙일 수 없어요'));
        await page.screenshot({ path: SHOT + 'verified.png' });
        await page.close();

        //.// 3. Claim 스텝 ② — 코드 입력 후 진입 (보호자 계정, 신청 전 단계까지만 — 데이터 무변경)
        const code = sql(`SELECT TOP 1 Code FROM SoccerPlayerInvites WHERE Status='Pending' AND (ExpiresAt IS NULL OR ExpiresAt > GETUTCDATE())`);
        const guardian = await login('verify-player-u15@test.local');
        page = await browser.newPage();
        await page.setViewport({ width: 390, height: 844 });
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), guardian);
        await page.goto(BASE + '/claim', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));
        // 코드 입력 — 투명 오버레이 input (shot-claim.js와 같은 방식)
        await page.locator('input[aria-label="초대코드"]').fill(code);
        await new Promise(r => setTimeout(r, 400));
        await page.evaluate(() => [...document.querySelectorAll('button')]
            .filter(b => b.getBoundingClientRect().width > 0)
            .find(b => b.innerText.includes('프로필 찾기'))?.click());
        await new Promise(r => setTimeout(r, 1800));
        const step2 = await page.evaluate(() => document.body.innerText);
        if (step2.includes('이 프로필이 맞나요?')) {
            await clickInfo(page);
            await new Promise(r => setTimeout(r, 500));
            const claimPop = await page.evaluate(() => document.body.innerText);
            check('Claim ⓘ: 스텝 ② 팝오버 (자녀 연결 설명)', claimPop.includes('자녀 연결이란?')
                && claimPop.includes('보호자 계정과 연결하는 절차'));
            await page.screenshot({ path: SHOT + 'claim.png' });
        } else {
            check('Claim ⓘ: 스텝 ② 진입', false, '코드 조회 실패 — ' + step2.slice(0, 60));
        }
        await page.close();

        console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
        console.log('참고: 에이전트 열람 승인 ⓘ는 feature flag off — 다음 flag-on 검증 때 화면 확인');
        process.exitCode = fail > 0 ? 1 : 0;
    } finally {
        browser.disconnect();
        edge.kill();
    }
})();
