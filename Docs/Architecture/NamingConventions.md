# 네이밍 규칙 — .NET 표준 + PlayGround 결정

> 기록일: 2026-07-12
> .NET 일반 관례와, PlayGround에서 별도로 정한 규칙 · 그렇게 정한 이유 · 장단점을 정리한다.
> 규칙 자체는 `CLAUDE.md`가 단일 진실 소스이고, 이 문서는 **배경·근거**를 담는다.

---

## 1. .NET 표준 관례 (Microsoft Framework Design Guidelines)

일반적으로 통용되는 기준. 우리도 아래는 그대로 따른다.

| 대상 | 관례 | 예 |
|---|---|---|
| 클래스·구조체·enum·메서드·속성·이벤트 | **PascalCase** | `PlayerRepository`, `GetByEmailAsync` |
| 인터페이스 | **`I` 접두 + PascalCase** | `IAccountRepository` |
| 상수·`static readonly`·`const` | **PascalCase** | `MinPasswordLength`, `Logger` |
| 지역 변수·매개변수 | **camelCase** | `userId`, `passwordHash` |
| 비동기 메서드 | **`Async` 접미사** | `ExecuteAsync` |
| 제네릭 타입 매개변수 | `T` 또는 `T` 접두 | `TResponse`, `TRow` |
| private 필드 | *가이드라인은 강제 안 함* — camelCase 또는 `_` 접두가 흔함 | (아래 우리 규칙 참조) |

> .NET은 **private 필드 접두사를 강제하지 않는다.** `_camelCase`(팀 다수), `camelCase`, `m_`/`m` 등 팀별로 갈린다.

---

## 2. PlayGround 결정 (표준과 다르게/추가로 정한 것)

| 대상 | .NET 기본/흔함 | **PlayGround 규칙** | 근거 요약 |
|---|---|---|---|
| private 인스턴스 필드 | `_camelCase` | **`m` 접두 + PascalCase** (`mRepository`, `mHttp`) — readonly여도 | 기존 코드베이스 계승, IDE1006과 정합(.editorconfig에 규칙화) |
| static/const 필드 | PascalCase | PascalCase (그대로) | 상수성 = 표준 유지 |
| 유즈케이스(Application) | `GetXQuery` / `CreateXCommand` (CQRS) | **`{기능}Command`** (읽기/쓰기 무관, 동사 없음) | "실행 가능한 비즈니스 동작" = GoF Command. 아래 §3.2 |
| 기술 서비스·어댑터 | `XService` | **`{역량}Service`** (`OAuthService`) | 유즈케이스가 아니라 *의존하는* 기술 수단. 아래 §3.3 |
| 종목 도메인 테이블/엔티티/유즈케이스 | 접두 없음 | **`Soccer` 프리픽스** (`SoccerPlayers`, `SoccerPlayerProfileCommand`) | 타 스포츠 대비 선반영. 아래 §3.4 |
| 프로시저 | — | **`Usp` 접두** (종목 프리픽스 없음, 네임스페이스로 구분) | 이미 종목 DB 안 |
| 생성 코드 | — | 테이블 매핑 `{테이블}Entity`, 프로시저 결과 `{이름}Record` | 파일명만으로 "테이블 vs 조회결과" 구분 |

---

## 3. 결정별 근거 · 장단점

### 3.1 private 인스턴스 필드 → `m` 접두사 (readonly 포함)

- **정한 이유**: 이전 PlayGround 코드베이스가 `m` 접두를 써왔고 이를 계승. `static readonly`(상수성)만 PascalCase,
  private 인스턴스는 readonly여도 `m` 접두 — `.editorconfig`의 `readonly_fields`를 `static` 한정으로 좁혀 IDE1006 오탐 제거.
- **장점**: 지역 변수/매개변수(camelCase)와 필드가 한눈에 구분됨. `this.` 없이도 필드임이 명확.
- **단점**: `_camelCase`가 .NET 다수파라 신규 합류자에겐 낯설 수 있음. 헝가리안 잔재라는 비판도 있음.

### 3.2 유즈케이스 → `{기능}Command` (읽기/쓰기 통일)

- **정한 이유**: 호출자(컨트롤러·액터)는 "이 유즈케이스를 실행"이면 충분하고, 읽기/쓰기는 알 필요 없다.
  `Command`를 **CQRS의 '쓰기 전용'이 아니라 GoF Command 패턴 = 캡슐화된 실행 가능 동작**으로 해석해 하나로 통일.
- **장점**:
  - 접미사 하나 → "이건 Query냐 Command냐" 판단 불필요(예: get-or-create처럼 애매한 경우).
  - 모든 유즈케이스가 동일한 형태 → 일관성·예측성.
  - 읽기/쓰기 구분이 **정말 중요한 곳(액터 토폴로지: 읽기 RoundRobin 풀 vs 쓰기 ConsistentHash)** 에는 이미 남아 있음.
- **단점**:
  - CQRS에 익숙한 사람은 `SoccerLandingContentsCommand`(순수 조회)를 보고 "상태를 바꾸나?"라고 오해할 수 있음.
    → **본 규칙을 CLAUDE.md에 명시**해 중화(내부 독자에겐 명확).
  - 한 기능에 쓰기가 여러 개(Update/Delete 등) 생기면 동사가 필요 — 그땐 그때 붙여 구분(현재는 기능당 1개).
- **대안(채택 안 함)**: 중립 접미사 `UseCase`(CQRS 오해 0) — 길고 C#에서 덜 흔해 `Command`를 택함.

### 3.3 Command vs Service — 판별 기준

- **Command** = **Application 유즈케이스**(앱이 노출하는 비즈니스 동작). `SoccerPlayerProfileCommand`, `LoginBySocialCommand`.
- **Service** = **기술 역량·외부 어댑터**(유즈케이스가 *의존하는* 재사용 수단, 인프라). `OAuthService`(제공자 I/O), `JwtTokenService`(토큰 발급), `PasswordHasherService`(해시).
- **판별 질문**: *"앱이 노출하는 비즈니스 동작인가, 그걸 수행하려고 갖다 쓰는 기술 수단인가?"*
- 예시로 자주 헷갈리는 것: `OAuthService`는 로그인 유즈케이스가 아니라 **Google/Kakao/LINE와 통신하는 어댑터**다.
  실제 로그인 동작(사용자 find-or-create + JWT 발급)은 `LoginBySocialCommand`이고, 이 커맨드가 `OAuthService`를 *사용*한다.
- **알려진 비일관(구조)**: `JwtTokenService`·`PasswordHasherService`는 Application 포트(`IJwtTokenService`·`IPasswordHasher`) 뒤에 있는데
  `OAuthService`만 포트 없이 컨트롤러가 직접 쓴다. 네이밍이 아닌 구조 이슈 — 후속으로 `IOAuthProvider` 포트화 검토 가능.

### 3.4 종목 프리픽스 `Soccer`

- **정한 이유**: 타 스포츠가 반드시 추가될 예정. 데이터 축적 후 리네임(마이그레이션)은 비싸므로 이름에 선반영.
  종목 구분이 이미 DB명(`PlayGround_Soccer`)·네임스페이스(`Generated.Soccer`)에 있어 *충돌 방지용*은 아니고, **코드 전반의 일관성** 목적.
- **적용**: 축구 도메인 테이블/엔티티/레코드/유즈케이스 → `Soccer*`. **프로시저는 `Usp*` 유지**(이미 종목 DB 안, 네임스페이스로 구분).
  **Account(공용 신원)는 프리픽스 없음**(`Users`, `LoginBySocialCommand` 등 — 인증은 종목 무관 공유).
- **장점**: "Soccer가 붙으면 축구 도메인"이 한눈에. 멀티스포츠 시 클래스명 충돌·혼동 없음.
- **단점**: 종목 DB 안에서 `PlayGround_Soccer.SoccerPlayers`는 다소 중복감. (엔티티=테이블명 불변식 유지를 위해 감수.)

---

## 4. 요약 — 새 코드 작성 시

- 유즈케이스면 `{기능}Command` (축구 전용이면 `Soccer{기능}Command`), 폴더 `{기능}/Commands/`.
- 기술 수단이면 `{역량}Service` (+ 가능하면 Application 포트 `I{역량}` 뒤에).
- private 인스턴스 필드는 `m` 접두, static/const는 PascalCase.
- 나머지는 .NET 표준(§1) 그대로.
