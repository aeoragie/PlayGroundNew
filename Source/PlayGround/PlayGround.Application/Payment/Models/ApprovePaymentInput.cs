using PlayGround.Shared.Time;

namespace PlayGround.Application.Payment.Models
{
    /// <summary>PG 승인 결과 반영 포트 입력.</summary>
    public class ApprovePaymentInput
    {
        public Guid UserId { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string PaymentKey { get; set; } = string.Empty;
        public string? Method { get; set; }
        public SystemTime ApprovedAt { get; set; }
    }
}
