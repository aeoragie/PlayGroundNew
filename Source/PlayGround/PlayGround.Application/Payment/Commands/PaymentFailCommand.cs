using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Payment;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Payment.Commands
{
    /// <summary>결제 실패 기록 유즈케이스 (failUrl 복귀). Pending 주문만 Failed로 바꾼다.</summary>
    public class PaymentFailCommand
    {
        // FailReason 컬럼 VARCHAR(600) = 한글 200자. 한글 최악 기준으로 문자 수를 자른다.
        public const int MaxReasonLength = 200;

        private readonly IPaymentRepository mRepository;
        private readonly ILogger<PaymentFailCommand> mLogger;

        public PaymentFailCommand(IPaymentRepository repository, ILogger<PaymentFailCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PaymentSummaryResponse>> ExecuteAsync(
            Guid userId, FailPaymentRequest request, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, request, cancellation)).LogWith(mLogger, "Execute", ("UserId", userId));

        private async Task<Result<PaymentSummaryResponse>> ExecuteCoreAsync(
            Guid userId, FailPaymentRequest request, CancellationToken cancellation = default)
        {
            Debug.Assert(request != null, "request is required");
            if (request is null)
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.InvalidInput, "request is null");
            }

            if (userId == Guid.Empty)
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            if (string.IsNullOrWhiteSpace(request.OrderId))
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.MissingRequired, "OrderId is required");
            }

            string reason = $"{request.FailCode.Trim()}: {request.FailMessage.Trim()}".Trim(':', ' ');
            if (string.IsNullOrEmpty(reason))
            {
                reason = "unknown";
            }

            if (reason.Length > MaxReasonLength)
            {
                reason = reason[..MaxReasonLength];
            }

            Result<PaymentSummaryResponse?> failed = await mRepository.FailAsync(userId, request.OrderId, reason, cancellation);
            if (failed.IsError)
            {
                return Result<PaymentSummaryResponse>.Failure(failed.ResultData);
            }

            if (failed.Value is null)
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.NotFound, "pending payment order not found");
            }

            mLogger.Info("Payment failed", ("UserId", userId), ("OrderId", request.OrderId), ("Reason", reason));
            return Result<PaymentSummaryResponse>.Success(failed.Value);
        }
    }
}
