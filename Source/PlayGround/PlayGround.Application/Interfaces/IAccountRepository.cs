using PlayGround.Shared.Result;
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

        Task<Result> UpdateRoleAsync(Guid userId, string role, CancellationToken cancellation = default);
    }
}
