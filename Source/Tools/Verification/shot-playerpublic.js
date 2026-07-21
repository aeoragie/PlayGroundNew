// 공개 선수 프로필 /player/{slug} 검증 (Design.PlayerPublicProfile 디테일 공개 뷰 1차).
// API: 공개 범위 필터(몸무게 비공개→null) · 친선 제외(임시 친선 경기 삽입→집계 불변→원복)
//      · Profile off→NotFound(원복) · 무소속·빈 데이터(신준우) · 없는 slug.
// UI: 히어로(캡슐·칩·학교 미노출) · 시즌 4카드 · 잠금 안내 · 팀 링크 왕복 · 공개홈 로스터 링크 복원
//     · 무동작 CTA 오렌지 1개 · 모바일(하단 고정 바·가로 스크롤 0).
// 임시 데이터는 전부 이 스크립트가 sqlcmd로 넣고 지운다.
const puppeteer = require('puppeteer-core');
const { spawn, execSync } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9553;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-pp-' + Date.now();
const SHOT = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\claude\\d--Study-Workspace-PlayGroundNew\\c91a78a4-3845-419f-bf82-306440282945\\scratchpad\\pp-';

const KIM = 'BD3393AD-5F09-46FC-AD54-D76BC93C8925'; // 김정현 (광주광주FCU15, 공식 4경기)
const FRIENDLY_MATCH = 'FE000000-0000-0000-0000-00000000FE01'; // 임시 친선 경기

const sql = (q) => execSync(
    `sqlcmd -S .\\SQLEXPRESS -d PlayGround_Soccer -E -b -f 65001 -Q "${q.replace(/"/g, '\\"')}"`,
    { encoding: 'utf8' });

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});

const api = async (slug) => {
    const r = await fetch(`${BASE}/api/soccer/player/${encodeURIComponent(slug)}/profile?season=2026`);
    return r.json();
};

let pass = 0, fail = 0;
const check = (name, ok, detail) => {
    console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${detail ? ' — ' + detail : ''}`);
    ok ? pass++ : fail++;
};

async function open(page, path, viewport) {
    await page.setViewport(viewport ?? { width: 1440, height: 900 });
    await page.goto(BASE + path, { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 1200));
}

(async () => {
    //.// API 검증

    const kim = await api('김정현');
    check('API: 김정현 조회 성공', kim?.isSuccess === true);
    const d = kim?.data;
    check('API: 시즌 요약 = 대시보드와 동일 (공식 4경기·265분·2골·1도움)',
        d?.season?.matchCount === 4 && d?.season?.totalMinutes === 265
        && d?.season?.goals === 2 && d?.season?.assists === 1,
        JSON.stringify(d?.season));
    check('API: 몸무게 비공개 → null / 키 기본 공개 → 값',
        d?.profile?.weightKg === null && d?.profile?.heightCm === 171,
        `w=${d?.profile?.weightKg} h=${d?.profile?.heightCm}`);
    check('API: 응답에 학교·연락처 필드 자체가 없음',
        !('schoolName' in (d?.profile ?? {})) && !JSON.stringify(kim).includes('GuardianPhone'));
    check('API: 팀 링크·인증 (gwangju-fc-u15·인증팀)',
        d?.profile?.teamSlug === 'gwangju-fc-u15' && d?.profile?.teamIsVerified === true);
    check('API: 커리어 2건 · 영상 3개 · 대표 영상 존재',
        d?.careers?.length === 2 && d?.videoCount === 3 && !!d?.primaryVideo,
        `careers=${d?.careers?.length} videos=${d?.videoCount}`);

    // 친선 제외 — 친선 경기+출전+골을 넣어도 공개 프로필 집계는 불변이어야 한다
    sql(`INSERT INTO SoccerMatches (MatchId, HomeTeamId, HomeTeamName, AwayTeamName, HomeScore, AwayScore, Status, MatchType, MatchedAt) VALUES ('${FRIENDLY_MATCH}', 'B0000000-0000-0000-0000-000000000004', '광주광주FCU15', '검증상대FC', 5, 0, 'Completed', 'Friendly', '2026-07-01 10:00'); INSERT INTO SoccerMatchAppearances (MatchId, PlayerId, TeamId, MinutesPlayed) VALUES ('${FRIENDLY_MATCH}', '${KIM}', 'B0000000-0000-0000-0000-000000000004', 80); INSERT INTO SoccerMatchEvents (MatchId, TeamId, TeamName, EventType, PlayerId, PlayerName, MinuteOfPlay) VALUES ('${FRIENDLY_MATCH}', 'B0000000-0000-0000-0000-000000000004', '광주광주FCU15', 'Goal', '${KIM}', '김정현', 10)`);
    const withFriendly = await api('김정현');
    check('API: 친선 경기·골을 넣어도 집계 불변 (공식만)',
        withFriendly?.data?.season?.matchCount === 4 && withFriendly?.data?.season?.goals === 2,
        JSON.stringify(withFriendly?.data?.season));
    sql(`DELETE FROM SoccerMatchEvents WHERE MatchId='${FRIENDLY_MATCH}'; DELETE FROM SoccerMatchAppearances WHERE MatchId='${FRIENDLY_MATCH}'; DELETE FROM SoccerMatches WHERE MatchId='${FRIENDLY_MATCH}'`);

    // 무소속·빈 데이터 — 시드에 무소속 선수가 없어 임시 선수를 넣고 지운다
    sql(`INSERT INTO SoccerPlayers (PlayerId, Name, Slug, BirthDate, AgeGroup) VALUES ('FE000000-0000-0000-0000-00000000FE11', '검증빈선수', '검증빈선수', '2013-01-01', 'U12')`);
    const empty = await api('검증빈선수');
    check('API: 무소속·빈 데이터 — 팀 null·시즌 null·영상 null·커리어 0건',
        empty?.data?.profile?.teamName === null && empty?.data?.season === null
        && empty?.data?.primaryVideo === null && empty?.data?.careers?.length === 0,
        JSON.stringify({ t: empty?.data?.profile?.teamName, s: empty?.data?.season, c: empty?.data?.careers?.length }));

    check('API: 없는 slug → 실패 응답', (await api('없는선수xyz'))?.isSuccess === false);

    //.// UI 검증

    const edge = spawn(EDGE, [
        '--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank',
    ], { stdio: 'ignore' });
    const ws = await waitCdp();
    const browser = await puppeteer.connect({ browserWSEndpoint: ws, defaultViewport: null });

    try {
        let page = await browser.newPage();
        await open(page, '/player/김정현');

        const text = await page.evaluate(() => document.body.innerText);
        check('UI: 히어로 — 이름·가족 관리 프로필·인증팀·팀명', ['김정현', '가족 관리 프로필', '✓ 인증팀', '광주광주FCU15'].every(s => text.includes(s)));
        check('UI: 키 칩 있음 · 몸무게 칩 없음 · 학교 문자열 없음',
            text.includes('키 171cm') && !text.includes('몸무게') && !text.includes('학교') === false ? text.includes('키 171cm') && !text.includes('몸무게 ') && !/중학교|고등학교|초등학교/.test(text) : false);
        check('UI: 시즌 요약 카피 (2026 출전 · 4경기 · 265\')', text.includes('2026 출전') && text.includes('4경기 · 265\''));
        check('UI: 잠금 안내 카피 원문', text.includes('보호자 승인 후 열람할 수 있어요') && text.includes('승인 내역과 열람 기록은 모두 보호자에게 공개됩니다'));
        check('UI: 커리어 — 팀 확인됨·본인 입력 캡슐', text.includes('팀 확인됨') && text.includes('본인 입력'));

        // 오렌지 채움 = CTA(열람 요청)와 대표 영상 뱃지만
        const orange = await page.evaluate(() => [...document.querySelectorAll('button, a, span')]
            .filter(e => e.getBoundingClientRect().width > 0 && getComputedStyle(e).backgroundColor === 'rgb(255, 107, 53)')
            .map(e => e.innerText.trim()));
        check('UI: 오렌지 = 열람 요청 CTA + 대표 영상 뱃지만', orange.length === 2
            && orange.includes('상세 정보 열람 요청') && orange.includes('대표 영상'), JSON.stringify(orange));
        await page.screenshot({ path: SHOT + 'detail.png' });

        // 팀 링크 → 공개홈
        await page.evaluate(() => [...document.querySelectorAll('a')]
            .find(a => a.innerText.includes('광주광주FCU15'))?.click());
        await new Promise(r => setTimeout(r, 1500));
        check('UI: 팀 링크 → 팀 공개홈', page.url().includes('/team/gwangju-fc-u15'));

        // 공개홈 로스터 → 공개 프로필 링크 복원
        await open(page, '/team/gwangju-fc-u15/roster');
        const rosterLinks = await page.evaluate(() => [...document.querySelectorAll('a')]
            .filter(a => a.getBoundingClientRect().width > 0 && a.innerText.includes('공개 프로필'))
            .map(a => a.getAttribute('href')));
        check('UI: 공개홈 로스터 "공개 프로필 →" 복원 (# 아님)', rosterLinks.length > 0
            && rosterLinks.every(h => h && h.startsWith('/player/')), JSON.stringify(rosterLinks));
        await page.screenshot({ path: SHOT + 'roster-link.png' });

        // 무소속·빈 데이터 — 시즌·영상·커리어·팀 줄 전부 미렌더 (빈 데이터 노출 금지)
        await open(page, '/player/검증빈선수');
        const emptyText = await page.evaluate(() => document.body.innerText);
        check('UI: 빈 선수 — 시즌·대표 영상·커리어·팀 줄 미노출',
            !emptyText.includes('출전') && !emptyText.includes('대표 영상')
            && !emptyText.includes('커리어') && !emptyText.includes('인증팀')
            && emptyText.includes('검증빈선수'));
        sql(`DELETE FROM SoccerPlayers WHERE PlayerId='FE000000-0000-0000-0000-00000000FE11'`);
        await page.close();

        // 모바일 390 — 하단 고정 CTA·가로 스크롤 0·시즌 2카드
        page = await browser.newPage();
        await open(page, '/player/김정현', { width: 390, height: 844 });
        const mobile = await page.evaluate(() => ({
            hScroll: document.documentElement.scrollWidth > document.documentElement.clientWidth,
            fixedCta: [...document.querySelectorAll('button')].some(b => {
                const r = b.getBoundingClientRect();
                return r.width > 0 && b.innerText.trim() === '상세 정보 열람 요청' && r.bottom > window.innerHeight - 80;
            }),
            statCombined: document.body.innerText.includes('득점 · 도움'),
        }));
        check('UI 모바일: 가로 스크롤 0', !mobile.hScroll);
        check('UI 모바일: 하단 고정 CTA', mobile.fixedCta);
        check('UI 모바일: 시즌 2카드(득점 · 도움 통합)', mobile.statCombined);
        await page.screenshot({ path: SHOT + 'mobile.png' });

        // Profile off → 화면도 NotFound (공개홈 로스터와 같은 기준)
        sql(`INSERT INTO SoccerPlayerFieldVisibilities (PlayerId, FieldName, IsPublic) VALUES ('${KIM}', 'Profile', 0)`);
        const offApi = await api('김정현');
        check('API: Profile off → 실패 응답 (NotFound)', offApi?.isSuccess === false);
        await open(page, '/player/김정현', { width: 1440, height: 900 });
        const offText = await page.evaluate(() => document.body.innerText);
        check('UI: Profile off → "찾을 수 없어요"', offText.includes('선수 프로필을 찾을 수 없어요'));
        await page.screenshot({ path: SHOT + 'notfound.png' });
        sql(`DELETE FROM SoccerPlayerFieldVisibilities WHERE PlayerId='${KIM}' AND FieldName='Profile'`);
        check('원복: Profile off 행 삭제 후 재조회 성공', (await api('김정현'))?.isSuccess === true);
        await page.close();

        console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
        process.exitCode = fail > 0 ? 1 : 0;
    } finally {
        browser.disconnect();
        edge.kill();
    }
})();
