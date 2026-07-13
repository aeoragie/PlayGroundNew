-- @entity: UserRecord
-- @source: join
-- @join: Users AS u (UserId, Email, EmailConfirmed, PasswordHash, AuthProvider, DisplayName, ProfileImageUrl, UserRole, UserStatus)
-- 사용자 역할 변경 (온보딩 완료 시 General → Player/TeamAdmin).
-- 갱신된 사용자 행을 반환한다 — 호출측이 승격된 역할로 JWT를 재발급하는 데 사용.
CREATE PROCEDURE [dbo].[UspUpdateUserRole]
    @UserId UNIQUEIDENTIFIER,
    @Role VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[Users]
    SET [UserRole] = @Role, [UpdatedAt] = GETUTCDATE()
    WHERE [UserId] = @UserId AND [DeletedAt] IS NULL;

    SELECT
        u.[UserId], u.[Email], u.[EmailConfirmed], u.[PasswordHash], u.[AuthProvider],
        u.[DisplayName], u.[ProfileImageUrl], u.[UserRole], u.[UserStatus]
    FROM [dbo].[Users] u WITH (NOLOCK)
    WHERE u.[UserId] = @UserId AND u.[DeletedAt] IS NULL;
END
