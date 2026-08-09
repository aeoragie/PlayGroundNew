using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Records;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Records.Commands
{
    /// <summary>공식 경기 상세 조회 유즈케이스 (Records 내 화면, 공개·읽기 전용).</summary>
    public class SoccerRecordsMatchDetailCommand
    {
        private readonly ISoccerRecordsRepository mRepository;
        private readonly ILogger<SoccerRecordsMatchDetailCommand> mLogger;

        public SoccerRecordsMatchDetailCommand(ISoccerRecordsRepository repository, ILogger<SoccerRecordsMatchDetailCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<RecordsMatchDetailResponse>> ExecuteAsync(Guid matchId, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(matchId, cancellation)).LogWith(mLogger, "Execute", ("MatchId", matchId));

        private async Task<Result<RecordsMatchDetailResponse>> ExecuteCoreAsync(Guid matchId, CancellationToken cancellation = default)
        {
            if (matchId == Guid.Empty)
            {
                return Result<RecordsMatchDetailResponse>.Error(ErrorCode.InvalidInput, "matchId is empty");
            }

            Result<RecordsMatchDetailResponse?> detail = await mRepository.GetMatchDetailAsync(matchId, cancellation);
            if (detail.IsError)
            {
                return Result<RecordsMatchDetailResponse>.Failure(detail.ResultData);
            }

            if (detail.Value is null)
            {
                return Result<RecordsMatchDetailResponse>.Error(ErrorCode.NotFound, "match not found");
            }

            return Result<RecordsMatchDetailResponse>.Success(detail.Value);
        }
    }
}
