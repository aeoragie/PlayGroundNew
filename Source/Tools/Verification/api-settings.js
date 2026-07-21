// 설정 API 검증 — ① 설정 조회(마스킹) ② 알림 기본값 병합 ③ 저장 왕복 ④ 잠금 알림 서버 거부
// ⑤ 계정 삭제(임시 계정 — 삭제 후 중복 삭제 거부, 검증 후 물리 삭제는 SQL로 정리)
const BASE = 'http://localhost:5000';

async function login(email, password) {
    const r = await fetch(`${BASE}/api/auth/login/email`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
    });
    const env = await r.json();
    if (!env.isSuccess) throw new Error(`login failed: ${email} ${env.message}`);
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

function check(name, cond, detail) {
    console.log(`${cond ? 'PASS' : 'FAIL'} ${name}${detail ? ' — ' + detail : ''}`);
    if (!cond) process.exitCode = 1;
}

(async () => {
    const token = await login('verify-teamadmin-0713@test.local', 'password123!');

    // ① 설정 조회 — 이메일 마스킹
    const settings = await api(token, 'GET', '/api/auth/me/settings');
    check('settings isSuccess', settings.isSuccess);
    check('masked email', settings.data?.maskedEmail === 'ver***@test.local', settings.data?.maskedEmail);
    check('displayName present', !!settings.data?.displayName, settings.data?.displayName);
    check('authProvider', settings.data?.authProvider === 'Local', settings.data?.authProvider);

    // ② 알림 기본값 병합 — 6항목 전부 + 기본값
    const noti = await api(token, 'GET', '/api/auth/me/notifications');
    const prefs = Object.fromEntries((noti.data?.preferences ?? []).map(p => [p.itemName, p.isEnabled]));
    check('notifications 6 items', (noti.data?.preferences ?? []).length === 6, JSON.stringify(prefs));
    check('defaults', prefs.PushChannel === true && prefs.EmailChannel === false
        && prefs.MatchResult === true && prefs.Recruit === true && prefs.Review === true
        && prefs.VisitSummary === false);

    // ③ 저장 왕복 — VisitSummary on → 재조회 true → off 원복
    const set1 = await api(token, 'PUT', '/api/auth/me/notifications', { itemName: 'VisitSummary', isEnabled: true });
    check('set VisitSummary=true', set1.isSuccess);
    const noti2 = await api(token, 'GET', '/api/auth/me/notifications');
    const visit2 = noti2.data.preferences.find(p => p.itemName === 'VisitSummary');
    check('roundtrip VisitSummary=true', visit2.isEnabled === true);
    const set2 = await api(token, 'PUT', '/api/auth/me/notifications', { itemName: 'VisitSummary', isEnabled: false });
    check('restore VisitSummary=false', set2.isSuccess);

    // ④ 잠금(승인형) 알림 — 어떤 이름으로도 저장 거부 (enum 화이트리스트)
    for (const name of ['ApprovalRequest', 'ConnectionRequest', 'ViewRequest', 'Approval', '0']) {
        const denied = await api(token, 'PUT', '/api/auth/me/notifications', { itemName: name, isEnabled: false });
        check(`locked item denied: ${name}`, denied.isSuccess === false, denied.codeName);
    }

    // ⑤ 계정 삭제 — 임시 계정(find-or-create) 생성 → 삭제 성공 → 중복 삭제 거부
    const tmpToken = await login('verify-delete-0721@test.local', 'password123!');
    const del1 = await api(tmpToken, 'DELETE', '/api/auth/me');
    check('delete account', del1.isSuccess);
    const del2 = await api(tmpToken, 'DELETE', '/api/auth/me');
    check('double delete denied', del2.isSuccess === false, del2.codeName);
    // 삭제된 계정은 설정 조회도 NotFound
    const afterDel = await api(tmpToken, 'GET', '/api/auth/me/settings');
    check('deleted account settings denied', afterDel.isSuccess === false, afterDel.codeName);

    console.log('done');
})().catch(e => { console.error(e); process.exit(1); });
