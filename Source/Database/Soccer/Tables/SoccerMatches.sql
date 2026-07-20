-- 경기. 친선 경기는 TournamentId NULL. 외부 팀 대비 TeamId NULL + TeamName 병행.
-- 승/무/패는 스코어에서 파생(저장하지 않음). 설계: Docs/Architecture/MatchSchemaDesign.md §3.2
CREATE TABLE [dbo].[SoccerMatches]
(
    [MatchId]        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    -- 공식/친선. 집계(순위표·시즌 스탯·공개 프로필)는 Official만 — 설계 결정 7 + Design.FriendlyMatch.
    -- 팀이 직접 입력하는 경기는 항상 Friendly다(공식 기록의 주체는 주최측).
    [MatchType]      VARCHAR(20)      NOT NULL DEFAULT 'Official', -- 'Official','Friendly'
    [TournamentId]   UNIQUEIDENTIFIER NULL,              -- SoccerTournaments.TournamentId (앱 계층 참조). NULL = 친선
    [StageType]      VARCHAR(20)      NULL,              -- 'Group','Split1','Split2','Knockout','League' — 상세 탭 배치. 친선은 NULL
    [GroupName]      VARCHAR(30)      NULL,              -- UTF-8 (한글 10자) 조별 스테이지의 조 ('1조'~'14조')
    [RoundName]      VARCHAR(30)      NULL,              -- 조별 'R1'~'R3', 토너먼트 'PO','R16','QF','SF','F'
    [HomeTeamId]     UNIQUEIDENTIFIER NULL,              -- SoccerTeams.TeamId (앱 계층 참조, 외부 팀은 NULL)
    [HomeTeamName]   VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자)
    [AwayTeamId]     UNIQUEIDENTIFIER NULL,
    [AwayTeamName]   VARCHAR(300)     NOT NULL,
    [HomeScore]      INT              NULL,              -- NULL = 미종료
    [AwayScore]      INT              NULL,
    [HomePkScore]    INT              NULL,              -- 승부차기 — "1 (4)" 괄호 표기 (있을 때만)
    [AwayPkScore]    INT              NULL,
    [Status]         VARCHAR(20)      NOT NULL DEFAULT 'Scheduled', -- 'Scheduled','Completed','Canceled'
    [MatchedAt]      DATETIME2        NULL,              -- 경기 일시 (시간 미정 대비 NULL 허용)
    [VenueName]      VARCHAR(300)     NULL,              -- UTF-8 (한글 100자) 구장

    -- KFA 데이터 적재 대비 (결정 #5)
    [DataSource]     VARCHAR(20)      NOT NULL DEFAULT 'User', -- 'User','KfaApi','Seed'
    [ExternalId]     VARCHAR(64)      NULL,
    [SyncStatus]     VARCHAR(20)      NULL,

    [CreatedAt]      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]      DATETIME2        NULL,

    CONSTRAINT [PK_SoccerMatches] PRIMARY KEY ([MatchId])
);
