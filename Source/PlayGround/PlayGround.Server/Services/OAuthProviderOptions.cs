namespace PlayGround.Server.Services
{
    /// <summary>
    /// provider별 OAuth 설정. 엔드포인트·Scope는 커밋되는 appsettings.json,
    /// ClientId·ClientSecret·RedirectUri는 gitignore된 appsettings.Local.json에서 병합된다.
    /// </summary>
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
