-- @entity: SoccerPendingInviteRecord
-- @source: join
-- @join: SoccerPlayerInvites AS i (InviteId, TeamId, PlayerId, Code, CreatedAt, ExpiresAt)
-- @join: SoccerPlayers AS p (Name)
-- @join: SoccerTeams AS t (TeamName)
-- 내가 관리하는 팀들의 **아직 연결되지 않은 초대** 전부.
--
-- "처리가 필요해요"(Design.DashboardHub)의 데이터 원천이다. 알림 테이블을 두지 않고
-- **현재 상태에서 파생**한다 — 알림을 쓰는 주체(생산자)가 없는 기능들이라 이벤트 로그를 만들면
-- 영원히 비어 있고, 파생은 항상 정확하며 동기화가 어긋날 일이 없다.
-- 대신 "읽음" 상태가 없다 — 처리하면 목록에서 사라지는 것이 곧 읽음 처리다.
--
-- 문구("미처리 연결 요청 2건")는 클라이언트가 조립한다 — 여기서는 사실만 준다.
CREATE PROCEDURE [dbo].[UspGetSoccerPendingInvitesByManager]
    @ManagerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        i.[InviteId], i.[TeamId], i.[PlayerId], i.[Code], i.[CreatedAt], i.[ExpiresAt],
        p.[Name],
        t.[TeamName]
    FROM [dbo].[SoccerPlayerInvites] i WITH (NOLOCK)
    INNER JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = i.[TeamId] AND t.[DeletedAt] IS NULL
    LEFT JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK)
        ON p.[PlayerId] = i.[PlayerId] AND p.[DeletedAt] IS NULL
    -- SoccerPlayerInvites에는 DeletedAt이 없다 — 폐기는 Status('Revoked','Expired')로 표현한다
    WHERE t.[ManagerUserId] = @ManagerUserId
      AND i.[Status] = 'Pending'
      AND (i.[ExpiresAt] IS NULL OR i.[ExpiresAt] > GETUTCDATE())
    ORDER BY i.[CreatedAt] DESC;
END
