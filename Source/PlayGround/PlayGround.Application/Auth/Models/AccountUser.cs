using PlayGround.Domain.Account;

namespace PlayGround.Application.Auth.Models
{
    /// <summary>인증용 사용자 모델 (서버 내부 전용). PasswordHash 포함 — 클라이언트로 노출 금지.</summary>
    public sealed class AccountUser
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public bool EmailConfirmed { get; init; }
        public string? PasswordHash { get; init; }
        public AccountAuthProvider AuthProvider { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string? ProfileImageUrl { get; init; }
        public AccountRole UserRole { get; init; }
        public string UserStatus { get; init; } = string.Empty;
    }
}
