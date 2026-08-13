-- @entity: PaymentRecord
-- @source: join
-- @join: Payments AS p (PaymentId, Sport, OrderId, OrderName, Amount, Currency, Status, PgProvider, PaymentKey, Method, ApprovedAt, FailReason, CreatedAt)
-- 결제 주문 생성. Pending으로 만들고 생성된 행을 반환한다.
-- OrderId는 앱이 만든 GUID N 포맷이라 충돌하지 않는다 (유니크 제약은 방어선).
CREATE PROCEDURE [dbo].[UspCreatePayment]
    @UserId UNIQUEIDENTIFIER,
    @Sport VARCHAR(20),
    @OrderId VARCHAR(64),
    @OrderName VARCHAR(300),
    @Amount INT,
    @Currency VARCHAR(10),
    @PgProvider VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PaymentId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO [dbo].[Payments]
        ([PaymentId], [UserId], [Sport], [OrderId], [OrderName], [Amount], [Currency], [PgProvider])
    VALUES (@PaymentId, @UserId, @Sport, @OrderId, @OrderName, @Amount, @Currency, @PgProvider);

    SELECT
        p.[PaymentId], p.[Sport], p.[OrderId], p.[OrderName], p.[Amount], p.[Currency],
        p.[Status], p.[PgProvider], p.[PaymentKey], p.[Method], p.[ApprovedAt], p.[FailReason], p.[CreatedAt]
    FROM [dbo].[Payments] p WITH (NOLOCK)
    WHERE p.[PaymentId] = @PaymentId;
END
