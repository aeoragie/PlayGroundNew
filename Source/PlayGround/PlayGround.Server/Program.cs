using Microsoft.AspNetCore.HttpOverrides;
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
    builder.Services.AddSoccerServices(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // 리버스 프록시(Nginx)가 TLS를 끝내고 앱에는 http로 넘긴다. 원래 스킴을 복원하지 않으면
    // 아래 UseHttpsRedirection이 매 요청을 https로 되돌려 **무한 리다이렉트**가 된다.
    // 기본 신뢰 대상이 루프백이라 같은 호스트의 Nginx만 이 헤더를 넣을 수 있다.
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    });

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
