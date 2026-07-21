// 허브 바로가기 카드 확인 — 팀 탐색(전체) + 계정 설정(PC만). 사전: 계정에 자녀 2명(SQL로 구성).
const { chromium } = require('playwright-core');

const BASE = 'http://localhost:5000';
let failed = false;
function check(name, cond, detail) {
    console.log(`${cond ? 'PASS' : 'FAIL'} ${name}${detail ? ' — ' + detail : ''}`);
    if (!cond) failed = true;
}

const vis = (page, sel, opt) => page.locator(sel, opt).filter({ visible: true });

(async () => {
    const r = await fetch(`${BASE}/api/auth/login/email`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: 'verify-orphan-0721@test.local', password: 'password123!' }),
    });
    const token = (await r.json()).data.accessToken;

    const browser = await chromium.launch({
        executablePath: 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
        headless: true,
    });

    let ctx = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    let page = await ctx.newPage();
    await page.goto(`${BASE}/`);
    await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
    await page.goto(`${BASE}/dashboard`);
    await vis(page, 'text=바로가기').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(600);
    check('hub shortcut: explore card', await vis(page, 'a', { hasText: '우리 지역 팀 찾기' }).count() > 0);
    check('hub shortcut: settings card (PC)', await vis(page, 'text=역할 · 가족 · 알림 설정').count() > 0);
    await page.screenshot({ path: 'en-hub-pc.png', fullPage: true });
    await vis(page, 'a', { hasText: '우리 지역 팀 찾기' }).first().click();
    await page.waitForTimeout(1500);
    check('hub explore -> /teams', page.url().endsWith('/teams'), page.url());
    await ctx.close();

    ctx = await browser.newContext({ viewport: { width: 390, height: 844 } });
    page = await ctx.newPage();
    await page.goto(`${BASE}/`);
    await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
    await page.goto(`${BASE}/dashboard`);
    await vis(page, 'text=바로가기').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(600);
    check('hub mobile: explore visible', await vis(page, 'a', { hasText: '우리 지역 팀 찾기' }).count() > 0);
    check('hub mobile: settings hidden', await vis(page, 'text=역할 · 가족 · 알림 설정').count() === 0);
    await page.screenshot({ path: 'en-hub-mobile.png', fullPage: true });
    await ctx.close();

    await browser.close();
    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
