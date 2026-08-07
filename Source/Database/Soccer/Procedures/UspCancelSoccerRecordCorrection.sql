-- @entity: SoccerCorrectionCancelRecord
-- @source: join
-- @join: SoccerRecordCorrections AS c (CorrectionId, DeletedAt)
-- 기록 수정 신청 취소 (소프트 삭제). 본인이 올린 **접수(Pending) 상태**만 취소할 수 있다 —
-- 주최측이 이미 심사한 건(Accepted/Rejected)은 손대지 않는다.
-- 권한 없음·대상 없음·이미 심사됨을 구분하지 않고 전부 빈 결과셋으로 응답한다.
CREATE PROCEDURE [dbo].[UspCancelSoccerRecordCorrection]
    @ManagerUserId UNIQUEIDENTIFIER,
    @CorrectionId  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE [dbo].[SoccerRecordCorrections]
    SET [DeletedAt] = @Now, [UpdatedAt] = @Now
    WHERE [CorrectionId] = @CorrectionId
      AND [RequestedByUserId] = @ManagerUserId
      AND [Status] = 'Pending'
      AND [DeletedAt] IS NULL;

    IF @@ROWCOUNT = 0
    BEGIN
        SELECT c.[CorrectionId], c.[DeletedAt]
        FROM [dbo].[SoccerRecordCorrections] c WITH (NOLOCK) WHERE 1 = 0;
        RETURN;
    END

    SELECT c.[CorrectionId], c.[DeletedAt]
    FROM [dbo].[SoccerRecordCorrections] c WITH (NOLOCK)
    WHERE c.[CorrectionId] = @CorrectionId;
END
