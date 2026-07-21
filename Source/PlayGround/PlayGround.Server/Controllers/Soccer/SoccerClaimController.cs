using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Claim;
using PlayGround.Server.Actors;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>보호자 Claim 플로우 (/claim 4스텝) + 팀 관리자 승인.
    /// 기존 즉시 연결(POST api/soccer/player/me/claim)과 별개 경로 — 여기는 승인이 필요한 요청 방식.</summary>
    [ApiController]
    [Route("api/soccer/claim")]
    [Authorize]
    public class SoccerClaimController : ControllerBase
    {
        private readonly ActorGateway mGateway;

        public SoccerClaimController(ActorGateway gateway)
        {
            mGateway = gateway;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : Guid.Empty;

        /// <summary>스텝 ①→②: 코드로 선수 카드 조회 (소진 없음).</summary>
        [HttpGet("invite/{code}")]
        public async Task<Envelope<ClaimInviteCardResponse>> LookupAsync(string code, CancellationToken cancellation)
        {
            Guid userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                return Result<ClaimInviteCardResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<ClaimInviteCardResponse> result = await mGateway.AskAsync<ClaimInviteCardResponse>(
                ActorNames.SoccerClaim, new GetClaimInviteCardMessage(userId, code), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>스텝 ②→③: 연결 요청 생성 (팀 관리자에게 액션형 알림 발송).</summary>
        [HttpPost("me/requests")]
        public async Task<Envelope<ClaimRequestSummaryResponse>> CreateAsync(
            [FromBody] CreateClaimRequestRequest request, CancellationToken cancellation)
        {
            Guid userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                return Result<ClaimRequestSummaryResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            // 요청자 표시 이름은 토큰 클레임 — 본문으로 받지 않는다 (위조 방지)
            string requesterName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? string.Empty;
            Result<ClaimRequestSummaryResponse> result = await mGateway.AskAsync<ClaimRequestSummaryResponse>(
                ActorNames.SoccerClaim, new CreateClaimRequestMessage(userId, requesterName, request), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>재방문 복원 — 내 최신 요청 (없으면 NotFound → 스텝 ①).</summary>
        [HttpGet("me/request")]
        public async Task<Envelope<ClaimRequestSummaryResponse>> GetMineAsync(CancellationToken cancellation)
        {
            Guid userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                return Result<ClaimRequestSummaryResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<ClaimRequestSummaryResponse> result = await mGateway.AskAsync<ClaimRequestSummaryResponse>(
                ActorNames.SoccerClaim, new GetOwnClaimRequestMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>승인/거절 — 소유 팀 관리자만 (판정은 프로시저, 거부는 Forbidden).</summary>
        [HttpPost("requests/review")]
        public async Task<Envelope<ReviewClaimResponse>> ReviewAsync(
            [FromBody] ReviewClaimRequestRequest request, CancellationToken cancellation)
        {
            Guid userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                return Result<ReviewClaimResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<ReviewClaimResponse> result = await mGateway.AskAsync<ReviewClaimResponse>(
                ActorNames.SoccerClaim, new ReviewClaimRequestMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }
    }
}
