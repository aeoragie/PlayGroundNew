using NLog;
using PlayGround.Infrastructure.Logging;

var logger = LogManager.GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.ConfigurePlayGroundLogger(builder.Configuration);
    builder.Services.AddPlayGroundLogger();

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
