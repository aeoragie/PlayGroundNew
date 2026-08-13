using PlayGround.Application.Interfaces;
using PlayGround.Application.Payment.Models;
using PlayGround.Domain.Payment;
using PlayGround.Shared.Result;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// 결제 비활성 어댑터 (Provider None). 시크릿 없는 환경에서도 기동은 되고,
    /// 결제 호출만 실패 Result가 된다 — 주문 생성은 커맨드가 Provider Unknown으로 먼저 거른다.
    /// </summary>
    public class DisabledPaymentGatewayService : IPaymentGateway
    {
        public PaymentProvider Provider => PaymentProvider.Unknown;

        public Task<Result<PaymentApproval>> ConfirmAsync(
            string paymentKey, string orderId, int amount, CancellationToken cancellation = default)
        {
            return Task.FromResult(Result<PaymentApproval>.Error(
                ErrorCode.ExternalServiceUnavailable, "payment provider is not configured"));
        }
    }
}
