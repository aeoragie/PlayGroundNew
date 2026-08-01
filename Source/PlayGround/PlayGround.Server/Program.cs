using NLog;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Logging;
using PlayGround.Server.DependencyInjection;
using PlayGround.Server.Middleware;

var logger = LogManager.GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

    builder.Host.ConfigurePlayGroundLogger(builder.Configuration);
    builder.Services.AddPlayGroundLogger();

    builder.Services.Configure<DatabaseConfiguration>(builder.Configuration.GetSection(DatabaseConfiguration.Section));

    //.// 모듈별 DI — 인프라(Akka) · 인증(공유) · 종목(축구)

    builder.Services.AddAkkaPipeline();
    builder.Services.AddAuthServices(builder.Configuration);
    builder.Services.AddSoccerServices();

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

    // 링크 공유 미리보기 — 크롤러면 메타 태그 최소 HTML 반환, 사람은 SPA 그대로 (정적 자산 뒤·라우팅 앞)
    app.UseMiddleware<OgMetaMiddleware>();

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
