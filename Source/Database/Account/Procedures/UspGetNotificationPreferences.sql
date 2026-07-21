-- 알림 설정 조회 — 저장된 행만 (기본값 병합은 Persistence 매핑에서).
CREATE PROCEDURE [dbo].[UspGetNotificationPreferences]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.[PreferenceId], p.[UserId], p.[ItemName], p.[IsEnabled], p.[CreatedAt], p.[UpdatedAt]
    FROM [dbo].[NotificationPreferences] p WITH (NOLOCK)
    WHERE p.[UserId] = @UserId;
END
