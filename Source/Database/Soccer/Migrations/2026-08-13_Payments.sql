-- 결제 원장 (토스페이먼츠 테스트 플로우 골격). 신규 테이블. 멱등. **다른 PC 필수 실행.**
IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Payments]
    (
        [PaymentId]   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [UserId]      UNIQUEIDENTIFIER NOT NULL,
        [Sport]       VARCHAR(20)      NOT NULL,
        [OrderId]     VARCHAR(64)      NOT NULL,
        [OrderName]   VARCHAR(300)     NOT NULL,
        [Amount]      INT              NOT NULL,
        [Currency]    VARCHAR(10)      NOT NULL DEFAULT 'KRW',
        [Status]      VARCHAR(20)      NOT NULL DEFAULT 'Pending',
        [PgProvider]  VARCHAR(20)      NOT NULL,
        [PaymentKey]  VARCHAR(200)     NULL,
        [Method]      VARCHAR(60)      NULL,
        [ApprovedAt]  DATETIME2        NULL,
        [FailReason]  VARCHAR(600)     NULL,
        [CreatedAt]   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        [DeletedAt]   DATETIME2        NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
        CONSTRAINT [UQ_Payments_OrderId] UNIQUE ([OrderId])
    );
END
