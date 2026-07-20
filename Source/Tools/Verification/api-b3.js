// B3 API 왕복 — 추가→수정→삭제→실행취소 + 입력 거부(유튜브 아닌 링크·기간 역전) + 대표 승계
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

const put = async (url, body) => {
    const r = await fetch(BASE + url, { method: 'PUT', headers: H(), body: JSON.stringify(body) });
    const j = await r.json().catch(() => null);
    return { ok: j?.isSuccess ?? false, msg: j?.message ?? null };
};
const post = async (url, body) => {
    const r = await fetch(BASE + url, { method: 'POST', headers: H(), body: JSON.stringify(body) });
    const j = await r.json().catch(() => null);
    return { ok: j?.isSuccess ?? false, msg: j?.message ?? null };
};
const getCareer = async () => (await (await fetch(BASE + '/api/soccer/player/me/career', { headers: H() })).json())?.data?.entries ?? [];
const getVideos = async () => (await (await fetch(BASE + '/api/soccer/player/me/portfolio', { headers: H() })).json())?.data?.videos ?? [];

(async () => {
    // 신준우 = 커리어 1건 / 영상 0건 (시드)
    TOKEN = await login('verify-player-u12@test.local');
    console.log('token:', TOKEN ? 'OK' : 'FAILED');

    const before = await getCareer();
    console.log('시작 커리어:', before.length, '건');

    //.// 1) 커리어 추가
    console.log('추가:', JSON.stringify(await put('/api/soccer/player/me/career', {
        careerId: '00000000-0000-0000-0000-000000000000',
        teamName: 'B3 검증 FC', startDate: '2024-03-01', endDate: null,
        role: 'U12 · MF', note: 'B3 검증용 이력',
    })));

    let list = await getCareer();
    const added = list.find(c => c.teamName === 'B3 검증 FC');
    console.log('추가 후:', list.length, '건 / isCurrent =', added?.isCurrent, '(EndDate 없음 → true 기대)');

    //.// 2) 커리어 수정
    console.log('수정:', JSON.stringify(await put('/api/soccer/player/me/career', {
        careerId: added.careerId, teamName: 'B3 검증 FC (수정)',
        startDate: '2024-03-01', endDate: '2025-02-01', role: 'U12 · FW', note: '수정됨',
    })));
    list = await getCareer();
    const edited = list.find(c => c.careerId === added.careerId);
    console.log('수정 후: name =', edited?.teamName, '/ isCurrent =', edited?.isCurrent, '(EndDate 생김 → false 기대)');

    //.// 3) 입력 거부 — 종료가 시작보다 앞섬
    console.log('기간 역전 거부:', JSON.stringify(await put('/api/soccer/player/me/career', {
        careerId: added.careerId, teamName: 'X', startDate: '2025-01-01', endDate: '2024-01-01',
    })));

    //.// 4) 삭제 → 실행취소
    console.log('삭제:', JSON.stringify(await post('/api/soccer/player/me/career/delete', { careerId: added.careerId, restore: false })));
    console.log('삭제 후:', (await getCareer()).length, '건');
    console.log('실행취소:', JSON.stringify(await post('/api/soccer/player/me/career/delete', { careerId: added.careerId, restore: true })));
    console.log('복구 후:', (await getCareer()).length, '건');

    //.// 정리
    await post('/api/soccer/player/me/career/delete', { careerId: added.careerId, restore: false });
    console.log('정리 후 커리어:', (await getCareer()).length, '건 (시작과 같아야 함:', before.length, ')');

    //.// 5) 포트폴리오 — 첫 영상 자동 대표
    console.log('\n--- 포트폴리오 ---');
    console.log('시작 영상:', (await getVideos()).length, '건');

    console.log('유튜브 아닌 링크 거부:', JSON.stringify(await put('/api/soccer/player/me/portfolio', {
        videoId: '00000000-0000-0000-0000-000000000000',
        title: '나쁜 링크', videoUrl: 'https://evil.example.com/x.mp4', tags: [],
    })));

    console.log('영상1 추가(youtu.be):', JSON.stringify(await put('/api/soccer/player/me/portfolio', {
        videoId: '00000000-0000-0000-0000-000000000000',
        title: 'B3 영상 1', videoUrl: 'https://youtu.be/dQw4w9WgXcQ', tags: ['#왼발'], isPrimary: false,
    })));

    let vids = await getVideos();
    const v1 = vids.find(v => v.title === 'B3 영상 1');
    console.log('영상1 — isPrimary =', v1?.isPrimary, '(첫 영상 → true 기대)');
    console.log('영상1 — 저장된 URL =', v1?.videoUrl, '(watch 형식으로 정규화 기대)');
    console.log('영상1 — 썸네일 =', v1?.thumbnailUrl, '(링크에서 파생 기대)');

    console.log('영상2 추가(shorts):', JSON.stringify(await put('/api/soccer/player/me/portfolio', {
        videoId: '00000000-0000-0000-0000-000000000000',
        title: 'B3 영상 2', videoUrl: 'https://www.youtube.com/shorts/abcdefghijk', tags: [], isPrimary: true,
    })));
    vids = await getVideos();
    console.log('대표 개수:', vids.filter(v => v.isPrimary).length, '(항상 1이어야 함)');
    console.log('대표 제목:', vids.find(v => v.isPrimary)?.title);

    //.// 6) 대표 삭제 → 승계
    const primary = vids.find(v => v.isPrimary);
    console.log('대표 삭제:', JSON.stringify(await post('/api/soccer/player/me/portfolio/delete', { videoId: primary.videoId, restore: false })));
    vids = await getVideos();
    console.log('승계 후 대표:', vids.find(v => v.isPrimary)?.title, '/ 대표 개수:', vids.filter(v => v.isPrimary).length);

    //.// 정리
    for (const v of await getVideos()) {
        await post('/api/soccer/player/me/portfolio/delete', { videoId: v.videoId, restore: false });
    }
    await post('/api/soccer/player/me/portfolio/delete', { videoId: primary.videoId, restore: false });
    console.log('정리 후 영상:', (await getVideos()).length, '건');
})();
