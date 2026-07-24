// 온보딩 중복 방지 검증 — 이미 팀이 있는 관리자가 팀 생성을 재요청해도 2번째 팀이 생기지 않는다.
const { execFileSync } = require('child_process');
const BASE = 'http://localhost:5000';
const MANAGER_ID = '55E9A639-83E2-45F8-B9E4-C717C276678F';

const sql = q => execFileSync('sqlcmd',
    ['-S', '.\\SQLEXPRESS', '-d', 'PlayGround_Soccer', '-E', '-h', '-1', '-W', '-f', '65001', '-Q', 'SET NOCOUNT ON; ' + q],
    { encoding: 'utf8' }).trim();

async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    return (await r.json())?.data?.accessToken ?? null;
}

let pass = 0, fail = 0;
const check = (n, ok, x = '') => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${n}${x ? ' — ' + x : ''}`); ok ? pass++ : fail++; };

(async () => {
    const t = await login('verify-teamadmin-0713@test.local'); // 이미 검증fc 보유

    const before = sql(`SELECT COUNT(*) FROM SoccerTeams WHERE ManagerUserId='${MANAGER_ID}' AND DeletedAt IS NULL`);
    console.log('생성 전 팀 수:', before);

    // 다른 이름으로 팀 생성 재요청 → 멱등: 기존 팀을 반환하고 2번째를 만들지 않아야 한다
    const r = await fetch(BASE + '/api/soccer/team/me', {
        method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + t },
        body: JSON.stringify({ teamName: '중복시도FC', teamType: '클럽', region: '서울', roster: [] }),
    });
    const body = await r.json().catch(() => null);
    check('생성 요청 성공(멱등 반환)', body?.isSuccess === true, body?.data?.slug);

    const after = sql(`SELECT COUNT(*) FROM SoccerTeams WHERE ManagerUserId='${MANAGER_ID}' AND DeletedAt IS NULL`);
    check('팀 수 그대로(2번째 생성 안 됨)', after === before, `${before} → ${after}`);

    const dup = sql(`SELECT COUNT(*) FROM SoccerTeams WHERE ManagerUserId='${MANAGER_ID}' AND TeamName='중복시도FC' AND DeletedAt IS NULL`);
    check('새 이름 팀 미생성', dup === '0');

    // 반환된 슬러그가 기존 팀(검증fc) 것인지
    const existSlug = sql(`SELECT TOP 1 Slug FROM SoccerTeams WHERE ManagerUserId='${MANAGER_ID}' AND DeletedAt IS NULL ORDER BY CreatedAt`);
    check('반환 = 기존 팀 슬러그', body?.data?.slug === existSlug, `${body?.data?.slug} vs ${existSlug}`);

    console.log(`\n${pass} PASS / ${fail} FAIL`);
})();
