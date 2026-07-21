// 계층 스위치 검증 — 선수 프로필 공개 범위 (Design.ToggleSwitch 계층 절):
// 상위 "프로필 공개" off → 하위 그룹 dimmed(.45)+비활성 + 실행취소 토스트
// → 공개 팀 홈 로스터에서 그 선수가 사라짐 → 실행취소 → 하위 복귀 + 로스터 복귀.
const { chromium } = require('playwright-core');

const BASE = 'http://localhost:5000';
const TEAM_HOME = `${BASE}/team/gwangju-fc-u15/roster`;
let failed = false;
function check(name, cond, detail) {
    console.log(`${cond ? 'PASS' : 'FAIL'} ${name}${detail ? ' — ' + detail : ''}`);
    if (!cond) failed = true;
}

async function rosterHasPlayer(browser, name) {
    const ctx = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const page = await ctx.newPage();
    await page.goto(TEAM_HOME);
    await page.locator('text=선수단').filter({ visible: true }).first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(1500);
    const count = await page.locator(`text=${name}`).filter({ visible: true }).count();
    await ctx.close();
    return count > 0;
}

(async () => {
    const r = await fetch(`${BASE}/api/auth/login/email`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: 'verify-player-u15@test.local', password: 'password123!' }),
    });
    const token = (await r.json()).data.accessToken;

    const browser = await chromium.launch({
        executablePath: 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
        headless: true,
    });

    // 0) 사전: 공개홈 로스터에 김정현 존재
    check('roster has 김정현 (before)', await rosterHasPlayer(browser, '김정현'));

    // 1) 선수 대시보드 프로필 — 계층 스위치 구조
    const ctx = await browser.newContext({ viewport: { width: 1440, height: 1100 } });
    const page = await ctx.newPage();
    await page.goto(`${BASE}/`);
    await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
    await page.goto(`${BASE}/dashboard/player/profile`);
    await page.locator('text=항목별 공개 설정').filter({ visible: true }).first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(800);

    const profileSwitch = page.locator('button[role="switch"]', { hasText: '프로필 공개' }).filter({ visible: true }).first();
    check('profile switch on by default', (await profileSwitch.getAttribute('aria-checked')) === 'true');
    check('caption copy', await page.locator('text=끄면 검색·팀 선수단에서 숨겨져요').filter({ visible: true }).count() > 0);

    const subSwitches = page.locator('button[role="switch"]').filter({ visible: true });
    const subCount = await subSwitches.count();
    check('sub switches rendered', subCount > 1, String(subCount));
    await page.screenshot({ path: 'hi-pc-on.png', fullPage: true });

    // 2) 상위 off → 하위 dimmed+비활성 + 실행취소 토스트
    await profileSwitch.click();
    await page.waitForTimeout(1000);
    check('profile switch off', (await profileSwitch.getAttribute('aria-checked')) === 'false');
    check('undo toast', await page.locator('text=프로필을 비공개로 바꿨어요').filter({ visible: true }).count() > 0);

    const firstSub = page.locator('button[role="switch"]', { hasText: '키' }).filter({ visible: true }).first();
    check('sub switch disabled', await firstSub.isDisabled());
    const subGroup = page.locator('.border-l-2').filter({ visible: true }).first();
    const opacity = await subGroup.evaluate(el => getComputedStyle(el).opacity);
    check('sub group dimmed .45', opacity === '0.45', opacity);
    await page.screenshot({ path: 'hi-pc-off.png', fullPage: true });

    // 3) 공개홈 로스터에서 사라짐
    check('roster hides 김정현 (off)', !(await rosterHasPlayer(browser, '김정현')));

    // 4) 실행취소 → 복귀
    await page.locator('button', { hasText: '실행취소' }).filter({ visible: true }).first().click();
    await page.waitForTimeout(1200);
    check('profile switch back on', (await profileSwitch.getAttribute('aria-checked')) === 'true');
    check('sub switch enabled again', !(await firstSub.isDisabled()));
    check('roster has 김정현 (restored)', await rosterHasPlayer(browser, '김정현'));
    await page.screenshot({ path: 'hi-pc-restored.png', fullPage: true });

    // 5) 모바일 — 같은 구조 확인
    const mctx = await browser.newContext({ viewport: { width: 390, height: 844 } });
    const mpage = await mctx.newPage();
    await mpage.goto(`${BASE}/`);
    await mpage.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
    await mpage.goto(`${BASE}/dashboard/player/profile`);
    await mpage.locator('text=항목별 공개 설정').filter({ visible: true }).first().waitFor({ timeout: 60000 });
    await mpage.waitForTimeout(800);
    const mProfile = mpage.locator('button[role="switch"]', { hasText: '프로필 공개' }).filter({ visible: true }).first();
    check('mobile profile switch', (await mProfile.getAttribute('aria-checked')) === 'true');
    const mRow = mpage.locator('button[role="switch"]').filter({ visible: true }).first();
    const rowHeight = await mRow.evaluate(el => el.getBoundingClientRect().height);
    check('mobile row touch target >= 56px', rowHeight >= 56, String(rowHeight));
    await mpage.screenshot({ path: 'hi-mo.png', fullPage: true });
    await mctx.close();
    await ctx.close();

    await browser.close();
    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
