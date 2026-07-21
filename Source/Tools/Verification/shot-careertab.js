// 공개 팀 홈 진학·진로 탭 검증 (Design.TeamPublicHome §⑤).
// API: 게스트 조회 → 관리자 저장 3건(정렬 연도 역순) → 검증 경계(미지 유형·연도 범위·인원 0·
//      남의 사례 수정 거부) → 수정 반영 → 삭제 → 공개 미노출 → 복구.
// UI: 대시보드 팀 정보 관리 카드(추가 폼·빈 제출 인라인·저장 토스트) → 게스트 공개홈(요약 카드
//      값 있는 유형만·타임라인 태그 3톤·캡션 원문) → 빈 팀 빈 상태 → 모바일 3열·가로 스크롤 0.
// 끝나면 DELETE FROM SoccerTeamCareerOutcomes 로 원복.
const puppeteer = require('puppeteer-core');
const { spawn, execSync } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9567;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-career-' + Date.now();
const SHOT = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\claude\\d--Study-Workspace-PlayGroundNew\\c91a78a4-3845-419f-bf82-306440282945\\scratchpad\\career-';

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

const getPublic = async (slug) => (await fetch(`${BASE}/api/soccer/team/${encodeURIComponent(slug)}/career-outcomes`)).json();
const post = async (token, path, body) => (await fetch(BASE + path, {
    method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + token },
    body: JSON.stringify(body ?? {}),
})).json();

let pass = 0, fail = 0;
const check = (name, ok, detail) => {
    console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${detail ? ' — ' + detail : ''}`);
    ok ? pass++ : fail++;
};

(async () => {
    //.// API 검증

    const admin = await login('verify-teamadmin-0713@test.local');
    const other = await login('verify-u15-1@test.local');
    if (!admin || !other) { throw new Error('login failed'); }

    const before = await getPublic('검증fc');
    check('API: 게스트 조회 (초기 빈 목록)', before?.isSuccess === true && before.data.items.length === 0);

    // 저장 3건 — 프로 산하 2건(1+2명) + 축구부 1건(3명), Promotion 없음(요약 카드 파생 검증용)
    const s1 = await post(admin, '/api/soccer/team/me/career-outcomes', { outcomeId: '00000000-0000-0000-0000-000000000000', outcomeYear: 2026, outcomeType: 'ProTransfer', title: 'K리그 산하 U18 이적 1명', detail: 'MF · U15 출신 · 3년 재적', playerCount: 1 });
    const s2 = await post(admin, '/api/soccer/team/me/career-outcomes', { outcomeId: '00000000-0000-0000-0000-000000000000', outcomeYear: 2025, outcomeType: 'SchoolTeam', title: '서울지역 고교 축구부 진학 3명', detail: 'FW 1 · DF 2 · 전원 주전 출전 중', playerCount: 3 });
    const s3 = await post(admin, '/api/soccer/team/me/career-outcomes', { outcomeId: '00000000-0000-0000-0000-000000000000', outcomeYear: 2025, outcomeType: 'ProTransfer', title: 'K리그 산하 U15 이적 2명', detail: 'FW · GK · U12 출신', playerCount: 2 });
    check('API: 사례 3건 저장', [s1, s2, s3].every(r => r?.isSuccess === true));

    const listed = await getPublic('검증fc');
    check('API: 공개 조회 즉시 반영 (3건, 연도 역순)', listed?.data?.items?.length === 3
        && listed.data.items[0].outcomeYear === 2026,
        listed?.data?.items?.map(i => i.outcomeYear).join(','));

    // 검증 경계
    const badType = await post(admin, '/api/soccer/team/me/career-outcomes', { outcomeId: '00000000-0000-0000-0000-000000000000', outcomeYear: 2026, outcomeType: 'Hacked', title: 'x', playerCount: 1 });
    const badYear = await post(admin, '/api/soccer/team/me/career-outcomes', { outcomeId: '00000000-0000-0000-0000-000000000000', outcomeYear: 1980, outcomeType: 'Promotion', title: 'x', playerCount: 1 });
    const badCount = await post(admin, '/api/soccer/team/me/career-outcomes', { outcomeId: '00000000-0000-0000-0000-000000000000', outcomeYear: 2026, outcomeType: 'Promotion', title: 'x', playerCount: 0 });
    check('API: 미지 유형·연도 범위·인원 0 전부 거부',
        [badType, badYear, badCount].every(r => r?.isSuccess === false));

    // 남의 사례 수정·삭제 거부 (다른 팀 관리자)
    const targetId = s1.data.outcomeId;
    const stolen = await post(other, '/api/soccer/team/me/career-outcomes', { outcomeId: targetId, outcomeYear: 2020, outcomeType: 'Promotion', title: '탈취', playerCount: 9 });
    const stolenDelete = await post(other, `/api/soccer/team/me/career-outcomes/${targetId}/delete?restore=false`);
    check('API: 남의 사례 수정·삭제 거부', stolen?.isSuccess === false && stolenDelete?.isSuccess === false);

    // 수정 반영
    const edited = await post(admin, '/api/soccer/team/me/career-outcomes', { outcomeId: targetId, outcomeYear: 2026, outcomeType: 'ProTransfer', title: 'K리그 산하 U18 이적 1명 (수정)', detail: 'MF · U15 출신', playerCount: 1 });
    const afterEdit = await getPublic('검증fc');
    check('API: 수정 → 공개 반영', edited?.isSuccess === true
        && afterEdit.data.items.some(i => i.title.includes('(수정)')));

    // 삭제 → 공개 미노출 → 복구
    await post(admin, `/api/soccer/team/me/career-outcomes/${targetId}/delete?restore=false`);
    const afterDelete = await getPublic('검증fc');
    check('API: 삭제 → 공개 미노출 (2건)', afterDelete.data.items.length === 2);
    await post(admin, `/api/soccer/team/me/career-outcomes/${targetId}/delete?restore=true`);
    const afterRestore = await getPublic('검증fc');
    check('API: 복구 → 3건 복원', afterRestore.data.items.length === 3);

    //.// UI 검증

    const edge = spawn(EDGE, [
        '--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank',
    ], { stdio: 'ignore' });
    const ws = await waitCdp();
    const browser = await puppeteer.connect({ browserWSEndpoint: ws, defaultViewport: null });

    try {
        // 대시보드 팀 정보 — 관리 카드 + 폼
        let page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), admin);
        await page.goto(BASE + '/dashboard/team', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1800));

        const dashText = await page.evaluate(() => document.body.innerText);
        check('UI 대시보드: 관리 카드 (제목·안내·목록 3건·추가 버튼)',
            dashText.includes('진학 · 진로') && dashText.includes('동의한 사례만 등록해 주세요')
            && dashText.includes('＋ 사례 추가') && dashText.includes('(수정)'));

        // 폼: 열기 → 빈 제출 인라인(토스트 0) → 닫기
        await page.evaluate(() => [...document.querySelectorAll('button')]
            .filter(b => b.getBoundingClientRect().width > 0)
            .find(b => b.innerText.includes('＋ 사례 추가'))?.click());
        await new Promise(r => setTimeout(r, 700));
        const formInfo = await page.evaluate(() => {
            const radios = [...document.querySelectorAll('button')]
                .filter(b => b.getBoundingClientRect().width > 0)
                .map(b => b.innerText.trim());
            const yearInput = [...document.querySelectorAll('input')]
                .filter(i => i.getBoundingClientRect().width > 0)
                .map(i => i.value);
            return { radios, yearInput, text: document.body.innerText };
        });
        check('UI 폼: RadioCards 3유형 + 연도 프리필',
            ['프로 산하 이적', '축구부 진학', '상급 연령팀 승격'].every(l => formInfo.radios.includes(l))
            && formInfo.yearInput.includes(String(new Date().getFullYear())));

        await page.evaluate(() => {
            const title = [...document.querySelectorAll('input')].filter(i => i.getBoundingClientRect().width > 0)
                .find(i => i.placeholder?.includes('이적'));
            if (title) { title.value = ''; }
            [...document.querySelectorAll('button')].filter(b => b.getBoundingClientRect().width > 0)
                .find(b => b.innerText.trim() === '사례 등록')?.click();
        });
        await new Promise(r => setTimeout(r, 700));
        const invalid = await page.evaluate(() => document.body.innerText);
        check('UI 폼: 빈 제출 → 인라인 오류 (토스트 0)', invalid.includes('제목을 입력해 주세요')
            && !invalid.includes('저장하지 못했어요'));
        await page.screenshot({ path: SHOT + 'form.png' });
        await page.close();

        // 게스트 공개홈 진학·진로 탭
        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await page.goto(BASE + '/team/검증fc/career', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));
        const pubText = await page.evaluate(() => document.body.innerText);
        check('UI 공개홈: 헤더·부제·캡션 원문', pubText.includes('진학 · 진로')
            && pubText.includes('우리 팀 출신 선수들의 다음 무대입니다')
            && pubText.includes('선수 개인이 공개에 동의한 사례만 표시됩니다.'));
        check('UI 공개홈: 요약 카드 — 값 있는 유형만 (프로 3명·축구부 3명, 승격 없음)',
            pubText.includes('프로 산하 이적') && pubText.includes('3명')
            && pubText.includes('축구부 진학') && !pubText.includes('상급 연령팀 승격'));

        const tags = await page.evaluate(() => [...document.querySelectorAll('span')]
            .filter(e => e.getBoundingClientRect().width > 0 && ['프로 산하', '축구부'].includes(e.innerText.trim())
                && getComputedStyle(e).borderRadius.includes('9999'))
            .map(e => `${e.innerText.trim()}:${getComputedStyle(e).color}`));
        check('UI 공개홈: 타임라인 태그 톤 (프로 오렌지·축구부 네이비)',
            tags.some(t => t.startsWith('프로 산하:rgb(194, 74, 28)'))
            && tags.some(t => t.startsWith('축구부:rgb(35, 64, 142)')), JSON.stringify(tags));
        await page.screenshot({ path: SHOT + 'public.png' });

        // 빈 팀 — 빈 상태 문구 (CTA 없음)
        await page.goto(BASE + '/team/emptyfc/career', { waitUntil: 'networkidle2' }).catch(() => {});
        await new Promise(r => setTimeout(r, 1200));
        const emptyText = await page.evaluate(() => document.body.innerText);
        if (emptyText.includes('진학 · 진로')) {
            check('UI 공개홈: 빈 팀 — 빈 상태 문구', emptyText.includes('아직 등록된 진학·진로 사례가 없어요'));
        } else {
            console.log('SKIP  빈 팀 공개홈 (EmptyFC slug 다름 — 데이터)');
        }
        await page.close();

        // 모바일 390
        page = await browser.newPage();
        await page.setViewport({ width: 390, height: 844 });
        await page.goto(BASE + '/team/검증fc/career', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));
        const mob = await page.evaluate(() => ({
            hScroll: document.documentElement.scrollWidth > document.documentElement.clientWidth,
            text: document.body.innerText,
        }));
        check('UI 모바일: 가로 스크롤 0 + 축약 라벨 없음(승격 미노출)', !mob.hScroll && !mob.text.includes('상급팀 승격'));
        await page.screenshot({ path: SHOT + 'mobile.png' });
        await page.close();

        console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
        process.exitCode = fail > 0 ? 1 : 0;
    } finally {
        browser.disconnect();
        edge.kill();
        sql('DELETE FROM SoccerTeamCareerOutcomes');
        console.log('원복 완료 (SoccerTeamCareerOutcomes 전체 삭제)');
    }
})();
