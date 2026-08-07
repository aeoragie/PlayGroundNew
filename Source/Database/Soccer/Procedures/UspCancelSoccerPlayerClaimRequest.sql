-- @entity: SoccerClaimCancelRecord
-- @source: join
-- @join: SoccerPlayerClaimRequests AS r (RequestId, DeletedAt)
-- 연결 요청 취소 — 요청자 본인의 미처리(Pending) 요청만 소프트 삭제 (Design.ClaimFlow 대기 화면 P1).
-- 코드는 소진하지 않는다(코드 요청은 승인 시점에만 소진) — 취소 후 다시 요청할 수 있다.
-- 팀 관리자의 해당 액션형 알림(ClaimRequest)은 읽음 처리해 대기 목록에서 치운다.
-- 소유·상태 불일치는 빈 결과(존재 여부 미노출 → Command가 Forbidden).
CREATE PROCEDURE [dbo].[UspCancelSoccerPlayerClaimRequest]
    @UserId UNIQUEIDENTIFIER,
    @RequestId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE [dbo].[SoccerPlayerClaimRequests]
        SET [DeletedAt] = @Now, [UpdatedAt] = @Now
        WHERE [RequestId] = @RequestId AND [RequesterUserId] = @UserId
          AND [Status] = 'Pending' AND [DeletedAt] IS NULL;

        DECLARE @Applied INT = @@ROWCOUNT;

        IF @Applied = 1
        BEGIN
            -- 관리자 쪽 미처리 연결 요청 알림을 읽음으로 (취소됐으니 처리할 게 없다)
            UPDATE [dbo].[SoccerNotifications]
            SET [IsRead] = 1, [ReadAt] = @Now
            WHERE [NotificationType] = 'ClaimRequest' AND [RefId] = @RequestId AND [IsRead] = 0;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    -- 적용된 요청 행을 돌려준다(없으면 빈 결과 → Forbidden)
    SELECT r.[RequestId], r.[DeletedAt]
    FROM [dbo].[SoccerPlayerClaimRequests] r WITH (NOLOCK)
    WHERE r.[RequestId] = @RequestId AND @Applied = 1;
END
