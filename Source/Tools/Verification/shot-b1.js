// B1 검증 — 경기 결과 입력 → 저장 → 순위표 자동 갱신 (PC/모바일)
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9501;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-b1-' + Date.now();

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
        const el = [...document.querySelectorAll(g)].find(x => x.innerText.trim().includes(t) && x.offsetParent !== null);
        if (!el) { return false; }
        el.click();
        return true;
    }, text, tag);
    if (!ok) { throw new Error('not found: ' + text); }
    await sleep(400);
};

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
                body: JSON.stringify({ email: 'verify-teamadmin-0713@test.local', password: 'password123!' }),
            });
            return (await r.json())?.data?.accessToken ?? null;
        }, BASE);
        console.log('token:', token ? 'OK' : 'FAILED');
        await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);

        //.// PC — 경기 결과 섹션 → 결과 입력
        await page.goto(BASE + '/dashboard/team/results', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '경기 결과');
        await sleep(800);

        await clickText(page, '＋ 결과 입력');
        await ready(page, '경기 결과 입력');
        await page.screenshot({ path: 'b1-01-form-open.png' });
        console.log('form opened');

        // 빈 제출 → 인라인 오류만 (토스트 없어야 함)
        await clickText(page, '결과 저장');
        await sleep(600);
        const afterEmpty = await page.evaluate(() => ({
            inline: document.body.innerText.includes('상대 팀 이름을 입력해 주세요'),
            dateErr: document.body.innerText.includes('경기 날짜를 선택해 주세요'),
            toast: document.querySelector('[role=status]') !== null,
        }));
        console.log('빈 제출 →', JSON.stringify(afterEmpty));
        await page.screenshot({ path: 'b1-02-inline-errors.png' });

        // 캘린더 열기 — 미래 비활성 확인
        await page.evaluate(() => {
            const labels = [...document.querySelectorAll('label')];
            const dateLabel = labels.find(l => l.innerText.includes('경기 날짜'));
            dateLabel.parentElement.querySelector('button').click();
        });
        await sleep(500);
        await page.screenshot({ path: 'b1-03-calendar.png' });
        const cal = await page.evaluate(() => {
            const cells = [...document.querySelectorAll('button')].filter(b => /^\d+$/.test(b.innerText.trim()) && b.offsetParent !== null);
            return {
                cells: cells.length,
                disabled: cells.filter(c => c.disabled).length,
                hasToday: document.querySelector('.ring-teal') !== null,
                quick: document.body.innerText.includes('오늘') && document.body.innerText.includes('이번 주말'),
            };
        });
        console.log('캘린더:', JSON.stringify(cal));

        await clickText(page, '오늘');
        await sleep(400);

        // 시간
        await page.evaluate(() => {
            const labels = [...document.querySelectorAll('label')];
            labels.find(l => l.innerText.includes('경기 시각')).parentElement.querySelector('button').click();
        });
        await sleep(400);
        await page.screenshot({ path: 'b1-04-timelist.png' });
        const times = await page.evaluate(() =>
            [...document.querySelectorAll('button')].filter(b => /^\d{2}:\d{2}$/.test(b.innerText.trim())).length);
        console.log('시간 옵션 수(15분 단위):', times);
        await clickText(page, '15:00');

        // 나머지 입력 — placeholder로 필드를 찾아 실제 타이핑(Blazor @bind가 input 이벤트를 받아야 한다)
        // focus + keyboard.type — 실제 클릭은 오버레이 hit-test에 걸려 멈출 수 있다
        const typeInto = async (placeholder, value) => {
            const selector = `input[placeholder="${placeholder}"]`;
            await page.waitForSelector(selector, { timeout: 10000 });
            await page.focus(selector);
            await page.keyboard.type(value, { delay: 30 });
            await sleep(200);
        };

        await typeInto('강동 SC', 'B1검증상대FC');   // 상대 팀
        await typeInto('3', '4');                    // 우리 득점
        await typeInto('1', '1');                    // 상대 득점
        await sleep(300);

        // 대회 선택 — SelectField 트리거(라벨 "대회 · 리그" 블록 안의 버튼)를 연다
        await page.evaluate(() => {
            const label = [...document.querySelectorAll('label')].find(l => l.innerText.includes('대회 · 리그'));
            label.parentElement.querySelector('button').click();
        });
        await sleep(600);
        await page.screenshot({ path: 'b1-05-tournament-select.png' });

        // 열린 시트 안의 옵션만 고른다 (뒤 화면의 필터 칩과 구분)
        const picked = await page.evaluate(() => {
            const sheet = [...document.querySelectorAll('div')]
                .find(d => d.className.includes('z-[60]'));
            if (!sheet) { return 'SHEET_NOT_OPEN'; }
            const opts = [...sheet.querySelectorAll('button')];
            if (opts.length === 0) { return 'NO_OPTIONS'; }
            // 순위표 갱신을 보려면 리그를 고른다 (컵·스플릿은 조 입력 전까지 순위표 스코프 없음)
            const league = opts.find(o => o.innerText.includes('리그') && !o.innerText.includes('스플릿')) ?? opts[0];
            const name = league.innerText.trim().split('\n')[0];
            league.click();
            return name;
        });
        console.log('선택한 대회:', picked);
        await sleep(600);

        // 득점자 칩 — 득점자 블록 안에서만 (같은 선수 2번 = 2골)
        const scorer = await page.evaluate(() => {
            const label = [...document.querySelectorAll('label')].find(l => l.innerText.includes('득점자'));
            if (!label) { return 'NO_SCORER_BLOCK'; }
            const chips = [...label.parentElement.querySelectorAll('button')]
                .filter(b => !b.innerText.includes('비우기'));
            if (chips.length === 0) { return 'NO_CHIPS'; }
            const name = chips[0].innerText.trim();
            chips[0].click();
            chips[0].click();
            return name;
        });
        console.log('득점자 칩:', scorer);
        await sleep(400);
        await page.screenshot({ path: 'b1-06-filled.png' });

        await clickText(page, '결과 저장');
        await sleep(2500);
        const saved = await page.evaluate(() => ({
            toast: document.querySelector('[role=status]')?.innerText ?? null,
            formClosed: !document.body.innerText.includes('경기 결과 입력'),
            listed: document.body.innerText.includes('B1검증상대FC'),
        }));
        console.log('저장 후:', JSON.stringify(saved));
        await page.screenshot({ path: 'b1-07-saved.png' });

        // 순위표 자동 갱신 — 공개 경기기록(Records)에서 확인 (같은 순위표를 읽는 화면)
        const rank = await page.evaluate(async base => {
            const r = await fetch(base + '/api/soccer/team/me/matches?season=' + new Date().getFullYear(), {
                headers: { Authorization: 'Bearer ' + localStorage.getItem('pg.accessToken') },
            });
            const j = await r.json();
            return j?.data?.leagueRank ?? null;
        }, BASE);
        console.log('저장 후 리그 순위(API):', rank);

        //.// 모바일 — 폼이 시트로 뜨는지
        await page.setViewport({ width: 390, height: 860, isMobile: true });
        await page.goto(BASE + '/dashboard/team/results', { waitUntil: 'networkidle0' });
        await ready(page, '경기');
        await sleep(800);
        await clickText(page, '＋ 결과 입력');
        await ready(page, '경기 결과 입력');
        await sleep(400);
        await page.screenshot({ path: 'b1-08-mobile-form.png' });
        await page.evaluate(() => {
            const labels = [...document.querySelectorAll('label')];
            labels.find(l => l.innerText.includes('경기 날짜')).parentElement.querySelector('button').click();
        });
        await sleep(600);
        await page.screenshot({ path: 'b1-09-mobile-calendar-sheet.png' });
        const sheet = await page.evaluate(() => {
            const cells = [...document.querySelectorAll('button')].filter(b => /^\d+$/.test(b.innerText.trim()) && b.offsetParent !== null);
            return {
                cellHeight: cells.length ? Math.round(cells[0].getBoundingClientRect().height) : 0,
                confirmLabel: [...document.querySelectorAll('button')].map(b => b.innerText.trim()).find(t => t.includes('선택')) ?? null,
            };
        });
        console.log('모바일 시트:', JSON.stringify(sheet));

        await browser.disconnect();
    } catch (e) {
        console.error('FAILED:', e.message);
        process.exitCode = 1;
    } finally {
        edge.kill();
    }
})();
