namespace PlayGround.Application.Auth.Models
{
    /// <summary>
    /// 인증용 사용자 모델 (서버 내부 전용). PasswordHash 포함 — 클라이언트로 노출 금지.
    /// (Persistence의 생성 UserRecord를 Application 계층으로 매핑한 형태)
    /// </summary>
    public sealed class AccountUser
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public bool EmailConfirmed { get; init; }
        public string? PasswordHash { get; init; }
        public string AuthProvider { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string? ProfileImageUrl { get; init; }
        public string UserRole { get; init; } = string.Empty;
        public string UserStatus { get; init; } = string.Empty;
    }
}
