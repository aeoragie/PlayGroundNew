using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>공개 팀 홈 시즌성적 조회 유즈케이스 (Slug 기준, 비로그인 공개).</summary>
    public class SoccerTeamSeasonRecordCommand
    {
        private const int MinSeasonYear = 2000;
        private const int MaxSeasonYear = 2100;

        private readonly ISoccerTeamRepository mRepository;

        private readonly ILogger<SoccerTeamSeasonRecordCommand> mLogger;

        public SoccerTeamSeasonRecordCommand(ISoccerTeamRepository repository, ILogger<SoccerTeamSeasonRecordCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<TeamSeasonRecordResponse>> ExecuteAsync(string slug, int seasonYear, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(slug, seasonYear, cancellation)).LogWith(mLogger, "Execute", ("Slug", slug));

        private async Task<Result<TeamSeasonRecordResponse>> ExecuteCoreAsync(string slug, int seasonYear, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Result<TeamSeasonRecordResponse>.Error(ErrorCode.MissingRequired, "slug is required");
            }

            if (seasonYear is < MinSeasonYear or > MaxSeasonYear)
            {
                return Result<TeamSeasonRecordResponse>.Error(ErrorCode.OutOfRange, "seasonYear is out of range");
            }

            return await mRepository.GetTeamSeasonRecordBySlugAsync(slug, seasonYear, cancellation);
        }
    }
}
