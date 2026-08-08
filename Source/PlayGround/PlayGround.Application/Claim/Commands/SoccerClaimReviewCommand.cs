using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Claim;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Claim.Commands
{
    /// <summary>연결 요청 승인/거절 유즈케이스 — 소유 팀 관리자만. 권한·존재 판정은 프로시저가 하고
    /// 거부는 빈 결과 → 일괄 Forbidden (요청 존재 여부를 흘리지 않는다).</summary>
    public class SoccerClaimReviewCommand
    {
        private readonly IClaimRepository mRepository;
        private readonly ILogger<SoccerClaimReviewCommand> mLogger;

        public SoccerClaimReviewCommand(IClaimRepository repository, ILogger<SoccerClaimReviewCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ReviewClaimResponse>> ExecuteAsync(
            Guid managerUserId, ReviewClaimRequestRequest request, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(managerUserId, request, cancellation)).LogWith(mLogger, "Execute");

        private async Task<Result<ReviewClaimResponse>> ExecuteCoreAsync(
            Guid managerUserId, ReviewClaimRequestRequest request, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || request is null || request.RequestId == Guid.Empty)
            {
                return Result<ReviewClaimResponse>.Error(ErrorCode.InvalidInput, "managerUserId/requestId required");
            }

            Result<ReviewClaimResponse?> reviewed =
                await mRepository.ReviewAsync(managerUserId, request.RequestId, request.Approve, cancellation);
            if (reviewed.IsError)
            {
                return Result<ReviewClaimResponse>.Failure(reviewed.ResultData);
            }

            if (reviewed.Value is null)
            {
                return Result<ReviewClaimResponse>.Error(ErrorCode.Forbidden, "request not reviewable");
            }

            return Result<ReviewClaimResponse>.Success(reviewed.Value);
        }
    }
}
