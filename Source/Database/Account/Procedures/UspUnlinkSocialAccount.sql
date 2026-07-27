-- 로그인 수단 해제 (Design.SettingsFlows ②). **마지막 1개는 해제 불가** — SP가 원자적으로 강제한다.
-- 로그인 수단 수 = 소셜 계정 수 + (비밀번호 있으면 1). 해제 후 0이 되면 아무것도 하지 않고 'LastMeans' 반환.
-- 결과 상태(스칼라 문자열): 'Ok'(해제됨) / 'LastMeans'(유일 수단이라 거부) / 'NotLinked'(연결 안 됨).
CREATE PROCEDURE [dbo].[UspUnlinkSocialAccount]
    @UserId   UNIQUEIDENTIFIER,
    @Provider VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Status VARCHAR(20);

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Exists BIT = CASE WHEN EXISTS (
            SELECT 1 FROM [dbo].[SocialAccounts] WITH (UPDLOCK)
            WHERE [UserId] = @UserId AND [Provider] = @Provider) THEN 1 ELSE 0 END;

        DECLARE @Means INT =
            (SELECT COUNT(*) FROM [dbo].[SocialAccounts] WHERE [UserId] = @UserId)
            + (SELECT COUNT(*) FROM [dbo].[Users] WHERE [UserId] = @UserId AND [PasswordHash] IS NOT NULL AND [DeletedAt] IS NULL);

        IF @Exists = 0
        BEGIN
            SET @Status = 'NotLinked';
        END
        ELSE IF @Means <= 1
        BEGIN
            SET @Status = 'LastMeans';
        END
        ELSE
        BEGIN
            DELETE FROM [dbo].[SocialAccounts] WHERE [UserId] = @UserId AND [Provider] = @Provider;
            SET @Status = 'Ok';
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @Status AS [Status];
END
