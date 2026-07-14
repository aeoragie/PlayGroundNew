using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Server.Actors;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>축구 선수(본인 데이터). 온보딩 프로필 생성 등.</summary>
    [ApiController]
    [Route("api/soccer/player")]
    [Authorize]
    public class SoccerPlayerController : ControllerBase
    {
        private readonly ActorGateway mGateway;

        public SoccerPlayerController(ActorGateway gateway)
        {
            mGateway = gateway;
        }

        [HttpPost("me/profile")]
        public async Task<Envelope<CreatePlayerProfileResponse>> CreateMyProfileAsync(
            [FromBody] CreatePlayerProfileRequest request, CancellationToken cancellation)
        {
            // 인증 컨텍스트는 메일박스를 못 넘으므로 UserId(sub)를 메시지에 실어 보낸다
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<CreatePlayerProfileResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<CreatePlayerProfileResponse> result = await mGateway.AskAsync<CreatePlayerProfileResponse>(
                ActorNames.SoccerPlayerProfile, new CreatePlayerProfileMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/info")]
        public async Task<Envelope<PlayerInfoResponse>> GetMyInfoAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PlayerInfoResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PlayerInfoResponse> result = await mGateway.AskAsync<PlayerInfoResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerPlayerInfoMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        [HttpPost("me/claim")]
        public async Task<Envelope<ClaimPlayerInviteResponse>> ClaimInviteAsync(
            [FromBody] ClaimPlayerInviteRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<ClaimPlayerInviteResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            // 현재 역할은 JWT 클레임에서 — General만 Player로 승격 (상위 역할 강등 방지)
            string? currentRole = User.FindFirstValue(ClaimTypes.Role);
            Result<ClaimPlayerInviteResponse> result = await mGateway.AskAsync<ClaimPlayerInviteResponse>(
                ActorNames.SoccerPlayerProfile, new ClaimSoccerPlayerInviteMessage(userId, currentRole, request), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/career")]
        public async Task<Envelope<PlayerCareerResponse>> GetMyCareerAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PlayerCareerResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PlayerCareerResponse> result = await mGateway.AskAsync<PlayerCareerResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerPlayerCareerMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/portfolio")]
        public async Task<Envelope<PlayerPortfolioResponse>> GetMyPortfolioAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PlayerPortfolioResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PlayerPortfolioResponse> result = await mGateway.AskAsync<PlayerPortfolioResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerPlayerPortfolioMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        [HttpPut("me/profile/visibility")]
        public async Task<Envelope<bool>> SetMyFieldVisibilityAsync(
            [FromBody] SetPlayerFieldVisibilityRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new SetSoccerPlayerFieldVisibilityMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }
    }
}
