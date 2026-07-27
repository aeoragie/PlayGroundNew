-- 로그인 수단 연결 (Design.SettingsFlows ②). OAuth 콜백이 현재 로그인 사용자에 소셜을 붙인다.
-- 결과 상태(스칼라 문자열): 'Ok'(연결됨) / 'AlreadyLinked'(이미 내 계정에 연결 — 멱등 성공) /
--   'Conflict'(다른 계정에 이미 연결 — 인라인 오류 "이미 다른 계정에 연결된 …").
-- 소셜 신원(Provider, ProviderUserId)은 전역 유일(UQ_SocialAccounts_Provider) — 그래서 소유 계정을 먼저 본다.
CREATE PROCEDURE [dbo].[UspLinkSocialAccount]
    @UserId         UNIQUEIDENTIFIER,
    @Provider       VARCHAR(20),
    @ProviderUserId VARCHAR(255),
    @Email          VARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Status VARCHAR(20);

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @OwnerUserId UNIQUEIDENTIFIER = (
            SELECT TOP 1 [UserId] FROM [dbo].[SocialAccounts] WITH (UPDLOCK)
            WHERE [Provider] = @Provider AND [ProviderUserId] = @ProviderUserId);

        IF @OwnerUserId IS NULL
        BEGIN
            INSERT INTO [dbo].[SocialAccounts] ([UserId], [Provider], [ProviderUserId], [Email])
            VALUES (@UserId, @Provider, @ProviderUserId, @Email);
            SET @Status = 'Ok';
        END
        ELSE IF @OwnerUserId = @UserId
        BEGIN
            SET @Status = 'AlreadyLinked';
        END
        ELSE
        BEGIN
            SET @Status = 'Conflict';
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @Status AS [Status];
END
