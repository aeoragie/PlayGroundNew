using PlayGround.Application.Payment.Models;
using PlayGround.Domain.Payment;
using PlayGround.Shared.Result;

namespace PlayGround.Application.Interfaces
{
    /// <summary>
    /// PG 벤더 중립 결제 포트 (Server에서 어댑터 구현). 실패는 Result로 돌려준다 (ExternalServiceError 계열).
    /// 비활성 구성이면 Provider가 Unknown이고 모든 호출이 실패 Result다.
    /// </summary>
    public interface IPaymentGateway
    {
        PaymentProvider Provider { get; }

        /// <summary>PG 결제 승인. 결제창에서 돌아온 paymentKey·orderId·amount를 PG에 확정 요청한다.</summary>
        Task<Result<PaymentApproval>> ConfirmAsync(string paymentKey, string orderId, int amount, CancellationToken cancellation = default);
    }
}
