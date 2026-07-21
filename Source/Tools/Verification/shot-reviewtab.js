// 공개 팀 홈 리뷰 탭 검증 (Design.TeamPublicHome §⑥).
// API: 게스트(자격 없음) → 보호자 자격 판정 → 작성(마스킹·메타·MyReviewId) → 중복 신규 거부 →
//      무자격 계정 거부 → 별점·본문 경계 → 수정 → 남의 리뷰 삭제 거부 → 본인 삭제 → 복구.
// UI: 게스트(쓰기 버튼 없음·캡션 원문) → 보호자(쓰기 버튼 → 폼 별점·본문 → 저장 토스트 → 카드
//      마스킹·재원 확인됨·평균 헤더·내 카드 ⋯) → 모바일. 끝나면 DELETE FROM SoccerTeamReviews.
const puppeteer = require('puppeteer-core');
const { spawn, execSync } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9571;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-review-' + Date.now();
const SHOT = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\claude\\d--Study-Workspace-PlayGroundNew\\c91a78a4-3845-419f-bf82-306440282945\\scratchpad\\review-';
const SLUG = 'gwangju-fc-u15'; // 김정현(재원 자녀) 보호자 = verify-player-u15

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

const getReviews = async (token) => (await fetch(`${BASE}/api/soccer/team/${SLUG}/reviews`,
    { headers: token ? { Authorization: 'Bearer ' + token } : {} })).json();
const saveReview = async (token, body) => (await fetch(`${BASE}/api/soccer/team/${SLUG}/reviews`, {
    method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + token },
    body: JSON.stringify(body),
})).json();
const deleteReview = async (token, id, restore = false) => (await fetch(
    `${BASE}/api/soccer/team/reviews/${id}/delete?restore=${restore}`, {
    method: 'POST', headers: { Authorization: 'Bearer ' + token },
})).json();

let pass = 0, fail = 0;
const check = (name, ok, detail) => {
    console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${detail ? ' — ' + detail : ''}`);
    ok ? pass++ : fail++;
};

const EMPTY_ID = '00000000-0000-0000-0000-000000000000';

(async () => {
    //.// API 검증

    const guardian = await login('verify-player-u15@test.local');
    const outsider = await login('verify-teamadmin-0713@test.local'); // 광주 재원 자녀 없음
    if (!guardian || !outsider) { throw new Error('login failed'); }

    const guest = await getReviews(null);
    check('API: 게스트 — 빈 목록·자격 없음', guest?.isSuccess === true
        && guest.data.items.length === 0 && guest.data.isResidentGuardian === false);

    const asGuardian = await getReviews(guardian);
    check('API: 보호자 — 자격 있음·내 리뷰 없음',
        asGuardian?.data?.isResidentGuardian === true && asGuardian.data.myReviewId === null);

    // 작성
    const created = await saveReview(guardian, { reviewId: EMPTY_ID, teamSlug: SLUG, rating: 5, body: '출전 기록을 정말 공개합니다. 분기 성장 리뷰가 알찹니다.' });
    const afterCreate = await getReviews(guardian);
    check('API: 작성 → 반영·MyReviewId 세팅', created?.isSuccess === true
        && afterCreate.data.items.length === 1 && afterCreate.data.myReviewId !== null);
    check('API: 작성자 마스킹·메타 파생', /○○ 학부모$/.test(afterCreate.data.items[0].authorDisplayName)
        && /U15 · 재원 \d+년차/.test(afterCreate.data.items[0].meta ?? ''),
        `${afterCreate.data.items[0].authorDisplayName} / ${afterCreate.data.items[0].meta}`);

    // 중복 신규 거부 (계정당 1건)
    const dup = await saveReview(guardian, { reviewId: EMPTY_ID, teamSlug: SLUG, rating: 4, body: '중복' });
    check('API: 중복 신규 거부', dup?.isSuccess === false);

    // 무자격 계정 거부
    const noAuth = await saveReview(outsider, { reviewId: EMPTY_ID, teamSlug: SLUG, rating: 5, body: '무자격' });
    check('API: 재원 자녀 없는 계정 거부', noAuth?.isSuccess === false);

    // 별점·본문 경계
    const bad0 = await saveReview(guardian, { reviewId: afterCreate.data.myReviewId, teamSlug: SLUG, rating: 0, body: 'x' });
    const bad6 = await saveReview(guardian, { reviewId: afterCreate.data.myReviewId, teamSlug: SLUG, rating: 6, body: 'x' });
    const badBody = await saveReview(guardian, { reviewId: afterCreate.data.myReviewId, teamSlug: SLUG, rating: 5, body: '' });
    check('API: 별점 0·6·빈 본문 거부', [bad0, bad6, badBody].every(r => r?.isSuccess === false));

    // 수정
    const myId = afterCreate.data.myReviewId;
    const edited = await saveReview(guardian, { reviewId: myId, teamSlug: SLUG, rating: 4, body: '진학 준비 지원이 좋았어요. 다만 원정 이동 부담이 좀 있어요. (수정)' });
    const afterEdit = await getReviews(null);
    check('API: 수정 → 반영 (4★)', edited?.isSuccess === true
        && afterEdit.data.items[0].rating === 4 && afterEdit.data.items[0].body.includes('(수정)'));

    // 남의 리뷰 삭제 거부 → 본인 삭제 → 복구
    const stolenDelete = await deleteReview(outsider, myId);
    check('API: 남의 리뷰 삭제 거부 (팀 관리자 포함)', stolenDelete?.isSuccess === false);
    await deleteReview(guardian, myId);
    const afterDelete = await getReviews(null);
    check('API: 본인 삭제 → 공개 미노출', afterDelete.data.items.length === 0);
    await deleteReview(guardian, myId, true);
    const afterRestore = await getReviews(null);
    check('API: 복구 → 재노출', afterRestore.data.items.length === 1);

    //.// UI 검증

    const edge = spawn(EDGE, [
        '--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank',
    ], { stdio: 'ignore' });
    const ws = await waitCdp();
    const browser = await puppeteer.connect({ browserWSEndpoint: ws, defaultViewport: null });

    try {
        // 게스트 — 쓰기 버튼 없음·카드·캡션 원문·평균 헤더
        let page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await page.goto(BASE + `/team/${SLUG}/review`, { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));
        const guestText = await page.evaluate(() => document.body.innerText);
        check('UI 게스트: 헤더·부제·캡션 원문·평균(4.0)·리뷰 1개',
            guestText.includes('학부모 리뷰') && guestText.includes('재원 중이거나 재원했던 가족만 작성할 수 있어요')
            && guestText.includes('팀은 삭제할 수 없고 답글만 달 수 있습니다')
            && guestText.includes('4.0') && guestText.includes('리뷰 1개'));
        check('UI 게스트: 쓰기 버튼 없음·재원 확인됨 캡슐·별점',
            !guestText.includes('리뷰 쓰기') && guestText.includes('재원 확인됨') && guestText.includes('★★★★☆'));
        await page.screenshot({ path: SHOT + 'guest.png' });
        await page.close();

        // 보호자 — 내 카드 ⋯ 노출 (이미 작성 상태라 쓰기 버튼은 없음)
        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), guardian);
        await page.goto(BASE + `/team/${SLUG}/review`, { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));
        const mineMenu = await page.evaluate(() => [...document.querySelectorAll('button')]
            .filter(b => b.getBoundingClientRect().width > 0 && b.innerText.trim() === '⋯').length);
        check('UI 보호자: 내 리뷰 카드 ⋯ 1개 (이미 작성 → 쓰기 버튼 없음)', mineMenu >= 1,
            `⋯ ${mineMenu}개`);

        // 삭제 후 쓰기 버튼 → 폼 왕복
        await deleteReview(guardian, myId);
        await page.reload({ waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));
        await page.evaluate(() => [...document.querySelectorAll('button')]
            .filter(b => b.getBoundingClientRect().width > 0)
            .find(b => b.innerText.trim() === '리뷰 쓰기')?.click());
        await new Promise(r => setTimeout(r, 700));
        const formText = await page.evaluate(() => document.body.innerText);
        check('UI 폼: 열림 — 별점·본문·부제', formText.includes('별점')
            && formText.includes('리뷰 내용') && formText.includes('팀은 리뷰를 삭제할 수 없어요'));

        // 별 3점 선택 + 본문 입력 + 제출
        await page.evaluate(() => {
            const stars = [...document.querySelectorAll('button[aria-label$="점"]')]
                .filter(b => b.getBoundingClientRect().width > 0);
            stars[2]?.click(); // 3점
        });
        await new Promise(r => setTimeout(r, 300));
        await page.evaluate(() => {
            const area = [...document.querySelectorAll('textarea')].find(t => t.getBoundingClientRect().width > 0);
            if (area) {
                area.value = 'UI 검증 리뷰 — 코치진 소통이 좋아요.';
                area.dispatchEvent(new Event('input', { bubbles: true }));
                area.dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
        await new Promise(r => setTimeout(r, 300));
        await page.evaluate(() => [...document.querySelectorAll('button')]
            .filter(b => b.getBoundingClientRect().width > 0)
            .find(b => b.innerText.trim() === '리뷰 올리기')?.click());
        await new Promise(r => setTimeout(r, 1500));
        const savedText = await page.evaluate(() => document.body.innerText);
        check('UI 폼: 저장 → 토스트 + 카드 반영 (★★★☆☆)',
            savedText.includes('리뷰를 올렸어요') && savedText.includes('★★★☆☆')
            && savedText.includes('UI 검증 리뷰'));
        await page.screenshot({ path: SHOT + 'written.png' });
        await page.close();

        // 모바일 390
        page = await browser.newPage();
        await page.setViewport({ width: 390, height: 844 });
        await page.goto(BASE + `/team/${SLUG}/review`, { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));
        const mob = await page.evaluate(() => ({
            hScroll: document.documentElement.scrollWidth > document.documentElement.clientWidth,
            text: document.body.innerText,
        }));
        check('UI 모바일: 축약 부제·가로 스크롤 0', !mob.hScroll && mob.text.includes('재원 가족만 작성할 수 있어요'));
        await page.screenshot({ path: SHOT + 'mobile.png' });
        await page.close();

        console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
        process.exitCode = fail > 0 ? 1 : 0;
    } finally {
        browser.disconnect();
        edge.kill();
        sql('DELETE FROM SoccerTeamReviews');
        console.log('원복 완료 (SoccerTeamReviews 전체 삭제)');
    }
})();
