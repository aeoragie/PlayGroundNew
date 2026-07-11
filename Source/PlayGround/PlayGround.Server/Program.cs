using PlayGround.Infrastructure.Logging;

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
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
