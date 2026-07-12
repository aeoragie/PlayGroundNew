using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Landing;
using PlayGround.Server.Actors;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>축구 랜딩(공개) 콘텐츠. 인증 불필요.</summary>
    [ApiController]
    [Route("api/soccer/landing")]
    public class SoccerLandingController : ControllerBase
    {
        private readonly ActorGateway mGateway;

        public SoccerLandingController(ActorGateway gateway)
        {
            mGateway = gateway;
        }

        [HttpGet("contents")]
        public async Task<Envelope<LandingContentsResponse>> GetContentsAsync(CancellationToken cancellation)
        {
            Result<LandingContentsResponse> result = await mGateway.AskAsync<LandingContentsResponse>(
                ActorNames.SoccerLanding, new GetLandingContentsRequest(), cancellation);
            return result.ToEnvelope();
        }
    }
}
