using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Records;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Records.Commands
{
    /// <summary>공식 경기 상세 조회 유즈케이스 (Records 내 화면, 공개·읽기 전용).</summary>
    public class SoccerRecordsMatchDetailCommand
    {
        private readonly ISoccerRecordsRepository mRepository;

        public SoccerRecordsMatchDetailCommand(ISoccerRecordsRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<RecordsMatchDetailResponse>> ExecuteAsync(Guid matchId, CancellationToken cancellation = default)
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
