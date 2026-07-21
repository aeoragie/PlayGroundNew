// 모집 공고 API 왕복 — 작성 → 공개 열람 반영 → 수정 → 마감(수정 불가) → 삭제/복구 →
// 팀 탐색 IsRecruiting 파생 → 경계(남의 계정·검증 규칙·과거 마감일). 끝나면 공고 전부 삭제(물리).
const BASE = 'http://localhost:5000';
const SLUG = encodeURIComponent('검증fc');

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
    const headers = { 'Content-Type': 'application/json' };
    if (token) headers.Authorization = `Bearer ${token}`;
    const r = await fetch(`${BASE}${path}`, { method, headers, body: body ? JSON.stringify(body) : undefined });
    return await r.json();
}

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

(async () => {
    const manager = await login('verify-teamadmin-0713@test.local');   // 검증fc
    const other = await login('verify-empty-0714@test.local');         // EmptyFC

    //.// 탐색 파생 — 공고 없을 때 모집중 아님
    const explore0 = await api(null, 'GET', '/api/soccer/teams');
    const team0 = explore0.data.teams.find(t => t.teamName === '검증fc');
    check('explore not recruiting before', team0 && team0.isRecruiting === false, JSON.stringify(team0?.isRecruiting));

    //.// 작성 (조건 칩 2 + 마감일)
    const deadline = new Date(Date.now() + 10 * 86400000).toISOString().slice(0, 10);
    const created = await api(manager, 'POST', '/api/soccer/team/me/recruitments', {
        recruitmentId: EMPTY_GUID, title: 'U15 공격수 모집',
        description: '최전방 스트라이커 1명을 찾습니다. 9월 리그 등록 가능자, 주전 기회가 열려 있습니다.',
        conditions: ['테스트 1회 · 주말', '9월 리그 등록 가능'], deadlineDate: deadline,
    });
    check('create open', created.isSuccess && created.data.isOpen === true, created.codeName);
    const id = created.data?.recruitmentId;

    //.// 검증 규칙 — 제목 없음 / 조건 5개 / 과거 마감일
    const noTitle = await api(manager, 'POST', '/api/soccer/team/me/recruitments',
        { recruitmentId: EMPTY_GUID, title: ' ', description: 'x', conditions: [] });
    check('empty title denied', noTitle.isSuccess === false && noTitle.codeName === 'InvalidInput', noTitle.codeName);
    const tooMany = await api(manager, 'POST', '/api/soccer/team/me/recruitments',
        { recruitmentId: EMPTY_GUID, title: 't', description: 'd', conditions: ['1', '2', '3', '4', '5'] });
    check('5 conditions denied', tooMany.isSuccess === false && tooMany.codeName === 'InvalidInput', tooMany.codeName);
    const pastDeadline = await api(manager, 'POST', '/api/soccer/team/me/recruitments',
        { recruitmentId: EMPTY_GUID, title: 't', description: 'd', conditions: [], deadlineDate: '2026-01-01' });
    check('past deadline denied', pastDeadline.isSuccess === false && pastDeadline.codeName === 'InvalidInput', pastDeadline.codeName);

    //.// 공개 열람 (비로그인)
    const pub = await api(null, 'GET', `/api/soccer/team/${SLUG}/recruitments`);
    const pubItem = pub.data?.items?.find(i => i.recruitmentId === id);
    check('public tab shows posting', !!pubItem && pubItem.isOpen && pubItem.conditions.length === 2,
        JSON.stringify(pubItem?.conditions));

    //.// 탐색 파생 — 이제 모집중
    const explore1 = await api(null, 'GET', '/api/soccer/teams');
    const team1 = explore1.data.teams.find(t => t.teamName === '검증fc');
    check('explore recruiting after create', team1 && team1.isRecruiting === true);

    //.// 남의 계정 수정 시도 — 소유 아님 (EmptyFC 관리자에게는 이 공고가 없다)
    const foreign = await api(other, 'POST', '/api/soccer/team/me/recruitments',
        { recruitmentId: id, title: '탈취', description: 'x', conditions: [] });
    check('foreign edit denied', foreign.isSuccess === false && foreign.codeName === 'Forbidden', foreign.codeName);

    //.// 수정
    const edited = await api(manager, 'POST', '/api/soccer/team/me/recruitments', {
        recruitmentId: id, title: 'U15 공격수 모집', description: '수정된 본문입니다.',
        conditions: ['테스트 1회 · 주말'], deadlineDate: deadline,
    });
    check('edit ok', edited.isSuccess && edited.data.description === '수정된 본문입니다.');

    //.// 마감 → 공개 탭에서 마감 카드 + 수정 불가 + 탐색 파생 꺼짐
    const closed = await api(manager, 'POST', `/api/soccer/team/me/recruitments/${id}/close`);
    check('close ok', closed.isSuccess && closed.data.status === 'Closed' && closed.data.isOpen === false);
    const reClose = await api(manager, 'POST', `/api/soccer/team/me/recruitments/${id}/close`);
    check('double close denied', reClose.isSuccess === false, reClose.codeName);
    const editClosed = await api(manager, 'POST', '/api/soccer/team/me/recruitments',
        { recruitmentId: id, title: 'x', description: 'y', conditions: [] });
    check('edit closed denied', editClosed.isSuccess === false && editClosed.codeName === 'Forbidden', editClosed.codeName);
    const pub2 = await api(null, 'GET', `/api/soccer/team/${SLUG}/recruitments`);
    const pubItem2 = pub2.data.items.find(i => i.recruitmentId === id);
    check('public shows closed', pubItem2 && pubItem2.isOpen === false);
    const explore2 = await api(null, 'GET', '/api/soccer/teams');
    check('explore not recruiting after close',
        explore2.data.teams.find(t => t.teamName === '검증fc').isRecruiting === false);

    //.// 삭제 → 공개 탭에서 사라짐 → 복구(실행취소) → 돌아옴
    const del = await api(manager, 'POST', `/api/soccer/team/me/recruitments/${id}/delete?restore=false`);
    check('delete ok', del.isSuccess);
    const pub3 = await api(null, 'GET', `/api/soccer/team/${SLUG}/recruitments`);
    check('public hides deleted', !pub3.data.items.some(i => i.recruitmentId === id));
    const restore = await api(manager, 'POST', `/api/soccer/team/me/recruitments/${id}/delete?restore=true`);
    check('restore ok', restore.isSuccess);
    const pub4 = await api(null, 'GET', `/api/soccer/team/${SLUG}/recruitments`);
    check('public shows restored', pub4.data.items.some(i => i.recruitmentId === id));

    console.log(failed ? 'RESULT: FAIL' : 'RESULT: ALL PASS');
    process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
