using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PlayGround.Application.Interfaces;

namespace PlayGround.Server.Services
{
    /// <summary>JWT 발급·리프레시 토큰 생성. 설정은 Jwt 섹션(appsettings.Local.json).</summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration Configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string GenerateAccessToken(Guid userId, string email, string displayName, string role, string? avatarUrl)
        {
            Debug.Assert(userId != Guid.Empty, "UserId cannot be empty");
            Debug.Assert(!string.IsNullOrWhiteSpace(email), "Email cannot be null or empty");

            var key = Configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("Jwt:Key is not configured (appsettings.Local.json).");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var expirationMinutes = Configuration.GetValue("Jwt:AccessTokenExpirationMinutes", 30);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new("name", displayName),
                new(ClaimTypes.Role, role),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                claims.Add(new Claim("avatar", avatarUrl));
            }

            var token = new JwtSecurityToken(
                issuer: Configuration["Jwt:Issuer"],
                audience: Configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
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
