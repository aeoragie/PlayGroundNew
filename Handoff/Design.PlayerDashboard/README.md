# Handoff: 선수 대시보드 P0 (Player Dashboard)

> 대상 저장소: `C:\Workspace\PlayGroundNew` · 압축 해제 위치: `Handoff\Design.PlayerDashboard`
> 이 번들의 HTML은 **디자인 레퍼런스(인터랙티브 프로토타입)**입니다. 코드베이스 환경(Blazor WASM + Tailwind)에서 재구현하세요. 토큰·아이콘 규칙은 Design.TeamDashboard와 동일.

## Overview
개발 로드맵 4단계. 선수·보호자가 쓰는 관리 도구 — 라우트 `/dashboard/player/{section}`.
**P0 핵심 시나리오는 보호자 대리 관리**: 학부모가 자녀 프로필을 관리(Claim 완료 상태). 팀 대시보드와 같은 관리 도구 문법(이모지 금지·모노톤 SVG stroke 1.8·이니셜 아바타·오렌지 주 액션 1개).

## 구조
**PC (≥1024px)**: GNB(네이비) + 사이드바 230px + 콘텐츠 max-width 1100px
- GNB: 로고 · **"보호자 관리 모드" teal 뱃지** · 공개 프로필 보기 · 알림(주황 점) · 아바타
- 사이드바: 선수 요약(이니셜·이름·U15 · FW · #9 · 소속) + 메뉴 4개 + "P1 예정"(성장 지표/목표·일정, 비활성) + 하단 "소속팀 연결됨" 안내 박스

**모바일 (≤480px)**: 상단 바(이니셜+이름+보호자 관리 모드, 공개 프로필·알림 아이콘) + **하단 탭 4개**(safe-area, 콘텐츠 하단 패딩 96px)

메뉴/탭: 프로필 / 커리어 / 시즌 통계 / 포트폴리오

## 백엔드 연결점
- 프로필 항목별 공개 토글 → PlayerFieldVisibility (보호자 권한만 쓰기)
- 가족 계정: FamilyLink(보호자=관리자/본인=열람), 만 14세 이상 권한 이전
- 커리어: PlayerCareer + 팀 관리자 확인 시 verified 플래그("팀 확인됨"/"본인 입력")
- 시즌 통계: 팀 대시보드 경기 결과 이벤트에서 **자동 집계** (읽기전용)
- 포트폴리오: 유튜브 링크 등록, 대표 영상 1개 지정(IsPrimary), MatchId 연결 옵션
- 공개 프로필 카드: 방문 수·에이전트 열람 요청 수 노출 (공개 4뷰와 연결)

## Files
- `Player Dashboard.dc.html` — PC 레퍼런스 / `Player Dashboard Mobile.dc.html` — 모바일(390px)
- `SPEC.PLAYERDASHBOARD.md` — 섹션별 명세 + 체크리스트
- `support.js`, `image-slot.js` — 레퍼런스 실행용 (구현 대상 아님). 사진은 Pexels 스톡 — 실데이터로 교체

## Claude Code 첫 프롬프트 예시
```
Handoff/Design.PlayerDashboard/README.md와 SPEC.PLAYERDASHBOARD.md를 읽어.
두 dc.html을 레퍼런스로 /dashboard/player를 구현해.
1차: 레이아웃(GNB+사이드바/탭바) + 프로필 섹션(공개 토글 포함). 완료 후 검수 요청해.
공개 토글 쓰기는 보호자 role 가드 필수.
```
→ 이후: 커리어 → 시즌 통계 → 포트폴리오 순.
