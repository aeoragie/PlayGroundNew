using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Server.Actors;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>대시보드 허브. 팀·자녀·액션을 한 번에 준다 — 로그인 직후 첫 화면이라 왕복을 줄인다.</summary>
    [ApiController]
    [Route("api/soccer/dashboard")]
    [Authorize]
    public class SoccerDashboardController : ControllerBase
    {
        private readonly ActorGateway mGateway;

        public SoccerDashboardController(ActorGateway gateway)
        {
            mGateway = gateway;
        }

        /// <summary>허브 묶음. 응답의 관리 대상 수(팀+자녀)가 곧 라우팅 3분기의 근거다.</summary>
        [HttpGet("me/hub")]
        public async Task<Envelope<DashboardHubResponse>> GetMyHubAsync(
            [FromQuery] int? season, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<DashboardHubResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            string displayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? string.Empty;
            int seasonYear = season ?? DateTime.UtcNow.Year;

            Result<DashboardHubResponse> result = await mGateway.AskAsync<DashboardHubResponse>(
                ActorNames.SoccerDashboard,
                new GetSoccerDashboardHubMessage(userId, displayName, seasonYear), cancellation);
            return result.ToEnvelope();
        }
    }
}
