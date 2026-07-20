// 자녀 전환 검증 — sql-twochildren.sql 실행 후 돌린다.
// 확인: 전환 컨트롤 노출(2명일 때만) / URL 동기화 / 섹션 데이터가 실제로 갈아끼워지는지
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9527;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-cs-' + Date.now();
const sleep = ms => new Promise(r => setTimeout(r, ms));

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});

const ready = (page, text, timeout = 30000) =>
    page.waitForFunction(t => document.body.innerText.includes(t), { timeout, polling: 300 }, text);

const bodyHas = (page, t) => page.evaluate(x => document.body.innerText.includes(x), t);

const clickChild = async (page, name) => {
    const ok = await page.evaluate(n => {
        const btn = [...document.querySelectorAll('button[role=tab]')]
            .find(b => b.innerText.trim() === n && b.getBoundingClientRect().width > 0);
        if (!btn) { return false; }
        btn.click();
        return true;
    }, name);
    if (!ok) { throw new Error('child tab not found: ' + name); }
    await sleep(1500);
};

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
        await page.setViewport({ width: 1440, height: 1000 });
        await page.goto(BASE, { waitUntil: 'networkidle0', timeout: 60000 });

        //.// 자녀 2명 계정
        await loginAs(page, 'verify-player-u15@test.local');
        await page.goto(BASE + '/dashboard/player/career', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '커리어');
        await sleep(2000);

        console.log('전환 컨트롤 노출:', await bodyHas(page, '관리 중인 자녀'));
        console.log('두 자녀 탭:', JSON.stringify({
            first: await bodyHas(page, '김정현'),
            second: await bodyHas(page, '김서연'),
        }));
        console.log('첫째 커리어 표시:', await bodyHas(page, '첫째전용FC'),
            '/ 둘째 것 섞임(false여야):', await bodyHas(page, '둘째전용FC'));
        await page.screenshot({ path: 'cs-01-first-child.png' });

        //.// 둘째로 전환
        await clickChild(page, '김서연');
        console.log('전환 후 URL:', await page.evaluate(() => location.search));
        console.log('둘째 커리어 표시:', await bodyHas(page, '둘째전용FC'),
            '/ 첫째 것 잔존(false여야):', await bodyHas(page, '첫째전용FC'));
        await page.screenshot({ path: 'cs-02-second-child.png' });

        //.// 새로고침해도 유지
        await page.reload({ waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '커리어');
        const kept = await ready(page, '둘째전용FC', 15000).then(() => true).catch(() => false);
        console.log('새로고침 후 URL 유지:', await page.evaluate(() => location.search), '/ 둘째 유지:', kept);

        //.// 프로필 섹션도 갈아끼워지는지
        await page.goto(BASE + '/dashboard/player?playerId=E0000000-0000-0000-0000-00000000C11D',
            { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '프로필');
        await sleep(2000);
        // 전환 탭 자체가 두 자녀 이름을 담고 있으므로 탭을 뺀 본문만 본다
        console.log('프로필 = 둘째:', await bodyHas(page, '김서연'), '/ 첫째 데이터 잔존(false여야):',
            await page.evaluate(() => {
                const main = document.querySelector('main');
                if (!main) { return false; }
                const clone = main.cloneNode(true);
                clone.querySelectorAll('[role=tablist], [role=tablist] ~ *').forEach(el => el.remove());
                const tabs = clone.querySelector('div');
                if (tabs) { tabs.remove(); }
                return clone.innerText.includes('김정현');
            }));
        await page.screenshot({ path: 'cs-03-profile-second.png' });

        //.// 자녀 1명 계정 — 전환 컨트롤이 없어야 한다
        await loginAs(page, 'verify-player-u12@test.local');
        await page.goto(BASE + '/dashboard/player/career', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '커리어');
        await sleep(1800);
        console.log('자녀 1명 — 전환 컨트롤 노출(false여야):', await bodyHas(page, '관리 중인 자녀'));
        await page.screenshot({ path: 'cs-04-single-child.png' });

        await browser.disconnect();
    } catch (e) {
        console.error('FAILED:', e.message);
        process.exitCode = 1;
    } finally {
        edge.kill();
    }
})();
