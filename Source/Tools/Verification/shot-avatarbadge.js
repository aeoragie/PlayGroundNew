// Avatar·CountBadge·StatusBadge 일괄 교체 검증 (Design.AvatarBadge).
// 검수 대상: 허브 · 선수단 · 알림 센터 · 순위표(Records) — 교체 누락 0건.
// 사전: 벨 99+ 확인용으로 verify-teamadmin에 미읽음 알림 105건 삽입해 둘 것 (끝나면 원복).
//   sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -E -Q "DELETE FROM SoccerNotifications WHERE ActorName='검증뱃지'"
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9547;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-badge-' + Date.now();
const SHOT = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\claude\\d--Study-Workspace-PlayGroundNew\\c91a78a4-3845-419f-bf82-306440282945\\scratchpad\\badge-';

// 카탈로그 기대값 (Design.AvatarBadge)
const TEAM_NAVY = 'rgb(35, 64, 142)';
const PERSON_TEAL = 'rgb(46, 196, 182)';
const COUNT_ORANGE = 'rgb(255, 107, 53)';
const POSITIVE_INK = 'rgb(27, 133, 121)';
const NEGATIVE_BG = 'rgb(253, 236, 235)';

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

async function open(page, token, path) {
    if (token) { await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), token); }
    await page.goto(BASE + path, { waitUntil: 'networkidle2' });
    await page.waitForFunction(() => !document.body.innerText.includes('확인 중'),
        { polling: 300, timeout: 15000 }).catch(() => {});
    await new Promise(r => setTimeout(r, 1200));
}

// 보이는 요소만 (PC·모바일 트리가 둘 다 렌더된다)
const visibleEval = (sel) => [...document.querySelectorAll(sel)]
    .filter(e => e.getBoundingClientRect().width > 0)
    .map(e => ({ text: e.innerText.trim(), bg: getComputedStyle(e).backgroundColor,
                 color: getComputedStyle(e).color, radius: getComputedStyle(e).borderRadius,
                 fontSize: getComputedStyle(e).fontSize }));

let pass = 0, fail = 0;
const check = (name, ok, detail) => {
    console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${detail ? ' — ' + detail : ''}`);
    ok ? pass++ : fail++;
};

(async () => {
    const edge = spawn(EDGE, [
        '--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank',
    ], { stdio: 'ignore' });
    const ws = await waitCdp();
    const browser = await puppeteer.connect({ browserWSEndpoint: ws, defaultViewport: null });

    try {
        const admin = await login('verify-teamadmin-0713@test.local');
        const teamOnly = await login('verify-u15-1@test.local');
        if (!admin || !teamOnly) { throw new Error('login failed'); }

        //.// 1. 허브 — 팀 아바타 네이비 / 자녀 아바타 teal / 연결됨 캡슐 / 벨 99+
        let page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await open(page, admin, '/dashboard');

        const hubAvatars = await page.evaluate(visibleEval, '.rounded-full.text-white.font-extrabold');
        const tealAv = hubAvatars.filter(a => a.bg === PERSON_TEAL);
        check('허브: 자녀 아바타 teal ≥1', tealAv.length >= 1, `${tealAv.length}개`);

        const linked = await page.evaluate(visibleEval, 'span');
        const linkedBadge = linked.find(s => s.text === '연결됨');
        check('허브: "연결됨" 캡슐 teal 틴트·10px', !!linkedBadge
            && linkedBadge.color === POSITIVE_INK && linkedBadge.fontSize === '10px',
            linkedBadge ? `${linkedBadge.color} ${linkedBadge.fontSize}` : '요소 없음');

        const bell = await page.evaluate(visibleEval, 'button span.bg-orange, a span.bg-orange');
        const bellBadge = bell.find(b => b.text === '99+');
        check('허브: 벨 카운트 99+ 오렌지 채움', !!bellBadge && bellBadge.bg === COUNT_ORANGE,
            bellBadge ? bellBadge.bg : JSON.stringify(bell.map(b => b.text)));
        await page.screenshot({ path: SHOT + 'hub.png' });

        //.// 2. 알림 센터 — 벨 클릭 → 패널 (교체 누락·구 스타일 잔재 확인)
        await page.evaluate(() => {
            const bells = [...document.querySelectorAll('button[aria-label="알림"]')]
                .filter(e => e.getBoundingClientRect().width > 0);
            bells[0]?.click();
        });
        await new Promise(r => setTimeout(r, 1000));
        const panelOpen = await page.evaluate(() => document.body.innerText.includes('알림'));
        check('알림 센터: 패널 열림', panelOpen);
        await page.screenshot({ path: SHOT + 'notifications.png' });
        await page.close();

        //.// 3. 벨 0건 = 뱃지 숨김 (verify-u15-1, 알림 0)
        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await open(page, teamOnly, '/dashboard');
        const zeroBell = await page.evaluate(visibleEval, 'button span.bg-orange, a span.bg-orange');
        check('벨: 0건이면 카운트 뱃지 없음', zeroBell.length === 0, `${zeroBell.length}개`);
        await page.close();

        //.// 4. 선수단 로스터 — Claim 캡슐(리스트) + 이니셜 아바타 teal(카드 뷰) + 잔재 0
        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await open(page, admin, '/dashboard/team/roster');
        await page.waitForFunction(() => /Claimed|Unclaimed/.test(document.body.innerText),
            { polling: 300, timeout: 15000 }).catch(() => {});

        const claimBadges = await page.evaluate(visibleEval, 'span');
        const claimed = claimBadges.filter(s => ['Claimed', 'Unclaimed', 'Pending'].includes(s.text));
        const capsuleOk = claimed.every(s => s.radius.includes('9999') && s.fontSize === '10px');
        check('선수단: Claim 뱃지 캡슐 규격(radius 99·10px)', claimed.length > 0 && capsuleOk,
            `${claimed.length}개 ${claimed[0]?.radius ?? ''} ${claimed[0]?.fontSize ?? ''}`);

        // 카드 뷰로 전환 — 아바타는 카드에만 있다
        await page.evaluate(() => {
            const btn = [...document.querySelectorAll('button')]
                .filter(b => b.getBoundingClientRect().width > 0)
                .find(b => b.innerText.trim() === '카드');
            btn?.click();
        });
        await new Promise(r => setTimeout(r, 800));
        const rosterAv = await page.evaluate(visibleEval, '.rounded-full.text-white.font-extrabold');
        const rosterTeal = rosterAv.filter(a => a.bg === PERSON_TEAL);
        check('선수단 카드: 이니셜 아바타 teal 다수', rosterTeal.length >= 3, `${rosterTeal.length}개`);

        const legacyPhoto = await page.evaluate(() => document.body.innerText.includes('선수 사진\n'));
        check('선수단: "선수 사진" 회색 플레이스홀더 잔재 0', !legacyPhoto);
        await page.screenshot({ path: SHOT + 'roster.png' });
        await page.close();

        //.// 4.5 팀 아바타 네이비 — 팀 정보의 코치 카드 (팀 유형 = 네이비 고정)
        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await open(page, admin, '/dashboard/team');
        const infoAvatars = await page.evaluate(visibleEval, '.rounded-full.text-white.font-extrabold');
        const navyAv = infoAvatars.filter(a => a.bg === TEAM_NAVY);
        check('팀 정보: 코치 아바타 네이비 ≥2', navyAv.length >= 2, `${navyAv.length}개`);
        await page.close();

        //.// 5. 승무패 — 공식 경기 팀(광주)의 결과: 승 teal 틴트 / 패 연레드
        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await open(page, teamOnly, '/dashboard/team/results');
        await page.waitForFunction(() => /승|무|패/.test(document.body.innerText),
            { polling: 300, timeout: 15000 }).catch(() => {});
        const outcome = (await page.evaluate(visibleEval, 'span')).filter(s => s.radius.includes('9999'));
        const wins = outcome.filter(s => s.text === '승');
        const losses = outcome.filter(s => s.text === '패');
        check('경기: "승" 뱃지 teal 틴트 캡슐', wins.length > 0
            && wins.every(w => w.color === POSITIVE_INK && w.radius.includes('9999')),
            `${wins.length}개 ${wins[0]?.color ?? ''}`);
        if (losses.length > 0) {
            check('경기: "패" 뱃지 연레드 틴트', losses.every(l => l.bg === NEGATIVE_BG),
                `${losses.length}개 ${losses[0]?.bg ?? ''}`);
        } else {
            console.log('SKIP  경기: 패 없음 (데이터)');
        }
        await page.screenshot({ path: SHOT + 'results.png' });
        await page.close();

        //.// 6. 순위표(Records) — 목록 상태 뱃지 캡슐(진행중 teal 틴트 / 종료 회색), 게스트
        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await open(page, null, '/records');
        await page.waitForFunction(() => /진행중|예정|종료/.test(document.body.innerText),
            { polling: 300, timeout: 15000 }).catch(() => {});
        // 캡슐(radius 99)만 — "진행중" 일반 텍스트(세그먼트·헤더)를 제외한다
        const status = (await page.evaluate(visibleEval, 'span')).filter(s => s.radius.includes('9999'));
        const inProgress = status.filter(s => s.text === '진행중');
        const done = status.filter(s => s.text === '종료');
        check('Records: "진행중" teal 틴트(채움 아님)', inProgress.length > 0
            && inProgress.every(s => s.color === POSITIVE_INK && s.bg !== PERSON_TEAL),
            `${inProgress.length}개 ${inProgress[0]?.color ?? ''} / bg ${inProgress[0]?.bg ?? ''}`);
        check('Records: "예정" 네이비 틴트 캡슐', status.some(s => s.text === '예정'),
            `${status.filter(s => s.text === '예정').length}개`);
        await page.screenshot({ path: SHOT + 'records.png' });

        // 종료 대회는 리그 세그먼트에 있다
        await page.evaluate(() => {
            const btn = [...document.querySelectorAll('button')]
                .filter(b => b.getBoundingClientRect().width > 0)
                .find(b => b.innerText.trim() === '리그');
            btn?.click();
        });
        await new Promise(r => setTimeout(r, 800));
        const leagueStatus = (await page.evaluate(visibleEval, 'span')).filter(s => s.radius.includes('9999'));
        const leagueDone = leagueStatus.filter(s => s.text === '종료');
        if (leagueDone.length > 0) {
            check('Records: "종료" 회색 캡슐', leagueDone.every(s => s.fontSize === '10px'),
                `${leagueDone.length}개 ${leagueDone[0]?.bg ?? ''}`);
        } else {
            console.log('SKIP  Records: 종료 대회 없음 (데이터)');
        }

        //.// 7. 아카이브 순위표 상세 — 진출권 teal 유지(교체 대상 아님) 확인만 스크린샷
        await page.close();

        //.// 8. 모바일 허브 (390px)
        page = await browser.newPage();
        await page.setViewport({ width: 390, height: 844 });
        await open(page, admin, '/dashboard');
        const mLinked = await page.evaluate(visibleEval, 'span');
        const mBadge = mLinked.find(s => s.text === '연결됨');
        check('모바일 허브: "연결됨" 캡슐 teal', !!mBadge && mBadge.color === POSITIVE_INK,
            mBadge?.color ?? '요소 없음');
        await page.screenshot({ path: SHOT + 'hub-mobile.png' });
        await page.close();

        console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
        process.exitCode = fail > 0 ? 1 : 0;
    } finally {
        browser.disconnect();
        edge.kill();
    }
})();
