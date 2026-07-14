using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>선수단(로스터) 조회 유즈케이스 (대시보드 선수단 섹션). 관리자 본인 팀 기준.</summary>
    public class SoccerTeamRosterCommand
    {
        private readonly ISoccerTeamRepository mRepository;

        public SoccerTeamRosterCommand(ISoccerTeamRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<TeamRosterResponse>> ExecuteAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<TeamRosterResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            return await mRepository.GetTeamRosterByManagerAsync(managerUserId, cancellation);
        }
    }
}
