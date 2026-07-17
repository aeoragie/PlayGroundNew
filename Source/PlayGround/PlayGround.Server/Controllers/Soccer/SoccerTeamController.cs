using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Server.Actors;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>축구 팀(본인 운영). 온보딩 팀 생성 등.</summary>
    [ApiController]
    [Route("api/soccer/team")]
    [Authorize]
    public class SoccerTeamController : ControllerBase
    {
        private readonly ActorGateway mGateway;

        public SoccerTeamController(ActorGateway gateway)
        {
            mGateway = gateway;
        }

        [HttpPost("me")]
        public async Task<Envelope<CreateTeamResponse>> CreateMyTeamAsync(
            [FromBody] CreateTeamRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<CreateTeamResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<CreateTeamResponse> result = await mGateway.AskAsync<CreateTeamResponse>(
                ActorNames.SoccerTeamProfile, new CreateSoccerTeamMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/info")]
        public async Task<Envelope<TeamInfoResponse>> GetMyTeamInfoAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamInfoResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamInfoResponse> result = await mGateway.AskAsync<TeamInfoResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamInfoMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        // 공개 팀 홈페이지 — 비로그인 읽기전용. 'me' 리터럴 라우트가 {slug}보다 우선 매칭된다.
        [AllowAnonymous]
        [HttpGet("{slug}/home")]
        public async Task<Envelope<TeamPublicHomeResponse>> GetTeamHomeAsync(string slug, CancellationToken cancellation)
        {
            Result<TeamPublicHomeResponse> result = await mGateway.AskAsync<TeamPublicHomeResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamHomeMessage(slug), cancellation);
            return result.ToEnvelope();
        }

        // 공개 팀 홈 시즌성적 탭 — 비로그인 읽기전용. 탭 진입 시 지연 로드.
        [AllowAnonymous]
        [HttpGet("{slug}/season-record")]
        public async Task<Envelope<TeamSeasonRecordResponse>> GetTeamSeasonRecordAsync(
            string slug, [FromQuery] int season, CancellationToken cancellation)
        {
            Result<TeamSeasonRecordResponse> result = await mGateway.AskAsync<TeamSeasonRecordResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamSeasonRecordMessage(slug, season), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/roster")]
        public async Task<Envelope<TeamRosterResponse>> GetMyTeamRosterAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamRosterResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamRosterResponse> result = await mGateway.AskAsync<TeamRosterResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamRosterMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/matches")]
        public async Task<Envelope<TeamMatchesResponse>> GetMyTeamMatchesAsync(
            [FromQuery] int season, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamMatchesResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamMatchesResponse> result = await mGateway.AskAsync<TeamMatchesResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamMatchesMessage(userId, season), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/videos")]
        public async Task<Envelope<TeamVideosResponse>> GetMyTeamVideosAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamVideosResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamVideosResponse> result = await mGateway.AskAsync<TeamVideosResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamVideosMessage(userId), cancellation);
            return result.ToEnvelope();
        }
    }
}
