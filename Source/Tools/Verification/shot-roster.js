// 로스터 쓰기 UI 검증 (Design.TeamDashboard §2).
// ＋선수 추가 → 다이얼로그 → 저장 → 토스트 + 목록 반영 / ⋯ → 선수 내보내기 → 확인 모달 → 토스트(실행취소).
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9551;
const BASE = 'http://localhost:5000';
const URL = BASE + '/dashboard/team/roster';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-roster-' + Date.now();

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
const NAME = 'UI검증선수';

(async () => {
    const edge = spawn(EDGE, ['--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank'], { stdio: 'ignore' });
    const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1280, height: 1000 });
        const token = await login('verify-teamadmin-0713@test.local');
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), token);
        await page.goto(URL, { waitUntil: 'networkidle2' });
        await ready(page, '선수단');

        const countText = () => page.evaluate(() => document.body.innerText.match(/선수단\s*(\d+)명/)?.[1]);
        const before = await countText();

        //.// 1) ＋ 선수 추가 → 다이얼로그
        await clickText(page, '＋ 선수 추가');
        await sleep(400);
        check('추가 다이얼로그 열림', await page.evaluate(() => document.body.innerText.includes('선수 이름') && document.body.innerText.includes('초대코드')));

        //.// 빈 제출 → 인라인 오류 (토스트 아님)
        await clickText(page, '추가');
        await sleep(400);
        check('빈 제출 → 인라인 오류', await page.evaluate(() => document.body.innerText.includes('선수 이름을 입력해 주세요')));

        //.// 이름 입력 후 저장
        await page.evaluate((label) => {
            const lbl = [...document.querySelectorAll('label')].find(l => l.innerText.includes('선수 이름'));
            const input = lbl ? document.getElementById(lbl.getAttribute('for')) : null;
            if (input) { input.value = ''; }
        }, NAME);
        // Blazor 바인딩은 fill로 이벤트를 발화해야 잡힌다
        const nameId = await page.evaluate(() => {
            const lbl = [...document.querySelectorAll('label')].find(l => l.innerText.includes('선수 이름'));
            return lbl?.getAttribute('for');
        });
        await page.focus(`#${nameId}`);
        await page.type(`#${nameId}`, NAME, { delay: 10 });
        await sleep(200);
        await clickText(page, '추가');
        await sleep(900);

        check('저장 성공 토스트', await page.evaluate(n => document.body.innerText.includes(n + ' 선수를 추가했어요'), NAME));
        const afterAdd = await countText();
        check('선수단 수 +1', Number(afterAdd) === Number(before) + 1);
        check('목록에 새 선수 표시', await page.evaluate(n => document.body.innerText.includes(n), NAME));
        await page.screenshot({ path: 'roster-added.png' });

        //.// 2) ⋯ → 선수 내보내기 → 확인 모달
        // ⋯ 트리거의 aria-label은 "{이름} 추가 작업" — 새 선수 행 것을 정확히 집는다
        const openedMenu = await page.evaluate((n) => {
            const dots = [...document.querySelectorAll(`button[aria-label="${n} 추가 작업"]`)]
                .find(b => b.getBoundingClientRect().width > 0);
            if (dots) { dots.click(); return true; } return false;
        }, NAME);
        await sleep(400);
        check('⋯ 메뉴 열림 (선수 내보내기)', openedMenu && await page.evaluate(() => document.body.innerText.includes('선수 내보내기')));

        await clickText(page, '선수 내보내기');
        await sleep(400);
        check('확인 모달 (내보내기)', await page.evaluate(n => document.body.innerText.includes(n + ' 선수를 선수단에서 내보낼까요'), NAME));

        await clickText(page, '내보내기');
        await sleep(900);
        check('내보내기 토스트 + 실행취소', await page.evaluate(n => document.body.innerText.includes(n + ' 선수를 내보냈어요') && document.body.innerText.includes('실행취소'), NAME));
        const afterRemove = await countText();
        check('선수단 수 원복(-1)', Number(afterRemove) === Number(before));

        //.// 3) 실행취소 → 다시 나타남
        await clickText(page, '실행취소');
        await sleep(900);
        check('실행취소 → 다시 나타남', await page.evaluate(n => document.body.innerText.includes(n), NAME));
        await page.screenshot({ path: 'roster-pc.png' });

        //.// 4) 모바일 — ＋선수 추가 버튼 + ⋯ 존재, 가로 스크롤 없음
        await page.setViewport({ width: 390, height: 844 });
        await page.reload({ waitUntil: 'networkidle2' });
        await ready(page, '선수단');
        const mobile = await page.evaluate(() => ({
            addBtn: [...document.querySelectorAll('button')].some(b => b.innerText.includes('선수 추가') && b.getBoundingClientRect().width > 0),
            overflow: !!document.querySelector('button[aria-label$="추가 작업"]'),
            hScroll: document.documentElement.scrollWidth > window.innerWidth,
        }));
        check('모바일 ＋선수 추가 + ⋯ 존재', mobile.addBtn && mobile.overflow);
        check('모바일 가로 스크롤 없음', !mobile.hScroll);
        await page.screenshot({ path: 'roster-mobile.png', fullPage: true });

        console.log(`\n${pass} PASS / ${fail} FAIL`);
    } finally {
        await browser.disconnect();
        edge.kill();
    }
})();
