using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Notification;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Notification.Commands
{
    public class SoccerNotificationCommand
    {
        private readonly INotificationRepository mRepository;
        private readonly ILogger<SoccerNotificationCommand> mLogger;

        public SoccerNotificationCommand(INotificationRepository repository, ILogger<SoccerNotificationCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<NotificationsResponse>> GetAsync(Guid userId, CancellationToken cancellation = default) =>
            (await GetCoreAsync(userId, cancellation)).LogWith(mLogger, "Get", ("UserId", userId));

        private async Task<Result<NotificationsResponse>> GetCoreAsync(Guid userId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<NotificationsResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            return await mRepository.GetByUserAsync(userId, cancellation);
        }

        /// <summary>알림 센터 페이지 — 세그먼트 필터 + 페이지네이션. 필터는 화이트리스트(all|action|unread), 그 외는 all.</summary>
        public async Task<Result<NotificationPageResponse>> GetPageAsync(
            Guid userId, string? filter, int offset, int limit, CancellationToken cancellation = default) =>
            (await GetPageCoreAsync(userId, filter, offset, limit, cancellation)).LogWith(mLogger, "GetPage", ("UserId", userId));

        private async Task<Result<NotificationPageResponse>> GetPageCoreAsync(
            Guid userId, string? filter, int offset, int limit, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<NotificationPageResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            string normalized = filter is "action" or "unread" ? filter : "all";
            int safeOffset = offset < 0 ? 0 : offset;
            int safeLimit = limit is < 1 or > 50 ? 20 : limit;

            return await mRepository.GetPageByUserAsync(userId, normalized, safeOffset, safeLimit, cancellation);
        }

        /// <summary>여러 건 읽음 — 페이지 진입 시 화면에 보인 알림. 빈 목록은 0 성공.</summary>
        public async Task<Result<int>> MarkReadBulkAsync(
            Guid userId, IReadOnlyCollection<Guid> notificationIds, CancellationToken cancellation = default) =>
            (await MarkReadBulkCoreAsync(userId, notificationIds, cancellation)).LogWith(mLogger, "MarkReadBulk", ("UserId", userId));

        private async Task<Result<int>> MarkReadBulkCoreAsync(
            Guid userId, IReadOnlyCollection<Guid> notificationIds, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<int>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            return await mRepository.MarkReadBulkAsync(userId, notificationIds, cancellation);
        }

        public async Task<Result<bool>> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellation = default) =>
            (await MarkReadCoreAsync(userId, notificationId, cancellation)).LogWith(mLogger, "MarkRead", ("UserId", userId));

        private async Task<Result<bool>> MarkReadCoreAsync(Guid userId, Guid notificationId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty || notificationId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "userId/notificationId required");
            }

            Result<bool> marked = await mRepository.MarkReadAsync(userId, notificationId, cancellation);
            if (marked.IsError)
            {
                return marked;
            }

            mLogger.InfoWith("Notification marked as read", ("UserId", userId));

            if (!marked.Value)
            {
                return Result<bool>.Error(ErrorCode.NotFound, "notification not found");
            }

            return Result<bool>.Success(true);
        }
    }
}
