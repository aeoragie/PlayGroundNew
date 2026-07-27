-- @entity: UserRecord
-- @source: join
-- @join: Users AS u (UserId, Email, EmailConfirmed, PasswordHash, AuthProvider, DisplayName, ProfileImageUrl, UserRole, UserStatus)
-- 이름(DisplayName) 변경 (Design.SettingsFlows ①). 검증은 Application(한글 2~10/영문 2~20·특수문자·숫자 불가),
-- 여기서는 **30일 2회 제한을 원자적으로 판정**하고 변경+로그를 한 트랜잭션으로 처리한다.
-- 최근 30일 로그가 2건 이상이면 아무것도 하지 않고 빈 결과 → 호출측이 거부로 변환(제한 초과).
-- 동명이인 허용(중복 검사 없음 — 실명 서비스). 성공 시 갱신된 UserRecord 반환(호출측 JWT 재발급용).
CREATE PROCEDURE [dbo].[UspUpdateUserDisplayName]
    @UserId  UNIQUEIDENTIFIER,
    @NewName VARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Applied INT = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @PreviousName VARCHAR(300) = (
            SELECT [DisplayName] FROM [dbo].[Users] WITH (UPDLOCK)
            WHERE [UserId] = @UserId AND [DeletedAt] IS NULL);

        DECLARE @RecentCount INT = (
            SELECT COUNT(*) FROM [dbo].[UserNameChangeLogs]
            WHERE [UserId] = @UserId AND [ChangedAt] >= DATEADD(DAY, -30, GETUTCDATE()));

        -- 존재 + 제한 미초과 + 실제 변경일 때만 반영
        IF @PreviousName IS NOT NULL AND @RecentCount < 2 AND @PreviousName <> @NewName
        BEGIN
            UPDATE [dbo].[Users]
            SET [DisplayName] = @NewName, [UpdatedAt] = GETUTCDATE()
            WHERE [UserId] = @UserId AND [DeletedAt] IS NULL;

            INSERT INTO [dbo].[UserNameChangeLogs] ([UserId], [PreviousName], [NewName])
            VALUES (@UserId, @PreviousName, @NewName);

            SET @Applied = 1;
        END

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
    WHERE u.[UserId] = @UserId AND @Applied = 1;
END
