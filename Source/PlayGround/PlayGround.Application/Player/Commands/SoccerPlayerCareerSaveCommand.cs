using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>커리어 이력 저장·삭제 유즈케이스. 관리 주체(보호자) 계정만 —
    /// 소유 판정은 프로시저가 UserId로 하고, 여기서는 입력 형태만 막는다.</summary>
    public class SoccerPlayerCareerSaveCommand
    {
        /// <summary>선수 커리어가 이보다 이른 해에 시작할 수는 없다고 본다(오타 방어).</summary>
        private const int EarliestYear = 1990;

        private readonly IPlayerRepository mRepository;

        public SoccerPlayerCareerSaveCommand(IPlayerRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<bool>> ExecuteAsync(Guid userId, SavePlayerCareerRequest request, Guid? playerId = null, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            ArgumentNullException.ThrowIfNull(request);

            request.TeamName = request.TeamName?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(request.TeamName))
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "team name is required");
            }

            int currentYear = DateTime.UtcNow.Year;
            if (request.StartDate.Year < EarliestYear || request.StartDate.Year > currentYear + 1)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "start date is out of range");
            }

            // 종료가 시작보다 앞설 수 없다 (null = 현재 소속이라 검사 대상 아님)
            if (request.EndDate is not null && request.EndDate < request.StartDate)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "end date precedes start date");
            }

            request.Role = NullIfBlank(request.Role);
            request.Note = NullIfBlank(request.Note);
            request.BadgeLabel = NullIfBlank(request.BadgeLabel);

            Result<bool> applied = await mRepository.SaveCareerAsync(userId, request, playerId, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            // 소유가 아니거나 대상이 없음 — 구분해서 알려주지 않는다
            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "career save not permitted for user");
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeleteAsync(Guid userId, DeletePlayerCareerRequest request, Guid? playerId = null, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            ArgumentNullException.ThrowIfNull(request);

            if (request.CareerId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "careerId is empty");
            }

            Result<bool> applied = await mRepository.DeleteCareerAsync(userId, request.CareerId, request.Restore, playerId, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "career delete not permitted for user");
            }

            return Result<bool>.Success(true);
        }

        private static string? NullIfBlank(string? value)
        {
            string? trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }
    }
}
