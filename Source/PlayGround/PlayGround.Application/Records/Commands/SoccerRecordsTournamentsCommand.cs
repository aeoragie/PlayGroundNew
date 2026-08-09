using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Records;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Records.Commands
{
    /// <summary>시즌 대회/리그 목록 조회 유즈케이스 (Records 목록·아카이브, 공개).</summary>
    public class SoccerRecordsTournamentsCommand
    {
        private const int MinSeasonYear = 2000;
        private const int MaxSeasonYear = 2100;

        private readonly ISoccerRecordsRepository mRepository;

        private readonly ILogger<SoccerRecordsTournamentsCommand> mLogger;

        public SoccerRecordsTournamentsCommand(ISoccerRecordsRepository repository, ILogger<SoccerRecordsTournamentsCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<RecordsTournamentsResponse>> ExecuteAsync(int seasonYear, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(seasonYear, cancellation)).LogWith(mLogger, "Execute", ("SeasonYear", seasonYear));

        private async Task<Result<RecordsTournamentsResponse>> ExecuteCoreAsync(int seasonYear, CancellationToken cancellation = default)
        {
            if (seasonYear is < MinSeasonYear or > MaxSeasonYear)
            {
                return Result<RecordsTournamentsResponse>.Error(ErrorCode.OutOfRange, "seasonYear is out of range");
            }

            return await mRepository.GetTournamentsBySeasonAsync(seasonYear, cancellation);
        }
    }
}
