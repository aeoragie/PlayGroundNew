namespace PlayGround.Application.Interfaces
{
    /// <summary>JWT 발급·리프레시 토큰 포트 (Server에서 구현).</summary>
    public interface IJwtTokenService
    {
        string GenerateAccessToken(Guid userId, string email, string displayName, string role, string? avatarUrl);

        string GenerateRefreshToken();

        /// <summary>토큰 해시 (저장용 — 원문은 저장하지 않는다).</summary>
        string HashToken(string token);
    }
}
