using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>팀 경기영상 목록 조회 유즈케이스 (팀 대시보드 경기영상 섹션). 관리자 본인 팀 기준.</summary>
    public class SoccerTeamVideosCommand
    {
        private readonly ISoccerTeamRepository mRepository;

        public SoccerTeamVideosCommand(ISoccerTeamRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<TeamVideosResponse>> ExecuteAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<TeamVideosResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            return await mRepository.GetTeamVideosByManagerAsync(managerUserId, cancellation);
        }
    }
}
