using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PlayGround.Server.Services
{
    /// <summary>소셜 OAuth(authorization-code) 처리. provider 인증 URL 생성 + 코드→사용자정보 교환.
    /// 엔드포인트는 설정(OAuth:{Provider})에서 읽는다.</summary>
    public class OAuthService
    {
        private readonly IHttpClientFactory HttpClientFactory;
        private readonly IConfiguration Configuration;

        public OAuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            HttpClientFactory = httpClientFactory;
            Configuration = configuration;
        }

        public bool IsSupported(string provider) =>
            provider.ToLowerInvariant() is "google" or "kakao" or "line";

        /// <summary>자격증명(ClientId)이 설정되어 실제 로그인이 가능한 provider인지.</summary>
        public bool IsConfigured(string provider)
        {
            string key = provider.ToLowerInvariant() switch
            {
                "google" => "Google",
                "kakao" => "Kakao",
                "line" => "Line",
                _ => string.Empty
            };
            if (key.Length == 0)
            {
                return false;
            }

            OAuthProviderOptions? options = Configuration.GetSection($"OAuth:{key}").Get<OAuthProviderOptions>();
            return !string.IsNullOrWhiteSpace(options?.ClientId);
        }

        public string GetAuthorizationUrl(string provider, string state) => provider.ToLowerInvariant() switch
        {
            "google" => BuildUrl("Google", state, extra: "&access_type=offline"),
            "kakao" => BuildUrl("Kakao", state),
            "line" => BuildUrl("Line", state),
            _ => throw new ArgumentException($"Unsupported provider: {provider}")
        };

        public async Task<OAuthUserInfo?> GetUserInfoAsync(string provider, string code) => provider.ToLowerInvariant() switch
        {
            "google" => await GetGoogleUserInfoAsync(code),
            "kakao" => await GetKakaoUserInfoAsync(code),
            "line" => await GetLineUserInfoAsync(code),
            _ => throw new ArgumentException($"Unsupported provider: {provider}")
        };

        private OAuthProviderOptions Options(string providerKey)
        {
            return Configuration.GetSection($"OAuth:{providerKey}").Get<OAuthProviderOptions>()
                ?? throw new InvalidOperationException($"OAuth:{providerKey} is not configured.");
        }

        private string BuildUrl(string providerKey, string state, string extra = "")
        {
            var options = Options(providerKey);
            var authEndpoint = options.AuthorizationEndpoint
                ?? throw new InvalidOperationException($"OAuth:{providerKey}:AuthorizationEndpoint is not configured (appsettings.json).");
            if (string.IsNullOrWhiteSpace(options.ClientId))
            {
                throw new InvalidOperationException($"OAuth:{providerKey}:ClientId is not configured (appsettings.Local.json).");
            }

            var url = $"{authEndpoint}?client_id={Uri.EscapeDataString(options.ClientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(options.RedirectUri ?? string.Empty)}" +
                      $"&response_type=code&state={Uri.EscapeDataString(state)}";
            if (!string.IsNullOrWhiteSpace(options.Scope))
            {
                url += $"&scope={Uri.EscapeDataString(options.Scope)}";
            }
            return url + extra;
        }

        private async Task<string?> ExchangeCodeAsync(OAuthProviderOptions options, string code)
        {
            var client = HttpClientFactory.CreateClient();
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = options.ClientId ?? string.Empty,
                ["redirect_uri"] = options.RedirectUri ?? string.Empty,
                ["code"] = code
            };
            if (!string.IsNullOrWhiteSpace(options.ClientSecret))
            {
                form["client_secret"] = options.ClientSecret;
            }

            var response = await client.PostAsync(options.TokenEndpoint, new FormUrlEncodedContent(form));
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.TryGetProperty("access_token", out var token) ? token.GetString() : null;
        }

        private async Task<JsonElement?> GetUserJsonAsync(string userEndpoint, string accessToken)
        {
            var client = HttpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetAsync(userEndpoint);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private async Task<OAuthUserInfo?> GetGoogleUserInfoAsync(string code)
        {
            var options = Options("Google");
            var accessToken = await ExchangeCodeAsync(options, code);
            if (accessToken is null)
            {
                return null;
            }

            var user = await GetUserJsonAsync(options.UserInfoEndpoint!, accessToken);
            if (user is null)
            {
                return null;
            }

            var u = user.Value;
            return new OAuthUserInfo
            {
                Provider = "Google",
                ProviderUserId = u.GetProperty("id").GetString() ?? string.Empty,
                Email = u.TryGetProperty("email", out var email) ? email.GetString() : null,
                FullName = u.TryGetProperty("name", out var name) ? name.GetString() : null,
                ProfileImageUrl = u.TryGetProperty("picture", out var picture) ? picture.GetString() : null
            };
        }

        private async Task<OAuthUserInfo?> GetKakaoUserInfoAsync(string code)
        {
            var options = Options("Kakao");
            var accessToken = await ExchangeCodeAsync(options, code);
            if (accessToken is null)
            {
                return null;
            }

            var user = await GetUserJsonAsync(options.UserInfoEndpoint!, accessToken);
            if (user is null)
            {
                return null;
            }

            var u = user.Value;
            string? email = null, fullName = null, profileImageUrl = null;
            if (u.TryGetProperty("kakao_account", out var account))
            {
                email = account.TryGetProperty("email", out var e) ? e.GetString() : null;
                if (account.TryGetProperty("profile", out var profile))
                {
                    fullName = profile.TryGetProperty("nickname", out var n) ? n.GetString() : null;
                    profileImageUrl = profile.TryGetProperty("profile_image_url", out var p) ? p.GetString() : null;
                }
            }

            return new OAuthUserInfo
            {
                Provider = "Kakao",
                ProviderUserId = u.GetProperty("id").GetInt64().ToString(),
                Email = email,
                FullName = fullName,
                ProfileImageUrl = profileImageUrl
            };
        }

        private async Task<OAuthUserInfo?> GetLineUserInfoAsync(string code)
        {
            // LINE Login(OAuth 2.1): 토큰 응답의 id_token(JWT)에 프로필·이메일이 담긴다.
            var options = Options("Line");
            var client = HttpClientFactory.CreateClient();
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = options.RedirectUri ?? string.Empty,
                ["client_id"] = options.ClientId ?? string.Empty,
                ["client_secret"] = options.ClientSecret ?? string.Empty
            };

            var response = await client.PostAsync(options.TokenEndpoint, new FormUrlEncodedContent(form));
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var idToken = json.TryGetProperty("id_token", out var it) ? it.GetString() : null;
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return null;
            }

            // id_token은 LINE 토큰 엔드포인트에서 TLS로 직접 받은 신뢰 토큰 — 서명 검증 없이 클레임만 읽는다.
            var claims = new JwtSecurityTokenHandler().ReadJwtToken(idToken);
            var sub = claims.Subject;
            if (string.IsNullOrWhiteSpace(sub))
            {
                return null;
            }

            return new OAuthUserInfo
            {
                Provider = "Line",
                ProviderUserId = sub,
                Email = claims.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                FullName = claims.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
                ProfileImageUrl = claims.Claims.FirstOrDefault(c => c.Type == "picture")?.Value
            };
        }
    }

    public class OAuthUserInfo
    {
        public string Provider { get; set; } = string.Empty;
        public string ProviderUserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
