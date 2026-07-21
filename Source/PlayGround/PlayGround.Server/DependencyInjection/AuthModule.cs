using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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

            //.// 계정 설정 (설정 화면 — 계정·알림 탭 + 계정 삭제)
            services.AddScoped<AccountSettingsCommand>();
            services.AddScoped<NotificationPreferenceCommand>();
            services.AddScoped<AccountDeleteCommand>();

            //.// JWT 발급 + Bearer 검증
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
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "dev-only-insecure-placeholder-key-change-me"))
                };
            });
            services.AddAuthorization();

            return services;
        }
    }
}
