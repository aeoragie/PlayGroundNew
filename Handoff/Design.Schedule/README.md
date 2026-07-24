# Handoff: 팀 일정 (Schedule)

> 대상 저장소: `C:\Workspace\PlayGroundNew` · 압축 해제 위치: `Handoff\Design.Schedule` (신규)
> DesignWorkOrders ① — 3곳 동시 해소: 팀 대시보드 일정 섹션(현재 목업) + 공개홈 Schedule 탭 + 허브 "다음 경기" 줄.

## 흐름
팀 관리자가 일정 추가 → 대시보드 일정 섹션(전체) + 공개 홈페이지 Schedule 탭(공개 일정만) + 허브 내 팀 카드 "다음 경기"(경기·대회 중 가장 가까운 미래 1건, 훈련 제외·없으면 줄 생략)에 자동 노출.

## 일정 추가 — PC 모달 / 모바일 바텀시트
- **유형** 라디오 카드 3종: 경기 / 대회 / 훈련 (FormPatterns ≤4)
- 필드 가변: 상대 팀 = 경기·대회만 노출(훈련 숨김) · 제목 = 대회만("서울시장배 8강" — 경기는 "vs {상대}" 파생, 훈련은 제목 필수)
- **날짜** = DatePicker 캘린더 **FutureOnly**(과거 비활성) · **시간** = 15분 리스트, **기본값 = 최근 일정 시각**
- **공개 스위치**: "공개 홈페이지에 노출" — 기본 켬, **훈련만 기본 끔**. 헬퍼 "끄면 팀 내부(대시보드)에서만 보여요"
- 저장 성공 = 토스트, 검증 오류 = 인라인

## 일정 행 (TeamDashboard SPEC 유지)
- 날짜 컬럼 PC 52px / 모바일 38px(pill+시간을 제목 위 한 줄) · 요일 색: 일 `#c46a5e`
- 유형 pill: **경기 = 네이비 채움 / 대회 = 오렌지톤(`#c24a1c`/`#fff0e9`) / 훈련 = teal 톤**
- **상태 = StartsAt 경과로 자동 파생**(컬럼·수동 전환 없음): 종료 행 = 55% 투명 + 메타 "종료", 경기·대회는 결과 연결 상태 표시("종료 — 결과 입력됨")
- 비공개 = 제목 옆 자물쇠 12px + 메타 "비공개"
- ⋯ = 수정/삭제(DropdownMenu — 파괴 맨 아래 레드 → 확인 모달, **삭제 시 팀원 알림**), 모바일 = 바텀시트

## 공개홈 Schedule 탭
- 공개 일정만 · ⋯ 없음(읽기 전용) · 종료 일정은 최근 3개까지만 표시

## 구독 캘린더
- 팀별 iCal 피드 URL(공개 일정만 포함 · 토큰 없는 공개 URL) — "구독 링크 복사" 버튼(복사 = 토스트)
- 위치: 공개홈 Schedule 탭 하단 + 대시보드 일정 섹션 캡션

## 스키마 — SoccerSchedules
`Id · TeamId · Type(Match/Tournament/Training) · Title(nullable — 대회·훈련) · OpponentName(nullable) · StartsAt · Venue · IsPublic · MatchId(nullable — 결과 입력 연결) · CreatedBy · CreatedAt`
- 상태 컬럼 없음(StartsAt 파생) · 반복 일정·지난 일정 아카이브 = **P1 분리**

## Files
- `Schedule.dc.html` — PC(추가 모달/행 상태/노출 3곳) / `Schedule Mobile.dc.html` — 모바일(바텀시트/행)
- `SPEC.SCHEDULE.md` — 섹션 순서·카피 고정·체크리스트
- `support.js` — 레퍼런스 실행용 (구현 대상 아님)

## Claude Code 첫 프롬프트 예시
```
Handoff/Design.Schedule/README.md와 SPEC.SCHEDULE.md, 두 dc.html을 읽어.
1) SoccerSchedules 테이블 + CRUD SP/API (상태 컬럼 없음 — StartsAt 파생).
2) 팀 대시보드 일정 섹션을 실데이터로 교체: 추가 모달(PC)/바텀시트(모바일),
   유형별 필드 가변, DatePicker FutureOnly, 공개 스위치(훈련 기본 끔), ⋯ 수정/삭제+알림.
3) 공개홈 Schedule 탭(공개만·읽기·종료 3개) + 허브 "다음 경기" 줄 연결.
4) iCal 피드 엔드포인트(공개 일정만) + 구독 링크 복사 버튼.
완료 후 추가→공개홈·허브 반영→비공개 전환→미노출 확인 시나리오로 검수 요청해.
```

## 완료 기준 체크리스트
- [ ] 유형 3종 필드 가변·FutureOnly·시간 기본값
- [ ] 상태 자동 파생(수동 전환 0건), 비공개 표시·공개홈 미노출
- [ ] 허브 다음 경기(훈련 제외·없으면 생략), 공개홈 읽기 전용
- [ ] iCal 피드 공개 일정만, 삭제 시 팀원 알림
