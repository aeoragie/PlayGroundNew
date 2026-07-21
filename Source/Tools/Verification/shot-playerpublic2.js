// 공개 선수 프로필 2차 — 디테일 권한 뷰 검증 (Design.PlayerPublicProfile).
// 흐름: 에이전트 임시 계정 생성(find-or-create) → 시드 요청 → [승인 전 = 공개 뷰] → SQL 승인 →
//       [권한 뷰: Grant·학교·경기별 기록(친선 포함)·요약은 공식만·열람 로그 적재] →
//       [보호자 토큰 = 공개 뷰(에이전트 아님)] → SQL 만료 → [공개 뷰 폴백] → 전부 원복.
// 승인 플로우 자체(api-agent.js)는 기존 검증 — 여기서는 SQL로 상태만 만든다.
const puppeteer = require('puppeteer-core');
const { spawn, execSync } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9557;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-pp2-' + Date.now();
const SHOT = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\claude\\d--Study-Workspace-PlayGroundNew\\c91a78a4-3845-419f-bf82-306440282945\\scratchpad\\pp2-';

const KIM = 'BD3393AD-5F09-46FC-AD54-D76BC93C8925'; // 김정현
const AGENT_ID = 'A9000000-0000-0000-0000-0000000000F2'; // 임시 에이전트 프로필
const REQUEST_ID = 'A9000000-0000-0000-0000-0000000000F3'; // 임시 열람 요청
const FRIENDLY_MATCH = 'FE000000-0000-0000-0000-00000000FE02'; // 임시 친선 경기
const AGENT_EMAIL = 'verify-agent-pp2@test.local';

const sql = (q, db = 'PlayGround_Soccer') => execSync(
    `sqlcmd -S .\\SQLEXPRESS -d ${db} -E -b -f 65001 -h -1 -W -Q "SET NOCOUNT ON; ${q.replace(/\s+/g, ' ').replace(/"/g, '\\"')}"`,
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

const api = async (slug, token) => {
    const headers = token ? { Authorization: 'Bearer ' + token } : {};
    const r = await fetch(`${BASE}/api/soccer/player/${encodeURIComponent(slug)}/profile?season=2026`, { headers });
    return r.json();
};

let pass = 0, fail = 0;
const check = (name, ok, detail) => {
    console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${detail ? ' — ' + detail : ''}`);
    ok ? pass++ : fail++;
};

(async () => {
    //.// 준비 — 에이전트 임시 계정(find-or-create) + 프로필·요청·친선 경기

    const agentToken = await login(AGENT_EMAIL);
    if (!agentToken) { throw new Error('agent login failed'); }
    const agentUserId = sql(`SELECT CONVERT(VARCHAR(36), UserId) FROM Users WHERE Email='${AGENT_EMAIL}'`, 'PlayGround_Account');

    sql(`DELETE FROM SoccerAgentViewLogs WHERE RequestId='${REQUEST_ID}';
         DELETE FROM SoccerAgentViewRequests WHERE RequestId='${REQUEST_ID}';
         DELETE FROM SoccerAgentProfiles WHERE AgentId='${AGENT_ID}';
         INSERT INTO SoccerAgentProfiles (AgentId, UserId, Name, AgencyName, RegisteredYear, IsVerified) VALUES ('${AGENT_ID}', '${agentUserId}', '검증에이전트', '검증 에이전시', 2024, 1);
         INSERT INTO SoccerAgentViewRequests (RequestId, AgentId, PlayerId, GuardianUserId, Message) VALUES ('${REQUEST_ID}', '${AGENT_ID}', '${KIM}', (SELECT TOP 1 UserId FROM SoccerPlayers WHERE PlayerId='${KIM}'), '검증용 열람 요청');
         INSERT INTO SoccerMatches (MatchId, HomeTeamId, HomeTeamName, AwayTeamName, HomeScore, AwayScore, Status, MatchType, MatchedAt) VALUES ('${FRIENDLY_MATCH}', 'B0000000-0000-0000-0000-000000000004', '광주광주FCU15', '검증상대FC', 5, 0, 'Completed', 'Friendly', '2026-07-01 10:00');
         INSERT INTO SoccerMatchAppearances (MatchId, PlayerId, TeamId, MinutesPlayed) VALUES ('${FRIENDLY_MATCH}', '${KIM}', 'B0000000-0000-0000-0000-000000000004', 80);
         INSERT INTO SoccerMatchEvents (MatchId, TeamId, TeamName, EventType, PlayerId, PlayerName, MinuteOfPlay) VALUES ('${FRIENDLY_MATCH}', 'B0000000-0000-0000-0000-000000000004', '광주광주FCU15', 'Goal', '${KIM}', '김정현', 10)`);

    //.// API 검증

    // 승인 전(Pending) — 에이전트 토큰이어도 공개 뷰
    const pending = await api('김정현', agentToken);
    check('API: 승인 전(Pending) — Grant null·학교 null·경기 목록 null',
        pending?.data?.grant === null && pending?.data?.profile?.schoolName === null
        && pending?.data?.matches === null);

    // SQL 승인 (+30일)
    sql(`UPDATE SoccerAgentViewRequests SET Status='Approved', ReviewedAt=GETUTCDATE(), ExpiresAt=DATEADD(DAY, 30, GETUTCDATE()) WHERE RequestId='${REQUEST_ID}'`);

    const granted = await api('김정현', agentToken);
    check('API: 승인 후 — Grant 있음 (승인일·만료일)',
        !!granted?.data?.grant?.approvedAt && !!granted?.data?.grant?.expiresAt);
    check('API: 권한 뷰 — 학교 실림 (값 존재)', typeof granted?.data?.profile?.schoolName === 'string'
        && granted.data.profile.schoolName.length > 0, granted?.data?.profile?.schoolName);
    check('API: 경기별 기록 — 친선 포함 5경기 (공식 4 + 친선 1)',
        granted?.data?.matches?.length === 5
        && granted?.data?.matches?.some(m => m.matchType === 'Friendly'),
        `${granted?.data?.matches?.length}경기`);
    check('API: 시즌 요약은 여전히 공식만 (4경기·2골)',
        granted?.data?.season?.matchCount === 4 && granted?.data?.season?.goals === 2,
        JSON.stringify(granted?.data?.season));

    // 열람 로그 — 권한 조회마다 ProfileView 적재 (승인 전 2회 조회는 미적재)
    const logCount1 = parseInt(sql(`SELECT COUNT(*) FROM SoccerAgentViewLogs WHERE RequestId='${REQUEST_ID}' AND EventType='ProfileView'`));
    await api('김정현', agentToken);
    const logCount2 = parseInt(sql(`SELECT COUNT(*) FROM SoccerAgentViewLogs WHERE RequestId='${REQUEST_ID}' AND EventType='ProfileView'`));
    check('API: 열람 로그 ProfileView 적재 (권한 조회마다 +1)',
        logCount1 >= 1 && logCount2 === logCount1 + 1, `${logCount1} → ${logCount2}`);

    // 다른 로그인 계정(보호자 본인)은 에이전트가 아니다 — 공개 뷰
    const guardianToken = await login('verify-player-u15@test.local');
    const asGuardian = await api('김정현', guardianToken);
    check('API: 보호자 토큰 — 에이전트 아님 → 공개 뷰(Grant null)', asGuardian?.data?.grant === null);

    // 게스트 — 공개 뷰 그대로 (친선 미포함 확인)
    const asGuest = await api('김정현', null);
    check('API: 게스트 — Grant null·경기 목록 null·학교 null',
        asGuest?.data?.grant === null && asGuest?.data?.matches === null
        && asGuest?.data?.profile?.schoolName === null);

    //.// UI 검증

    const edge = spawn(EDGE, [
        '--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank',
    ], { stdio: 'ignore' });
    const ws = await waitCdp();
    const browser = await puppeteer.connect({ browserWSEndpoint: ws, defaultViewport: null });

    try {
        let page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), agentToken);
        await page.goto(BASE + '/player/김정현', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));

        const text = await page.evaluate(() => document.body.innerText);
        check('UI: 상단 teal 배너 — 승인·만료·열람 기록 고지',
            text.includes('보호자 승인으로 상세 정보 열람 중') && text.includes('일 후 만료')
            && text.includes('열람 기록이 보호자에게 표시됩니다'));
        check('UI: 학교 칩 (권한 항목)', text.includes(granted.data.profile.schoolName));
        check('UI: 경기별 상세 기록 카드 + "승인 열람" + 친선 pill',
            text.includes('경기별 상세 기록') && text.includes('승인 열람') && text.includes('친선'));
        check('UI: CTA = "보호자에게 연락하기" + 캡션, 열람 요청 없음',
            text.includes('보호자에게 연락하기') && text.includes('연락은 플랫폼 내 메시지로 시작됩니다')
            && !text.includes('상세 정보 열람 요청'));
        check('UI: 잠금 안내 미노출', !text.includes('보호자 승인 후 열람할 수 있어요'));
        await page.screenshot({ path: SHOT + 'granted.png' });

        // 경기 행 구성 — vs 제목·스탯·출전분
        check('UI: 경기 행 — vs 제목·출전분', /vs .+ \(\d+:\d+ (승|무|패)\)/.test(text) && text.includes("출전 80'"));
        await page.close();

        // 모바일 권한 뷰
        page = await browser.newPage();
        await page.setViewport({ width: 390, height: 844 });
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), agentToken);
        await page.goto(BASE + '/player/김정현', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));
        const mob = await page.evaluate(() => ({
            text: document.body.innerText,
            hScroll: document.documentElement.scrollWidth > document.documentElement.clientWidth,
        }));
        check('UI 모바일: 배너 축약 카피 + 가로 스크롤 0',
            mob.text.includes('보호자 승인 열람 중') && !mob.hScroll);
        check('UI 모바일: 하단 CTA = 보호자에게 연락하기', mob.text.includes('보호자에게 연락하기'));
        await page.screenshot({ path: SHOT + 'granted-mobile.png' });
        await page.close();

        // SQL 만료 → 공개 뷰 폴백
        sql(`UPDATE SoccerAgentViewRequests SET ExpiresAt=DATEADD(DAY, -1, GETUTCDATE()) WHERE RequestId='${REQUEST_ID}'`);
        const expired = await api('김정현', agentToken);
        check('API: 만료 후 — 공개 뷰 폴백 (Grant null·학교 null·친선 미포함)',
            expired?.data?.grant === null && expired?.data?.profile?.schoolName === null
            && expired?.data?.matches === null && expired?.data?.season?.matchCount === 4);

        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), agentToken);
        await page.goto(BASE + '/player/김정현', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));
        const expiredText = await page.evaluate(() => document.body.innerText);
        check('UI: 만료 후 — 배너 없음·잠금 안내 복귀',
            !expiredText.includes('상세 정보 열람 중') && expiredText.includes('보호자 승인 후 열람할 수 있어요'));
        await page.screenshot({ path: SHOT + 'expired.png' });
        await page.close();

        console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
        process.exitCode = fail > 0 ? 1 : 0;
    } finally {
        browser.disconnect();
        edge.kill();

        //.// 원복 — 임시 데이터 전부 삭제 (에이전트 계정은 Account에서 물리 삭제)
        sql(`DELETE FROM SoccerAgentViewLogs WHERE RequestId='${REQUEST_ID}';
             DELETE FROM SoccerAgentViewRequests WHERE RequestId='${REQUEST_ID}';
             DELETE FROM SoccerAgentProfiles WHERE AgentId='${AGENT_ID}';
             DELETE FROM SoccerMatchEvents WHERE MatchId='${FRIENDLY_MATCH}';
             DELETE FROM SoccerMatchAppearances WHERE MatchId='${FRIENDLY_MATCH}';
             DELETE FROM SoccerMatches WHERE MatchId='${FRIENDLY_MATCH}'`);
        sql(`DELETE FROM Users WHERE Email='${AGENT_EMAIL}'`, 'PlayGround_Account');
        console.log('원복 완료 (에이전트 요청·프로필·친선 경기·임시 계정)');
    }
})();
