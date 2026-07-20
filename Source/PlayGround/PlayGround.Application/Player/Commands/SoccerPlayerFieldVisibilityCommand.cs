using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Domain.Soccer;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 프로필 항목 공개 설정 변경 유즈케이스. 관리 주체(보호자) 계정만 —
    /// UserId로 소유 선수를 해석하므로 타인 프로필은 변경할 수 없다.</summary>
    public class SoccerPlayerFieldVisibilityCommand
    {
        private readonly IPlayerRepository mRepository;

        public SoccerPlayerFieldVisibilityCommand(IPlayerRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<bool>> ExecuteAsync(Guid userId, string fieldName, bool isPublic, Guid? playerId = null, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            // 허용 항목만 통과 (enum 이름 형태 — 숫자 문자열 거부)
            if (string.IsNullOrWhiteSpace(fieldName)
                || char.IsAsciiDigit(fieldName[0])
                || !Enum.TryParse(fieldName, out SoccerPlayerProfileField field))
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "unknown field name");
            }

            Result<bool> applied = await mRepository.SetFieldVisibilityAsync(userId, field.ToString(), isPublic, playerId, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.NotFound, "player not found for user");
            }

            return Result<bool>.Success(true);
        }
    }
}
