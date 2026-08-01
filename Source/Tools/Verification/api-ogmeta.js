// 링크 공유 미리보기(OG 메타) 검증 (DECISION.OGMETA — 아키텍처 B). 크롤러 UA로 4종 라우트 + 폴백 + 이미지 규격.
// 팀/선수/대회 검증 데이터는 시드 기준(공개 팀·공개 선수·대회 1건). 데이터 무변경(조회만) + 비공개 게이팅은 임시 토글 후 원복.
const BASE = 'http://localhost:5000';
const FB = 'facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)';
const KAKAO = 'Mozilla/5.0 (compatible; kakaotalk-scrap/1.0)';
const HUMAN = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36';

// 시드 식별자 (검증 계정 GUID처럼 PC마다 다를 수 있어 필요 시 아래 값을 갱신)
const TEAM = process.env.OG_TEAM || 'seoulsindapfcu12';
const TOUR = (process.env.OG_TOUR || 'D0000000-0000-0000-0000-0000000000A1');
const PLAYER = process.env.OG_PLAYER || 'igaram';

let pass = 0, fail = 0;
const check = (name, ok, extra = '') => { console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${extra ? ' — ' + extra : ''}`); ok ? pass++ : fail++; };
const html = async (ua, path) => await (await fetch(BASE + path, { headers: { 'User-Agent': ua } })).text();
const metaOf = (h, prop) => (h.match(new RegExp(`<meta property="${prop}" content="([^"]*)"`)) || [])[1] ?? null;

async function pngInfo(path) {
    const r = await fetch(BASE + path, { headers: { 'User-Agent': FB } });
    const ct = r.headers.get('content-type') || '';
    const buf = new Uint8Array(await r.arrayBuffer());
    const sig = [...buf.slice(0, 8)].map(b => b.toString(16).padStart(2, '0')).join('');
    const dv = new DataView(buf.buffer);
    const w = buf.length > 24 ? dv.getUint32(16) : 0;
    const h = buf.length > 24 ? dv.getUint32(20) : 0;
    return { ct, sig, w, h };
}

(async () => {
    //.// 1) 랜딩 / — 최소 세트 8종 + 브랜드 이미지
    let h = await html(FB, '/');
    check('랜딩 og:title=PlayGround Soccer', metaOf(h, 'og:title') === 'PlayGround Soccer');
    check('랜딩 og:image=브랜드 절대URL', metaOf(h, 'og:image') === `${BASE}/og/brand.png`);
    check('og:type=website', metaOf(h, 'og:type') === 'website');
    check('og:site_name', metaOf(h, 'og:site_name') === 'PlayGround Soccer');
    check('og:locale=ko_KR', metaOf(h, 'og:locale') === 'ko_KR');
    check('twitter:card=summary_large_image', h.includes('summary_large_image'));

    //.// 2) 팀 — 실팀명 + 팀 동적 이미지 + 절대 og:url
    h = await html(KAKAO, `/team/${TEAM}`);
    check('팀 og:image=team png', metaOf(h, 'og:image') === `${BASE}/og/team/${TEAM}.png`);
    check('팀 og:url 절대', metaOf(h, 'og:url') === `${BASE}/team/${TEAM}`);
    check('팀 제목=실팀명(랜딩 아님)', metaOf(h, 'og:title') !== 'PlayGround Soccer', metaOf(h, 'og:title'));

    //.// 3) 선수 — 이름 + 고정 문구 + 브랜드 이미지(개인 이미지 없음)
    h = await html(FB, `/player/${PLAYER}`);
    check('선수 설명=고정 문구', metaOf(h, 'og:description') === 'PlayGround Soccer 선수 프로필');
    check('선수 이미지=브랜드(개인 이미지 아님)', metaOf(h, 'og:image') === `${BASE}/og/brand.png`);
    check('선수 제목=이름(랜딩 아님)', metaOf(h, 'og:title') !== 'PlayGround Soccer', metaOf(h, 'og:title'));

    //.// 4) 대회 — 대회명 + 대회 동적 이미지 (og:image의 guid는 소문자, :guid 라우트가 대소문자 무시)
    h = await html(FB, `/records/${TOUR}`);
    check('대회 og:image=tournament png', metaOf(h, 'og:image')?.toLowerCase() === `${BASE}/og/tournament/${TOUR}.png`.toLowerCase());
    check('대회 제목=대회명(랜딩 아님)', metaOf(h, 'og:title') !== 'PlayGround Soccer', metaOf(h, 'og:title'));

    //.// 5) 사람 UA는 SPA(index.html) — og 태그 없음
    h = await html(HUMAN, `/team/${TEAM}`);
    check('사람 UA=SPA(id=app), og 없음', h.includes('id="app"') && !h.includes('og:title'));

    //.// 6) 화이트리스트 밖(크롤러) → 랜딩 폴백
    h = await html(FB, '/dashboard');
    check('화이트리스트 밖 → 랜딩 폴백', metaOf(h, 'og:title') === 'PlayGround Soccer' && metaOf(h, 'og:image').includes('/og/brand.png'));

    //.// 7) 없는 선수 → 폴백(존재 확인 불가)
    h = await html(FB, '/player/no-such-player-xyz');
    check('없는 선수 → 랜딩 폴백', metaOf(h, 'og:title') === 'PlayGround Soccer');

    //.// 8) 이미지 규격 — PNG 1200×630
    for (const path of ['/og/brand.png', `/og/team/${TEAM}.png`, `/og/tournament/${TOUR}.png`]) {
        const { ct, sig, w, h: ht } = await pngInfo(path);
        check(`${path} = image/png`, ct.includes('image/png'), ct);
        check(`${path} PNG 시그니처`, sig === '89504e470d0a1a0a');
        check(`${path} 1200x630`, w === 1200 && ht === 630, `${w}x${ht}`);
    }

    //.// 9) 크롤러의 .png(og:image) 요청은 HTML로 가로채지 않음
    h = await html(FB, `/og/team/${TEAM}.png`);
    check('.png 요청은 이미지(HTML 아님)', !h.includes('og:title'));

    console.log(`\n=== ${pass} PASS / ${fail} FAIL ===`);
    process.exitCode = fail > 0 ? 1 : 0;
})();
