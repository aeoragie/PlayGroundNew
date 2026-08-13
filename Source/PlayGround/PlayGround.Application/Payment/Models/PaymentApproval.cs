using PlayGround.Shared.Time;

namespace PlayGround.Application.Payment.Models
{
    /// <summary>PG 승인 응답 (벤더 중립). Method 어휘는 PG 소유라 문자열 그대로 전달한다.</summary>
    public class PaymentApproval
    {
        public string PaymentKey { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string? Method { get; set; }
        public SystemTime ApprovedAt { get; set; }
        public string RawStatus { get; set; } = string.Empty;
    }
}
