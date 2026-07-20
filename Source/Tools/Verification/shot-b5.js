// B5 UI 검증 — 친선 행 마킹 / 세그먼트 URL 동기화 / 요약은 공식만 / 입력 화면 정리
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9521;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-b5-' + Date.now();
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
        const el = [...document.querySelectorAll('button, a')]
            .find(x => x.innerText?.trim() === t && x.getBoundingClientRect().width > 0);
        if (!el) { return false; }
        el.click();
        return true;
    }, text);
    if (!ok) { throw new Error('not found: ' + text); }
    await sleep(800);
};

const bodyHas = (page, t) => page.evaluate(x => document.body.innerText.includes(x), t);

// 친선 행 = 점선 보더가 실제 적용됐는지 계산된 스타일로 본다
const friendlyRowStyle = page => page.evaluate(() => {
    const el = [...document.querySelectorAll('div')]
        .find(d => d.className && typeof d.className === 'string'
            && d.className.includes('border-dashed') && d.getBoundingClientRect().width > 0);
    if (!el) { return null; }
    const cs = getComputedStyle(el);
    return { borderStyle: cs.borderTopStyle, bg: cs.backgroundColor };
});

(async () => {
    const edge = spawn(EDGE, ['--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
        `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`, 'about:blank'], { stdio: 'ignore' });

    try {
        const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });
        const page = await browser.newPage();
        page.on('pageerror', e => console.log('PAGE ERROR:', e.message));
        await page.setViewport({ width: 1440, height: 1100 });

        await page.goto(BASE, { waitUntil: 'networkidle0', timeout: 60000 });
        const token = await page.evaluate(async base => {
            const r = await fetch(base + '/api/auth/login/email', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: 'verify-teamadmin-0713@test.local', password: 'password123!' }),
            });
            return (await r.json())?.data?.accessToken ?? null;
        }, BASE);
        await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
        console.log('token:', token ? 'OK' : 'FAILED');

        //.// 1) 팀 대시보드 경기 결과 — 검증fc는 친선 2건뿐
        await page.goto(BASE + '/dashboard/team/results', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '경기 결과');
        await sleep(1800);

        console.log('요약 라벨(공식 명시):', JSON.stringify({
            record: await bodyHas(page, '시즌 (공식)'),
            goalsFor: await bodyHas(page, '득점 (공식)'),
        }));
        console.log('친선 별도 안내:', await bodyHas(page, '친선경기 2경기는 별도예요'));
        console.log('친선 라벨 노출:', await bodyHas(page, '친선경기'));
        console.log('친선 행 스타일:', JSON.stringify(await friendlyRowStyle(page)));

        // 공식만 집계 → 검증fc는 공식 경기가 없으므로 0승 0무 0패여야 한다
        const summary = await page.evaluate(() => {
            const m = document.body.innerText.match(/(\d+)승 (\d+)무 (\d+)패/);
            return m ? m[0] : null;
        });
        console.log('시즌 전적(공식만 → 0승 0무 0패 기대):', summary);
        await page.screenshot({ path: 'b5-01-dashboard-results.png' });

        //.// 2) 세그먼트 URL 동기화
        console.log('세그먼트 3종:', JSON.stringify({
            all: await bodyHas(page, '전체'),
            official: await bodyHas(page, '공식'),
            friendly: await bodyHas(page, '친선경기'),
        }));

        await clickText(page, '공식');
        console.log('공식 선택 후 URL:', await page.evaluate(() => location.search));
        console.log('  → 목록 비었는지(공식 0건):', await bodyHas(page, 'vs') === false || !(await bodyHas(page, '강동 SC')));
        await page.screenshot({ path: 'b5-02-segment-official.png' });

        await clickText(page, '친선경기');
        console.log('친선 선택 후 URL:', await page.evaluate(() => location.search));
        console.log('  → 친선 경기 보임:', await bodyHas(page, '강동 SC'));

        // 새로고침해도 유지되는가
        // 고정 sleep으로 기다리면 지연 로드가 늦은 날 거짓 실패가 난다 — 실제 내용이 뜰 때까지 기다린다
        await page.reload({ waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '경기 결과');
        const restored = await ready(page, '강동 SC', 15000).then(() => true).catch(() => false);
        console.log('새로고침 후 URL 유지:', await page.evaluate(() => location.search),
            '/ 친선 목록 유지:', restored);
        await page.screenshot({ path: 'b5-03-segment-reload.png' });

        //.// 3) 입력 화면 정리
        await clickText(page, '＋ 결과 입력');
        await sleep(1200);
        console.log('입력 화면:', JSON.stringify({
            title: await bodyHas(page, '친선경기 결과 입력'),
            subtitle: await bodyHas(page, '대회·리그 공식 기록은 주최측이 입력해요'),
            tournamentField: await bodyHas(page, '대회 · 리그'),
        }), '(tournamentField는 false여야 함)');
        await page.screenshot({ path: 'b5-04-form.png' });

        await browser.disconnect();
    } catch (e) {
        console.error('FAILED:', e.message);
        process.exitCode = 1;
    } finally {
        edge.kill();
    }
})();
