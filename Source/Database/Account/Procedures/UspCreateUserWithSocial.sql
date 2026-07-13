-- 소셜 회원가입: User + SocialAccount 동시 생성(트랜잭션). 생성 후 UserRecord 반환.
CREATE PROCEDURE [dbo].[UspCreateUserWithSocial]
    @Email VARCHAR(255),
    @DisplayName VARCHAR(300),
    @Provider VARCHAR(20),
    @ProviderUserId VARCHAR(255),
    @ProfileImageUrl VARCHAR(2048) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[Users]
            ([UserId], [Email], [EmailConfirmed], [AuthProvider], [DisplayName], [ProfileImageUrl], [UserRole])
        VALUES
            (@UserId, @Email, 1, @Provider, @DisplayName, @ProfileImageUrl, 'General');

        INSERT INTO [dbo].[SocialAccounts]
            ([UserId], [Provider], [ProviderUserId], [Email])
        VALUES
            (@UserId, @Provider, @ProviderUserId, @Email);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT
        u.[UserId], u.[Email], u.[EmailConfirmed], u.[PasswordHash], u.[AuthProvider],
        u.[DisplayName], u.[ProfileImageUrl], u.[UserRole], u.[UserStatus]
    FROM [dbo].[Users] u WITH (NOLOCK)
    WHERE u.[UserId] = @UserId;
END
