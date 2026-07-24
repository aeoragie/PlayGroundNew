// General(역할 없음) 루프 수정 검증 — /dashboard → /records 리다이렉트 + 역할 유도 배너.
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');
const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9621, BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-general-' + Date.now();
const waitCdp = () => new Promise((res, rej) => { let t = 0; const k = () => http.get(`http://localhost:${PORT}/json/version`, r => { let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl)); }).on('error', () => { if (++t > 60) rej(new Error('to')); else setTimeout(k, 250); }); k(); });
const sleep = ms => new Promise(r => setTimeout(r, ms));
async function login(e) { const r = await fetch(BASE + '/api/auth/login/email', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: e, password: 'password123!' }) }); return (await r.json())?.data?.accessToken; }
const settle = async page => { let last = page.url(); for (let s = 0; s < 3;) { await sleep(400); const n = page.url(); s = n === last ? s + 1 : 0; last = n; } return new URL(last).pathname; };

let pass = 0, fail = 0;
const check = (n, ok) => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${n}`); ok ? pass++ : fail++; };

(async () => {
  const edge = spawn(EDGE, ['--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`, '--no-first-run', '--disable-gpu', 'about:blank'], { stdio: 'ignore' });
  const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });
  try {
    const page = await browser.newPage(); await page.setViewport({ width: 1280, height: 900 });
    const t = await login('ft-general@test.local');
    await page.evaluateOnNewDocument(tk => localStorage.setItem('pg.accessToken', tk), t);

    // /dashboard 진입 → /records로 튕겨야 한다 (환영 카드 아님)
    await page.goto(BASE + '/dashboard', { waitUntil: 'networkidle2' });
    const landed = await settle(page);
    check('/dashboard → /records 리다이렉트', landed === '/records');
    check('환영 카드(막다른) 미표시', !(await page.evaluate(() => document.body.innerText.includes('아직 역할을 선택하지 않았어요'))));

    await page.waitForFunction(() => document.body.innerText.includes('경기기록'), { timeout: 20000, polling: 300 }).catch(() => {});
    await sleep(600);

    // 역할 유도 배너
    const banner = await page.evaluate(() => {
      const el = document.querySelector('[role="status"], [role="alert"]');
      const txt = el?.innerText || '';
      const link = [...(el?.querySelectorAll('a') || [])].find(a => a.innerText.includes('역할 선택하기'));
      return { shown: /역할을 선택하면/.test(txt), href: link?.getAttribute('href'), dismiss: !!el?.querySelector('button[aria-label="닫기"]') };
    });
    check('역할 유도 배너 표시', banner.shown);
    check('배너 링크 → /settings/select-role', banner.href === '/settings/select-role');
    check('정보톤 X 닫기 가능', banner.dismiss);
    await page.screenshot({ path: 'general-records.png' });

    // 닫기 → 사라지고 새로고침해도 안 뜸
    await page.evaluate(() => document.querySelector('button[aria-label="닫기"]')?.click());
    await sleep(400);
    check('배너 닫힘', !(await page.evaluate(() => /역할을 선택하면/.test(document.querySelector('[role="status"],[role="alert"]')?.innerText || ''))));
    await page.reload({ waitUntil: 'networkidle2' }); await sleep(800);
    check('새로고침해도 닫은 배너 안 뜸', !(await page.evaluate(() => /역할을 선택하면/.test(document.querySelector('[role="status"],[role="alert"]')?.innerText || ''))));

    console.log(`\n${pass} PASS / ${fail} FAIL`);
  } finally { await browser.disconnect(); edge.kill(); }
})();
