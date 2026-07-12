using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Landing;
using PlayGround.Server.Actors;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>축구 랜딩(공개) 콘텐츠. 인증 불필요.
    /// Server는 여러 종목을 함께 호스팅하므로 컨트롤러는 종목별로 분리한다.
    /// 컨트롤러는 얇게 — 요청을 액터 게이트웨이로 보내고 Envelope로 변환만.</summary>
    [ApiController]
    [Route("api/soccer/landing")]
    public class SoccerLandingController : ControllerBase
    {
        private readonly ActorGateway Gateway;

        public SoccerLandingController(ActorGateway gateway)
        {
            Gateway = gateway;
        }

        [HttpGet("contents")]
        public async Task<Envelope<LandingContentsResponse>> GetContentsAsync(CancellationToken cancellation)
        {
            Result<LandingContentsResponse> result = await Gateway.AskAsync<LandingContentsResponse>(
                ActorNames.Landing, new GetLandingContentsRequest(), cancellation);
            return result.ToEnvelope();
        }
    }
}
