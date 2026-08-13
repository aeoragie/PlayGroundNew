-- 주문 키로 내 결제 단건 조회 (승인 전 금액 대조용).
-- 남의 주문·미존재는 빈 결과셋 (존재 여부 미노출). 결과 모양은 UspCreatePayment의 PaymentRecord와 동일.
CREATE PROCEDURE [dbo].[UspGetPaymentByOrder]
    @UserId UNIQUEIDENTIFIER,
    @OrderId VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.[PaymentId], p.[Sport], p.[OrderId], p.[OrderName], p.[Amount], p.[Currency],
        p.[Status], p.[PgProvider], p.[PaymentKey], p.[Method], p.[ApprovedAt], p.[FailReason], p.[CreatedAt]
    FROM [dbo].[Payments] p WITH (NOLOCK)
    WHERE p.[OrderId] = @OrderId AND p.[UserId] = @UserId AND p.[DeletedAt] IS NULL;
END
