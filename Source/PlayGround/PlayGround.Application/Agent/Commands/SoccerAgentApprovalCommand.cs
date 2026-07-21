using System.Diagnostics;
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

        public SoccerAgentApprovalCommand(IAgentApprovalRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<AgentViewRequestResponse>> GetAsync(
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
            Guid guardianUserId, ReviewAgentViewRequestRequest request, CancellationToken cancellation = default)
        {
            if (guardianUserId == Guid.Empty || request is null || request.RequestId == Guid.Empty)
            {
                return Result<AgentViewRequestResponse>.Error(ErrorCode.InvalidInput, "guardianUserId/requestId required");
            }

            // 액션은 enum 화이트리스트 (이름 형태만 — 숫자 문자열 거부)
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

        public async Task<Result<bool>> BlockAsync(Guid guardianUserId, Guid requestId, CancellationToken cancellation = default)
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

            if (!blocked.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "request not blockable");
            }

            return Result<bool>.Success(true);
        }
    }
}
