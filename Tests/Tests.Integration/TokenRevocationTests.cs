using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PlayGround.Application.Interfaces;
using PlayGround.Server.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace PlayGround.Tests.Integration
{
    /// <summary>
    /// 토큰 무효화의 **판정 규칙**. Redis 자체는 여기서 다루지 않고(H0 배선은 통합 환경에서 확인),
    /// 무효화가 성립하려면 토큰이 갖춰야 할 것과 저장소가 없을 때의 동작을 고정한다.
    /// </summary>
    public class TokenRevocationTests
    {
        private const string TestKey = "test-signing-key-at-least-32-bytes-long-000";

        private static JwtTokenService CreateTokenService(int expirationMinutes = 30)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = TestKey,
                    ["Jwt:Issuer"] = "playground-test",
                    ["Jwt:Audience"] = "playground-client",
                    ["Jwt:AccessTokenExpirationMinutes"] = expirationMinutes.ToString(),
                })
                .Build();

            return new JwtTokenService(config);
        }

        private static JwtSecurityToken Decode(string token) =>
            new JwtSecurityTokenHandler().ReadJwtToken(token);

        //.// 무효화에 필요한 클레임

        [Fact]
        public void Token_CarriesJti()
        {
            // 로그아웃은 "이 토큰 하나"를 지목해야 하므로 식별자가 없으면 성립하지 않는다
            JwtSecurityToken token = Decode(
                CreateTokenService().GenerateAccessToken(Guid.NewGuid(), "a@b.com", "테스터", "General", null));

            string? jti = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

            jti.Should().NotBeNullOrWhiteSpace();
            Guid.TryParse(jti, out _).Should().BeTrue();
        }

        [Fact]
        public void Jti_DiffersPerToken()
        {
            JwtTokenService service = CreateTokenService();
            var userId = Guid.NewGuid();

            string[] ids = Enumerable.Range(0, 5)
                .Select(_ => Decode(service.GenerateAccessToken(userId, "a@b.com", "테스터", "General", null)))
                .Select(t => t.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value)
                .ToArray();

            ids.Should().OnlyHaveUniqueItems("같은 jti면 한 기기 로그아웃이 다른 기기까지 끊는다");
        }

        [Fact]
        public void Token_CarriesIat()
        {
            // 탈퇴 시 "이 시각 이전 토큰 전부 무효" 판정이 iat에 기댄다.
            // JwtSecurityToken이 자동으로 넣어 주지 않으므로 회귀하기 쉬운 지점이다.
            JwtSecurityToken token = Decode(
                CreateTokenService().GenerateAccessToken(Guid.NewGuid(), "a@b.com", "테스터", "General", null));

            string? iat = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iat)?.Value;

            iat.Should().NotBeNullOrWhiteSpace();
            long.TryParse(iat, out long seconds).Should().BeTrue();

            DateTimeOffset issuedAt = DateTimeOffset.FromUnixTimeSeconds(seconds);
            issuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void Token_CarriesExp()
        {
            // 무효화 기록은 토큰 만료까지만 들고 있으면 된다 — exp가 그 보관 기간을 정한다
            JwtSecurityToken token = Decode(
                CreateTokenService(expirationMinutes: 30)
                    .GenerateAccessToken(Guid.NewGuid(), "a@b.com", "테스터", "General", null));

            token.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void LaterToken_HasGreaterOrEqualIat()
        {
            // 탈퇴 기준선 비교가 성립하려면 발급 순서가 iat에 반영돼야 한다
            JwtTokenService service = CreateTokenService();
            var userId = Guid.NewGuid();

            long first = IssuedAtOf(service.GenerateAccessToken(userId, "a@b.com", "테스터", "General", null));
            long second = IssuedAtOf(service.GenerateAccessToken(userId, "a@b.com", "테스터", "Player", null));

            second.Should().BeGreaterThanOrEqualTo(first);
        }

        private static long IssuedAtOf(string token) =>
            long.Parse(Decode(token).Claims.First(c => c.Type == JwtRegisteredClaimNames.Iat).Value);

        //.// 검증 훅 — 무효화된 토큰만 떨군다

        [Fact]
        public async Task TokenPasses_WhenStoreUnavailable()
        {
            // 미등록(로컬 개발 등)에서 인증이 통째로 막히면 안 된다
            TokenValidatedContext context = BuildContext(store: null);

            await TokenRevocationCheck.OnTokenValidatedAsync(context);

            context.Result?.Succeeded.Should().NotBe(false);
        }

        [Fact]
        public async Task NonRevokedToken_Passes()
        {
            var store = new Mock<ITokenRevocationStore>();
            store.Setup(s => s.IsRevokedAsync(
                    It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            TokenValidatedContext context = BuildContext(store.Object);

            await TokenRevocationCheck.OnTokenValidatedAsync(context);

            context.Result?.Succeeded.Should().NotBe(false);
        }

        [Fact]
        public async Task RevokedToken_IsRejected()
        {
            var store = new Mock<ITokenRevocationStore>();
            store.Setup(s => s.IsRevokedAsync(
                    It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            TokenValidatedContext context = BuildContext(store.Object);

            await TokenRevocationCheck.OnTokenValidatedAsync(context);

            context.Result!.Succeeded.Should().BeFalse();
            context.Result.Failure.Should().NotBeNull();
        }

        [Fact]
        public async Task PassesJtiUserAndIssuedAt_ToStore()
        {
            // 셋 중 하나라도 빠지면 판정이 헐거워진다(예: iat 없이는 탈퇴 기준선을 못 쓴다)
            var userId = Guid.NewGuid();
            string token = CreateTokenService().GenerateAccessToken(userId, "a@b.com", "테스터", "General", null);
            JwtSecurityToken decoded = Decode(token);
            string jti = decoded.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            long iat = long.Parse(decoded.Claims.First(c => c.Type == JwtRegisteredClaimNames.Iat).Value);

            var store = new Mock<ITokenRevocationStore>();
            store.Setup(s => s.IsRevokedAsync(
                    It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            await TokenRevocationCheck.OnTokenValidatedAsync(BuildContext(store.Object, decoded, userId));

            store.Verify(s => s.IsRevokedAsync(
                jti, userId, DateTimeOffset.FromUnixTimeSeconds(iat), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>검증 통과 직후 상태를 흉내 낸다 — principal은 토큰의 클레임으로 만든다.</summary>
        private static TokenValidatedContext BuildContext(
            ITokenRevocationStore? store, JwtSecurityToken? token = null, Guid? userId = null)
        {
            var services = new ServiceCollection();
            if (store is not null)
            {
                services.AddSingleton(store);
            }

            var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

            token ??= Decode(CreateTokenService()
                .GenerateAccessToken(userId ?? Guid.NewGuid(), "a@b.com", "테스터", "General", null));

            var context = new TokenValidatedContext(
                http,
                new AuthenticationScheme("Bearer", "Bearer", typeof(JwtBearerHandler)),
                new JwtBearerOptions())
            {
                Principal = new ClaimsPrincipal(new ClaimsIdentity(token.Claims, "Bearer")),
                SecurityToken = token,
            };

            return context;
        }
    }
}
