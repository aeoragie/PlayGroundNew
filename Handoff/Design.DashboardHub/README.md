# Handoff: 대시보드 허브 (Dashboard Hub)

> 대상 저장소: `C:\Workspace\PlayGroundNew` · 압축 해제 위치: `Handoff\Design.DashboardHub`
> 이 번들의 HTML은 **디자인 레퍼런스**입니다. 코드베이스 환경(Blazor WASM + Tailwind)에서 재구현하세요. 토큰은 기존 핸드오프와 동일.

## Overview
로그인 직후 도착하는 상위 화면 — 라우트 `/dashboard`. 한 계정의 여러 역할(팀 관리자 + 보호자, 자녀 다수)을 한눈에 모아 각 대시보드로 분기시키는 현관.

**핵심 라우팅 규칙 (필수)**
- 역할이 **1개뿐인 계정은 허브를 건너뛰고** 해당 대시보드로 즉시 리다이렉트 (`/dashboard/team` 또는 `/dashboard/player`)
- 역할 2개 이상 또는 자녀 2명 이상일 때만 허브 표시
- General(역할 미설정) 계정 → 역할 선택 화면으로

## 구조 (PC max-width 1100px / 모바일 480px 세로 스택)
1. **GNB**(네이비): 워드마크 + 퀵서치(모바일=아이콘) + 벨(미읽음 카운트 오렌지) + 아바타·이름
2. **인사 + 오늘 요약**: "안녕하세요, {이름} 님" + 이번 주 경기·미처리 알림 수 (동적 문장, 없으면 생략)
3. **처리가 필요해요** (오렌지 `rgba(255,107,53,.35)` 테두리 카드): 액션형 알림만 추려 딥링크 카드로. 유형 칩: 연결(오렌지톤)/열람(violet)/결과(네이비톤). 0건이면 섹션 자체 숨김. "알림 센터 전체 보기 →" → Design.ClaimFlow의 알림 센터
4. **내 팀** (팀 관리자 역할 보유 시): 네이비 그라디언트 대형 카드 — 팀명+인증 뱃지 + 요약(선수 수·미처리 요청) + 다음 경기(teal 강조) + [팀 대시보드(오렌지) / 공개 홈페이지]. 복수 팀이면 카드 반복. "＋ 팀 만들기" 점선 버튼
5. **내 자녀** (보호자 역할 보유 시): 흰 카드 grid(PC minmax 340px / 모바일 세로) — 이니셜 아바타 + 이름 + Claim 상태 뱃지(연결됨 teal / 승인 대기 오렌지톤) + 시즌 3스탯(출전/득점/도움) + 노트 + [선수 대시보드 or 요청 상태 보기 / 공개 프로필]. **Pending 자녀 = 스탯 "–" + 공개 프로필 버튼 숨김**. "＋ 자녀 연결" → Claim 플로우
6. **바로가기**: 경기기록(개인화 문구 "U15 진행중 대회 N개") / 팀 탐색 / 계정 설정(PC만)

## 데이터 소스
- 역할·팀·자녀 목록: 계정 Role + TeamStaff + FamilyLink
- 액션형 알림: Notification 중 ActionRequired만 상위 3건
- 자녀 스탯: SeasonStats 집계(읽기전용), 다음 경기: 팀 Schedule
- 허브에 쓰기 기능 없음 — 전부 읽기 + 딥링크

## Files
- `Dashboard Hub.dc.html` — PC / `Dashboard Hub Mobile.dc.html` — 모바일(390px, 하단 탭바 없음 — 진입 화면)
- `SPEC.DASHBOARDHUB.md` — 상세 명세 + 체크리스트
- `support.js` — 레퍼런스 실행용 (구현 대상 아님)

## Claude Code 첫 프롬프트 예시
```
Handoff/Design.DashboardHub/README.md와 SPEC.DASHBOARDHUB.md를 읽어.
두 dc.html을 레퍼런스로 /dashboard 허브를 구현해.
핵심: 역할 1개 계정은 허브 스킵 리다이렉트. 완료 후 검수 요청해.
```
