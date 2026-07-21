-- 계정 설정 조회 — 결과셋 2개: 사용자 행 → 연결된 소셜 계정.
-- 이메일 마스킹은 Persistence 매핑에서 (원본은 API 밖으로 내보내지 않는다).
CREATE PROCEDURE [dbo].[UspGetUserSettings]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.[UserId], u.[Email], u.[EmailConfirmed], u.[PasswordHash], u.[AuthProvider],
        u.[DisplayName], u.[ProfileImageUrl], u.[UserRole], u.[UserStatus],
        u.[CreatedAt], u.[UpdatedAt], u.[DeletedAt]
    FROM [dbo].[Users] u WITH (NOLOCK)
    WHERE u.[UserId] = @UserId AND u.[DeletedAt] IS NULL;

    SELECT
        s.[SocialAccountId], s.[UserId], s.[Provider], s.[ProviderUserId], s.[Email], s.[CreatedAt]
    FROM [dbo].[SocialAccounts] s WITH (NOLOCK)
    WHERE s.[UserId] = @UserId
    ORDER BY s.[CreatedAt];
END
