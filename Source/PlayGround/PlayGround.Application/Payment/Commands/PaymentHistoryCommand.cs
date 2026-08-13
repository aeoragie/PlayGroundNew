using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Payment;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Payment.Commands
{
    /// <summary>내 결제 내역 조회 유즈케이스 (최근순, 저장소 TOP 50).</summary>
    public class PaymentHistoryCommand
    {
        private readonly IPaymentRepository mRepository;
        private readonly ILogger<PaymentHistoryCommand> mLogger;

        public PaymentHistoryCommand(IPaymentRepository repository, ILogger<PaymentHistoryCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<List<PaymentSummaryResponse>>> ExecuteAsync(
            Guid userId, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, cancellation)).LogWith(mLogger, "Execute", ("UserId", userId));

        private async Task<Result<List<PaymentSummaryResponse>>> ExecuteCoreAsync(
            Guid userId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<List<PaymentSummaryResponse>>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            Result<List<PaymentSummaryResponse>> list = await mRepository.ListByUserAsync(userId, cancellation);
            if (list.IsError)
            {
                return Result<List<PaymentSummaryResponse>>.Failure(list.ResultData);
            }

            return list;
        }
    }
}
