-- 서명 URL 다운로드 소비 (Design.SettingsFlows ③). 토큰으로 원자 검증+증가+파일키 반환.
-- Ready + 미만료(ExpiresAt > now) + 횟수 < 상한 + 미삭제일 때만 DownloadCount를 올리고 StorageKey를 반환한다.
--   조건 미충족(만료·횟수 초과·잘못된 토큰)이면 빈 결과 → 엔드포인트 404/410. 토큰이 곧 자격(추측 불가).
CREATE PROCEDURE [dbo].[UspConsumeSoccerDataExportDownload]
    @Token VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    DECLARE @StorageKey VARCHAR(400) = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE [dbo].[SoccerDataExportRequests]
        SET @StorageKey = [StorageKey],
            [DownloadCount] = [DownloadCount] + 1,
            [UpdatedAt] = @Now
        WHERE [DownloadToken] = @Token
          AND [Status] = 'Ready'
          AND [DeletedAt] IS NULL
          AND [ExpiresAt] > @Now
          AND [DownloadCount] < [MaxDownloads];

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @StorageKey AS [StorageKey] WHERE @StorageKey IS NOT NULL;
END
