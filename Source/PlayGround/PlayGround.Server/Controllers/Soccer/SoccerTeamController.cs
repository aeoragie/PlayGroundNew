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
    }
}
