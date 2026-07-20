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

    UPDATE [dbo].[SoccerRecordCorrections]
    SET [DeletedAt] = GETUTCDATE(), [UpdatedAt] = GETUTCDATE()
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
