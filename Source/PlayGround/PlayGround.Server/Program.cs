using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NLog;
using PlayGround.Infrastructure.Actor;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Auth.Commands;
using PlayGround.Application.Landing.Queries;
using PlayGround.Application.Player.Commands;
using PlayGround.Persistence;
using PlayGround.Server.Actors;
using PlayGround.Server.Services;

var logger = LogManager.GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 시크릿(Jwt·OAuth)은 gitignore된 appsettings.Local.json에서만 로드 (커밋 금지)
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

    builder.Host.ConfigurePlayGroundLogger(builder.Configuration);
    builder.Services.AddPlayGroundLogger();

    // Account / Soccer 2-DB 설정 바인딩 (Repository가 IOptions<DatabaseConfiguration>로 주입)
    builder.Services.Configure<DatabaseConfiguration>(
        builder.Configuration.GetSection(DatabaseConfiguration.Section));

    // Persistence 리포지토리 + 유즈케이스
    builder.Services.AddSoccerPersistence();
    builder.Services.AddScoped<GetLandingContentsQuery>();
    builder.Services.AddScoped<CreatePlayerProfileCommand>();

    //.// Akka: Controller → 액터(비동기 메일박스) → 유즈케이스 → DB

    // AkkaService는 싱글턴이자 HostedService(같은 인스턴스). 그 다음에 토폴로지 서비스를
    // 등록해 ActorSystem 기동 이후 액터를 생성한다 (HostedService는 등록 순서대로 기동).
    builder.Services.AddSingleton<AkkaService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<AkkaService>());
    builder.Services.AddSingleton<ActorGateway>();
    builder.Services.AddHostedService<ActorTopologyService>();

    // 소셜 OAuth (Google/Kakao/Naver) — provider HTTP 호출용 HttpClient + 유즈케이스
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<OAuthService>();
    builder.Services.AddScoped<LoginBySocialCommand>();

    // 인증: JWT 발급 서비스 + JWT Bearer 검증
    builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
    builder.Services.AddAuthentication(options =>
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "dev-only-insecure-placeholder-key-change-me"))
        };
    });
    builder.Services.AddAuthorization();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseWebAssemblyDebugging();
    }
    else
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();
    app.UseRouting();

    // .NET 10 지문(fingerprint) 정적 자산(importmap이 가리키는 js/landing.{hash}.js 등)은
    // UseStaticFiles가 아닌 MapStaticAssets 엔드포인트로만 서빙된다
    app.MapStaticAssets();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapFallbackToFile("index.html");

    logger.InfoWith("Server starting", ("Environment", app.Environment.EnvironmentName));

    app.Run();
}
catch (Exception ex)
{
    // 기동 실패도 반드시 로그에 남긴다
    logger.FatalWith(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    // 버퍼링된 로그 flush 후 종료
    LogManager.Shutdown();
}
