// 기록 수정 심사 결과 알림 — 지연 생성 검증.
// 신청 생성(API) → 주최측 심사를 SQL로 흉내(외부에서 실행) → 알림 조회 시 CorrectionReviewed 생성 확인.
// 사용: node api-correction-noti.js create   → 신청 생성 후 correctionId 출력
//       node api-correction-noti.js verify   → 알림 조회로 지연 생성·중복 미생성 확인
const BASE = 'http://localhost:5000';
const mode = process.argv[2];

let failed = false;
function check(name, cond, detail) {
    console.log(`${cond ? 'PASS' : 'FAIL'} ${name}${detail ? ' — ' + detail : ''}`);
    if (!cond) failed = true;
}

async function login(email) {
    const r = await fetch(`${BASE}/api/auth/login/email`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json()).data.accessToken;
}

async function api(token, method, path, body) {
    const r = await fetch(`${BASE}${path}`, {
        method,
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: body ? JSON.stringify(body) : undefined,
    });
    return await r.json();
}

(async () => {
    const token = await login('verify-u12-1@test.local'); // 서울신답FCU12 — 공식 리그 경기 보유

    if (mode === 'create') {
        const matches = await api(token, 'GET', '/api/soccer/team/me/matches?season=2026');
        const official = matches.data?.matches?.find(m => m.tournamentName);
        if (!official) { console.error('no official match'); process.exit(1); }
        const created = await api(token, 'POST', '/api/soccer/team/me/corrections', {
            matchId: official.matchId, fieldType: 'Score', currentValue: '1:0', requestedValue: '2:0', description: '검증용',
        });
        console.log('created', JSON.stringify(created.data));
        return;
    }

    // verify — 심사(SQL) 후 조회하면 알림이 생겨 있어야 한다
    const noti = await api(token, 'GET', '/api/soccer/notifications/me');
    const reviewed = noti.data?.items?.filter(i => i.type === 'CorrectionReviewed');
    check('CorrectionReviewed lazily created', reviewed?.length === 1, String(reviewed?.length));
    check('snapshot: field/status', reviewed?.[0]?.metaText === 'Score' && reviewed?.[0]?.subText === 'Accepted',
        JSON.stringify(reviewed?.[0]));
    // 재조회해도 중복 생성 없음 (멱등)
    const noti2 = await api(token, 'GET', '/api/soccer/notifications/me');
    check('idempotent on re-fetch', noti2.data.items.filter(i => i.type === 'CorrectionReviewed').length === 1);
    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
