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

- **DB 간 FK·트랜잭션 불가** (SQL Server). `Players.UserId → Account.Users.Id`는 **앱 계층 정합성**으로 관리.
- 온보딩처럼 두 DB에 걸치는 작업은 **Account 먼저 생성 → 성공 시 Soccer 프로필** 순서 (분산 트랜잭션 회피).
- Community/Records는 현 단계 Soccer에 포함 (독립 서비스화 시점에 분리 검토).

## 규칙

- 테이블명 PascalCase 복수형(`Players`), 컬럼명 PascalCase(`PlayerId`), 프로시저 `Usp` 접두사.
- 스키마 변경은 반드시 이 SQL 파일을 먼저 수정한다.
- 각 DB 폴더: `Schema/ Tables/ Procedures/ Queries/ Indexes/ Seeds/`.

## 로컬 개발 DB (LocalDB) 셋업

한글 시드는 UTF-8 코드페이지(`-f 65001`) 필수.

```bash
# Soccer
sqlcmd -S '(localdb)\MSSQLLocalDB' -Q "IF DB_ID('PlayGround_Soccer') IS NULL CREATE DATABASE [PlayGround_Soccer];"
sqlcmd -S '(localdb)\MSSQLLocalDB' -d PlayGround_Soccer -i Soccer/Tables/LandingContents.sql
sqlcmd -S '(localdb)\MSSQLLocalDB' -d PlayGround_Soccer -i Soccer/Procedures/UspGetLandingContents.sql
sqlcmd -S '(localdb)\MSSQLLocalDB' -d PlayGround_Soccer -f 65001 -i Soccer/Seeds/LandingContents.Seed.sql

# Account (테이블 먼저, 그다음 프로시저)
sqlcmd -S '(localdb)\MSSQLLocalDB' -Q "IF DB_ID('PlayGround_Account') IS NULL CREATE DATABASE [PlayGround_Account];"
sqlcmd -S '(localdb)\MSSQLLocalDB' -d PlayGround_Account -i Account/Tables/Users.sql
sqlcmd -S '(localdb)\MSSQLLocalDB' -d PlayGround_Account -i Account/Tables/SocialAccounts.sql
sqlcmd -S '(localdb)\MSSQLLocalDB' -d PlayGround_Account -f 65001 -i Account/Procedures/UspGetUserByEmail.sql
# … 나머지 Account/Procedures/*.sql 동일하게 적용
```

`appsettings.Development.json`의 커넥션이 `(localdb)\MSSQLLocalDB`(PlayGround_Account / PlayGround_Soccer)를 가리킨다.
