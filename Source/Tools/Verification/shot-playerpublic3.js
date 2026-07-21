// 공개 선수 프로필 3차 — 카드 뷰 2종 검증 (Design.PlayerPublicProfile 카드 공개/권한).
// 공개 카드(공개 항목만·QR 실렌더·스탯 4칩) → 이미지 저장(1080×1350 PNG — CDP 다운로드로 파일 검사)
// → 링크 공유(클립보드) → 디테일 진입점(PC 버튼·모바일 아이콘) → 권한 카드(승인 열람 블록·보호자 마스킹·
// 재공유 금지 캡션) → Profile off = 카드도 NotFound. 에이전트 상태는 2차와 같은 SQL 구성, 전부 원복.
const puppeteer = require('puppeteer-core');
const { spawn, execSync } = require('child_process');
const http = require('http');
const fs = require('fs');
const path = require('path');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9563;
const BASE = 'http://localhost:5000';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-pp3-' + Date.now();
const SHOT = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\claude\\d--Study-Workspace-PlayGroundNew\\c91a78a4-3845-419f-bf82-306440282945\\scratchpad\\pp3-';
const DOWNLOAD_DIR = SHOT + 'downloads';

const KIM = 'BD3393AD-5F09-46FC-AD54-D76BC93C8925';
const AGENT_ID = 'A9000000-0000-0000-0000-0000000000F2';
const REQUEST_ID = 'A9000000-0000-0000-0000-0000000000F3';
const AGENT_EMAIL = 'verify-agent-pp2@test.local';

const sql = (q, db = 'PlayGround_Soccer') => execSync(
    `sqlcmd -S .\\SQLEXPRESS -d ${db} -E -b -f 65001 -h -1 -W -Q "SET NOCOUNT ON; ${q.replace(/\s+/g, ' ').replace(/"/g, '\\"')}"`,
    { encoding: 'utf8' }).trim();

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});

async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json())?.data?.accessToken ?? null;
}

let pass = 0, fail = 0;
const check = (name, ok, detail) => {
    console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${detail ? ' — ' + detail : ''}`);
    ok ? pass++ : fail++;
};

// PNG IHDR에서 크기 읽기 (시그니처 8 + 길이 4 + 'IHDR' 4 → width/height BE 4+4)
function pngSize(file) {
    const buf = fs.readFileSync(file);
    return { width: buf.readUInt32BE(16), height: buf.readUInt32BE(20) };
}

(async () => {
    fs.mkdirSync(DOWNLOAD_DIR, { recursive: true });

    const edge = spawn(EDGE, [
        '--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank',
    ], { stdio: 'ignore' });
    const ws = await waitCdp();
    const browser = await puppeteer.connect({ browserWSEndpoint: ws, defaultViewport: null });

    try {
        //.// 1. 공개 카드 (게스트)
        let page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await page.goto(BASE + '/player/김정현/card', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));

        const text = await page.evaluate(() => document.body.innerText);
        check('공개 카드: 팀명·시즌·이름·teal 메타', ['광주광주FCU15', '2026 시즌 · U15', '김정현', 'GK · 왼발 · 2012'].every(s => text.includes(s)));
        check('공개 카드: 키(공개)만, 몸무게(비공개) 없음', text.includes('키 171cm') && !text.includes('몸무게'));
        check('공개 카드: 스탯 4칩 (출전·득점·도움·시간)', ['출전', '득점', '도움', '시간'].every(s => text.includes(s)) && text.includes("265'"));
        check('공개 카드: 승인 열람 블록 없음 + 공개 캡션', !text.includes('승인 열람 정보')
            && text.includes('카드에는 공개 설정된 정보만 담깁니다') && text.includes('QR 스캔으로 전체 프로필 열람'));

        // QR 실렌더 — 캔버스에 다크 셀 존재
        const qrDark = await page.evaluate(() => {
            const canvas = document.querySelector('canvas');
            if (!canvas) { return -1; }
            const ctx = canvas.getContext('2d');
            const px = ctx.getImageData(0, 0, canvas.width, canvas.height).data;
            let dark = 0;
            for (let i = 0; i < px.length; i += 4) {
                if (px[i] < 100) { dark++; }
            }
            return dark;
        });
        check('공개 카드: QR 캔버스 실렌더 (다크 셀 존재)', qrDark > 100, `dark px=${qrDark}`);
        await page.screenshot({ path: SHOT + 'card-public.png' });

        //.// 2. 이미지 저장 — CDP 다운로드로 1080×1350 PNG 검사
        const client = await page.createCDPSession();
        await client.send('Browser.setDownloadBehavior', { behavior: 'allow', downloadPath: DOWNLOAD_DIR });
        await page.evaluate(() => [...document.querySelectorAll('button')]
            .find(b => b.innerText.includes('이미지로 저장'))?.click());
        let saved = null;
        for (let i = 0; i < 20 && !saved; i++) {
            await new Promise(r => setTimeout(r, 500));
            const files = fs.readdirSync(DOWNLOAD_DIR).filter(f => f.endsWith('.png'));
            if (files.length > 0) { saved = path.join(DOWNLOAD_DIR, files[0]); }
        }
        if (saved) {
            const size = pngSize(saved);
            check('이미지 저장: PNG 1080×1350', size.width === 1080 && size.height === 1350, `${size.width}×${size.height}`);
        } else {
            check('이미지 저장: PNG 1080×1350', false, '다운로드 파일 없음');
        }

        //.// 3. 링크 공유 — 클립보드 (권한 부여 후 성공 토스트)
        await browser.defaultBrowserContext().overridePermissions(BASE, ['clipboard-read', 'clipboard-write', 'clipboard-sanitized-write']).catch(() => {});
        await page.evaluate(() => [...document.querySelectorAll('button')]
            .find(b => b.innerText.includes('링크 공유'))?.click());
        await new Promise(r => setTimeout(r, 800));
        const toastText = await page.evaluate(() => document.body.innerText);
        check('링크 공유: 토스트 피드백', toastText.includes('링크를 복사했어요') || toastText.includes('복사하지 못했어요'),
            toastText.includes('링크를 복사했어요') ? '복사 성공' : '실패 안내(headless 권한)');
        await page.close();

        //.// 4. 디테일 진입점 — PC "선수 카드 공유" 버튼 → 카드
        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await page.goto(BASE + '/player/김정현', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1200));
        await page.evaluate(() => [...document.querySelectorAll('a')]
            .find(a => a.innerText.includes('선수 카드 공유'))?.click());
        await new Promise(r => setTimeout(r, 1200));
        check('디테일 PC: "선수 카드 공유" → /card 이동', page.url().endsWith('/card'));
        await page.close();

        // 모바일 하단 바 공유 아이콘
        page = await browser.newPage();
        await page.setViewport({ width: 390, height: 844 });
        await page.goto(BASE + '/player/김정현', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1200));
        const mobileShareHref = await page.evaluate(() => {
            const link = [...document.querySelectorAll('a[aria-label="선수 카드 공유"]')]
                .find(a => a.getBoundingClientRect().width > 0);
            return link?.getAttribute('href');
        });
        check('디테일 모바일: 하단 공유 아이콘 복원 (/card 링크)', mobileShareHref?.endsWith('/card') === true, mobileShareHref ?? '없음');

        // 모바일 카드 — 가로 스크롤 0
        await page.goto(BASE + '/player/김정현/card', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1200));
        const mobHScroll = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth);
        check('모바일 카드: 가로 스크롤 0', !mobHScroll);
        await page.screenshot({ path: SHOT + 'card-mobile.png' });
        await page.close();

        //.// 5. 권한 카드 — 에이전트 승인 상태 구성 (2차와 동일 패턴)
        const agentToken = await login(AGENT_EMAIL);
        const agentUserId = sql(`SELECT CONVERT(VARCHAR(36), UserId) FROM Users WHERE Email='${AGENT_EMAIL}'`, 'PlayGround_Account');
        sql(`DELETE FROM SoccerAgentProfiles WHERE AgentId='${AGENT_ID}';
             INSERT INTO SoccerAgentProfiles (AgentId, UserId, Name, IsVerified) VALUES ('${AGENT_ID}','${agentUserId}','검증에이전트',1);
             DELETE FROM SoccerAgentViewRequests WHERE RequestId='${REQUEST_ID}';
             INSERT INTO SoccerAgentViewRequests (RequestId, AgentId, PlayerId, GuardianUserId, Message, Status, ReviewedAt, ExpiresAt)
             VALUES ('${REQUEST_ID}','${AGENT_ID}','${KIM}',(SELECT TOP 1 UserId FROM SoccerPlayers WHERE PlayerId='${KIM}'),'x','Approved',GETUTCDATE(),DATEADD(DAY,30,GETUTCDATE()))`);

        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), agentToken);
        await page.goto(BASE + '/player/김정현/card', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));
        const grantedText = await page.evaluate(() => document.body.innerText);
        check('권한 카드: 승인 열람 블록 (학교·학년·연락 안내)', grantedText.includes('승인 열람 정보')
            && grantedText.includes('플랫폼 메시지로 연락 가능'));
        check('권한 카드: 보호자 이름 마스킹 ("보호자 " + OO)', /보호자 .OO/.test(grantedText), (grantedText.match(/보호자 \S+/) ?? [])[0]);
        check('권한 카드: 재공유 금지 캡션', grantedText.includes('승인 열람 카드 · 재공유 금지')
            && grantedText.includes('재공유가 제한됩니다'));
        await page.screenshot({ path: SHOT + 'card-granted.png' });
        await page.close();

        //.// 6. Profile off → 카드도 NotFound
        sql(`INSERT INTO SoccerPlayerFieldVisibilities (PlayerId, FieldName, IsPublic) VALUES ('${KIM}', 'Profile', 0)`);
        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });
        await page.goto(BASE + '/player/김정현/card', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1200));
        const offText = await page.evaluate(() => document.body.innerText);
        check('Profile off: 카드도 "찾을 수 없어요"', offText.includes('선수 프로필을 찾을 수 없어요'));
        sql(`DELETE FROM SoccerPlayerFieldVisibilities WHERE PlayerId='${KIM}' AND FieldName='Profile'`);
        await page.close();

        console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
        process.exitCode = fail > 0 ? 1 : 0;
    } finally {
        browser.disconnect();
        edge.kill();
        sql(`DELETE FROM SoccerAgentViewLogs WHERE RequestId='${REQUEST_ID}';
             DELETE FROM SoccerAgentViewRequests WHERE RequestId='${REQUEST_ID}';
             DELETE FROM SoccerAgentProfiles WHERE AgentId='${AGENT_ID}'`);
        sql(`DELETE FROM Users WHERE Email='${AGENT_EMAIL}'`, 'PlayGround_Account');
        fs.rmSync(DOWNLOAD_DIR, { recursive: true, force: true });
        console.log('원복 완료 (에이전트·임시 계정·다운로드 파일)');
    }
})();
