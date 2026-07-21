// 모집 탭 UI 왕복 — 대시보드 작성 폼(빈 제출 인라인) → 저장 토스트 → 공개홈 게스트 열람
// (모집중 카드·칩·지원하기 / 마감 카드 회색) → 마감 확인 모달 → 삭제 → 실행취소 → 모바일.
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

(async () => {
    const manager = await login('verify-teamadmin-0713@test.local');
    const browser = await chromium.launch({
        executablePath: 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
        headless: true,
    });

    //.// 1) 대시보드 모집 섹션 — 마감 카드 1장(API 검증 잔여) + 작성 폼
    let ctx = await browser.newContext({ viewport: { width: 1440, height: 1050 } });
    let page = await ctx.newPage();
    await page.goto(`${BASE}/`);
    await page.evaluate(t => localStorage.setItem('pg.accessToken', t), manager);
    await page.goto(`${BASE}/dashboard/team/recruit`);
    await vis(page, 'text=선수 모집').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(1000);
    check('closed card shown', await vis(page, 'text=마감').count() > 0);
    check('no applicant list (no schema)', await vis(page, 'text=지원자').count() === 0);
    await page.screenshot({ path: 'rc-dash-before.png', fullPage: true });

    // 작성 폼 — 빈 제출 = 인라인 오류(토스트 금지)
    await vis(page, 'button', { hasText: '＋ 공고 올리기' }).first().click();
    await vis(page, 'text=모집 공고 올리기').first().waitFor({ timeout: 10000 });
    await vis(page, 'button', { hasText: '공고 올리기' }).last().click();
    await page.waitForTimeout(500);
    check('inline errors on empty submit', await vis(page, 'text=공고 제목을 입력해 주세요.').count() > 0
        && await vis(page, 'text=공고 내용을 입력해 주세요.').count() > 0);
    await page.screenshot({ path: 'rc-form-errors.png' });

    // 입력 → 저장
    await vis(page, 'input[placeholder="U15 공격수 모집"]').first().fill('U12 골키퍼 모집');
    await vis(page, 'textarea').first().fill('선방 능력이 좋은 골키퍼 1명을 찾습니다.');
    await vis(page, 'input[placeholder="테스트 1회 · 주말, 9월 리그 등록 가능"]').first().fill('테스트 1회 · 주말');
    await vis(page, 'button', { hasText: '공고 올리기' }).last().click();
    await vis(page, 'text=공고를 올렸어요').first().waitFor({ timeout: 15000 });
    check('save toast', true);
    await page.waitForTimeout(800);
    check('open card rendered', await vis(page, 'text=U12 골키퍼 모집').count() > 0
        && await vis(page, 'text=모집중').count() > 0);
    await page.screenshot({ path: 'rc-dash-after.png', fullPage: true });
    await ctx.close();

    //.// 2) 공개홈 게스트 열람 (PC)
    ctx = await browser.newContext({ viewport: { width: 1280, height: 1000 } });
    page = await ctx.newPage();
    await page.goto(`${BASE}/team/검증fc/recruit`);
    await vis(page, 'text=선수 모집').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(1000);
    check('guest sees open posting', await vis(page, 'text=U12 골키퍼 모집').count() > 0);
    check('condition chip', await vis(page, 'text=테스트 1회 · 주말').count() > 0);
    check('apply button (no-op)', await vis(page, 'button', { hasText: '지원하기' }).count() > 0);
    check('closed posting grey', await vis(page, 'text=수정된 본문입니다.').count() > 0);
    check('inquiry card', await vis(page, 'text=모집 공고가 없어도 문의할 수 있어요').count() > 0
        && await vis(page, 'button', { hasText: '입단 문의 남기기' }).count() > 0);
    await page.screenshot({ path: 'rc-public-pc.png', fullPage: true });
    await ctx.close();

    //.// 3) 공개홈 모바일 — 문의 카드에 버튼 없음 + 가로 스크롤 없음
    ctx = await browser.newContext({ viewport: { width: 390, height: 844 } });
    page = await ctx.newPage();
    await page.goto(`${BASE}/team/검증fc/recruit`);
    await vis(page, 'text=선수 모집').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(1000);
    check('mobile open posting', await vis(page, 'text=U12 골키퍼 모집').count() > 0);
    check('mobile inquiry has no button', await vis(page, 'button', { hasText: '입단 문의 남기기' }).count() === 0);
    const hScroll = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth);
    check('no horizontal scroll', !hScroll);
    await page.screenshot({ path: 'rc-public-mobile.png', fullPage: true });
    await ctx.close();

    //.// 4) 마감 확인 모달 → 삭제 → 실행취소
    ctx = await browser.newContext({ viewport: { width: 1440, height: 1050 } });
    page = await ctx.newPage();
    await page.goto(`${BASE}/`);
    await page.evaluate(t => localStorage.setItem('pg.accessToken', t), manager);
    await page.goto(`${BASE}/dashboard/team/recruit`);
    await vis(page, 'text=U12 골키퍼 모집').first().waitFor({ timeout: 60000 });
    await page.waitForTimeout(800);

    // ⋯ → 마감하기 → 확인 모달(재오픈 불가 명시) → 마감
    const menus = vis(page, 'button[aria-label*="U12 골키퍼 모집"]');
    await menus.first().click();
    await vis(page, 'button', { hasText: '마감하기' }).first().click();
    await vis(page, 'text=공고를 마감할까요?').first().waitFor({ timeout: 10000 });
    check('close modal mentions one-way', await vis(page, 'text=다시 열 수 없어요').count() > 0);
    await page.screenshot({ path: 'rc-close-modal.png' });
    await vis(page, 'button', { hasText: /^마감하기$/ }).last().click();
    await vis(page, 'text=공고를 마감했어요').first().waitFor({ timeout: 15000 });
    await page.waitForTimeout(800);

    // 마감된 공고의 ⋯ → 삭제 → 파괴 모달 → 실행취소 왕복
    await vis(page, 'button[aria-label*="U12 골키퍼 모집"]').first().click();
    await vis(page, 'button', { hasText: /^삭제$/ }).first().click();
    await vis(page, 'text=공고를 삭제할까요?').first().waitFor({ timeout: 10000 });
    await vis(page, 'button', { hasText: /^삭제$/ }).last().click();
    await vis(page, 'text=공고를 삭제했어요').first().waitFor({ timeout: 15000 });
    await page.waitForTimeout(600);
    check('card gone after delete', await vis(page, 'text=U12 골키퍼 모집').count() === 0);
    await vis(page, 'button', { hasText: '실행취소' }).first().click();
    await page.waitForTimeout(1200);
    check('card back after undo', await vis(page, 'text=U12 골키퍼 모집').count() > 0);
    await page.screenshot({ path: 'rc-dash-final.png', fullPage: true });
    await ctx.close();

    await browser.close();
    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
