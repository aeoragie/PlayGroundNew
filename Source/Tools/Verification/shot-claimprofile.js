// 코드 없이(공개 선수 프로필 경유) 연결 UI (Design.ClaimFlow "코드가 없어요").
// 공개 프로필 "내 아이 프로필 관리하기" → /claim?slug= 스텝 ② → 연결 요청 보내기 → 대기.
// + /claim ① "코드가 없어요"에 경기기록 경유 링크 존재.
const puppeteer = require('puppeteer-core');
const { spawn, execFileSync } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9591;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-claimprof-' + Date.now();
const MANAGER_ID = '55E9A639-83E2-45F8-B9E4-C717C276678F';
const GUARDIAN = 'verify-player-u12@test.local';
const GUARDIAN_ID = 'A0000000-0000-0000-0000-000000000D01';

const sql = q => execFileSync('sqlcmd',
    ['-S', '.\\SQLEXPRESS', '-d', 'PlayGround_Soccer', '-E', '-h', '-1', '-W', '-s', '|', '-f', '65001', '-Q', 'SET NOCOUNT ON; ' + q],
    { encoding: 'utf8' }).trim();
const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => { let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl)); })
        .on('error', () => { if (++t > 60) rej(new Error('to')); else setTimeout(k, 250); });
    k();
});
const sleep = ms => new Promise(r => setTimeout(r, ms));
const ready = (p, t) => p.waitForFunction(x => document.body.innerText.includes(x), { timeout: 40000, polling: 300 }, t);
const clickText = (p, t) => p.evaluate(x => { const e = [...document.querySelectorAll('button,a')].find(b => b.innerText.trim() === x && b.getBoundingClientRect().width > 0); if (e) { e.click(); return true; } return false; }, t);
async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password: 'password123!' }) });
    return (await r.json())?.data?.accessToken ?? null;
}

let pass = 0, fail = 0;
const check = (n, ok) => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${n}`); ok ? pass++ : fail++; };

(async () => {
    const edge = spawn(EDGE, ['--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`, '--no-first-run', '--disable-gpu', 'about:blank'], { stdio: 'ignore' });
    const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });
    const slugHex = sql(`SELECT TOP 1 CONVERT(varchar(max), CONVERT(varbinary(max), p.Slug), 2) FROM SoccerPlayers p JOIN SoccerTeamPlayers tp ON tp.PlayerId=p.PlayerId AND tp.Status='Active' AND tp.DeletedAt IS NULL WHERE p.UserId IS NULL AND p.DeletedAt IS NULL AND tp.TeamId=(SELECT TOP 1 TeamId FROM SoccerTeams WHERE ManagerUserId='${MANAGER_ID}')`);
    const slug = Buffer.from(slugHex, 'hex').toString('utf8');
    let requestId = null;

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1280, height: 1000 });
        const token = await login(GUARDIAN);
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), token);

        //.// A) /claim ① — 코드가 없어요에 경기기록 경유 링크
        await page.goto(BASE + '/claim', { waitUntil: 'networkidle2' });
        await sleep(1200);
        check('① 코드가 없어요 섹션', await page.evaluate(() => document.body.innerText.includes('코드가 없어요')));
        check('① 경기기록 경유 링크(프로필에서 바로 연결 요청)', await page.evaluate(() =>
            [...document.querySelectorAll('a')].some(a => a.innerText.includes('프로필에서 바로 연결 요청') && a.getAttribute('href') === '/records')));

        //.// B) 공개 프로필 "내 아이 프로필 관리하기" CTA
        await page.goto(BASE + '/player/' + encodeURIComponent(slug), { waitUntil: 'networkidle2' });
        await ready(page, '선수 프로필').catch(() => {});
        await sleep(800);
        const claimCta = await page.evaluate(() => [...document.querySelectorAll('a')].find(a => a.innerText.trim() === '내 아이 프로필 관리하기')?.getAttribute('href'));
        check('공개 프로필 "내 아이 프로필 관리하기" CTA', !!claimCta && claimCta.includes('/claim?slug='));

        //.// C) CTA 따라가 슬러그 진입 → 스텝 ② → 연결 요청 보내기
        await page.goto(BASE + claimCta, { waitUntil: 'networkidle2' });
        await sleep(1500);
        check('슬러그 진입 → 스텝 ②(프로필 확인)', await page.evaluate(() => document.body.innerText.includes('이 프로필이 맞나요')));
        check('미연결 뱃지 표시', await page.evaluate(() => document.body.innerText.includes('미연결')));

        await clickText(page, '연결 요청 보내기');
        await sleep(1500);
        check('스텝 ③ 승인 대기', await page.evaluate(() => document.body.innerText.includes('승인을 기다리고 있어요')));
        await page.screenshot({ path: 'claimprofile-pending.png', fullPage: true });

        requestId = sql(`SELECT TOP 1 CONVERT(varchar(50),RequestId) FROM SoccerPlayerClaimRequests WHERE RequesterUserId='${GUARDIAN_ID}' AND Status='Pending' AND InviteId IS NULL ORDER BY CreatedAt DESC`);
        check('DB에 코드 없는 Pending 요청 생성', /^[0-9A-Fa-f-]{36}$/.test(requestId));

        console.log(`\n${pass} PASS / ${fail} FAIL`);
    } finally {
        // 정리 — 요청·알림 삭제 (승인 안 했으므로 선수 연결 없음)
        try {
            sql(`DELETE FROM SoccerNotifications WHERE RefId IN (SELECT RequestId FROM SoccerPlayerClaimRequests WHERE RequesterUserId='${GUARDIAN_ID}' AND InviteId IS NULL)`);
            sql(`DELETE FROM SoccerPlayerClaimRequests WHERE RequesterUserId='${GUARDIAN_ID}' AND InviteId IS NULL`);
            console.log('cleanup: 코드 없는 검증 요청·알림 삭제');
        } catch {}
        await browser.disconnect();
        edge.kill();
    }
})();
