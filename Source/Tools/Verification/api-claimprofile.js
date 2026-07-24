// 코드 없이(공개 선수 프로필 경유) 연결 검증 (Design.ClaimFlow "코드가 없어요").
// 슬러그 카드 조회 → 코드 없는 요청 생성 → 팀 관리자 승인 → 선수가 보호자에 직접 연결(코드 소진 없이).
const { execFileSync } = require('child_process');
const BASE = 'http://localhost:5000';

const sql = q => execFileSync('sqlcmd',
    ['-S', '.\\SQLEXPRESS', '-d', 'PlayGround_Soccer', '-E', '-h', '-1', '-W', '-s', '|', '-f', '65001', '-Q', 'SET NOCOUNT ON; ' + q],
    { encoding: 'utf8' }).trim();

async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json())?.data?.accessToken ?? null;
}
const get = async (t, u) => (await (await fetch(BASE + u, { headers: { Authorization: 'Bearer ' + t } })).json())?.data ?? null;
const post = async (t, u, b) => {
    const r = await fetch(BASE + u, { method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + t }, body: JSON.stringify(b ?? {}) });
    return await r.json().catch(() => null);
};

let pass = 0, fail = 0;
const check = (n, ok, x = '') => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${n}${x ? ' — ' + x : ''}`); ok ? pass++ : fail++; };

const GUARDIAN = 'verify-player-u12@test.local';        // 신준우 보호자 (D01) — 새 선수를 코드 없이 연결
const MANAGER = 'verify-teamadmin-0713@test.local';     // 검증fc 관리자 (승인자)
const MANAGER_ID = '55E9A639-83E2-45F8-B9E4-C717C276678F';
const GUARDIAN_ID = 'A0000000-0000-0000-0000-000000000D01';

(async () => {
    // 검증fc의 미연결 공개 선수 하나 (슬러그는 hex로 받아 한글 인코딩 깨짐 방지)
    const row = sql(`SELECT TOP 1 CONVERT(varchar(max), CONVERT(varbinary(max), p.Slug), 2) + '|' + CONVERT(varchar(50),p.PlayerId) FROM SoccerPlayers p JOIN SoccerTeamPlayers tp ON tp.PlayerId=p.PlayerId AND tp.Status='Active' AND tp.DeletedAt IS NULL WHERE p.UserId IS NULL AND p.DeletedAt IS NULL AND tp.TeamId=(SELECT TOP 1 TeamId FROM SoccerTeams WHERE ManagerUserId='${MANAGER_ID}')`);
    const [slugHex, playerId] = row.split('|');
    const slug = Buffer.from(slugHex, 'hex').toString('utf8');
    console.log('대상: slug=' + slug + ' playerId=' + playerId);

    const gt = await login(GUARDIAN);

    // 1) 슬러그 카드 조회 (미연결 선수만)
    const card = await get(gt, `/api/soccer/claim/card?slug=${encodeURIComponent(slug)}`);
    check('슬러그 카드 조회', card?.playerId?.toLowerCase() === playerId.toLowerCase(), card?.name);

    // 2) 코드 없이 연결 요청 생성
    const created = await post(gt, '/api/soccer/claim/me/requests/by-player', { playerId, relation: 'Mother' });
    check('코드 없는 요청 생성', created?.isSuccess === true, created?.data?.status);

    // 멱등 — 재요청 시 같은 요청
    const again = await post(gt, '/api/soccer/claim/me/requests/by-player', { playerId, relation: 'Mother' });
    check('멱등 재요청', again?.isSuccess === true);

    // 3) 팀 관리자 알림에 ClaimRequest 도착
    const mt = await login(MANAGER);
    const notis = await get(mt, '/api/soccer/notifications/me');
    const claimNoti = (notis?.items ?? []).find(n => n.type === 'ClaimRequest' && n.refId?.toLowerCase() === created.data.requestId.toLowerCase());
    check('관리자 알림에 연결 요청 도착', !!claimNoti);

    // 4) 승인 → 선수가 보호자에 직접 연결 (코드 소진 없이)
    const reviewed = await post(mt, '/api/soccer/claim/requests/review', { requestId: created.data.requestId, approve: true });
    check('승인 성공', reviewed?.isSuccess === true, reviewed?.data?.status);

    // 5) 선수가 보호자(D01) 소유가 됐는지
    const owner = sql(`SELECT CONVERT(varchar(50), UserId) FROM SoccerPlayers WHERE PlayerId='${playerId}'`);
    check('승인 후 선수가 보호자에 연결됨', owner.toLowerCase() === GUARDIAN_ID.toLowerCase(), owner);
    const fam = sql(`SELECT COUNT(*) FROM SoccerPlayerFamilyLinks WHERE PlayerId='${playerId}' AND UserId='${GUARDIAN_ID}' AND Role='Guardian' AND DeletedAt IS NULL`);
    check('가족 연결(Guardian) 생성', fam === '1');
    const invRequest = sql(`SELECT CASE WHEN InviteId IS NULL THEN 'null' ELSE 'set' END FROM SoccerPlayerClaimRequests WHERE RequestId='${created.data.requestId}'`);
    check('요청 InviteId = NULL (코드 없음)', invRequest === 'null');

    // 정리 — 원복 (선수 미연결로, 가족 연결·요청·알림 삭제)
    sql(`UPDATE SoccerPlayers SET UserId=NULL, IsGuardianManaged=0 WHERE PlayerId='${playerId}'`);
    sql(`DELETE FROM SoccerPlayerFamilyLinks WHERE PlayerId='${playerId}' AND UserId='${GUARDIAN_ID}'`);
    sql(`DELETE FROM SoccerNotifications WHERE RefId='${created.data.requestId}'`);
    sql(`DELETE FROM SoccerPlayerClaimRequests WHERE RequestId='${created.data.requestId}'`);
    console.log('\n정리: 선수 미연결 복원 · 가족 연결·요청·알림 삭제');
    console.log(`\n${pass} PASS / ${fail} FAIL`);
})();
