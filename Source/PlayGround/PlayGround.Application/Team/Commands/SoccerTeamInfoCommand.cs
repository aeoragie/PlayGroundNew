using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>팀 정보 묶음 조회 유즈케이스 (대시보드 팀 정보 섹션). 관리자 본인 팀 기준.</summary>
    public class SoccerTeamInfoCommand
    {
        private readonly ISoccerTeamRepository mRepository;
        private readonly ILogger<SoccerTeamInfoCommand> mLogger;

        public SoccerTeamInfoCommand(ISoccerTeamRepository repository, ILogger<SoccerTeamInfoCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<TeamInfoResponse>> ExecuteAsync(Guid managerUserId, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(managerUserId, cancellation)).LogWith(mLogger, "Execute");

        private async Task<Result<TeamInfoResponse>> ExecuteCoreAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<TeamInfoResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            Result<TeamInfoResponse?> info = await mRepository.GetTeamInfoByManagerAsync(managerUserId, cancellation);
            if (info.IsError)
            {
                return Result<TeamInfoResponse>.Failure(info.ResultData);
            }

            if (info.Value is null)
            {
                return Result<TeamInfoResponse>.Error(ErrorCode.NotFound, "team not found for manager");
            }

            return Result<TeamInfoResponse>.Success(info.Value);
        }
    }
}
