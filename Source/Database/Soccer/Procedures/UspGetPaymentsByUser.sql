-- 내 결제 내역 최근순 (테스트 플로우 화면용 TOP 50, 페이징은 실화면 설계 때).
-- 결과 모양은 UspCreatePayment의 PaymentRecord와 동일.
CREATE PROCEDURE [dbo].[UspGetPaymentsByUser]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 50
        p.[PaymentId], p.[Sport], p.[OrderId], p.[OrderName], p.[Amount], p.[Currency],
        p.[Status], p.[PgProvider], p.[PaymentKey], p.[Method], p.[ApprovedAt], p.[FailReason], p.[CreatedAt]
    FROM [dbo].[Payments] p WITH (NOLOCK)
    WHERE p.[UserId] = @UserId AND p.[DeletedAt] IS NULL
    ORDER BY p.[CreatedAt] DESC;
END
