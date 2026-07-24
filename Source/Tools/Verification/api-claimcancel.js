// ClaimFlow 요청 취소 (P1) 검증 — 코드 없는 요청 생성 → 취소 → 재조회 없음 · 남의 요청 취소 거부.
const { execFileSync } = require('child_process');
const BASE = 'http://localhost:5000';
const MANAGER_ID = '55E9A639-83E2-45F8-B9E4-C717C276678F';
const GUARDIAN_ID = 'A0000000-0000-0000-0000-000000000D01';

const sql = q => execFileSync('sqlcmd', ['-S', '.\\SQLEXPRESS', '-d', 'PlayGround_Soccer', '-E', '-h', '-1', '-W', '-f', '65001', '-Q', 'SET NOCOUNT ON; ' + q], { encoding: 'utf8' }).trim();
async function login(e) { const r = await fetch(BASE + '/api/auth/login/email', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: e, password: 'password123!' }) }); return (await r.json())?.data?.accessToken; }
const H = t => ({ 'Content-Type': 'application/json', Authorization: 'Bearer ' + t });
const post = async (t, u, b) => await (await fetch(BASE + u, { method: 'POST', headers: H(t), body: JSON.stringify(b ?? {}) })).json().catch(() => null);
const del = async (t, u) => await (await fetch(BASE + u, { method: 'DELETE', headers: H(t) })).json().catch(() => null);
const get = async (t, u) => (await (await fetch(BASE + u, { headers: H(t) })).json())?.data ?? null;

let pass = 0, fail = 0;
const check = (n, ok, x = '') => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${n}${x ? ' — ' + x : ''}`); ok ? pass++ : fail++; };

(async () => {
    const gt = await login('verify-player-u12@test.local');
    // 검증fc 미연결 선수 하나로 코드 없는 요청 생성
    const playerId = sql(`SELECT TOP 1 CONVERT(varchar(50),p.PlayerId) FROM SoccerPlayers p JOIN SoccerTeamPlayers tp ON tp.PlayerId=p.PlayerId AND tp.Status='Active' AND tp.DeletedAt IS NULL WHERE p.UserId IS NULL AND p.DeletedAt IS NULL AND tp.TeamId=(SELECT TOP 1 TeamId FROM SoccerTeams WHERE ManagerUserId='${MANAGER_ID}')`);
    const created = await post(gt, '/api/soccer/claim/me/requests/by-player', { playerId, relation: 'Mother' });
    const reqId = created?.data?.requestId;
    check('요청 생성', !!reqId);

    // 재방문 복원에 Pending 나옴
    const before = await get(gt, '/api/soccer/claim/me/request');
    check('취소 전 대기 요청 존재', before?.status === 'Pending');

    // 남의 요청 취소 시도 (다른 계정) → 거부
    const other = await login('verify-teamadmin-0713@test.local');
    const bad = await del(other, `/api/soccer/claim/me/requests/${reqId}`);
    check('남의 요청 취소 거부', bad?.isSuccess === false);

    // 본인 취소 → 성공
    const ok = await del(gt, `/api/soccer/claim/me/requests/${reqId}`);
    check('본인 요청 취소 성공', ok?.isSuccess === true);

    // 재조회 시 대기 요청 사라짐(소프트 삭제)
    const after = await get(gt, '/api/soccer/claim/me/request');
    check('취소 후 대기 요청 없음', after === null || after?.status !== 'Pending', after?.status ?? 'null');

    // 관리자 알림 읽음 처리 확인
    const unread = sql(`SELECT COUNT(*) FROM SoccerNotifications WHERE NotificationType='ClaimRequest' AND RefId='${reqId}' AND IsRead=0`);
    check('관리자 알림 읽음 처리', unread === '0');

    // 취소 후 재요청 가능(멱등 아님 — 새 요청)
    const again = await post(gt, '/api/soccer/claim/me/requests/by-player', { playerId, relation: 'Mother' });
    check('취소 후 재요청 가능', again?.isSuccess === true);

    // 정리
    sql(`DELETE FROM SoccerNotifications WHERE RefId IN (SELECT RequestId FROM SoccerPlayerClaimRequests WHERE RequesterUserId='${GUARDIAN_ID}')`);
    sql(`DELETE FROM SoccerPlayerClaimRequests WHERE RequesterUserId='${GUARDIAN_ID}'`);
    console.log('\n정리: 검증 요청·알림 삭제');
    console.log(`\n${pass} PASS / ${fail} FAIL`);
})();
