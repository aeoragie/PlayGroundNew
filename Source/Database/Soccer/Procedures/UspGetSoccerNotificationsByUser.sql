-- @entity: SoccerNotificationRecord
-- @source: join
-- @join: SoccerNotifications AS n (NotificationId, NotificationType, RefId, TargetPlayerId, ActorName, PlayerName, TeamName, MetaText, SubText, Relation, IsRead, CreatedAt)
-- @join: SoccerPlayerClaimRequests AS r (Status)
-- 알림 목록(벨 패널) — 결과셋 2개: ⓪미읽음 카운트 → ①최근 50건.
-- 지연 생성·보관 90일 정리는 UspSyncSoccerNotifications가 담당(페이지 조회와 공유 — 단일 진실).
-- 액션형(ClaimRequest·RosterInvite)의 처리 여부는 스냅샷이 아니라 라이브 상태를 조인한다(Status).
--   ClaimRequest는 요청 상태, RosterInvite는 SoccerApplications.ConfirmedAt로 파생(있으면 'Confirmed', 없으면 'Pending').
CREATE PROCEDURE [dbo].[UspGetSoccerNotificationsByUser]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    EXEC [dbo].[UspSyncSoccerNotifications] @UserId;

    --.// ⓪ 미읽음 카운트 (벨 뱃지 — 목록 50건 컷과 무관한 전체 수)
    SELECT COUNT(*) AS [UnreadCount]
    FROM [dbo].[SoccerNotifications] WITH (NOLOCK)
    WHERE [RecipientUserId] = @UserId AND [IsRead] = 0;

    --.// ① 목록 (최근 50건)
    SELECT TOP 50
        n.[NotificationId], n.[NotificationType], n.[RefId], n.[TargetPlayerId],
        n.[ActorName], n.[PlayerName], n.[TeamName], n.[MetaText], n.[SubText], n.[Relation],
        n.[IsRead], n.[CreatedAt],
        COALESCE(
            r.[Status],
            CASE WHEN app.[ApplicationId] IS NOT NULL
                 THEN CASE WHEN app.[ConfirmedAt] IS NOT NULL THEN 'Confirmed' ELSE 'Pending' END
            END) AS [Status]
    FROM [dbo].[SoccerNotifications] n WITH (NOLOCK)
    LEFT JOIN [dbo].[SoccerPlayerClaimRequests] r WITH (NOLOCK)
        ON n.[NotificationType] = 'ClaimRequest' AND r.[RequestId] = n.[RefId]
    LEFT JOIN [dbo].[SoccerApplications] app WITH (NOLOCK)
        ON n.[NotificationType] = 'RosterInvite' AND app.[ApplicationId] = n.[RefId]
    WHERE n.[RecipientUserId] = @UserId
    ORDER BY n.[CreatedAt] DESC;
END
