using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace PlayGround.Client.Services.Auth
{
    /// <summary>JWT 기반 인증 상태 공급자.
    /// localStorage의 액세스 토큰을 읽어 ClaimsPrincipal을 구성하고,
    /// 공유 HttpClient에 Bearer 헤더를 부착한다.</summary>
    public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState Anonymous =
            new(new ClaimsPrincipal(new ClaimsIdentity()));

        private readonly TokenStore mTokenStore;
        private readonly HttpClient mHttp;
        private readonly ILogger<JwtAuthenticationStateProvider> mLogger;

        public JwtAuthenticationStateProvider(
            TokenStore tokenStore,
            HttpClient http,
            ILogger<JwtAuthenticationStateProvider> logger)
        {
            ArgumentNullException.ThrowIfNull(tokenStore);
            ArgumentNullException.ThrowIfNull(http);
            ArgumentNullException.ThrowIfNull(logger);
            Debug.Assert(tokenStore is not null && http is not null && logger is not null);
            mTokenStore = tokenStore;
            mHttp = http;
            mLogger = logger;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? token = await mTokenStore.GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                ApplyAuthorizationHeader(null);
                return Anonymous;
            }

            List<Claim> claims = ParseClaimsFromJwt(token);
            if (IsExpired(claims))
            {
                mLogger.LogInformation("Stored token expired — signing out. {{ Reason:Expired }}");
                await mTokenStore.ClearTokenAsync();
                ApplyAuthorizationHeader(null);
                return Anonymous;
            }

            ApplyAuthorizationHeader(token);
            mLogger.LogInformation("Authentication state restored from token. {{ Role:{Role} }}", RoleOf(claims));
            return new AuthenticationState(BuildPrincipal(claims));
        }

        /// <summary>로그인 성공 — 토큰을 저장하고 인증 상태를 갱신한다.</summary>
        public async Task MarkUserAuthenticatedAsync(string accessToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
            Debug.Assert(!string.IsNullOrWhiteSpace(accessToken));

            await mTokenStore.SaveTokenAsync(accessToken);
            ApplyAuthorizationHeader(accessToken);
            List<Claim> claims = ParseClaimsFromJwt(accessToken);
            mLogger.LogInformation("User signed in. {{ Role:{Role} }}", RoleOf(claims));
            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(BuildPrincipal(claims))));
        }

        /// <summary>로그아웃 — 토큰을 삭제하고 익명 상태로 전환한다.</summary>
        public async Task MarkUserLoggedOutAsync()
        {
            await mTokenStore.ClearTokenAsync();
            ApplyAuthorizationHeader(null);
            mLogger.LogInformation("User signed out.");
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
        }

        private void ApplyAuthorizationHeader(string? token)
        {
            mHttp.DefaultRequestHeaders.Authorization =
                string.IsNullOrWhiteSpace(token)
                    ? null
                    : new AuthenticationHeaderValue("Bearer", token);
        }

        private static ClaimsPrincipal BuildPrincipal(List<Claim> claims)
        {
            // nameType="name", roleType=ClaimTypes.Role — 서버 발급 클레임과 일치시킨다.
            var identity = new ClaimsIdentity(claims, "jwt", "name", ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }

        private static string RoleOf(List<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "(none)";
        }

        private static bool IsExpired(List<Claim> claims)
        {
            Claim? exp = claims.FirstOrDefault(c => c.Type == "exp");
            if (exp is null || !long.TryParse(exp.Value, out long seconds))
            {
                return false; // exp가 없으면 만료 판단 불가 — 유효로 취급
            }

            DateTimeOffset expiresAt = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return expiresAt <= DateTimeOffset.UtcNow;
        }

        private static List<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            string[] parts = jwt.Split('.');
            if (parts.Length < 2)
            {
                return claims;
            }

            byte[] payload = DecodeBase64Url(parts[1]);
            Dictionary<string, JsonElement>? map =
                JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload);
            if (map is null)
            {
                return claims;
            }

            foreach (KeyValuePair<string, JsonElement> pair in map)
            {
                string type = NormalizeClaimType(pair.Key);
                if (pair.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in pair.Value.EnumerateArray())
                    {
                        claims.Add(new Claim(type, item.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(type, pair.Value.ToString()));
                }
            }

            return claims;
        }

        // 서버가 발급한 role 클레임 키(짧은 "role"/"roles" 또는 ClaimTypes.Role 긴 URI)를
        // 표준 Role 타입으로 통일 — AuthorizeView Roles / IsInRole 이 동작하도록.
        private static string NormalizeClaimType(string key)
        {
            return key switch
            {
                "role" or "roles" => ClaimTypes.Role,
                _ => key
            };
        }

        private static byte[] DecodeBase64Url(string value)
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
    }
}
