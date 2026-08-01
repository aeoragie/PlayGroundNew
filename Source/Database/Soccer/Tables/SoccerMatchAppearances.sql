-- 선수 출전 기록 (시즌 통계 "12경기 824분" 원천 + 공식 경기 상세 라인업 원천).
-- 플랫폼 선수는 PlayerId로 집계에 연결되고, 외부 선수(원정 라인업 등)는 PlayerId NULL + PlayerName만.
-- 팀 귀속은 TeamName으로(외부 팀은 TeamId NULL). 등번호·포지션·주장은 상세 라인업 표시 전용.
-- 설계: Docs/Architecture/MatchSchemaDesign.md §3.4
CREATE TABLE [dbo].[SoccerMatchAppearances]
(
    [AppearanceId]  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [MatchId]       UNIQUEIDENTIFIER NOT NULL,          -- SoccerMatches.MatchId (앱 계층 참조)
    [TeamId]        UNIQUEIDENTIFIER NULL,              -- SoccerTeams.TeamId (외부 팀은 NULL)
    [TeamName]      VARCHAR(300)     NOT NULL DEFAULT '', -- UTF-8 (한글 100자) 홈/원정 귀속 (경기의 HomeTeamName/AwayTeamName와 매칭)
    [PlayerId]      UNIQUEIDENTIFIER NULL,              -- SoccerPlayers.PlayerId (외부 선수는 NULL)
    [PlayerName]    VARCHAR(150)     NOT NULL DEFAULT '', -- UTF-8 (한글 50자)
    [JerseyNumber]  INT              NULL,              -- 등번호
    [Position]      VARCHAR(10)      NULL,              -- 'GK','DF','MF','FW'
    [IsCaptain]     BIT              NOT NULL DEFAULT 0, -- 주장 (라인업 'C' 마크)
    [MinutesPlayed] INT              NULL,              -- NULL = 분 미상 (경기 수만 집계)
    [IsStarter]     BIT              NOT NULL DEFAULT 0,

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,

    CONSTRAINT [PK_SoccerMatchAppearances] PRIMARY KEY ([AppearanceId])
);
