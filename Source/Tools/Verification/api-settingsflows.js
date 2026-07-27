// 계정 설정 플로우 3종 검증 (Design.SettingsFlows). 이름 변경(반영·30일 2회·검증) · 로그인 수단(마스킹·수단수·해제 상태) ·
// 데이터 내려받기(요청→준비→다운로드 3회 상한→쿨다운). 임시 이메일 계정으로 하고 끝나면 SQL로 물리 삭제(원복).
const BASE = 'http://localhost:5000';
const EMAIL = 'verify-settingsflows-0727b@test.local';

const login = async () => (await (await fetch(BASE + '/api/auth/login/email', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: EMAIL, password: 'password123!' }),
})).json())?.data?.accessToken ?? null;

let TOKEN = null;
const H = () => ({ 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + TOKEN });
const j = async r => await r.json().catch(() => null);
const settings = async () => (await j(await fetch(BASE + '/api/auth/me/settings', { headers: H() })))?.data;
const me = async () => (await j(await fetch(BASE + '/api/auth/me', { headers: H() })))?.data;
const changeName = async name => await j(await fetch(BASE + '/api/auth/me/display-name', { method: 'PUT', headers: H(), body: JSON.stringify({ displayName: name }) }));
const unlink = async p => await j(await fetch(BASE + `/api/auth/me/social/${p}`, { method: 'DELETE', headers: H() }));
const exportRequest = async () => await j(await fetch(BASE + '/api/soccer/exports/me', { method: 'POST', headers: H(), body: JSON.stringify({ includeProfile: true, includeRecords: true, includeRequests: true }) }));
const exportCurrent = async () => (await j(await fetch(BASE + '/api/soccer/exports/me', { headers: H() })))?.data;
const nameClaim = t => { try {
    const bin = atob(t.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'));
    const bytes = Uint8Array.from(bin, c => c.charCodeAt(0));
    return JSON.parse(new TextDecoder('utf-8').decode(bytes))['name'];
} catch { return null; } };

let pass = 0, fail = 0;
const check = (name, ok, extra = '') => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${extra ? ' — ' + extra : ''}`); ok ? pass++ : fail++; };
const sleep = ms => new Promise(r => setTimeout(r, ms));

(async () => {
    TOKEN = await login();
    if (!TOKEN) { console.log('login FAILED'); return; }

    //.// ① 이름 변경
    const s0 = await settings();
    check('설정 로드', !!s0, `name=${s0?.displayName}`);
    check('로그인 수단 수 = 1 (비밀번호)', s0?.loginMeansCount === 1, `means=${s0?.loginMeansCount}`);
    check('이름 변경 가능 2회', s0?.nameChangeRemaining === 2, `remaining=${s0?.nameChangeRemaining}`);
    check('이메일 마스킹', typeof s0?.maskedEmail === 'string' && s0.maskedEmail.includes('***'), s0?.maskedEmail);

    const c1 = await changeName('김검증');
    check('이름 변경 성공', c1?.isSuccess === true);
    check('새 토큰 name 클레임 반영', nameClaim(c1?.data?.accessToken) === '김검증', nameClaim(c1?.data?.accessToken));
    TOKEN = c1?.data?.accessToken ?? TOKEN;
    const s1 = await settings();
    check('설정에 새 이름 반영', s1?.displayName === '김검증');
    check('남은 횟수 1', s1?.nameChangeRemaining === 1, `remaining=${s1?.nameChangeRemaining}`);

    const c2 = await changeName('이검증');
    check('두 번째 변경 성공', c2?.isSuccess === true);
    TOKEN = c2?.data?.accessToken ?? TOKEN;
    const s2 = await settings();
    check('남은 횟수 0', s2?.nameChangeRemaining === 0, `remaining=${s2?.nameChangeRemaining}`);
    check('다음 변경 가능일 설정됨', !!s2?.nameChangeAvailableAt);

    const c3 = await changeName('박검증');
    check('세 번째 변경 차단 (30일 2회)', c3?.isSuccess === false);

    //.// 검증 규칙 (별도 계정 영향 없이 — 이미 제한이라 검증 규칙만 확인은 어려우니 규칙은 클라와 동일함을 신뢰; 여기선 제한만)
    // 제한 상태라 검증 케이스가 전부 차단으로 수렴하므로 형식 검증은 UI 테스트에서 확인

    //.// ② 로그인 수단 — 없는 소셜 해제는 NotLinked (마지막 수단 로직은 SQL 테스트에서)
    const u = await unlink('Google');
    check('없는 소셜 해제 = NotLinked', u?.data === 'NotLinked', `status=${u?.data}`);

    //.// ③ 데이터 내려받기 — 요청 → 준비 → 다운로드 3회 상한 → 쿨다운
    const r1 = await exportRequest();
    check('내려받기 요청 접수', r1?.data?.status === 'Ok', `status=${r1?.data?.status}`);
    check('요청 직후 상태 Pending', r1?.data?.export?.status === 'Pending');

    const r2 = await exportRequest();
    check('진행 중 재요청 차단', r2?.data?.status === 'InProgress', `status=${r2?.data?.status}`);

    // 워커가 파일을 만들 때까지 폴링 (최대 ~25초)
    let cur = null;
    for (let i = 0; i < 25; i++) {
        await sleep(1000);
        cur = await exportCurrent();
        if (cur?.status && cur.status !== 'Pending') break;
    }
    check('파일 준비 완료 (Ready)', cur?.status === 'Ready', `status=${cur?.status}`);
    check('다운로드 토큰 발급', typeof cur?.downloadToken === 'string' && cur.downloadToken.length >= 32);
    check('만료일(7일) 설정', !!cur?.expiresAt);

    if (cur?.downloadToken) {
        const dl = t => fetch(BASE + `/api/soccer/exports/download/${t}`);
        const d1 = await dl(cur.downloadToken);
        const buf = await d1.arrayBuffer();
        check('다운로드 1회 = 200 zip', d1.status === 200 && buf.byteLength > 0, `bytes=${buf.byteLength}`);
        check('zip 시그니처(PK)', new Uint8Array(buf)[0] === 0x50 && new Uint8Array(buf)[1] === 0x4B);
        const d2 = await dl(cur.downloadToken); await d2.arrayBuffer();
        const d3 = await dl(cur.downloadToken); await d3.arrayBuffer();
        check('2·3회 다운로드 성공', d2.status === 200 && d3.status === 200);
        const d4 = await dl(cur.downloadToken);
        check('4회째 = 404 (3회 상한)', d4.status === 404, `status=${d4.status}`);
        const bad = await dl('deadbeef'.repeat(8));
        check('잘못된 토큰 = 404', bad.status === 404);
    }

    // Ready 이후 재요청 = 쿨다운(24h)
    const r3 = await exportRequest();
    check('완료 후 재요청 = 쿨다운', r3?.data?.status === 'Cooldown', `status=${r3?.data?.status}`);

    const uid = (await me())?.userId;
    console.log('\nTEMP_USER_ID=' + uid);
    console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
    process.exitCode = fail > 0 ? 1 : 0;
})();
