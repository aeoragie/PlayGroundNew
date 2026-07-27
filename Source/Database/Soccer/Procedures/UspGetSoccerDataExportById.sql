-- 데이터 내려받기 요청 단건 조회 (백그라운드 잡이 처리 대상 상세를 읽는다 — UserId·포함 항목).
-- 취소(소프트 삭제)된 요청은 제외 — 잡이 이미 취소된 요청을 만들지 않게.
CREATE PROCEDURE [dbo].[UspGetSoccerDataExportById]
    @RequestId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [RequestId], [UserId], [Status], [IncludeProfile], [IncludeRecords], [IncludeRequests],
        [DownloadToken], [StorageKey], [SizeBytes], [ExpiresAt], [DownloadCount], [MaxDownloads],
        [CreatedAt], [UpdatedAt], [CompletedAt], [DeletedAt]
    FROM [dbo].[SoccerDataExportRequests] WITH (NOLOCK)
    WHERE [RequestId] = @RequestId AND [DeletedAt] IS NULL;
END
