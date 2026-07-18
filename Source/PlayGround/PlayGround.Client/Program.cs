using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PlayGround.Client;
using PlayGround.Client.Services;
using PlayGround.Client.Services.Auth;
using PlayGround.Client.Services.Feedback;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<LandingContentClient>();
builder.Services.AddScoped<PlayerClient>();
builder.Services.AddScoped<TeamClient>();
builder.Services.AddScoped<AuthClient>();
builder.Services.AddScoped<RecordsClient>();
builder.Services.AddScoped<OnboardingState>();

// 전역 피드백 (Design.FeedbackPatterns) — 토스트는 동시 1개, 확인 모달은 중첩 금지
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ConfirmService>();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());

await builder.Build().RunAsync();
