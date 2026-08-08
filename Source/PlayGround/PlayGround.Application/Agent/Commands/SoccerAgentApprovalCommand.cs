using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Agent;
using PlayGround.Domain.Soccer;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Agent.Commands
{
    /// <summary>에이전트 열람 요청 심사 유즈케이스 (보호자 측 — 미성년자 보호 관문).
    /// 요청 생성·열람 로그 적재·재요청 쿨다운은 에이전트 서비스의 몫 — 여기는 조회·심사·차단만.
    /// 소유 아님·전이 불가는 일괄 Forbidden (요청 존재 여부를 흘리지 않는다).</summary>
    public class SoccerAgentApprovalCommand
    {
        private readonly IAgentApprovalRepository mRepository;
        private readonly ILogger<SoccerAgentApprovalCommand> mLogger;

        public SoccerAgentApprovalCommand(IAgentApprovalRepository repository, ILogger<SoccerAgentApprovalCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<AgentViewRequestResponse>> GetAsync(
            Guid guardianUserId, Guid requestId, CancellationToken cancellation = default) =>
            (await GetCoreAsync(guardianUserId, requestId, cancellation)).LogWith(mLogger, "Get", ("GuardianUserId", guardianUserId));

        private async Task<Result<AgentViewRequestResponse>> GetCoreAsync(
            Guid guardianUserId, Guid requestId, CancellationToken cancellation = default)
        {
            if (guardianUserId == Guid.Empty || requestId == Guid.Empty)
            {
                return Result<AgentViewRequestResponse>.Error(ErrorCode.InvalidInput, "guardianUserId/requestId required");
            }

            Result<AgentViewRequestResponse?> request = await mRepository.GetRequestAsync(guardianUserId, requestId, cancellation);
            if (request.IsError)
            {
                return Result<AgentViewRequestResponse>.Failure(request.ResultData);
            }

            if (request.Value is null)
            {
                return Result<AgentViewRequestResponse>.Error(ErrorCode.NotFound, "request not found");
            }

            return Result<AgentViewRequestResponse>.Success(request.Value);
        }

        public async Task<Result<AgentViewRequestResponse>> ReviewAsync(
            Guid guardianUserId, ReviewAgentViewRequestRequest request, CancellationToken cancellation = default) =>
            (await ReviewCoreAsync(guardianUserId, request, cancellation)).LogWith(mLogger, "Review", ("GuardianUserId", guardianUserId));

        private async Task<Result<AgentViewRequestResponse>> ReviewCoreAsync(
            Guid guardianUserId, ReviewAgentViewRequestRequest request, CancellationToken cancellation = default)
        {
            if (guardianUserId == Guid.Empty || request is null || request.RequestId == Guid.Empty)
            {
                return Result<AgentViewRequestResponse>.Error(ErrorCode.InvalidInput, "guardianUserId/requestId required");
            }

            if (string.IsNullOrWhiteSpace(request.Action)
                || char.IsAsciiDigit(request.Action[0])
                || !Enum.TryParse(request.Action, out SoccerAgentReviewAction action))
            {
                return Result<AgentViewRequestResponse>.Error(ErrorCode.InvalidInput, "unknown action");
            }

            Result<AgentViewRequestResponse?> reviewed =
                await mRepository.ReviewAsync(guardianUserId, request.RequestId, action.ToString(), cancellation);
            if (reviewed.IsError)
            {
                return Result<AgentViewRequestResponse>.Failure(reviewed.ResultData);
            }

            if (reviewed.Value is null)
            {
                return Result<AgentViewRequestResponse>.Error(ErrorCode.Forbidden, "request not reviewable");
            }

            return Result<AgentViewRequestResponse>.Success(reviewed.Value);
        }

        /// <summary>요청 자격 판정 — 만료·거절 쿨다운·차단(PlayGround 단독). requesterUserId로 에이전트 본인 해석
        /// (남의 자격은 조회 불가). 에이전트 서비스가 요청 생성 전에 조회한다 — 생성 자체는 여기서 하지 않는다.</summary>
        public async Task<Result<AgentRequestEligibilityResponse>> GetEligibilityAsync(
            Guid requesterUserId, Guid playerId, Guid guardianUserId, CancellationToken cancellation = default) =>
            (await GetEligibilityCoreAsync(requesterUserId, playerId, guardianUserId, cancellation)).LogWith(mLogger, "GetEligibility", ("RequesterUserId", requesterUserId));

        private async Task<Result<AgentRequestEligibilityResponse>> GetEligibilityCoreAsync(
            Guid requesterUserId, Guid playerId, Guid guardianUserId, CancellationToken cancellation = default)
        {
            if (requesterUserId == Guid.Empty || playerId == Guid.Empty || guardianUserId == Guid.Empty)
            {
                return Result<AgentRequestEligibilityResponse>.Error(ErrorCode.InvalidInput, "requesterUserId/playerId/guardianUserId required");
            }

            return await mRepository.GetEligibilityAsync(requesterUserId, playerId, guardianUserId, cancellation);
        }

        public async Task<Result<bool>> BlockAsync(Guid guardianUserId, Guid requestId, CancellationToken cancellation = default) =>
            (await BlockCoreAsync(guardianUserId, requestId, cancellation)).LogWith(mLogger, "Block", ("GuardianUserId", guardianUserId));

        private async Task<Result<bool>> BlockCoreAsync(Guid guardianUserId, Guid requestId, CancellationToken cancellation = default)
        {
            if (guardianUserId == Guid.Empty || requestId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "guardianUserId/requestId required");
            }

            Result<bool> blocked = await mRepository.BlockAgentAsync(guardianUserId, requestId, cancellation);
            if (blocked.IsError)
            {
                return blocked;
            }

            mLogger.InfoWith("Agent blocked", ("GuardianUserId", guardianUserId));

            if (!blocked.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "request not blockable");
            }

            return Result<bool>.Success(true);
        }
    }
}
