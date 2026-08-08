# 테스트 안전망 — 구조와 작업 규칙

> 대상: `Tests.Unit` / `Tests.Integration` / `Tests.Infrastructure` (xunit.v3 · Moq · FluentAssertions).
> 2026-08-02 durable 스위트로 전환. 그전까지 검증은 기능별 폐기용 스크립트(`shot-*`/`api-*`,
> git 히스토리 `f19ad41^`에 보존)로 1회성이었고 회귀 안전망이 없었다.

## 1. 원칙

1. **DB 없이 전부 돈다.** clone 즉시 `dotnet test`가 통과해야 한다 — 목킹으로 대체하고,
   실제 DB·브라우저가 필요한 검증은 이 스위트에 넣지 않는다.
2. **문구가 아니라 규칙을 고정한다.** SPEC 카피가 바뀌면 리소스만 고치면 되도록, 표시 로직
   테스트는 구조·판정을 검증하고 카피 자체는 리소스 정합성 테스트가 본다.
3. **"왜 이 규칙인가"를 테스트가 말한다.** 테스트 이름과 주석이 설계 결정을 가리킨다
   (예: 심사는 주최측의 몫 · 승인형 알림은 항상 켜짐 · 친선은 집계에서 제외).
4. **보안·프라이버시 판정은 반드시 덮는다.** 사유를 구분하지 않는 실패(NotFound/Forbidden),
   저장 화이트리스트, 호스트 허용 목록이 여기 해당한다.
5. **식별자는 ASCII, 설명은 한글.** 메서드·클래스 이름은 `Subject_Behavior`
   (예: `Winner_UsesPenalties_WhenRegulationTied`), 맥락은 주석과 실패 메시지에 남긴다 — 아래 §5-6.

## 2. 프로젝트별 역할

| 프로젝트 | 대상 | 외부 의존 |
|---|---|---|
| `Tests.Unit` | Domain 규칙 · Application 유즈케이스(Moq) · Client 순수 로직 | 없음 |
| `Tests.Integration` | Server 서비스(JWT 등) | 없음 |
| `Tests.Infrastructure` | Core.Infrastructure(설정 바인딩·액터·로깅) + **DB 계약** | 로컬 DB(없으면 Skip) |

`Tests.Unit`은 **PlayGround.Client를 참조한다** — 표시 파생·조사 해석 같은 순수 로직이 거기 있기
때문이다. UI 렌더링(bUnit 등)은 참조하지 않는다.

## 3. Tests.Unit 구성

```
Tests/Tests.Unit/
├── Core/          Result 모나드 · Shared 원시 타입
├── Domain/        순수 규칙 — 파싱 가드 · 기본값 · 링크 해석 · 배치 규칙
├── Application/   유즈케이스 가드 — 인가 · 입력 검증 · 정규화 · 저장소 결과 해석
└── Client/        표시 파생 · 조사 해석 · i18n 리소스 정합성
```

### Domain

| 파일 | 무엇을 지키나 |
|---|---|
| `YouTubeVideoLinkTests` | 호스트 화이트리스트·11자 ID·경로 조작 차단. 클라이언트 미리보기와 서버 저장이 **같은 규칙**을 써야 한다 |
| `SoccerEnumRulesTests` | 숫자 문자열 enum 파싱 차단 · 파싱 실패 시 기본값 · 공개/알림 기본값이 SPEC과 일치 · 승인형은 알림 설정 enum에 없음 |
| `DisplayStringPlacementTests` | **Domain·Contracts에 표시 문자열 없음** (아래 §5) |
| `SqlProjectCoverageTests` | **모든 `.sql`이 sqlproj 검증 대상에 포함**됨 · 개별 파일 나열 금지 · Build/None 구분 (아래 §5) |
| `TimeBaselineGuardTests` | **시각 기준이 UTC 하나**임 — C# `DateTime` 타입 금지 · SQL 내장 시각 함수 금지 (아래 §5-4) |
| `TestNamingGuardTests` | **테스트 이름이 ASCII**임 — 도구가 깨지지 않게 (아래 §5-6) |
| `LoggingGuardTests` | **로그가 규칙대로**임 — 계층·영어 메시지·개인정보·보간 (아래 §5-7) |

### Application

| 파일 | 무엇을 지키나 |
|---|---|
| `SoccerRecordCorrectionCommandTests` | 남의 경기·친선·중복을 **구분하지 않고** Forbidden · 취소는 접수 상태만 · 값 정규화/절단 · **승인/반려 메서드가 생기지 않음**(설계 결정 7) |
| `SoccerPlayerStrengthTagsCommandTests` | 5개·12자 상한 · 중복/해시 정규화 · 연락처·링크 차단 · 빈 목록은 NULL |
| `SoccerTeamMatchResultCommandTests` | 점수 0~99 · 미래 경기 거부(하루 여유) · 보호자 알림이 수신 설정을 존중 · **알림 실패가 저장을 되돌리지 않음** |
| `SoccerClaimFlowCommandTests` | 코드 정규화(대문자·4~12자) · 관계 화이트리스트 · 무효 코드/연결 불가를 사유 구분 없이 처리 |
| `SettingsGuardCommandTests` | 알림 설정·프로필 공개 항목의 **저장 화이트리스트** — 승인형은 어떤 이름으로도 저장 불가 |
| `LoginBySocialCommandTests` · `SoccerLandingContentsCommandTests` | 기존 |

### Client

| 파일 | 무엇을 지키나 |
|---|---|
| `LocalizationResourceTests` | **생성물 최신성**(아래 §4) · ko↔ja 키/플레이스홀더 일치 · ja에 조사 모디파이어 없음 |
| `KoreanParticleTests` | 받침 판정(한글·숫자·영문) · `으로/로`의 ㄹ 예외 · 인자 부족 시 원문 유지 |
| `NotificationPresenterTests` | 액션 판정(처리 필요 카운트) · 딥링크 · 에이전트 유형 식별(flag OFF 숨김) · 상대 시각 구간 |
| `RecordsFormattingTests` | PK 표기 · 승자 판정(정규시간 우선) · 라운드/스테이지 라벨 · 미종료 처리 |
| `SoccerMatchSegmentTests` | 공식/친선 필터가 **전체를 빠짐없이 배타적으로** 나눔 · URL 왕복 |

## 4. i18n 생성물 최신성 — 이 스위트의 핵심

생성기(`Generator.Localization`)는 **수동 실행**이라, JSON만 고치고 재생성을 잊으면
화면에 키 문자열이 그대로 뜬다. **빌드는 통과하므로 사람이 볼 때까지 모른다.**

`LocalizationResourceTests`가 디스크의 `wwwroot/i18n`을 실제 `JsonLocalizer`로 읽어
`AppText`에 물린 뒤, 생성된 접근자를 **전부 리플렉션으로 호출**해 확인한다.

- 키가 그대로 반환되면 → 생성물이 리소스와 어긋남
- `{0}`이 남아 있으면 → 인자 개수 불일치·`FormatException` 폴백
- `{0:이/가}`가 남아 있으면 → 조사 해석 실패

`LocalizationFixture`(컬렉션 픽스처)가 로드를 담당한다. `AppText.Loc`은 **정적 상태**라
같은 컬렉션으로 묶어 병렬 실행을 직렬화한다. 카피를 검증하는 Client 테스트는
`[Collection(LocalizationCollection.Name)]`을 붙인다 — 안 붙이면 `AppText`가
`NullLocalizer`라 키 문자열이 나와 테스트가 아무것도 검증하지 못한다.

Client의 `internal` 주입점(`AppText.Loc`·`AppText.Domains`)은 csproj의
`<InternalsVisibleTo Include="PlayGround.Tests.Unit" />`로 열어 두었다.

## 5. 표시 문자열 배치 가드

`DisplayStringPlacementTests`가 `PlayGround.Domain`·`PlayGround.Contracts`의 `.cs`를 훑어
**문자열 리터럴 안의 한글**을 찾는다(주석은 제외). 발견되면 실패한다.

Domain은 Client를 참조할 수 없어 리소스에 닿지 못한다 — 여기에 라벨이 생기면 그 문구만
영원히 번역되지 않는다. 실제로 `SoccerCareerOutcomeType.ToTagLabel()`,
`SoccerCorrectionField.ToLabel()`이 i18n 이관에서 누락된 채 남아 있었고, Client가 직접
호출하고 있었다. 표시 라벨은 `Client/Models/SoccerDomainEnumLabels.cs`로 옮겨
`AppText.Enums`를 거치게 했다.

> **Application은 이 가드의 대상이 아니다.** 이메일·알림 본문 등 **서버 발신 문자열**이
> 남아 있고, 이는 Client 전용인 현재 i18n 구조 밖이다(Localization.md §10 미착수 항목).

## 5-2. sqlproj 검증 범위 가드

`SqlProjectCoverageTests`가 `Database.{Soccer,Account}.sqlproj`를 본다.

- `.sql`이 든 폴더가 전부 `<Build>` 또는 `<None>` **글롭**에 덮이는지
- **개별 파일 나열이 없는지** (손으로 관리하면 반드시 드리프트한다)
- 스키마 폴더는 `<Build>`, 데이터·이력 폴더(`Seeds`/`Queries`/`Migrations`)는 `<None>`인지

항목을 손으로 나열하던 시절 Soccer에서 **56개**(테이블 13 · 프로시저 37 · 인덱스 1 · 마이그레이션 5)가
빠져 있었고, 빠진 테이블을 참조하는 프로시저가 전부 SQL71502로 새고 있었다(경고 246건).
글롭으로 바꾼 뒤 **0 오류 / 0 경고**. 글롭이라 파일 추가는 자동으로 잡히지만 **새 폴더**는
글롭을 넣어야 하므로, 그 누락을 이 테스트가 막는다.

> sqlproj는 SSDT가 필요해 `dotnet build`로는 검증되지 않는다(솔루션 빌드 시 MSB4278로 건너뛴다).
> 실제 컴파일은 Visual Studio나 아래로 확인한다.
>
> ```bash
> "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" \
>   Source/Database/Soccer/Database.Soccer.sqlproj -t:Rebuild -p:VisualStudioVersion=18.0
> ```

## 5-3. DB 계약 테스트 (Tests.Infrastructure/Database)

SQL과 C#이 어긋나는 건 **빌드로 못 잡고 런타임에서야 터진다.** 실제로 겪은 것: SQL만 고치고
제너레이터 미실행 · DB에 프로시저 미배포 · 프로시저가 sqlproj 빌드에서 빠져 있어 미검증.

| 테스트 | 무엇을 지키나 | 범위 |
|---|---|---|
| `ProcedureContractTests` | 생성 객체의 프로시저가 **DB에 배포돼 있는지** + **파라미터 이름 일치** | 리플렉션으로 **전량 자동** — 새 프로시저도 자동 포함 |
| `ResultSetContractTests` | 다중 결과셋의 **개수·순서**와 각 결과셋 컬럼을 매핑 타입이 받는지 | 계약 표에 명시(현재 MatchDetail 5 · TournamentDetail 8) |

- **결과가 없는 식별자로 실행해 스키마만 본다** — 시드 데이터가 필요 없다
  (대상 프로시저는 조기 반환이 없어 빈 결과에서도 모든 SELECT를 낸다)
- 컬럼 검사 방향은 **"프로시저가 내는 컬럼을 타입이 받을 수 있는가"** 한쪽만 —
  반대는 부분 매핑(슬림 조회)이 정상이라 검사하면 오탐이 난다
- `@ReturnValue`는 Dapper가 프로시저 RETURN 값을 받는 자리라 파라미터 비교에서 제외한다

### DB가 없는 환경

`LocalDatabaseFixture`가 연결을 시도하고, 실패하면 **Skip**한다(실패가 아니다).
CI·새 clone에서 빨개지면 진짜 실패와 구분이 안 된다. 커넥션 문자열은
환경변수 `PLAYGROUND_TEST_SOCCER_CONNSTR`·`PLAYGROUND_TEST_ACCOUNT_CONNSTR` → 개발 기본값(`.\SQLEXPRESS`).

```bash
# DB 있을 때: 251 통과 / DB 없을 때: 18 통과 + 233 Skip (실패 0)
dotnet test Tests/Tests.Infrastructure/Tests.Infrastructure.csproj
```

> **첫 실행에서 실제 드리프트 4건을 잡았다** — 로컬 Account DB에 `NotificationPreferences`
> 테이블과 프로시저 4종(`UspDeleteUser`·알림 설정 3종)이 배포돼 있지 않았다.
> 알림 설정 화면과 계정 탈퇴가 런타임에 터지는 상태였다.

## 5-4. 시각 기준 가드

`TimeBaselineGuardTests`가 `Source/` 전체(`.cs`·`.razor`·`.sql`)를 훑는다.

| 잡는 것 | 왜 |
|---|---|
| C# `DateTime` **타입 자체** | 순간은 `SystemTime`, 달력 날짜는 `DateOnly`. 예외는 경계 파일뿐 |
| `DateTime.Now`·`UtcNow`·`Today`, `DateTimeOffset.Now`·`UtcNow` | 시계 읽기는 `SystemTime.Now`(UTC) 하나 |
| `ToLocalTime()`·`TimeZoneInfo.Local` | 시간대를 아는 곳은 Client의 `DisplayTime` 하나 |
| SQL 내장 시각 함수 (**`GETUTCDATE()` 포함**) | 프로시저는 `dbo.UfnSystemDate()`만 부른다 |
| `dbo.UfnSystemDate()`를 `WHERE`에 직접 | `DECLARE @Now …`로 받는다 (아래) |

**이 가드가 없으면 새 코드가 반드시 다시 샌다.** 개발 PC(KST)에서는 `DateTime.Now`가 멀쩡히
돌기 때문에 리뷰로도 일반 테스트로도 안 잡히고, **UTC 서버에서만 9시간 어긋난다.**
실제로 그 상태로 7곳이 쌓여 마감된 모집이 9시간 더 살아 있었다.

주석과 문자열 리터럴은 걷어낸 뒤 검사한다(설명문의 `DateTime.Now`까지 막으면 못 쓴다).
시각의 원천(`UfnSystemDate.sql`·테이블 DEFAULT·마이그레이션·시드·`Debug/`)은 예외다.

### `@Now`로 받아야 하는 이유

```sql
DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();   -- 프로시저당 1회
... WHERE [ExpiresAt] > @Now
```

**스칼라 UDF는 인라인되지 않는다.** SQL Server의 UDF 인라인(Froid)은 시간 의존 내장 함수를
호출하는 함수를 제외하므로, `WHERE`에 직접 쓰면 **행마다** 호출된다. 변수로 받으면 1회로 끝나고,
부수 이득으로 **한 프로시저 안의 "지금"이 일관**된다(예전에는 `GETUTCDATE()`를 여러 번 불러
같은 트랜잭션의 행마다 시각이 미세하게 달랐다).

`DisplayTimeTests`는 표시 쪽 경계를 따로 지킨다 — 날짜 경계(UTC 15시), 서머타임에
**존재하지 않는 시각**, 폼 왕복 무손실, `Kind` 고정.

## 5-5. 인가 누락 가드 (Tests.Integration/Api)

`EndpointAuthorizationTests`는 컨트롤러 액션을 리플렉션으로 훑어 **익명으로 열린 것이
명시적 허용 목록에 있는지** 본다. 없으면 실패한다.

대부분의 컨트롤러는 클래스에 `[Authorize]`를 걸고 공개 액션만 `[AllowAnonymous]`로 여는데,
**`AuthController`·`SoccerLandingController`·`SoccerExportController`는 클래스 수준 가드가 없다.**
여기에 액션을 추가하면서 `[Authorize]`를 빠뜨리면 아무 경고 없이 공개된다 — 그 사고를 막는다.

- 판정 순서는 MVC와 같다: 액션의 `[AllowAnonymous]` > 액션의 `[Authorize]` >
  클래스의 `[AllowAnonymous]` > 클래스의 `[Authorize]` > (없으면) 익명
- 허용 목록에 **죽은 항목이 남는 것도 실패**로 본다 — 지운 액션 이름이 남아 있으면
  나중에 같은 이름이 생겼을 때 조용히 통과한다
- 새로 공개할 때는 목록에 줄을 추가하게 만들어 **실수가 아니라 결정**이 되게 한다.
  넣기 전에 "비로그인 아무나 봐도 되는 데이터인가"를 확인한다.

> **한계: 특성만 본다.** 미들웨어·라우팅·토큰 검증까지 실제로 태우는 검증은
> `WebApplicationFactory`가 필요하고, 그건 `Microsoft.AspNetCore.Mvc.Testing` 패키지가 있어야 한다.
> 지금 개발 환경에서 nuget.org에 닿지 않아 도입하지 못했다(§8).

## 5-6. 테스트 이름 가드

`TestNamingGuardTests`가 세 테스트 프로젝트의 **메서드·타입 이름에 비ASCII가 없는지** 본다.

한글 이름(`승자판정_정규시간_동점이면_PK로_가린다`)은 읽기엔 좋았지만 도구를 계속 깨뜨렸다.

- **테스트 로그가 UTF-16이라** 실패한 테스트 이름을 보려면 매번 `iconv`를 거쳐야 했다
- `--filter-method`에 한글을 넘기면 셸·CI 설정마다 이스케이프가 달라진다
- CI 로그·TRX 리포트에서 깨져 어느 테스트가 실패했는지 못 읽는다

**표현력은 이름이 아니라 주석과 실패 메시지로 낸다** — 거기는 한글이 자유롭고,
실패했을 때 실제로 눈에 들어오는 곳이다. 이름은 `Subject_Behavior` 형태로 쓴다.

```csharp
[Fact]
public void Winner_UsesPenalties_WhenRegulationTied()
{
    // 정규시간이 동점일 때만 PK로 승자를 가린다 — 정규시간에서 갈리면 PK는 보지 않는다
```

주석과 문자열 리터럴은 걷어낸 뒤 검사하므로, 설명에 쓴 한글 이름은 위반이 아니다.

## 5-7. 로깅 가드

`LoggingGuardTests`가 `Source/` 전체를 훑는다. **로그는 빌드로도 일반 테스트로도 안 잡히고,
운영에서 필요할 때 비어 있는 걸로만 드러난다.**

| 잡는 것 | 왜 |
|---|---|
| Persistence·Domain·Contracts의 로그 호출 | 맥락을 아는 층에서 남긴다. 실패는 `Result`가 스택까지 실어 올린다 |
| 유즈케이스 public 메서드에 `LogWith` 누락 | 반환 지점이 246곳이다. 경계에서 한 번 잡지 않으면 반드시 샌다 |
| 로그 메시지의 한글 | 수집·검색 도구가 인코딩을 가리지 않게 |
| 로그 필드의 개인정보 (`Email`·`Phone`·`Token` 등) | 로그는 평문으로 오래 남고 백업까지 따라간다 |
| 로그 호출의 문자열 보간 | 식별자가 문자열에 묻히면 검색·집계가 안 된다 |

개인정보 가드는 **실제로 샌 것을 잡아 만들었다.** 래퍼에 행위자를 자동으로 싣는 작업에서
`LoginByEmailCommand`의 첫 파라미터가 `email`이라 그대로 로그에 들어갔다. 지금은 `UserId`를 쓴다.

## 6. 작업 규칙

### 새 유즈케이스를 만들 때

가드가 있으면 테스트도 함께 만든다. 최소 4종:

1. 인가 실패(빈 사용자 → Unauthorized)
2. 입력 검증 경계(상한/하한 **양쪽** — 99는 통과, 100은 거부)
3. 정규화 결과(공백 제거·절단·enum 이름화)를 **저장소에 전달된 값으로** 확인
4. 저장소가 `null`/`false`를 줄 때의 해석(Forbidden인지 NotFound인지)

### 새 리소스 키를 추가할 때

`LocalizationResourceTests`가 자동으로 덮는다 — 생성기만 돌리면 된다.
ja를 빼먹거나 플레이스홀더가 어긋나면 테스트가 잡는다.

### 테스트를 믿기 전에

**새로 만든 테스트는 일부러 깨뜨려 본다.** 통과만 확인하면 아무것도 검증하지 않는
테스트를 통과로 착각한다(`LocalizationResourceTests`도 리소스 키를 개명해 실패를 확인한 뒤
확정했다). 리소스·소스를 잠깐 고쳐 실패를 보고 되돌린다.

### 이름 규칙

- 클래스: `{대상}Tests`
- 메서드: `{메서드}_{조건}_{기대}` 한글 (예: `ExecuteAsync_빈_사용자는_Unauthorized다`)
  — 실패 목록이 곧 규칙 목록이 된다
- 섹션 구분은 `//.// 섹션명` (CLAUDE.md 컨벤션)

## 7. 실행

```bash
dotnet test PlayGround.slnx                      # 전체 (sqlproj 2건 오류는 SSDT 미설치 — 무관)
dotnet test Tests/Tests.Unit/Tests.Unit.csproj   # 단위만
```

특정 클래스만 돌릴 때는 테스트 exe를 직접 부른다 (실패 상세가 콘솔에 그대로 나온다 —
`dotnet test`의 로그 파일은 UTF-16이라 읽기 나쁘다).

```bash
cd Binary/Debug/Tests.Unit
./PlayGround.Tests.Unit.exe -class "*LocalizationResourceTests"
./PlayGround.Tests.Unit.exe -method "*조사*"
```

현재: **Tests.Unit 415 · Tests.Integration 126 · Tests.Infrastructure 251**
(DB 미연결 시 Infrastructure는 18 통과 + 233 Skip).

## 8. 미착수

- **실제 HTTP 왕복** — `WebApplicationFactory`로 미들웨어·라우팅·토큰 검증까지 태운다.
  덮을 것: 토큰 없음/서명 불일치/무효화 토큰 → 401 · 남의 자원 → 403 · `Envelope<T>` 형태.
  **`Microsoft.AspNetCore.Mvc.Testing` 패키지가 필요한데 지금 개발 환경에서 nuget.org에 닿지 않아
  도입하지 못했다**(§5-4의 한계). 네트워크가 되면 패키지만 추가해 얹으면 된다 —
  현재 인가 가드는 특성만 보므로 파이프라인 회귀는 못 잡는다.
- **결과셋 계약의 나머지 프로시저** — 다중 결과셋 23개 중 현재 2개(MatchDetail·TournamentDetail)만
  계약 표에 있다. 나머지는 필요할 때 표에 줄을 추가하면 된다.
- **E2E 저니** — `Handoff/TEST.INTEGRATIONSCENARIOS.md`(S1~S7)는 현재 수동 검수.
  자동화한다면 Playwright(헤드리스 Edge 연결은 환경 의존적 — CLAUDE.md 반복 함정 참조).
