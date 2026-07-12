using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PlayGround.Server.Services;
using Xunit;

namespace PlayGround.Tests.Integration
{
    public class JwtTokenServiceTests
    {
        private const string TestKey = "test-signing-key-at-least-32-bytes-long-000";
        private const string Issuer = "playground-test";
        private const string Audience = "playground-client";

        private static JwtTokenService CreateService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = TestKey,
                    ["Jwt:Issuer"] = Issuer,
                    ["Jwt:Audience"] = Audience,
                    ["Jwt:AccessTokenExpirationMinutes"] = "30"
                })
                .Build();
            return new JwtTokenService(config);
        }

        [Fact]
        public void GenerateAccessToken_EmbedsClaims_AndValidates()
        {
            var service = CreateService();
            var userId = Guid.NewGuid();

            var token = service.GenerateAccessToken(userId, "user@test.com", "테스터", "General", null);

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey))
            };

            var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParams, out _);

            // 핸들러 기본 인바운드 매핑: sub → NameIdentifier, email → Email (JWT Bearer 미들웨어도 동일)
            Assert.Equal(userId.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal("user@test.com", principal.FindFirst(ClaimTypes.Email)?.Value);
            Assert.Equal("General", principal.FindFirst(ClaimTypes.Role)?.Value);
            Assert.Equal("테스터", principal.FindFirst("name")?.Value);
        }

        [Fact]
        public void ValidateToken_WrongKey_Throws()
        {
            var token = CreateService().GenerateAccessToken(Guid.NewGuid(), "u@test.com", "n", "General", null);

            var wrongParams = new TokenValidationParameters
            {
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-completely-different-signing-key-000000"))
            };

            Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(
                () => new JwtSecurityTokenHandler().ValidateToken(token, wrongParams, out _));
        }

        [Fact]
        public void GenerateRefreshToken_IsRandomAndHashable()
        {
            var service = CreateService();

            var a = service.GenerateRefreshToken();
            var b = service.GenerateRefreshToken();

            Assert.NotEqual(a, b);
            Assert.Equal(service.HashToken(a), service.HashToken(a));   // 결정적
            Assert.NotEqual(service.HashToken(a), service.HashToken(b));
        }

        [Fact]
        public void GenerateAccessToken_MissingKey_Throws()
        {
            var config = new ConfigurationBuilder().Build();
            var service = new JwtTokenService(config);

            Assert.Throws<InvalidOperationException>(
                () => service.GenerateAccessToken(Guid.NewGuid(), "u@test.com", "n", "General", null));
        }
    }
}
