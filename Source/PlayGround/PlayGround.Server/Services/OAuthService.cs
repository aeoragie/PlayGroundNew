using PlayGround.Shared.Time;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PlayGround.Server.Services
{
    /// <summary>소셜 OAuth(authorization-code) 처리 — 인증 URL 생성 + 코드→사용자정보 교환.</summary>
    public class OAuthService
    {
        private readonly IHttpClientFactory mHttpClientFactory;
        private readonly IConfiguration mConfiguration;

        public OAuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            mHttpClientFactory = httpClientFactory;
            mConfiguration = configuration;
        }

        public bool IsSupported(string provider) =>
            provider.ToLowerInvariant() is "google" or "kakao" or "line";

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

            OAuthProviderOptions? options = mConfiguration.GetSection($"OAuth:{key}").Get<OAuthProviderOptions>();
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
            return mConfiguration.GetSection($"OAuth:{providerKey}").Get<OAuthProviderOptions>()
                ?? throw new InvalidOperationException($"OAuth:{providerKey} is not configured.");
        }

        //.// 연결(link) 모드 상태 — 현재 로그인 사용자를 리다이렉트 왕복 동안 서명해 실어 보낸다.
        // 콜백은 state가 유효한 링크 토큰이면 연결 모드(그 UserId에 붙임), 아니면 로그인 모드로 분기한다.

        /// <summary>연결 모드 서명 상태 생성 — "link|{userId}|{만료(unix)}" + HMAC(Jwt:Key). 15분 유효.</summary>
        public string CreateLinkState(Guid userId)
        {
            long expiry = SystemTime.OffsetNow.AddMinutes(15).ToUnixTimeSeconds();
            string payload = $"link|{userId:N}|{expiry}";
            return $"{ToBase64Url(Encoding.UTF8.GetBytes(payload))}.{ToBase64Url(SignPayload(payload))}";
        }

        /// <summary>연결 모드 상태 검증 — 서명·만료 확인 후 UserId 추출. 로그인 상태(랜덤 hex)면 false.</summary>
        public bool TryReadLinkState(string? state, out Guid userId)
        {
            userId = Guid.Empty;
            if (string.IsNullOrEmpty(state))
            {
                return false;
            }

            string[] parts = state.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            string payload;
            try
            {
                payload = Encoding.UTF8.GetString(FromBase64Url(parts[0]));
            }
            catch
            {
                return false;
            }

            if (!CryptographicOperations.FixedTimeEquals(FromBase64Url(parts[1]), SignPayload(payload)))
            {
                return false;
            }

            string[] fields = payload.Split('|');
            if (fields.Length != 3 || fields[0] != "link")
            {
                return false;
            }

            if (!long.TryParse(fields[2], out long expiry) || SystemTime.OffsetNow.ToUnixTimeSeconds() > expiry)
            {
                return false;
            }

            return Guid.TryParseExact(fields[1], "N", out userId);
        }

        private byte[] SignPayload(string payload)
        {
            string key = mConfiguration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is not configured (appsettings.Local.json).");
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(key));
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        }

        private static string ToBase64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] FromBase64Url(string value)
        {
            string s = value.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2:
                    s += "==";
                    break;
                case 3:
                    s += "=";
                    break;
            }
            return Convert.FromBase64String(s);
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
            var client = mHttpClientFactory.CreateClient();
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
            var client = mHttpClientFactory.CreateClient();
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
            var client = mHttpClientFactory.CreateClient();
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
