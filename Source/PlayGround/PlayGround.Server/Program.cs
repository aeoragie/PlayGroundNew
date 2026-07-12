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

    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

    builder.Host.ConfigurePlayGroundLogger(builder.Configuration);
    builder.Services.AddPlayGroundLogger();

    builder.Services.Configure<DatabaseConfiguration>(
        builder.Configuration.GetSection(DatabaseConfiguration.Section));

    builder.Services.AddSoccerPersistence();
    builder.Services.AddScoped<GetLandingContentsQuery>();
    builder.Services.AddScoped<CreatePlayerProfileCommand>();

    // AkkaService(HostedService) 기동 후 토폴로지가 액터를 만든다 — 등록 순서 유지
    builder.Services.AddSingleton<AkkaService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<AkkaService>());
    builder.Services.AddSingleton<ActorGateway>();
    builder.Services.AddHostedService<ActorTopologyService>();

    builder.Services.AddHttpClient();
    builder.Services.AddScoped<OAuthService>();
    builder.Services.AddScoped<LoginBySocialCommand>();

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

    app.MapStaticAssets(); // .NET 10 지문 정적 자산(js/{hash}.js)은 이 엔드포인트로만 서빙됨
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapFallbackToFile("index.html");

    logger.InfoWith("Server starting", ("Environment", app.Environment.EnvironmentName));

    app.Run();
}
catch (Exception ex)
{
    logger.FatalWith(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    LogManager.Shutdown();
}
