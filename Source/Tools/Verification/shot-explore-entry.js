// 팀 탐색 진입점 4갈래 검증 (Design.Navigation 7/21 보강):
// ① 랜딩 PC GNB 메뉴 + 히어로 보조 CTA / 모바일 콘텐츠 카드(히어로 CTA엔 없음)
// ② 선수 대시보드 — 무소속 카드 "팀 찾아보기" / 소속 선수에겐 미노출
// ③ 팀 대시보드 사이드바 하단 회색 링크  ④ 허브 바로가기(팀 탐색 전체 + 계정 설정 PC만)
const { chromium } = require('playwright-core');

const BASE = 'http://localhost:5000';
let failed = false;
function check(name, cond, detail) {
    console.log(`${cond ? 'PASS' : 'FAIL'} ${name}${detail ? ' — ' + detail : ''}`);
    if (!cond) failed = true;
}

async function login(email) {
    const r = await fetch(`${BASE}/api/auth/login/email`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json()).data.accessToken;
}

const vis = (page, sel, opt) => page.locator(sel, opt).filter({ visible: true });

async function open(browser, token, url, viewport) {
    const ctx = await browser.newContext({ viewport });
    const page = await ctx.newPage();
    if (token) {
        await page.goto(`${BASE}/`);
        await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
    }
    await page.goto(url);
    return { ctx, page };
}

(async () => {
    const browser = await chromium.launch({
        executablePath: 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
        headless: true,
    });

    //.// ① 랜딩 PC — GNB 메뉴 + 히어로 보조 CTA → /teams 이동
    let { ctx, page } = await open(browser, null, `${BASE}/`, { width: 1440, height: 1000 });
    await vis(page, 'text=유소년 축구,').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(800);
    check('landing PC gnb menu', await vis(page, 'nav a', { hasText: '팀 탐색' }).count() > 0);
    const heroCta = vis(page, 'a', { hasText: '우리 동네 팀 둘러보기' });
    check('landing PC hero secondary CTA', await heroCta.count() > 0);
    await page.screenshot({ path: 'en-landing-pc.png' });
    await heroCta.first().click();
    await vis(page, 'text=우리 지역 팀 찾기').first().waitFor({ timeout: 30000 });
    check('hero CTA -> /teams', page.url().endsWith('/teams'), page.url());
    await ctx.close();

    //.// ① 랜딩 모바일 — 히어로 CTA 없음, 히어로 아래 콘텐츠 카드
    ({ ctx, page } = await open(browser, null, `${BASE}/`, { width: 390, height: 844 }));
    await vis(page, 'text=유소년 축구,').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(800);
    const mobileCards = vis(page, 'a', { hasText: '우리 동네 팀 둘러보기' });
    check('landing mobile card exists (only once)', await mobileCards.count() === 1, String(await mobileCards.count()));
    const hScroll = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth);
    check('landing mobile no horizontal scroll', !hScroll);
    await page.screenshot({ path: 'en-landing-mobile.png' });
    await ctx.close();

    //.// ② 선수 대시보드 — 무소속(EmptyFC 아님 — 무소속 선수 계정 필요): verify-player-u12는 소속.
    // 소속 선수(신준우) = 미노출 확인
    const attached = await login('verify-player-u12@test.local');
    ({ ctx, page } = await open(browser, attached, `${BASE}/dashboard/player/profile`, { width: 1440, height: 1000 }));
    await vis(page, 'text=항목별 공개 설정').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(600);
    check('attached player: no explore CTA', await vis(page, 'a', { hasText: '팀 찾아보기' }).count() === 0);
    await ctx.close();

    // 무소속 = 온보딩 임시 프로필 계정 (find-or-create 새 계정 + 선수 프로필 온보딩) — API로 만든다
    const orphanToken = await login('verify-orphan-0721@test.local');
    const created = await fetch(`${BASE}/api/soccer/player/me/profile`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${orphanToken}` },
        body: JSON.stringify({ name: '홑선수', birthDate: '2012.03.14', ageGroup: 'U12' }),
    }).then(r => r.json());
    if (!created.isSuccess) { console.error('profile creation failed', created); process.exit(1); }
    const orphanToken2 = created?.data?.accessToken ?? orphanToken; // 승격 토큰 교체
    ({ ctx, page } = await open(browser, orphanToken2, `${BASE}/dashboard/player/profile`, { width: 1440, height: 1000 }));
    await vis(page, 'text=초대코드로 팀 연결').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(600);
    check('orphan player: explore CTA shown', await vis(page, 'a', { hasText: '팀 찾아보기' }).count() > 0);
    check('orphan helper copy', await vis(page, 'text=받은 코드가 없다면 우리 지역 팀을 둘러보세요.').count() > 0);
    await page.screenshot({ path: 'en-player-orphan.png', fullPage: true });
    await vis(page, 'a', { hasText: '팀 찾아보기' }).first().click();
    await vis(page, 'text=우리 지역 팀 찾기').first().waitFor({ timeout: 30000 });
    check('orphan CTA -> /teams', page.url().endsWith('/teams'));
    await ctx.close();

    //.// ③ 팀 대시보드 사이드바 하단 링크
    const manager = await login('verify-teamadmin-0713@test.local');
    ({ ctx, page } = await open(browser, manager, `${BASE}/dashboard/team`, { width: 1440, height: 1000 }));
    await vis(page, 'text=팀 정보').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(800);
    const sideLink = vis(page, 'aside a', { hasText: '팀 탐색' });
    check('team sidebar bottom link', await sideLink.count() > 0);
    await page.screenshot({ path: 'en-team-sidebar.png' });
    await sideLink.first().click();
    await vis(page, 'text=우리 지역 팀 찾기').first().waitFor({ timeout: 30000 });
    check('sidebar link -> /teams', page.url().endsWith('/teams'));
    await ctx.close();

    //.// ④ 허브 바로가기는 자녀 2명 구성이 필요해 별도 스크립트(shot-hub-shortcut.js)에서 확인한다

    await browser.close();
    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
