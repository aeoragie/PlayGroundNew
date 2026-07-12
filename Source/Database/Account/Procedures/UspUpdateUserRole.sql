-- 사용자 역할 변경 (온보딩 완료 시 General → Player/TeamAdmin). 결과셋 없음.
CREATE PROCEDURE [dbo].[UspUpdateUserRole]
    @UserId UNIQUEIDENTIFIER,
    @Role VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[Users]
    SET [UserRole] = @Role, [UpdatedAt] = GETUTCDATE()
    WHERE [UserId] = @UserId AND [DeletedAt] IS NULL;
END
