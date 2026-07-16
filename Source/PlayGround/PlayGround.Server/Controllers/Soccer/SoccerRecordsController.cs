using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Records;
using PlayGround.Server.Actors;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>공개 경기기록(Records). 비로그인 전면 무료 공개 — AllowAnonymous.</summary>
    [ApiController]
    [Route("api/soccer/records")]
    [AllowAnonymous]
    public class SoccerRecordsController : ControllerBase
    {
        private readonly ActorGateway mGateway;

        public SoccerRecordsController(ActorGateway gateway)
        {
            mGateway = gateway;
        }

        [HttpGet("tournaments")]
        public async Task<Envelope<RecordsTournamentsResponse>> GetTournamentsAsync(
            [FromQuery] int season, CancellationToken cancellation)
        {
            Result<RecordsTournamentsResponse> result = await mGateway.AskAsync<RecordsTournamentsResponse>(
                ActorNames.SoccerRecords, new GetSoccerRecordsTournamentsMessage(season), cancellation);
            return result.ToEnvelope();
        }
    }
}
