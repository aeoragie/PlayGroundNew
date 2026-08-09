using Microsoft.IdentityModel.Tokens;
using PlayGround.Application.Interfaces;
using PlayGround.Shared.Time;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PlayGround.Server.Services
{
    /// <summary>JWT 발급·리프레시 토큰 생성. 설정은 Jwt 섹션(appsettings.Local.json).</summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration mConfiguration;

        public JwtTokenService(IConfiguration configuration)
        {
            mConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string GenerateAccessToken(Guid userId, string email, string displayName, string role, string? avatarUrl)
        {
            Debug.Assert(userId != Guid.Empty, "UserId cannot be empty");
            Debug.Assert(!string.IsNullOrWhiteSpace(email), "Email cannot be null or empty");

            var key = mConfiguration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("Jwt:Key is not configured (appsettings.Local.json).");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var expirationMinutes = mConfiguration.GetValue("Jwt:AccessTokenExpirationMinutes", 30);

            // iat는 JwtSecurityToken이 자동으로 넣지 않는다 — 탈퇴 시 "이 시각 이전 토큰 전부 무효"
            // 판정(ITokenRevocationStore)이 이 값에 기대므로 명시적으로 담는다.
            var issuedAt = SystemTime.OffsetNow;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new("name", displayName),
                new(ClaimTypes.Role, role),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat,
                    issuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                    ClaimValueTypes.Integer64)
            };

            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                claims.Add(new Claim("avatar", avatarUrl));
            }

            var token = new JwtSecurityToken(
                issuer: mConfiguration["Jwt:Issuer"],
                audience: mConfiguration["Jwt:Audience"],
                claims: claims,
                expires: SystemTime.Now.AddMinutes(expirationMinutes).UtcDateTime,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public string HashToken(string token)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(token), "Token cannot be null or empty");

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}
