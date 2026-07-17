using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Records;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Records.Commands
{
    /// <summary>대회 상세 묶음 조회 유즈케이스 (Records 상세, 공개).</summary>
    public class SoccerRecordsTournamentDetailCommand
    {
        private readonly ISoccerRecordsRepository mRepository;

        public SoccerRecordsTournamentDetailCommand(ISoccerRecordsRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<RecordsTournamentDetailResponse>> ExecuteAsync(Guid tournamentId, CancellationToken cancellation = default)
        {
            if (tournamentId == Guid.Empty)
            {
                return Result<RecordsTournamentDetailResponse>.Error(ErrorCode.InvalidInput, "tournamentId is empty");
            }

            Result<RecordsTournamentDetailResponse?> detail = await mRepository.GetTournamentDetailAsync(tournamentId, cancellation);
            if (detail.IsError)
            {
                return Result<RecordsTournamentDetailResponse>.Failure(detail.ResultData);
            }

            if (detail.Value is null)
            {
                return Result<RecordsTournamentDetailResponse>.Error(ErrorCode.NotFound, "tournament not found");
            }

            return Result<RecordsTournamentDetailResponse>.Success(detail.Value);
        }
    }
}
