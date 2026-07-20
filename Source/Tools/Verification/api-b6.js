// B6 API 왕복 — 신청 → 중복 차단 → 취소 → 재신청 + 경계(친선 거부·남의 경기 거부)
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

const post = async (url, body) => {
    const r = await fetch(BASE + url, { method: 'POST', headers: H(), body: JSON.stringify(body ?? {}) });
    const j = await r.json().catch(() => null);
    return { ok: j?.isSuccess ?? false, msg: j?.message ?? null };
};
const get = async url => (await (await fetch(BASE + url, { headers: H() })).json())?.data ?? null;

const create = (matchId, extra = {}) => post('/api/soccer/team/me/corrections', {
    matchId, fieldType: 'Score', currentValue: '3:1', requestedValue: '3:2',
    description: '후반 39분 실점 누락', ...extra,
});

(async () => {
    //.// 팀 관리자 A = 광주광주FCU15 (공식 경기 보유)
    TOKEN = await login('verify-u15-1@test.local');
    console.log('token:', TOKEN ? 'OK' : 'FAILED');

    const matches = await get('/api/soccer/team/me/matches?season=2026');
    const official = (matches?.matches ?? []).filter(m => m.matchType === 'Official');
    const friendly = (matches?.matches ?? []).filter(m => m.matchType === 'Friendly');
    console.log(`경기: 공식 ${official.length}건 / 친선 ${friendly.length}건`);

    if (official.length === 0) { console.log('공식 경기가 없어 검증 불가'); return; }
    const target = official[0];
    console.log('대상 경기:', target.opponentName, `${target.teamScore}:${target.opponentScore}`);

    //.// 시작 상태 정리 (이전 검증 잔여분)
    for (const c of (await get('/api/soccer/team/me/corrections'))?.corrections ?? []) {
        if (c.status === 'Pending') { await post(`/api/soccer/team/me/corrections/${c.correctionId}/cancel`); }
    }
    console.log('시작 신청 수:', ((await get('/api/soccer/team/me/corrections'))?.corrections ?? []).length);

    //.// 1) 신청
    console.log('신청:', JSON.stringify(await create(target.matchId)));
    let list = (await get('/api/soccer/team/me/corrections'))?.corrections ?? [];
    const mine = list.find(c => c.matchId === target.matchId && c.status === 'Pending');
    console.log('신청 후:', list.length, '건 / 상태:', mine?.status,
        '/ 요약값:', mine?.currentValue, '→', mine?.requestedValue);

    //.// 2) 같은 경기 중복 신청 → 차단되어야 한다
    console.log('중복 신청(차단 기대):', JSON.stringify(await create(target.matchId, { requestedValue: '4:1' })));
    console.log('  → 신청 수 변화 없음:',
        ((await get('/api/soccer/team/me/corrections'))?.corrections ?? []).length === list.length);

    //.// 3) 친선 경기에 신청 → 거부되어야 한다 (친선은 직접 고칠 수 있다)
    // 이 팀에 친선이 없으면 친선을 가진 팀(검증fc)의 계정으로 자기 친선 경기에 시도한다 —
    // "남의 경기라서 거부"와 섞이지 않도록 반드시 소유 팀 계정으로 확인해야 한다.
    {
        const keep = TOKEN;
        let friendlyMatch = friendly[0] ?? null;

        if (friendlyMatch === null) {
            TOKEN = await login('verify-teamadmin-0713@test.local');
            const own = await get('/api/soccer/team/me/matches?season=2026');
            friendlyMatch = (own?.matches ?? []).find(m => m.matchType === 'Friendly') ?? null;
        }

        if (friendlyMatch === null) {
            console.log('친선 경기 신청: 검증할 친선 경기를 찾지 못함');
        } else {
            console.log('친선 경기 신청(소유 팀 계정, 거부 기대):',
                JSON.stringify(await create(friendlyMatch.matchId)));
        }

        TOKEN = keep;
    }

    //.// 4) 남의 경기에 신청 → 거부되어야 한다
    const otherToken = await login('verify-teamadmin-0713@test.local');
    const keep = TOKEN;
    TOKEN = otherToken;
    console.log('남의 경기 신청(거부 기대):', JSON.stringify(await create(target.matchId)));
    TOKEN = keep;

    //.// 5) 취소 → 다시 신청 가능해져야 한다
    console.log('취소:', JSON.stringify(await post(`/api/soccer/team/me/corrections/${mine.correctionId}/cancel`)));
    console.log('취소 후 목록:', ((await get('/api/soccer/team/me/corrections'))?.corrections ?? []).length, '건');
    console.log('재신청(이제 가능해야 함):', JSON.stringify(await create(target.matchId)));

    //.// 6) 잘못된 항목명 거부
    console.log('없는 항목명(거부 기대):',
        JSON.stringify(await create(target.matchId, { fieldType: 'Nonsense' })));

    //.// 정리
    for (const c of (await get('/api/soccer/team/me/corrections'))?.corrections ?? []) {
        if (c.status === 'Pending') { await post(`/api/soccer/team/me/corrections/${c.correctionId}/cancel`); }
    }
    console.log('정리 후:', ((await get('/api/soccer/team/me/corrections'))?.corrections ?? []).length, '건');
})();
