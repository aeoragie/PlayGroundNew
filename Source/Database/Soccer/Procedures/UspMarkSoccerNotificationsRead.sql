-- 알림 여러 건 읽음 처리 (알림 센터 페이지 진입 시 — 화면에 보인 알림을 한 번에 읽음).
-- 본인 소유 + 미읽음만 갱신한다. 액션형도 읽음이 되지만 소멸은 라이브 상태(처리)로 결정되므로 표시엔 무해하다.
-- @IdsJson = ["guid", ...]. 반환은 갱신 건수.
CREATE PROCEDURE [dbo].[UspMarkSoccerNotificationsRead]
    @UserId  UNIQUEIDENTIFIER,
    @IdsJson VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE n
    SET n.[IsRead] = 1, n.[ReadAt] = GETUTCDATE()
    FROM [dbo].[SoccerNotifications] n
    JOIN OPENJSON(@IdsJson) WITH ([Id] UNIQUEIDENTIFIER '$') j ON j.[Id] = n.[NotificationId]
    WHERE n.[RecipientUserId] = @UserId AND n.[IsRead] = 0;

    SELECT @@ROWCOUNT AS [Affected];
END
