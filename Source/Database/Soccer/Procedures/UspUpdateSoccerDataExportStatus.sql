-- 데이터 내려받기 상태 전환 (백그라운드 잡 완료 시). Pending → Ready(파일·토큰·만료 세팅) 또는 Failed.
-- Pending일 때만 전환한다(취소된 요청·재실행 경합 방어). @Status='Ready'면 토큰·키·크기·만료를 함께 기록.
CREATE PROCEDURE [dbo].[UspUpdateSoccerDataExportStatus]
    @RequestId     UNIQUEIDENTIFIER,
    @Status        VARCHAR(20),
    @DownloadToken VARCHAR(64)  = NULL,
    @StorageKey    VARCHAR(400) = NULL,
    @SizeBytes     BIGINT       = NULL,
    @ExpiresAt     DATETIME2    = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE [dbo].[SoccerDataExportRequests]
    SET [Status] = @Status,
        [DownloadToken] = @DownloadToken,
        [StorageKey] = @StorageKey,
        [SizeBytes] = @SizeBytes,
        [ExpiresAt] = @ExpiresAt,
        [CompletedAt] = @Now,
        [UpdatedAt] = @Now
    WHERE [RequestId] = @RequestId AND [Status] = 'Pending' AND [DeletedAt] IS NULL;

    SELECT @@ROWCOUNT AS [Affected];
END
