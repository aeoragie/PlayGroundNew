-- 현재 데이터 내려받기 상태 (설정 계정 관리 절의 상태 행). 최신 비삭제 요청 1건.
-- 만료 판정(Ready인데 ExpiresAt 경과)은 Persistence가 한다 — 만료면 상태 행 대신 "요청" 버튼으로 되돌린다.
-- SoccerDataExportRequestsEntity 그대로 반환.
CREATE PROCEDURE [dbo].[UspGetSoccerDataExportByUser]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        [RequestId], [UserId], [Status], [IncludeProfile], [IncludeRecords], [IncludeRequests],
        [DownloadToken], [StorageKey], [SizeBytes], [ExpiresAt], [DownloadCount], [MaxDownloads],
        [CreatedAt], [UpdatedAt], [CompletedAt], [DeletedAt]
    FROM [dbo].[SoccerDataExportRequests] WITH (NOLOCK)
    WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
    ORDER BY [CreatedAt] DESC;
END
