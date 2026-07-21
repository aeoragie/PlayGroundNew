-- @entity: SoccerClaimRequestOwnRecord
-- @source: join
-- @join: SoccerPlayerClaimRequests AS r (RequestId, Relation, Status, CreatedAt)
-- @join: SoccerPlayers AS p (Name)
-- @join: SoccerTeams AS t (TeamName)
-- /claim 재방문 복원 — 내 최신 요청 1건 (Pending → 대기 화면 / Approved → 완료 화면).
CREATE PROCEDURE [dbo].[UspGetSoccerPlayerClaimRequestByUser]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 r.[RequestId], r.[Relation], r.[Status], r.[CreatedAt], p.[Name], t.[TeamName]
    FROM [dbo].[SoccerPlayerClaimRequests] r WITH (NOLOCK)
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK) ON p.[PlayerId] = r.[PlayerId]
    JOIN [dbo].[SoccerTeams] t WITH (NOLOCK) ON t.[TeamId] = r.[TeamId]
    WHERE r.[RequesterUserId] = @UserId AND r.[DeletedAt] IS NULL
    ORDER BY r.[CreatedAt] DESC;
END
