using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PlayGround.Infrastructure.Store;
using PlayGround.Infrastructure.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Auth.Commands;
using PlayGround.Application.Settings.Commands;
using PlayGround.Persistence;
using PlayGround.Server.Services;

namespace PlayGround.Server.DependencyInjection
{
    /// <summary>인증(공유 — 종목 무관): 소셜·이메일 로그인, 비밀번호 해시, JWT 발급·검증.</summary>
    public static class AuthModule
    {
        private static readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAccountPersistence();

            //.// 소셜 OAuth
            services.AddHttpClient();
            services.AddScoped<OAuthService>();
            services.AddScoped<LoginBySocialCommand>();

            //.// 이메일 로그인/가입 + 비밀번호 해시
            services.AddSingleton<IPasswordHasher, PasswordHasherService>();
            services.AddScoped<LoginByEmailCommand>();

            //.// 계정 설정 (설정 화면 — 계정·알림 탭 + 계정 삭제 + 이름 변경 + 로그인 수단 연결/해제)
            services.AddScoped<AccountSettingsCommand>();
            services.AddScoped<NotificationPreferenceCommand>();
            services.AddScoped<AccountDeleteCommand>();
            services.AddScoped<DisplayNameChangeCommand>();
            services.AddScoped<SocialLinkCommand>();

            //.// 서버측 임시 상태 저장소 (Redis) — 토큰 무효화가 여기 얹힌다.
            // RedisService는 IHostedService라 기동 시 연결하고, 설정이 없으면 경고만 남기고 논다.
            services.AddSingleton<RedisService>();
            services.AddHostedService(sp => sp.GetRequiredService<RedisService>());
            services.AddSingleton<ITokenRevocationStore, RedisTokenRevocationStore>();

            //.// JWT 발급 + Bearer 검증
            string? signingKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(signingKey))
            {
                // 이 상태로 뜨면 서명 키가 소스에 적힌 값이라 누구나 토큰을 위조할 수 있다.
                Logger.FatalWith("Jwt:Key is missing — falling back to the public placeholder key",
                    ("Issuer", configuration["Jwt:Issuer"]));
            }

            Logger.InfoWith("JWT configured",
                ("Issuer", configuration["Jwt:Issuer"]),
                ("Audience", configuration["Jwt:Audience"]),
                ("SigningKey", string.IsNullOrWhiteSpace(signingKey) ? "Placeholder" : "Configured"));

            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(signingKey ?? "dev-only-insecure-placeholder-key-change-me"))
                };

                // 서명·만료가 멀쩡해도 로그아웃·탈퇴한 토큰이면 여기서 떨군다.
                // 서명 검증을 통과한 뒤라 조회 비용은 정상 요청에만 든다.
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = TokenRevocationCheck.OnTokenValidatedAsync,
                };
            });
            services.AddAuthorization();

            return services;
        }
    }
}
