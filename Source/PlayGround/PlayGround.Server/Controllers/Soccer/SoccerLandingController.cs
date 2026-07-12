using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Landing;
using PlayGround.Application.Landing.Queries;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>축구 랜딩(공개) 콘텐츠. 인증 불필요.
    /// Server는 여러 종목을 함께 호스팅하므로 컨트롤러는 종목별로 분리한다.</summary>
    [ApiController]
    [Route("api/soccer/landing")]
    public class SoccerLandingController : ControllerBase
    {
        private readonly GetLandingContentsQuery GetContents;

        public SoccerLandingController(GetLandingContentsQuery getContents)
        {
            GetContents = getContents;
        }

        [HttpGet("contents")]
        public async Task<Envelope<LandingContentsResponse>> GetContentsAsync(CancellationToken cancellation)
        {
            var result = await GetContents.ExecuteAsync(cancellation);
            return result.ToEnvelope();
        }
    }
}
