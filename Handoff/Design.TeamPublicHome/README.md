# Handoff: 공개 팀 홈페이지 (Team Public Home)

> 대상 저장소: `C:\Workspace\PlayGroundNew` · 압축 해제 위치: `Handoff\Design.TeamPublicHome`
> 이 번들의 HTML은 **디자인 레퍼런스(인터랙티브 프로토타입)**입니다. 코드베이스 환경(Blazor WASM + Tailwind)에서 재구현하세요. 디자인 토큰은 기존 핸드오프(Design.Landing.Phase0 등)와 동일.

## Overview
개발 로드맵 3단계의 공개면. 라우트 `/team/{slug}` — **비로그인 읽기전용**, 학부모의 팀 선택 정보 페이지이자 팀 획득 플라이휠의 쇼케이스.
**팀 대시보드에서 입력한 데이터의 읽기전용 조합** — 별도 콘텐츠 관리 없음. 데이터 소스: 팀 정보(핵심가치·코칭스태프·공식 채널) / 선수단(Claim 상태) / 경기 결과 / 경기영상 / 모집 공고.

## Fidelity
**High-fidelity + 인터랙티브.** PC(`Team Public Home.dc.html`)·모바일(`Team Public Home Mobile.dc.html`) 모두 브라우저에서 탭 전환 조작 가능.

## 구조 (6탭)
소개 / 선수단 / 시즌성적 / 모집 / 진학·진로 / 리뷰 — 라우트 `/team/{slug}/{tab}`, 기본 소개.

### 공통 골격
- **GNB**: 좌측 = **팀 엠블럼+팀명** (PlayGround 브랜드 아님 — 팀의 자체 사이트처럼). 우측 = 경기기록 · 로그인. PlayGround 링크는 PC 우측 끝 11.5px 회색 텍스트만, 모바일은 푸터 표기만.
- **GNB 팀명 표시 규칙**: 첫 화면에선 숨김(히어로와 중복 방지) → 스크롤로 히어로 통과 시(PC 300px / 모바일 170px) fade-in. `transition opacity+translateY .25s`, 숨김 상태 `pointer-events:none`, flex 자리 유지.
- **히어로**: 커버 사진(그라디언트 오버레이 `rgba(28,43,74,.2)→.7/.78`) + 엠블럼(PC 118px / 모바일 84px, 흰 라운드 박스, **커버와 겹침 — 반드시 텍스트 블록에 position:relative+z-index, 엠블럼 SVG는 박스 내부 여백 확보**) + 팀명 + ✓ 인증팀 + 메타. 텍스트는 커버 아래 흰 배경에서 시작(사진 위 금지).
- **오렌지 CTA는 "입단 문의" 1개**: PC 히어로 우측 버튼 / 모바일 하단 고정 바(공유 버튼 + 풀폭 오렌지, safe-area 대응). 클릭 → 모집 탭.
- 탭바: PC 히어로 아래 보더탭 / 모바일 sticky(top:54px) 가로 스크롤, 스크롤바 숨김.
- 푸터: "이 홈페이지는 PlayGround에서 자동 생성되었습니다" + "우리 팀 홈페이지 만들기 →" (팀 획득 훅).

### 탭별 명세: SPEC.TEAMPUBLICHOME.md 참조

## 공개/비공개 규칙 (중요)
- 선수단: 이름·포지션·등번호·학년만 공개. Claim 상태 뱃지는 **비노출**(관리 정보). Claimed 선수만 "공개 프로필 →" 링크.
- 진학·진로: 선수 개인이 공개 동의한 사례만 표시 (문구 명시).
- 리뷰: 재원 이력 확인 계정만 작성. "재원 확인됨" 뱃지. 팀은 삭제 불가·답글만 (문구 명시).
- OG 메타(팀명·엠블럼·커버) + 방문자 통계 수집(노출은 대시보드에서만).

## Files
- `Team Public Home.dc.html` — PC 레퍼런스
- `Team Public Home Mobile.dc.html` — 모바일(390px) 레퍼런스
- `SPEC.TEAMPUBLICHOME.md` — 탭별 명세 + 체크리스트
- `support.js`, `image-slot.js` — 레퍼런스 실행용 (구현 대상 아님)
- 엠블럼은 예시 SVG 크레스트 — 실제 팀 엠블럼 이미지로 교체. 사진은 Pexels 무료 스톡.

## Claude Code 첫 프롬프트 예시
```
Handoff/Design.TeamPublicHome/README.md와 SPEC.TEAMPUBLICHOME.md를 읽어.
두 dc.html을 브라우저 레퍼런스로 삼아 /team/{slug}를 구현해.
데이터는 팀 대시보드와 같은 API를 읽기전용으로 사용 (공개/비공개 규칙 준수).
1차: GNB(스크롤 팀명 규칙) + 히어로 + 탭 골격 + 소개 탭. 완료 후 검수 요청해.
```
→ 이후: 선수단·시즌성적 → 모집·진학진로·리뷰 순.
