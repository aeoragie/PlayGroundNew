// 허브 묶음 + 라우팅 3분기의 근거 검증 (Design.DashboardHub).
// 화면 이동은 shot-hub.js가 본다 — 여기서는 "무엇을 근거로 갈라지는가"만 확인한다.
const BASE = 'http://localhost:5000';

async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json())?.data?.accessToken ?? null;
}

const get = async (token, url) =>
    (await (await fetch(BASE + url, { headers: { Authorization: 'Bearer ' + token } })).json())?.data ?? null;

async function probe(label, email, expect) {
    const token = await login(email);
    if (!token) { console.log(`${label}: 로그인 실패`); return; }

    const hub = await get(token, '/api/soccer/dashboard/me/hub');
    const managed = (hub?.teams?.length ?? 0) + (hub?.children?.length ?? 0);
    const branch = managed === 0 ? '역할 선택/역할 기준' : managed === 1 ? '스킵 리다이렉트' : '허브';

    console.log(`\n[${label}] ${email}`);
    console.log(`  팀 ${hub?.teams?.length ?? 0} · 자녀 ${hub?.children?.length ?? 0} → 관리 대상 ${managed} → ${branch} (기대: ${expect})`);
    console.log(`  ${branch === expect ? 'OK' : '불일치'}`);
    for (const t of hub?.teams ?? []) {
        console.log(`    팀: ${t.teamName} · 선수 ${t.playerCount}명 · 미처리 초대 ${t.pendingInviteCount}건 · slug=${t.slug ?? '-'}`);
    }
    for (const c of hub?.children ?? []) {
        console.log(`    자녀: ${c.name} (${c.ageGroup ?? '-'}) 출전 ${c.appearances} 득점 ${c.goals} 도움 ${c.assists}`);
    }
    console.log(`    벨 카운트(액션 전체) = ${hub?.actions?.totalCount ?? 0} · 표시 ${hub?.actions?.items?.length ?? 0}건`);
}

(async () => {
    await probe('팀만 1개', 'verify-u15-1@test.local', '스킵 리다이렉트');
    await probe('자녀 1명', 'verify-player-u12@test.local', '스킵 리다이렉트');
    await probe('자녀 2명', 'verify-player-u15@test.local', '허브');
    await probe('팀1+자녀1', 'verify-teamadmin-0713@test.local', '허브');

    //.// 자녀 스탯이 선수 대시보드와 같은 숫자인지 — 두 화면이 어긋나면 안 된다
    const token = await login('verify-player-u15@test.local');
    const hub = await get(token, '/api/soccer/dashboard/me/hub');
    const year = new Date().getFullYear();
    console.log('\n[스탯 일치] 허브 vs 선수 대시보드 (공식 경기만)');
    for (const c of hub?.children ?? []) {
        const s = await get(token, `/api/soccer/player/me/season-stats?season=${year}&playerId=${c.playerId}`);
        const official = (s?.matches ?? []).filter(m => m.matchType !== 'Friendly');
        const goals = official.reduce((a, m) => a + m.goals, 0);
        const assists = official.reduce((a, m) => a + m.assists, 0);
        const same = official.length === c.appearances && goals === c.goals && assists === c.assists;
        console.log(`  ${c.name}: 허브(${c.appearances}/${c.goals}/${c.assists}) vs 대시보드(${official.length}/${goals}/${assists}) → ${same ? 'OK' : '불일치'}`);
    }
})();
