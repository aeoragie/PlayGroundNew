# 로컬 검증 가이드 — 테스트 계정과 화면 확인 방법

로컬 개발 DB 전용이다. 계정·시드는 커밋되지 않으므로 **PC를 옮기면 이 문서대로 재구축**한다.
(선행: `Source/Database/README.md`의 로컬 DB 셋업이 끝난 상태)

## 검증 계정

비밀번호는 전부 `password123!`.

| 계정 | 팀 | 용도 |
|---|---|---|
| `verify-teamadmin-0713@test.local` | 검증fc (로스터 11명·팀 정보 풀시드) | 팀 대시보드 — 데이터 있는 상태 |
| `verify-empty-0714@test.local` | EmptyFC (빈 팀) | 빈 데이터 숨김(뱃지·칸·카드) 확인 |
| `verify-u12-1@test.local` | 서울신답FCU12 (30명: 초4·5·6 각 10) | U12 대규모 로스터 |
| `verify-u12-2@test.local` | 서울K리거강용FC (30명, 미인증·회비 없음) | U12 + 빈 항목 혼합 |
| `verify-u12-3@test.local` | 전남순천중앙초 (30명, 학교·회비 비공개) | U12 학교 팀 |
| `verify-u15-1@test.local` | 광주광주FCU15 (42명: 중1·2·3 각 14) | U15 대규모 로스터 |
| `verify-u15-2@test.local` | 부산아이파크U15낙동중 (42명, 미인증) | U15 + 빈 항목 혼합 |
| `verify-u15-3@test.local` | 전북U15군산시민축구단 (42명) | U15 시민구단 |
| `verify-player-u12@test.local` | 서울신답FCU12 소속 신준우 (#1 MF 초6) | U12 선수 계정 (역할 Player) |
| `verify-player-u15@test.local` | 광주광주FCU15 소속 김정현 (#1 GK 중3) | U15 선수 계정 (역할 Player) |

- 위 2종(검증fc·EmptyFC)은 **이메일 첫 로그인 = 가입**(find-or-create)으로 만들고,
  아래 6종(리그 팀 관리자)·선수 2종은 **Account 시드가 계정까지 생성**한다 (동일 해시 재사용).
- 리그 팀 선수 216명은 크롤러 백업 실데이터(팀명·선수명·포지션)에서 샘플링. Claimed 약 1/3,
  학년별 상위 3명에 사진.
- 선수 계정 2종은 리그 팀의 등번호 1번 선수와 `SoccerPlayers.UserId`로 연결된다
  (Soccer의 `VerificationPlayerLinks.Seed.sql`). 나머지 Claimed 선수의 UserId는 표시용 더미.
  선수 대시보드는 미구현 — 현재는 로그인 시 `/dashboard`에서 "선수 대시보드 준비 중" 안내가
  뜨는 것까지가 정상이고, 선수 대시보드(Handoff/Design.PlayerDashboard) 개발·검증용 계정이다.

## 재구축 절차 (새 PC · DB 재생성 후)

1. 서버 실행: `dotnet run --project Source/PlayGround/PlayGround.Server`
2. 계정·팀 생성 — 화면으로 하려면 `/login`에서 위 계정으로 로그인 → 역할 선택(팀 관리자·코치) →
   팀 온보딩(팀명 `검증fc`, 로스터: 김민준 FW 9 / 이서준 MF 8 / 박도윤 DF 4 / 최시우 GK 1).
   EmptyFC 계정도 동일하게 팀명 `EmptyFC`, 로스터 없이 생성.
   API로 자동화하려면 (PowerShell, 서버 기동 상태):

   ```powershell
   [Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
   $login = '{"email":"verify-teamadmin-0713@test.local","password":"password123!"}'
   $r = Invoke-WebRequest -Uri "https://localhost:50451/api/auth/login/email" -Method Post -ContentType "application/json" -Body $login -UseBasicParsing
   $token = ($r.Content | ConvertFrom-Json).data.accessToken
   $team = [Text.Encoding]::UTF8.GetBytes('{"teamName":"검증fc","teamType":"클럽","region":"서울 강동구","roster":[{"name":"김민준","position":"FW","number":"9"},{"name":"이서준","position":"MF","number":"8"},{"name":"박도윤","position":"DF","number":"4"},{"name":"최시우","position":"GK","number":"1"}]}')
   Invoke-WebRequest -Uri "https://localhost:50451/api/soccer/team/me" -Method Post -ContentType "application/json; charset=utf-8" -Headers @{ Authorization = "Bearer $token" } -Body $team -UseBasicParsing

   $login2 = '{"email":"verify-empty-0714@test.local","password":"password123!"}'
   $r2 = Invoke-WebRequest -Uri "https://localhost:50451/api/auth/login/email" -Method Post -ContentType "application/json" -Body $login2 -UseBasicParsing
   $token2 = ($r2.Content | ConvertFrom-Json).data.accessToken
   Invoke-WebRequest -Uri "https://localhost:50451/api/soccer/team/me" -Method Post -ContentType "application/json; charset=utf-8" -Headers @{ Authorization = "Bearer $token2" } -Body '{"teamName":"EmptyFC","roster":[]}' -UseBasicParsing
   ```

3. 검증 시드 주입 — 팀 정보(핵심가치·코칭스태프·공식 채널·확장 컬럼·엠블럼) + 선수단
   (11명: 사진·연령 그룹·Claim 혼합·초대코드. 온보딩으로 만든 로스터는 시드가 대체한다):

   ```powershell
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -b -f 65001 -i Source\Database\Soccer\Seeds\VerificationTeamInfo.Seed.sql
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -b -f 65001 -i Source\Database\Soccer\Seeds\VerificationRoster.Seed.sql
   ```

   선수 사진은 Pexels, 엠블럼은 DiceBear 외부 URL이라 인터넷 연결이 필요하다.
   Claimed 선수의 UserId는 표시용 더미(NEWID) — Account에 실제 사용자는 없다.

4. 리그 팀 시드 (U12 3팀·U15 3팀 + 관리자 계정 6종 — 계정 생성까지 시드가 처리):

   ```powershell
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Account -b -f 65001 -i Source\Database\Account\Seeds\VerificationTeamAdmins.Seed.sql
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -b -f 65001 -i Source\Database\Soccer\Seeds\VerificationLeagueTeams.Seed.sql
   ```

5. 선수 계정 시드 (U12·U15 각 1명 — 계정 생성 + 리그 팀 선수 연결).
   **연결 시드는 리그 팀 시드(4번) 이후에 실행**해야 하고, 리그 시드를 다시 돌리면
   PlayerId가 재생성되므로 연결 시드도 다시 실행한다:

   ```powershell
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Account -b -f 65001 -i Source\Database\Account\Seeds\VerificationPlayers.Seed.sql
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -b -f 65001 -i Source\Database\Soccer\Seeds\VerificationPlayerLinks.Seed.sql
   ```

## 화면 검증 방법

1. `https://localhost:50451/login` → 검증 계정으로 로그인.
   - 팀 관리자 계정은 `/dashboard` 진입 시 `/dashboard/team`으로 자동 직행한다 (JWT 역할 분기).
2. **검증fc 계정**: 팀 정보 섹션이 DB 데이터로 렌더되는지 — 엠블럼 이미지(VF, 사이드바·정보
   카드·모바일 상단바), 인증팀 뱃지, 월 회비 `250,000원 · 공개`, 훈련 `주 4회 · 화목금토`,
   핵심가치 3장, 코치 2명(김수연은 "유튜브 미등록"), 공식 채널 2행.
   선수단 시드(사진·연령 탭·Claim)는 선수단 섹션 백엔드 연동 후 화면에서 확인 가능.
3. **EmptyFC 계정**: 핵심가치·코칭스태프·공식 채널 **카드 자체가 없어야** 하고, 기본 카드도
   뱃지·요약 칸이 숨겨진 채 팀명만 노출 (빈 데이터 노출 금지 규칙).
4. **선수 계정** (`verify-player-u12/u15`): 로그인 → `/dashboard`에 "선수 대시보드 준비 중"
   안내가 뜨면 정상 (JWT 역할 `Player` 분기). 선수 대시보드 구현 후 이 계정으로 본검증.
5. 모바일: 브라우저 폭 480px 이하 — 하단 탭 5개, 경기 탭은 결과/영상 서브탭.
6. 로그아웃 대신 다른 계정 확인 시: 시크릿 창을 쓰거나 localStorage의 `pg.accessToken` 삭제.

## 헤드리스 검증 (자동화 팁)

- 헤드리스 Edge + playwright-core(또는 puppeteer-core)를 스크래치패드에 설치해 사용.
- 로그인 UI를 거치지 않으려면 API로 받은 토큰을 localStorage `pg.accessToken`에 주입 후 진입:

  ```js
  await page.addInitScript(t => localStorage.setItem('pg.accessToken', t), token);
  await page.goto('http://localhost:5000/dashboard/team');
  ```

- Blazor WASM은 부팅이 느리므로 고정 sleep 대신 화면 텍스트를 `waitForSelector`로 기다린다.
  PC/모바일 이중 DOM이라 텍스트 셀렉터는 숨겨진 쪽에 걸릴 수 있다 — 보이는 쪽을 명시(`last()` 등).
- 스크립트는 PowerShell 기준 (`python`은 스토어 스텁). 한글 포함 `.ps1`은 UTF-8 BOM 필수.

## 주의

- 검증 계정·시드는 **로컬 개발 DB 전용** — 운영·스테이징에 넣지 않는다.
- 시드 스크립트는 재실행 안전 (검증fc의 팀 정보 행을 지우고 다시 삽입).
- 계정 자체를 초기화하려면 두 DB에서 해당 이메일 사용자·팀 행을 지우거나 DB를 재생성한다.
