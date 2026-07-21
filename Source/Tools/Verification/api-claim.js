// Claim 4스텝 + 알림 센터 API 왕복 검증 (Design.ClaimFlow):
// 신청(보호자) → 관리자 액션형 알림 → 승인 → 보호자 승인 알림 + 허브 자녀 카드 반영 →
// 거절 경로 → 경계(남의 요청·무효 코드·잘못된 관계) → 친선경기 결과 알림(설정 필터 on/off) →
// 기록 수정 심사 결과 지연 동기화. 데이터는 sql-claim-restore.sql로 원복.
const BASE = 'http://localhost:5000';
const CODE_APPROVE = '23702F'; // 박도윤 (검증fc)
const CODE_REJECT = '5FD247';  // 최시우 (검증fc)

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
    const env = await r.json();
    if (!env.isSuccess) throw new Error(`login failed ${email}`);
    return env.data.accessToken;
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
    const guardian = await login('verify-guardian-0721@test.local'); // find-or-create
    const manager = await login('verify-teamadmin-0713@test.local');
    const otherManager = await login('verify-u15-1@test.local');

    //.// 스텝 ①→②: 코드 조회
    const card = await api(guardian, 'GET', `/api/soccer/claim/invite/${CODE_APPROVE}`);
    check('lookup card', card.isSuccess && card.data.name === '박도윤', JSON.stringify(card.data ?? card.codeName));
    const badCode = await api(guardian, 'GET', '/api/soccer/claim/invite/ZZZZZZ');
    check('invalid code -> NotFound', badCode.isSuccess === false && badCode.codeName === 'NotFound', badCode.codeName);

    //.// 스텝 ②→③: 요청 생성 (+ 멱등, 잘못된 관계 거부)
    const badRel = await api(guardian, 'POST', '/api/soccer/claim/me/requests', { code: CODE_APPROVE, relation: 'Hacker' });
    check('unknown relation denied', badRel.isSuccess === false && badRel.codeName === 'InvalidInput', badRel.codeName);

    const created = await api(guardian, 'POST', '/api/soccer/claim/me/requests', { code: CODE_APPROVE, relation: 'Mother' });
    check('request created (Pending)', created.isSuccess && created.data.status === 'Pending', created.data?.status);
    const created2 = await api(guardian, 'POST', '/api/soccer/claim/me/requests', { code: CODE_APPROVE, relation: 'Mother' });
    check('idempotent re-submit', created2.isSuccess && created2.data.requestId === created.data.requestId);

    const mine = await api(guardian, 'GET', '/api/soccer/claim/me/request');
    check('own request restored', mine.isSuccess && mine.data.status === 'Pending' && mine.data.playerName === '박도윤');

    //.// 관리자 알림 — 액션형 + 미읽음
    const mgrNoti = await api(manager, 'GET', '/api/soccer/notifications/me');
    const claimNoti = mgrNoti.data?.items?.find(i => i.type === 'ClaimRequest' && i.refId === created.data.requestId);
    check('manager has ClaimRequest notification', !!claimNoti, JSON.stringify(mgrNoti.data?.items?.map(i => i.type)));
    check('action notification unread', claimNoti && !claimNoti.isRead && mgrNoti.data.unreadCount >= 1, String(mgrNoti.data?.unreadCount));
    check('notification snapshot', claimNoti && claimNoti.playerName === '박도윤' && claimNoti.subText === CODE_APPROVE
        && claimNoti.relation === 'Mother' && claimNoti.requestStatus === 'Pending');

    //.// 경계: 남의 팀 관리자가 승인 시도 → Forbidden
    const foreign = await api(otherManager, 'POST', '/api/soccer/claim/requests/review',
        { requestId: created.data.requestId, approve: true });
    check('foreign manager denied', foreign.isSuccess === false && foreign.codeName === 'Forbidden', foreign.codeName);

    //.// 승인 → 연결·가족·알림·읽음
    const approved = await api(manager, 'POST', '/api/soccer/claim/requests/review',
        { requestId: created.data.requestId, approve: true });
    check('approved', approved.isSuccess && approved.data.status === 'Approved', approved.codeName);

    const mineAfter = await api(guardian, 'GET', '/api/soccer/claim/me/request');
    check('own request now Approved', mineAfter.isSuccess && mineAfter.data.status === 'Approved');

    const gdnNoti = await api(guardian, 'GET', '/api/soccer/notifications/me');
    const approvedNoti = gdnNoti.data?.items?.find(i => i.type === 'ClaimApproved');
    check('guardian ClaimApproved notification', !!approvedNoti && approvedNoti.playerName === '박도윤');
    check('guardian unread >= 1', gdnNoti.data?.unreadCount >= 1, String(gdnNoti.data?.unreadCount));

    const mgrNoti2 = await api(manager, 'GET', '/api/soccer/notifications/me');
    const claimNoti2 = mgrNoti2.data?.items?.find(i => i.refId === created.data.requestId);
    check('manager action marked read after review', claimNoti2 && claimNoti2.isRead && claimNoti2.requestStatus === 'Approved');

    //.// 허브 자녀 카드 반영 (SoccerPlayers.UserId 연결로 자동)
    const hub = await api(guardian, 'GET', '/api/soccer/dashboard/me/hub');
    const child = hub.data?.children?.find(c => c.name === '박도윤');
    check('hub child card appears', !!child, JSON.stringify(hub.data?.children?.map(c => c.name)));

    //.// 이동형 읽음 처리
    if (approvedNoti) {
        const read = await api(guardian, 'PUT', `/api/soccer/notifications/me/${approvedNoti.notificationId}/read`);
        check('mark read', read.isSuccess);
        const afterRead = await api(guardian, 'GET', '/api/soccer/notifications/me');
        check('unread decreased', afterRead.data.unreadCount === gdnNoti.data.unreadCount - 1,
            `${gdnNoti.data.unreadCount} -> ${afterRead.data.unreadCount}`);
    }

    //.// 거절 경로 (두 번째 선수)
    const rej = await api(guardian, 'POST', '/api/soccer/claim/me/requests', { code: CODE_REJECT, relation: 'Father' });
    check('second request created', rej.isSuccess && rej.data.status === 'Pending');
    const rejected = await api(manager, 'POST', '/api/soccer/claim/requests/review',
        { requestId: rej.data.requestId, approve: false });
    check('rejected', rejected.isSuccess && rejected.data.status === 'Rejected');
    const gdnNoti3 = await api(guardian, 'GET', '/api/soccer/notifications/me');
    check('guardian ClaimRejected notification', gdnNoti3.data?.items?.some(i => i.type === 'ClaimRejected' && i.playerName === '최시우'));
    // 거절은 코드를 소진하지 않는다 — 재신청 가능
    const reLookup = await api(guardian, 'GET', `/api/soccer/claim/invite/${CODE_REJECT}`);
    check('rejected code still valid (retry possible)', reLookup.isSuccess);

    //.// 친선경기 결과 알림 — 방금 연결된 박도윤의 보호자에게 (MatchResult 기본 켬)
    const match1 = await api(manager, 'POST', '/api/soccer/team/me/matches', {
        opponentName: '알림검증FC', ourScore: 2, opponentScore: 1,
        matchedAt: '2026-07-20T14:00:00', goals: [],
    });
    check('friendly match saved', match1.isSuccess, match1.codeName ?? match1.message);
    const gdnNoti4 = await api(guardian, 'GET', '/api/soccer/notifications/me');
    const matchNoti = gdnNoti4.data?.items?.find(i => i.type === 'MatchResult' && i.actorName === '알림검증FC');
    check('MatchResult notification sent', !!matchNoti && matchNoti.metaText === '2:1' && matchNoti.playerName === '박도윤');

    // 설정 off → 두 번째 경기는 발송 안 됨
    await api(guardian, 'PUT', '/api/auth/me/notifications', { itemName: 'MatchResult', isEnabled: false });
    const match2 = await api(manager, 'POST', '/api/soccer/team/me/matches', {
        opponentName: '알림검증FC2', ourScore: 0, opponentScore: 0,
        matchedAt: '2026-07-21T14:00:00', goals: [],
    });
    check('second match saved', match2.isSuccess);
    const gdnNoti5 = await api(guardian, 'GET', '/api/soccer/notifications/me');
    check('MatchResult suppressed when setting off',
        !gdnNoti5.data?.items?.some(i => i.type === 'MatchResult' && i.actorName === '알림검증FC2'));
    await api(guardian, 'PUT', '/api/auth/me/notifications', { itemName: 'MatchResult', isEnabled: true });

    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
