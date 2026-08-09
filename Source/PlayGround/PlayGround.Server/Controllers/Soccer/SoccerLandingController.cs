using Microsoft.AspNetCore.Mvc;
using PlayGround.Contracts.Landing;
using PlayGround.Server.Actors;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;

namespace PlayGround.Server.Controllers.Soccer
{
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
