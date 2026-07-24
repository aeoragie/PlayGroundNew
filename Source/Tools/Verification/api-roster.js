// 로스터 쓰기 검증 (선수 추가·내보내기·복구). Design.TeamDashboard §2.
// 확인: 추가→목록 반영·초대코드 발급 / 등번호 숫자 검증 / 연령대 화이트리스트 / 내보내기→목록에서 사라짐 →
//       복구→다시 나타남·코드 되살아남 / 남의 팀 teamPlayerId 거부.
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
const roster = async () => (await (await fetch(BASE + '/api/soccer/team/me/roster', { headers: H() })).json())?.data?.players ?? [];
const add = async body => {
    const r = await fetch(BASE + '/api/soccer/team/me/roster/players', { method: 'POST', headers: H(), body: JSON.stringify(body) });
    return await r.json().catch(() => null);
};
const remove = async (id, restore = false) => {
    const r = await fetch(BASE + `/api/soccer/team/me/roster/players/${id}?restore=${restore}`, { method: 'DELETE', headers: H() });
    return (await r.json().catch(() => null))?.isSuccess ?? false;
};

let pass = 0, fail = 0;
const check = (name, ok, extra = '') => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${extra ? ' — ' + extra : ''}`); ok ? pass++ : fail++; };

(async () => {
    TOKEN = await login('verify-teamadmin-0713@test.local');
    if (!TOKEN) { console.log('login FAILED'); return; }

    const before = await roster();
    const beforeCount = before.length;

    //.// 1) 정상 추가 — 목록 +1, Unclaimed, 초대코드 발급
    const res = await add({ name: '검증추가선수', jerseyNumber: '99', position: 'FW', grade: '중2', ageGroup: 'U15' });
    const dto = res?.data;
    check('추가 성공', res?.isSuccess === true);
    check('반환 = Unclaimed', dto?.claimStatus === 'Unclaimed', `claim=${dto?.claimStatus}`);
    check('초대코드 발급됨', typeof dto?.inviteCode === 'string' && dto.inviteCode.length === 6, `code=${dto?.inviteCode}`);
    check('연령대 반영', dto?.ageGroup === 'U15');

    const afterAdd = await roster();
    check('목록 +1', afterAdd.length === beforeCount + 1, `${beforeCount}→${afterAdd.length}`);
    const added = afterAdd.find(p => p.teamPlayerId === dto?.teamPlayerId);
    check('목록에 새 선수 존재', !!added && added.name === '검증추가선수');

    //.// 2) 등번호 숫자 검증 — 문자면 거부
    const badJersey = await add({ name: '숫자아님', jerseyNumber: 'AB' });
    check('등번호 문자 거부', badJersey?.isSuccess === false);

    //.// 3) 연령대 화이트리스트 — U99 거부
    const badAge = await add({ name: '연령오류', ageGroup: 'U99' });
    check('연령대 화이트리스트 거부', badAge?.isSuccess === false);

    //.// 4) 이름 없으면 거부
    const noName = await add({ name: '  ' });
    check('빈 이름 거부', noName?.isSuccess === false);

    //.// 5) 내보내기 → 목록에서 사라짐, 코드 회수
    const removed = await remove(dto.teamPlayerId);
    check('내보내기 성공', removed);
    const afterRemove = await roster();
    check('목록에서 사라짐', !afterRemove.some(p => p.teamPlayerId === dto.teamPlayerId), `count=${afterRemove.length}`);

    //.// 6) 복구 → 다시 나타남
    const restored = await remove(dto.teamPlayerId, true);
    check('복구 성공', restored);
    const afterRestore = await roster();
    const back = afterRestore.find(p => p.teamPlayerId === dto.teamPlayerId);
    check('복구 후 다시 나타남', !!back);
    check('복구 후 초대코드 되살아남', typeof back?.inviteCode === 'string' && back.inviteCode.length === 6, `code=${back?.inviteCode}`);

    //.// 7) 남의 팀 teamPlayerId 내보내기 거부 (다른 관리자 계정)
    TOKEN = await login('verify-u15-1@test.local');
    const forbidden = await remove(dto.teamPlayerId);
    check('남의 팀 선수 내보내기 거부', forbidden === false);

    //.// 정리 — 검증 선수 물리 제거는 SQL로(아래 안내). 여기서는 소프트 삭제로 숨긴다.
    TOKEN = await login('verify-teamadmin-0713@test.local');
    await remove(dto.teamPlayerId);
    console.log(`\n정리: 검증 선수 TeamPlayerId=${dto.teamPlayerId} PlayerId=${dto.playerId} (물리 삭제는 sql-roster-cleanup.sql)`);

    console.log(`\n${pass} PASS / ${fail} FAIL`);
})();
