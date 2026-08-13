-- PG 승인 결과 반영. Pending이면서 소유자가 일치하는 행만 Approved로 바꾼다.
-- 조건 불일치(이미 승인됨·남의 주문·미존재)는 빈 결과셋. 중복 승인 방어가 이 조건의 목적이다.
-- 결과 모양은 UspCreatePayment의 PaymentRecord와 동일.
CREATE PROCEDURE [dbo].[UspApprovePayment]
    @UserId UNIQUEIDENTIFIER,
    @OrderId VARCHAR(64),
    @PaymentKey VARCHAR(200),
    @Method VARCHAR(60),
    @ApprovedAt DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();
    DECLARE @Applied INT = 0;

    UPDATE [dbo].[Payments]
    SET [Status] = 'Approved', [PaymentKey] = @PaymentKey, [Method] = @Method,
        [ApprovedAt] = @ApprovedAt, [UpdatedAt] = @Now
    WHERE [OrderId] = @OrderId AND [UserId] = @UserId
      AND [Status] = 'Pending' AND [DeletedAt] IS NULL;

    SET @Applied = @@ROWCOUNT;

    SELECT
        p.[PaymentId], p.[Sport], p.[OrderId], p.[OrderName], p.[Amount], p.[Currency],
        p.[Status], p.[PgProvider], p.[PaymentKey], p.[Method], p.[ApprovedAt], p.[FailReason], p.[CreatedAt]
    FROM [dbo].[Payments] p WITH (NOLOCK)
    WHERE p.[OrderId] = @OrderId AND @Applied = 1;
END
