// B3 UI 검증 — 빈 상태 → 추가 → 소멸 → 수정 → ⋯ → 삭제(모달) → 실행취소
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9517;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-b3-' + Date.now();

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

const clickText = async (page, text, tag = 'button') => {
    const ok = await page.evaluate((t, g) => {
        const el = [...document.querySelectorAll(g)].find(x => x.innerText?.trim() === t && x.offsetParent !== null);
        if (!el) { return false; }
        el.click();
        return true;
    }, text, tag);
    if (!ok) { throw new Error('not found: ' + text); }
    await sleep(500);
};

// 라벨로 입력칸을 찾아 값을 넣는다 (A1 필드는 label + input 구조)
const fill = async (page, label, value) => {
    const ok = await page.evaluate((lb, v) => {
        // label[for] ↔ input#id 로 찾는다 (A1 필드는 레이블과 입력이 형제가 아니다)
        const field = [...document.querySelectorAll('label[for]')]
            .find(l => l.innerText.trim().startsWith(lb) && l.offsetParent !== null);
        if (!field) { return false; }
        const input = document.getElementById(field.getAttribute('for'));
        if (!input) { return false; }
        const proto = input.tagName === 'TEXTAREA' ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype;
        Object.getOwnPropertyDescriptor(proto, 'value').set.call(input, v);
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
        return true;
    }, label, value);
    if (!ok) { throw new Error('field not found: ' + label); }
    await sleep(250);
};

// PC·모바일 트리가 둘 다 렌더되므로(한쪽은 hidden) 실제로 보이는 ⋯만 누른다
const clickOverflow = async page => {
    const ok = await page.evaluate(() => {
        const btn = [...document.querySelectorAll('button[aria-label*="추가 작업"]')]
            .find(b => b.getBoundingClientRect().width > 0);
        if (!btn) { return false; }
        btn.click();
        return true;
    });
    if (!ok) { throw new Error('visible overflow trigger not found'); }
    await sleep(700);
};

const bodyHas = (page, t) => page.evaluate(x => document.body.innerText.includes(x), t);
const toastText = page => page.evaluate(() => document.querySelector('[role=status]')?.innerText ?? null);

(async () => {
    const edge = spawn(EDGE, ['--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
        `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`, 'about:blank'], { stdio: 'ignore' });

    try {
        const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });
        const page = await browser.newPage();
        page.on('pageerror', e => console.log('PAGE ERROR:', e.message));
        await page.setViewport({ width: 1440, height: 1000 });

        await page.goto(BASE, { waitUntil: 'networkidle0', timeout: 60000 });
        const token = await page.evaluate(async base => {
            const r = await fetch(base + '/api/auth/login/email', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: 'verify-player-u12@test.local', password: 'password123!' }),
            });
            return (await r.json())?.data?.accessToken ?? null;
        }, BASE);
        await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
        console.log('token:', token ? 'OK' : 'FAILED');

        //.// 포트폴리오 — 시드가 0건이라 빈 상태부터 시작
        await page.goto(BASE + '/dashboard/player/portfolio', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '포트폴리오');
        await sleep(1500);
        console.log('빈 상태 표시:', await bodyHas(page, '아직 등록된 영상이 없어요'));
        await page.screenshot({ path: 'b3-01-portfolio-empty.png' });

        //.// 추가
        await clickText(page, '＋ 영상 추가');
        await ready(page, '영상 추가');
        console.log('다이얼로그 열림 / 첫 영상 안내:', await bodyHas(page, '첫 영상이라 자동으로 대표 영상이 돼요'));

        // 잘못된 링크 → 인라인 오류(토스트 아님)
        await fill(page, '유튜브 링크', 'https://vimeo.com/12345');
        await fill(page, '제목', 'B3 UI 영상');
        await clickText(page, '저장');
        await sleep(700);
        console.log('잘못된 링크:', JSON.stringify({
            inline: await bodyHas(page, '유튜브 영상 링크가 아니에요'),
            toast: (await toastText(page)) !== null,
        }));
        await page.screenshot({ path: 'b3-02-invalid-url.png' });

        // 올바른 링크 → 썸네일 미리보기
        await fill(page, '유튜브 링크', 'https://youtu.be/dQw4w9WgXcQ');
        await sleep(800);
        const preview = await page.evaluate(() => {
            const img = [...document.querySelectorAll('img')].find(i => i.src.includes('img.youtube.com'));
            return img?.getAttribute('src') ?? null;
        });
        console.log('썸네일 미리보기:', preview);
        await page.screenshot({ path: 'b3-03-thumb-preview.png' });

        await clickText(page, '저장');
        await sleep(2500);
        console.log('저장 토스트:', JSON.stringify(await toastText(page)));
        console.log('빈 상태 사라짐:', !(await bodyHas(page, '아직 등록된 영상이 없어요')));
        console.log('대표 영상 뱃지:', await bodyHas(page, '대표 영상'));
        await page.screenshot({ path: 'b3-04-portfolio-added.png' });

        //.// ⋯ 메뉴 → 수정
        await clickOverflow(page);
        console.log('⋯ 메뉴 열림:', JSON.stringify({
            edit: await bodyHas(page, '영상 수정'),
            del: await bodyHas(page, '영상 삭제'),
        }));
        await page.screenshot({ path: 'b3-05-overflow-menu.png' });

        await clickText(page, '영상 수정');
        await sleep(800);
        await fill(page, '제목', 'B3 UI 영상 (수정)');
        await clickText(page, '저장');
        await sleep(2500);
        console.log('수정 반영:', await bodyHas(page, 'B3 UI 영상 (수정)'), '/ 토스트:', JSON.stringify(await toastText(page)));

        //.// ⋯ → 삭제 → 확인 모달 → 실행취소
        await clickOverflow(page);
        await clickText(page, '영상 삭제');
        await sleep(800);
        console.log('확인 모달:', JSON.stringify({
            open: await bodyHas(page, '삭제할까요?'),
            primaryNotice: await bodyHas(page, '대표 영상이라'),
        }));
        await page.screenshot({ path: 'b3-06-confirm-modal.png' });

        await clickText(page, '삭제');
        await sleep(2500);
        const afterDelete = await toastText(page);
        console.log('삭제 후 토스트:', JSON.stringify(afterDelete));
        console.log('빈 상태 복귀:', await bodyHas(page, '아직 등록된 영상이 없어요'));
        await page.screenshot({ path: 'b3-07-deleted.png' });

        await clickText(page, '실행취소', 'button');
        await sleep(2500);
        console.log('실행취소 후 목록 복원:', await bodyHas(page, 'B3 UI 영상 (수정)'));
        console.log('실행취소 토스트:', JSON.stringify(await toastText(page)));
        await page.screenshot({ path: 'b3-08-restored.png' });

        //.// 커리어 — ⋯ 메뉴 + 연·월 입력
        await page.goto(BASE + '/dashboard/player/career', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '커리어');
        await sleep(1500);
        await clickText(page, '＋ 이력 추가');
        await ready(page, '이력 추가');
        await fill(page, '팀 이름', 'B3 UI FC');
        await fill(page, '시작', '202403');
        const startVal = await page.evaluate(() => {
            const l = [...document.querySelectorAll('label[for]')].find(x => x.innerText.trim().startsWith('시작'));
            return l ? document.getElementById(l.getAttribute('for'))?.value ?? null : null;
        });
        console.log('연·월 자동 포맷:', JSON.stringify(startVal), '(2024. 03 기대)');
        console.log('캘린더 미노출(네이티브 date input 0건):',
            await page.evaluate(() => document.querySelectorAll('input[type=date]').length));
        await page.screenshot({ path: 'b3-09-career-form.png' });

        await clickText(page, '저장');
        await sleep(2500);
        console.log('커리어 추가:', await bodyHas(page, 'B3 UI FC'), '/ 토스트:', JSON.stringify(await toastText(page)));
        await page.screenshot({ path: 'b3-10-career-added.png' });

        //.// 모바일 — ⋯ 가 바텀시트로 뜨는지
        await page.setViewport({ width: 390, height: 844, isMobile: true, hasTouch: true });
        await page.goto(BASE + '/dashboard/player/career', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '커리어');
        await sleep(1800);
        await clickOverflow(page);
        const sheet = await page.evaluate(() => {
            const cancel = [...document.querySelectorAll('button')].find(b => b.innerText.trim() === '취소' && b.offsetParent !== null);
            if (!cancel) { return null; }
            const card = cancel.closest('div');
            const r = card.getBoundingClientRect();
            return { bottomAligned: Math.abs(r.bottom - window.innerHeight) < 2, width: Math.round(r.width) };
        });
        console.log('모바일 바텀시트:', JSON.stringify(sheet), '(하단 정렬 + 전체 폭 기대)');
        await page.screenshot({ path: 'b3-11-mobile-sheet.png' });

        await browser.disconnect();
    } catch (e) {
        console.error('FAILED:', e.message);
        process.exitCode = 1;
    } finally {
        edge.kill();
    }
})();
