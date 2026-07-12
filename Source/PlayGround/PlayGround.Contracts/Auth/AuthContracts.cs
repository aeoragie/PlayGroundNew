namespace PlayGround.Contracts.Auth
{
    /// <summary>인증 결과 — 액세스 토큰 + 사용자 요약(민감정보 제외).</summary>
    public class AuthResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public AuthUserDto User { get; set; } = new();
    }

    /// <summary>클라이언트에 노출되는 사용자 정보 (PasswordHash 미포함).</summary>
    public class AuthUserDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
    }
}
