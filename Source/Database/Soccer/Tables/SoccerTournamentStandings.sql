-- 순위표 (저장식 — D5 확정안). 키 = (TournamentId, StageType, GroupName), 조·리그·스플릿 공용.
-- 경기 결과 저장 시 UspRecalculateSoccerTournamentStandings가 자동 갱신 (IsQualified·0전 행은 보존).
-- 설계: Docs/Architecture/MatchSchemaDesign.md §3.5 + §5 D5 확정안
CREATE TABLE [dbo].[SoccerTournamentStandings]
(
    [StandingId]    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TournamentId]  UNIQUEIDENTIFIER NOT NULL,          -- SoccerTournaments.TournamentId (앱 계층 참조)
    [StageType]     VARCHAR(20)      NOT NULL,          -- 'Group','Split1','Split2','League'
    [GroupName]     VARCHAR(30)      NULL,              -- UTF-8 (한글 10자) 조 ('1조'…). 리그·스플릿은 NULL
    [TeamId]        UNIQUEIDENTIFIER NULL,              -- SoccerTeams.TeamId (외부 팀은 NULL)
    [TeamName]      VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자) — 스코프 내 팀 식별 키 (대회 내 팀명 유일 가정)
    [TeamRank]      INT              NOT NULL,          -- 순위 (Rank는 T-SQL 예약어라 회피)
    [Played]        INT              NOT NULL DEFAULT 0,
    [Won]           INT              NOT NULL DEFAULT 0,
    [Drawn]         INT              NOT NULL DEFAULT 0,
    [Lost]          INT              NOT NULL DEFAULT 0,
    [Points]        INT              NOT NULL DEFAULT 0,
    [GoalsFor]      INT              NOT NULL DEFAULT 0,
    [GoalsAgainst]  INT              NOT NULL DEFAULT 0, -- 득실차는 파생 (GoalsFor - GoalsAgainst)
    [IsQualified]   BIT              NOT NULL DEFAULT 0, -- 진출권 teal 행 (조별 진출·리그 왕중왕전 공용, 수동/시드 설정)

    -- KFA 데이터 적재 대비 (결정 #5) — 경기 없이 순위표만 적재되는 경우 대비
    [DataSource]    VARCHAR(20)      NOT NULL DEFAULT 'User', -- 'User','KfaApi','Seed'
    [ExternalId]    VARCHAR(64)      NULL,
    [SyncStatus]    VARCHAR(20)      NULL,

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,

    CONSTRAINT [PK_SoccerTournamentStandings] PRIMARY KEY ([StandingId])
);
