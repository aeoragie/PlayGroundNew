-- 선수 출전 기록 (시즌 통계 "12경기 824분" 원천). 플랫폼 선수만 수집 — 외부 선수 출전은 기록하지 않음.
-- 설계: Docs/Architecture/MatchSchemaDesign.md §3.4
CREATE TABLE [dbo].[SoccerMatchAppearances]
(
    [AppearanceId]  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [MatchId]       UNIQUEIDENTIFIER NOT NULL,          -- SoccerMatches.MatchId (앱 계층 참조)
    [TeamId]        UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeams.TeamId (앱 계층 참조)
    [PlayerId]      UNIQUEIDENTIFIER NOT NULL,          -- SoccerPlayers.PlayerId (앱 계층 참조)
    [MinutesPlayed] INT              NULL,              -- NULL = 분 미상 (경기 수만 집계)
    [IsStarter]     BIT              NOT NULL DEFAULT 0,

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,

    CONSTRAINT [PK_SoccerMatchAppearances] PRIMARY KEY ([AppearanceId])
);
