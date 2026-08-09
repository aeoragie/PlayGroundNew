# PlayGround (리뉴얼 — 신규 구축)

유소년 축구(U12~U18) **매칭 플랫폼**. 3대 축(팀 · 선수 · 에이전트) + 공개 경기기록(Records).
개발 순서: **랜딩(Phase 0) → 인증·온보딩 → Team → Player → Records 보강**.

> 기존 `C:\Workspace\PlayGround`의 **기술스택·아키텍처·컨벤션을 계승**하되,
> 코드는 가져오지 않고 **처음부터 새로 구현**하는 리뉴얼 프로젝트다.
> 기존 코드가 필요하면 참고(읽기)만 하고, 복사해 오지 않는다.

## 현재 상태 · 다음 작업 (2026-08-01 갱신)

**디자인 핸드오프 UI 구현 전부 완료.** 랜딩·인증·온보딩, 팀/선수 대시보드, 공개 팀 홈 6탭,
공개 선수 프로필 4뷰(디테일·권한·카드 2종), Claim(초대코드/프로필 경유), 허브 3분기,
팀 탐색·설정 3탭+계정 플로우 3종, 알림 센터(벨 패널 + `/notifications`), 팀 게시판, 일정,
강점 태그, 선수 지원, 모집, 에이전트 열람 승인(**flag off**), OG 메타, Records 목록·상세,
**공식 경기 상세**(경기 행 확장 → 주요 로그 → `/records/match/{id}` 상세: 스코어보드·타임라인·라인업).

> **Records 시드 자산은 최소 베이스라인(마스터 + 검증fc)만 커밋** — 대회·경기 상세 데이터는
> 온디맨드로 생성한다. 경기 상세 확인용 시드 = `Seeds/Verification/OnDemandMatchDetail.Seed.sql`
> (검증fc 조별 1경기 + PK·전후반·주심·감독 + 이벤트 10건 + 홈/원정 라인업). 공식 경기 상세의
> 데이터(전후반·주심·경기순번·감독·카드·교체·등번호·포지션·주장)는 **대회 서비스 SingleIdx 모델
> 선반영**으로 스키마 확장했다(생산자 = 대회 운영 서비스, 아직 미착수 — 아래 큰 덩어리 ①).

> **페이즈별 상세 구현 이력·설계 근거·검증 결과·겪은 함정은 `Docs/History/DevelopmentJournal.md`**
> (2026-08-01 CLAUDE.md에서 이관). 특정 기능이 "왜 이렇게 됐나"를 알아야 하면 거기서 검색한다.
> 이 파일에는 **영속 규칙 + 현재 상태 + 반복 함정 + 미해결**만 압축해 남긴다.

### durable 테스트 안전망 (2026-08-02 구축)

폐기용 스크립트(`shot-*`/`api-*`, 제거됨 — git 히스토리 `f19ad41^`) 대신 xUnit 스위트로 전환.
**Tests.Unit 415 · Tests.Integration 126 · Tests.Infrastructure 251**.
구조·작업 규칙·미착수는 **`Docs/Development/Testing.md`**.

핵심 4가지: ① i18n **생성물 최신성**(생성기가 수동 실행이라 빌드로는 못 잡는다 — 리플렉션으로
전 접근자 호출) ② **표시 문자열 배치 가드**(Domain·Contracts에 한글 리터럴 금지)
③ **DB 프로시저 계약**(배포 여부·파라미터 일치를 전량 자동 — 로컬 DB 없으면 Skip)
④ **인가 누락 가드**(익명 엔드포인트는 명시 목록에만) ⑤ 유즈케이스 가드(인가·경계·정규화).
Tests.Unit이 **PlayGround.Client를 참조**한다(표시 파생·조사 해석이 거기 있다).

미착수: 실제 HTTP 왕복(Mvc.Testing 패키지 필요 — nuget.org 미접근) · E2E 저니 자동화.
E2E 저니 스펙 = `Handoff/TEST.INTEGRATIONSCENARIOS.md`(S1~S7, 현재 수동 검수).

### 배포 (2026-08-02 확정)

**AWS EC2 t3.medium 한 대**(Ubuntu 22.04)에 앱 + SQL Server 2022 Express + Redis를 함께 올린다.
도메인 **playgroundsport.com**(Route 53) · Nginx + Let's Encrypt. 자산·절차는 **`Deploy/README.md`**.

**환경은 1단이고 이름은 `Production`이다.** 투자 유치용 구축 단계라 서버가 한 대지만,
`Development`로 두면 OpenAPI·WASM 디버깅·**상세 예외 페이지**가 공개 URL에 켜지고 HSTS가 빠진다 —
환경 "개수"와 "이름"은 별개 결정이다. 정식 스펙 재구성 시
**Local(개발자) · Dev(공동 테스트) · Staging(정식 QA) · Production(라이브)** 4단으로 확장 예정.

> Ubuntu는 **22.04**다 — SQL Server 2022가 24.04용 저장소를 제공하지 않는다(2025만 있음).

**시각 기준은 UTC 하나다** (H7 완료, 2026-08-03). 저장·비교는 전부 UTC이고, 지역 시각은
표시와 "그 지역 달력" 값에만 쓴다. 서버 시간대도 UTC다.

**시간대를 아는 곳은 `DisplayTime` 하나다** (2026-08-07, 전 세계 대상 확정).
DB·서버는 시간대를 아예 모른다 — 저장·전송이 전부 UTC 순간이라 **경기가 어디서 열렸든 상관없다.**
브라우저만 자기 시간대로 바꿔 보여준다.

| 층 | 금지 | 대신 |
|---|---|---|
| C# | **`DateTime` 타입 자체** (직접 호출 포함) | 순간 = **`SystemTime` 구조체**(항상 UTC, `SystemTime.Now`가 `SystemTime` 반환), 달력 날짜(생년월일·커리어 기간) = **`DateOnly`** |
| SQL | **내장 시각 함수 전부** (`GETUTCDATE()`·`GETDATE()`·`SYSDATETIME()`·`CURRENT_TIMESTAMP`) | **`dbo.UfnSystemDate()`**(UTC). 프로시저 첫머리에서 `DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();`로 받아 `@Now`만 쓴다. 시간대 산술은 SQL에 두지 않는다 |
| Client | 시계 직접 읽기, `ToLocalTime()`·`TimeZoneInfo.Local` | **`DisplayTime`**(Client/Services) — 표시 `Format()`/`ToWallClock()`, 픽커 입력 `FromWallClock()`. **기본은 브라우저 시간대**, 계정 설정은 `Override`로 덮는다 |

**시간 이동(디버그 전용).** 만료·마감처럼 "며칠 뒤"를 봐야 하는 로직을 기다리지 않고 확인한다.
DB는 `SystemClockOffset` 테이블, 앱은 `DebugClock`이 **같은 값**으로 움직인다(한쪽만 옮기면
앱이 넘긴 시각과 DB의 "지금"이 어긋나 거짓 결과가 난다). 서버의 `DebugClockSyncService`가
10초마다 테이블을 읽어 맞춘다.

```sql
UPDATE dbo.SystemClockOffset SET OffsetSeconds = 3*24*60*60;   -- 3일 뒤
UPDATE dbo.SystemClockOffset SET OffsetSeconds = 0;            -- 원복
```

> **RELEASE에는 이 경로가 물리적으로 없다.** C#은 `#if DEBUG`라 컴파일되지 않고,
> DB는 `Debug/` 폴더(오프셋 테이블 + 함수 덮어쓰기)를 **배포하지 않는다** —
> 운영본 `Functions/UfnSystemDate.sql`은 테이블을 읽지 않고 `SYSUTCDATETIME()`만 돌려준다.
> 런타임 플래그(`IsDevelopment()`)보다 강한 보장이다.

**변환은 브라우저에서 한 번씩만 일어난다.** 입력은 `FromWallClock`으로 UTC로 바꿔 보내고
(일정·경기 시각·**모집 마감**), 표시는 `ToWallClock`으로 되돌린다. 마감일도 등록자의 브라우저가
"그 날의 끝"을 UTC 순간으로 만들어 보내므로 **닫히는 순간은 모두에게 같고**, 보는 사람에 따라
달라지는 것은 날짜 라벨뿐이다. 서버는 `[DeadlineAt] > dbo.UfnSystemDate()` 하나로만 판정한다.

`SystemTime`은 타입 수준 강제다(2026-08-06 확장). 어떤 `DateTime`을 넣어도 생성자가 UTC로
정규화하고, DB(Dapper `SystemTimeTypeHandler`)·JSON(ISO-8601 `Z`) 경계는 자동 변환이라
로직 코드가 `DateTime`을 아예 모른다. 원시 `DateTime` 허용 파일은
`TimeBaselineGuardTests.AllowedTypeFiles`(SystemTime·Dapper 핸들러) +
**Client 표시층**(벽시계 `DateTime`을 다루는 것이 표시의 본질)뿐이고, 위반은 같은 테스트가 자동으로 잡는다.
`DisplayTime.ToWallClock()`이 돌려주는 벽시계는 **`Kind`가 `Unspecified`로 고정**이다 — `Local`·`Utc`로
표식하면 누가 `ToUniversalTime()`을 부르는 순간 오프셋이 그대로 샌다.
제너레이터도 datetime2→`SystemTime`, DATE→`DateOnly`로 생성한다.

> **팀에 시간대 컬럼을 두지 않는다** (2026-08-07 판단). 저장이 UTC 순간이라 경기가 어디서
> 열렸든 닫히는 순간·일어난 순간은 하나뿐이고, 지역 정보는 **표시 라벨을 위해서만** 필요한데
> 그건 보는 사람 기준으로 충분하다. 실제로 만들다가 되돌렸다 — 이득 없이 스키마·프로시저·
> 호출부가 전부 무거워졌다.

> **`DATE` 컬럼을 전부 바꾸지는 않았다** — 생년월일·커리어 기간·대회 일정은 `DATE`로 남는다.
> 기준은 "**now와 비교해 상태가 갈리는 날짜만**". 생년월일을 순간으로 만들면 보는 시간대에
> 따라 생일이 하루 밀린다.

### 다음 방향 — 첫 배포까지 (2026-08-02 결정)

**지금 만들어진 것을 다듬고 · 테스트로 굳히고 · 배포한다.** 생산자 서비스(대회 운영·에이전트)는
그 다음이다. 단계·완료 기준·선행 결정(D1~D6)은 **`Docs/Development/ReleasePlan.md`**.
착수 순서: DB 프로시저 계약 테스트 → 전 화면 수동 검수 → 결함·P1 처리 → API 통합 테스트 →
export 내구성·JWT 무효화 → (호스팅 결정 후) 나머지 하드닝 → 배포 파이프라인.

### 미착수 큰 덩어리 (여러 "빈 데이터·flag off"의 원천)

- **생산자 서비스 미착수** — ① 대회 운영 서비스(공식 경기 기록 입력, 설계결정 6·7 — Server
  공유·Client 분리) ② 에이전트 서비스(요청 생성·열람 로그 적재·인증 심사·조건 검색, 설계결정 4).
  이들이 없어 공식 기록 순위표 재계산 호출자 0, 에이전트 표면 flag off, 여러 화면이 빈 데이터.
- **프로덕션 하드닝** — 실제 이메일 발송(현 `LogOnlyEmailSender`)·JWT 무효화(세션 저장소)·
  export 내구성(인메모리 큐)·Linux 배포 SkiaSharp 한글 폰트·CI/CD.
  이미지 원격 저장은 **완료**(2026-08-03) — 운영·개발 모두 오브젝트 스토리지 + `/uploads`
  프록시 서빙, URL 형태는 불변.
  **`UploadStorageConfiguration.Provider`가 어댑터를 고른다**(`DatabaseConfiguration`과 같은 방식) —
  `Local` · `S3` · `Google` · `Azure` 중 **S3만 구현**했고 Google·Azure는 껍질이라 고르면 기동에 실패한다.
  소비자(`Remote*`)는 `IObjectStore`만 보므로 벤더를 아는 곳은 어댑터 한 개뿐이다.
  로컬 디스크 어댑터(`Local*`)는 **안 쓰더라도 남겨 둔다**(자격 증명 없는 PC·오프라인 개발 경로).
- 소소한 잔여(P1): 요청 취소 링크·증빙 사진 첨부·일정 반복/월전환·코치 계정 권한·삭제 시 팀원
  알림 발송 훅·배너 3톤 생산자·공개 선수 프로필 강점 태그 카드 PNG 반영.

### 반복 함정 (실제로 반복해서 겪음 — 재발 방지)

- **직접 URL 진입·게스트 허용 페이지는 인증 상태 선확정 필수** — GNB 컴포넌트(NotificationBell·
  PublicGnb)가 페이지보다 먼저 초기화되면 첫 조회가 헤더 레이스로 401이 샌다. TeamDashboardPage·
  공개홈·알림 페이지가 이 패턴. 페이지가 인증을 먼저 확정한 뒤 자식 GNB를 그린다.
- **Client(.razor/wwwroot JS) 수정 후 서버 재시작 필수** — `dotnet run`이 기동 시점 WASM을 계속
  서빙해 API만 새 코드로 도는 반쪽 상태가 된다(UI만 전부 FAIL로 보임). 전체 taskkill → 포트 5000
  free 확인 → 단일 기동.
- **Tailwind content 글롭이 `Styles/**/*.cs`만 스캔** — 클래스 문자열을 담은 .cs는 반드시
  `Styles/`에 둔다(Components/에 두면 클래스가 생성 안 돼 색이 죽는다). 새 클래스 추가 후
  `cd Source/PlayGround/PlayGround.Client && npm run css:build`.
- **Razor: 한글 바로 뒤 `@메서드`/`@프로퍼티`는 이메일 리터럴로 오인** → 그대로 출력된다.
  단일 식(`@SegmentLabel("전체", n)`)이나 괄호(`@(Max)개`)로 감싼다. `@{ var x=…; }` 인라인 금지.
- **PowerShell로 .js·한글 문자열 치환 금지** — Get-Content가 UTF-8(BOM 없음)을 ANSI로 읽어 한글이
  깨진다. 스크립트/문자열 수정은 Edit/Write 도구로.
- **헤드리스 검증**: Edge 150+는 `puppeteer.launch` 실패 → `--headless=new --remote-debugging-port`로
  직접 띄우고 `puppeteer.connect`. PC·모바일 트리 둘 다 렌더되므로 `getBoundingClientRect().width>0`로
  보이는 요소만 고른다. 리다이렉트는 await 뒤 발생 → URL 멈출 때까지 대기, 경로 비교 `startsWith` 금지.

### DB 동기화 (다른 PC / 재구축)

로컬 DB가 소스보다 뒤처져 프로시저만 배포된 채 런타임 오류가 반복 발생했다. **다른 PC/새 작업
전 `Docs/Development/LocalVerification.md`의 DB 동기화 절차를 먼저 수행**한다. 마이그레이션은
`Source/Database/Soccer/Migrations/`(+ Account)에 날짜별로 있으니 미적용분을 순서대로 적용하고,
프로시저 SQL은 재배포한다. 새 PC 최초 셋업은 `Source/Database/README.md` + LocalVerification.md.

## 핵심 설계 결정 (2026-07-11 확정)

1. **축구 전용** — UI·스키마 모두 축구만. 멀티스포츠 추상화(SportId/SportConfig 등)를
   만들지 않는다. 확장은 실제 필요가 생겼을 때 진행.
2. **클린 아키텍처 유지** — Core(재사용 범용)와 PlayGround(프로젝트 전용)를 분리하고,
   아래 "프로젝트별 역할과 규칙"의 참조 방향을 절대 위반하지 않는다.
3. **최소 골격에서 출발 (YAGNI)** — 기능은 화면/유즈케이스 단위로 설계 확정 후 추가한다.
   미리 만들어 두는 코드 금지. **단, 4·5번은 "나중에 추가하는 비용이 훨씬 비싼" 확정 항목이라 예외.**
4. **에이전트 축 선반영** — 에이전트는 반드시 도입 예정. 스키마(AgentProfile, PlayerAgentLink,
   TeamRecommendation, Tournament.OrganizerId/Type, Commission, AgentReview, CompetitionStaff)는
   선반영하되, API는 `[Authorize(Roles="Agent,AgencyAdmin")]` 가드, UI는 feature flag로 숨긴다.
5. **KFA 데이터는 어댑터 경유 자체 DB화** — 직접 연동이 아니라 읽어와서 우리 DB에 적재한다.
   API를 쓰게 되더라도 `IExternalMatchProvider` 어댑터가 내부 구조에 맞춰 리턴.
   Match/Tournament에 `DataSource(Manual/AgentHosted/KfaApi)` · `ExternalRef`(멱등키) · `SyncStatus` 선반영.
6. **대회 운영은 별도 웹 서비스로 분리** — **Server는 공유, Client 프로젝트만 분리**
   (착수 시 `PlayGround.Competition.Client` 신규 추가 — 지금 만들지 않는다).
   대회 서비스는 Tournament/Match/CompetitionStaff에만 쓰기, Team/Player는 읽기 전용. 인증은 SSO 공유.
7. **공식 경기 기록의 주체는 주최측** (2026-07-19 확정) — 대회·리그 경기 결과는 주최측이 입력하고,
   **팀·선수에게는 읽기 전용**이다. 팀이 기록 오류를 발견하면 직접 고치는 게 아니라 **수정 신청**을
   올리고 주최측이 반영한다(플로우 명세는 `Handoff/Design.RecordCorrection` — 위 B6).
   - **팀이 직접 입력하는 것은 연습경기·친선경기뿐이다.** B1에서 만든 결과 입력 폼은 이 용도로 남긴다.
   - 따라서 **팀 대시보드 입력 경로에는 대회·리그 선택이 없어야 하고**, 순위표 재계산(D5)도
     팀 입력 경로에서는 발생하지 않는다 — 재계산은 주최측 입력 경로의 책임으로 옮겨간다.
   - 현재 코드는 아직 대회 선택이 남아 있다(B1 절 "정리 필요" 참조). 기능은 살아 있으니 급히 걷어내지
     않고, 주최측 입력 경로를 설계할 때 함께 정리한다.

## 기술 스택

- **.NET 10.0** / C#
- **Blazor WebAssembly** (SPA 프론트엔드, Server가 호스팅)
- **ASP.NET Core Web API** (REST API 서버)
- **Entity Framework Core 10.x** (CRUD, 마이그레이션) + **Dapper** (SP 호출, 고성능 조회)
- **SQL Server** (주 저장소) + **Redis** (캐시)
- **ASP.NET Core Identity + JWT** (인증/인가)
- **Tailwind CSS** (유틸리티 기반 스타일링)
- **NLog** (로깅)
- **xUnit(v3), Moq, FluentAssertions** (테스트)

## 프로젝트 구조

```
PlayGroundNew/
├── PlayGround.slnx                    솔루션 (신형 XML 포맷 — .sln 아님에 주의)
├── Directory.Build.props              빌드 출력 경로 중앙 관리 (Binary/, Intermediate/)
├── Directory.Packages.props           NuGet 패키지 버전 중앙 관리 (CPM)
├── .editorconfig                      코딩 스타일 규칙
│
├── Source/
│   ├── Core/                          (재사용 가능한 범용 레이어 — PlayGround 비종속)
│   │   ├── Shared/                    → Core.Shared.csproj (네임스페이스 PlayGround.Shared)
│   │   └── Infrastructure/            → Core.Infrastructure.csproj (네임스페이스 PlayGround.Infrastructure)
│   │
│   ├── PlayGround/                    (PlayGround 프로젝트 전용 레이어)
│   │   ├── PlayGround.Contracts/      Client/Server 공유 DTO
│   │   ├── PlayGround.Domain/         엔티티, 도메인 Enum, 비즈니스 규칙
│   │   ├── PlayGround.Application/    유즈케이스 (Command), 인프라 포트
│   │   ├── PlayGround.Persistence/    DB 접근 구현 (EF Core, Dapper, Repository)
│   │   ├── PlayGround.Server/         ASP.NET Core API + Blazor 호스팅
│   │   ├── PlayGround.Client/         Blazor WebAssembly 프론트엔드
│   │   └── (예정) PlayGround.Competition.Client — 대회 운영 전용 Client (착수 시 추가, Server 공유)
│   │
│   └── Database/                      SQL 원본 (버전 관리의 단일 진실 소스)
│       ├── Account/ Database.Account.sqlproj  인증·신원 DB (SSO 공유), SDK 스타일 SQL 프로젝트(dacpac)
│       └── Soccer/  Database.Soccer.sqlproj   도메인 DB (Team, Player, Match, Agent, Content)
│           └── (각) Schema/ Tables/ Procedures/ Queries/ Indexes/ Seeds/
│
├── Tests/
│   ├── Tests.Unit/                    단위 테스트 (Domain, Application, Core.Shared, Client 순수 로직)
│   ├── Tests.Integration/             통합 테스트 (Server 서비스)
│   └── Tests.Infrastructure/          인프라 테스트 (DB 계약 등 외부 의존 — 없으면 Skip)
│
├── Docs/                              문서 (설계 근거·개발 절차)
│   ├── Architecture/                  설계·구조 (설정 주입 플로우·i18n·네이밍 등)
│   ├── Development/                   작업 절차 (로컬 검증·테스트·릴리스 계획)
│   ├── Learning/                      기반 기술 개념 정리 (AWS 네트워크 등)
│   └── History/                       구현 이력 (DevelopmentJournal)
│
├── Deploy/                            **배포는 여기 하나로 완결** — 문서 + 서버에 올라가는 실행물
│   ├── README.md                      구성·규칙·설치·배포 후 확인
│   ├── AwsSetup.md                    AWS 콘솔 클릭 가이드 (최초 1회)
│   ├── ManualSetup.md                 ec2-setup.sh를 손으로 따라가기 (이해용, 최초 1회)
│   ├── Servers.md                     서버 목록(IP·포트·경로) + 자주 쓰는 명령
│   ├── Runbook.md                     장애 대응 (증상으로 찾는다)
│   └── ec2-setup.sh · deploy-app.sh · backup-database.sh · playground.service · playground.conf
│
└── Others/                            로컬 개발용 외부 실행물 (제품 소스 아님)
    ├── README.md                      무엇을·왜·어떻게
    ├── FetchRedis.ps1                Redis 내려받기 + 해시 검증 (바이너리는 커밋 안 함)
    └── Redis/                         gitignore — 스크립트로 받는다
```

## 프로젝트별 역할과 규칙 (반드시 준수)

### Core.Shared — 범용 유틸리티

- **역할**: 어떤 프로젝트에서도 재사용 가능한 순수 .NET 코드.
  `Result<T>` 모나드, `Envelope<T>`/`PagedData<T>` 래퍼, 확장 메서드, 범용 검증.
- **참조**: 없음 (NuGet 포함 외부 의존 최소화).
- **금지**: 도메인(축구/선수/팀) 특화 코드, 외부 라이브러리 의존.

### Core.Infrastructure — 외부 라이브러리 래핑

- **역할**: 외부 기술을 프레임워크에 맞게 래핑. DB 기반 클래스(RepositoryBase,
  CommandBase, QueryBase, ProcedureBase 등), Redis 래핑(RedisService/RedisSession),
  NLog 설정(LoggingExtensions), 텔레메트리/복원력 확장(ServiceDefaultsExtensions).
- **참조**: Core.Shared만.
- **금지**: PlayGround.* 참조 (PlayGround 비종속이어야 다른 프로젝트에서 재사용 가능).
- **Akka Actor 래핑(`Actor/`)**: Controller → Database 전달 과정의 비동기 처리에 사용.
  `AkkaService`(IHostedService)가 ActorSystem 생명주기를 관리하고, 액터 생성은 DI 리졸버
  경유(`CreateActor`/`CreateRouter`/`CreateHashRouter`) — 액터 생성자에 서비스 주입 가능.
  요청-응답은 `ActorRef.SendAsync(message, timeout)` 사용 (타임아웃 시 `ActorResultCode.Timeout`).
- **다중 결과셋 SP**: `ProcedureMultipleAsync`는 `MultiQueryReader`를 반환 —
  반드시 `using`으로 dispose (커넥션 소유권 포함).

### PlayGround.Contracts — 공유 DTO

- **역할**: Client와 Server가 함께 쓰는 요청/응답 DTO. 도메인별 폴더에
  `{Domain}Contracts.cs` 하나로 통합 (예: `Team/TeamContracts.cs`).
- **참조**: Core.Shared만 (2026-08-06, 시각 타입 `SystemTime` 공유 목적).
- **금지**: 로직(메서드), 엔티티, 외부 의존. 순수 데이터 클래스만.
  시각 필드는 순간이면 `SystemTime`, 달력 날짜면 `DateOnly` — `DateTime` 금지.

### PlayGround.Domain — 도메인 모델

- **역할**: 엔티티, 값 객체, 도메인 Enum(포지션·경기상태 등), 도메인 특화
  ResultCode, 순수 비즈니스 규칙.
- **참조**: Core.Shared만.
- **금지**: 외부 라이브러리(EF Core 포함) 의존, DB/HTTP 등 인프라 관심사.

### PlayGround.Application — 유즈케이스

- **역할**: API 하나 = 유즈케이스 하나. `{기능}/Commands/`, 인프라 포트 인터페이스(`Interfaces/`),
  Entity↔DTO 매핑(`Mappers/`), 입력 검증(`Validators/`).
- **참조**: Domain, Contracts, Core.Shared.
- **금지**: Persistence/Server 참조, DB 직접 접근 (반드시 포트 인터페이스 경유).
- **네이밍 (필수)**:
  - **유즈케이스는 읽기/쓰기 무관 `{기능}Command`** — 액션 동사(Get/Create) 붙이지 않는다.
    `Command`는 CQRS의 '쓰기 전용'이 아니라 **GoF Command = 실행 가능한 비즈니스 동작**의 의미.
    (예: 조회도 `SoccerLandingContentsCommand`, 생성도 `SoccerPlayerProfileCommand`.) 폴더는 `{기능}/Commands/`.
  - **기술 역량·외부 어댑터는 `{역량}Service`** (인증·JWT·해시·외부 API 등). 유즈케이스가 아니라 유즈케이스가 *의존하는* 수단.
    (예: `OAuthService`, `JwtTokenService`, `PasswordHasherService`.) 판별: "비즈니스 동작이면 Command, 갖다 쓰는 기술 수단이면 Service."
  - **축구 전용 유즈케이스는 `Soccer` 프리픽스** (종목 접두 규칙). 상세·근거는 `Docs/Architecture/NamingConventions.md`.

### PlayGround.Persistence — DB 구현

- **역할**: Application이 정의한 포트의 구현체. EF Core DbContext·마이그레이션,
  Dapper SP 호출, Repository 구현.
- **참조**: Application, Domain, Contracts, Core.Shared, Core.Infrastructure.
- **금지**: 비즈니스 규칙 (규칙은 Domain/Application에, 여기는 저장·조회만).

### PlayGround.Server — API 서버

- **역할**: ASP.NET Core 컨트롤러, 인증/인가(JWT), DI 구성, Blazor Client 호스팅.
  컨트롤러는 얇게 — 유즈케이스 호출 + `Envelope<T>` 응답 변환만.
- **참조**: 모든 레이어.
- **규칙**: URL은 `api/{role}/me/{resource}`(본인 데이터), `api/{role}/{resource}`(검색).
  응답은 항상 `Envelope<T>`.
- **종목별 분리**: Server는 여러 스포츠 종목을 함께 호스팅한다. 컨트롤러는 종목별로 분리 —
  폴더 `Controllers/{Sport}/`, 네임스페이스 `...Controllers.{Sport}`, 클래스 `{Sport}XxxController`
  (예: `Controllers/Soccer/SoccerLandingController`), 라우트 `api/{sport}/...`.
  (SportId/SportConfig 같은 추상화는 만들지 않는다 — 단순 명명·폴더 분리만.)

### PlayGround.Client — Blazor WASM 프론트엔드

- **역할**: SPA UI. Layout, Pages, 재사용 컴포넌트, API 통신 서비스, 인증 상태 관리.
- **참조**: Contracts, Domain, Core.Shared. (서버 레이어 참조 불가 — HTTP로만 통신)
- **규칙**: **하나의 시각 패턴은 한 곳에만.**
  우선순위 = 컴포넌트(.razor) > 시맨틱 클래스/상수(`Styles/Css.*.cs`) > 페이지에 raw 유틸 직접(금지).
  같은 마크업이 2번째 등장하면 즉시 컴포넌트로 추출. 새 화면 = "공용 컴포넌트에서 먼저 찾고 없으면 만든다".
- **공용 컴포넌트 (`Components/Shared/`)** — 새 화면에서 우선 재사용:
  - `PillButton` (Variant: Orange/Ghost/White/Navy × Size: Small/Medium/Large/ExtraLarge, `Class`로 배치 지정)
  - `BrandLogo` (`Href` null이면 정적, `Compact` 크기, `InheritColor` 색상 상속)
  - `CardTitle`/`CardText` (`SizeClass`로 뷰포트별 크기, `Inverted`로 어두운 배경 대응)
  - `SectionHeader` (오버라인 + H2, `BottomMarginClass`)
  - 도메인 카드는 `Components/Landing/` 등 기능 폴더에 (예: `RoleCard`).

### Source/Database — SQL 원본 (Account / Soccer 2-DB)

- **역할**: 테이블 DDL, 저장 프로시저, 인덱스의 단일 진실 소스. DB 배포는 이 파일 기준.
- **분리**: `Account`(인증·신원, SSO 공유 대비) / `Soccer`(도메인). 논리 DB는
  `DatabaseTypes` enum(Account/Soccer)과 매핑, 커넥션은 `DatabaseConfiguration` 섹션.
- **DB 간 FK·트랜잭션 불가** — `SoccerPlayers.UserId → Account.Users.Id`는 앱 계층 정합성.
  두 DB 걸치는 작업(온보딩)은 Account 먼저 → 성공 시 Soccer 순서 (분산 트랜잭션 회피).
- **종목 프리픽스** — 타 스포츠 도입 대비, **Soccer 도메인 테이블은 `Soccer` 프리픽스**
  (`SoccerPlayers`, `SoccerTeams`, `SoccerLandingContents`). 생성물도 자동으로 `Soccer{테이블}Entity`,
  프로시저 결과 Record도 `Soccer~Record`. **프로시저는 `Usp*` 유지**(네임스페이스로 종목 구분).
  **Account(공용 신원)는 프리픽스 없음**(`Users`, `SocialAccounts`).
- **규칙**: 테이블명 PascalCase 복수형, 컬럼명 PascalCase(`PlayerId`),
  프로시저 `Usp` 접두사. 스키마 변경은 반드시 SQL 파일 먼저 수정. 상세는 `Source/Database/README.md`.
- **코드 생성**: `Source/Tools/Generator.Database`가 SQL 파일을 읽어 Entity/Procedure/Query C#를
  `PlayGround.Persistence/Database/Generated/{Account,Soccer}.{Entities,Procedures,Queries}`에 생성.
  실행: `cd Source/Tools/Generator.Database && dotnet run` (경로 상대라 이 폴더에서 실행).
  생성물은 `// <auto-generated />` — 수동 편집 금지, SQL 수정 후 재생성.

### Tests.* — 테스트

- **Tests.Unit**: 외부 의존 없는 순수 단위 테스트 (Domain, Application, Core.Shared, **Client 순수 로직**).
- **Tests.Integration**: API 엔드포인트 통합 테스트 (Server 참조).
- **Tests.Infrastructure**: 실제 DB/Redis가 필요한 테스트.
- 상세는 `Docs/Development/Testing.md` — 새 유즈케이스에 붙일 최소 4종 가드, 리소스 테스트가
  자동으로 덮는 범위, **새 테스트는 일부러 깨뜨려 확인**하는 규칙.

### 의존성 그래프

```
Core.Shared (의존성 없음)          PlayGround.Contracts (Core.Shared 참조 — SystemTime)
  ↑                                  ↑
Core.Infrastructure                PlayGround.Domain (Core.Shared 참조)
  ↑                                  ↑
  │                                PlayGround.Application (Domain, Contracts, Core.Shared)
  │                                  ↑
  └────────────── PlayGround.Persistence (Application, Domain, Contracts, Core.Shared, Core.Infrastructure)
                                     ↑
                  PlayGround.Server (모든 레이어)

PlayGround.Client (Contracts, Domain, Core.Shared) — Server와는 HTTP만
```

**새 코드를 어디에 둘지 판단 기준**: "이 코드가 PlayGround가 아닌 다른 프로젝트에서도
쓸 수 있는가?" → Yes면 Core, No면 PlayGround. "DB/외부 기술을 아는가?" →
Yes면 Infrastructure/Persistence, No면 Shared/Domain/Application.

## 데이터 흐름 패턴

- **내부 로직**: `Result<T>` 모나드로 함수형 에러 처리 (예외는 예외 상황에만)
- **API 응답**: `Envelope<T>` + 페이징은 `PagedData<T>`

## 로깅 규칙 (필수)

**로직을 작성할 때는 반드시 로그를 함께 작성한다.**

### 계층별 책임

| 계층 | 로깅 책임 |
|---|---|
| Core.Shared | 로그 없음 — Result가 곧 반환값 |
| Core.Infrastructure | **Trace/Debug 진단만** (SQL 실행시간, 재시도 등) + 생명주기 Info. 오류는 Result/Exception으로 반환하고 **Error 로그 금지** (중복 방지) |
| **Application (유즈케이스)** | **비즈니스 로그의 주 책임 계층** — 맥락(누가·무엇을)을 아는 곳에서 로깅 |
| Persistence | 로그 없음 — Result로 반환만 한다 (진단은 base가 Debug로) |
| Server (Controller) | 최소화 — 컨트롤러는 얇게 |

### 레벨 기준

| 레벨 | 기준 | 예 |
|---|---|---|
| **Info** | **상태를 바꾸는 일 + 서비스 초기화** — 유즈케이스가 식별자를 담아 직접 남긴다 | 로그인, 팀 생성, 승인/반려, S3 어댑터 초기화 |
| Debug | 개발 진단 · **성공한 유즈케이스** | SQL 실행시간, 캐시 히트, 조회 성공 |
| Trace | 상세 덤프 (평소 꺼둠) | 파라미터 전체 |
| Warn | 자동 복구된 이상 (NotFound 같은 정상적 빈 결과는 Warn 아님) | 재시도 후 성공, 폴백 사용 |
| **Error** | **요청 실패는 반드시 남긴다** | 유즈케이스 실패, 예외 → Result 변환 지점 |
| Fatal | 프로세스 지속 불가 | 기동 실패, 설정 누락 |

성공한 조회는 아무것도 남기지 않는다 — 경계의 `LogWith`가 성공이면 Debug다. 서비스 초기화 로그에는 **어떤 구성으로 떴는지**(Provider·Endpoint 등)를 함께 남긴다.

### 포맷·헬퍼 (Core.Shared/Logging — MEL `ILogger` 확장)

- **메시지 포맷: `문장. { Key:Value, Key:Value }`** — 헬퍼가 자동 생성 + 구조화 속성 동시 기록.
- `Logger.InfoWith("Team created", ("TeamId", id))` — Trace/Debug/Info/Warn/Error/Fatal 각 `~With` 제공.
  식별자는 반드시 이 필드로 넘긴다. **문자열 보간(`$"Team {id}"`) 금지** — 검색·집계가 안 된다.
- **실패 Result를 받은 로직은 반드시 `result.LogWith(Logger, "작업명")` 호출** — 코드 종류가 곧 레벨이다
  (`ErrorCode`→Error, 그중 `IsCritical`만 Fatal · `WarningCode`→Warn · 나머지→Info).
  라이브러리가 Error를 남기지 않으므로 이걸 빼먹으면 오류가 로그에 남지 않는다.
- **한 요청 = 한 줄, 한 실패 = 한 줄.** requested/received 짝으로 남기지 않고 결과 시점에 한 줄.
  아래층이 로깅하고 위층이 또 하면 같은 실패가 3줄이 된다.
- **catch에서 Result로 바꿀 때는 예외 객체째로** — `ErrorWith(ex, …)`. `ex`를 빼면 스택이 사라진다.
- 로깅 금지: 패스워드·토큰·API 키 + **이메일·전화번호·생년월일·주소**(식별은 `UserId`로). 메시지는 영어.

> 상세·근거·반복해서 틀린 것들은 **`Docs/Architecture/Logging.md`**. 위반은 `LoggingGuardTests`가 잡는다.

## UI 구현 규칙 (SPEC 기반 — 필수)

1. **UI 작성 전 해당 화면의 SPEC 문서 필독** (`Handoff/*/SPEC*.md`).
   섹션 순서·카피·컴포넌트 구성을 임의로 변경/추가/삭제하지 않는다. 카피는 한 글자도 바꾸지 않는다.
2. **디자인 토큰(tailwind.config)만 사용, 색상 하드코딩 금지.**
   토큰 정의: `PlayGround.Client/tailwind.config.js` + `Styles/app.tailwind.css`(CSS 변수).
   오렌지(`#FF6B35`)는 **CTA 전용, 전체의 5~10%만**.
3. **한글 버튼/pill은 `white-space:nowrap`**, 한글 문단은 `word-break:keep-all`(모바일 필수).
4. **빈 데이터 노출 금지** — 통계·리뷰 등 데이터가 없는 시기엔 해당 섹션 자체를 넣지 않는다.
5. 디자인 레퍼런스 HTML(`Handoff/*/*.html`)은 브라우저로 열어 시각 비교.
6. **섹션/화면 단위로 작게 구현하고 사람이 검수 후 다음 단계 진행.**
7. Tailwind 빌드: `cd Source/PlayGround/PlayGround.Client && npm run css:build` (watch는 `css:watch`).
8. **사용자 노출 문자열 하드코딩 금지 (i18n)** — 새 문구는 `wwwroot/i18n/{Domain}.ko.json`(+`.ja.json`)에
   키를 추가하고 `cd Source/Tools/Generator.Localization && dotnet run` 후 `@AppText.{Domain}.{Key}`로 쓴다.
   ko 값은 SPEC 카피 그대로. **문화권 분기 금지** — 한국어 조사는 리소스 모디파이어 `{0:이/가}`로 표현한다.
   구조·키 컨벤션·이관 절차·제외 대상은 **`Docs/Architecture/Localization.md`**.
   **이관 완료**(2026-08-02) — `.razor`·`Models/`·`Services/` 잔여 0, 15도메인 1,817키 × ko/ja.
   열거형 표시 라벨은 `Enums`, API 폴백 실패 메시지는 `Errors` 도메인(횡단 관심사).

## 빌드 & 테스트

- **빌드**: `dotnet build PlayGround.slnx`
- **테스트**: `dotnet test PlayGround.slnx`
- **실행**: `dotnet run --project Source/PlayGround/PlayGround.Server` (Client 포함 호스팅)
- 빌드 출력: `Binary/`, 중간 산출물: `Intermediate/` (git 제외)
- 패키지 추가 시: `Directory.Packages.props`에 버전 등록 → csproj에는 버전 없이 `<PackageReference Include="..." />`

---

# 코딩 컨벤션

## 파일 네이밍 — "읽는 생태계의 관례를 따른다"

한 가지 케이스로 통일하지 않는다. 도구가 이름을 고정한 파일(`appsettings.json`·`package.json`·
`.gitignore`)이 이미 소문자라 애초에 불가능하고, **리눅스는 대소문자를 구분해서
Windows에서 멀쩡하던 참조가 서버에서만 깨진다.**

| 대상 | 규칙 | 예 |
|---|---|---|
| **우리 소유** — `.cs` `.sql` `.md` `.ps1` | **PascalCase**, 하이픈 금지(구분은 점) | `KoreanParticleTests.cs` · `VerificationRoster.Seed.sql` · `FetchRedis.ps1` |
| **리눅스 실행물** — `.sh` `.service` `.conf` | **소문자 + 하이픈** | `ec2-setup.sh` · `playground.service` |
| **외부 생태계 관례** — `.yml`(GitHub Actions) | 그 관례(소문자) | `ci.yml` · `deploy.yml` |
| **도구가 고정** | 그대로 | `appsettings.json` · `nlog.config` |

- 하이픈 금지는 **우리 소유 파일에만** 적용한다 — 리눅스 실행물은 하이픈이 관례이고,
  설치되는 이름도 이미 하이픈이다(`playground-deploy`·`playground-backup`).
- 마이그레이션 앞의 ISO 날짜(`2026-08-01_…`)는 날짜 표기 자체라 하이픈이 맞다.
- 테스트 클래스 파일은 `{대상}Tests.cs`.

## C# 네이밍

- **클래스, 메서드, 속성, 상수(const), static 필드**: PascalCase (`private static readonly Logger`도 포함)
- **지역 변수, 매개변수**: camelCase
- **private 인스턴스 필드**: `m` 접두사 + PascalCase — **readonly여도 m 접두사** (예: `mConnectionString`, `mHttp`, `mRepository`).
  (static/const만 위의 PascalCase. `private readonly`가 static/const 규칙과 겹칠 때는 이 규칙이 우선.)
- **인터페이스**: `I` 접두사 (예: `IPlayerRepository`)
- **비동기 메서드**: `Async` 접미사 필수 (예: `GetPlayerByIdAsync`)

## C# 포매팅

- **들여쓰기**: 공백 4칸. **중괄호**: Allman 스타일 (여는 중괄호 새 줄).
- **모든 제어문에 중괄호 필수** — `if (x) return;` 한 줄 작성 금지.
- **var**: 타입이 명확할 때(`new`, 캐스트, 리터럴)만. 기본 타입은 명시적 선언.
- **네임스페이스**: block scoped (`namespace Foo { }`).
- **LINQ 체이닝**: 메서드마다 새 줄, 첫 메서드와 동일 들여쓰기 레벨(계단식 금지).
- **패턴 매칭 선호**: `as`+null 체크 대신 패턴 매칭, `switch` 문 대신 `switch` 식.
- **using 선언문 선호**: `using var x = ...;`
- **값 정렬 금지**: `=` 열 맞춤하지 않음.
- **블록 섹션 주석**: `// ────` 장식 금지. `//.// 섹션명` 형식 (앞뒤 빈 줄).

## using 지시문 순서

그룹 순서: **System → Microsoft → 3rd Party → Core → PlayGround** (그룹 간 빈 줄 없음,
그룹 내 알파벳순, Core/PlayGround는 의존성 낮은 순).

## 방어적 코딩

- **모든 public 메서드**: 매개변수 유효성 검증 + `Debug.Assert`
- **예상 못한 상황**: `Debug.Assert(false, "설명")` 후 안전한 반환
- **기반 라이브러리(`Core.*`)는 예외를 던지지 않는다** — 어디서 어떻게 불릴지 모르는 코드가
  던지면 호출자가 예상 못한 곳에서 터진다. `Debug.Assert` + `Result<T>` 반환이 기본.
  실패할 수 있는 연산은 `TryXxx`가 `Result<T>`를 돌려주고(`out` 파라미터 금지), 짝인 `Xxx`는
  유효성을 호출자가 보장할 때 쓰는 버전이다(BCL `Parse`/`TryParse` 관례).
- **계속할 수 없는 경로는 직접 던지지 않고 `Panic.Fail` 하나를 거친다** — DEBUG에서는 죽고
  RELEASE에서는 예외를 던진다. `Result`에서 값을 강제로 꺼낼 때는 `GetValueOrPanic()`.
  그 외 예외가 남는 자리는 **반환 통로가 없는 곳뿐**이다 — 생성자·정적 등록(기동 시 실패),
  BCL 계약(`IComparable`).
- **`Result<T>`를 반환하는 메서드 안에서 다른 `Result`를 받으면 그대로 흘려보낸다** —
  `Result<T>.Success(inner)`로 다시 감싸지 않는다(실패가 성공으로 둔갑한다).

## 주석 & 로그

- **가장 좋은 주석은 쉬운 코드다.** 주석도 사실상 코드다 — 코드가 바뀌었는데 주석이 안 바뀌면
  잘못된 정보를 전달한다. 코드를 보면 뻔히 아는 내용의 주석을 달지 않는다.
  - 남긴다면 **역할과 전체 흐름** 수준의 구조적 설명으로. 줄 단위 동작 설명은 쓰지 않는다.
  - **상세한 설명은 핵심 로직·주의할 점 같은 특수 케이스로 한정**한다.
  - 이름으로 알 수 있으면 생략. 배경과 근거는 커밋 메시지와 `Docs/`에 남기고 코드는 가리키기만 한다
    (문서에 있는 설명을 코드에 복제하면 두 곳이 어긋난다).
  - 한글, 간결하게.
- **로그/예외 메시지**: 영어 (`Logger.LogError(ex, "Failed to retrieve player")`).
- **민감정보 로깅 금지**: 패스워드, 토큰, API 키.

## 문서 문체 (2026-08-03 확정)

- **긴 대시(—)와 화살표(← →) 장식을 쓰지 않는다.** 대시로 잇던 자리는 쉼표를 쓰거나 문장을 나눈다.
- **서술형 문장 끝에 콜론(:)을 붙이지 않는다.** 코드 블록 앞 문장도 마침표로 끝내고 블록을 바로 잇는다.
- 기존 문서는 수정하는 문단부터 자연스럽게 정리한다. 일괄 치환은 하지 않는다.

## Blazor 컴포넌트

- **파일명**: PascalCase (예: `KpiCard.razor`). 마크업 → `@code` 블록 순서.
- **매개변수**: `[Parameter]` public 속성, 이벤트는 `EventCallback<T>`.
- **스타일**: Tailwind 유틸리티 클래스. 다크 모드(`dark:`)와 반응형을 컴포넌트 단위로 내재화.
- **Razor 주석**: `@* 섹션 이름 *@` (장식 문자 금지).
- **함정**: `@{ var x = ...; }` 인라인 패턴은 컴파일 에러 → `@code` 블록 사용.
  한글 접미사는 `(@mData.Year)년`처럼 괄호 필수.

## 데이터베이스

- **쿼리 호출은 프로시저가 기본.** 로직에서 DB 조회는 저장 프로시저를 통해 한다.
  raw 쿼리 구문은 **테스트/일회성 확인 수준만** 허용 (`Queries/` + QueryBase).
- **enum은 정수(0,1)가 아니라 문자열로 저장한다** (2026-07-13 확정 규칙).
  컬럼은 `VARCHAR(20)` + enum 멤버 이름 그대로(`'General'`, `'Pending'`), 주석에 허용 값 명시.
  생성 엔티티는 string 프로퍼티, 읽는 쪽(Application/Client)에서 `Enum.TryParse`로 컨버팅하고
  쓸 때는 `ToString()`. **enum 멤버 이름 = DB 저장 문자열**이므로 개명은 데이터 마이그레이션과 함께.
- **DB 문자열 인코딩은 UTF-8로 강제, 다른 인코딩 금지** (2026-07-13 확정 규칙).
  DB 생성 시 `COLLATE Latin1_General_100_CI_AS_SC_UTF8` (글로벌 목표 — 한국은 로케일 중 하나).
  컬럼·파라미터는 `VARCHAR`만 사용 — `NVARCHAR`·`N''` 리터럴 금지. `VARCHAR` 크기는
  바이트라 한글 컬럼은 글자수×3 (예: `VARCHAR(300) -- UTF-8 (한글 100자)`).
  상세는 `Source/Database/README.md`.
- **엔티티·프로시저 호출 객체는 손으로 쓰지 않고 제너레이터로 생성한다.**
  1. SQL 작성: 테이블은 `Source/Database/{Account,Soccer}/Tables/`, 프로시저는 `.../Procedures/`.
     프로시저 결과 전용 슬림 엔티티는 마커로 지정 — `-- @entity: XxxRow` / `-- @source: join` /
     `-- @join: 테이블 AS 별칭 (컬럼들)`.
  2. 생성: `cd Source/Tools/Generator.Database && dotnet run`
     → `PlayGround.Persistence/Database/Generated/{DB}.{Entities,Procedures}`에 생성 (수동 편집 금지).
  3. 로직: Repository(`RepositoryBase`)에서 **생성된 프로시저 호출 객체 + 엔티티**를 사용.
     예) `var p = new UspGetLandingContents(this); var qr = await p.QueryAsync<LandingContentRecord>();`
  - **네이밍 규칙**: 테이블 전체 매핑 = `{테이블}Entity`(자동), 프로시저/쿼리 결과 투영 = `{이름}Record`
    (마커 `-- @entity:`에 `~Record`로 지정). 파일명만으로 "테이블 엔티티 vs 조회 결과"가 구분된다.
    (`~Result`는 `Result<T>` 모나드와 충돌하므로 쓰지 않는다.)
  - **생성 코드는 커밋한다** (수동 실행 단계라 clone 즉시 빌드되도록). `Generated/`는 ignore 안 함.
  4. DB 배포: 프로시저 SQL을 대상 DB에 적용 (LocalDB는 `Source/Database/README.md` 참조).
- EF Core = 마이그레이션/일부 CRUD, Dapper = 프로시저 호출(생성 객체 경유)·성능 중요 조회.

---

# Claude 작업 규칙

- **요청한 기능만 정확히 구현** — 예상 기능 선반영, 복잡한 추가 코드 생성 금지.
  추가 기능이 필요해 보이면 코드 작성 없이 제안만.
- **레이어 참조 방향 위반 금지** — 새 파일 생성 전 위 "프로젝트별 역할과 규칙"에서 위치 확인.
- **기존 코드 패턴 따르기** — 같은 일을 하는 코드가 이미 있으면 그 패턴 재사용.
- **기존 PlayGround 코드는 참고만** — 파일을 통째로 복사해 오지 않는다.
- **패키지 버전은 CPM으로만** — csproj에 Version 속성 직접 기입 금지.

## 디자인 핸드오프
- UI 작업 전 Handoff/Design.Landing.Phase0/의 README.md, SPEC.LANDING.md,
  CLAUDE.APPEND.md를 반드시 읽고 그 규칙(토큰·카피·섹션 순서 고정)을 따른다.
- 디자인 레퍼런스 HTML(*.dc.html)은 브라우저로 열어 시각 비교한다.