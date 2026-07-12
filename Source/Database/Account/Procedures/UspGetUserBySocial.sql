-- 소셜 연동으로 사용자 조회 (Provider + ProviderUserId). 결과 컬럼은 UserRecord와 동일.
CREATE PROCEDURE [dbo].[UspGetUserBySocial]
    @Provider VARCHAR(20),
    @ProviderUserId VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.[UserId], u.[Email], u.[EmailConfirmed], u.[PasswordHash], u.[AuthProvider],
        u.[DisplayName], u.[ProfileImageUrl], u.[UserRole], u.[UserStatus]
    FROM [dbo].[SocialAccounts] sa WITH (NOLOCK)
    INNER JOIN [dbo].[Users] u WITH (NOLOCK) ON sa.[UserId] = u.[UserId]
    WHERE sa.[Provider] = @Provider AND sa.[ProviderUserId] = @ProviderUserId AND u.[DeletedAt] IS NULL;
END
