# 다국어(i18n) 구조와 작업 규칙

> 대상: `PlayGround.Client`(Blazor WASM)의 **사용자 노출 문자열 전량**.
> 기본 문화권 **ko**(SPEC 카피가 원본), 확장 예정 **ja / en**.
> 도입 커밋 `7d9de8d` · 생성기 분리 `163aecc` · 조사 모디파이어 `51c8b7e`.

## 1. 원칙 5가지

1. **사용자에게 보이는 문자열은 코드에 두지 않는다.** 리소스(JSON) → 타입드 접근자(`AppText.*`) 경유.
2. **키는 매직 스트링이 아니다.** 생성기가 JSON에서 타입드 접근자를 만들어 **오타·존재하지 않는 키는 컴파일 실패**.
3. **문법은 리소스가 소유한다.** 어순·조사·경어 등 언어별 문법은 각 언어 JSON 안에서 해결한다.
   컴포넌트 코드에 **문화권 분기(`if (culture == "ko")`)를 만들지 않는다.**
4. **ko 값은 SPEC 카피 그대로.** 한 글자도 바꾸지 않는다(CLAUDE.md UI 규칙과 동일). 번역은 ja/en에서 한다.
5. **활성 문화권만 내려받는다.** WASM 다운로드를 늘리지 않기 위해 전 언어 번들링을 하지 않는다.

## 2. 파일 구조

```
PlayGround.Client/
├── wwwroot/i18n/
│   ├── {Domain}.ko.json         ← 원본(SPEC 카피). 예: Team.ko.json
│   └── {Domain}.ja.json         ← 번역. 없으면 ko로 폴백
├── Localization/
│   ├── ILocalizer.cs            ← 키 → 문자열 해석 포트
│   ├── JsonLocalizer.cs         ← 지연 로드·병합·폴백 구현
│   ├── KoreanParticle.cs        ← 한국어 조사 모디파이어 해석
│   ├── CultureState.cs          ← 문화권 전환 + localStorage(`pg.culture`)
│   ├── AppText.cs               ← 앰비언트 접근점(부분 클래스)
│   └── Generated/AppText.g.cs   ← **생성물. 수동 편집 금지**
└── Program.cs                   ← 기동 시 지속 문화권 로드 + AppText.Loc 주입

Source/Tools/Generator.Localization/   ← 별도 콘솔 도구(DB 생성기와 분리)
```

## 3. 런타임 동작

- 기동 시 `localStorage['pg.culture']`를 읽어 해당 문화권 JSON을 fetch. 없으면 **ko**.
- **폴백 3단**: 활성 문화권 값 → ko 값 → **키 문자열 그대로**(번역 누락을 화면에서 바로 발견하기 위함).
- 도메인 파일은 병합되고 내부 키는 `"{Domain}.{Key}"`로 네임스페이스가 붙는다.
- 문화권 전환은 `CultureState.SetCultureAsync("ja")` — 활성 파일만 새로 받는다.

## 4. 생성기

```bash
cd Source/Tools/Generator.Localization && dotnet run
```

- 입력: `wwwroot/i18n/*.ko.json`(기본 문화권이 스키마의 기준)
- 출력: `Localization/Generated/AppText.g.cs` — **커밋한다**(clone 즉시 빌드되도록. DB 생성기와 같은 정책)
- 값에 플레이스홀더가 없으면 **프로퍼티**, 있으면 **인자 메서드**로 생성한다.

```jsonc
"Timeline": "타임라인"              → AppText.Records.Timeline
"Coach":    "{0} 감독"              → AppText.Records.Coach(name)
"CardSummary": "경고 {0} · 퇴장 {1}" → AppText.Records.CardSummary(y, r)
```

**ja에만 있고 ko에 없는 키는 생성되지 않는다** — 키 추가는 반드시 ko부터.

## 5. 키 컨벤션

| 규칙 | 내용 |
|---|---|
| 파일 = 도메인 | 화면/기능 묶음 단위. `Landing` `Auth` `Settings` `Claim` `Notification` `Team` `Player` `Dashboard` `Hub` `Records` `Agent` `Correction` `Shared` + 횡단 `Enums` `Errors` |
| 키 = **PascalCase** | `ContinueLine`, `ExportErrorCooldown` (camelCase 금지 — 접근자와 1:1 대응) |
| 접두 중복 금지 | `Landing.PageTitle`이지 `Landing.LandingPageTitle`이 아니다 |
| 의미 기반 | 문구가 아니라 **역할**로 짓는다. `ErrorLoginFailed`(○) / `LoginFailedMessage2`(✕) |
| 접미 관례 | `...Title` `...Body` `...Placeholder` `...Helper` `...Toast` `...Error` `...Aria` |
| 문장 조각 | 굵게/링크로 쪼개지는 문장은 `...Prefix` / `...Bold` / `...Link` / `...Suffix`로 분해 |

## 6. 한국어 조사(助詞) — 모디파이어

한국어 조사는 **앞 값의 받침**에 따라 달라져서 정적 번역문으로는 맞출 수 없다
(`"{0}가 연결됐어요"` → "구글**가**" ✕). 리소스에 모디파이어를 쓴다.

```jsonc
// Settings.ko.json
"LinkedToast": "{0:이/가} 연결됐어요."          // 구글이 / 카카오가
"ConnectHint": "{0:으로도/로도} 로그인할 수 있게 연결해요"
// Notification.ko.json
"BodyTeamInvite": "{0:이/가} {1:을/를} 선수단에 초대했어요"
```

- 표기: **`{n:받침있음/받침없음}`** — `이/가`, `은/는`, `을/를`, `과/와`, `으로/로`
- 호출부는 **원본 값만** 넘긴다: `AppText.Settings.LinkedToast(provider)`
- **일본어·영어 리소스는 모디파이어를 쓰지 않는다** → 언어 구분이 코드가 아니라 데이터에 있다
- 받침 판별(`KoreanParticle`): 한글은 유니코드 종성 규칙, 숫자는 발음(0·1·3·6·7·8 받침),
  영문은 알파벳 이름 발음(L·M·N·R 받침)
- **`으로/로`만 ㄹ 받침이 예외** — "이메일**로**"가 맞고 "이메일으로"는 틀리다.
  받침 문자열이 `으`로 시작하면 ㄹ 종성을 받침 없음으로 본다.
- 회귀 방지: `Tests.Unit/Client/KoreanParticleTests.cs` (한글·숫자·영문·ㄹ 예외·인자 부족 24케이스)

## 7. 작업 규칙

### 신규 코드 (필수)

> **하드코딩 한글 금지.** 새 문자열은 ① `{Domain}.ko.json`에 키 추가 → ② `.ja.json`에도 추가 →
> ③ 생성기 실행 → ④ `@AppText.{Domain}.{Key}` 사용.

### 기존 화면 이관 절차

1. **대상 수집** — `grep -rlP "[가-힣]" <폴더> --include=*.razor`
2. **리소스 작성** — ko는 SPEC 카피 그대로 복사(재작성 금지), ja는 번역
3. **생성** — `cd Source/Tools/Generator.Localization && dotnet run`
4. **치환** — 마크업·`@code`·토스트·확인 모달·검증 오류·`PageTitle`·`aria-label`·`placeholder` 전부
5. **빌드** — `dotnet build Source/PlayGround/PlayGround.Client/PlayGround.Client.csproj`
6. **검증** — 헤드리스로 ko 카피 동일 + 미해석 키 0 + ja 전환 렌더 (§9)
7. **커밋** — 도메인 단위로 (`i18n 이관 — {Domain} 도메인 (N파일)`)

### 이관 대상이 아닌 것 (건드리지 말 것)

| 대상 | 이유 |
|---|---|
| DB 저장 enum 멤버명 (`'Completed'`, `'Goal'`) | 데이터 값 — 개명은 마이그레이션 동반 |
| 로그·예외 메시지 | 영어 규칙(CLAUDE.md) |
| Tailwind 클래스·CSS | 표시 문자열 아님 |
| `.razor` 주석(`@* *@`), C# 주석 | 개발자용 |
| 이모지·기호 아이콘 (`⚽`, `★`, `→`, `·`) | 디자인 요소 — 번역 대상 아님 |
| 문자 범위 검증 로직 (`c >= '가' && c <= '힣'`) | 표시가 아니라 로직 |
| 팀명·선수명 등 **데이터** | 사용자 입력값 |
| `Pages/Dev/*` 컴포넌트 카탈로그 | 개발자 전용 — 사용자에게 노출되지 않는다 |

### 안티패턴

```csharp
// ✕ 문화권 분기 — 언어가 늘수록 증식한다
if (AppText.Culture == "ko") { ... }

// ✕ 키를 문자열로 직접 — 오타를 컴파일러가 못 잡는다
Loc.Get("Team.RosterTitle")

// ✕ 문장을 코드에서 조립 — 어순이 다른 언어에서 깨진다
$"{teamName} 팀의 {count}명"        →  "Team.RosterSummary": "{0} 팀의 {1}명"

// ✕ Domain enum 표시 라벨을 Domain 계층에서 반환 (한글 하드코딩)
//   → 표시는 표현 계층(AppText)으로 라우팅한다. 예: CorrectionListSection의 FieldLabel/StatusLabel
```

## 8. 진행 현황 (2026-08-02)

**이관 완료** — `Pages/`·`Components/`의 `.razor` 잔여 0줄, `Models/`·`Services/`의 표시 문자열 0건.
(`Pages/Dev/*` 개발자 카탈로그는 §7 "이관 대상이 아닌 것"에 따라 제외.)

| 도메인 | 키 | 비고 |
|---|---:|---|
| Dashboard | 443 | 팀 관리·모바일·공용 다이얼로그 |
| Player | 324 | 선수 대시보드·공개 프로필 |
| Team | 172 | 탐색 + 공개홈 |
| Records | 136 | 목록·대회 상세·경기 상세·아카이브 |
| Shared | 128 | 공용 컴포넌트·폼·업로더·알림 패널 |
| Settings | 126 | |
| Auth | 109 | |
| Landing | 64 | |
| Notification | 61 | 알림 문구·딥링크 라벨·상대 시각 |
| Claim | 59 | |
| Hub | 58 | |
| Agent | 49 | flag off 표면 포함 |
| **Enums** | 40 | `Models/*.ToLabel()` 표시 라벨 |
| **Errors** | 32 | API 클라이언트·다이얼로그 공통 실패 메시지 |
| Correction | 16 | |

합계 **1,817키** × ko/ja. 키 일치·플레이스홀더 인덱스 일치는 §9로 검증한다.

`Enums`·`Errors`는 화면이 아니라 **횡단 관심사** 도메인이다.
- `Enums` — `SoccerCompetitionType.ToLabel()` 같은 열거형 표시 라벨. 코드가 라벨 문자열로
  분기하면 안 된다(`label switch { "리그" => ... }` ✕ → `type switch { SoccerCompetitionType.League => ... }` ○).
- `Errors` — `*Client.cs`의 폴백 실패 메시지. 같은 문구가 20곳 넘게 중복돼 있어 값 단위로 묶었다.

## 9. 검증 방법

빌드만으로는 **누락**을 못 잡는다(하드코딩이 남아도 빌드는 통과). 화면으로 확인한다.

```bash
# 서버 기동 (Client 수정 후에는 반드시 전체 재시작 — CLAUDE.md 반복 함정)
dotnet build Source/PlayGround/PlayGround.Server/PlayGround.Server.csproj
dotnet run --project Source/PlayGround/PlayGround.Server
```

리소스 정합성(ko/ja 키 일치 + 플레이스홀더 인덱스 일치 + ja에 조사 모디파이어 혼입)은
빌드가 못 잡으므로 `wwwroot/i18n`에서 스크립트로 확인한다 — ja의 `{0}`이 하나 빠지면
문화권 전환 시 `string.Format`이 던진다.

```bash
cd Source/PlayGround/PlayGround.Client/wwwroot/i18n
node -e "const fs=require('fs');const P=/\{(\d+)(?::[^}]*)?\}/g;
const idx=s=>{const t=new Set();let m;P.lastIndex=0;while((m=P.exec(s)))t.add(+m[1]);return [...t].sort().join(',')};
let bad=0;
for(const d of [...new Set(fs.readdirSync('.').map(f=>f.split('.')[0]))]){
 const ko=JSON.parse(fs.readFileSync(d+'.ko.json','utf8')),ja=JSON.parse(fs.readFileSync(d+'.ja.json','utf8'));
 for(const k of Object.keys(ko)){
  if(!(k in ja)){console.log('ja누락',d,k);bad++;continue}
  if(idx(ko[k])!==idx(ja[k])){console.log('플레이스홀더 불일치',d,k);bad++}
  if(/\{\d+:[^}]*\/[^}]*\}/.test(ja[k])){console.log('ja에 조사 모디파이어',d,k);bad++}}}
console.log(bad?'❌ '+bad:'✅ 정합')"
```

헤드리스 체크 3종:
1. **ko 카피 동일** — 이관 전 문구가 그대로 렌더되는가
2. **미해석 키 0** — 화면 텍스트에 `"{Domain}."` 형태가 보이면 키 오타/누락
3. **ja 전환** — `localStorage.setItem('pg.culture','ja')` 후 새로고침 시 일본어 렌더,
   미이관 문자열만 한국어로 남는지(= 점진 이관 정상)

## 10. 미착수 (ja/en 실제 오픈 시)

- **언어 스위처 UI** + 전환 시 앱 전역 재렌더 배선(`CultureState.Changed` 구독)
- 날짜·숫자 형식 문화권 대응(현재 `CultureInfo.InvariantCulture` 고정 자리 존재)
- en 리소스 작성, 번역 워크플로(외부 번역가 전달 형식)
- 서버 발신 문자열(이메일·알림 본문) 다국어 — 현재 Client 전용 구조

## 11. 열거형 표기 모델 (2026-08-09 기록 — 실작업 보류)

enum은 세 영역을 지나며 두 번 모습을 바꾼다.

```
화면(브라우저 표기)  ←[① 로컬라이징 표기]→  클라·서버 로직(enum)  ←[② enum↔string]→  DB(저장)
```

- **로직 영역**(Client·Server·와이어 JSON) 안에서는 enum 그대로 오간다. 와이어의
  `LenientEnumJsonConverter`는 전송 표현일 뿐 영역 전이가 아니다.
- **② DB 전이**는 `EnumColumn`(Core.Shared) 한 곳으로 완결됐다.
- **① 표기 전이**가 이 문서의 영역이다 — 국가별 서비스 로컬라이징과 직결되며, 전이 도구는 둘뿐이다.

**화면이 enum을 표기할 때는 `ToLabel()`(`Models/SoccerDomainEnumLabels`) 하나만 지난다** (2026-08-09 구조 완료).
raw `ToString()`·문자열 보간 금지. 아직 국가별 표기가 결정되지 않은 enum(포지션·학년 U표기·연령 그룹)은
**통과형 ToLabel**(멤버 이름 그대로 반환, 파일 안 TODO)로 같은 전이 지점을 지난다 — 리소스로 승격해도
호출부는 변하지 않는다. `ToText()`(`Models/EnumDisplay`)는 통과형의 구현 재료이자 "-"·생략 폴백용
내부 도구다(Unknown→null).

중앙에 두는 기준은 **정식 라벨(값의 이름)**이다. enum으로 분기하는 화면 문맥 카피는 각 화면에 남는다 —
상세 히어로의 긴 형식 표기("조별 예선 + 토너먼트"), 상태별 본문·액션 버튼(MyApplicationStatusCard·
RosterSection ActionLabel), 항목별 예시 플레이스홀더(Correction 다이얼로그) 등.

### 완료 (2026-08-09 구조 선행)

1. **표기 전이 지점 단일화** — 페이지-로컬 정식 라벨 switch를 `SoccerDomainEnumLabels`로 이관
   (CorrectionListSection 항목·상태, ClaimPage·NotificationPresenter 관계, AgentApprovalPage 열람 로그,
   PlayerPublicProfilePage 대회 유형, RecordsDetailPage 수상). `SoccerPreferredFootLabels`도 흡수.
   관계 라벨처럼 두 도메인에 같은 값으로 중복돼 있던 키는 한쪽(Claim)만 쓴다.
2. **라벨 전수 가드** — `SoccerEnumLabelGuardTests`가 리플렉션으로 라벨 메서드 전부를 찾아
   전 멤버 호출: Unknown은 무예외만, 그 외는 비어 있지 않은 라벨 필수. switch `_` 폴백 때문에
   컴파일러가 못 잡는 멤버 추가·라벨 누락을 잡는다.

### 보류 작업 (로컬라이징 착수 시)

1. **통과형 라벨의 국가별 표기 결정** — 포지션(GK/DF/MF/FW)·학년(U표기)·연령 그룹.
   파일 안 TODO(로컬라이징) 주석이 자리를 표시한다.
2. **라벨 키의 Enums 도메인 통합** — 중앙 함수가 참조하는 키가 Correction·Claim·Agent·Records
   도메인에 흩어져 있다(값 이동 없이 함수만 먼저 모았다). 키를 `Enums` 도메인으로 옮기고
   중복 키(Notification.Relation* 등)를 정리한다.
3. **raw 표기 가드** — `.razor`에서 enum 프로퍼티 직접 보간(`@item.Status`)을 잡는
   정적 가드는 타입 정보가 필요해 비용이 크다. 1·2 완료 후 실익이 작으면 하지 않는다.
