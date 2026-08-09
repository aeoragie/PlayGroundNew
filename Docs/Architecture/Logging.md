# 로깅 규칙

> 대상: 전 레이어. 헬퍼는 `Core.Shared/Logging`(MEL `ILogger` 확장), 출력은 NLog가 받는다.
> 2026-08-08 전면 정리. 그전까지 유즈케이스 50개 전부 로그가 없었고 Persistence에 215건이 쏠려 있었다.

## 1. 어디서 로깅하나

**맥락을 아는 곳에서 로깅한다.** "누가·무엇을·왜 실패했는가"를 아는 층은 유즈케이스다.
Repository는 `managerUserId`는 알아도 그게 권한 판정을 통과한 값인지, 어떤 요청의 일부인지 모른다.

| 계층 | 책임 |
|---|---|
| Core.Shared | 로그 없음. `Result`가 곧 반환값이다 |
| Core.Infrastructure | **Trace/Debug 진단만**(SQL 실행시간·커넥션·재시도) + 기동 Info. 오류는 `Result`로 반환하고 **Error 로그 금지** |
| **Application (유즈케이스)** | **비즈니스 로그의 주 책임 계층.** 아래 모든 규칙의 기본 적용 대상 |
| Persistence | 로그 없음. 진단이 필요하면 Core.Infrastructure의 base가 Debug로 낸다 |
| Server (Controller) | 최소화. 유즈케이스가 이미 남겼다. 인증·미들웨어 등 컨트롤러만 아는 사건만 |
| Client | 사용자 화면 오류만. 서버로 보내지 않는다 |

## 2. 무엇을 Info로 남기나

**상태를 바꾸는 일은 예외 없이 Info.** 로그인, 가입, 생성·수정·삭제, 승인·반려, 권한 변경,
알림 발송, 데이터 내보내기.

**서비스 초기화도 Info.** 기동 시 각 서비스·어댑터가 로드·초기화 완료를 한 줄씩 남기고,
**어떤 구성으로 떴는지**(Provider·Endpoint·Bucket 등)를 함께 남긴다. 자격 증명은 제외한다.
장애 대응은 "이 프로세스가 무슨 설정으로 떠 있나"에서 시작한다.

**성공한 조회는 아무것도 남기지 않는다.** 유즈케이스 경계의 `LogWith`는 성공이면 Debug라
평소에 꺼져 있다. 요청마다 한 줄씩 쌓이면 정작 봐야 할 실패가 묻힌다.

**상태를 바꾼 일만 Info로 남긴다.** 그것도 경계의 한 줄에 맡기지 않고 **식별자를 담은
`Info`를 유즈케이스 안에서 직접** 남긴다 — 누가 어떤 팀을 만들었는지는
`Operation completed`로는 알 수 없다.

## 3. 레벨

| 레벨 | 기준 | 예 |
|---|---|---|
| **Info** | 비즈니스 이벤트 · 서비스 초기화 | 팀 생성 완료, 로그인 성공, S3 어댑터 초기화 |
| Debug | 개발 진단 | SQL 실행시간, 캐시 히트, 일반 조회 |
| Trace | 상세 덤프 (평소 꺼둠) | 파라미터 전체 |
| Warn | **자동 복구된 이상** | 재시도 후 성공, 폴백 사용 |
| **Error** | **요청 실패는 반드시 남긴다** | 유즈케이스 실패, 예외 → `Result` 변환 지점 |
| Fatal | 프로세스 지속 불가 | 기동 실패, 설정 누락 |

`NotFound`처럼 **정상적인 빈 결과는 Warn이 아니다.** 사용자가 없는 팀을 조회한 것은 이상이 아니다.

`result.LogWith(Logger, "작업명")`을 쓰면 **코드 종류가 곧 레벨이 된다.**

| 코드 | 레벨 |
|---|---|
| `ErrorCode` | Error (`IsCritical`이면 Fatal) |
| `WarningCode` | Warn |
| `InformationCode` · `SuccessCode` | Info |

레벨을 따로 정하는 표는 두지 않는다. 예전에는 `GetLogLevel()`이 같은 `ErrorCode`를 다시 갈라
입력 오류를 Info로 낮췄는데, 분류가 두 벌이 되는 데다 문자열로 돌려주고 다시 enum으로 바꾸고 있었다.
**입력 오류도 실패는 실패다** — Error로 남는다.

## 4. 반복해서 틀렸던 것들

### 한 요청 = 한 줄

`"Team roster requested"` + `"Team roster received"`처럼 **짝으로 남기지 않는다.** 로그가 2배가 되고
정작 둘 사이에 실패하면 어느 쪽도 원인을 말해주지 않는다. **결과 시점에 한 줄**이 원칙이다.

예외는 오래 걸리는 작업(export·일괄 처리)뿐이다. 시작 로그가 있어야 "멈춰 있는지"를 안다.

### 한 실패 = 한 줄

아래층이 로깅하고 위층이 또 로깅하면 같은 실패가 3줄이 된다. **`Result`를 반환하는 층은 로깅하지
않고**, 맥락을 아는 유즈케이스가 `LogWith`로 한 번 남긴다.

### 예외는 예외 객체째로

```csharp
catch (Exception ex)
{
    mLogger.Error(ex, "Failed to create team", ("ManagerUserId", managerUserId));
    return Result<TeamResponse>.Error(ErrorCode.UnhandledException);
}
```

`ex`를 빼고 메시지만 남기면 스택이 사라져 로그가 있어도 원인을 못 찾는다.

### 식별자는 구조화 필드로

```csharp
Logger.Info("Team created", ("TeamId", teamId));    // O — 검색·집계 가능
Logger.Info($"Team {teamId} created");                  // X — 문자열에 묻힌다
```

헬퍼가 사람이 읽는 문장(`Team created. { TeamId:… }`)과 구조화 속성을 **동시에** 만든다.

### 개인정보는 남기지 않는다

패스워드·토큰·API 키는 물론이고 **이메일·전화번호·생년월일·주소도 남기지 않는다.**
식별이 필요하면 `UserId`·`PlayerId`를 쓴다. 로그는 평문으로 오래 남고 백업까지 따라간다.

## 5. 쓰는 법

**유즈케이스의 public 메서드는 얇은 래퍼다.** 실제 로직은 `~CoreAsync`에 있고, 래퍼가 결과를
`LogWith`로 한 번 남긴다. 이렇게 두면 **어느 경로로 실패해도 반드시 한 줄이 남는다** —
반환 지점마다 로깅을 챙기는 규율에 기대지 않는다(반환 지점은 246곳이다).

```csharp
public async Task<Result<TeamResponse>> ExecuteAsync(Guid managerUserId, CreateTeamRequest request, CancellationToken cancellation = default) =>
    (await ExecuteCoreAsync(managerUserId, request, cancellation)).LogWith(mLogger, "Execute");

private async Task<Result<TeamResponse>> ExecuteCoreAsync(Guid managerUserId, CreateTeamRequest request, CancellationToken cancellation = default)
{
    ...
    mLogger.Info("Team created", ("ManagerUserId", managerUserId), ("TeamId", saved.Value));
    return Result<TeamResponse>.Success(response);
}
```

작업 이름은 메서드 이름에서 `Async`를 뗀 것이다. 어느 유즈케이스인지는 로거 이름
(`ILogger<SoccerTeamCommand>`)이 이미 들고 있으므로 `"Execute"`로 충분하다.

메시지는 **영어**다. 완료는 과거형(`Team created`), 실패는 `Failed to …`.

`Core.Shared`가 `Microsoft.Extensions.Logging.Abstractions`만 참조하므로 유즈케이스 층은
NLog·DB 드라이버를 끌고 오지 않는다. 실제 출력은 Server가 구성한 NLog가 받는다.

## 6. 가드

`LoggingGuardTests`가 아래를 자동으로 막는다.

- Persistence·Domain·Contracts에 로그 호출이 없을 것
- 유즈케이스(`*Command.cs`)가 로거를 보유할 것
- `catch` 블록이 예외를 삼키지 않을 것 (같은 블록에 Error 로깅이 있을 것)
- 로그 메시지에 한글이 없을 것 (메시지는 영어)
