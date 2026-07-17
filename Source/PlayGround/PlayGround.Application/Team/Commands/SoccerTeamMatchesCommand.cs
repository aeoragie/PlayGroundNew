using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>팀 시즌 경기 결과 조회 유즈케이스 (팀 대시보드 경기 결과 섹션). 관리자 본인 팀 기준.</summary>
    public class SoccerTeamMatchesCommand
    {
        private const int MinSeasonYear = 2000;
        private const int MaxSeasonYear = 2100;

        private readonly ISoccerTeamRepository mRepository;

        public SoccerTeamMatchesCommand(ISoccerTeamRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<TeamMatchesResponse>> ExecuteAsync(Guid managerUserId, int seasonYear, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<TeamMatchesResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            if (seasonYear is < MinSeasonYear or > MaxSeasonYear)
            {
                return Result<TeamMatchesResponse>.Error(ErrorCode.OutOfRange, "seasonYear is out of range");
            }

            return await mRepository.GetTeamMatchesByManagerAsync(managerUserId, seasonYear, cancellation);
        }
    }
}
