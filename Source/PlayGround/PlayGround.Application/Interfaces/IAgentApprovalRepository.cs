using PlayGround.Shared.Result;
using PlayGround.Contracts.Agent;

namespace PlayGround.Application.Interfaces
{
    /// <summary>에이전트 열람 요청 심사 포트 (보호자 측). 요청 생성·열람 로그 적재는 에이전트 서비스의 몫 —
    /// 여기는 조회·심사(승인/거절/철회)·차단만. 소유 아님·전이 불가는 Success(null/false).</summary>
    public interface IAgentApprovalRepository
    {
        /// <summary>심사 화면 묶음 (요청+선수, 에이전트 신원, 열람 기록). 소유 아님·미존재는 Success(null).</summary>
        Task<Result<AgentViewRequestResponse?>> GetRequestAsync(Guid guardianUserId, Guid requestId, CancellationToken cancellation = default);

        /// <summary>심사 — Approve(+30일)/Deny/Revoke. 전이 불가 상태는 Success(null).</summary>
        Task<Result<AgentViewRequestResponse?>> ReviewAsync(Guid guardianUserId, Guid requestId, string action, CancellationToken cancellation = default);

        /// <summary>차단 ("다시 받지 않기") — 차단 행 생성(멱등) + 대기 요청 거절 처리.</summary>
        Task<Result<bool>> BlockAgentAsync(Guid guardianUserId, Guid requestId, CancellationToken cancellation = default);
    }
}
