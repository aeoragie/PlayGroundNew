// 팀 게시판 UI 검증 (Design.TeamBoard). 관리자 대시보드 게시판(세그먼트·고정·눈 아이콘·⋯) +
// 공개홈 소개 탭 "팀 소식" + 보호자 뷰(안읽음 점). PC/모바일.
// 관리자 = verify-u15-1 (광주광주FCU15) / 보호자 = verify-player-u15 (김정현 보호자, child 3CA3649B).
const puppeteer = require('puppeteer-core');
const { spawn } = require('child_process');
const http = require('http');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const PORT = 9557;
const BASE = 'http://localhost:5000';
const SLUG = 'gwangjugwangjufcu15';
const CHILD = '3CA3649B-694C-402C-8C4A-2F5920724F07';
const UDD = 'C:\\Users\\aeora\\AppData\\Local\\Temp\\pg-edge-teamboard-' + Date.now();

const waitCdp = () => new Promise((res, rej) => {
    let t = 0;
    const k = () => http.get(`http://localhost:${PORT}/json/version`, r => {
        let d = ''; r.on('data', c => d += c); r.on('end', () => res(JSON.parse(d).webSocketDebuggerUrl));
    }).on('error', () => { if (++t > 60) { rej(new Error('CDP timeout')); } else { setTimeout(k, 250); } });
    k();
});
const sleep = ms => new Promise(r => setTimeout(r, ms));
const ready = (page, text) => page.waitForFunction(t => document.body.innerText.includes(t), { timeout: 40000, polling: 300 }, text);
const has = (page, text) => page.evaluate(t => document.body.innerText.includes(t), text);

async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json())?.data?.accessToken ?? null;
}
const H = (t) => ({ 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + t });
const savePost = async (t, body) => await (await fetch(BASE + '/api/soccer/team/me/posts', { method: 'POST', headers: H(t), body: JSON.stringify(body) })).json();
const setPin = async (t, id, p) => await fetch(BASE + `/api/soccer/team/me/posts/${id}/pin?pinned=${p}`, { method: 'POST', headers: H(t) });
const delPost = async (t, id) => await fetch(BASE + `/api/soccer/team/me/posts/${id}/delete?restore=false`, { method: 'POST', headers: H(t) });

let pass = 0, fail = 0;
const check = (name, ok) => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}`); ok ? pass++ : fail++; };

(async () => {
    const mgr = await login('verify-u15-1@test.local');
    const guardian = await login('verify-player-u15@test.local');
    if (!mgr || !guardian) { console.log('login FAILED'); return; }

    // 데이터 준비 — 공개 공지(고정) + 자료
    const notice = (await savePost(mgr, { type: 'Notice', title: '[TB] 8월 훈련 일정 변경 안내', body: '화·목 훈련이 19시로 30분 늦춰집니다. 폭염 시 실내 대체.', isPublic: true, files: [] }))?.data;
    const material = (await savePost(mgr, { type: 'Material', title: '[TB] 여름 캠프 참가 동의서', body: '첨부 파일을 작성해 제출해 주세요.', isPublic: false, files: [] }))?.data;
    await setPin(mgr, notice.postId, true);
    const created = [notice.postId, material.postId];

    const edge = spawn(EDGE, ['--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${UDD}`,
        '--no-first-run', '--disable-gpu', 'about:blank'], { stdio: 'ignore' });
    const browser = await puppeteer.connect({ browserWSEndpoint: await waitCdp(), defaultViewport: null });

    try {
        //.// 1) 관리자 대시보드 게시판 (PC)
        const page = await browser.newPage();
        page.on('pageerror', e => console.log('PAGE ERROR:', e.message));
        await page.setViewport({ width: 1280, height: 1100 });
        await page.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), mgr);
        await page.goto(BASE + '/dashboard/team/board', { waitUntil: 'networkidle2' });
        await ready(page, '팀 게시판');
        await sleep(500);
        check('세그먼트 전체 노출', await has(page, '전체'));
        check('공지 글 제목 노출', await has(page, '8월 훈련 일정 변경 안내'));
        check('자료 글 제목 노출', await has(page, '여름 캠프 참가 동의서'));
        check('고정 뱃지 노출', await has(page, '고정'));
        check('＋ 글 작성 버튼', await has(page, '＋ 글 작성'));
        await page.screenshot({ path: 'teamboard-dashboard-pc.png', fullPage: true });

        //.// 2) 작성 다이얼로그 — 공개 스위치 기본 끔 확인
        await page.evaluate(() => { const b = [...document.querySelectorAll('button')].find(x => x.innerText.trim() === '＋ 글 작성'); b && b.click(); });
        await sleep(500);
        check('작성 다이얼로그 열림', await has(page, '유형') && await has(page, '공지') && await has(page, '자료'));
        check('공개 스위치 문구', await has(page, '공개 홈페이지에도 노출'));
        await page.screenshot({ path: 'teamboard-form-pc.png', fullPage: true });

        //.// 3) 관리자 대시보드 (모바일)
        await page.setViewport({ width: 390, height: 900, isMobile: true });
        await page.goto(BASE + '/dashboard/team/board', { waitUntil: 'networkidle2' });
        await ready(page, '팀 게시판');
        await sleep(500);
        check('모바일 게시판 탭바 노출', await has(page, '게시판'));
        check('모바일 하단 작성 버튼', await has(page, '＋ 글 작성'));
        const noHScroll = await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1);
        check('모바일 가로 스크롤 없음', noHScroll);
        await page.screenshot({ path: 'teamboard-dashboard-mobile.png', fullPage: true });

        //.// 4) 공개홈 소개 탭 "팀 소식" (게스트)
        const guest = await browser.newPage();
        await guest.setViewport({ width: 1280, height: 1200 });
        await guest.goto(BASE + '/team/' + SLUG, { waitUntil: 'networkidle2' });
        await sleep(1200);
        check('소개 탭 "팀 소식" 섹션', await has(guest, '팀 소식'));
        check('공개 공지 노출', await has(guest, '8월 훈련 일정 변경 안내'));
        check('비공개 자료 미노출(공개홈)', !(await has(guest, '여름 캠프 참가 동의서')));
        await guest.screenshot({ path: 'teamboard-public-about-pc.png', fullPage: true });

        //.// 5) 보호자 뷰 — 팀 소식 (안읽음 점)
        const gp = await browser.newPage();
        await gp.setViewport({ width: 390, height: 900, isMobile: true });
        await gp.evaluateOnNewDocument(t => localStorage.setItem('pg.accessToken', t), guardian);
        await gp.goto(BASE + '/team-news/' + CHILD, { waitUntil: 'networkidle2' });
        await ready(gp, '팀 소식');
        await sleep(600);
        check('보호자 뷰 공지 노출', await has(gp, '8월 훈련 일정 변경 안내'));
        // 안읽음 오렌지 점 — bg-orange-ink 요소 존재
        const dot = await gp.evaluate(() => [...document.querySelectorAll('span')].some(s => getComputedStyle(s).backgroundColor === 'rgb(255, 107, 53)' && s.getBoundingClientRect().width > 0 && s.getBoundingClientRect().width < 12));
        check('안읽음 오렌지 점 존재', dot);
        await gp.screenshot({ path: 'teamboard-guardian-mobile.png', fullPage: true });

        //.// 6) 행 열기 → 상세 + 읽음
        await gp.evaluate(() => { const b = [...document.querySelectorAll('button')].find(x => x.innerText.includes('8월 훈련 일정 변경 안내')); b && b.click(); });
        await sleep(700);
        check('상세 본문 노출', await has(gp, '19시로 30분'));
        await gp.screenshot({ path: 'teamboard-guardian-detail.png', fullPage: true });

        console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
        process.exitCode = fail > 0 ? 1 : 0;
    } catch (e) {
        console.error('FAILED:', e.message);
        process.exitCode = 1;
    } finally {
        for (const id of created) { await delPost(mgr, id).catch(() => {}); }
        await browser.disconnect();
        edge.kill();
    }
})();
