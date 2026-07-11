# CLAUDE.md 초안 — C:\Workspace\PlayGroundNew 루트에 배치

> 기존 기획(Notion 로드맵)의 "저장소 루트 CLAUDE.md + SPEC.md = UI 구현의 단일 진실 소스" 원칙을 따른다.

```markdown
# PlayGround — 개발 규칙

## 프로젝트
유소년 축구(U12~U18) 매칭 플랫폼. 3대 축(팀·선수·에이전트) + 공개 경기기록(Records).
개발 순서: 랜딩(Phase 0) → 인증·온보딩 → Team → Player → Records 보강.
※ 에이전트·대회 운영 시스템은 **별도 서비스로 분리 개발** (DB 공유, 프론트 분리).

## 스택
Blazor WASM + ASP.NET Core API + Tailwind + MSSQL + Redis

## UI 규칙 (필수)
1. UI 작성 전 해당 화면의 SPEC 문서 필독 (`design_handoff_*/SPEC-*.md`).
2. 섹션 순서·카피·컴포넌트 구성을 임의로 변경하지 않는다.
3. 디자인 토큰(tailwind.config)만 사용, 색상 하드코딩 금지:
   - orange #FF6B35 (CTA 전용, 5~10%), navy-deep #1c2b4a, navy #23408e,
     teal #2EC4B6, bg #fdfdfc, border #e6e8ee,
     text: #5b6577 / #3c465a / #8a93a6
   - font: Plus Jakarta Sans + Pretendard
4. 한글 버튼/pill은 white-space:nowrap.
5. 빈 데이터 노출 금지 (Phase 0 랜딩에 통계·리뷰 섹션 없음).

## 아키텍처 원칙 (스키마 선반영)
- UI는 축구만, 스키마는 다종목(SportId/SportConfig 분리)
- 에이전트 관련 스키마 선반영: AgentProfile, PlayerAgentLink, TeamRecommendation,
  Tournament.OrganizerId/Type, Commission, AgentReview, CompetitionStaff
  — API는 [Authorize(Roles="Agent,AgencyAdmin")] 가드, UI는 feature flag
- KFA 연동 대비: Match/Tournament에 DataSource(Manual/AgentHosted/KfaApi),
  ExternalRef(멱등), SyncStatus + IExternalMatchProvider 어댑터
- 대회 서비스(별도 앱)와의 경계: 대회 서비스는 Tournament/Match/CompetitionStaff에만 쓰기,
  Team/Player는 읽기 전용. 인증은 SSO 공유.

## 작업 방식
- 섹션/화면 단위로 작게 구현하고 사람이 검수 후 다음 단계 진행
- 디자인 레퍼런스 HTML(design_handoff_*/​*.html)은 브라우저로 열어 시각 비교
```
