using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>선수단(로스터) 쓰기 — 선수 추가·내보내기(소프트 삭제)·복구(실행취소).
    /// 소유 판정은 프로시저가 팀 ManagerUserId로 하고, 거부는 빈 결과로 돌아온다(존재 여부 미노출).
    /// 조회는 SoccerTeamRosterCommand가 담당 — 여기는 쓰기만.</summary>
    public class SoccerTeamRosterWriteCommand
    {
        private const int MaxNameLength = 50;
        private const int MaxJerseyLength = 10;
        private const int MaxPositionLength = 20;
        private const int MaxGradeLength = 20;

        private static readonly HashSet<string> AllowedAgeGroups = new(StringComparer.Ordinal) { "U12", "U15", "U18" };

        private readonly ISoccerTeamRepository mRepository;

        public SoccerTeamRosterWriteCommand(ISoccerTeamRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<TeamRosterPlayerDto>> AddAsync(
            Guid managerUserId, AddTeamPlayerRequest request, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || request is null)
            {
                return Result<TeamRosterPlayerDto>.Error(ErrorCode.Unauthorized, "managerUserId/request required");
            }

            // 클라이언트 인라인 검증과 같은 규칙 — 우회 요청도 같은 기준으로 막는다
            request.Name = request.Name?.Trim() ?? string.Empty;
            request.JerseyNumber = Trimmed(request.JerseyNumber);
            request.Position = Trimmed(request.Position);
            request.Grade = Trimmed(request.Grade);
            request.AgeGroup = Trimmed(request.AgeGroup);

            if (request.Name.Length is 0 or > MaxNameLength)
            {
                return Result<TeamRosterPlayerDto>.Error(ErrorCode.InvalidInput, "name is required");
            }

            if (request.JerseyNumber is { Length: > MaxJerseyLength }
                || request.Position is { Length: > MaxPositionLength }
                || request.Grade is { Length: > MaxGradeLength })
            {
                return Result<TeamRosterPlayerDto>.Error(ErrorCode.InvalidInput, "field too long");
            }

            // 등번호는 숫자만 — 화면도 numeric inputmode로 받는다
            if (request.JerseyNumber is not null && !request.JerseyNumber.All(char.IsAsciiDigit))
            {
                return Result<TeamRosterPlayerDto>.Error(ErrorCode.InvalidInput, "jersey must be numeric");
            }

            // 연령대는 화이트리스트 — 저장 화이트리스트라 클라이언트가 우회해도 서버가 막는다
            if (request.AgeGroup is not null && !AllowedAgeGroups.Contains(request.AgeGroup))
            {
                return Result<TeamRosterPlayerDto>.Error(ErrorCode.InvalidInput, "invalid age group");
            }

            Result<TeamRosterPlayerDto?> added = await mRepository.AddTeamPlayerByManagerAsync(managerUserId, request, cancellation);
            if (added.IsError)
            {
                return Result<TeamRosterPlayerDto>.Failure(added.ResultData);
            }

            if (added.Value is null)
            {
                return Result<TeamRosterPlayerDto>.Error(ErrorCode.Forbidden, "team not owned");
            }

            return Result<TeamRosterPlayerDto>.Success(added.Value);
        }

        public async Task<Result<bool>> RemoveAsync(
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
