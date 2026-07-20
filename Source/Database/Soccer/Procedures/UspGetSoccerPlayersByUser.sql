-- @entity: SoccerManagedPlayerRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId, Name, AgeGroup, PhotoUrl, IsGuardianManaged)
-- @join: SoccerTeamPlayers AS tp (JerseyNumber, Position)
-- @join: SoccerTeams AS t (TeamName)
-- 한 계정이 관리하는 선수(자녀) 전부. **보호자는 자녀를 여러 명 가질 수 있다.**
-- 다른 조회 프로시저들이 TOP 1로 한 명만 집던 자리를 이 목록이 대체한다
-- (어느 자녀를 볼지는 화면이 PlayerId로 정한다).
-- 시즌 스탯은 공식 경기만 집계한다 — 선수 대시보드·허브가 같은 수를 보여야 한다(Design.FriendlyMatch).
CREATE PROCEDURE [dbo].[UspGetSoccerPlayersByUser]
    @UserId UNIQUEIDENTIFIER,
    @SeasonYear INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.[PlayerId], p.[Name], p.[AgeGroup], p.[PhotoUrl], p.[IsGuardianManaged],
        tp.[JerseyNumber], tp.[Position],
        t.[TeamName],
        (SELECT COUNT(*)
         FROM [dbo].[SoccerMatchAppearances] a WITH (NOLOCK)
         INNER JOIN [dbo].[SoccerMatches] am WITH (NOLOCK)
            ON am.[MatchId] = a.[MatchId] AND am.[DeletedAt] IS NULL
           AND am.[Status] = 'Completed' AND am.[MatchType] = 'Official'
         WHERE a.[PlayerId] = p.[PlayerId] AND a.[DeletedAt] IS NULL
           AND YEAR(am.[MatchedAt]) = @SeasonYear) AS [Appearances],
        (SELECT COUNT(*)
         FROM [dbo].[SoccerMatchEvents] e WITH (NOLOCK)
         INNER JOIN [dbo].[SoccerMatches] em WITH (NOLOCK)
            ON em.[MatchId] = e.[MatchId] AND em.[DeletedAt] IS NULL
           AND em.[Status] = 'Completed' AND em.[MatchType] = 'Official'
         WHERE e.[PlayerId] = p.[PlayerId] AND e.[EventType] <> 'OwnGoal' AND e.[DeletedAt] IS NULL
           AND YEAR(em.[MatchedAt]) = @SeasonYear) AS [Goals],
        (SELECT COUNT(*)
         FROM [dbo].[SoccerMatchEvents] e WITH (NOLOCK)
         INNER JOIN [dbo].[SoccerMatches] em WITH (NOLOCK)
            ON em.[MatchId] = e.[MatchId] AND em.[DeletedAt] IS NULL
           AND em.[Status] = 'Completed' AND em.[MatchType] = 'Official'
         WHERE e.[AssistPlayerId] = p.[PlayerId] AND e.[DeletedAt] IS NULL
           AND YEAR(em.[MatchedAt]) = @SeasonYear) AS [Assists]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    LEFT JOIN [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        ON tp.[PlayerId] = p.[PlayerId] AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
    LEFT JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = tp.[TeamId] AND t.[DeletedAt] IS NULL
    WHERE p.[UserId] = @UserId AND p.[DeletedAt] IS NULL
    ORDER BY p.[CreatedAt];
END
