# PLAN.IMPLEMENTATIONORDER — 신규 핸드오프 8종 + /notifications 구현 순서 (Phase E)

> 위치: `Handoff\PLAN.IMPLEMENTATIONORDER.md` · 기준: DesignWorkOrders ①~⑩ 디자인 완료(7/25)
> Phase A~D(PLAN.DEVELOPMENTORDER.md) 완료 후속. 각 단계 = 작게 구현 → 사람 검수 → 다음.

| 순서 | 작업 | 패키지 | 선행 이유 |
|---|---|---|---|
| E1 | 친선경기 구분 | Design.FriendlyMatch | MatchType이 E2의 "공식 행만 진입" 조건 |
| E2 | 기록 수정 신청 | Design.RecordCorrection | E1 필요 |
| E3 | 팀 일정 | Design.Schedule | 대시보드·공개홈·허브가 함께 쓰는 기반 데이터 |
| E4 | 강점 태그 | Design.StrengthTags | 소형·독립. 프로필 노출 3곳 |
| E5 | 선수 모집·지원 | Design.Application | 공개홈 모집 탭 + 팀측 지원자 리스트(기존 섹션 확장) |
| E6 | 팀 게시판 | Design.TeamBoard | SPEC.TEAMPUBLICHOME ① 소개 "팀 소식" 섹션 동반 |
| E7 | 설정 3종 | Design.SettingsFlows | 독립. 이름 변경·로그인 수단·데이터 내려받기 |
| E8 | `/notifications` 라우트 | DECISION.NOTIFICATIONCENTER | E5·E6이 알림을 늘린 뒤에 해야 실물 검증 가능 |
| E9 | OG 메타 | DECISION.OGMETA | 라우트가 확정된 뒤 마지막 |
| E10 | 에이전트 축 | Design.AgentDashboard | flag OFF 기본 — 최후순위 |

공통: `Design.PatternsIndex/README.md`(결정표) 먼저 → 패키지 README + dc.html → README 첫 프롬프트 사용 → 완료 기준 체크리스트 자가 검수 → 루트 CLAUDE.md 갱신.
확정 결정 예외는 PLAN.DEVELOPMENTORDER.md "확정 결정과 핸드오프가 다른 곳" 절 유지.
