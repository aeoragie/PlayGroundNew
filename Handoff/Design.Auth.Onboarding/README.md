# Handoff: 인증 · 온보딩 (Auth & Onboarding)

> 대상 저장소: `C:\Workspace\PlayGroundNew` · 압축 해제 위치: `Handoff\Design.Auth.Onboarding`
> 이 번들의 HTML은 **디자인 레퍼런스(인터랙티브 프로토타입)**입니다. 그대로 배포하지 말고 코드베이스 환경(Blazor WASM + Tailwind)에서 재구현하세요. 랜딩과 동일한 디자인 토큰을 사용합니다 (`Design.Landing.Phase0` 패키지의 토큰 표 참조).

## Overview
개발 로드맵 2단계. 소셜 로그인 → General 자동 가입 → 역할 선택 → 역할별 온보딩 → 대시보드 진입.
**GTM 핵심**: 팀 관리자 온보딩에 선수단 등록 단계 포함 → 선수 Unclaimed 프로필 대량 생성 → 초대코드 Claim.

## Fidelity
**High-fidelity + 인터랙티브.** `Auth Onboarding.dc.html`을 브라우저로 열면 실제 플로우대로 클릭해 이동할 수 있음(상단 네이비 FLOW 바는 리뷰용 — 구현 대상 아님). 미니 헤더(로고+현재 라우트)도 리뷰 보조용이며, 실제로는 앱 공통 헤더를 사용.

## 플로우 & 라우트
```
/login ─ 소셜/이메일 ─→ /settings/select-role ─┬─ 선수·학부모 → /onboarding/player (3스텝) → /dashboard
                                              ├─ 팀 관리자·코치 → /onboarding/team (2스텝) → /dashboard
                                              ├─ 에이전트: 비활성(준비중) — 별도 서비스
                                              └─ 스킵 → General 상태로 /dashboard (Records 진입 시 역할 유도 배너)
```
로그인 성공 시 항상 **General 역할 자동 가입**. 역할 선택은 이후 언제든 변경 가능.

상세 화면 명세: `SPEC-AuthOnboarding.md` 필독. 카피 원문 변경 금지.

## State Management
- `screen`: login | role | player | team | done (라우트로 매핑)
- 선수 온보딩 `pStep` 1~3 · 팀 온보딩 `tStep` 1~2 (진행바 = 오렌지/회색 세그먼트)
- 선택 상태: 연령(U12/U15/U18, 기본 U15) · 팀 유형(클럽/학교/학원, 기본 클럽) — 선택 칩 = 네이비 채움
- 로스터 배열: {이름, 포지션, 등번호} — 추가/삭제 즉시 반영, 이름 없으면 추가 무시
- 완료 화면 분기: doneFrom = player | team | general (아이콘·타이틀·본문 3종)

## 소셜 로그인 버튼 (공식 브랜드 규정 — 중요)
| 버튼 | 배경 | 텍스트 | 아이콘 |
|---|---|---|---|
| Google | #fff + 1.5px #e6e8ee 보더 | #1c2b4a | 공식 4색 G 로고 |
| Kakao | #FEE500 | #191919 | 공식 말풍선 심벌 #191919 |
| LINE | #06C755 | #fff | 공식 LINE 로고 흰색 |
| Naver / Apple | 점선 보더, 비활성 "준비중" | #b6bdc9 | — |

**구현 시 반드시 각 사 공식 로그인 버튼 가이드/SDK 에셋 사용** (레퍼런스의 SVG는 시안용):
- Google: developers.google.com/identity/branding-guidelines
- Kakao: developers.kakao.com/docs/latest/ko/kakaologin/design-guide
- LINE: developers.line.biz/en/docs/line-login/login-button/

## 백엔드 연결점 (스키마)
- 가입: User + Role.General 자동 부여 (6역할 스키마: player/parent/team_admin/coach/agent/admin)
- 선수 온보딩 완료: PlayerProfile 생성 (학부모 계정이면 보호자 대리 관리 = Unclaimed→Claim 모델의 보호자측)
- 팀 온보딩 완료: Team 생성 + slug 발급(공개 홈페이지 `/team/{slug}`) + 로스터 행마다 **Unclaimed PlayerProfile + PlayerLinkCode(초대코드)** 생성
- 스킵 시에도 온보딩 재진입 가능해야 함

## Files
- `Auth Onboarding.dc.html` — 인터랙티브 디자인 레퍼런스 (브라우저에서 열기)
- `SPEC-AuthOnboarding.md` — 화면별 상세 명세 + 완료 체크리스트
- `support.js` — 레퍼런스 실행용 런타임 (구현 대상 아님)

## Claude Code 첫 프롬프트 예시
```
Handoff/Design.Auth.Onboarding/README.md와 SPEC-AuthOnboarding.md를 읽어.
Auth Onboarding.dc.html을 브라우저 레퍼런스로 삼아
/login → /settings/select-role → /onboarding/player, /onboarding/team 플로우를 구현해.
1차: 라우팅 + 화면 껍데기 + 상태 전이만 (백엔드 목킹). 완료 후 멈추고 검수 요청해.
소셜 로그인 버튼은 각 사 공식 가이드 에셋으로 대체할 것.
```
→ 검수 후 2차: 실제 인증 연동(Google·Kakao·LINE OAuth) / 3차: 온보딩 완료 시 엔티티 생성 API.
