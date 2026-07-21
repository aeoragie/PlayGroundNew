-- @entity: NotificationPreferenceSetRecord
-- @source: join
-- @join: Users AS u (UserId)
-- 알림 설정 업서트. 항목 화이트리스트 검증은 Application(enum) — 승인형은 enum에 없어 저장 자체가 불가.
-- 사용자 미존재 시 빈 결과.
CREATE PROCEDURE [dbo].[UspSetNotificationPreference]
    @UserId UNIQUEIDENTIFIER,
    @ItemName VARCHAR(30),
    @IsEnabled BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserId] = @UserId AND [DeletedAt] IS NULL)
    BEGIN
        UPDATE [dbo].[NotificationPreferences]
        SET [IsEnabled] = @IsEnabled, [UpdatedAt] = GETUTCDATE()
        WHERE [UserId] = @UserId AND [ItemName] = @ItemName;

        IF @@ROWCOUNT = 0
        BEGIN
            INSERT INTO [dbo].[NotificationPreferences] ([UserId], [ItemName], [IsEnabled])
            VALUES (@UserId, @ItemName, @IsEnabled);
        END

        SELECT u.[UserId]
        FROM [dbo].[Users] u WITH (NOLOCK)
        WHERE u.[UserId] = @UserId;
    END
    ELSE
    BEGIN
        SELECT u.[UserId]
        FROM [dbo].[Users] u WITH (NOLOCK)
        WHERE 1 = 0;
    END
END
