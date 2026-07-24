// 보호자 기록 수정 신청 검증 (Design.RecordCorrection 보호자 경로).
// 신청→목록 반영 / 중복 차단 / 취소→재신청 가능 / 남의 자녀 경기 거부 / 친선 거부.
const BASE = 'http://localhost:5000';

async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json())?.data?.accessToken ?? null;
}

let TOKEN = null;
const H = () => ({ 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + TOKEN });
const get = async url => (await (await fetch(BASE + url, { headers: H() })).json())?.data ?? null;
const post = async (url, body) => {
    const r = await fetch(BASE + url, { method: 'POST', headers: H(), body: JSON.stringify(body ?? {}) });
    return await r.json().catch(() => null);
};
const del = async url => {
    const r = await fetch(BASE + url, { method: 'DELETE', headers: H() });
    return (await r.json().catch(() => null))?.isSuccess ?? false;
};

let pass = 0, fail = 0;
const check = (name, ok, extra = '') => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${extra ? ' — ' + extra : ''}`); ok ? pass++ : fail++; };

const YEAR = 2026;

(async () => {
    TOKEN = await login('verify-player-u15@test.local'); // 김정현 보호자
    if (!TOKEN) { console.log('login FAILED'); return; }

    // 김정현의 playerId + 공식 경기 하나
    const players = await get('/api/soccer/player/me/players');
    const child = players?.players?.[0];
    const stats = await get(`/api/soccer/player/me/season-stats?season=${YEAR}&playerId=${child.playerId}`);
    const official = (stats?.matches ?? []).find(m => m.matchType === 'Official');
    const friendly = (stats?.matches ?? []).find(m => m.matchType === 'Friendly');
    console.log('대상 자녀:', child?.name, '· 공식경기 matchId:', official?.matchId);

    const body = (matchId, playerId) => ({
        matchId, targetPlayerId: playerId ?? child.playerId,
        fieldType: 'GoalAssist', currentValue: '득점 2 · 도움 1', requestedValue: '득점 3 · 도움 1',
        description: '보호자 검증용',
    });

    // 1) 정상 신청
    const created = await post('/api/soccer/player/me/corrections', body(official.matchId));
    check('신청 성공', created?.isSuccess === true);

    const list1 = await get('/api/soccer/player/me/corrections');
    const mine = (list1?.corrections ?? []).find(c => c.matchId === official.matchId && c.status === 'Pending');
    check('목록에 내 신청 반영', !!mine);

    // 2) 같은 경기 중복 신청 차단
    const dup = await post('/api/soccer/player/me/corrections', body(official.matchId));
    check('중복 신청 차단', dup?.isSuccess === false);

    // 3) 친선 경기 신청 거부
    if (friendly) {
        const fr = await post('/api/soccer/player/me/corrections', body(friendly.matchId));
        check('친선 경기 신청 거부', fr?.isSuccess === false);
    } else {
        console.log('(친선 경기 없음 — 스킵)');
    }

    // 4) 남의 자녀(임의 playerId)로 신청 거부 — 내 자녀 아님
    const bogus = await post('/api/soccer/player/me/corrections',
        body(official.matchId, '00000000-0000-0000-0000-0000000000AA'));
    check('남의 자녀 playerId 거부', bogus?.isSuccess === false);

    // 5) 취소 → 재신청 가능
    const canceled = await del(`/api/soccer/player/me/corrections/${mine.correctionId}`);
    check('취소 성공', canceled);
    const list2 = await get('/api/soccer/player/me/corrections');
    check('취소 후 목록에서 사라짐', !(list2?.corrections ?? []).some(c => c.correctionId === mine.correctionId));

    const again = await post('/api/soccer/player/me/corrections', body(official.matchId));
    check('취소 후 재신청 가능', again?.isSuccess === true);

    // 정리 — 방금 재신청 건 취소(소프트 삭제). 물리 삭제는 sql로.
    const list3 = await get('/api/soccer/player/me/corrections');
    for (const c of (list3?.corrections ?? [])) {
        if (c.status === 'Pending') { await del(`/api/soccer/player/me/corrections/${c.correctionId}`); }
    }
    console.log('\n정리: 검증 신청 취소(소프트). 물리 삭제는 sql-guardiancorrection-cleanup.sql');
    console.log(`\n${pass} PASS / ${fail} FAIL`);
})();
