using PlayGround.Application.Payment.Models;
using PlayGround.Contracts.Payment;
using PlayGround.Shared.Result;

namespace PlayGround.Application.Interfaces
{
    /// <summary>결제 원장 포트 (Persistence에서 구현). 미존재·소유 불일치·상태 불일치는 Success(null).</summary>
    public interface IPaymentRepository
    {
        Task<Result<PaymentSummaryResponse?>> CreateAsync(CreatePaymentInput input, CancellationToken cancellation = default);

        Task<Result<PaymentSummaryResponse?>> FindByOrderAsync(Guid userId, string orderId, CancellationToken cancellation = default);

        /// <summary>Pending이면서 소유자가 일치할 때만 승인 반영. 아니면 Success(null) — 중복 승인 방어.</summary>
        Task<Result<PaymentSummaryResponse?>> ApproveAsync(ApprovePaymentInput input, CancellationToken cancellation = default);

        /// <summary>Pending이면서 소유자가 일치할 때만 실패 반영. 아니면 Success(null).</summary>
        Task<Result<PaymentSummaryResponse?>> FailAsync(Guid userId, string orderId, string failReason, CancellationToken cancellation = default);

        Task<Result<List<PaymentSummaryResponse>>> ListByUserAsync(Guid userId, CancellationToken cancellation = default);
    }
}
