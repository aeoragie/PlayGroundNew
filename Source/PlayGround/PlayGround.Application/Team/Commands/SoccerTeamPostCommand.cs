using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Domain.Soccer;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>팀 게시판 유즈케이스 (Design.TeamBoard) — 관리자 편집(작성·수정·고정·공개·삭제) +
    /// 공개 소식 열람(슬러그) + 보호자 열람·읽음. **공지 발행 시 로스터 보호자 전원 알림**(자료는 없음).
    /// 작성 계정은 현재 팀 관리자뿐 — 코치 계정이 생기면 권한 판정을 확장한다(현재는 ManagerUserId 소유로 판정).</summary>
    public class SoccerTeamPostCommand
    {
        private const int MaxTitleLength = 60;
        private const int MinTitleLength = 2;
        private const int MaxBodyLength = 2000;
        private const int MinBodyLength = 2;
        private const int MaxFiles = 3;

        private readonly ISoccerTeamRepository mRepository;
        private readonly INotificationRepository mNotificationRepository;
        private readonly ILogger<SoccerTeamPostCommand> mLogger;

        public SoccerTeamPostCommand(ISoccerTeamRepository repository, INotificationRepository notificationRepository, ILogger<SoccerTeamPostCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            Debug.Assert(notificationRepository != null, "notificationRepository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mNotificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<TeamPostsResponse>> GetMineAsync(Guid managerUserId, CancellationToken cancellation = default) =>
            (await GetMineCoreAsync(managerUserId, cancellation)).LogWith(mLogger, "GetMine", ("ManagerUserId", managerUserId));

        private async Task<Result<TeamPostsResponse>> GetMineCoreAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<TeamPostsResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            return await mRepository.GetPostsByManagerAsync(managerUserId, cancellation);
        }

        public async Task<Result<TeamNewsResponse>> GetNewsBySlugAsync(string slug, CancellationToken cancellation = default) =>
            (await GetNewsBySlugCoreAsync(slug, cancellation)).LogWith(mLogger, "GetNewsBySlug", ("Slug", slug));

        private async Task<Result<TeamNewsResponse>> GetNewsBySlugCoreAsync(string slug, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Result<TeamNewsResponse>.Error(ErrorCode.InvalidInput, "slug is required");
            }

            return await mRepository.GetNewsBySlugAsync(slug.Trim(), cancellation);
        }

        public async Task<Result<GuardianTeamPostsResponse>> GetForGuardianAsync(
            Guid userId, Guid playerId, CancellationToken cancellation = default) =>
            (await GetForGuardianCoreAsync(userId, playerId, cancellation)).LogWith(mLogger, "GetForGuardian", ("UserId", userId));

        private async Task<Result<GuardianTeamPostsResponse>> GetForGuardianCoreAsync(
            Guid userId, Guid playerId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty || playerId == Guid.Empty)
            {
                return Result<GuardianTeamPostsResponse>.Error(ErrorCode.InvalidInput, "userId/playerId required");
            }

            return await mRepository.GetPostsByGuardianAsync(userId, playerId, cancellation);
        }

        public async Task<Result<TeamPostDto>> SaveAsync(
            Guid managerUserId, SaveTeamPostRequest request, string? authorName, CancellationToken cancellation = default) =>
            (await SaveCoreAsync(managerUserId, request, authorName, cancellation)).LogWith(mLogger, "Save", ("ManagerUserId", managerUserId));

        private async Task<Result<TeamPostDto>> SaveCoreAsync(
            Guid managerUserId, SaveTeamPostRequest request, string? authorName, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || request is null)
            {
                return Result<TeamPostDto>.Error(ErrorCode.Unauthorized, "managerUserId/request required");
            }

            // 클라이언트 인라인 검증과 같은 규칙 — 우회 요청도 같은 기준으로 막는다
            request.Title = request.Title?.Trim() ?? string.Empty;
            request.Body = request.Body?.Trim() ?? string.Empty;
            request.Files = (request.Files ?? new List<TeamPostFileInput>())
                .Where(f => !string.IsNullOrWhiteSpace(f.Url) && !string.IsNullOrWhiteSpace(f.Name))
                .Take(MaxFiles + 1)
                .ToList();

            if (!Enum.TryParse(request.Type, out SoccerTeamPostType type))
            {
                return Result<TeamPostDto>.Error(ErrorCode.InvalidInput, "unknown post type");
            }

            request.Type = type.ToString();

            if (request.Title.Length is < MinTitleLength or > MaxTitleLength)
            {
                return Result<TeamPostDto>.Error(ErrorCode.InvalidInput, "title length out of range");
            }

            if (request.Body.Length is < MinBodyLength or > MaxBodyLength)
            {
                return Result<TeamPostDto>.Error(ErrorCode.InvalidInput, "body length out of range");
            }

            if (request.Files.Count > MaxFiles)
            {
                return Result<TeamPostDto>.Error(ErrorCode.InvalidInput, "too many attachments");
            }

            // 첨부는 우리 업로드 경로만 허용 — 외부 URL을 게시판에 심지 못하게 막는다
            if (request.Files.Any(f => !f.Url.StartsWith("/uploads/", StringComparison.Ordinal)))
            {
                return Result<TeamPostDto>.Error(ErrorCode.InvalidInput, "attachment url not allowed");
            }

            bool isNewNotice = request.PostId == Guid.Empty && type == SoccerTeamPostType.Notice;

            Result<TeamPostDto?> saved = await mRepository.SavePostByManagerAsync(managerUserId, request, authorName, cancellation);
            if (saved.IsError)
            {
                return Result<TeamPostDto>.Failure(saved.ResultData);
            }

            mLogger.Info("Post saved", ("ManagerUserId", managerUserId));

            if (saved.Value is null)
            {
                return Result<TeamPostDto>.Error(ErrorCode.Forbidden, "post not editable");
            }

            // 공지 발행(신규)만 로스터 보호자 전원 알림 — 자료·수정은 알림 없음(README, 재알림 없음)
            if (isNewNotice)
            {
                await NotifyRosterGuardiansAsync(managerUserId, saved.Value, cancellation);
            }

            return Result<TeamPostDto>.Success(saved.Value);
        }

        /// <summary>공지 발행 알림 — 팀 로스터 보호자 전원(설정 필터 없음, "전원"). 자녀별 수신자를 UserId로 중복
        /// 제거하고 각 1건 발송(멱등은 프로시저가 보장). 발송 실패는 저장 결과에 영향을 주지 않는다(부가 작업).</summary>
        private async Task NotifyRosterGuardiansAsync(Guid managerUserId, TeamPostDto post, CancellationToken cancellation)
        {
            Result<List<NotificationRecipient>> recipients =
                await mRepository.GetPostRecipientsByManagerAsync(managerUserId, cancellation);
            if (recipients.IsError || recipients.Value.Count == 0)
            {
                return;
            }

            // 같은 보호자가 여러 자녀를 가지면 한 번만 — 첫 자녀를 딥링크 대상(TargetPlayerId)으로.
            IEnumerable<NotificationRecipient> unique = recipients.Value
                .GroupBy(r => r.UserId)
                .Select(g => g.First());

            foreach (NotificationRecipient recipient in unique)
            {
                await mNotificationRepository.CreateAsync(
                    recipient.UserId, SoccerNotificationType.TeamNotice.ToString(), post.PostId, recipient.PlayerId,
                    actorName: null, playerName: recipient.PlayerName, teamName: recipient.TeamName,
                    metaText: post.Title, subText: null, cancellation);
            }
        }

        public async Task<Result<TeamPostDto>> SetPinnedAsync(
            Guid managerUserId, Guid postId, bool isPinned, CancellationToken cancellation = default) =>
            (await SetPinnedCoreAsync(managerUserId, postId, isPinned, cancellation)).LogWith(mLogger, "SetPinned", ("ManagerUserId", managerUserId));

        private async Task<Result<TeamPostDto>> SetPinnedCoreAsync(
            Guid managerUserId, Guid postId, bool isPinned, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || postId == Guid.Empty)
            {
                return Result<TeamPostDto>.Error(ErrorCode.InvalidInput, "managerUserId/postId required");
            }

            Result<TeamPostDto?> updated = await mRepository.SetPostPinnedByManagerAsync(managerUserId, postId, isPinned, cancellation);
            if (updated.IsError)
            {
                return Result<TeamPostDto>.Failure(updated.ResultData);
            }

            mLogger.Info("Post pinned updated", ("ManagerUserId", managerUserId));

            if (updated.Value is null)
            {
                // 소유 아님·삭제·고정 초과가 모두 빈 결과 — 고정 시도였다면 개수 초과가 가장 흔한 원인
                return Result<TeamPostDto>.Error(ErrorCode.InvalidInput, isPinned ? "pin limit exceeded or not editable" : "not editable");
            }

            return Result<TeamPostDto>.Success(updated.Value);
        }

        public async Task<Result<TeamPostDto>> SetPublicAsync(
            Guid managerUserId, Guid postId, bool isPublic, CancellationToken cancellation = default) =>
            (await SetPublicCoreAsync(managerUserId, postId, isPublic, cancellation)).LogWith(mLogger, "SetPublic", ("ManagerUserId", managerUserId));

        private async Task<Result<TeamPostDto>> SetPublicCoreAsync(
            Guid managerUserId, Guid postId, bool isPublic, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || postId == Guid.Empty)
            {
                return Result<TeamPostDto>.Error(ErrorCode.InvalidInput, "managerUserId/postId required");
            }

            Result<TeamPostDto?> updated = await mRepository.SetPostPublicByManagerAsync(managerUserId, postId, isPublic, cancellation);
            if (updated.IsError)
            {
                return Result<TeamPostDto>.Failure(updated.ResultData);
            }

            mLogger.Info("Post public updated", ("ManagerUserId", managerUserId));

            if (updated.Value is null)
            {
                return Result<TeamPostDto>.Error(ErrorCode.Forbidden, "post not editable");
            }

            return Result<TeamPostDto>.Success(updated.Value);
        }

        public async Task<Result<bool>> DeleteAsync(
            Guid managerUserId, Guid postId, bool restore, CancellationToken cancellation = default) =>
            (await DeleteCoreAsync(managerUserId, postId, restore, cancellation)).LogWith(mLogger, "Delete", ("ManagerUserId", managerUserId));

        private async Task<Result<bool>> DeleteCoreAsync(
            Guid managerUserId, Guid postId, bool restore, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || postId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "managerUserId/postId required");
            }

            Result<bool> applied = await mRepository.DeletePostByManagerAsync(managerUserId, postId, restore, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            mLogger.Info("Post deleted", ("ManagerUserId", managerUserId));

            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "post not deletable");
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> MarkReadAsync(Guid userId, Guid postId, CancellationToken cancellation = default) =>
            (await MarkReadCoreAsync(userId, postId, cancellation)).LogWith(mLogger, "MarkRead", ("UserId", userId));

        private async Task<Result<bool>> MarkReadCoreAsync(Guid userId, Guid postId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty || postId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "userId/postId required");
            }

            return await mRepository.MarkPostReadAsync(userId, postId, cancellation);
        }
    }
}
