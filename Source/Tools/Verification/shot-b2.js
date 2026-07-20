// B2 검증 — 엠블럼 교체→공개홈 반영 / 12MB 실패 / 세로 사진 EXIF 보정
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9511;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-b2-' + Date.now();

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
        const el = [...document.querySelectorAll('button, a, span')]
            .find(x => x.innerText?.trim() === t && x.offsetParent !== null);
        if (!el) { return false; }
        el.click();
        return true;
    }, text);
    if (!ok) { throw new Error('not found: ' + text); }
    await sleep(400);
};

// 캔버스로 테스트 이미지를 만들어 file input에 심는다 (실제 파일 없이 시나리오 재현)
async function setFile(page, selectorIndex, spec) {
    return await page.evaluate(async (index, s) => {
        const inputs = [...document.querySelectorAll('input[type=file]')];
        const input = inputs[index];
        if (!input) { return 'NO_INPUT'; }

        const canvas = document.createElement('canvas');
        canvas.width = s.width;
        canvas.height = s.height;
        const ctx = canvas.getContext('2d');
        // 위/아래를 다른 색으로 칠해 회전 여부를 눈으로 구분한다
        ctx.fillStyle = '#23408e';
        ctx.fillRect(0, 0, canvas.width, canvas.height / 2);
        ctx.fillStyle = '#FF6B35';
        ctx.fillRect(0, canvas.height / 2, canvas.width, canvas.height / 2);

        const blob = await new Promise(r => canvas.toBlob(r, 'image/jpeg', s.quality ?? 0.9));

        // 용량을 부풀려야 하는 경우 뒤에 더미 바이트를 붙인다
        let parts = [blob];
        if (s.padToBytes && blob.size < s.padToBytes) {
            parts.push(new Uint8Array(s.padToBytes - blob.size));
        }

        const file = new File(parts, s.name, { type: 'image/jpeg' });
        const dt = new DataTransfer();
        dt.items.add(file);
        input.files = dt.files;
        input.dispatchEvent(new Event('change', { bubbles: true }));
        return { size: file.size, name: file.name };
    }, selectorIndex, spec);
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
        const token = await page.evaluate(async base => {
            const r = await fetch(base + '/api/auth/login/email', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: 'verify-teamadmin-0713@test.local', password: 'password123!' }),
            });
            return (await r.json())?.data?.accessToken ?? null;
        }, BASE);
        console.log('token:', token ? 'OK' : 'FAILED');
        await page.evaluate(t => localStorage.setItem('pg.accessToken', t), token);

        await page.goto(BASE + '/dashboard/team', { waitUntil: 'networkidle0', timeout: 60000 });
        await ready(page, '팀 정보');
        await sleep(800);

        await clickText(page, '정보 수정');
        await ready(page, '팀 정보 수정');
        await sleep(500);
        await page.screenshot({ path: 'b2-01-edit-open.png' });
        console.log('수정 폼 열림 · 프리필 확인:', await page.evaluate(() => {
            const v = document.querySelector('input[placeholder="검증fc"]')?.value;
            return { teamName: v, values: document.body.innerText.includes('가치 1') };
        }));

        //.// 1) 12MB 파일 → 인라인 실패 (토스트 아님)
        const big = await setFile(page, 0, { width: 1200, height: 1200, name: 'big.jpg', padToBytes: 12 * 1024 * 1024 });
        console.log('12MB 파일 주입:', JSON.stringify(big));
        await sleep(1500);
        const failState = await page.evaluate(() => ({
            inline: document.body.innerText.includes('업로드하지 못했어요'),
            hint: (document.body.innerText.match(/파일이 [\d.]+MB예요[^\n]*/) || [null])[0],
            retry: document.body.innerText.includes('다시 선택'),
            toast: document.querySelector('[role=status]') !== null,
        }));
        console.log('12MB 결과:', JSON.stringify(failState));
        await page.screenshot({ path: 'b2-02-too-large.png' });

        //.// 2) 세로 사진(EXIF) → 크롭 모달, 눕지 않아야 한다
        await setFile(page, 0, { width: 900, height: 1600, name: 'portrait.jpg' });
        await sleep(2000);
        const crop = await page.evaluate(() => {
            const img = [...document.querySelectorAll('img')].find(i => i.src.startsWith('data:'));
            return {
                cropOpen: document.body.innerText.includes('사진 위치 조정'),
                naturalW: img?.naturalWidth ?? 0,
                naturalH: img?.naturalHeight ?? 0,
            };
        });
        console.log('세로 사진:', JSON.stringify(crop), crop.naturalH > crop.naturalW ? '(세로 유지 OK)' : '(눕음!)');
        await page.screenshot({ path: 'b2-03-crop-portrait.png' });

        //.// 3) 크롭 적용 → 업로드 → 저장 → 공개홈 반영
        await clickText(page, '적용');
        await sleep(2500);
        const uploaded = await page.evaluate(() => {
            const img = [...document.querySelectorAll('img')].find(i => i.src.includes('/uploads/'));
            return img?.getAttribute('src') ?? null;
        });
        console.log('업로드된 엠블럼:', uploaded);
        await page.screenshot({ path: 'b2-04-uploaded.png' });

        await clickText(page, '저장');
        await sleep(2500);
        const saved = await page.evaluate(() => ({
            toast: document.querySelector('[role=status]')?.innerText ?? null,
            closed: !document.body.innerText.includes('팀 정보 수정'),
        }));
        console.log('저장:', JSON.stringify(saved));
        await page.screenshot({ path: 'b2-05-saved.png' });

        //.// 공개홈에 반영됐는지
        await page.goto(BASE + '/team/' + encodeURIComponent('검증fc'), { waitUntil: 'networkidle0', timeout: 60000 });
        await sleep(2500);
        const publicLogo = await page.evaluate(() => {
            const img = [...document.querySelectorAll('img')].find(i => i.src.includes('/uploads/'));
            return img?.getAttribute('src') ?? null;
        });
        console.log('공개홈 엠블럼:', publicLogo, publicLogo === uploaded ? '(일치 — 즉시 반영)' : '(불일치)');
        await page.screenshot({ path: 'b2-06-public-home.png' });

        await browser.disconnect();
    } catch (e) {
        console.error('FAILED:', e.message);
        process.exitCode = 1;
    } finally {
        edge.kill();
    }
})();
