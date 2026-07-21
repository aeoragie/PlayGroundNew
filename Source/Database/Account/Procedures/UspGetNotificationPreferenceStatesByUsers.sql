-- @entity: NotificationPreferenceStateRecord
-- @source: join
-- @join: NotificationPreferences AS p (UserId, IsEnabled)
-- 여러 사용자의 특정 알림 항목 저장값 벌크 조회 — 친선경기 결과 발송 전 수신 설정 필터용.
-- **저장 행이 있는 사용자만 반환한다** — 반환되지 않은 사용자는 호출측(Application)이 enum 기본값을 적용.
CREATE PROCEDURE [dbo].[UspGetNotificationPreferenceStatesByUsers]
    @UserIdsJson VARCHAR(MAX),
    @ItemName VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.[UserId], p.[IsEnabled]
    FROM OPENJSON(@UserIdsJson) WITH ([UserId] UNIQUEIDENTIFIER '$') u
    JOIN [dbo].[NotificationPreferences] p WITH (NOLOCK)
        ON p.[UserId] = u.[UserId] AND p.[ItemName] = @ItemName;
END
