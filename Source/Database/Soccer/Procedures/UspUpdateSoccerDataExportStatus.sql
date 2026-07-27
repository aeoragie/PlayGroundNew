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

    UPDATE [dbo].[SoccerDataExportRequests]
    SET [Status] = @Status,
        [DownloadToken] = @DownloadToken,
        [StorageKey] = @StorageKey,
        [SizeBytes] = @SizeBytes,
        [ExpiresAt] = @ExpiresAt,
        [CompletedAt] = GETUTCDATE(),
        [UpdatedAt] = GETUTCDATE()
    WHERE [RequestId] = @RequestId AND [Status] = 'Pending' AND [DeletedAt] IS NULL;

    SELECT @@ROWCOUNT AS [Affected];
END
