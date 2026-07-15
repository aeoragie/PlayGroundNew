-- 대회 수상 (우승/준우승/페어플레이). 역대 우승팀 = 같은 SeriesSlug의 과거 연도 Champion.
-- 아카이브 목록의 "우승" 뱃지도 여기서. 설계: Docs/Architecture/MatchSchemaDesign.md §3.7
CREATE TABLE [dbo].[SoccerTournamentAwards]
(
    [AwardId]       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TournamentId]  UNIQUEIDENTIFIER NOT NULL,          -- SoccerTournaments.TournamentId (앱 계층 참조)
    [AwardType]     VARCHAR(20)      NOT NULL,          -- 'Champion','RunnerUp','FairPlay'
    [TeamId]        UNIQUEIDENTIFIER NULL,              -- SoccerTeams.TeamId (외부 팀은 NULL)
    [TeamName]      VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자)
    [DisplayOrder]  INT              NOT NULL DEFAULT 0,

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,

    CONSTRAINT [PK_SoccerTournamentAwards] PRIMARY KEY ([AwardId])
);
