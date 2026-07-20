// B4 UI 검증 — 보호자(뱃지 보임) / 제3자·본인관리(뱃지 없음) / 팀 관리자(로스터 카드 뱃지+다이얼로그)
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9514;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-b4-' + Date.now();

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

async function loginAs(page, email) {
    const token = await page.evaluate(async (base, mail) => {
        const r = await fetch(base + '/api/auth/login/email', {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: mail, password: 'password123!' }),
        });
        return (await r.json())?.data?.accessToken ?? null;
    }, BASE, email);
    await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);
    return token;
}

// 카메라 뱃지 = aria-label에 "사진 변경"이 들어간 버튼
const countBadges = page => page.evaluate(() =>
    [...document.querySelectorAll('button[aria-label]')]
        .filter(b => b.getAttribute('aria-label').includes('사진') && b.offsetParent !== null).length);

(async () => {
    const edge = spawn(EDGE, ['--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
        `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`, 'about:blank'], { stdio: 'ignore' });

    try {
        const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });
        const page = await browser.newPage();
        page.on('pageerror', e => console.log('PAGE ERROR:', e.message));
        await page.setViewport({ width: 1440, height: 1000 });
        await page.goto(BASE, { waitUntil: 'networkidle0', timeout: 60000 });

        //.// 1) 보호자 — 선수 대시보드 프로필
        await loginAs(page, 'verify-player-u15@test.local');
        await page.goto(BASE + '/dashboard/player', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '프로필');
        await sleep(1200);
        const guardian = await page.evaluate(() => ({
            canEdit: !!document.querySelector('button[aria-label*="사진"]'),
            initials: !!document.querySelector('span.bg-teal'),
        }));
        console.log('보호자 프로필 — 카메라 뱃지:', await countBadges(page), JSON.stringify(guardian));
        await page.screenshot({ path: 'b4-01-guardian-profile.png' });

        //.// 2) 본인 관리(보호자 아님) — 뱃지가 없어야 한다
        await loginAs(page, 'verify-player-u12@test.local');
        await page.goto(BASE + '/dashboard/player', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '프로필');
        await sleep(1200);
        const selfManaged = await page.evaluate(async base => {
            const t = localStorage.getItem('pg.accessToken');
            const r = await fetch(base + '/api/soccer/player/me/info', { headers: { Authorization: 'Bearer ' + t } });
            const j = await r.json();
            return { canEditPhoto: j?.data?.profile?.canEditPhoto, name: j?.data?.profile?.name };
        }, BASE);
        console.log('본인관리 프로필 — canEditPhoto:', JSON.stringify(selfManaged), '카메라 뱃지:', await countBadges(page));
        await page.screenshot({ path: 'b4-02-selfmanaged-profile.png' });

        //.// 3) 팀 관리자 — 로스터 카드 뷰
        await loginAs(page, 'verify-u15-1@test.local');
        await page.goto(BASE + '/dashboard/team/roster', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '선수단');
        await sleep(1500);
        await page.evaluate(() => [...document.querySelectorAll('button')].find(b => b.innerText.trim() === '카드')?.click());
        await sleep(900);
        console.log('팀 관리자 로스터(카드) — 카메라 뱃지:', await countBadges(page));
        await page.screenshot({ path: 'b4-03-admin-roster-cards.png' });

        // 다이얼로그 열기
        await page.evaluate(() => document.querySelector('button[aria-label*="사진 변경"]')?.click());
        await sleep(900);
        const dialog = await page.evaluate(() => ({
            open: document.body.innerText.includes('선수 사진'),
            hint: (document.body.innerText.match(/[^\n]*선수의 사진을 등록해요[^\n]*/) || [null])[0],
        }));
        console.log('사진 다이얼로그:', JSON.stringify(dialog));
        await page.screenshot({ path: 'b4-04-photo-dialog.png' });

        //.// 4) 공개 팀 홈 — 방문자에겐 업로드 경로가 없어야 한다
        await page.evaluate(() => localStorage.removeItem('pg.accessToken'));
        await page.goto(BASE + '/team/' + encodeURIComponent('광주광주FCU15') + '/roster', { waitUntil: 'networkidle0', timeout: 60000 });
        await sleep(2000);
        console.log('공개 홈 선수단 — 카메라 뱃지(0이어야 함):', await countBadges(page),
            '/ 이니셜 아바타:', await page.evaluate(() => document.querySelectorAll('span.bg-teal').length));
        await page.screenshot({ path: 'b4-05-public-roster.png' });

        //.// 5) 모바일 — 보호자 프로필
        await page.setViewport({ width: 390, height: 844, isMobile: true, hasTouch: true });
        await loginAs(page, 'verify-player-u15@test.local');
        await page.goto(BASE + '/dashboard/player', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '프로필');
        await sleep(1500);
        console.log('모바일 보호자 프로필 — 카메라 뱃지:', await countBadges(page));
        await page.screenshot({ path: 'b4-06-mobile-guardian.png' });

        await browser.disconnect();
    } catch (e) {
        console.error('FAILED:', e.message);
        process.exitCode = 1;
    } finally {
        edge.kill();
    }
})();
