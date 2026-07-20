// B4 권한 검증 — 보호자 / 팀 관리자 / 제3자 2종이 같은 선수에게 PUT을 시도한다.
const BASE = 'http://localhost:5000';
const PLAYER_ID = '3CA3649B-694C-402C-8C4A-2F5920724F07'; // 김정현 (광주광주FCU15)
const PHOTO = '/uploads/player-photo/202607/b4verify.jpg';

async function login(email) {
    const r = await fetch(BASE + '/api/auth/login/email', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password: 'password123!' }),
    });
    const j = await r.json();
    return j?.data?.accessToken ?? null;
}

async function setPhoto(token, photoUrl) {
    const r = await fetch(BASE + '/api/soccer/player/photo', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token },
        body: JSON.stringify({ playerId: PLAYER_ID, photoUrl }),
    });
    const j = await r.json().catch(() => null);
    return { http: r.status, isSuccess: j?.isSuccess ?? null, detail: j?.resultData?.detailCode ?? j?.detailCode ?? null };
}

async function currentPhoto() {
    // 보호자 계정으로 me/info를 읽어 실제 저장값과 CanEditPhoto를 확인
    const t = await login('verify-player-u15@test.local');
    const r = await fetch(BASE + '/api/soccer/player/me/info', { headers: { Authorization: 'Bearer ' + t } });
    const j = await r.json();
    return { photoUrl: j?.data?.profile?.photoUrl ?? null, canEditPhoto: j?.data?.profile?.canEditPhoto ?? null };
}

(async () => {
    const cases = [
        ['보호자      (verify-player-u15)', 'verify-player-u15@test.local', true],
        ['팀 관리자   (verify-u15-1)', 'verify-u15-1@test.local', true],
        ['제3자 팀관리(verify-teamadmin-0713)', 'verify-teamadmin-0713@test.local', false],
        ['제3자 보호자(verify-player-u12)', 'verify-player-u12@test.local', false],
    ];

    console.log('=== 시작 상태 ===', JSON.stringify(await currentPhoto()));

    for (const [label, email, expectAllow] of cases) {
        const token = await login(email);
        if (!token) { console.log(`${label}: 로그인 실패`); continue; }
        const res = await setPhoto(token, PHOTO);
        const allowed = res.isSuccess === true;
        const verdict = allowed === expectAllow ? 'OK' : '!!! 기대와 다름';
        console.log(`${label}: http=${res.http} isSuccess=${res.isSuccess} detail=${res.detail} → 기대=${expectAllow ? '허용' : '차단'} ${verdict}`);
    }

    console.log('=== 쓰기 후 상태 ===', JSON.stringify(await currentPhoto()));

    // 외부 URL 차단 (업로드 경로만 저장)
    const g = await login('verify-player-u15@test.local');
    console.log('외부 URL 저장 시도:', JSON.stringify(await setPhoto(g, 'https://evil.example.com/x.jpg')));

    // 삭제 → 이니셜 복귀
    console.log('삭제(null):', JSON.stringify(await setPhoto(g, null)));
    console.log('=== 삭제 후 상태 ===', JSON.stringify(await currentPhoto()));
})();
