-- @entity: SoccerTeamMatchRecord
-- @source: join
-- @join: SoccerMatches AS m (MatchId, TournamentId, HomeTeamId, HomeTeamName, AwayTeamId, AwayTeamName, HomeScore, AwayScore, HomePkScore, AwayPkScore, Status, MatchedAt, VenueName, MatchType)
-- @join: SoccerTournaments AS t (Name, Format)
-- 팀 관리자 기준 시즌 경기 결과 조회 (팀 대시보드 경기 결과 섹션).
-- 결과셋 4개: ⓪우리 팀 TeamId(IsHome 판별용, 팀 없으면 NULL) → ①종료 경기+대회명·형식(친선은 NULL)
--            → ②우리 팀 득점 이벤트(칩 조립용) → ③리그 순위(해당 시즌 League 스테이지의 우리 팀 행).
-- 팀 관점 변환(IsHome·상대·아군 스코어)·승무패·시즌 요약은 Persistence/클라이언트 몫.
CREATE PROCEDURE [dbo].[UspGetSoccerTeamMatchesByManager]
    @ManagerUserId UNIQUEIDENTIFIER,
    @SeasonYear INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    SELECT @TeamId AS [TeamId];

    SELECT
        m.[MatchId], m.[TournamentId], m.[HomeTeamId], m.[HomeTeamName], m.[AwayTeamId], m.[AwayTeamName],
        m.[HomeScore], m.[AwayScore], m.[HomePkScore], m.[AwayPkScore], m.[Status], m.[MatchedAt], m.[VenueName], m.[MatchType],
        t.[Name], t.[Format]
    FROM [dbo].[SoccerMatches] m WITH (NOLOCK)
    LEFT JOIN [dbo].[SoccerTournaments] t WITH (NOLOCK)
        ON t.[TournamentId] = m.[TournamentId] AND t.[DeletedAt] IS NULL
    WHERE (m.[HomeTeamId] = @TeamId OR m.[AwayTeamId] = @TeamId)
      AND m.[Status] = 'Completed' AND m.[DeletedAt] IS NULL
      AND (t.[SeasonYear] = @SeasonYear OR (m.[TournamentId] IS NULL AND YEAR(m.[MatchedAt]) = @SeasonYear))
    ORDER BY m.[MatchedAt] DESC;

    SELECT
        e.[EventId], e.[MatchId], e.[TeamId], e.[TeamName], e.[EventType],
        e.[PlayerId], e.[PlayerName], e.[AssistPlayerId], e.[AssistPlayerName], e.[MinuteOfPlay],
        e.[CreatedAt], e.[UpdatedAt], e.[DeletedAt]
    FROM [dbo].[SoccerMatchEvents] e WITH (NOLOCK)
    JOIN [dbo].[SoccerMatches] m WITH (NOLOCK)
        ON m.[MatchId] = e.[MatchId] AND (m.[HomeTeamId] = @TeamId OR m.[AwayTeamId] = @TeamId)
        AND m.[Status] = 'Completed' AND m.[DeletedAt] IS NULL
    WHERE e.[TeamId] = @TeamId AND e.[DeletedAt] IS NULL;

    SELECT TOP 1 s.[TeamRank]
    FROM [dbo].[SoccerTournamentStandings] s WITH (NOLOCK)
    JOIN [dbo].[SoccerTournaments] t WITH (NOLOCK)
        ON t.[TournamentId] = s.[TournamentId] AND t.[SeasonYear] = @SeasonYear AND t.[DeletedAt] IS NULL
    WHERE s.[TeamId] = @TeamId AND s.[StageType] = 'League' AND s.[DeletedAt] IS NULL
    ORDER BY s.[UpdatedAt] DESC;
END
