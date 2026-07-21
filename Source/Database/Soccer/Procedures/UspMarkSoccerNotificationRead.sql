-- @entity: SoccerNotificationReadRecord
-- @source: join
-- @join: SoccerNotifications AS n (NotificationId)
-- 알림 읽음 처리 — 본인 것만. 남의 알림·미존재는 빈 결과.
CREATE PROCEDURE [dbo].[UspMarkSoccerNotificationRead]
    @UserId UNIQUEIDENTIFIER,
    @NotificationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[SoccerNotifications]
    SET [IsRead] = 1, [ReadAt] = GETUTCDATE()
    WHERE [NotificationId] = @NotificationId AND [RecipientUserId] = @UserId AND [IsRead] = 0;

    SELECT n.[NotificationId]
    FROM [dbo].[SoccerNotifications] n WITH (NOLOCK)
    WHERE n.[NotificationId] = @NotificationId AND n.[RecipientUserId] = @UserId AND n.[IsRead] = 1;
END
