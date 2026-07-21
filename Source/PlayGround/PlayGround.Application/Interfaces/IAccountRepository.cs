using PlayGround.Shared.Result;
using PlayGround.Contracts.Settings;
using PlayGround.Application.Auth.Models;

namespace PlayGround.Application.Interfaces
{
    /// <summary>계정(Account DB) 조회·생성 포트 (Persistence에서 구현).
    /// 조회는 미존재 시 Success(null) — 에러가 아니다.</summary>
    public interface IAccountRepository
    {
        Task<Result<AccountUser?>> GetByEmailAsync(string email, CancellationToken cancellation = default);

        Task<Result<AccountUser?>> GetBySocialAsync(string provider, string providerUserId, CancellationToken cancellation = default);

        Task<Result<AccountUser>> CreateByEmailAsync(string email, string passwordHash, string displayName, CancellationToken cancellation = default);

        Task<Result<AccountUser>> CreateWithSocialAsync(string email, string displayName, string provider, string providerUserId, string? profileImageUrl, CancellationToken cancellation = default);

        /// <summary>역할 변경 후 갱신된 사용자를 반환 (JWT 재발급용).</summary>
        Task<Result<AccountUser>> UpdateRoleAsync(Guid userId, string role, CancellationToken cancellation = default);

        /// <summary>계정 설정 묶음 조회 (마스킹 적용). 미존재·삭제 계정은 Success(null).</summary>
        Task<Result<AccountSettingsResponse?>> GetSettingsAsync(Guid userId, CancellationToken cancellation = default);

        /// <summary>알림 설정 조회 — 6개 항목 전부(저장값 없는 항목은 기본값).</summary>
        Task<Result<NotificationPreferencesResponse>> GetNotificationPreferencesAsync(Guid userId, CancellationToken cancellation = default);

        /// <summary>알림 설정 업서트. 사용자 미존재 시 Success(false).</summary>
        Task<Result<bool>> SetNotificationPreferenceAsync(Guid userId, string itemName, bool isEnabled, CancellationToken cancellation = default);

        /// <summary>계정 소프트 삭제. 이미 삭제·미존재면 Success(false).</summary>
        Task<Result<bool>> SoftDeleteAsync(Guid userId, CancellationToken cancellation = default);

        /// <summary>여러 사용자의 특정 알림 항목 저장값 — **저장 행이 있는 사용자만** 담긴다
        /// (없는 사용자는 호출측이 enum 기본값 적용). 알림 발송 전 수신 설정 필터용.</summary>
        Task<Result<Dictionary<Guid, bool>>> GetNotificationStatesAsync(IReadOnlyCollection<Guid> userIds, string itemName, CancellationToken cancellation = default);
    }
}
