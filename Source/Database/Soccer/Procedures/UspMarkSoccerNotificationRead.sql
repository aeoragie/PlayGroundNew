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

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE [dbo].[SoccerNotifications]
    SET [IsRead] = 1, [ReadAt] = @Now
    WHERE [NotificationId] = @NotificationId AND [RecipientUserId] = @UserId AND [IsRead] = 0;

    SELECT n.[NotificationId]
    FROM [dbo].[SoccerNotifications] n WITH (NOLOCK)
    WHERE n.[NotificationId] = @NotificationId AND n.[RecipientUserId] = @UserId AND n.[IsRead] = 1;
END
