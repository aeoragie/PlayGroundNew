using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
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
        private readonly ILogger<SoccerPlayerFieldVisibilityCommand> mLogger;

        public SoccerPlayerFieldVisibilityCommand(IPlayerRepository repository, ILogger<SoccerPlayerFieldVisibilityCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> ExecuteAsync(Guid userId, string fieldName, bool isPublic, Guid? playerId = null, CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(userId, fieldName, isPublic, playerId, cancellation)).LogWith(mLogger, "Execute", ("UserId", userId));

        private async Task<Result<bool>> ExecuteCoreAsync(Guid userId, string fieldName, bool isPublic, Guid? playerId = null, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

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

            mLogger.InfoWith("Field visibility updated", ("UserId", userId));

            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.NotFound, "player not found for user");
            }

            return Result<bool>.Success(true);
        }
    }
}
