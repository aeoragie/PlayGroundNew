// 배너 3톤·스텝퍼 검증 (Design.BannerStepper). 개발 데모 페이지 /dev/banner-stepper 사용.
// 확인: 심각도 우선(동시 1개) · 정보만 X 닫힘 + 새로고침 유지 · 스텝퍼 완료 스텝만 클릭 ·
//       모바일 진행 바(도트 나열 0건) · teal 운영 배너 0건.
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9541;
const BASE = 'http://localhost:5000';
const URL = BASE + '/dev/banner-stepper';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-banner-' + Date.now();

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});

const ready = (page, text) => page.waitForFunction(t => document.body.innerText.includes(t), { timeout: 40000, polling: 300 }, text);
const clickBtn = (page, label) => page.evaluate(t => {
    const b = [...document.querySelectorAll('button')].find(x => x.innerText.trim() === t && x.getBoundingClientRect().width > 0);
    if (b) { b.click(); return true; } return false;
}, label);
const sleep = ms => new Promise(r => setTimeout(r, ms));

// 현재 떠 있는 배너의 접두어(정보/주의/오류) — 없으면 null
const bannerPrefix = page => page.evaluate(() => {
    const el = document.querySelector('[role="status"] b, [role="alert"] b');
    return el ? el.innerText.trim() : null;
});

let pass = 0, fail = 0;
const check = (name, ok) => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}`); ok ? pass++ : fail++; };

(async () => {
    const edge = spawn(EDGE, ['--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank'], { stdio: 'ignore' });
    const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1280, height: 900 });
        await page.goto(URL, { waitUntil: 'networkidle2' });
        await ready(page, '배너·스텝퍼 데모');

        //.// 1) 심각도 우선 — 셋 다 올리면 오류가 보인다
        await clickBtn(page, '정보 올리기'); await sleep(150);
        await clickBtn(page, '주의 올리기'); await sleep(150);
        await clickBtn(page, '오류 올리기'); await sleep(200);
        check('세 배너 올림 → 오류가 이긴다', (await bannerPrefix(page)) === '오류');

        //.// 2) 오류 내리면 주의, 주의 내리면 정보
        await clickBtn(page, '모두 내리기'); await sleep(150);
        await clickBtn(page, '정보 올리기'); await sleep(100);
        await clickBtn(page, '주의 올리기'); await sleep(200);
        check('정보+주의 → 주의가 이긴다', (await bannerPrefix(page)) === '주의');

        //.// 3) 주의·오류에는 닫기 버튼이 없다
        const warnHasClose = await page.evaluate(() => !!document.querySelector('[role="status"] button[aria-label="닫기"], [role="alert"] button[aria-label="닫기"]'));
        check('주의 배너에 X 없음', !warnHasClose);

        //.// 4) 정보만 남기고 X로 닫기 → 사라짐
        await clickBtn(page, '모두 내리기'); await sleep(120);
        await clickBtn(page, '정보 올리기'); await sleep(200);
        check('정보 배너에 X 있음', await page.evaluate(() => !!document.querySelector('button[aria-label="닫기"]')));
        await page.evaluate(() => document.querySelector('button[aria-label="닫기"]').click());
        await sleep(200);
        check('정보 X 닫기 → 배너 사라짐', (await bannerPrefix(page)) === null);

        //.// 5) 새로고침해도 닫은 정보 배너는 다시 안 뜬다(localStorage) — 올려도 무시
        await page.reload({ waitUntil: 'networkidle2' });
        await ready(page, '배너·스텝퍼 데모');
        await clickBtn(page, '정보 올리기'); await sleep(200);
        check('닫은 정보 배너는 다시 올려도 안 뜬다', (await bannerPrefix(page)) === null);
        // 주의는 닫은 적 없으니 그대로 뜬다
        await clickBtn(page, '주의 올리기'); await sleep(200);
        check('닫지 않은 주의 배너는 정상 표시', (await bannerPrefix(page)) === '주의');

        //.// 6) 스텝퍼 — 완료 스텝(왼쪽 도트)만 클릭돼 뒤로 이동
        await clickBtn(page, '다음 →'); await sleep(120);
        await clickBtn(page, '다음 →'); await sleep(120); // 현재 스텝 index 2(대기)
        const stepBefore = await page.evaluate(() => document.body.innerText.match(/현재 스텝: (\d)/)?.[1]);
        // 완료 스텝(첫 도트=코드) 클릭 → 0으로
        await page.evaluate(() => {
            const dots = [...document.querySelectorAll('button')].filter(b => b.querySelector('svg') || /^[1-4]$/.test(b.innerText.trim().slice(0,1)));
            const first = [...document.querySelectorAll('button:not([disabled])')].find(b => b.innerText.includes('코드'));
            if (first) { first.click(); }
        });
        await sleep(150);
        const stepAfter = await page.evaluate(() => document.body.innerText.match(/현재 스텝: (\d)/)?.[1]);
        check(`완료 스텝 클릭 → 뒤로 이동 (${stepBefore}→${stepAfter})`, stepBefore === '3' && stepAfter === '1');

        //.// 미래 스텝은 disabled라 클릭돼도 이동 안 함
        const futureDisabled = await page.evaluate(() => {
            const btns = [...document.querySelectorAll('button')].filter(b => ['확인','대기','완료'].some(l => b.innerText.includes(l)));
            return btns.filter(b => b.disabled).length;
        });
        check('미래 스텝 도트는 비활성', futureDisabled >= 1);

        await page.screenshot({ path: 'banner-pc.png' });

        //.// 7) teal 운영 배너 0건 — 배너에 teal 배경/보더가 쓰이면 안 된다
        const tealBanner = await page.evaluate(() => {
            const el = document.querySelector('[role="status"], [role="alert"]');
            if (!el) { return false; }
            const bg = getComputedStyle(el).backgroundColor;
            return bg.includes('46, 196, 182'); // teal rgb
        });
        check('teal 운영 배너 0건', !tealBanner);

        //.// 8) 모바일 — 스텝퍼가 진행 바 형태(도트 나열 아님) + 가로 스크롤 없음
        await page.setViewport({ width: 390, height: 844 });
        await page.reload({ waitUntil: 'networkidle2' });
        await ready(page, '배너·스텝퍼 데모');
        const mobile = await page.evaluate(() => {
            // 모바일 스텝퍼: "n / 4" 카운트 텍스트가 보이고 진행 바가 있다
            const hasCount = /\d \/ 4/.test(document.body.innerText);
            const bar = [...document.querySelectorAll('span')].some(s => s.style.width && s.style.width.includes('%'));
            return { hasCount, bar, hScroll: document.documentElement.scrollWidth > window.innerWidth };
        });
        check('모바일 스텝퍼 = 카운트+진행 바', mobile.hasCount && mobile.bar);
        check('모바일 가로 스크롤 없음', !mobile.hScroll);
        await page.screenshot({ path: 'banner-mobile.png', fullPage: true });

        console.log(`\n${pass} PASS / ${fail} FAIL`);
    } finally {
        await browser.disconnect();
        edge.kill();
    }
})();
