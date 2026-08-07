-- 데이터 내려받기 요청 취소 (준비 중 상태 행의 "요청 취소"). Pending + 소유자일 때만 소프트 삭제.
-- Ready/Failed는 취소 대상이 아니다(이미 종료). 반환은 영향 행 수(성공 여부).
CREATE PROCEDURE [dbo].[UspCancelSoccerDataExportRequest]
    @UserId    UNIQUEIDENTIFIER,
    @RequestId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE [dbo].[SoccerDataExportRequests]
    SET [DeletedAt] = @Now, [UpdatedAt] = @Now
    WHERE [RequestId] = @RequestId AND [UserId] = @UserId
      AND [Status] = 'Pending' AND [DeletedAt] IS NULL;

    SELECT @@ROWCOUNT AS [Affected];
END
