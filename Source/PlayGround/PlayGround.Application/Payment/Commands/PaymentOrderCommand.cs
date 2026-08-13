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
    /// <summary>결제 주문 생성 유즈케이스. 검증·정규화 후 Pending 원장 행을 만들고 PG 전달용 orderId를 돌려준다.</summary>
    public class PaymentOrderCommand
    {
        // 토스 최소 결제금액이 100원. 상한은 테스트 플로우 안전선 (실상품 설계 때 재검토).
        public const int MinAmount = 100;
        public const int MaxAmount = 10_000_000;
        public const int MaxOrderNameLength = 100;

        private readonly IPaymentRepository mRepository;
        private readonly IPaymentGateway mGateway;
        private readonly ILogger<PaymentOrderCommand> mLogger;

        public PaymentOrderCommand(IPaymentRepository repository, IPaymentGateway gateway, ILogger<PaymentOrderCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            Debug.Assert(gateway != null, "gateway is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mGateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PaymentSummaryResponse>> ExecuteAsync(
            Guid userId, CreatePaymentOrderRequest request, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, request, cancellation)).LogWith(mLogger, "Execute", ("UserId", userId));

        private async Task<Result<PaymentSummaryResponse>> ExecuteCoreAsync(
            Guid userId, CreatePaymentOrderRequest request, CancellationToken cancellation = default)
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

            if (mGateway.Provider == PaymentProvider.Unknown)
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.FeatureNotAvailable, "payment provider is not configured");
            }

            if (request.Sport == Sport.Unknown)
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.InvalidInput, "Sport is required");
            }

            if (string.IsNullOrWhiteSpace(request.OrderName))
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.MissingRequired, "OrderName is required");
            }

            if (request.Amount is < MinAmount or > MaxAmount)
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.OutOfRange, "Amount is out of range");
            }

            string orderName = request.OrderName.Trim();
            if (orderName.Length > MaxOrderNameLength)
            {
                orderName = orderName[..MaxOrderNameLength];
            }

            var input = new CreatePaymentInput
            {
                UserId = userId,
                Sport = request.Sport,
                OrderId = Guid.NewGuid().ToString("N"),
                OrderName = orderName,
                Amount = request.Amount,
                Currency = "KRW",
                PgProvider = mGateway.Provider
            };

            Result<PaymentSummaryResponse?> created = await mRepository.CreateAsync(input, cancellation);
            if (created.IsError)
            {
                return Result<PaymentSummaryResponse>.Failure(created.ResultData);
            }

            if (created.Value is null)
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.OperationFailed, "payment row was not created");
            }

            mLogger.Info("Payment order created", ("UserId", userId), ("OrderId", input.OrderId), ("Amount", input.Amount));
            return Result<PaymentSummaryResponse>.Success(created.Value);
        }
    }
}
