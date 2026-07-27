-- 계정 설정 조회 — 결과셋 4개: 사용자 행 → 연결된 소셜 계정 → 최근 30일 이름 변경 횟수 → 가장 오래된 변경 시각.
-- 이메일 마스킹은 Persistence 매핑에서 (원본은 API 밖으로 내보내지 않는다).
-- RS3·RS4(스칼라)로 제한 근거를 내린다: 횟수 2 이상이면 "다음 변경 가능" 날짜 = 가장 오래된 변경 + 30일.
--   스칼라 2개로 나눈 이유 — 계산 컬럼 전용 Record 없이 Persistence가 int·DateTime?로 직접 읽는다.
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

    SELECT COUNT(*)
    FROM [dbo].[UserNameChangeLogs] WITH (NOLOCK)
    WHERE [UserId] = @UserId AND [ChangedAt] >= DATEADD(DAY, -30, GETUTCDATE());

    SELECT MIN([ChangedAt])
    FROM [dbo].[UserNameChangeLogs] WITH (NOLOCK)
    WHERE [UserId] = @UserId AND [ChangedAt] >= DATEADD(DAY, -30, GETUTCDATE());
END
