-- @entity: UserRecord
-- @source: join
-- @join: Users AS u (UserId, Email, EmailConfirmed, PasswordHash, AuthProvider, DisplayName, ProfileImageUrl, UserRole, UserStatus)
-- 계정 소프트 삭제 — DeletedAt 마킹 (로그인·조회 전부 차단됨). 자녀 프로필(FamilyLink) 이전은 후속 플로우.
-- 삭제된 행을 반환한다 — 호출측 감사 로그용. 이미 삭제됐거나 없으면 빈 결과.
CREATE PROCEDURE [dbo].[UspDeleteUser]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[Users]
    SET [DeletedAt] = GETUTCDATE(), [UpdatedAt] = GETUTCDATE(), [UserStatus] = 'Deleted'
    WHERE [UserId] = @UserId AND [DeletedAt] IS NULL;

    IF @@ROWCOUNT = 0
    BEGIN
        SELECT u.[UserId], u.[Email], u.[EmailConfirmed], u.[PasswordHash], u.[AuthProvider],
               u.[DisplayName], u.[ProfileImageUrl], u.[UserRole], u.[UserStatus]
        FROM [dbo].[Users] u WITH (NOLOCK)
        WHERE 1 = 0;
        RETURN;
    END

    SELECT u.[UserId], u.[Email], u.[EmailConfirmed], u.[PasswordHash], u.[AuthProvider],
           u.[DisplayName], u.[ProfileImageUrl], u.[UserRole], u.[UserStatus]
    FROM [dbo].[Users] u WITH (NOLOCK)
    WHERE u.[UserId] = @UserId;
END
