-- @entity: SoccerPlayerMatchStatRecord
-- @source: join
-- @join: SoccerMatchAppearances AS a (MatchId, TeamId, MinutesPlayed)
-- @join: SoccerMatches AS m (HomeTeamId, HomeTeamName, AwayTeamName, HomeScore, AwayScore, TournamentId, MatchedAt, MatchType)
-- @join: SoccerTournaments AS t (Format)
-- 관리 주체(UserId) 기준 선수 시즌 통계 조회 (선수 대시보드 시즌 통계 섹션 — 팀 경기 결과에서 자동 집계).
-- 결과셋 4개: ⓪선수 PlayerId(득점/도움 구분용, 없으면 NULL) → ①시즌 출전 경기(출전+종료 경기+대회 형식)
--            → ②선수의 득점·도움 이벤트 → ③출전 연도 목록(시즌 pill).
-- 팀 관점 변환·요약 집계는 Persistence/클라이언트 몫.
CREATE PROCEDURE [dbo].[UspGetSoccerPlayerSeasonStatsByUser]
    @UserId UNIQUEIDENTIFIER,
    @SeasonYear INT,
    @TargetPlayerId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [PlayerId]
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
          AND (@TargetPlayerId IS NULL OR [PlayerId] = @TargetPlayerId)
        ORDER BY [CreatedAt]);

    SELECT @PlayerId AS [PlayerId];

    SELECT
        a.[MatchId], a.[TeamId], a.[MinutesPlayed],
        m.[HomeTeamId], m.[HomeTeamName], m.[AwayTeamName], m.[HomeScore], m.[AwayScore], m.[TournamentId], m.[MatchedAt], m.[MatchType],
        t.[Format]
    FROM [dbo].[SoccerMatchAppearances] a WITH (NOLOCK)
    JOIN [dbo].[SoccerMatches] m WITH (NOLOCK)
        ON m.[MatchId] = a.[MatchId] AND m.[Status] = 'Completed' AND m.[DeletedAt] IS NULL
    LEFT JOIN [dbo].[SoccerTournaments] t WITH (NOLOCK)
        ON t.[TournamentId] = m.[TournamentId] AND t.[DeletedAt] IS NULL
    WHERE a.[PlayerId] = @PlayerId AND a.[DeletedAt] IS NULL
      AND (t.[SeasonYear] = @SeasonYear OR (m.[TournamentId] IS NULL AND YEAR(m.[MatchedAt]) = @SeasonYear))
    ORDER BY m.[MatchedAt] DESC;

    SELECT
        e.[EventId], e.[MatchId], e.[TeamId], e.[TeamName], e.[EventType],
        e.[PlayerId], e.[PlayerName], e.[AssistPlayerId], e.[AssistPlayerName], e.[MinuteOfPlay],
        e.[CreatedAt], e.[UpdatedAt], e.[DeletedAt]
    FROM [dbo].[SoccerMatchEvents] e WITH (NOLOCK)
    WHERE (e.[PlayerId] = @PlayerId OR e.[AssistPlayerId] = @PlayerId) AND e.[DeletedAt] IS NULL;

    SELECT DISTINCT COALESCE(t.[SeasonYear], YEAR(m.[MatchedAt])) AS SeasonYear
    FROM [dbo].[SoccerMatchAppearances] a WITH (NOLOCK)
    JOIN [dbo].[SoccerMatches] m WITH (NOLOCK)
        ON m.[MatchId] = a.[MatchId] AND m.[Status] = 'Completed' AND m.[DeletedAt] IS NULL
    LEFT JOIN [dbo].[SoccerTournaments] t WITH (NOLOCK)
        ON t.[TournamentId] = m.[TournamentId] AND t.[DeletedAt] IS NULL
    WHERE a.[PlayerId] = @PlayerId AND a.[DeletedAt] IS NULL
      AND COALESCE(t.[SeasonYear], YEAR(m.[MatchedAt])) IS NOT NULL
    ORDER BY SeasonYear DESC;
END
