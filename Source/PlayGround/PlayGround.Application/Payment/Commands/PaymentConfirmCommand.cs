using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Payment.Models;
using PlayGround.Contracts.Payment;
using PlayGround.Domain.Payment;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Payment.Commands
{
    /// <summary>
    /// 결제 승인 유즈케이스. 저장 주문과 금액·소유자를 대조한 뒤에만 PG 승인을 호출한다 (금액 위·변조 방어).
    /// 이미 승인된 주문은 저장 결과를 그대로 돌려준다 (복귀 페이지 새로고침 멱등).
    /// </summary>
    public class PaymentConfirmCommand
    {
        private readonly IPaymentRepository mRepository;
        private readonly IPaymentGateway mGateway;
        private readonly ILogger<PaymentConfirmCommand> mLogger;

        public PaymentConfirmCommand(IPaymentRepository repository, IPaymentGateway gateway, ILogger<PaymentConfirmCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            Debug.Assert(gateway != null, "gateway is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mGateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PaymentSummaryResponse>> ExecuteAsync(
            Guid userId, ConfirmPaymentRequest request, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, request, cancellation)).LogWith(mLogger, "Execute", ("UserId", userId));

        private async Task<Result<PaymentSummaryResponse>> ExecuteCoreAsync(
            Guid userId, ConfirmPaymentRequest request, CancellationToken cancellation = default)
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

            if (string.IsNullOrWhiteSpace(request.PaymentKey) || string.IsNullOrWhiteSpace(request.OrderId))
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.MissingRequired, "PaymentKey and OrderId are required");
            }

            Result<PaymentSummaryResponse?> found = await mRepository.FindByOrderAsync(userId, request.OrderId, cancellation);
            if (found.IsError)
            {
                return Result<PaymentSummaryResponse>.Failure(found.ResultData);
            }

            if (found.Value is null)
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.NotFound, "payment order not found");
            }

            if (found.Value.Status == PaymentStatus.Approved)
            {
                return Result<PaymentSummaryResponse>.Success(found.Value);
            }

            if (found.Value.Status != PaymentStatus.Pending)
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.InvalidState, "payment order is not pending");
            }

            if (found.Value.Amount != request.Amount)
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.InvalidInput, "Amount does not match the stored order");
            }

            Result<PaymentApproval> approval = await mGateway.ConfirmAsync(
                request.PaymentKey.Trim(), request.OrderId.Trim(), request.Amount, cancellation);
            if (approval.IsError)
            {
                // 승인 실패를 원장에 남긴다. 기록 실패는 부가 작업이라 원인 실패를 그대로 돌려준다.
                await mRepository.FailAsync(userId, request.OrderId, approval.ResultData.Message ?? "confirm failed", cancellation);
                return Result<PaymentSummaryResponse>.Failure(approval.ResultData);
            }

            var input = new ApprovePaymentInput
            {
                UserId = userId,
                OrderId = request.OrderId,
                PaymentKey = approval.Value.PaymentKey,
                Method = approval.Value.Method,
                ApprovedAt = approval.Value.ApprovedAt
            };

            Result<PaymentSummaryResponse?> approved = await mRepository.ApproveAsync(input, cancellation);
            if (approved.IsError)
            {
                return Result<PaymentSummaryResponse>.Failure(approved.ResultData);
            }

            if (approved.Value is null)
            {
                // PG 승인은 났는데 원장 반영 조건이 어긋난 상태 (경합 등). 운영 대응이 필요한 신호라 Error로 남긴다.
                return Result<PaymentSummaryResponse>.Error(ErrorCode.Conflict, "payment approved by PG but ledger update was rejected");
            }

            mLogger.Info("Payment approved", ("UserId", userId), ("OrderId", request.OrderId), ("Amount", request.Amount));
            return Result<PaymentSummaryResponse>.Success(approved.Value);
        }
    }
}
