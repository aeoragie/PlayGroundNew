# 로컬 검증 가이드 — 테스트 계정과 화면 확인 방법

로컬 개발 DB 전용이다. 계정·시드는 커밋되지 않으므로 **PC를 옮기면 이 문서대로 재구축**한다.
(선행: `Source/Database/README.md`의 로컬 DB 셋업이 끝난 상태)

## Redis (로그아웃·탈퇴 검증에 필요)

```powershell
.\Others\FetchRedis.ps1              # 최초 1회 — 바이너리는 커밋되지 않는다
.\Others\Redis\start.bat              # 기동 (또는 서비스로 등록 — Others/README.md)
```

없어도 앱은 돌지만 **로그아웃·탈퇴가 토큰을 실제로 끊지 못한다**(fail-open, 기동 시 경고).
연결은 `appsettings.Development.json`의 `RedisConfig`에 `localhost:6379`로 잡혀 있다.

## 검증 계정

비밀번호는 전부 `password123!`.

| 계정 | 팀 | 용도 |
|---|---|---|
| `verify-teamadmin-0713@test.local` | 검증fc (로스터 11명·팀 정보 풀시드) | 팀 대시보드 — 데이터 있는 상태 |
| `verify-empty-0714@test.local` | EmptyFC (빈 팀) | 빈 데이터 숨김(뱃지·칸·카드) 확인 |
| `verify-player-u15@test.local` | 검증fc #1 김정현 (GK 중3) | U15 선수 계정 (프로필·커리어 2·영상 3 시드) |
| `verify-player-u12@test.local` | 검증fc #7 신준우 (MF 초5) | U12 선수 계정 (커리어 1·영상 0 빈 상태) |

- 검증fc·EmptyFC 관리자 계정은 **이메일 첫 로그인 = 가입**(find-or-create) + 팀 온보딩으로 만들고,
  선수 계정 2종은 **Account 시드가 계정까지 생성**한다 (동일 해시 재사용).
- 선수 계정 2종은 검증fc 로스터의 김정현(#1)·신준우(#7)와 `SoccerPlayers.UserId`로 연결된다
  (`Seeds/Verification/VerificationPlayerLinks.Seed.sql`). 나머지 Claimed 선수의 UserId는 표시용 더미.
- **대규모 로스터·리그·경기(Records) 데이터는 이 최소 베이스라인에서 제거됐다** (2026-08-01).
  팀 탐색·대규모 로스터·연령 탭·순위표·기록 화면을 검증할 땐 그 상황에 맞춰 데이터를 생성한다(온디맨드).
  과거 대형 픽스처는 git 히스토리(`f19ad41^`의 `VerificationLeagueTeams`/`VerificationMatches`·
  `VerificationTeamAdmins.Seed.sql`)에서 참조·복원할 수 있다.

## DB 동기화 기준 커밋 (PC 이동 시 필독)

다른 PC에서 작업하고 돌아오면 **로컬 DB가 리포보다 뒤처져 있다** — 시드 몇 개만 돌리면
된다고 넘겨짚지 말 것 (실제로 경기 도메인 테이블 전체가 빠진 채 프로시저만 배포돼
실행 시점 오류로 발견된 적 있음). 아래 절차로 미반영 산출물을 전부 뽑아 반영한다:

1. 미반영 목록: `git diff --name-status <기준 커밋>..HEAD -- Source/Database`
2. 적용 순서: **Tables(신규 CREATE) → Indexes → Migrations(멱등 ALTER — 기존 테이블
   컬럼 추가는 여기) → Procedures(변경분은 `DROP PROCEDURE` 후 재생성) → Seeds**
3. 반영을 마치면 아래 기준 커밋을 갱신하고 함께 커밋한다.

> **이제 뒤처짐을 손으로 찾지 않는다** — `dotnet test Tests/Tests.Infrastructure/Tests.Infrastructure.csproj`
> 가 프로시저 미배포·파라미터 불일치를 전량 자동으로 보고한다(`Testing.md` §5-3).
> 테이블 누락은 프로시저 배포 실패로 드러난다. **DB 작업 전후로 이 테스트를 돌린다.**
>
> **기준 커밋: `b022a51` (2026-08-02, Account 누락분 반영 — NotificationPreferences 테이블 +
> UspDeleteUser·알림 설정 프로시저 3종. 이전 기준 `c74166a`는 이 누락을 담지 못했다)**
> — 이 줄은 "이 커밋까지의 DB 산출물이 로컬 SQLEXPRESS에 반영돼 있다"는 뜻이다.
> DB 산출물이 포함된 커밋을 만들고 로컬에 반영했다면 그 해시로 갱신할 것.
>
> **2026-07-21 전량 동기화 기록** — 이 PC 로컬 DB가 소스보다 크게 뒤처져 있어(테이블 8종·프로시저
> 25종 누락, 마이그레이션 미적용) 한 번에 맞췄다: 누락 테이블 8종(SoccerAgent* 4·Notifications·
> Recruitments·Reviews·CareerOutcomes) 배포 → 마이그레이션 5종(MatchType·Slug·Code6·Relation·
> IsRecruiting) 멱등 적용 → **프로시저 60종 전량 DROP+CREATE로 소스 버전 재배포**(STALE였던 로스터
> 조회 Slug 포함) → 스모크 테스트(모집·리뷰·진학진로·알림·허브·공개홈 전부 정상). 이후로는 이 기준
> 커밋 이하 DB 산출물이 전부 반영돼 있으니, 새 산출물만 위 "적용 순서"대로 얹으면 된다.
>
> **추가 반영(ClaimFlow 코드 없는 연결, 2026-07-21)**: `SoccerPlayerClaimRequests.InviteId` NULL 마이그레이션 +
> `UspGetSoccerClaimCardBySlug`·`UspCreateSoccerPlayerClaimRequestByPlayer` 신규 + `UspReviewSoccerPlayerClaimRequest`·
> `UspGetSoccerPlayerPublicProfileBySlug` 재배포까지 로컬 반영됨.
>
> **추가 반영(온보딩 중복 방지, 2026-07-21)**: `UspCreateSoccerTeamWithRoster` 재배포(관리자 기존 팀 확인 → 멱등 반환)까지 로컬 반영됨.
>
> **추가 반영(ClaimFlow 요청 취소, 2026-07-21)**: `UspCancelSoccerPlayerClaimRequest` 신규까지 로컬 반영됨.
>
> **추가 반영(시각 기준 UTC 통일, 2026-08-03)**: 마이그레이션
> `Migrations/2026-08-03_Utc.TimeBaseline.sql` + 모집 프로시저 7개 재배포까지 로컬 반영됨.
> **이 마이그레이션은 멱등하지 않다** — `StartsAt`·`MatchedAt`을 −9h 시프트하므로 두 번 돌리면
> 두 번 빠진다. 마커 테이블(`SoccerSchemaMigrations`)이 막아 주지만, 다른 PC에서 적용할 때
> **한 번만** 돌리는지 확인한다. 이후 `DeadlineDate`는 없고 `DeadlineAt`(UTC)만 있다.

## 새 PC 최초 셋업 (클론만으로 안 되는 것 — gitignore 대상)

아래를 먼저 끝내야 다음 절의 "재구축 절차"가 돈다.

1. **로컬 DB**: SQL Server 2019+ (UTF-8 콜레이션 필요, 개발은 SQLEXPRESS 기준) 설치 후
   `Source/Database/README.md`의 셋업 명령 실행 (UTF-8 `COLLATE` 포함 생성 → Tables →
   Procedures → Seeds).
2. **시크릿**: `Source/PlayGround/PlayGround.Server/appsettings.Local.json`을
   `appsettings.Local.json.example` 복사로 생성 후 Jwt:Key·OAuth(Google/Kakao) 입력.
3. **Redis**: 위 "Redis" 절 참조 (로그아웃·탈퇴 검증에 필요).
4. **AWS 자격 증명**: `aws configure`로 IAM 사용자 키 등록 (`Docs/Learning/AwsCli.md`) —
   **개발도 이미지 업로드가 S3(dev 버킷)라서** 없으면 업로드·OG 엠블럼이 실패한다.
   오프라인/자격 증명 없이 돌려야 하면 `appsettings.Development.json`의
   `UploadStorageConfiguration.Provider`를 `Local`로 바꾼다 (wwwroot/uploads 디스크 폴백 — 남겨 둔 경로다).
5. **Tailwind**: `cd Source/PlayGround/PlayGround.Client && npm install && npm run css:build`.
6. **실행 확인**: `dotnet run --project Source/PlayGround/PlayGround.Server` →
   `https://localhost:50451` (랜딩) / `/dashboard/team` (팀 대시보드).
   SQL 프로젝트(.sqlproj)는 dotnet CLI로 빌드되지 않음 — VS로 열거나 앱 프로젝트만 빌드.

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

3. 검증fc 팀 정보·선수단 시드 (`Seeds/Verification/`) — 팀 정보(핵심가치·코칭스태프·공식 채널·
   확장 컬럼·엠블럼) + 선수단 11명(사진·연령 그룹·Claim 혼합·초대코드. 온보딩 로스터는 시드가 대체):

   ```powershell
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -b -f 65001 -i Source\Database\Soccer\Seeds\Verification\VerificationTeamInfo.Seed.sql
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -b -f 65001 -i Source\Database\Soccer\Seeds\Verification\VerificationRoster.Seed.sql
   ```

   선수 사진은 Pexels, 엠블럼은 DiceBear 외부 URL이라 인터넷 연결이 필요하다.
   **#1 김정현·#7 신준우가 선수 계정 연결 자리**(다음 단계). 나머지 Claimed UserId는 표시용 더미(NEWID).

4. 선수 계정 + 검증fc 연결 + 프로필·커리어 (Account·Soccer의 `Verification/`).
   **로스터(3번) 이후 실행**하고, 로스터를 다시 돌리면 PlayerId가 재생성되므로 Links도 다시 실행한다:

   ```powershell
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Account -b -f 65001 -i Source\Database\Account\Seeds\Verification\VerificationPlayers.Seed.sql
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -b -f 65001 -i Source\Database\Soccer\Seeds\Verification\VerificationPlayerLinks.Seed.sql
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -b -f 65001 -i Source\Database\Soccer\Seeds\Verification\VerificationPlayerProfiles.Seed.sql
   sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -b -f 65001 -i Source\Database\Soccer\Seeds\Verification\VerificationPlayerCareers.Seed.sql
   ```

   Links는 검증fc #1/#7의 UserId를 D11/D01로 주입한다(팀이 아니라 등번호로 해석 — 리그 시드 의존 없음).
   Profiles = 프로필 데이터(키·몸무게·주발·학교·연락처 + 항목별 공개 설정 + 가족 계정, 김정현은 공개
   설정 2행만 저장해 기본값 병합 검증). Careers = 김정현 커리어 2·영상 3(대표 1), 신준우 커리어 1·영상 0.

> **대규모 로스터·리그·경기(Records) 데이터가 필요한 화면**(팀 탐색·연령 탭·순위표·기록 상세)은
> 검증하는 그 시점에 상황에 맞춰 데이터를 만든다(온디맨드). 과거 대형 시드가 필요하면 git
> 히스토리 `f19ad41^`의 `VerificationLeagueTeams`·`VerificationMatches`·`VerificationTeamAdmins.Seed.sql`을 꺼내 쓴다.

## 화면 검증 방법

1. `https://localhost:50451/login` → 검증 계정으로 로그인.
   - 팀 관리자 계정은 `/dashboard` 진입 시 `/dashboard/team`으로 자동 직행한다 (JWT 역할 분기).
2. **검증fc 계정**: 팀 정보 섹션이 DB 데이터로 렌더되는지 — 엠블럼 이미지(VF, 사이드바·정보
   카드·모바일 상단바), 인증팀 뱃지, 월 회비 `250,000원 · 공개`, 훈련 `주 4회 · 화목금토`,
   핵심가치 3장, 코치 2명(김수연은 "유튜브 미등록"), 공식 채널 2행.
   선수단 섹션(`/dashboard/team/roster`)에서 연령 탭(U12/U15/U18)·Claim 뱃지·카드 뷰 사진 확인.
3. **EmptyFC 계정**: 핵심가치·코칭스태프·공식 채널 **카드 자체가 없어야** 하고, 기본 카드도
   뱃지·요약 칸이 숨겨진 채 팀명만 노출 (빈 데이터 노출 금지 규칙).
4. **선수 계정** (`verify-player-u12/u15`): 로그인 → `/dashboard`가 `/dashboard/player`로
   자동 직행. 프로필 섹션에서 실데이터(키·몸무게·주발·학교·마스킹 연락처)와 공개 토글
   (저장 후 새로고침 유지), 가족 계정 카드 확인. 커리어·포트폴리오는 시드 데이터(김정현 커리어 2·
   영상 3 / 신준우 커리어 1·영상 0). **시즌 통계는 경기 시드가 없어 빈 상태** — 필요 시 온디맨드로 생성.
5. **초대코드 Claim**: 팀 관리자 로스터에서 Unclaimed 선수의 "초대코드 보내기" 클릭 → 코드
   표시·복사. 새 이메일 계정(General)으로 로그인 → `/dashboard/player` "초대코드로 팀 연결"
   카드에 입력 → Player 승격 + 팀 연결. 같은 코드 재사용은 거부됨. **검증 후 Verification 시드
   (Roster→Links→Profiles→Careers) 재실행으로 원복**하고 임시 계정은 삭제할 것.
6. 모바일: 브라우저 폭 480px 이하 — 팀 대시보드 하단 탭 5개(경기 탭은 결과/영상 서브탭),
   선수 대시보드 하단 탭 4개.
7. 로그아웃 대신 다른 계정 확인 시: 시크릿 창을 쓰거나 localStorage의 `pg.accessToken` 삭제.

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
