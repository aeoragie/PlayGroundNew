-- 이메일 회원가입. 생성 후 UserRecord 반환.
CREATE PROCEDURE [dbo].[UspCreateUserByEmail]
    @Email VARCHAR(255),
    @PasswordHash VARCHAR(255),
    @DisplayName NVARCHAR(100),
    @UserRole VARCHAR(20) = 'General'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO [dbo].[Users]
        ([UserId], [Email], [PasswordHash], [AuthProvider], [DisplayName], [UserRole])
    VALUES
        (@UserId, @Email, @PasswordHash, 'Local', @DisplayName, @UserRole);

    SELECT
        u.[UserId], u.[Email], u.[EmailConfirmed], u.[PasswordHash], u.[AuthProvider],
        u.[DisplayName], u.[ProfileImageUrl], u.[UserRole], u.[UserStatus]
    FROM [dbo].[Users] u WITH (NOLOCK)
    WHERE u.[UserId] = @UserId;
END
