-- @entity: SoccerTournamentOptionRecord
-- @source: join
-- @join: SoccerTournaments AS t (TournamentId, Name, Format, AgeGroup, SeasonYear)
-- 경기 결과 입력 폼의 "대회/리그" 선택지.
-- 해당 시즌의 진행중·종료 대회를 노출하되, 우리 팀이 이미 참가한 대회를 먼저 보여준다
-- (팀은 대개 같은 리그에 계속 결과를 입력한다 — 손이 덜 가는 순서).
CREATE PROCEDURE [dbo].[UspGetSoccerTournamentOptionsByManager]
    @ManagerUserId UNIQUEIDENTIFIER,
    @SeasonYear    INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    SELECT
        t.[TournamentId], t.[Name], t.[Format], t.[AgeGroup], t.[SeasonYear]
    FROM [dbo].[SoccerTournaments] t WITH (NOLOCK)
    -- 예정 대회도 포함한다 — 첫 경기 결과가 예정 상태에서 들어오는 일이 흔하다
    WHERE t.[SeasonYear] = @SeasonYear
      AND t.[Status] IN ('Scheduled', 'InProgress', 'Completed')
      AND t.[DeletedAt] IS NULL
    ORDER BY
        CASE WHEN EXISTS (
            SELECT 1 FROM [dbo].[SoccerMatches] m WITH (NOLOCK)
            WHERE m.[TournamentId] = t.[TournamentId] AND m.[DeletedAt] IS NULL
              AND (m.[HomeTeamId] = @TeamId OR m.[AwayTeamId] = @TeamId)
        ) THEN 0 ELSE 1 END,
        t.[Name];
END
