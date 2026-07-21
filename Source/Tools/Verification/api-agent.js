// 에이전트 열람 승인 API 왕복 — 지시 시나리오: 요청 → 승인 → 30일 만료 (+거절·철회·차단·경계).
// 요청 생성·열람 로그 적재는 SQL로 에이전트 서비스를 흉내 낸다. 사용: node api-agent.js <requestId> [requestId2] [requestId3]
const BASE = 'http://localhost:5000';
const [ID_MAIN, ID_DENY, ID_BLOCK] = process.argv.slice(2);

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
    const guardian = await login('verify-player-u15@test.local');  // 김정현 보호자(관리 계정)
    const other = await login('verify-teamadmin-0713@test.local');

    //.// 알림 — 열람 요청 지연 생성 (3건: main/deny/block)
    const noti = await api(guardian, 'GET', '/api/soccer/notifications/me');
    const viewNotis = noti.data?.items?.filter(i => i.type === 'ViewRequest') ?? [];
    check('ViewRequest notifications lazily created', viewNotis.length === 3, String(viewNotis.length));
    check('notification snapshot (agent/player)', viewNotis.every(n => n.actorName === '박OO' && n.playerName === '김정현'));

    //.// 조회 — pending + 신원 카드 + 남의 계정 거부
    const req = await api(guardian, 'GET', `/api/soccer/agent-approvals/me/${ID_MAIN}`);
    check('get pending request', req.isSuccess && req.data.status === 'Pending', req.codeName);
    check('agent identity card', req.data?.agent?.isVerified === true && req.data.agent.agencyName === '드림 스포츠 에이전시'
        && req.data.agent.brokerageCount === 14 && Number(req.data.agent.rating) === 4.7);
    check('player meta', req.data?.playerName === '김정현' && req.data.playerAgeGroup === 'U15');
    const foreign = await api(other, 'GET', `/api/soccer/agent-approvals/me/${ID_MAIN}`);
    check('foreign account denied', foreign.isSuccess === false && foreign.codeName === 'NotFound', foreign.codeName);
    const badAction = await api(guardian, 'POST', '/api/soccer/agent-approvals/me/review', { requestId: ID_MAIN, action: 'Hack' });
    check('unknown action denied', badAction.isSuccess === false && badAction.codeName === 'InvalidInput', badAction.codeName);

    //.// 승인 — +30일 만료, 승인 로그, 알림 읽음
    const approved = await api(guardian, 'POST', '/api/soccer/agent-approvals/me/review', { requestId: ID_MAIN, action: 'Approve' });
    check('approve ok', approved.isSuccess && approved.data.status === 'Approved');
    const days = (new Date(approved.data.expiresAt) - Date.now()) / 86400000;
    check('expires in ~30 days', days > 29 && days <= 30.1, days.toFixed(2));
    const afterApprove = await api(guardian, 'GET', `/api/soccer/agent-approvals/me/${ID_MAIN}`);
    check('approved log written', afterApprove.data.logs.some(l => l.eventType === 'Approved'));
    check('not expired yet', afterApprove.data.isExpired === false);
    const noti2 = await api(guardian, 'GET', '/api/soccer/notifications/me');
    const mainNoti = noti2.data.items.find(i => i.type === 'ViewRequest' && i.refId === ID_MAIN.toLowerCase());
    check('notification marked read on review', mainNoti && mainNoti.isRead === true);

    // 승인 상태에서 재승인·거절 불가 (Revoke만 가능)
    const reApprove = await api(guardian, 'POST', '/api/soccer/agent-approvals/me/review', { requestId: ID_MAIN, action: 'Approve' });
    check('double approve denied', reApprove.isSuccess === false && reApprove.codeName === 'Forbidden');

    //.// 거절 경로 — 사유 없이 상태만
    const denied = await api(guardian, 'POST', '/api/soccer/agent-approvals/me/review', { requestId: ID_DENY, action: 'Deny' });
    check('deny ok', denied.isSuccess && denied.data.status === 'Denied');

    //.// 차단 경로 — 차단 행 + 대기 요청 거절
    const blocked = await api(guardian, 'POST', `/api/soccer/agent-approvals/me/${ID_BLOCK}/block`);
    check('block ok', blocked.isSuccess);
    const afterBlock = await api(guardian, 'GET', `/api/soccer/agent-approvals/me/${ID_BLOCK}`);
    check('blocked request denied', afterBlock.data?.status === 'Denied');

    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
