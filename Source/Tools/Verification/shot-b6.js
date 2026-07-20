// B6 UI 검증 — 공식 행 ⋯ 진입 / 친선 행 ⋯ 0건 / 신청 → 중복 시 "신청 처리 중" / 상태 3종 / 취소
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9523;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-b6-' + Date.now();
const sleep = ms => new Promise(r => setTimeout(r, ms));

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});

const ready = (page, text, timeout = 40000) =>
    page.waitForFunction(t => document.body.innerText.includes(t), { timeout, polling: 300 }, text);

const clickText = async (page, text) => {
    const ok = await page.evaluate(t => {
        const el = [...document.querySelectorAll('button, a, span')]
            .find(x => x.innerText?.trim() === t && x.getBoundingClientRect().width > 0);
        if (!el) { return false; }
        el.click();
        return true;
    }, text);
    if (!ok) { throw new Error('not found: ' + text); }
    await sleep(700);
};

// 보이는 ⋯만 (PC·모바일 트리가 둘 다 렌더된다)
const visibleOverflowCount = page => page.evaluate(() =>
    [...document.querySelectorAll('button[aria-label*="추가 작업"]')]
        .filter(b => b.getBoundingClientRect().width > 0).length);

const clickOverflow = async (page, index = 0) => {
    const ok = await page.evaluate(i => {
        const btns = [...document.querySelectorAll('button[aria-label*="추가 작업"]')]
            .filter(b => b.getBoundingClientRect().width > 0);
        if (!btns[i]) { return false; }
        btns[i].click();
        return true;
    }, index);
    if (!ok) { throw new Error('overflow trigger not found: ' + index); }
    await sleep(700);
};

const bodyHas = (page, t) => page.evaluate(x => document.body.innerText.includes(x), t);
const toastText = page => page.evaluate(() => document.querySelector('[role=status]')?.innerText ?? null);

const loginAs = async (page, email) => {
    const token = await page.evaluate(async (base, mail) => {
        const r = await fetch(base + '/api/auth/login/email', {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: mail, password: 'password123!' }),
        });
        return (await r.json())?.data?.accessToken ?? null;
    }, BASE, email);
    await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
    return token;
};

(async () => {
    const edge = spawn(EDGE, ['--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
        `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`, 'about:blank'], { stdio: 'ignore' });

    try {
        const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });
        const page = await browser.newPage();
        page.on('pageerror', e => console.log('PAGE ERROR:', e.message));
        await page.setViewport({ width: 1440, height: 1200 });
        await page.goto(BASE, { waitUntil: 'networkidle0', timeout: 60000 });

        //.// 1) 친선만 있는 팀 — ⋯ 가 0건이어야 한다
        await loginAs(page, 'verify-teamadmin-0713@test.local');
        await page.goto(BASE + '/dashboard/team/results', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '경기 결과');
        await sleep(1800);
        console.log('친선만 있는 팀 — 보이는 ⋯ 개수(0 기대):', await visibleOverflowCount(page));
        await page.screenshot({ path: 'b6-01-friendly-no-menu.png' });

        //.// 2) 공식 경기 팀 — ⋯ 진입
        await loginAs(page, 'verify-u15-1@test.local');
        await page.goto(BASE + '/dashboard/team/results', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '경기 결과');
        await sleep(1800);
        console.log('공식 경기 팀 — 보이는 ⋯ 개수:', await visibleOverflowCount(page));

        await clickOverflow(page, 0);
        console.log('⋯ 메뉴 항목:', JSON.stringify({
            request: await bodyHas(page, '기록 수정 신청'),
        }));
        await page.screenshot({ path: 'b6-02-menu.png' });

        //.// 3) 신청 폼
        await clickText(page, '기록 수정 신청');
        await sleep(1000);
        console.log('신청 폼:', JSON.stringify({
            title: await bodyHas(page, '기록 수정 신청'),
            fields4: (await bodyHas(page, '스코어')) && (await bodyHas(page, '득점·도움'))
                && (await bodyHas(page, '출전 선수')) && (await bodyHas(page, '기타')),
            current: await bodyHas(page, '현재 기록'),
            target: await bodyHas(page, '올바른 기록'),
            notice: await bodyHas(page, '신청은 주최측에 전달돼요'),
        }));
        await page.screenshot({ path: 'b6-03-form.png' });

        // 값을 안 바꾸고 제출 → 인라인 오류 (토스트 아님)
        await clickText(page, '신청하기');
        await sleep(700);
        console.log('변경 없이 제출:', JSON.stringify({
            inline: await bodyHas(page, '현재 기록과 같아요'),
            toast: (await toastText(page)) !== null,
        }));

        // 스코어를 고쳐 제출
        await page.evaluate(() => {
            const inputs = [...document.querySelectorAll('input[aria-label="상대 득점"]')]
                .filter(i => i.getBoundingClientRect().width > 0);
            const el = inputs[0];
            const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set;
            setter.call(el, '9');
            el.dispatchEvent(new Event('input', { bubbles: true }));
        });
        await sleep(400);
        await clickText(page, '신청하기');
        await sleep(2500);
        console.log('신청 토스트:', JSON.stringify(await toastText(page)));
        await page.screenshot({ path: 'b6-04-submitted.png' });

        //.// 4) 목록 + 중복 차단
        console.log('신청 목록 노출:', await bodyHas(page, '기록 수정 신청'), '/ 접수 뱃지:', await bodyHas(page, '접수'));
        await clickOverflow(page, 0);
        console.log('같은 경기 ⋯ (신청 처리 중 기대):', JSON.stringify({
            disabled: await bodyHas(page, '신청 처리 중'),
        }));
        await page.screenshot({ path: 'b6-05-pending-disabled.png' });
        await page.keyboard.press('Escape');
        await sleep(400);

        //.// 5) 반려 상태 — 주최측이 채우는 값이라 DB로 직접 심어 확인만 한다
        console.log('(반려 표시는 sql-b6.sql로 심사 결과를 심은 뒤 확인)');

        //.// 6) 취소
        const cancelIndex = await page.evaluate(() => {
            const btns = [...document.querySelectorAll('button[aria-label*="추가 작업"]')]
                .filter(b => b.getBoundingClientRect().width > 0);
            // 목록 행의 ⋯ 는 마지막 (경기 행들 다음)
            return btns.length - 1;
        });
        await clickOverflow(page, cancelIndex);
        const hasCancel = await bodyHas(page, '신청 취소');
        console.log('신청 행 ⋯ 에 취소 있음:', hasCancel);
        if (hasCancel) {
            await clickText(page, '신청 취소');
            await sleep(800);
            console.log('취소 확인 모달:', await bodyHas(page, '취소할까요?'));
            await page.screenshot({ path: 'b6-06-cancel-modal.png' });
            await clickText(page, '신청 취소');
            await sleep(2500);
            console.log('취소 토스트:', JSON.stringify(await toastText(page)));
        }
        await page.screenshot({ path: 'b6-07-after-cancel.png' });

        //.// 7) 모바일
        await page.setViewport({ width: 390, height: 844, isMobile: true, hasTouch: true });
        await page.goto(BASE + '/dashboard/team/results', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '경기');
        await sleep(1800);
        console.log('모바일 — 보이는 ⋯ 개수:', await visibleOverflowCount(page));
        await clickOverflow(page, 0);
        const sheet = await page.evaluate(() => {
            const cancel = [...document.querySelectorAll('button')]
                .find(b => b.innerText.trim() === '취소' && b.getBoundingClientRect().width > 0);
            if (!cancel) { return null; }
            const r = cancel.closest('div').getBoundingClientRect();
            return { bottomAligned: Math.abs(r.bottom - window.innerHeight) < 2, width: Math.round(r.width) };
        });
        console.log('모바일 바텀시트:', JSON.stringify(sheet));
        await page.screenshot({ path: 'b6-08-mobile-sheet.png' });

        await browser.disconnect();
    } catch (e) {
        console.error('FAILED:', e.message);
        process.exitCode = 1;
    } finally {
        edge.kill();
    }
})();
