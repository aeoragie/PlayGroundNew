// Claim UI 왕복 — 검수 시나리오: 신청(보호자 /claim) → 관리자 알림 패널 인라인 승인 → 허브 자녀 카드 반영.
// + Stepper(PC 도트/모바일 진행 바, 완료 스텝 클릭 뒤로 + 입력 유지) + 재방문 복원.
const { chromium } = require('playwright-core');

const BASE = 'http://localhost:5000';
const CODE = '3BA109'; // 한이든 (검증fc)

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

async function open(browser, token, url, viewport) {
    const ctx = await browser.newContext({ viewport });
    const page = await ctx.newPage();
    await page.goto(`${BASE}/`);
    await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
    await page.goto(url);
    return { ctx, page };
}

(async () => {
    const guardian = await login('verify-guardian-0721@test.local');
    const manager = await login('verify-teamadmin-0713@test.local');
    const browser = await chromium.launch({
        executablePath: 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
        headless: true,
    });

    //.// 1) 보호자 /claim — 스텝1 (거절 이력이 최신이라 스텝1에서 시작해야 한다)
    let { ctx, page } = await open(browser, guardian, `${BASE}/claim`, { width: 1280, height: 960 });
    await page.locator('text=초대코드를 입력해 주세요').filter({ visible: true }).first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(500);
    check('rejected history -> starts at step 1', true);
    await page.screenshot({ path: 'cl-step1.png', fullPage: true });

    // 코드 입력 (투명 오버레이 input)
    await page.locator('input[aria-label="초대코드"]').fill(CODE);
    await page.waitForTimeout(300);
    await page.screenshot({ path: 'cl-step1-filled.png' });
    await page.locator('button', { hasText: '프로필 찾기' }).filter({ visible: true }).first().click();
    await page.locator('text=이 프로필이 맞나요?').filter({ visible: true }).first().waitFor({ timeout: 30000 });
    check('step2 card', await page.locator('text=한이든').filter({ visible: true }).count() > 0);
    check('unclaimed badge', await page.locator('text=미연결').filter({ visible: true }).count() > 0);
    await page.screenshot({ path: 'cl-step2.png', fullPage: true });

    // 완료 스텝 클릭(뒤로) → 스텝1, 코드 유지 (입력 유지)
    await page.locator('.hidden.md\\:flex button').filter({ visible: true }).first().click();
    await page.waitForTimeout(400);
    check('back to step1 via stepper', await page.locator('text=초대코드를 입력해 주세요').filter({ visible: true }).count() > 0);
    const kept = await page.locator('input[aria-label="초대코드"]').inputValue();
    check('code kept after back', kept === CODE, kept);

    // 다시 진행 → 관계 아버지 → 요청
    await page.locator('button', { hasText: '프로필 찾기' }).filter({ visible: true }).first().click();
    await page.locator('text=이 프로필이 맞나요?').filter({ visible: true }).first().waitFor({ timeout: 30000 });
    await page.locator('button', { hasText: '아버지' }).filter({ visible: true }).first().click();
    await page.locator('button', { hasText: '연결 요청 보내기' }).filter({ visible: true }).first().click();
    await page.locator('text=승인을 기다리고 있어요').filter({ visible: true }).first().waitFor({ timeout: 30000 });
    check('step3 pending', await page.locator('text=승인 대기').filter({ visible: true }).count() > 0);
    check('summary relation', await page.locator('text=한이든 · 보호자(아버지)').filter({ visible: true }).count() > 0);
    await page.screenshot({ path: 'cl-step3.png', fullPage: true });

    // 재방문 복원 — 새로고침해도 대기 화면
    await page.reload();
    await page.locator('text=승인을 기다리고 있어요').filter({ visible: true }).first().waitFor({ timeout: 60000 });
    check('revisit restores pending', true);
    await ctx.close();

    //.// 2) 관리자 — 팀 대시보드 벨 → 패널 → 인라인 승인
    ({ ctx, page } = await open(browser, manager, `${BASE}/dashboard/team`, { width: 1440, height: 1000 }));
    await page.locator('text=팀 정보').filter({ visible: true }).first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(1500);
    const bell = page.locator('button[aria-label="알림"]').filter({ visible: true }).first();
    const badge = await bell.locator('span').filter({ visible: true }).count();
    check('bell badge visible', badge > 0);
    await bell.click();
    await page.locator('text=프로필 연결 요청').filter({ visible: true }).first().waitFor({ timeout: 30000 });
    check('action card copy', await page.locator('text=프로필 연결을 요청했어요').filter({ visible: true }).count() > 0);
    check('code meta', await page.locator(`text=초대코드 ${CODE} 사용`).filter({ visible: true }).count() > 0);
    check('permission foot', await page.locator('text=승인하면 프로필 관리 권한이 보호자에게 이전돼요').filter({ visible: true }).count() > 0);
    await page.screenshot({ path: 'cl-panel-pending.png' });

    await page.locator('button', { hasText: /^승인$/ }).filter({ visible: true }).first().click();
    await page.locator('text=승인 완료 — 한이든 선수가 Claimed 상태가 됐어요').filter({ visible: true }).first().waitFor({ timeout: 30000 });
    check('inline approve -> done box', true);
    await page.screenshot({ path: 'cl-panel-approved.png' });
    await ctx.close();

    //.// 3) 보호자 — /claim 재방문 = 완료 화면, 허브 자녀 카드 반영
    ({ ctx, page } = await open(browser, guardian, `${BASE}/claim`, { width: 1280, height: 960 }));
    await page.locator('text=한이든 선수와 연결됐어요').filter({ visible: true }).first().waitFor({ timeout: 60000 });
    check('step4 done', await page.locator('text=선수 대시보드로 가기').filter({ visible: true }).count() > 0);
    await page.screenshot({ path: 'cl-step4.png', fullPage: true });

    await page.goto(`${BASE}/dashboard`);
    await page.waitForTimeout(2500);
    const hubHasChildren = await page.locator('text=박도윤').filter({ visible: true }).count() > 0
        && await page.locator('text=한이든').filter({ visible: true }).count() > 0;
    check('hub shows both children', hubHasChildren, page.url());
    await page.screenshot({ path: 'cl-hub.png', fullPage: true });
    await ctx.close();

    //.// 4) 모바일 — 스텝퍼 진행 바(도트 없음) + 가로 스크롤
    ({ ctx, page } = await open(browser, manager, `${BASE}/claim`, { width: 390, height: 844 }));
    await page.locator('text=초대코드를 입력해 주세요').filter({ visible: true }).first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(400);
    check('mobile count 1 / 4', await page.locator('text=1 / 4').filter({ visible: true }).count() > 0);
    const hScroll = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth);
    check('no horizontal scroll', !hScroll);
    await page.screenshot({ path: 'cl-mobile-step1.png', fullPage: true });
    await ctx.close();

    await browser.close();
    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
