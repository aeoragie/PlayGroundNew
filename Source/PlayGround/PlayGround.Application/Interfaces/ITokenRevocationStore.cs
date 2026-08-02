namespace PlayGround.Application.Interfaces
{
    /// <summary>
    /// 발급된 액세스 토큰을 만료 전에 무효화하는 저장소.
    ///
    /// JWT는 서명만 맞으면 유효하므로 **로그아웃·탈퇴 뒤에도 남은 수명 동안 계속 통한다.**
    /// 이걸 막으려면 서버가 "무효" 상태를 따로 기억해야 한다(설계 결정: Redis).
    ///
    /// 두 가지 무효화가 필요하고 대상이 다르다:
    /// <list type="bullet">
    /// <item>로그아웃 — **그 토큰 하나만**(다른 기기는 유지). 토큰 식별자(jti)로 지운다.</item>
    /// <item>탈퇴 — **그 사용자의 전부**. 발급 시각 기준선으로 자른다.</item>
    /// </list>
    ///
    /// 역할 승격은 여기 없다 — 새 토큰이 상위 권한이고 기존 토큰은 하위 권한이라 위험하지 않다
    /// (강등 경로가 생기면 그때 탈퇴와 같은 방식으로 무효화한다).
    /// </summary>
    public interface ITokenRevocationStore
    {
        /// <summary>토큰 하나를 무효화한다(로그아웃). 보관은 토큰 만료까지면 충분하다.</summary>
        Task RevokeTokenAsync(string tokenId, DateTimeOffset expiresAt, CancellationToken cancellation = default);

        /// <summary>이 시각 이전에 발급된 해당 사용자의 토큰을 모두 무효화한다(탈퇴).</summary>
        Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellation = default);

        /// <summary>무효화된 토큰인지. **저장소를 못 쓰면 true를 반환하지 않는다** —
        /// 여기서 막아 버리면 Redis 장애가 전체 로그인 불가로 번진다(구현 주석 참조).</summary>
        Task<bool> IsRevokedAsync(
            string tokenId, Guid userId, DateTimeOffset issuedAt, CancellationToken cancellation = default);
    }
}
