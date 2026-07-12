-- @entity: UserRecord
-- @source: join
-- @join: Users AS u (UserId, Email, EmailConfirmed, PasswordHash, AuthProvider, DisplayName, ProfileImageUrl, UserRole, UserStatus)
-- 이메일로 사용자 조회 (로그인·중복 확인). 삭제된 계정 제외.
CREATE PROCEDURE [dbo].[UspGetUserByEmail]
    @Email VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.[UserId], u.[Email], u.[EmailConfirmed], u.[PasswordHash], u.[AuthProvider],
        u.[DisplayName], u.[ProfileImageUrl], u.[UserRole], u.[UserStatus]
    FROM [dbo].[Users] u WITH (NOLOCK)
    WHERE u.[Email] = @Email AND u.[DeletedAt] IS NULL;
END
