-- @entity: SoccerPendingChildClaimRecord
-- @source: join
-- @join: SoccerPlayerClaimRequests AS r (RequestId, PlayerId, CreatedAt)
-- @join: SoccerPlayers AS p (Name, AgeGroup)
-- @join: SoccerTeams AS t (TeamName)
-- 허브 "내 자녀"의 **승인 대기(Pending) 자녀** — 내가 올린 미처리 연결 요청 전부 (Design.DashboardHub).
-- 이미 연결된(내 UserId로 소유한) 선수는 제외한다 — 연결됨 카드와 대기 카드가 중복되면 안 된다.
-- 같은 선수에 대한 요청이 여럿이면 최신 1건만(요청은 멱등이라 보통 1건이지만 방어).
CREATE PROCEDURE [dbo].[UspGetSoccerPendingChildClaimsByUser]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.[RequestId], r.[PlayerId], r.[CreatedAt],
        p.[Name], p.[AgeGroup],
        t.[TeamName]
    FROM [dbo].[SoccerPlayerClaimRequests] r WITH (NOLOCK)
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK) ON p.[PlayerId] = r.[PlayerId] AND p.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerTeams] t WITH (NOLOCK) ON t.[TeamId] = r.[TeamId] AND t.[DeletedAt] IS NULL
    WHERE r.[RequesterUserId] = @UserId
      AND r.[Status] = 'Pending'
      AND r.[DeletedAt] IS NULL
      -- 이미 이 계정에 연결된 선수는 대기 카드로 다시 그리지 않는다
      AND (p.[UserId] IS NULL OR p.[UserId] <> @UserId)
      -- 같은 선수 중복 요청이면 최신 1건만
      AND r.[CreatedAt] = (
          SELECT MAX(r2.[CreatedAt])
          FROM [dbo].[SoccerPlayerClaimRequests] r2 WITH (NOLOCK)
          WHERE r2.[PlayerId] = r.[PlayerId] AND r2.[RequesterUserId] = @UserId
            AND r2.[Status] = 'Pending' AND r2.[DeletedAt] IS NULL)
    ORDER BY r.[CreatedAt] DESC;
END
