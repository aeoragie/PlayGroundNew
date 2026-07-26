// 팀 게시판 검증 (Design.TeamBoard). 공지 발행→보호자 알림→공개 전환→소개 탭 반영 왕복 + 경계.
// 관리자 = verify-u15-1 (광주광주FCU15, slug gwangjugwangjufcu15) / 보호자 = verify-player-u15 (김정현 보호자).
// 끝나고 생성 글·알림·읽음은 SQL로 물리 삭제(원복) — 아래 sql-teamboard-cleanup.sql.
const BASE = 'http://localhost:5000';
const SLUG = 'gwangjugwangjufcu15';
const CHILD = '3CA3649B-694C-402C-8C4A-2F5920724F07';

async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json())?.data?.accessToken ?? null;
}

const H = (t) => ({ 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + t });
const j = async (r) => await r.json().catch(() => null);

const listPosts = async (t) => (await j(await fetch(BASE + '/api/soccer/team/me/posts', { headers: H(t) })))?.data?.posts ?? [];
const savePost = async (t, body) => await j(await fetch(BASE + '/api/soccer/team/me/posts', { method: 'POST', headers: H(t), body: JSON.stringify(body) }));
const setPin = async (t, id, pinned) => await j(await fetch(BASE + `/api/soccer/team/me/posts/${id}/pin?pinned=${pinned}`, { method: 'POST', headers: H(t) }));
const setPublic = async (t, id, visible) => await j(await fetch(BASE + `/api/soccer/team/me/posts/${id}/public?visible=${visible}`, { method: 'POST', headers: H(t) }));
const delPost = async (t, id, restore = false) => await j(await fetch(BASE + `/api/soccer/team/me/posts/${id}/delete?restore=${restore}`, { method: 'POST', headers: H(t) }));
const news = async (slug) => (await j(await fetch(BASE + `/api/soccer/team/${encodeURIComponent(slug)}/news`)))?.data?.items ?? [];
const childPosts = async (t, pid) => (await j(await fetch(BASE + `/api/soccer/team/me/child-posts?playerId=${pid}`, { headers: H(t) })))?.data ?? null;
const markRead = async (t, id) => (await j(await fetch(BASE + `/api/soccer/team/me/posts/${id}/read`, { method: 'POST', headers: H(t) })))?.isSuccess ?? false;
const notifications = async (t) => (await j(await fetch(BASE + '/api/soccer/notifications/me', { headers: H(t) })))?.data ?? { unreadCount: 0, items: [] };

let pass = 0, fail = 0;
const check = (name, ok, extra = '') => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${extra ? ' — ' + extra : ''}`); ok ? pass++ : fail++; };

(async () => {
    const mgr = await login('verify-u15-1@test.local');
    const guardian = await login('verify-player-u15@test.local');
    const other = await login('verify-u15-2@test.local');
    if (!mgr || !guardian || !other) { console.log('login FAILED', { mgr: !!mgr, guardian: !!guardian, other: !!other }); return; }

    const before = await listPosts(mgr);
    const beforeCount = before.length;

    //.// 1) 공지 발행 (신규) — 기본 비공개, 조회수 0
    const created = await savePost(mgr, { type: 'Notice', title: '[TB] 8월 훈련 일정 변경', body: '화목 훈련이 19시로 변경됩니다.', isPublic: false, files: [] });
    check('공지 발행 성공', created?.isSuccess === true);
    const post = created?.data;
    check('반환 유형 Notice', post?.type === 'Notice');
    check('기본 비공개', post?.isPublic === false);
    check('조회수 0', post?.viewCount === 0);

    const afterCreate = await listPosts(mgr);
    check('목록 +1', afterCreate.length === beforeCount + 1, `${beforeCount}→${afterCreate.length}`);

    //.// 2) 보호자 알림 — TeamNotice 수신 (전원 발송)
    const noti = await notifications(guardian);
    const tn = noti.items.find(n => n.type === 'TeamNotice' && n.refId === post.postId);
    check('보호자에 TeamNotice 알림 도착', !!tn, tn ? `meta=${tn.metaText}` : 'none');
    check('알림 스냅샷 = 글 제목', tn?.metaText === '[TB] 8월 훈련 일정 변경');

    //.// 3) 공개 전 — 소개 탭 소식 없음
    const newsBefore = await news(SLUG);
    check('공개 전 소식 미노출', !newsBefore.some(n => n.postId === post.postId));

    //.// 4) 공개 전환 → 소개 탭 소식 반영 (파일명만·URL 없음)
    const pub = await setPublic(mgr, post.postId, true);
    check('공개 전환 성공', pub?.isSuccess === true && pub?.data?.isPublic === true);
    const newsAfter = await news(SLUG);
    const np = newsAfter.find(n => n.postId === post.postId);
    check('공개 후 소식 노출', !!np, np ? `title=${np.title}` : 'none');
    check('공개 소식에 관리정보 없음', np && np.type === undefined && np.authorName === undefined);

    //.// 5) 보호자 뷰 — 글 present, 안읽음
    const cp1 = await childPosts(guardian, CHILD);
    const gp = cp1?.posts?.find(p => p.postId === post.postId);
    check('보호자 뷰에 글 존재', !!gp);
    check('보호자 뷰 안읽음', gp?.isRead === false);
    check('보호자 뷰 팀명', typeof cp1?.teamName === 'string' && cp1.teamName.length > 0, `team=${cp1?.teamName}`);

    //.// 6) 읽음 처리 → 안읽음 해제
    const read = await markRead(guardian, post.postId);
    check('읽음 처리 성공', read);
    const cp2 = await childPosts(guardian, CHILD);
    check('읽음 후 isRead=true', cp2?.posts?.find(p => p.postId === post.postId)?.isRead === true);

    //.// 7) 자료(Material)는 알림 없음
    const mat = await savePost(mgr, { type: 'Material', title: '[TB] 여름캠프 동의서', body: '첨부를 확인해 주세요.', isPublic: false, files: [] });
    const matId = mat?.data?.postId;
    const noti2 = await notifications(guardian);
    check('자료는 알림 없음', !noti2.items.some(n => n.refId === matId));

    //.// 8) 고정 최대 2개 — 3번째 고정 거부
    const p2 = (await savePost(mgr, { type: 'Notice', title: '[TB] 고정테스트A', body: '내용내용', isPublic: false, files: [] }))?.data;
    const p3 = (await savePost(mgr, { type: 'Notice', title: '[TB] 고정테스트B', body: '내용내용', isPublic: false, files: [] }))?.data;
    check('고정 1', (await setPin(mgr, post.postId, true))?.isSuccess === true);
    check('고정 2', (await setPin(mgr, p2.postId, true))?.isSuccess === true);
    check('고정 3 거부(최대 2)', (await setPin(mgr, p3.postId, true))?.isSuccess === false);
    check('고정 해제는 허용', (await setPin(mgr, post.postId, false))?.isSuccess === true);

    //.// 9) 입력 검증 — 제목 짧음·본문 짧음·유형 미지·외부 URL
    check('제목 1자 거부', (await savePost(mgr, { type: 'Notice', title: 'a', body: '정상내용', isPublic: false, files: [] }))?.isSuccess === false);
    check('본문 1자 거부', (await savePost(mgr, { type: 'Notice', title: '정상제목', body: 'a', isPublic: false, files: [] }))?.isSuccess === false);
    check('유형 미지 거부', (await savePost(mgr, { type: 'Weird', title: '정상제목', body: '정상내용', isPublic: false, files: [] }))?.isSuccess === false);
    check('외부 첨부 URL 거부', (await savePost(mgr, { type: 'Notice', title: '정상제목', body: '정상내용', isPublic: false, files: [{ url: 'http://evil.com/x.pdf', name: 'x.pdf', sizeBytes: 10 }] }))?.isSuccess === false);

    //.// 10) 남의 팀 관리자는 삭제 거부
    check('남의 팀 삭제 거부', (await delPost(other, post.postId))?.isSuccess === false);

    //.// 11) 삭제 → 목록에서 사라짐 → 복구 → 다시 나타남 / 공개 글은 소식에서도 사라짐
    check('삭제 성공', (await delPost(mgr, post.postId))?.isSuccess === true);
    check('삭제 후 목록에서 사라짐', !(await listPosts(mgr)).some(p => p.postId === post.postId));
    check('공개 글 삭제 → 소식에서도 사라짐', !(await news(SLUG)).some(n => n.postId === post.postId));
    check('복구 성공', (await delPost(mgr, post.postId, true))?.isSuccess === true);
    check('복구 후 다시 나타남', (await listPosts(mgr)).some(p => p.postId === post.postId));

    //.// 12) 남의 자녀 playerId로 보호자 뷰 조회 = 빈 결과(자격 없음)
    const otherChild = await childPosts(mgr, CHILD); // 관리자는 이 자녀의 보호자가 아니다
    check('남의 자녀 보호자 뷰 빈 결과', (otherChild?.posts?.length ?? 0) === 0);

    console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
    process.exitCode = fail > 0 ? 1 : 0;
})();
