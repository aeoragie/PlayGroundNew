-- @entity: SoccerManagedPlayerRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId, Name, AgeGroup, PhotoUrl, IsGuardianManaged)
-- @join: SoccerTeamPlayers AS tp (JerseyNumber, Position)
-- @join: SoccerTeams AS t (TeamName)
-- 한 계정이 관리하는 선수(자녀) 전부. **보호자는 자녀를 여러 명 가질 수 있다.**
-- 다른 조회 프로시저들이 TOP 1로 한 명만 집던 자리를 이 목록이 대체한다
-- (어느 자녀를 볼지는 화면이 PlayerId로 정한다).
-- 시즌 스탯은 여기서 주지 않는다 — 선수 대시보드와 같은 집계 경로(me/season-stats?playerId=)를 써야
-- 두 화면의 숫자가 어긋나지 않는다(공식만 집계 — Design.FriendlyMatch).
CREATE PROCEDURE [dbo].[UspGetSoccerPlayersByUser]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.[PlayerId], p.[Name], p.[AgeGroup], p.[PhotoUrl], p.[IsGuardianManaged],
        tp.[JerseyNumber], tp.[Position],
        t.[TeamName]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    LEFT JOIN [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        ON tp.[PlayerId] = p.[PlayerId] AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
    LEFT JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = tp.[TeamId] AND t.[DeletedAt] IS NULL
    WHERE p.[UserId] = @UserId AND p.[DeletedAt] IS NULL
    ORDER BY p.[CreatedAt];
END
