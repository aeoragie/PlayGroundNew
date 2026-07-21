using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Agent;
using PlayGround.Server.Actors;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>에이전트 열람 요청 심사 (보호자 측 — Design.AgentViewApproval).
    /// **요청 생성·열람 로그 적재는 에이전트 서비스의 몫이다** (설계 결정 4·6) — 여기에 생성
    /// 엔드포인트를 만들면 축 분리가 무너진다. UI 진입점은 클라 FeatureFlags.AgentApproval 뒤에 있다.</summary>
    [ApiController]
    [Route("api/soccer/agent-approvals")]
    [Authorize]
    public class SoccerAgentApprovalController : ControllerBase
    {
        private readonly ActorGateway mGateway;

        public SoccerAgentApprovalController(ActorGateway gateway)
        {
            mGateway = gateway;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : Guid.Empty;

        /// <summary>심사 화면 묶음 — 심사 주체 본인 것만.</summary>
        [HttpGet("me/{requestId:guid}")]
        public async Task<Envelope<AgentViewRequestResponse>> GetAsync(Guid requestId, CancellationToken cancellation)
        {
            Guid userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                return Result<AgentViewRequestResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<AgentViewRequestResponse> result = await mGateway.AskAsync<AgentViewRequestResponse>(
                ActorNames.SoccerClaim, new GetAgentViewRequestMessage(userId, requestId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>심사 — Approve(30일)/Deny/Revoke. 거절은 사유 없이 상태만 전달된다.</summary>
        [HttpPost("me/review")]
        public async Task<Envelope<AgentViewRequestResponse>> ReviewAsync(
            [FromBody] ReviewAgentViewRequestRequest request, CancellationToken cancellation)
        {
            Guid userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                return Result<AgentViewRequestResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<AgentViewRequestResponse> result = await mGateway.AskAsync<AgentViewRequestResponse>(
                ActorNames.SoccerClaim, new ReviewAgentViewRequestMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>차단 — "이 에이전트의 요청 다시 받지 않기" (대기 요청은 함께 거절 처리).</summary>
        [HttpPost("me/{requestId:guid}/block")]
        public async Task<Envelope<bool>> BlockAsync(Guid requestId, CancellationToken cancellation)
        {
            Guid userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerClaim, new BlockAgentMessage(userId, requestId), cancellation);
            return result.ToEnvelope();
        }
    }
}
