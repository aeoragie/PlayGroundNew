// A3 검증 — 스켈레톤(API 응답을 의도적으로 지연) + 빈 상태(EmptyFC 계정)
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9479;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-st-' + Date.now();

const sleep = ms => new Promise(r => setTimeout(r, ms));

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});

async function login(page, email) {
    return await page.evaluate(async (base, mail) => {
        const r = await fetch(base + '/api/auth/login/email', {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: mail, password: 'password123!' }),
        });
        const j = await r.json();
        return j?.data?.accessToken ?? j?.Data?.AccessToken ?? null;
    }, BASE, email);
}

const clickNav = (page, href) => page.evaluate(h => document.querySelector(`a[href="${h}"]`).click(), href);

// 가로챈 API는 기본적으로 그대로 통과시키고, rule에 걸린 경로만 붙잡아 둔다.
// (핸들러가 하나도 없으면 모든 /api/ 요청이 영구히 멈추므로 항상 등록해 둔다)
let gRule = null;

function installFetchHandler(cdp) {
    cdp.on('Fetch.requestPaused', async ({ requestId, request }) => {
        if (gRule && request.url.includes(gRule.match)) {
            await sleep(gRule.delayMs);
        }
        try { await cdp.send('Fetch.continueRequest', { requestId }); } catch { /* 이미 종료 */ }
    });
}

// 지정한 API 경로의 응답만 delayMs 만큼 지연 — 스켈레톤 구간을 결정적으로 만든다
function delayApi(match, delayMs) {
    gRule = { match, delayMs };
    return () => { gRule = null; };
}

(async () => {
    const edge = spawn(EDGE, ['--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
        `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`, 'about:blank'], { stdio: 'ignore' });

    try {
        const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });
        const page = await browser.newPage();
        page.on('pageerror', e => console.log('PAGE ERROR:', e.message));
        await page.setViewport({ width: 1440, height: 1000 });

        await page.goto(BASE, { waitUntil: 'networkidle0', timeout: 60000 });
        const token = await login(page, 'verify-empty-0714@test.local');
        console.log('token(empty):', token ? 'OK' : 'FAILED');
        if (!token) { throw new Error('login failed'); }
        await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);

        const cdp = await page.target().createCDPSession();
        await cdp.send('Network.enable');
        await cdp.send('Network.setCacheDisabled', { cacheDisabled: true });
        await cdp.send('Fetch.enable', { patterns: [{ urlPattern: '*/api/*' }] });
        installFetchHandler(cdp);

        //.// 1) 스켈레톤 + 3초 문구 — 로스터 API를 5초 붙잡는다
        await page.goto(BASE + '/dashboard/team', { waitUntil: 'networkidle0', timeout: 60000 });
        await page.waitForSelector('a[href="/dashboard/team/roster"]', { timeout: 30000, polling: 300 });

        let stop = delayApi('/roster', 5000);
        await clickNav(page, '/dashboard/team/roster');

        // 200ms 지연 규칙: 클릭 직후엔 스켈레톤이 없어야 한다
        await sleep(120);
        console.log('at 120ms — skeleton drawn?', await page.evaluate(() => document.querySelector('.animate-shimmer') !== null));

        await page.waitForFunction(() => document.querySelector('.animate-shimmer') !== null, { timeout: 20000, polling: 300 });
        console.log('skeleton visible: true');
        await page.screenshot({ path: 'st-01-skeleton-roster.png' });

        await sleep(3300);
        const slow = await page.evaluate(() => document.body.innerText.includes('불러오는 중이에요'));
        console.log('slow notice(3s):', slow);
        await page.screenshot({ path: 'st-02-skeleton-slow.png' });
        stop();

        await page.waitForFunction(() => !document.querySelector('.animate-shimmer'), { timeout: 30000, polling: 300 });

        //.// 2) 빈 상태 (EmptyFC — 선수 0명 / 경기 0건 / 영상 0건)
        const shots = [
            ['/dashboard/team/roster', 'st-03-empty-roster.png', '아직 등록된 선수가 없어요'],
            ['/dashboard/team/results', 'st-04-empty-results.png', '아직 등록된 경기 결과가 없어요'],
            ['/dashboard/team/videos', 'st-05-empty-videos.png', '아직 등록된 영상이 없어요'],
        ];
        for (const [url, file, expect] of shots) {
            await page.goto(BASE + url, { waitUntil: 'networkidle0', timeout: 60000 });
            await page.waitForFunction(t => document.body.innerText.includes(t), { timeout: 40000, polling: 300 }, expect);
            await sleep(300);
            await page.screenshot({ path: file });
            console.log('empty OK:', url);
        }

        //.// 3) 모바일 빈 상태
        await page.setViewport({ width: 390, height: 860, isMobile: true });
        await page.goto(BASE + '/dashboard/team/roster', { waitUntil: 'networkidle0' });
        await page.waitForFunction(() => document.body.innerText.includes('아직 등록된 선수가 없어요'), { timeout: 40000, polling: 300 });
        await sleep(300);
        await page.screenshot({ path: 'st-06-empty-roster-mobile.png' });
        console.log('mobile OK');

        //.// 4) 레이아웃 점프 — 검증fc(데이터 있음) 결과 섹션 스켈레톤 → 실물
        await page.setViewport({ width: 1440, height: 1000 });
        await page.goto(BASE, { waitUntil: 'networkidle0' });
        const t2 = await login(page, 'verify-teamadmin-0713@test.local');
        console.log('token(verifyfc):', t2 ? 'OK' : 'FAILED');
        await page.evaluate(t => localStorage.setItem('pg.accessToken', t), t2);

        await page.goto(BASE + '/dashboard/team', { waitUntil: 'networkidle0', timeout: 60000 });
        await page.waitForSelector('a[href="/dashboard/team/results"]', { timeout: 30000, polling: 300 });

        stop = delayApi('/matches', 4000);
        await clickNav(page, '/dashboard/team/results');
        await page.waitForFunction(() => document.querySelector('.animate-shimmer') !== null, { timeout: 20000, polling: 300 });
        const skTop = await page.evaluate(() =>
            Math.round(document.querySelector('main > div').getBoundingClientRect().top));
        await page.screenshot({ path: 'st-07-skeleton-results.png' });
        stop();

        await page.waitForFunction(() => !document.querySelector('.animate-shimmer'), { timeout: 30000, polling: 300 });
        await sleep(400);
        const realTop = await page.evaluate(() =>
            Math.round(document.querySelector('main > div').getBoundingClientRect().top));
        console.log('section top skeleton/real:', skTop, realTop, skTop === realTop ? '(no jump)' : '(JUMP)');
        await page.screenshot({ path: 'st-08-results-loaded.png' });

        //.// 5) 경기기록 목록
        await page.goto(BASE + '/records', { waitUntil: 'networkidle0' });
        await sleep(600);
        await page.screenshot({ path: 'st-09-records.png' });
        console.log('records OK');

        await browser.disconnect();
    } catch (e) {
        console.error('FAILED:', e.message);
        process.exitCode = 1;
    } finally {
        edge.kill();
    }
})();
