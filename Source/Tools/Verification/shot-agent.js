// 에이전트 열람 승인 UI 왕복 — 알림 패널(violet 뱃지) 딥링크 → pending 3액션 → 승인 →
// 카운트다운·열람 기록 → (SQL 만료 후) 만료 표시 → 철회 모달 → denied. 모바일 카드.
// 사용: node shot-agent.js <pendingRequestId> <mobileRequestId>
const { chromium } = require('playwright-core');

const BASE = 'http://localhost:5000';
const [ID_UI, ID_MOBILE] = process.argv.slice(2);
const MODE = process.argv[4] ?? 'phase1'; // phase1: pending→승인 / phase2: 만료→철회

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
    await page.goto(`${BASE}/`);
    await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
    await page.goto(url);
    return { ctx, page };
}

(async () => {
    const guardian = await login('verify-player-u15@test.local');
    const browser = await chromium.launch({
        executablePath: 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
        headless: true,
    });

    if (MODE === 'phase1') {
        //.// 1) 알림 패널 — 열람 요청 행(violet 뱃지) → 딥링크
        let { ctx, page } = await open(browser, guardian, `${BASE}/dashboard/player/profile`, { width: 1440, height: 1050 });
        await vis(page, 'text=항목별 공개 설정').first().waitFor({ timeout: 60000 });
        await page.waitForTimeout(1000);
        await vis(page, 'button[aria-label="알림"]').first().click();
        await vis(page, 'text=상세 정보 열람 요청').first().waitFor({ timeout: 30000 });
        check('view request notification row', true);
        check('agent violet badge', await vis(page, 'text=에이전트').count() > 0);
        check('type chip', await vis(page, 'button', { hasText: '열람 요청' }).count() > 0);
        await page.screenshot({ path: 'ag-panel.png' });
        await vis(page, 'text=박OO 님이 김정현 선수의 상세 정보 열람을 요청했어요').first().click();
        await page.waitForTimeout(2500);
        check('deep link to approval page', page.url().includes('/approvals/agent/'), page.url());

        //.// 2) pending 화면 — 신원 카드·범위·안전 안내·3액션
        await vis(page, 'text=승인 시 열람되는 정보').first().waitFor({ timeout: 60000 });
        await page.waitForTimeout(600);
        check('agent card', await vis(page, 'text=인증 에이전트').count() > 0
            && await vis(page, 'text=드림 스포츠 에이전시 · 등록 2024').count() > 0);
        check('stats', await vis(page, 'text=중개 이력').count() > 0 && await vis(page, 'text=활동 지역').count() > 0);
        check('contact always excluded copy', await vis(page, 'text=연락처 직접 노출').count() > 0
            && await vis(page, 'text=항상 제외 — 플랫폼 메시지만').count() > 0);
        check('safety box (30d/log/revoke)', await vis(page, 'text=30일 후 자동 만료').count() > 0);
        check('pending actions', await vis(page, 'button', { hasText: '30일 열람 승인' }).count() > 0
            && await vis(page, 'button', { hasText: '이 에이전트의 요청 다시 받지 않기' }).count() > 0);
        await page.screenshot({ path: 'ag-pending.png', fullPage: true });

        //.// 3) 승인 → approved (카운트다운 + 열람 기록 + 철회)
        await vis(page, 'button', { hasText: '30일 열람 승인' }).first().click();
        await vis(page, 'text=일 남음').first().waitFor({ timeout: 20000 });
        check('approved countdown', await vis(page, 'text=승인됨 —').count() > 0);
        check('view log with Approved', await vis(page, 'text=열람 기록').count() > 0
            && await vis(page, 'text=승인 완료').count() > 0);
        check('revoke button', await vis(page, 'button', { hasText: '열람 권한 철회' }).count() > 0);
        await page.screenshot({ path: 'ag-approved.png', fullPage: true });
        await ctx.close();

        //.// 4) 모바일 pending 카드
        ({ ctx, page } = await open(browser, guardian, `${BASE}/approvals/agent/${ID_MOBILE}`, { width: 390, height: 844 }));
        await vis(page, 'text=승인 시 열람되는 정보').first().waitFor({ timeout: 60000 });
        await page.waitForTimeout(600);
        check('mobile card', await vis(page, 'text=알림 센터에서 열림').count() > 0);
        const hScroll = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth);
        check('no horizontal scroll', !hScroll);
        await page.screenshot({ path: 'ag-mobile.png', fullPage: true });
        await ctx.close();
    }
    else {
        //.// phase2: (SQL로 만료·로그 적재 후) 만료 표시 → 철회 모달 → denied
        const { ctx, page } = await open(browser, guardian, `${BASE}/approvals/agent/${ID_UI}`, { width: 1440, height: 1050 });
        await vis(page, 'text=승인 시 열람되는 정보').first().waitFor({ timeout: 60000 });
        await page.waitForTimeout(600);
        check('expired label after 30 days', await vis(page, 'text=만료됨 —').count() > 0);
        check('agent view logs listed', await vis(page, 'text=경기별 상세 기록 열람').count() > 0
            && await vis(page, 'text=디테일 권한 뷰 방문').count() > 0);
        await page.screenshot({ path: 'ag-expired.png', fullPage: true });

        await vis(page, 'button', { hasText: '열람 권한 철회' }).first().click();
        await vis(page, 'text=열람 권한을 철회할까요?').first().waitFor({ timeout: 10000 });
        await vis(page, 'button', { hasText: /^철회$/ }).last().click();
        await vis(page, 'text=거절됨 — 에이전트에게는 사유 없이').first().waitFor({ timeout: 20000 });
        check('revoke -> denied view', await vis(page, 'text=같은 에이전트는 30일간 다시 요청할 수 없어요').count() > 0);
        await page.screenshot({ path: 'ag-denied.png', fullPage: true });
        await ctx.close();
    }

    await browser.close();
    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
