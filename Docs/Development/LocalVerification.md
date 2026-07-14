# 로컬 검증 가이드 — 테스트 계정과 화면 확인 방법

로컬 개발 DB 전용이다. 계정·시드는 커밋되지 않으므로 **PC를 옮기면 이 문서대로 재구축**한다.
(선행: `Source/Database/README.md`의 로컬 DB 셋업이 끝난 상태)

## 검증 계정

| 계정 | 비밀번호 | 팀 | 용도 |
|---|---|---|---|
| `verify-teamadmin-0713@test.local` | `password123!` | 검증fc (로스터 4명) | 팀 대시보드 — 데이터 있는 상태 |
| `verify-empty-0714@test.local` | `password123!` | EmptyFC (빈 팀) | 빈 데이터 숨김(뱃지·칸·카드) 확인 |

이메일 로그인은 find-or-create라서 **첫 로그인이 곧 가입**이다. 비밀번호가 다르면 로그인 실패이므로
반드시 위 비밀번호를 그대로 쓴다.

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

3. 팀 정보 시드 주입 (검증fc에 핵심가치·코칭스태프·공식 채널·확장 컬럼):

   ```powershell
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -b -f 65001 -i Source\Database\Soccer\Seeds\VerificationTeamInfo.Seed.sql
   ```

## 화면 검증 방법

1. `https://localhost:50451/login` → 검증 계정으로 로그인.
   - 팀 관리자 계정은 `/dashboard` 진입 시 `/dashboard/team`으로 자동 직행한다 (JWT 역할 분기).
2. **검증fc 계정**: 팀 정보 섹션이 DB 데이터로 렌더되는지 — 인증팀 뱃지, 월 회비 `250,000원 · 공개`,
   훈련 `주 4회 · 화목금토`, 핵심가치 3장, 코치 2명(김수연은 "유튜브 미등록"), 공식 채널 2행,
   사이드바·모바일 상단바 팀 요약.
3. **EmptyFC 계정**: 핵심가치·코칭스태프·공식 채널 **카드 자체가 없어야** 하고, 기본 카드도
   뱃지·요약 칸이 숨겨진 채 팀명만 노출 (빈 데이터 노출 금지 규칙).
4. 모바일: 브라우저 폭 480px 이하 — 하단 탭 5개, 경기 탭은 결과/영상 서브탭.
5. 로그아웃 대신 다른 계정 확인 시: 시크릿 창을 쓰거나 localStorage의 `pg.accessToken` 삭제.

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
