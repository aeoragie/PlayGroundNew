using PlayGround.Shared.Time;
using PlayGround.Domain.Payment;

namespace PlayGround.Contracts.Payment
{
    public class CreatePaymentOrderRequest
    {
        public Sport Sport { get; set; }
        public string OrderName { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    public class ConfirmPaymentRequest
    {
        public string PaymentKey { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    public class FailPaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string FailCode { get; set; } = string.Empty;
        public string FailMessage { get; set; } = string.Empty;
    }

    public class PaymentSummaryResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public string OrderName { get; set; } = string.Empty;
        public Sport Sport { get; set; }
        public int Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public PaymentProvider PgProvider { get; set; }
        public string? Method { get; set; }
        public string? FailReason { get; set; }
        public SystemTime? ApprovedAt { get; set; }
        public SystemTime CreatedAt { get; set; }
    }

    public class PaymentConfigResponse
    {
        public bool Enabled { get; set; }

        // 위젯용 공개 키. 시크릿 키는 절대 싣지 않는다.
        public string ClientKey { get; set; } = string.Empty;
    }
}
