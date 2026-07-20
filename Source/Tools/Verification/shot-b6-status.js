// B6 상태 3종 렌더 확인 — sql-b6.sql로 심사 결과를 심은 뒤 실행한다.
// (반영·반려는 주최측이 만드는 상태라 PlayGround에서는 만들 수 없다 — 설계 결정 6·7)
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9525;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-b6s-' + Date.now();
const sleep = ms => new Promise(r => setTimeout(r, ms));

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});

(async () => {
    const edge = spawn(EDGE, ['--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
        `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`, 'about:blank'], { stdio: 'ignore' });

    try {
        const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });
        const page = await browser.newPage();
        page.on('pageerror', e => console.log('PAGE ERROR:', e.message));
        await page.setViewport({ width: 1440, height: 1300 });
        await page.goto(BASE, { waitUntil: 'networkidle0', timeout: 60000 });

        const token = await page.evaluate(async base => {
            const r = await fetch(base + '/api/auth/login/email', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: 'verify-u15-1@test.local', password: 'password123!' }),
            });
            return (await r.json())?.data?.accessToken ?? null;
        }, BASE);
        await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);

        await page.goto(BASE + '/dashboard/team/results', { waitUntil: 'networkidle0', timeout: 60000 });
        await page.waitForFunction(() => document.body.innerText.includes('경기 결과'), { polling: 300, timeout: 40000 });
        await sleep(2500);

        const result = await page.evaluate(() => {
            const text = document.body.innerText;
            return {
                접수: text.includes('접수'),
                반영: text.includes('반영'),
                반려: text.includes('반려'),
                반려사유라벨: text.includes('반려 사유'),
                반려사유본문: text.includes('경기 감독관 기록지와 대조한'),
                건수: (text.match(/(\d+)건/) || [])[0] ?? null,
            };
        });
        console.log('상태 3종 + 반려 사유:', JSON.stringify(result, null, 1));

        // 접수 행에만 ⋯(취소)가 붙어야 한다 — 심사가 끝난 건은 손댈 수 없다
        const cancellable = await page.evaluate(() =>
            [...document.querySelectorAll('button[aria-label*="추가 작업"]')]
                .filter(b => b.getBoundingClientRect().width > 0).length);
        console.log('보이는 ⋯ 개수 (경기 4 + 접수 신청 1 = 5 기대):', cancellable);

        await page.screenshot({ path: 'b6-09-statuses.png' });
        await browser.disconnect();
    } catch (e) {
        console.error('FAILED:', e.message);
        process.exitCode = 1;
    } finally {
        edge.kill();
    }
})();
