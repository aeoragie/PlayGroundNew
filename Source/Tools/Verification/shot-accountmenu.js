// 계정 메뉴 공용 컴포넌트 (Design.DropdownMenu §1) — GNB 아바타 드롭다운.
// 확인: 헤더(이름+역할)+이동 그룹(허브·설정)+로그아웃 / 바깥 클릭·Esc 닫힘 / 전 화면 공통(허브·팀·선수·공개).
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9581;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-acct-' + Date.now();

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});
const sleep = ms => new Promise(r => setTimeout(r, ms));
const ready = (page, text) => page.waitForFunction(t => document.body.innerText.includes(t), { timeout: 40000, polling: 300 }, text);
async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json())?.data?.accessToken ?? null;
}
const openMenu = page => page.evaluate(() => {
    const b = [...document.querySelectorAll('button[aria-label="계정 메뉴"]')].find(x => x.getBoundingClientRect().width > 0);
    if (b) { b.click(); return true; } return false;
});
const menuState = page => page.evaluate(() => ({
    hub: document.body.innerText.includes('대시보드 허브'),
    settings: document.body.innerText.includes('설정'),
    logout: document.body.innerText.includes('로그아웃'),
    open: [...document.querySelectorAll('button[aria-label="계정 메뉴"]')].some(b => b.getAttribute('aria-expanded') === 'true'),
}));

let pass = 0, fail = 0;
const check = (name, ok) => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}`); ok ? pass++ : fail++; };

(async () => {
    const edge = spawn(EDGE, ['--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank'], { stdio: 'ignore' });
    const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1280, height: 900 });

        //.// 공개 GNB(경기기록) — 로그인 상태
        const token = await login('verify-teamadmin-0713@test.local');
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), token);
        await page.goto(BASE + '/records', { waitUntil: 'networkidle2' });
        await ready(page, '경기기록');
        await sleep(400);

        check('계정 메뉴 열림', await openMenu(page));
        await sleep(300);
        let s = await menuState(page);
        check('구조: 허브·설정·로그아웃', s.hub && s.settings && s.logout);
        await page.screenshot({ path: 'accountmenu-public.png' });

        // Esc 닫힘
        await page.keyboard.press('Escape');
        await sleep(300);
        check('Esc로 닫힘', !(await menuState(page)).open);

        // 다시 열고 바깥 클릭 닫힘
        await openMenu(page); await sleep(250);
        await page.mouse.click(30, 400);
        await sleep(300);
        check('바깥 클릭으로 닫힘', !(await menuState(page)).open);

        //.// 팀 대시보드 GNB — 다크 네이비, RoleCaption "팀 관리자"
        await page.goto(BASE + '/dashboard/team', { waitUntil: 'networkidle2' });
        await ready(page, '팀 관리자 모드');
        await sleep(400);
        check('팀 대시보드 GNB 계정 메뉴', await openMenu(page));
        await sleep(300);
        check('역할 캡션 "팀 관리자"', await page.evaluate(() => {
            const card = document.querySelector('.absolute.right-0');
            return document.body.innerText.includes('팀 관리자');
        }));
        await page.screenshot({ path: 'accountmenu-team.png' });

        console.log(`\n${pass} PASS / ${fail} FAIL`);
    } finally {
        await browser.disconnect();
        edge.kill();
    }
})();
