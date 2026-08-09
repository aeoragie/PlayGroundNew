using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Team;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>학부모 리뷰 유즈케이스 (Design.TeamPublicHome ⑥) — 공개 조회 + 작성·수정·삭제.
    /// 재원 판정(보호자 연결 자녀의 팀 Active 소속)은 저장 프로시저가 한다. 팀 관리자용 삭제 경로는 없다.</summary>
    public class SoccerTeamReviewCommand
    {
        private const int MaxBodyLength = 500;

        private readonly ISoccerTeamRepository mRepository;

        private readonly ILogger<SoccerTeamReviewCommand> mLogger;

        public SoccerTeamReviewCommand(ISoccerTeamRepository repository, ILogger<SoccerTeamReviewCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <param name="viewerUserId">로그인 열람자 — 리뷰 쓰기 자격·내 리뷰 판정에만 쓴다. 게스트는 null.</param>
        public async Task<Result<TeamReviewsResponse>> GetBySlugAsync(string slug, Guid? viewerUserId = null, CancellationToken cancellation = default) =>
            (await GetBySlugCoreAsync(slug, viewerUserId, cancellation)).LogWith(mLogger, "GetBySlug", ("Slug", slug));

        private async Task<Result<TeamReviewsResponse>> GetBySlugCoreAsync(string slug, Guid? viewerUserId = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Result<TeamReviewsResponse>.Error(ErrorCode.InvalidInput, "slug is required");
            }

            return await mRepository.GetReviewsBySlugAsync(slug.Trim(), viewerUserId, cancellation);
        }

        public async Task<Result<bool>> SaveAsync(Guid authorUserId, SaveTeamReviewRequest request, CancellationToken cancellation = default) =>
            (await SaveCoreAsync(authorUserId, request, cancellation)).LogWith(mLogger, "Save", ("AuthorUserId", authorUserId));

        private async Task<Result<bool>> SaveCoreAsync(Guid authorUserId, SaveTeamReviewRequest request, CancellationToken cancellation = default)
        {
            if (authorUserId == Guid.Empty || request is null)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "authorUserId/request required");
            }

            // 클라이언트 인라인 검증과 같은 규칙 — 우회 요청도 같은 기준으로 막는다
            request.TeamSlug = request.TeamSlug?.Trim() ?? string.Empty;
            request.Body = request.Body?.Trim() ?? string.Empty;

            if (request.TeamSlug.Length == 0)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "team slug is required");
            }

            if (request.Rating is < 1 or > 5)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "rating out of range");
            }

            if (request.Body.Length is 0 or > MaxBodyLength)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "body is required");
            }

            Result<bool> saved = await mRepository.SaveReviewAsync(authorUserId, request, cancellation);
            if (saved.IsError)
            {
                return Result<bool>.Failure(saved.ResultData);
            }

            mLogger.Info("Review saved", ("AuthorUserId", authorUserId));

            if (!saved.Value)
            {
                // 재원 자격 없음·중복 신규·남의 리뷰 — 사유를 구분하지 않는다
                return Result<bool>.Error(ErrorCode.Forbidden, "review not writable");
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeleteAsync(Guid authorUserId, Guid reviewId, bool restore, CancellationToken cancellation = default) =>
            (await DeleteCoreAsync(authorUserId, reviewId, restore, cancellation)).LogWith(mLogger, "Delete", ("AuthorUserId", authorUserId));

        private async Task<Result<bool>> DeleteCoreAsync(Guid authorUserId, Guid reviewId, bool restore, CancellationToken cancellation = default)
        {
            if (authorUserId == Guid.Empty || reviewId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "authorUserId/reviewId required");
            }

            Result<bool> deleted = await mRepository.DeleteReviewAsync(authorUserId, reviewId, restore, cancellation);
            if (deleted.IsError)
            {
                return Result<bool>.Failure(deleted.ResultData);
            }

            mLogger.Info("Review deleted", ("AuthorUserId", authorUserId));

            if (!deleted.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "review not deletable");
            }

            return Result<bool>.Success(true);
        }
    }
}
