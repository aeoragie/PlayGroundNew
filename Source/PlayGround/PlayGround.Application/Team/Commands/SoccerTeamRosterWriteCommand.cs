using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Soccer;
using PlayGround.Contracts.Team;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>선수단(로스터) 쓰기 — 선수 추가·내보내기(소프트 삭제)·복구(실행취소).
    /// 소유 판정은 프로시저가 팀 ManagerUserId로 하고, 거부는 빈 결과로 돌아온다(존재 여부 미노출).
    /// 조회는 SoccerTeamRosterCommand가 담당 — 여기는 쓰기만.</summary>
    public class SoccerTeamRosterWriteCommand
    {
        private const int MaxNameLength = 50;
        private const int MaxJerseyLength = 10;

        private readonly ISoccerTeamRepository mRepository;

        private readonly ILogger<SoccerTeamRosterWriteCommand> mLogger;

        public SoccerTeamRosterWriteCommand(ISoccerTeamRepository repository, ILogger<SoccerTeamRosterWriteCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<TeamRosterPlayerDto>> AddAsync(
            Guid managerUserId, AddTeamPlayerRequest request, CancellationToken cancellation = default) =>
            (await AddCoreAsync(managerUserId, request, cancellation)).LogWith(mLogger, "Add", ("ManagerUserId", managerUserId));

        private async Task<Result<TeamRosterPlayerDto>> AddCoreAsync(
            Guid managerUserId, AddTeamPlayerRequest request, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || request is null)
            {
                return Result<TeamRosterPlayerDto>.Error(ErrorCode.Unauthorized, "managerUserId/request required");
            }

            // 클라이언트 인라인 검증과 같은 규칙 — 우회 요청도 같은 기준으로 막는다
            request.Name = request.Name?.Trim() ?? string.Empty;
            request.JerseyNumber = Trimmed(request.JerseyNumber);

            if (request.Name.Length is 0 or > MaxNameLength)
            {
                return Result<TeamRosterPlayerDto>.Error(ErrorCode.InvalidInput, "name is required");
            }

            if (request.JerseyNumber is { Length: > MaxJerseyLength })
            {
                return Result<TeamRosterPlayerDto>.Error(ErrorCode.InvalidInput, "field too long");
            }

            if (request.JerseyNumber is not null && !request.JerseyNumber.All(char.IsAsciiDigit))
            {
                return Result<TeamRosterPlayerDto>.Error(ErrorCode.InvalidInput, "jersey must be numeric");
            }

            // Unknown = 미지 값 폴백 — 저장 값이 아니다
            if (request.Position == SoccerPosition.Unknown
                || request.Grade == SoccerGrade.Unknown
                || request.AgeGroup == SoccerAgeGroup.Unknown)
            {
                return Result<TeamRosterPlayerDto>.Error(ErrorCode.InvalidInput, "invalid enum value");
            }

            Result<TeamRosterPlayerDto?> added = await mRepository.AddTeamPlayerByManagerAsync(managerUserId, request, cancellation);
            if (added.IsError)
            {
                return Result<TeamRosterPlayerDto>.Failure(added.ResultData);
            }

            mLogger.Info("Team player added", ("ManagerUserId", managerUserId));

            if (added.Value is null)
            {
                return Result<TeamRosterPlayerDto>.Error(ErrorCode.Forbidden, "team not owned");
            }

            return Result<TeamRosterPlayerDto>.Success(added.Value);
        }

        public async Task<Result<bool>> RemoveAsync(
            Guid managerUserId, Guid teamPlayerId, bool restore, CancellationToken cancellation = default) =>
            (await RemoveCoreAsync(managerUserId, teamPlayerId, restore, cancellation)).LogWith(mLogger, "Remove", ("ManagerUserId", managerUserId));

        private async Task<Result<bool>> RemoveCoreAsync(
            Guid managerUserId, Guid teamPlayerId, bool restore, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || teamPlayerId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "managerUserId/teamPlayerId required");
            }

            Result<bool> removed = await mRepository.RemoveTeamPlayerByManagerAsync(managerUserId, teamPlayerId, restore, cancellation);
            if (removed.IsError)
            {
                return Result<bool>.Failure(removed.ResultData);
            }

            mLogger.Info("Team player removed", ("ManagerUserId", managerUserId));

            // 빈 결과(false) = 남의 팀이거나 이미 그 상태 — 존재 여부를 흘리지 않고 Forbidden으로 통일
            if (!removed.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "roster entry not editable");
            }

            return Result<bool>.Success(true);
        }

        private static string? Trimmed(string? value)
        {
            string? t = value?.Trim();
            return string.IsNullOrEmpty(t) ? null : t;
        }
    }
}
