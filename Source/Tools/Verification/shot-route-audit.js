// 라우트 재감사 검증 — 이번에 연결한 링크들의 실제 동작:
// PublicGnb(팀 탐색 메뉴·설정 드롭다운) · 404 팀 탐색 버튼 · 허브 ＋자녀 연결/＋팀 만들기 ·
// 공개홈 관리자 "관리" 링크(본인만) · 공개홈 로스터 # 링크 0건 · 팀 탐색 returnUrl.
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
    const ctx = await browser.newContext({ viewport: viewport ?? { width: 1440, height: 1000 } });
    const page = await ctx.newPage();
    if (token) {
        await page.goto(`${BASE}/`);
        await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
    }
    await page.goto(url);
    return { ctx, page };
}

(async () => {
    const manager = await login('verify-teamadmin-0713@test.local');
    const other = await login('verify-u15-1@test.local');
    const browser = await chromium.launch({
        executablePath: 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
        headless: true,
    });

    //.// 1) PublicGnb (경기기록 페이지) — 게스트: 팀 탐색 메뉴 / 로그인: 설정 드롭다운
    let { ctx, page } = await open(browser, null, `${BASE}/records`);
    await vis(page, 'text=경기기록').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(800);
    check('guest gnb: explore link', await vis(page, 'header a', { hasText: '팀 탐색' }).count() > 0);
    await ctx.close();

    ({ ctx, page } = await open(browser, manager, `${BASE}/records`));
    await vis(page, 'header button[aria-haspopup="true"]').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(500);
    check('authed gnb: explore link', await vis(page, 'header a', { hasText: '팀 탐색' }).count() > 0);
    await vis(page, 'header button[aria-haspopup="true"]').first().click();
    await page.waitForTimeout(300);
    check('avatar dropdown: settings', await vis(page, 'a', { hasText: /^설정$/ }).count() > 0);
    await vis(page, 'a', { hasText: /^설정$/ }).first().click();
    await page.waitForTimeout(1500);
    check('settings link works', page.url().includes('/settings'), page.url());
    await ctx.close();

    //.// 2) 404 — [홈으로 / 팀 탐색]
    ({ ctx, page } = await open(browser, null, `${BASE}/not-found`));
    await vis(page, 'text=페이지를 찾을 수 없어요').first().waitFor({ timeout: 60000 });
    check('404 explore button', await vis(page, 'a', { hasText: '팀 탐색' }).count() > 0);
    await ctx.close();

    //.// 3) 공개홈 — 관리자 본인 "관리" 링크 / 남·게스트 미노출 / 로스터 # 링크 0건
    ({ ctx, page } = await open(browser, manager, `${BASE}/team/검증fc`));
    await vis(page, 'text=핵심가치').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(800);
    check('owner sees 관리 link', await vis(page, 'header a', { hasText: /^관리$/ }).count() > 0);
    await vis(page, 'header a', { hasText: /^관리$/ }).first().click();
    await page.waitForTimeout(1800);
    check('관리 -> team dashboard', page.url().includes('/dashboard/team'), page.url());
    await ctx.close();

    ({ ctx, page } = await open(browser, other, `${BASE}/team/검증fc`));
    await vis(page, 'text=핵심가치').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(800);
    check('non-owner: no 관리 link', await vis(page, 'header a', { hasText: /^관리$/ }).count() === 0);
    await ctx.close();

    ({ ctx, page } = await open(browser, null, `${BASE}/team/검증fc/roster`));
    await vis(page, 'text=선수단').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(800);
    check('guest: no 관리 link', await vis(page, 'header a', { hasText: /^관리$/ }).count() === 0);
    const deadLinks = await page.locator('a[href="#"]').count();
    check('roster tab: zero # links', deadLinks === 0, String(deadLinks));
    await ctx.close();

    //.// 4) 대시보드 로스터 — 선수명 # 링크 0건
    ({ ctx, page } = await open(browser, manager, `${BASE}/dashboard/team/roster`));
    await vis(page, 'text=선수단').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(1200);
    check('dashboard roster: zero # links', await page.locator('a[href="#"]').count() === 0);
    await ctx.close();

    //.// 5) 로그인 페이지 — 약관 # 링크 0건
    ({ ctx, page } = await open(browser, null, `${BASE}/login`));
    await vis(page, 'text=이용약관').first().waitFor({ timeout: 60000 });
    check('login page: zero # links', await page.locator('a[href="#"]').count() === 0);
    await ctx.close();

    //.// 6) 팀 탐색 — "팀 홈페이지 만들기" returnUrl 보존
    ({ ctx, page } = await open(browser, null, `${BASE}/teams`));
    await vis(page, 'text=우리 지역 팀 찾기').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(500);
    const createHref = await vis(page, 'a', { hasText: '팀 홈페이지 만들기' }).first().getAttribute('href');
    check('team create keeps returnUrl', createHref?.includes('returnUrl=%2Fteams') === true, createHref ?? 'none');
    await ctx.close();

    await browser.close();
    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
