// "처리가 필요해요" 파생 검증 — 알림 테이블 없이 현재 상태에서 만들어지는지.
// 확인: 미처리 초대 → 팀 단위 묶음 / 수정 신청은 심사 끝난 것만 / TotalCount는 자르기 전 값
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
    return (await r.json().catch(() => null))?.isSuccess ?? false;
};

(async () => {
    //.// 팀 관리자 — 미처리 초대가 많은 팀
    TOKEN = await login('verify-u15-1@test.local');
    console.log('token:', TOKEN ? 'OK' : 'FAILED');

    const items = await get('/api/soccer/team/me/action-items');
    console.log('전체 건수:', items?.totalCount, '/ 보여줄 항목:', items?.items?.length, '(최대 3)');
    for (const i of items?.items ?? []) {
        console.log(`  [${i.kind}] ${i.title} — ${i.description}`);
        console.log(`      링크 대상: teamId=${i.teamId ?? '-'} matchId=${i.matchId ?? '-'}`);
    }

    //.// 초대는 팀 단위로 묶여야 한다 (선수 28명이면 항목 28개가 아니라 1개 "28명")
    const inviteItems = (items?.items ?? []).filter(i => i.kind === 'Invite');
    console.log('초대 항목 수(팀 1개 → 1건 기대):', inviteItems.length);

    //.// 접수(Pending) 상태 신청은 액션이 아니어야 한다
    const matches = await get('/api/soccer/team/me/matches?season=2026');
    const official = (matches?.matches ?? []).find(m => m.matchType === 'Official');
    if (official) {
        await post('/api/soccer/team/me/corrections', {
            matchId: official.matchId, fieldType: 'Score',
            currentValue: '1:1', requestedValue: '2:1', description: 'ACTION 검증용',
        });
        const after = await get('/api/soccer/team/me/action-items');
        console.log('접수 신청 추가 후 항목 수(변화 없어야):', after?.totalCount, '←', items?.totalCount);

        // 정리
        for (const c of (await get('/api/soccer/team/me/corrections'))?.corrections ?? []) {
            if (c.status === 'Pending') { await post(`/api/soccer/team/me/corrections/${c.correctionId}/cancel`); }
        }
    }

    //.// 다른 팀 관리자 — 자기 팀 초대만 묶여야 한다(남의 팀 초대가 새지 않는지)
    TOKEN = await login('verify-teamadmin-0713@test.local');
    const other = await get('/api/soccer/team/me/action-items');
    console.log('다른 팀 관리자 — 항목:', (other?.items ?? []).map(i => i.title + ' / ' + i.description));

    //.// 팀이 없는 계정(보호자) — 원천이 아예 없어 0건이어야 한다.
    // 허브는 0건이면 "처리가 필요해요" 섹션 자체를 숨긴다(빈 상태 카드 금지).
    TOKEN = await login('verify-player-u12@test.local');
    const none = await get('/api/soccer/team/me/action-items');
    console.log('팀 없는 계정 — 항목 수(0 기대):', none?.totalCount);
})();
