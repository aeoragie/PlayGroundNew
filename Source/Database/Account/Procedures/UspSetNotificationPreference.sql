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

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserId] = @UserId AND [DeletedAt] IS NULL)
    BEGIN
        UPDATE [dbo].[NotificationPreferences]
        SET [IsEnabled] = @IsEnabled, [UpdatedAt] = @Now
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
