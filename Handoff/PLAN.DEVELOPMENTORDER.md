# PLAN.DEVELOPMENTORDER — 디자인 핸드오프 구현 순서 가이드

> 위치: `Handoff\PLAN.DEVELOPMENTORDER.md` · 기준: 루트 CLAUDE.md 진행 상황(2026-07-16 갱신분) + Handoff 30개 패키지
> 기존 `Docs/Development/SharedPatternsPlan.md`(패턴 4부작 계획)를 **대체·확장**한다 — 이후 순서 판단은 이 문서 기준.
> 각 단계는 "섹션/화면 단위로 작게 구현 → 사람 검수 → 다음" 규칙(CLAUDE.md UI 규칙 6) 유지.

## 전체 그림

디자인은 완결 상태: 화면 핸드오프 15종 + **공용 패턴 15종**(목차·결정표 = `Design.PatternsIndex/README.md` — **모든 UI 작업 전 필독**).
구현 순서 원칙: **① 입력 기능을 막고 있는 횡단 패턴 먼저 → ② 미구현 입력 UI → ③ 신규 화면 → ④ 잔여 패턴은 화면 작업에 얹어 점진 적용.**

## Phase A — 횡단 기반 (다른 모든 작업을 unblock)

| 순서 | 작업 | 핸드오프 | 비고 |
|---|---|---|---|
| A1 | 폼 공용 컴포넌트 (TextField/Select/RadioCards/Checkbox/SubmitButton) | Design.FormPatterns | 검증 타이밍 3단계가 핵심. B단계 전부의 선행 조건 |
| A2 | Toast 서비스 + ConfirmModal/BottomSheet | Design.FeedbackPatterns | 저장·삭제 플로우의 선행 조건. 기존 화면의 애드혹 알림 교체 |
| A3 | 스켈레톤 프리미티브 + 빈 상태 Tier A/B | Design.LoadingStates, Design.EmptyStates | 기존 "점선 카드" 애드혹(커리어·포트폴리오·경기 결과)을 공용 컴포넌트로 통합 |
| A4 | 내비게이션 배선 + 에러 페이지 | Design.Navigation | 404/403/500 신규(Error Pages*.dc.html), returnUrl, 게스트/로그인 GNB 매트릭스(모바일 GNB 규칙 포함) — 기존 잔여 항목 "공개 페이지 로그인 상태 GNB"를 여기서 흡수. Routes.cs 상수 기반 죽은 링크 감사 |

## Phase B — 입력 UI (A1·A2 위에)

| 순서 | 작업 | 사용 패턴 | 비고 |
|---|---|---|---|
| B1 | 경기 결과 입력 ("＋ 결과 입력") | Form + Feedback + **DatePicker**(캘린더+시간 리스트) | 저장 시 `UspRecalculateSoccerTournamentStandings` 자동 호출 필수(CLAUDE.md D5) |
| B2 | 팀 정보 수정 UI | Form + Feedback + **ImageUploader**(엠블럼 원형·커버 3:1) | 조회 연동은 완료 상태 — 쓰기 SP·PUT 신규 |
| B3 | 커리어·포트폴리오 입력 UI | Form + Feedback | 조회 연동 완료 — 쓰기만. 영상은 URL 입력(업로더 아님) |
| B4 | 선수 사진 업로드 | ImageUploader | 보호자·팀 관리자만(권한 절 참조). PhotoUrl 컬럼 기존재 |

## Phase C — 신규 화면 (핸드오프만 있고 미구현)

| 순서 | 화면 | 핸드오프 | 의존 |
|---|---|---|---|
| C1 | 대시보드 허브 `/dashboard` | Design.DashboardHub | 현재 JWT 역할 분기를 유지하되 **역할 2개 이상만 허브 표시**(1개=스킵 리다이렉트). 다중 역할 도입 시점과 맞물림 |
| C2 | 팀 탐색 | Design.TeamExplore | **SearchFilter**(칩·URL 동기화) 선행 — 이 화면에서 함께 구현 권장 |
| C3 | 설정 (계정·역할·알림) | Design.Settings | **ToggleSwitch**(스위치·잠금 뱃지) 함께 구현 |
| C4 | 보호자 Claim 4스텝 + 알림 센터 | Design.ClaimFlow | **BannerStepper**(스텝퍼) 함께. 초대코드 Claim(구현 완료)과 별개 플로우 — 병합 주의 |
| C5 | 공개 팀 홈 잔여 탭 (모집·진학진로·리뷰) | Design.TeamPublicHome SPEC | 각각 스키마 신설 필요 — 탭당 1단계로 쪼갤 것 |
| C6 | 에이전트 열람 승인 | Design.AgentViewApproval | 에이전트 축 feature flag 뒤 — **가장 후순위** |

## Phase D — 잔여 패턴: 별도 단계 없음, 화면 작업에 얹는다

- **TableList·TabAccordion·AvatarBadge**: 신규 화면(C1~C4)을 만들 때 그 화면 범위만 적용. 단, **AvatarBadge는 기존 화면 하드코딩 일괄 교체 1회 작업**(랜덤 색 방지) 가치 있음 — Phase C 완료 후 1커밋으로.
- **TooltipHelp·PaginationBreadcrumb·SearchFilter GNB 퀵서치**: 필요 화면 등장 시.
- 새 요소가 필요하면 PatternsIndex 결정표 → 기존 15종 조합 우선.

## 각 단계 공통 절차

1. `Design.PatternsIndex/README.md`(입구) → 해당 패키지 README + dc.html(브라우저 시각 비교)
2. 각 패키지 README 하단의 "Claude Code 첫 프롬프트"를 그대로 사용
3. 완료 기준 체크리스트로 자가 검수 → 화면 검증(LocalVerification.md 절차) → 사람 검수 요청
4. 루트 CLAUDE.md 진행 상황 갱신

## 확정 결정과 핸드오프가 다른 곳 (핸드오프를 덮는 예외)

- 공개 팀 홈 GNB: 좌측 브랜드 고정 + 팀명 fade-in (SPEC의 "우측 회색 링크" 아님 — 획득 플라이휠 목적, CLAUDE.md 기록)
- LINE 로그인: 채널 미발급으로 보류 — 버튼 노출하지 않음 (핸드오프에는 존재)
- 빈 상태 CTA: 기존 구현의 "주 액션은 헤더 오렌지 버튼 하나" 결정과 EmptyStates의 "오렌지 CTA 금지"가 충돌 시 → **헤더 버튼은 유지, 빈 상태 카드 안 CTA만 네이비** 로 해석한다
