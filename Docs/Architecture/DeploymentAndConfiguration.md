# 배포 & 설정(appsettings) 전체 플로우

> 기록일: 2026-07-12
> 맥락: 환경 구분(Development/Staging/Production) · appsettings 계층 · 시크릿 주입 · CI/CD 파이프라인을
> 세팅한 뒤, "옵션 설정 파일이 배포되어 나가는 전체 플로우"를 정리한 문서.

---

## 1. 세 개의 독립 축 (헷갈리면 안 되는 개념)

배포를 이해하려면 서로 **직교(orthogonal)** 하는 세 가지를 분리해야 한다.

| 축 | 값 | 시점 | 무엇을 결정 |
|---|---|---|---|
| **빌드 구성(Build config)** | `Debug` / `Release` | 컴파일 타임 | 최적화, `Debug.Assert` 활성 여부. **배포는 항상 Release** |
| **환경(Environment)** | `Development` / `Staging` / `Production` | 런타임 | `ASPNETCORE_ENVIRONMENT`가 지정. 어떤 `appsettings.{Env}.json`을 로드할지 |
| **배포 대상(Deploy target)** | 로컬 / Staging 호스트 / Production 호스트 | 배포 시 | 산출물이 실제로 도는 곳 |

**핵심 원칙 — "build once, run anywhere" (12-factor)**
> 산출물(artifact)은 **환경 무관하게 딱 하나(Release)** 만 만든다.
> Staging이든 Production이든 **같은 산출물**이 나가고, 차이는 **런타임에** 호스트가 주입하는
> `ASPNETCORE_ENVIRONMENT` + 시크릿(환경변수)으로만 갈린다.
> → 환경별로 다시 빌드하지 않는다 (재빌드 = "테스트한 것과 다른 바이너리가 나감" 위험).

---

## 2. 설정 파일 레이아웃

### Server (`PlayGround.Server/`)

| 파일 | 커밋? | 용도 |
|---|---|---|
| `appsettings.json` | ✅ 커밋 | 공통 비-시크릿 (로깅 기본, OAuth 엔드포인트 URL, 빈 커넥션 문자열 골격) |
| `appsettings.Development.json` | ✅ 커밋 | 개발 전용 비-시크릿 (LogLevel Debug, **LocalDB 커넥션**) |
| `appsettings.Staging.json` | ✅ 커밋 | 스테이징 비-시크릿 (LogLevel Info, 콘솔 로그 on, 아카이브 30) |
| `appsettings.Production.json` | ✅ 커밋 | 운영 비-시크릿 (LogLevel Info, 콘솔 로그 off, 아카이브 90) |
| `appsettings.Local.json.example` | ✅ 커밋 | 시크릿 채우는 **템플릿** (실제 값 없음) |
| `appsettings.Local.json` | ❌ **gitignore** | **로컬 개발 시크릿** (Jwt:Key, OAuth ClientId/Secret, 실 커넥션). 절대 커밋 금지 |

### Client (`PlayGround.Client/wwwroot/`)

| 파일 | 커밋? | 용도 |
|---|---|---|
| `appsettings.json` | ✅ 커밋 | 공개 가능 공통값 (피처 플래그 등) |
| `appsettings.{Development,Staging,Production}.json` | ✅ 커밋 | 환경별 공개값 (예: `Features:AgentEnabled` — Dev/Staging true, Prod false) |

> ⚠️ **Client 파일은 WASM으로 브라우저에 공개 서빙된다 = 누구나 다운로드 가능.**
> 여기엔 **시크릿을 절대 두지 않는다.** 시크릿이 필요한 처리는 인증된 Server API 뒤에 둔다.
> Client엔 `Local.json`도 없다 (WASM은 로드하지 않고, 애초에 넣을 시크릿이 없어야 함).

### gitignore (실제)

```
appsettings.Local.json
appsettings.*.Local.json
.env
```

---

## 3. 설정 병합(merge) 순서 = 우선순위

### Server — 뒤 소스가 앞을 덮어씀 (last-wins)

`WebApplication.CreateBuilder`의 기본 체인 + `Program.cs`에서 추가한 `Local.json`:

```
① appsettings.json                     (공통, 커밋)
② appsettings.{Environment}.json        (환경별, 커밋)          ← ASPNETCORE_ENVIRONMENT로 선택
③ 환경변수 (Environment Variables)       (배포 호스트가 시크릿 주입)   '__' = 계층 구분자
④ 명령줄 인자
⑤ appsettings.Local.json                (로컬 시크릿, gitignore)  ← Program.cs가 마지막에 append
```

> `Program.cs`: `builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, ...)`
> → 마지막에 붙으므로 **로컬에선 Local.json이 최우선**으로 이긴다.
> 배포 산출물엔 Local.json이 **없으므로**(gitignore + CI 체크아웃에 없음) 충돌하지 않고,
> 배포에선 **③ 환경변수**가 시크릿을 공급한다.

**환경변수 계층 표기 (`__` → `:`)**

| 설정 경로 | 환경변수 이름 |
|---|---|
| `Jwt:Key` | `Jwt__Key` |
| `OAuth:Google:ClientSecret` | `OAuth__Google__ClientSecret` |
| `DatabaseConfiguration:Databases:Account:ConnectionString` | `DatabaseConfiguration__Databases__Account__ConnectionString` |

### Client (Blazor WASM) — 훨씬 단순

```
① wwwroot/appsettings.json
② wwwroot/appsettings.{Environment}.json   ← 환경은 서버가 전파 (아래)
```

- **환경변수·Local.json 없음** (브라우저엔 그런 개념이 없음).
- **환경 전파**: 호스팅 Server가 자기 `ASPNETCORE_ENVIRONMENT`를 `Blazor-Environment` 응답 헤더로 내려주고,
  WASM 런타임이 그 값으로 `appsettings.{Environment}.json`을 자동 로드한다. **즉 Client 환경 = Server 환경.**
  (별도 코드 불필요 — 프레임워크 자동 처리.)

---

## 4. 시크릿 취급 규칙 (필수)

1. **시크릿은 어떤 커밋 파일에도 넣지 않는다.** (Jwt:Key, OAuth ClientId/Secret, 실 DB 커넥션 등)
2. **로컬**: `appsettings.Local.json` (gitignore). `appsettings.Local.json.example`를 복사해 채운다.
3. **배포(Staging/Prod)**: **호스트 환경변수**로 주입. GitHub Actions에선 **Environment Secrets**에 저장하고
   deploy 스텝의 `env:`로 매핑 (예: `Jwt__Key: ${{ secrets.PROD_JWT_KEY }}`).
4. **비-시크릿은 커밋 OK** — OAuth 인증 엔드포인트 URL, 로그 레벨, 피처 플래그, LocalDB 커넥션(개발용).
5. **Client(WASM)엔 시크릿 자체가 없어야 한다** — 공개 서빙되므로.
6. 시크릿을 로그/전사에 출력 금지. 마이그레이션은 파일→파일로만.

---

## 5. CI/CD 파이프라인 (GitHub Actions)

### `ci.yml` — 모든 PR·main 푸시의 게이트

```
checkout → setup .NET 10 / Node 20
  → dotnet restore
  → npm ci + npm run css:build     (Tailwind 산출물 재생성 — 설정 오류도 여기서 잡힘)
  → dotnet build -c Release
  → dotnet test -c Release          (외부 DB 불필요한 CI-safe 테스트)
```
- `windows-latest` 러너 — LocalDB·SSDT(.sqlproj) 빌드가 dev와 동일하게 동작하도록.

### `deploy.yml` — main 푸시 시 배포 (수동 실행도 지원)

```
git push main
   │
   ▼
[ publish ]  (환경 무관 단일 산출물 = build once)
   css:build → dotnet publish Server -c Release -o publish/   ← 호스팅되는 WASM Client 포함
   → artifact 'playground-app' 업로드
   │
   ▼
[ deploy-staging ]   environment: Staging   (main 푸시 시 자동)
   artifact 다운로드 → Staging 호스트에 배포
   호스트가 주입: ASPNETCORE_ENVIRONMENT=Staging  +  시크릿(env vars)
   │  (성공 시)
   ▼
[ deploy-production ]   environment: Production   (승인 게이트 권장)
   GitHub → Settings → Environments → 'Production'에 Required reviewers 설정 시
   수동 승인 대기 후 실행 (운영 안전장치)
   같은 artifact 다운로드 → Production 호스트에 배포
   호스트가 주입: ASPNETCORE_ENVIRONMENT=Production  +  시크릿(env vars)
```

> **Staging과 Production은 완전히 같은 artifact를 받는다.** 차이는 오직
> 런타임 `ASPNETCORE_ENVIRONMENT`(어떤 appsettings.{Env}.json을 읽을지)와 주입된 시크릿뿐.
> deploy 스텝은 현재 placeholder(echo) — 실제 배포(SSH/rsync, Azure Web App, 컨테이너 등)로 교체 예정.

---

## 6. 값 하나가 실제로 운영까지 도달하는 경로 (구체 예시)

### 예 1) `Jwt:Key` (시크릿)
- 커밋 파일엔 **없음**.
- 로컬: `appsettings.Local.json` → `Jwt:Key`.
- 운영: 호스트 환경변수 `Jwt__Key` (GitHub Environment secret `PROD_JWT_KEY`를 deploy 스텝 `env:`로 주입).
- 코드: `builder.Configuration["Jwt:Key"]`가 공급된 provider에서 자동 해석.

### 예 2) Account DB 커넥션 (시크릿)
- 커밋 `appsettings.json`: 빈 문자열(골격만).
- 개발: `appsettings.Development.json` → LocalDB 커넥션(비-시크릿이라 커밋).
- 운영: 환경변수 `DatabaseConfiguration__Databases__Account__ConnectionString`.

### 예 3) `Features:AgentEnabled` (Client, 공개값)
- `wwwroot/appsettings.Production.json` → `false` (커밋, 공개 서빙돼도 무방).
- Client 환경이 Production이면(=Server가 Production) 이 값이 로드되어 에이전트 UI가 숨겨짐.

---

## 7. 규칙 체크리스트

- [ ] 새 시크릿? → `appsettings.Local.json`(로컬) + `.example`에 키만 추가 + 배포 환경변수 등록. **커밋 파일 금지.**
- [ ] 새 비-시크릿 환경별 값? → `appsettings.{Env}.json`(Server) 또는 `wwwroot/appsettings.{Env}.json`(Client).
- [ ] Client에 넣으려는 값이 시크릿인가? → **그렇다면 Client 금지.** Server API 뒤로.
- [ ] 배포는 항상 `-c Release`. 환경별 재빌드 금지 (같은 artifact + 런타임 환경변수).
- [ ] 환경변수 계층은 `__` 구분자.
- [ ] Production 배포엔 Required reviewers(승인 게이트) 유지.

---

## 관련 파일

- 워크플로우: `.github/workflows/ci.yml`, `.github/workflows/deploy.yml`
- Server 설정: `Source/PlayGround/PlayGround.Server/appsettings*.json`, `Program.cs`(Local.json 로드)
- Client 설정: `Source/PlayGround/PlayGround.Client/wwwroot/appsettings*.json`
- 시크릿 템플릿: `Source/PlayGround/PlayGround.Server/appsettings.Local.json.example`
