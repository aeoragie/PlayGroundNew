using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Notification;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Notification.Commands
{
    /// <summary>알림 센터 유즈케이스 — 목록(미읽음 카운트 포함)·읽음 처리.</summary>
    public class SoccerNotificationCommand
    {
        private readonly INotificationRepository mRepository;

        public SoccerNotificationCommand(INotificationRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<NotificationsResponse>> GetAsync(Guid userId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<NotificationsResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            return await mRepository.GetByUserAsync(userId, cancellation);
        }

        public async Task<Result<bool>> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellation = default)
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

            if (!marked.Value)
            {
                return Result<bool>.Error(ErrorCode.NotFound, "notification not found");
            }

            return Result<bool>.Success(true);
        }
    }
}
