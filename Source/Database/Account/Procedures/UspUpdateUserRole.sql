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

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE [dbo].[Users]
    SET [UserRole] = @Role, [UpdatedAt] = @Now
    WHERE [UserId] = @UserId AND [DeletedAt] IS NULL;

    SELECT
        u.[UserId], u.[Email], u.[EmailConfirmed], u.[PasswordHash], u.[AuthProvider],
        u.[DisplayName], u.[ProfileImageUrl], u.[UserRole], u.[UserStatus]
    FROM [dbo].[Users] u WITH (NOLOCK)
    WHERE u.[UserId] = @UserId AND u.[DeletedAt] IS NULL;
END
