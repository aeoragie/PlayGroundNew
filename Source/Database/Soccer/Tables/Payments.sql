-- 결제 원장 (테스트 플로우 골격). 주문 생성 시 Pending으로 만들고 PG 승인 결과로 갱신한다.
-- 결제는 종목 공통 기능이라 Soccer 프리픽스를 붙이지 않는다. 종목은 Sport 컬럼으로 구분한다.
-- 무엇을 팔지 미정 단계라 상품 참조 컬럼은 두지 않는다 (실대상 확정 시 확장).
-- 금액 대조의 진실 소스다. 승인 요청의 amount는 이 행의 Amount와 일치해야 한다.
CREATE TABLE [dbo].[Payments]
(
    [PaymentId]   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId]      UNIQUEIDENTIFIER NOT NULL,           -- Account Users.Id (앱 계층 참조)
    [Sport]       VARCHAR(20)      NOT NULL,           -- 종목 구분 'Soccer' (공통 결제 원장)
    [OrderId]     VARCHAR(64)      NOT NULL,           -- PG 전달용 주문 키 (GUID N 포맷 32자), 유니크
    [OrderName]   VARCHAR(300)     NOT NULL,           -- UTF-8 (한글 100자) 주문 표시명
    [Amount]      INT              NOT NULL,           -- KRW 정수
    [Currency]    VARCHAR(10)      NOT NULL DEFAULT 'KRW',
    [Status]      VARCHAR(20)      NOT NULL DEFAULT 'Pending', -- 'Pending','Approved','Failed','Canceled'
    [PgProvider]  VARCHAR(20)      NOT NULL,           -- 'Toss'
    [PaymentKey]  VARCHAR(200)     NULL,               -- PG 결제 키 (승인 후)
    [Method]      VARCHAR(60)      NULL,               -- PG가 준 결제수단 문자열 (표시 전용, 어휘는 PG 소유)
    [ApprovedAt]  DATETIME2        NULL,               -- 승인 순간(UTC)
    [FailReason]  VARCHAR(600)     NULL,               -- UTF-8 (한글 200자) 실패 코드·사유

    [CreatedAt]   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]   DATETIME2        NULL,               -- 삭제 = 소프트

    CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
    CONSTRAINT [UQ_Payments_OrderId] UNIQUE ([OrderId])
);
