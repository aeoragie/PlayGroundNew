# Handoff: 팀 대시보드 P0 (Team Dashboard)

> 대상 저장소: `C:\Workspace\PlayGroundNew` · 압축 해제 위치: `Handoff\Design.TeamDashboard`
> 이 번들의 HTML은 **디자인 레퍼런스(인터랙티브 프로토타입)**입니다. 코드베이스 환경(Blazor WASM + Tailwind)에서 재구현하세요. 디자인 토큰은 `Design.Landing.Phase0` 패키지와 동일.

## Overview
개발 로드맵 3단계. 팀 관리자·코치가 쓰는 관리 도구(듀얼 모드 중 관리 모드).
**GTM 핵심**: 선수단 등록 → Unclaimed 프로필 → 초대코드 Claim. 팀 정보(핵심가치·코칭스태프·공식 채널)는 공개 팀 홈페이지에 자동 노출 — 학부모의 팀 선택 기준.

## Fidelity
**High-fidelity + 인터랙티브.** `Team Dashboard.dc.html` 브라우저로 열어 사이드바·탭·필터·뷰 토글 직접 조작 가능.

## 구조
**PC (≥1024px)**
- 상단 GNB(네이비) + 좌측 사이드바(230px) + 콘텐츠
- 메뉴: 팀 정보 / 선수단 / 일정 / 경기 결과 / 경기영상 / 선수 모집 + P1 예정(비활성)

**모바일 (≤480px)**
- 상단 바(팀명+인증팀 요약, 홈페이지·알림 아이콘) + **하단 탭 5개**(safe-area 대응)
- 탭: 팀 정보 / 선수단 / 일정 / 경기 / 모집 — **경기 결과+영상은 "경기" 탭의 서브탭(결과/영상)으로 통합** (모바일 탭 5개 권장 한계)
- 라우트 `/dashboard/team/{section}`은 동일 — 뷰포트만 다른 네비게이션

## 디자인 원칙 (이 화면 고유)
1. **이모지 금지** — 모든 아이콘은 모노톤 라인 SVG(stroke 1.8, currentColor). 아바타는 이니셜.
2. **브랜드 아이콘 예외** — 유튜브(레드 플레이)·인스타그램(그라디언트 글리프) 공식 아이콘은 콘텐츠 영역(공식 채널·코치 SNS·영상 카드)에만. **사이드바 내비게이션에는 금지.**
3. 한글 메타 텍스트는 `white-space:nowrap` 또는 `word-break:keep-all` 필수.
4. 오렌지는 각 화면 주 액션 버튼 1개에만.

## State Management
- section(6개) · ageTab(U12/U15/U18) · resultTab(전체/리그/컵/친선) · videoTab(전체/하이라이트/풀경기/훈련) · rosterView(list/cards)
- 선택 칩/탭 = 네이비 채움. 상세 명세는 `SPEC.TEAMDASHBOARD.md`.

## 백엔드 연결점
- 선수단: Claim 상태 3종(Claimed/Pending/Unclaimed) + 상태별 액션(프로필/승인/초대코드) · PlayerLinkCode 재발송
- 경기 결과: 득점·도움 이벤트 → 선수 SeasonStats 자동 반영 · Records/공개 홈페이지 노출
- 경기영상: 유튜브 URL 등록(파일 업로드 없음) · MatchId 연결 시 경기 상세·홈페이지 자동 노출
- 선수 모집: TeamRecommendation 수신(대기/검토중/수락/거절) — 에이전트 추천은 Violet 뱃지
- 팀 정보(핵심가치·코칭스태프·공식 채널)는 공개 홈페이지 소개 탭의 데이터 소스

## Files
- `Team Dashboard.dc.html` — PC 인터랙티브 디자인 레퍼런스
- `Team Dashboard Mobile.dc.html` — 모바일(390px) 레퍼런스
- `SPEC.TEAMDASHBOARD.md` — PC + 모바일 명세 + 완료 체크리스트
- `support.js`, `image-slot.js` — 레퍼런스 실행용 (구현 대상 아님)
- 선수·영상 예시 사진은 Pexels 무료 스톡 — 실데이터로 교체

## Claude Code 첫 프롬프트 예시
```
Handoff/Design.TeamDashboard/README.md와 SPEC.TEAMDASHBOARD.md를 읽어.
Team Dashboard.dc.html을 브라우저 레퍼런스로 삼아 /dashboard/team을 구현해.
1차: 레이아웃(GNB+사이드바) + 팀 정보 섹션만. 완료 후 멈추고 검수 요청해.
아이콘은 이모지 금지, 모노톤 라인 SVG. 브랜드 아이콘은 SNS 링크에만.
```
→ 이후 섹션 단위로: 선수단(리스트+카드+Claim) → 경기 결과 → 일정·경기영상 → 선수 모집.
