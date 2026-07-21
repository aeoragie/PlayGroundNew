# 검증 스크립트 (Verification)

각 단계에서 실제로 돌려 본 화면·API 검증 스크립트다. **앱 빌드와 무관**하고 솔루션에도 포함되지 않는다 —
회귀 테스트 스위트가 아니라 **"그때 무엇을 어떻게 확인했는지"의 기록**이자 다음 검증의 출발점이다.

CLAUDE.md의 각 Phase 절이 여기 파일들을 이름으로 참조한다.

## 준비

```powershell
cd Source/Tools/Verification
npm install                 # puppeteer-core만 받는다
dotnet run --project ../../PlayGround/PlayGround.Server --urls http://localhost:5000
```

스크립트는 `http://localhost:5000`을 가정한다. 스크린샷은 실행한 디렉터리에 저장된다.

## 파일

| 파일 | 확인하는 것 |
|---|---|
| `shot-connect.js` | **CDP 연결 참고 구현** — 나머지 스크립트가 전부 이 패턴을 쓴다 |
| `shot-states.js` | A3 스켈레톤 타이밍(200ms/3초)·빈 상태·로드 후 점프 0 |
| `shot-b1.js` | B1 경기 결과 입력 — 인라인 오류·캘린더·시간 리스트 |
| `shot-b2.js` | B2 엠블럼 교체→공개홈 반영 · 12MB 실패 · 세로 사진 EXIF |
| `shot-b3.js` | B3 커리어·포트폴리오 — 추가→수정→삭제(모달)→실행취소 |
| `api-b3.js` | B3 API 왕복 — 기간 역전 거부·유튜브 링크 정규화·대표 승계 |
| `shot-b4.js` | B4 선수 사진 — 권한별 카메라 뱃지 노출 |
| `perm-b4.js` | B4 권한 4종(보호자/팀 관리자/제3자 2종) + 외부 URL 거부 |
| `shot-b5.js` | B5 친선 행 마킹 · 세그먼트 URL 동기화 · 요약 공식만 |
| `sql-b5.sql` | B5 집계 경계 — 친선을 리그 스코프에 넣어도 순위표 무변화(양방향) |
| `api-b6.js` | B6 신청 경계 — 중복·친선·남의 경기 거부 / 취소 후 재신청 |
| `shot-b6.js` | B6 진입점(친선 행 ⋯ 0건)·신청 폼·중복 시 "신청 처리 중"·취소 |
| `sql-b6.sql` | B6 상태 3종 심기 — **주최측이 채우는 값을 흉내 낸다** (아래 참고) |
| `shot-b6-status.js` | B6 반영·반려 렌더 + 반려 사유 표시 (sql-b6.sql 실행 후) |
| `sql-multichild.sql` | 자녀 N명 — 프로시저가 `@TargetPlayerId`로 갈라지는지 |
| `sql-twochildren.sql` | 자녀 2명 만들기 (선수 대시보드 전환 · 허브 표시 조건) |
| `shot-childswitch.js` | 자녀 전환 시 화면 데이터가 실제로 갈아끼워지는지 |
| `api-actionitems.js` | "처리가 필요해요" 파생 — 팀 단위 묶음 · 접수 상태 제외 · TotalCount |
| `sql-hub.sql` | 허브 표시 조건 — **팀 관리자이면서 보호자**인 계정 만들기 |
| `api-hub.js` | 허브 묶음 + 라우팅 3분기의 근거(관리 대상 수) · 자녀 스탯 = 선수 대시보드 |
| `shot-hub.js` | 라우팅 3분기 실제 도착지 · 허브 PC/모바일 · **역할 가드 제거로 튕김 없음** |
| `shot-hub-roles.js` | General → 역할 선택 · 자녀 2명 카드 반복 (역할 일시 변경 후 복구) |
| `api-settings.js` | 설정 API — 이메일 마스킹 · 알림 기본값 병합 · **승인형 저장 거부**(enum 화이트리스트) · 계정 소프트 삭제(임시 계정 — 끝나면 SQL로 물리 삭제) |
| `shot-settings.js` | 설정 3탭 URL 동기화 · 삭제 모달 문구 입력 잠금 · "항상 켜짐" 뱃지 · **스위치 실패 롤백**(PUT abort → 낙관 반영 → 롤백+오류 토스트) · 모바일 세그먼트 탭. playwright-core 필요 |
| `shot-hierarchy.js` | 계층 스위치 — 상위 "프로필 공개" off → 하위 dimmed(.45)+비활성 + 실행취소 토스트 → **공개홈 로스터에서 선수 숨김** → 실행취소 복귀. 끝나면 `DELETE FROM SoccerPlayerFieldVisibilities WHERE FieldName='Profile'`로 원복 |
| `api-claim.js` | Claim 4스텝 API 왕복 — 코드 조회·요청 생성(멱등)·**남의 팀 관리자 거부**·승인(연결+가족+알림+읽음)·**허브 자녀 카드 반영**·거절(코드 미소진)·친선경기 결과 알림(**수신 설정 on/off 필터**). 보호자 계정은 find-or-create — 끝나면 sql-claim-restore로 원복 |
| `api-correction-noti.js` | 기록 수정 심사 결과 **지연 생성** — `create`로 신청 → SQL로 주최측 심사 흉내(Accepted) → `verify`로 조회 시점 생성+멱등 확인 |
| `shot-claim.js` | Claim UI 왕복 — 코드 6칸(투명 오버레이 input)·스텝퍼 뒤로+입력 유지·재방문 복원·관리자 벨 배지→패널 인라인 승인→완료 박스·보호자 완료 화면·**General 보호자 허브 자녀 카드**·모바일 진행 바. playwright-core 필요 |
| `sql-claim-restore.sql` | Claim 검증 원복 — 선수 연결 해제·코드 Pending 복구·요청/알림/친선경기/수정신청 삭제. Account의 보호자 임시 계정은 별도 DELETE |
| `api-recruit.js` | 모집 공고 API 왕복 — 작성→공개 열람→수정→마감(단방향·마감 후 수정 거부)→삭제/복구 · **팀 탐색 IsRecruiting 파생 on/off** · 경계(남의 계정 Forbidden·조건 5개·과거 마감일). 끝나면 `DELETE FROM SoccerTeamRecruitments`로 원복 |
| `shot-recruit.js` | 모집 탭 UI 왕복 — 대시보드 폼(빈 제출 인라인)→저장 토스트→**게스트 공개홈**(모집중 카드·칩·무동작 지원하기 / 마감 회색 / 문의 카드)→마감 모달(재오픈 불가 명시)→삭제→실행취소·모바일. playwright-core 필요 |
| `sql-agent-seed.sql` | 에이전트 열람 요청 시드 — **에이전트 서비스(프로필·요청 생성)를 흉내 낸다**(sql-b6 성격). 실행마다 Pending 요청 1건, RequestId 출력. 끝나면 SoccerAgent* 4테이블 + ViewRequest 알림 DELETE로 원복 |
| `api-agent.js` | 열람 승인 API — 알림 지연 생성 · 승인(+30일·로그·알림 읽음) · **재승인/남의 계정/미지 액션 거부** · 거절 · 차단(대기 요청 함께 거절). `node api-agent.js <id1> <id2> <id3>` |
| `shot-agent.js` | 열람 승인 UI — **FeatureFlags.AgentApproval을 true로 켜고 빌드해야 한다**. phase1: 알림 패널(violet 뱃지) 딥링크→pending(신원·범위·연락처 제외 문구)→승인→카운트다운·모바일 / phase2: (SQL 만료·로그 적재 후) "만료됨"→철회 모달→denied |
| `shot-avatarbadge.js` | Avatar·CountBadge·StatusBadge 일괄 교체 — 허브(자녀 teal·연결됨 캡슐·벨 99+)·선수단(Claim 캡슐·카드 아바타)·팀 정보(코치 네이비)·경기(승 teal 틴트)·Records(진행중/예정 캡슐)·모바일. 사전: 벨 99+용 알림 105건 삽입(스크립트 헤더 참고), 허브용 sql-hub.sql — **UserId는 PC마다 다르니 파일 헤더의 조회 명령으로 먼저 확인** |
| `shot-playerpublic.js` | 공개 선수 프로필 /player/{slug} — API(공개 범위 필터·**친선 삽입→집계 불변**·Profile off→NotFound·무소속 임시 선수·없는 slug) + UI(히어로 캡슐·칩·잠금 안내 카피·오렌지 2곳만·팀 링크 왕복·**공개홈 로스터 "공개 프로필 →" 복원**·모바일 하단 CTA·가로 스크롤 0). 임시 데이터는 스크립트가 sqlcmd로 넣고 지운다 |
| `shot-playerpublic2.js` | 공개 선수 프로필 **권한 뷰** — 에이전트 임시 계정(find-or-create)+시드 요청 → 승인 전 공개 뷰 → SQL 승인 → 권한 뷰(Grant·학교·**경기별 기록 친선 포함**·요약은 공식만·**ProfileView 로그 +1**) → 보호자/게스트는 공개 뷰 → SQL 만료 → 폴백 · UI(teal 배너·학교 칩·승인 열람 카드·보호자 연락 CTA·잠금 안내 미노출·모바일). **UI 검증 전 서버 재시작 필수**(Client 수정이 옛 WASM에 안 실림 — 실제로 겪음). 끝나면 전부 원복(임시 계정 물리 삭제 포함) |
| `shot-playerpublic3.js` | 공개 선수 프로필 **카드 뷰 2종** (/player/{slug}/card) — 공개 카드(공개 항목만·QR 캔버스 실렌더 픽셀 검사·스탯 4칩) · **이미지 저장 = CDP 다운로드로 PNG 1080×1350 IHDR 검사** · 링크 공유 클립보드 · 디테일 진입점(PC 버튼·모바일 아이콘) · 권한 카드(승인 열람 블록·보호자 이름 마스킹 김OO·재공유 금지 캡션) · Profile off = 카드도 NotFound. wwwroot JS 수정도 서버 재시작 필수. 전부 원복 |
| `shot-careertab.js` | 공개 팀 홈 **진학·진로 탭** — API(저장 3건→공개 즉시 반영·연도 역순 · 미지 유형/연도 범위/인원 0 거부 · **남의 사례 수정·삭제 거부** · 수정→삭제→복구) + UI(대시보드 팀 정보 관리 카드·폼 RadioCards 3유형·연도 프리필·빈 제출 인라인 · 공개홈 요약 카드 **값 있는 유형만**·태그 3톤·캡션 원문 · 빈 팀 빈 상태 · 모바일). 끝나면 `DELETE FROM SoccerTeamCareerOutcomes`로 원복 |
| `shot-reviewtab.js` | 공개 팀 홈 **리뷰 탭** — API(게스트 무자격 · **재원 판정**(보호자 연결 자녀의 팀 Active 소속) · 작성→마스킹 "김○○ 학부모"·메타 "U15 · 재원 N년차"·MyReviewId · **계정당 1건**(중복 신규 거부) · 무자격/별점 경계/남의 리뷰 삭제 거부 · 수정→삭제→복구) + UI(게스트 쓰기 버튼 없음·평균 파생·재원 확인됨 캡슐 · 보호자 내 카드 ⋯ · 폼 별점 5개 탭+본문→저장 토스트 · 모바일). 끝나면 `DELETE FROM SoccerTeamReviews`로 원복 |

`.sql`은 sqlcmd로 돌린다:

```powershell
sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -E -b -f 65001 -i sql-b5.sql -W
```

**`sql-b6.sql`은 성격이 다르다.** PlayGround에는 심사 API가 없어서(설계 결정 6·7 — 공식 기록의
주체는 주최측) 반영·반려 상태를 앱에서 만들 수 없다. 그 상태의 화면을 보려면 DB에 직접 심어야 하고,
이 스크립트가 하는 일이 곧 **"우리가 만들지 않기로 한 것"의 범위**다. 끝나면 지운다:

```powershell
sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -E -Q "DELETE FROM SoccerRecordCorrections"
```

## 검증 계정 (로컬 전용)

| 계정 | 용도 |
|---|---|
| `verify-teamadmin-0713@test.local` | 검증fc 팀 관리자 (팀 정보·경기·로스터) |
| `verify-empty-0714@test.local` | EmptyFC — 빈 상태 확인용 |
| `verify-player-u12@test.local` | 신준우 (커리어 1건·영상 0건 — 빈 상태부터 시작) |
| `verify-player-u15@test.local` | 김정현 (커리어 2건·영상 3건, 광주광주FCU15 소속) |
| `verify-u15-1@test.local` | 광주광주FCU15 관리자 (선수 사진 권한 검증용) |

비밀번호는 전부 `password123!`. 자세한 시드·재구축 절차는 `Docs/Development/LocalVerification.md`.

## 헤드리스 함정 (여러 번 걸린 것들)

- **Edge 150부터 `puppeteer.launch()`가 실패한다.** `msedge.exe --headless=new --remote-debugging-port=PORT`로
  직접 띄우고 `puppeteer.connect()`로 붙는다 — `shot-connect.js` 참고. `--user-data-dir`은 실행마다 고유하게.
- **A1 폼 필드는 레이블과 입력이 형제가 아니다.** `label[for]` ↔ `document.getElementById()`로 찾을 것.
- **PC·모바일 트리가 둘 다 렌더된다**(한쪽은 CSS로 숨김). `querySelector`는 안 보이는 쪽을 집는다 —
  `getBoundingClientRect().width > 0`으로 보이는 요소만 골라야 한다.
- **스크린샷 직후 `waitForFunction`의 기본 rAF 폴링이 멈춘다** → `{ polling: 300 }` 필수.
- API 지연을 만들 때 전체 스로틀은 WASM 부팅까지 느려져 화면이 안 뜬다 → CDP `Fetch.requestPaused`로
  특정 경로만 붙잡되, **통과용 기본 핸들러를 반드시 함께 등록**할 것(없으면 모든 요청이 영구 정지).

## 원칙

검증에 쓴 데이터는 **끝나고 시드 상태로 되돌린다.** 스크립트가 스스로 정리하거나(‑b3·b5),
정리 SQL을 따로 돌린다. 검증 흔적이 다음 검증의 전제를 흔들면 안 된다.

## 프로시저 배포 함정 (실제로 겪음)

**SQL 파일을 고친 뒤 DB에 다시 배포하지 않으면 조용히 어긋난다.** 컴파일도 통과하고
생성 코드도 최신이라 티가 안 나는데, Dapper가 없는 컬럼을 null로 채워 로직이 잘못 분기한다.
(B6 `UspGetSoccerRecordCorrectionsByManager`에 `TeamId`를 나중에 추가하고 재배포를 빠뜨려
상대팀 이름이 우리 팀으로 나왔다 — 화면에는 안 쓰이던 필드라 B6 검수에서도 안 걸렸다.)

배포 상태 확인은 `LIKE`가 아니라 `CHARINDEX`로 한다 — **`[TeamId]`는 LIKE에서 문자 클래스**라
`'%c.[TeamId]%'`가 엉뚱하게 매칭된다:

```sql
SELECT CASE WHEN CHARINDEX('c.[TeamId]', OBJECT_DEFINITION(OBJECT_ID('프로시저명'))) > 0
            THEN '포함' ELSE 'stale' END;
```

같은 이유로 테스트 스크립트에서 `Name LIKE '[MC]%'`도 의도대로 동작하지 않는다(M 또는 C로 시작하는 이름과 매칭).
