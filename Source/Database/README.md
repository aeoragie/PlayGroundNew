# Database — SQL 원본 (단일 진실 소스)

논리 DB를 **Account / Soccer** 두 개로 물리 분리한다. DB 배포는 이 폴더의 SQL 파일 기준.

## Account — 인증 · 신원

사용자 계정, 인증, 세션. 대회 운영 서비스(별도 Client)와 **SSO로 공유**할 수 있도록 분리.

- Users, UserRefreshTokens, UserSocialAccounts, UserSessions
- EmailVerifications, PasswordResetTokens, Roles 등

## Soccer — 도메인

축구 도메인 전체.

- Team: Teams, TeamStaff, TeamRecruitments, TeamPosts …
- Player: Players, SeasonStats, CareerEntries, PlayerLinkRequests …
- Match: Matches, Competitions, Schedules, CompetitionStaff …
- Agent(선반영): AgentProfile, PlayerAgentLink, Commission, AgentReview …
- Content: Articles, CommunityPosts …

## 분리 원칙 (2026-07-12 확정)

- **DB 간 FK·트랜잭션 불가** (SQL Server). `SoccerPlayers.UserId → Account.Users.UserId`는 **앱 계층 정합성**으로 관리.
- 온보딩처럼 두 DB에 걸치는 작업은 **Account 먼저 생성 → 성공 시 Soccer 프로필** 순서 (분산 트랜잭션 회피).
- Community/Records는 현 단계 Soccer에 포함 (독립 서비스화 시점에 분리 검토).

## 규칙

- 테이블명 PascalCase 복수형 + Soccer 도메인은 `Soccer` 프리픽스(`SoccerPlayers`), 컬럼명
  PascalCase(`PlayerId`), 프로시저 `Usp` 접두사 (Account 공용 신원 테이블은 프리픽스 없음).
- **enum 컬럼은 정수가 아니라 문자열(`VARCHAR(20)`)로 저장** — 값은 C# enum 멤버 이름 그대로
  (`'General'`, `'Pending'`), 컬럼 주석에 허용 값을 나열한다. 읽는 쪽에서 enum으로 컨버팅.
  (예: `Users.UserRole`, `SoccerPlayerInvites.Status`, `SoccerTeams.DataSource`)
- 스키마 변경은 반드시 이 SQL 파일을 먼저 수정한다.
- 각 DB 폴더: `Schema/ Tables/ Procedures/ Queries/ Indexes/ Seeds/`.

## 로컬 개발 DB (SQLEXPRESS) 셋업

로컬 개발 DB는 `.\SQLEXPRESS` (2026-07 LocalDB에서 전환). 한글이 포함된 파일은
UTF-8 코드페이지(`-f 65001`) 필수 — 전 파일에 붙여도 무해하므로 일괄 적용을 권장.

```powershell
# DB 생성
sqlcmd -S .\SQLEXPRESS -b -Q "IF DB_ID('PlayGround_Account') IS NULL CREATE DATABASE [PlayGround_Account]; IF DB_ID('PlayGround_Soccer') IS NULL CREATE DATABASE [PlayGround_Soccer];"

# 각 DB에 Tables → Procedures → Seeds 순으로 폴더 전체 적용 (FK 없음 — 폴더 내 순서 무관)
foreach ($f in (Get-ChildItem Account\Tables\*.sql) + (Get-ChildItem Account\Procedures\*.sql)) {
    sqlcmd -S .\SQLEXPRESS -d PlayGround_Account -b -f 65001 -i $f.FullName
}
foreach ($f in (Get-ChildItem Soccer\Tables\*.sql) + (Get-ChildItem Soccer\Procedures\*.sql) + (Get-ChildItem Soccer\Seeds\*.sql)) {
    sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -b -f 65001 -i $f.FullName
}
```

- SQL 파일은 SSDT 선언형이라 순수 `CREATE`문 — 신규 DB 기준이며 재실행 시 "이미 존재" 오류.
  기존 DB에 변경을 반영할 때는 대상 객체를 `DROP` 후 해당 파일만 개별 적용한다.
- 적용 후 한글 확인: `sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -u -Q "SELECT TOP 3 Title FROM SoccerLandingContents"`

`appsettings.Development.json`의 커넥션이 `.\SQLEXPRESS`(PlayGround_Account / PlayGround_Soccer)를 가리킨다.
