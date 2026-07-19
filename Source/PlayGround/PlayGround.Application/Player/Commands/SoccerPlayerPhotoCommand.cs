using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 사진 설정·삭제 유즈케이스. 미성년자 보호 규칙에 따라 주체는 보호자·소속팀 관리자만이며
    /// 판정은 프로시저가 한다 — 여기서는 입력 형태만 막는다.</summary>
    public class SoccerPlayerPhotoCommand
    {
        /// <summary>업로드 API가 돌려주는 경로만 저장한다 — 외부 URL을 프로필에 심지 못하게.</summary>
        private const string AllowedPathPrefix = "/uploads/";

        private readonly IPlayerRepository mRepository;

        public SoccerPlayerPhotoCommand(IPlayerRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<bool>> ExecuteAsync(Guid userId, Guid playerId, string? photoUrl, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            if (playerId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "playerId is empty");
            }

            // null = 삭제(이니셜 아바타로 복귀). 값이 있으면 우리 업로드 경로여야 한다
            string? normalized = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl!.Trim();
            if (normalized is not null && !normalized.StartsWith(AllowedPathPrefix, StringComparison.Ordinal))
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "photo url is not an uploaded path");
            }

            Result<bool> applied = await mRepository.SetPhotoAsync(userId, playerId, normalized, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            // 권한 없음·선수 없음을 구분하지 않는다 — 존재 여부를 알려주지 않기 위해
            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "photo edit not permitted for user");
            }

            return Result<bool>.Success(true);
        }
    }
}
