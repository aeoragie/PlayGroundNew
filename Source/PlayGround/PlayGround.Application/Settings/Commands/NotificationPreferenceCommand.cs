using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Settings;
using PlayGround.Domain.Account;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Settings.Commands
{
    /// <summary>알림 설정 조회·변경 유즈케이스.
    /// 변경은 NotificationPreferenceItem enum이 화이트리스트다 — 승인형(연결 요청·열람 요청)은
    /// enum에 없으므로 어떤 이름으로 보내도 InvalidInput. 서버가 "항상 켜짐"을 강제하는 지점.</summary>
    public class NotificationPreferenceCommand
    {
        private readonly IAccountRepository mRepository;

        public NotificationPreferenceCommand(IAccountRepository repository)
        {
            Debug.Assert(repository != null);
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<NotificationPreferencesResponse>> GetAsync(Guid userId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<NotificationPreferencesResponse>.Error(ErrorCode.InvalidInput, "userId required");
            }

            return await mRepository.GetNotificationPreferencesAsync(userId, cancellation);
        }

        public async Task<Result<bool>> SetAsync(Guid userId, SetNotificationPreferenceRequest request, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty || request is null)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "userId/request required");
            }

            // enum 멤버 이름과 정확히 일치할 때만 저장 — 승인형·미지의 이름은 여기서 끝난다
            if (!Enum.TryParse(request.ItemName, ignoreCase: false, out NotificationPreferenceItem item)
                || item.ToString() != request.ItemName)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "unknown or locked notification item");
            }

            Result<bool> saved = await mRepository.SetNotificationPreferenceAsync(userId, item.ToString(), request.IsEnabled, cancellation);
            if (saved.IsError)
            {
                return saved;
            }

            if (!saved.Value)
            {
                return Result<bool>.Error(ErrorCode.NotFound, "user not found");
            }

            return Result<bool>.Success(true);
        }
    }
}
