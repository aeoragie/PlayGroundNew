-- 알림 여러 건 읽음 처리 (알림 센터 페이지 진입 시 — 화면에 보인 알림을 한 번에 읽음).
-- 본인 소유 + 미읽음만 갱신한다. 액션형도 읽음이 되지만 소멸은 라이브 상태(처리)로 결정되므로 표시엔 무해하다.
-- @IdsJson = ["guid", ...]. 반환은 갱신 건수.
CREATE PROCEDURE [dbo].[UspMarkSoccerNotificationsRead]
    @UserId  UNIQUEIDENTIFIER,
    @IdsJson VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE n
    SET n.[IsRead] = 1, n.[ReadAt] = @Now
    FROM [dbo].[SoccerNotifications] n
    JOIN OPENJSON(@IdsJson) WITH ([Id] UNIQUEIDENTIFIER '$') j ON j.[Id] = n.[NotificationId]
    WHERE n.[RecipientUserId] = @UserId AND n.[IsRead] = 0;

    SELECT @@ROWCOUNT AS [Affected];
END
