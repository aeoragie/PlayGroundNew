// 허브 승인 대기 자녀 검증 (Design.DashboardHub). sql-pendingchild.sql 실행 후.
// 확인: Claimed + Pending 자녀가 함께 오는지 / Pending은 스탯 0·ClaimStatus=Pending·RequestedAt 있음 /
//       이미 연결된 선수는 Pending 카드로 중복되지 않는지.
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

let pass = 0, fail = 0;
const check = (name, ok, extra = '') => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${extra ? ' — ' + extra : ''}`); ok ? pass++ : fail++; };

(async () => {
    const token = await login('verify-player-u15@test.local'); // 김정현 보호자
    const hub = await get(token, '/api/soccer/dashboard/me/hub');
    const kids = hub?.children ?? [];
    console.log('자녀 카드:', kids.map(k => `${k.name}(${k.claimStatus})`).join(', '));

    const claimed = kids.filter(k => k.claimStatus === 'Claimed');
    const pending = kids.filter(k => k.claimStatus === 'Pending');

    check('Claimed 자녀 존재', claimed.length >= 1);
    check('Pending 자녀 존재', pending.length === 1, pending.map(k => k.name).join(','));

    const p = pending[0];
    check('Pending 스탯 0 (화면에서 "–")', p && p.appearances === 0 && p.goals === 0 && p.assists === 0);
    check('Pending RequestedAt 있음', !!p?.requestedAt, p?.requestedAt);
    check('Pending 팀명 있음', p?.teamName === '검증fc', p?.teamName);

    check('관리 대상 2 → 허브 표시', (hub?.teams?.length ?? 0) + kids.length >= 2);

    // 중복 방지 — 같은 선수가 Claimed와 Pending 양쪽에 있지 않다
    const names = kids.map(k => k.name);
    check('중복 카드 없음', new Set(names).size === names.length);

    console.log(`\n${pass} PASS / ${fail} FAIL`);
})();
