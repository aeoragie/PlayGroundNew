using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 프로필 수치(키·몸무게·주발·학교) 편집 유즈케이스. 관리 주체(보호자) 계정만 —
    /// UserId로 소유 선수를 해석하므로 타인 프로필은 변경할 수 없다.
    /// SoccerPreferredFoot enum은 Client 전용이라 여기선 화이트리스트를 직접 검사한다(로스터 AgeGroup 전례).</summary>
    public class SoccerPlayerProfileInfoUpdateCommand
    {
        private static readonly string[] AllowedFeet = { "Left", "Right", "Both" };

        private readonly IPlayerRepository mRepository;

        public SoccerPlayerProfileInfoUpdateCommand(IPlayerRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<bool>> ExecuteAsync(Guid userId, UpdatePlayerProfileInfoRequest request, Guid? playerId = null, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            if (request is null)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "request is null");
            }

            // 값 검증 — 저장 화이트리스트라 클라이언트 우회도 서버가 막는다.
            if (request.HeightCm is { } h && (h < 100 || h > 230))
            {
                return Result<bool>.Error(ErrorCode.OutOfRange, "heightCm must be 100-230");
            }

            if (request.WeightKg is { } w && (w < 20 || w > 150))
            {
                return Result<bool>.Error(ErrorCode.OutOfRange, "weightKg must be 20-150");
            }

            string? foot = string.IsNullOrWhiteSpace(request.PreferredFoot) ? null : request.PreferredFoot.Trim();
            if (foot is not null && Array.IndexOf(AllowedFeet, foot) < 0)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "unknown preferred foot");
            }

            string? school = string.IsNullOrWhiteSpace(request.SchoolName) ? null : request.SchoolName.Trim();
            if (school is { Length: > 100 })
            {
                return Result<bool>.Error(ErrorCode.OutOfRange, "schoolName too long");
            }

            Result<bool> applied = await mRepository.UpdateProfileInfoAsync(
                userId, request.HeightCm, request.WeightKg, foot, school, playerId, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "player not found for user");
            }

            return Result<bool>.Success(true);
        }
    }
}
