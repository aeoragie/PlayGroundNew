// 알림 센터 페이지 API 검증 (DECISION.NOTIFICATIONCENTER). 세그먼트 필터·페이지네이션·카운트·bulk 읽음·90일 정리.
// 계정 = verify-player-u15 (보호자, 시드 26건 중 100일 경과 1건은 90일 정리로 삭제 → 25건).
const BASE = 'http://localhost:5000';

async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json())?.data?.accessToken ?? null;
}
const H = (t) => ({ 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + t });
const page = async (t, filter, offset, limit) =>
    (await (await fetch(BASE + `/api/soccer/notifications/me/page?filter=${filter}&offset=${offset}&limit=${limit}`, { headers: H(t) })).json())?.data ?? null;
const markBulk = async (t, ids) =>
    (await (await fetch(BASE + '/api/soccer/notifications/me/read', { method: 'PUT', headers: H(t), body: JSON.stringify({ notificationIds: ids }) })).json())?.isSuccess ?? false;
const bell = async (t) => (await (await fetch(BASE + '/api/soccer/notifications/me', { headers: H(t) })).json())?.data ?? null;

let pass = 0, fail = 0;
const check = (name, ok, extra = '') => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${extra ? ' — ' + extra : ''}`); ok ? pass++ : fail++; };

(async () => {
    const t = await login('verify-player-u15@test.local');
    if (!t) { console.log('login FAILED'); return; }

    //.// 1) 전체 필터 — 90일 정리 후 25건, action=1, page1=20
    const all = await page(t, 'all', 0, 20);
    check('전체 카운트 25 (90일 정리 반영)', all?.totalCount === 25, `total=${all?.totalCount}`);
    check('처리 필요 카운트 1', all?.actionRequiredCount === 1, `action=${all?.actionRequiredCount}`);
    check('페이지 크기 20', all?.items?.length === 20, `len=${all?.items?.length}`);

    //.// 2) 더 보기 — offset 20 → 나머지 5
    const more = await page(t, 'all', 20, 20);
    check('더 보기 offset 20 → 5건', more?.items?.length === 5, `len=${more?.items?.length}`);

    //.// 3) 처리 필요 필터 — 1건(ClaimRequest Pending)
    const action = await page(t, 'action', 0, 20);
    check('처리 필요 필터 1건', action?.items?.length === 1, `len=${action?.items?.length}`);
    check('처리 필요 = 액션형', action?.items?.[0]?.type === 'ClaimRequest');
    check('액션 라이브 상태 Pending', action?.items?.[0]?.requestStatus === 'Pending');

    //.// 4) 읽지 않음 필터 — unread 카운트만큼
    const unread = await page(t, 'unread', 0, 20);
    check('읽지 않음 필터 = unread 카운트', unread?.items?.length === all?.unreadCount, `len=${unread?.items?.length} count=${all?.unreadCount}`);
    check('읽지 않음은 전부 IsRead=false', unread?.items?.every(i => i.isRead === false));

    //.// 5) bulk 읽음 → 읽지 않음 카운트 감소
    const unreadIds = unread.items.map(i => i.notificationId);
    const beforeUnread = all.unreadCount;
    check('bulk 읽음 처리 성공', await markBulk(t, unreadIds));
    const after = await page(t, 'all', 0, 20);
    check('읽음 처리 후 unread 0', after?.unreadCount === 0, `unread=${after?.unreadCount}`);
    check('벨 카운트도 0으로', (await bell(t))?.unreadCount === 0);
    check('읽어도 total 불변(25)', after?.totalCount === 25, `total=${after?.totalCount}`);
    check('읽어도 action 불변(1)', after?.actionRequiredCount === 1, `action=${after?.actionRequiredCount}`);

    //.// 6) 잘못된 필터는 all로 폴백
    const bad = await page(t, 'weird', 0, 20);
    check('알 수 없는 필터 → all 폴백(25)', bad?.totalCount === 25);

    console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
    process.exitCode = fail > 0 ? 1 : 0;
})();
