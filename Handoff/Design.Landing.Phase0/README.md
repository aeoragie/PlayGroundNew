# Handoff: PlayGround 랜딩 페이지 (Phase 0)

> 대상 저장소: `C:\Workspace\PlayGroundNew`
> 이 번들의 파일은 **HTML로 제작된 디자인 레퍼런스(프로토타입)**입니다. 그대로 배포하는 코드가 아니라, **대상 코드베이스의 환경(로드맵 기준: Blazor WASM + Tailwind)에서 동일하게 재구현**하는 것이 과제입니다. 아직 프로젝트가 비어 있다면 로드맵의 스택(Blazor WASM + ASP.NET Core API + Tailwind)을 그대로 채택하세요.

## Overview
유소년 축구 매칭 플랫폼 PlayGround의 출시기(Phase 0) 랜딩 페이지. 라우트 `/soccer`.
목적: 영업 시 보여줄 첫 얼굴. **가치 제안 + 역할별 진입(팀 / 선수·학부모)**. 데이터가 없는 시기이므로 **빈 데이터(팀 수, 리뷰 등) 노출 금지**.

## Fidelity
**High-fidelity.** 색·타이포·간격·인터랙션까지 최종안입니다. 픽셀 단위로 재현하되, 스타일 값은 하드코딩하지 말고 Tailwind 토큰으로 등록해 사용하세요.

## 섹션 순서 (임의 변경 금지)
1. GNB (하이브리드 헤더)
2. Hero — **배경 사진(흑백 50%) + 그레이·네이비 오버레이**
3. 역할 2분할 (팀 / 선수·학부모)
4. 핵심 기능 3
5. 작동 방식 3스텝
6. CTA
7. Footer

※ 초기안의 '사진 마키 스트립'은 폐기 — 히어로 배경 사진으로 대체됨.
※ **PC + 모바일 두 뷰포트 모두 구현** (반응형 1페이지). 뷰포트별 수치는 SPEC-Landing.md의 표 참조.

상세 레이아웃·카피·수치는 `SPEC-Landing.md` 참조. **카피는 SPEC의 텍스트를 한 글자도 바꾸지 말 것.**

## Interactions & Behavior
- **하이브리드 헤더**: `position:fixed`, 최초(hero 위)엔 투명 + 흰색 텍스트. 스크롤(PC 40px / 모바일 30px 초과) 시 solid + 네이비 텍스트. transition 250ms. 콘텐츠 첫 섹션은 hero 상단 패딩(PC 170px / 모바일 120px)으로 오프셋.
- **모바일 햄버거 메뉴**: 헤더 하단 드롭다운, 항목 선택 시 닫힘, 열림 중 헤더 solid 강제.
- **히어로 배경 사진**: 풀블리드 cover + `grayscale(50%)` + 오버레이 `linear-gradient(160deg, rgba(30,35,45,.9), rgba(28,43,74,.84) 55%, rgba(35,64,142,.76))`. 사진 로드 실패 시 `#1c2b4a` 배경 폴백. 콘텐츠 텍스트에 `text-shadow`(버튼 제외).
- 앵커 스크롤: GNB 메뉴 → 해당 섹션 (`scroll-behavior:smooth`, 각 섹션 `scroll-margin-top:80px`).
- 버튼 hover: 오렌지 → `#e85a26`, 네이비 → `#1c2b4a`, 흰 버튼 → teal 배경/흰 글자. 모든 pill 버튼 `white-space:nowrap` (한글 줄바꿈 방지 필수).
- CTA 버튼 → 회원가입/로그인 라우트(2단계 인증·온보딩에서 구현, 지금은 링크 스텁).

## State Management
- 헤더 solid 여부(boolean) 하나뿐. 스크롤 리스너(passive) + 변경 시에만 재렌더.
- 데이터 페칭 없음 (Phase 0은 정적).

## Design Tokens
| 토큰 | 값 | 용도 |
|---|---|---|
| orange (CTA 전용) | `#FF6B35` / hover `#e85a26` | 주요 CTA에만, 전체의 5~10% |
| navy-deep | `#1c2b4a` | hero·역할카드·CTA 배경, 본문 헤딩 |
| navy | `#23408e` | 그라디언트 끝, 보조 버튼, 아이콘 배경 |
| teal | `#2EC4B6` | 뱃지·체크·오버라인 포인트 |
| bg | `#fdfdfc` | 페이지 배경 |
| surface-alt | `#f4f6fa` / `#f1f3f8` | 3스텝 래퍼, 아이콘 배경 |
| border | `#e6e8ee` (1.5px) | 카드 보더 |
| text-body | `#5b6577` · text-strong `#3c465a` · text-muted `#8a93a6` | |
| radius | 카드 18~20px · 버튼 9~12px · 래퍼 24px · 마키카드 16px | |
| font | Plus Jakarta Sans(라틴·숫자) + Pretendard(한글), weight 500/600/700/800 | |
| 폭 | 콘텐츠 max-width 1200px, 좌우 패딩 32px | |

그라디언트: `linear-gradient(160deg, #1c2b4a 0%, #23408e 100%)` (hero·CTA 공통).
장식: 얇은 원(1.5px 보더, rgba 흰색 .06~.08) 2~3개를 절대배치 — SVG 아님, div border-radius 50%.

## Assets
- 마키 사진 6장: 미확보. placeholder → 실제 유소년 경기·팀 사진으로 교체 예정.
- 로고: 임시(⚽ + 오렌지 라운드 사각형). 정식 로고 확정 시 교체.
- 폰트: Google Fonts(Plus Jakarta Sans) + jsdelivr(Pretendard) CDN 또는 self-host.

## Assets (추가)
- 히어로 기본 사진: Pexels 33257251 (무료, 출처표기 불요) — `https://images.pexels.com/photos/33257251/pexels-photo-33257251.jpeg` (실제 촬영본 확보 시 교체)

## Files
- `Landing Phase 0.dc.html` — PC 디자인 레퍼런스 원본(브라우저에서 열어 확인). 인라인 스타일로 모든 수치 확인 가능.
- `Landing Phase 0 Mobile.dc.html` — 모바일(390px) 디자인 레퍼런스 원본.
- `SPEC-Landing.md` — PC·모바일 통합 명세(섹션별 표 비교). **구현 전 필독.**
- `CLAUDE-md-추가내용.md` — 저장소 루트 `CLAUDE.md`에 넣을 규칙 초안.

## Claude Code에게 지시하는 방법 (권장 절차)
1. 이 폴더를 `C:\Workspace\PlayGroundNew\design_handoff_landing_phase0\`로 복사.
2. 저장소 루트에 `CLAUDE.md`가 없으면 `CLAUDE-md-추가내용.md` 내용으로 생성.
3. Claude Code 첫 프롬프트 예시:
   ```
   design_handoff_landing_phase0/README.md와 SPEC-Landing.md를 읽고,
   Landing Phase 0.dc.html(PC) / Landing Phase 0 Mobile.dc.html(모바일)을
   시각 레퍼런스로 삼아 /soccer 랜딩 페이지를 Blazor WASM + Tailwind로
   반응형 1페이지로 구현해줘 (브레이크포인트 768px).
   섹션 순서·카피·색상 토큰을 임의로 바꾸지 마.
   완료 기준: SPEC의 체크리스트 전부 통과.
   ```
4. 큰 덩어리로 시키지 말고 **섹션 단위**로 검수하며 진행 (헤더+hero → 마키 → 역할 2분할 → 나머지).
