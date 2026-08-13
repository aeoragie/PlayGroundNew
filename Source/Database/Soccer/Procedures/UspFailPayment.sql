-- 결제 실패 기록. Pending이면서 소유자가 일치하는 행만 Failed로 바꾼다.
-- failUrl 복귀와 PG 승인 실패 두 경로가 공용한다. 조건 불일치는 빈 결과셋.
-- 결과 모양은 UspCreatePayment의 PaymentRecord와 동일.
CREATE PROCEDURE [dbo].[UspFailPayment]
    @UserId UNIQUEIDENTIFIER,
    @OrderId VARCHAR(64),
    @FailReason VARCHAR(600)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();
    DECLARE @Applied INT = 0;

    UPDATE [dbo].[Payments]
    SET [Status] = 'Failed', [FailReason] = @FailReason, [UpdatedAt] = @Now
    WHERE [OrderId] = @OrderId AND [UserId] = @UserId
      AND [Status] = 'Pending' AND [DeletedAt] IS NULL;

    SET @Applied = @@ROWCOUNT;

    SELECT
        p.[PaymentId], p.[Sport], p.[OrderId], p.[OrderName], p.[Amount], p.[Currency],
        p.[Status], p.[PgProvider], p.[PaymentKey], p.[Method], p.[ApprovedAt], p.[FailReason], p.[CreatedAt]
    FROM [dbo].[Payments] p WITH (NOLOCK)
    WHERE p.[OrderId] = @OrderId AND @Applied = 1;
END
