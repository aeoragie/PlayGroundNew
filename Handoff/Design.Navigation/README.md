# Handoff: 내비게이션 · 인증 상태 플로우 (Navigation & Auth Flow)

> 대상 저장소: `C:\Workspace\PlayGroundNew` · 압축 해제 위치: `Handoff\Design.Navigation`
> 이 패키지는 **화면이 아니라 연결 명세**입니다. 13개 화면 핸드오프를 라우팅·인증 상태로 묶습니다. 구현된 각 페이지의 링크를 이 문서 기준으로 배선하세요.

## 라우트 맵

| 라우트 | 화면 (핸드오프) | 접근 |
|---|---|---|
| `/` | 랜딩 (Design.Landing.Phase0) | 공개 |
| `/auth` | 로그인·온보딩 (Design.Auth.Onboarding) | 게스트 전용 — 로그인 상태면 `/dashboard`로 |
| `/dashboard` | 허브 (Design.DashboardHub) | 인증. **역할 1개면 스킵 리다이렉트** |
| `/dashboard/team` | 팀 대시보드 (Design.TeamDashboard) | 팀 관리자 |
| `/dashboard/player` | 선수 대시보드 (Design.PlayerDashboard) | 선수/보호자(자녀 선택) |
| `/teams` | 팀 탐색 (Design.TeamExplore) | 공개 |
| `/team/{slug}` (+6탭) | 공개 팀 홈 (Design.TeamPublicHome) | 공개 |
| `/player/{id}` | 공개 선수 프로필 (Design.PlayerPublicProfile) | 공개 (권한 뷰는 ViewGrant) |
| `/records` | 경기기록 (Design.Records) | 공개. 로그인 시 내 연령 우선 |
| `/claim` | Claim 4스텝 (Design.ClaimFlow) | 인증(보호자) |
| `/notifications` | 알림 센터 (Design.ClaimFlow) | 인증 |
| `/settings/{section}` | 설정 (Design.Settings) | 인증 |
| `/approvals/agent/{id}` | 열람 승인 (Design.AgentViewApproval) | 인증(해당 보호자만) |

## GNB 상태 매트릭스 (핵심 규칙)

**공통 GNB** (랜딩·탐색·기록·선수 프로필):
- 게스트: 워드마크(→`/`) + 경기기록 + 팀 탐색 + **[로그인]**(→`/auth?returnUrl={현재}`) + [시작하기 CTA — 랜딩만 오렌지]
- 로그인: 로그인 버튼 자리 → **벨(미읽음 카운트) + 아바타**. 아바타 클릭 = 드롭다운(대시보드 / 설정 / 로그아웃). 랜딩 CTA는 "내 대시보드"로 문구 교체
- **팀 공개홈은 예외**: 팀 엠블럼 GNB 유지(PlayGround 최소 노출 원칙) — 로그인 상태여도 벨·아바타를 넣지 않고 우측 "경기기록 · 로그인/내 대시보드" 텍스트 링크만

**모바일 GNB**: 게스트 = 워드마크 + [로그인] 버튼만(메뉴 링크는 숨김 — 경기기록·팀 탐색은 콘텐츠 내 진입점으로 충분) / 로그인 = 로그인 버튼 자리에 벨+아바타. 하단 탭바는 대시보드 계열만 사용(공개 페이지는 없음).

**팀 탐색 직접 진입점 (7/21 보강)**: ① 랜딩 PC GNB 메뉴 "팀 탐색" + 히어로 보조 CTA("우리 동네 팀 둘러보기" — 게스트 가치 체험 동선, 모바일은 히어로 아래 콘텐츠 카드) ② 선수 대시보드 — 무소속 선수의 팀 카드 빈 상태 CTA "팀 찾아보기"(네이비, 소속 선수에겐 미노출) ③ 팀 대시보드 — 좌측 메뉴 하단 보조 링크(회색 톤, 친선경기 상대 물색용) ④ 기존: 허브 바로가기 + GNB 퀵서치.

**대시보드 GNB** (허브·팀/선수 대시보드·설정): 항상 인증 상태 — 기존 핸드오프 명세 그대로

## 인증 플로우

1. 로그인 진입 = 항상 `?returnUrl` 보존. 성공 후: returnUrl 있으면 그곳, 없으면 `/dashboard`
2. `/dashboard` 분기: 역할 0개→역할 선택(온보딩) / 1개→해당 대시보드 즉시 리다이렉트 / 2개+→허브
3. 보호계정 규칙: 인증 필요 라우트에 게스트 접근 → `/auth?returnUrl=` / 권한 없는 역할 접근(예: 선수가 `/dashboard/team`) → `/dashboard`
4. 로그아웃 → `/` (랜딩)

## 페이지별 링크 배선 (핸드오프에 없던 연결만)

- **랜딩**: 히어로 CTA→`/auth` · 이용자 카드(팀→`/teams`, 선수→`/auth`) · 푸터 경기기록→`/records`
- **팀 탐색**: 팀 카드→`/team/{slug}` · 게스트가 "팀 만들기"→`/auth?returnUrl=/teams`
- **팀 공개홈**: 선수단 카드→`/player/{id}` · 경기 기록 행→`/records` 해당 대회 · **[승인됨]** 관리자 본인이 자기 팀 열람 시 우측 상단 "관리" 텍스트 링크 노출→`/dashboard/team` (팀 홈 PlayGround 최소 노출 원칙에 맞춰 버튼 아닌 텍스트 링크)
- **선수 프로필**: 소속팀명→`/team/{slug}` · 에이전트 열람 요청 버튼은 에이전트 서비스 몫(여기선 미노출)
- **경기기록**: 팀명→`/team/{slug}` · 로그인+자녀 보유 시 내 연령 세그먼트 기본 선택
- **허브/대시보드**: 기존 핸드오프의 딥링크 표 그대로 (알림→`/notifications`, 열람 요청→`/approvals/agent/{id}`, 자녀 연결→`/claim`)
- **에러 페이지** (`Error Pages.dc.html` / `Error Pages Mobile.dc.html` — 상단 전환 바는 프로토타입 전용):
  - **404**: 축구장+위치 핀 일러스트 · [홈으로(네이비) / 팀 탐색]
  - **403 권한 없음**: 실드+자물쇠 · [내 대시보드로 / 다른 계정으로 로그인] — 로그인 상태 전용(게스트는 `/auth?returnUrl` 리다이렉트)
  - **500**: 전광판+느낌표(오렌지 스파크) · [다시 시도 / 홈으로] + 오류 코드 `ERR-{시각}-{요청ID}` + 데이터 안심 문구
  - 공통: 게스트 GNB 유지, 주 버튼=네이비(오렌지 CTA 아님), 이모지 없음

## Claude Code 첫 프롬프트 예시
```
Handoff/Design.Navigation/README.md를 읽어.
구현된 전 페이지에 라우트 맵과 링크 배선표를 적용하고,
GNB를 게스트/로그인 상태 매트릭스대로 전환 컴포넌트로 만들어.
핵심: returnUrl 보존, /dashboard 역할 분기(0/1/2+), 팀 공개홈 GNB 예외.
완료 후 게스트→로그인→허브→각 화면 왕복 시나리오로 검수 요청해.
```

## 완료 기준 체크리스트
- [ ] 전 라우트 접근 규칙(공개/게스트 전용/인증/역할) 동작
- [ ] GNB 게스트↔로그인 전환 + 팀 공개홈 예외 유지
- [ ] returnUrl 왕복(게스트로 `/records` 열람→로그인→`/records` 복귀)
- [ ] `/dashboard` 3분기 + 로그아웃→랜딩
- [ ] 배선표의 모든 링크 실제 이동 확인, 죽은 링크 0개
