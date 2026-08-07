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

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE [dbo].[Users]
    SET [DeletedAt] = @Now, [UpdatedAt] = @Now, [UserStatus] = 'Deleted'
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
