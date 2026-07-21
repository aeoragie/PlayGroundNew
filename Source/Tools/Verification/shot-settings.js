// 설정 화면 UI 검증 — 지시 시나리오 3종 중심:
// ① 스위치 실패 롤백(PUT abort → 낙관 반영 → 롤백 + 오류 토스트)
// ② 계정 삭제 입력 잠금(HighRisk — 문구 일치 전 버튼 비활성)
// ③ 잠금 알림("항상 켜짐" 뱃지 + 스위치 없음)
// + 탭 URL 동기화 · 저장 성공 새로고침 유지 · 모바일 세그먼트 탭.
const { chromium } = require('playwright-core');

const BASE = 'http://localhost:5000';
let failed = false;
function check(name, cond, detail) {
    console.log(`${cond ? 'PASS' : 'FAIL'} ${name}${detail ? ' — ' + detail : ''}`);
    if (!cond) failed = true;
}

async function loginToken(email) {
    const r = await fetch(`${BASE}/api/auth/login/email`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    const env = await r.json();
    if (!env.isSuccess) throw new Error(`login failed ${email}`);
    return env.data.accessToken;
}

async function openWithToken(ctx, token, url) {
    const page = await ctx.newPage();
    await page.goto(`${BASE}/`);
    await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
    await page.goto(url);
    return page;
}

(async () => {
    const token = await loginToken('verify-teamadmin-0713@test.local');
    const browser = await chromium.launch({
        executablePath: 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
        headless: true,
    });

    //.// PC — 계정 탭 + 삭제 모달 잠금
    let ctx = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    let page = await openWithToken(ctx, token, `${BASE}/settings`);
    await page.locator('text=연결된 로그인').filter({ visible: true }).first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(600);
    check('masked email shown', await page.locator('text=ver***@test.local').filter({ visible: true }).count() > 0);
    await page.screenshot({ path: 'st-pc-account.png', fullPage: true });

    // 계정 삭제 → HighRisk 모달: 문구 입력 전 주 버튼 비활성
    await page.locator('button', { hasText: '삭제' }).filter({ visible: true }).first().click();
    await page.locator('text=계정을 삭제할까요?').filter({ visible: true }).first().waitFor({ timeout: 10000 });
    const modalConfirm = page.locator('[role="dialog"] button, .fixed button').filter({ hasText: /^삭제$/ }).filter({ visible: true }).last();
    check('delete locked before phrase', await modalConfirm.isDisabled());
    await page.screenshot({ path: 'st-pc-delete-locked.png' });

    const phraseInput = page.locator('input').filter({ visible: true }).last();
    await phraseInput.fill('계정 삭제');
    await page.waitForTimeout(300);
    check('delete unlocked after phrase', !(await modalConfirm.isDisabled()));
    await page.screenshot({ path: 'st-pc-delete-unlocked.png' });
    await page.keyboard.press('Escape');
    await page.waitForTimeout(400);
    check('modal closed, account intact', await page.locator('text=연결된 로그인').filter({ visible: true }).count() > 0);

    //.// 역할 탭 — URL 동기화 + 카드
    await page.locator('nav button', { hasText: '역할' }).filter({ visible: true }).first().click();
    await page.waitForTimeout(600);
    check('roles url', page.url().endsWith('/settings/roles'), page.url());
    check('team role card', await page.locator('text=팀 관리자').filter({ visible: true }).count() > 0);
    check('team name in card', await page.locator('text=검증fc').filter({ visible: true }).count() > 0);
    check('agent pending card', await page.locator('text=별도 서비스에서 운영돼요 · 준비 중').filter({ visible: true }).count() > 0);
    // 자녀 없음 → 보호자 카드 없음 (빈 데이터 노출 금지)
    check('no guardian card', await page.locator('text=프로필 관리').filter({ visible: true }).count() === 0);
    await page.screenshot({ path: 'st-pc-roles.png', fullPage: true });

    //.// 알림 탭 — 잠금 뱃지 + 스위치 수
    await page.locator('nav button', { hasText: '알림' }).filter({ visible: true }).first().click();
    await page.locator('text=수신 방법').filter({ visible: true }).first().waitFor({ timeout: 30000 });
    await page.waitForTimeout(600);
    check('notifications url', page.url().endsWith('/settings/notifications'), page.url());
    check('locked badge', await page.locator('text=항상 켜짐').filter({ visible: true }).count() === 1);
    const lockedRow = page.locator('button[role="switch"]', { hasText: '연결 요청 · 열람 요청' }).filter({ visible: true });
    const lockedRowTrack = lockedRow.locator('.bg-teal, .bg-switch-track');
    check('locked row has no track', await lockedRowTrack.count() === 0);
    const switches = page.locator('button[role="switch"]').filter({ visible: true });
    check('switch rows = 7 (locked 1 + toggleable 6)', await switches.count() === 7, String(await switches.count()));
    check('foot caption', await page.locator('text=승인이 필요한 알림은 안전을 위해 끌 수 없어요').filter({ visible: true }).count() > 0);
    await page.screenshot({ path: 'st-pc-notifications.png', fullPage: true });

    //.// 저장 성공 → 새로고침 유지 → 원복
    const visitRow = page.locator('button[role="switch"]', { hasText: '프로필 방문 요약' }).filter({ visible: true }).first();
    await visitRow.click();
    await page.waitForTimeout(800);
    await page.reload();
    await page.locator('text=수신 방법').filter({ visible: true }).first().waitFor({ timeout: 30000 });
    await page.waitForTimeout(800);
    const visitAfter = page.locator('button[role="switch"]', { hasText: '프로필 방문 요약' }).filter({ visible: true }).first();
    check('toggle persisted after reload', (await visitAfter.getAttribute('aria-checked')) === 'true');
    await visitAfter.click();
    await page.waitForTimeout(800);
    check('restored to off', (await visitAfter.getAttribute('aria-checked')) === 'false');

    //.// ① 실패 롤백 — PUT abort → 낙관 반영 후 롤백 + 오류 토스트
    await page.route('**/api/auth/me/notifications', route => {
        if (route.request().method() === 'PUT') return route.abort();
        return route.continue();
    });
    const emailRow = page.locator('button[role="switch"]', { hasText: '이메일' }).filter({ visible: true }).first();
    check('email switch off before', (await emailRow.getAttribute('aria-checked')) === 'false');
    await emailRow.click();
    await page.waitForTimeout(1200);
    check('rollback to off after fail', (await emailRow.getAttribute('aria-checked')) === 'false');
    check('error toast shown', await page.locator('text=설정을 저장하지 못했어요').filter({ visible: true }).count() > 0);
    await page.screenshot({ path: 'st-pc-rollback.png' });
    await page.unroute('**/api/auth/me/notifications');
    await ctx.close();

    //.// 모바일 — 세그먼트 탭 + 로그아웃 + 알림
    ctx = await browser.newContext({ viewport: { width: 390, height: 844 } });
    page = await openWithToken(ctx, token, `${BASE}/settings`);
    await page.locator('text=연결된 로그인').filter({ visible: true }).first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(600);
    check('mobile logout on account tab', await page.locator('main button', { hasText: '로그아웃' }).filter({ visible: true }).count() === 1);
    const hScroll = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth);
    check('no horizontal scroll', !hScroll);
    await page.screenshot({ path: 'st-mo-account.png', fullPage: true });

    const notiTab = page.locator('header button', { hasText: '알림' }).filter({ visible: true }).first();
    await notiTab.click();
    await page.locator('text=수신 방법').filter({ visible: true }).first().waitFor({ timeout: 30000 });
    await page.waitForTimeout(500);
    const tabColor = await notiTab.evaluate(el => getComputedStyle(el).borderBottomColor);
    check('mobile tab teal underline', tabColor === 'rgb(46, 196, 182)', tabColor);
    check('mobile locked badge', await page.locator('text=항상 켜짐').filter({ visible: true }).count() === 1);
    check('mobile no logout on noti tab', await page.locator('main button', { hasText: '로그아웃' }).filter({ visible: true }).count() === 0);
    await page.screenshot({ path: 'st-mo-notifications.png', fullPage: true });
    await ctx.close();

    await browser.close();
    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
