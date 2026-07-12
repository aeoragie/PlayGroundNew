namespace PlayGround.Server.Services
{
    /// <summary>provider별 OAuth 설정. 시크릿은 appsettings.Local.json, 엔드포인트는 appsettings.json에서 병합.</summary>
    public sealed class OAuthProviderOptions
    {
        // 시크릿 (Local)
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? RedirectUri { get; set; }

        // 엔드포인트·스코프 (appsettings.json)
        public string? AuthorizationEndpoint { get; set; }
        public string? TokenEndpoint { get; set; }
        public string? UserInfoEndpoint { get; set; }
        public string? Scope { get; set; }
    }
}
