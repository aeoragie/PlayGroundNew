using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Domain.Soccer;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>포트폴리오 영상 저장·삭제 유즈케이스. 관리 주체(보호자) 계정만 —
    /// 소유 판정은 프로시저가 UserId로 한다. 링크는 유튜브만 허용하고 저장 형태를 서버가 정규화한다.</summary>
    public class SoccerPlayerPortfolioSaveCommand
    {
        /// <summary>태그 칩은 화면 폭이 한정돼 있다 — 개수·길이를 여기서 막는다.</summary>
        private const int MaxTags = 5;
        private const int MaxTagLength = 20;

        private readonly IPlayerRepository mRepository;

        public SoccerPlayerPortfolioSaveCommand(IPlayerRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<bool>> ExecuteAsync(Guid userId, SavePlayerPortfolioVideoRequest request, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            ArgumentNullException.ThrowIfNull(request);

            request.Title = request.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(request.Title))
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "title is required");
            }

            // 유튜브 링크만 — 그 외 주소를 프로필에 심지 못하게 한다
            string? canonicalUrl = YouTubeVideoLink.ToCanonicalUrl(request.VideoUrl);
            if (canonicalUrl is null)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "video url is not a youtube link");
            }

            // 썸네일은 클라이언트 값을 쓰지 않고 링크에서 파생한다 (엉뚱한 이미지 주소 저장 방지)
            request.VideoUrl = canonicalUrl;
            request.ThumbnailUrl = YouTubeVideoLink.ToThumbnailUrl(canonicalUrl);

            request.Tags = request.Tags
                .Select(tag => tag?.Trim() ?? string.Empty)
                .Where(tag => tag.Length > 0)
                .Select(tag => tag.Length > MaxTagLength ? tag[..MaxTagLength] : tag)
                .Distinct()
                .Take(MaxTags)
                .ToList();

            Result<bool> applied = await mRepository.SavePortfolioVideoAsync(userId, request, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "portfolio save not permitted for user");
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeleteAsync(Guid userId, DeletePlayerPortfolioVideoRequest request, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            ArgumentNullException.ThrowIfNull(request);

            if (request.VideoId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "videoId is empty");
            }

            Result<bool> applied = await mRepository.DeletePortfolioVideoAsync(userId, request.VideoId, request.Restore, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "portfolio delete not permitted for user");
            }

            return Result<bool>.Success(true);
        }
    }
}
