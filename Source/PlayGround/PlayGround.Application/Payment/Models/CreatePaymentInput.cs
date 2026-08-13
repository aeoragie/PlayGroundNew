using PlayGround.Domain.Payment;

namespace PlayGround.Application.Payment.Models
{
    /// <summary>주문 생성 포트 입력 (검증·정규화 완료 값만 담는다).</summary>
    public class CreatePaymentInput
    {
        public Guid UserId { get; set; }
        public Sport Sport { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string OrderName { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public PaymentProvider PgProvider { get; set; }
    }
}
