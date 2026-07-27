-- 데이터 내려받기 요청 취소 (준비 중 상태 행의 "요청 취소"). Pending + 소유자일 때만 소프트 삭제.
-- Ready/Failed는 취소 대상이 아니다(이미 종료). 반환은 영향 행 수(성공 여부).
CREATE PROCEDURE [dbo].[UspCancelSoccerDataExportRequest]
    @UserId    UNIQUEIDENTIFIER,
    @RequestId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[SoccerDataExportRequests]
    SET [DeletedAt] = GETUTCDATE(), [UpdatedAt] = GETUTCDATE()
    WHERE [RequestId] = @RequestId AND [UserId] = @UserId
      AND [Status] = 'Pending' AND [DeletedAt] IS NULL;

    SELECT @@ROWCOUNT AS [Affected];
END
