using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using PlayGround.Client;
using PlayGround.Client.Localization;
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
builder.Services.AddScoped<DashboardClient>();
builder.Services.AddScoped<AuthClient>();
builder.Services.AddScoped<RecordsClient>();
builder.Services.AddScoped<RecordsDetailNavState>();
builder.Services.AddScoped<ClaimClient>();
builder.Services.AddScoped<NotificationClient>();
builder.Services.AddScoped<ExportClient>();
builder.Services.AddScoped<AgentClient>();
builder.Services.AddScoped<PaymentClient>();
builder.Services.AddScoped<OnboardingState>();

// 전역 피드백 (Design.FeedbackPatterns) — 토스트는 동시 1개, 확인 모달은 중첩 금지
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ConfirmService>();
builder.Services.AddScoped<BannerService>();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<ReturnUrlStore>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());

// i18n — 타입드 로컬라이제이션 (AppText.*). 기동 시 기본 문화권(ko) 선로드 + 앰비언트 주입.
builder.Services.AddScoped<JsonLocalizer>();
builder.Services.AddScoped<ILocalizer>(sp => sp.GetRequiredService<JsonLocalizer>());
builder.Services.AddScoped<CultureState>();

WebAssemblyHost host = builder.Build();
JsonLocalizer localizer = host.Services.GetRequiredService<JsonLocalizer>();
AppText.Loc = localizer;

// 지속된 문화권(localStorage) 선반영 — 없으면 기본(ko). WASM은 Build() 후 JS interop 가능.
string startupCulture = JsonLocalizer.BaseCulture;
try
{
    IJSRuntime js = host.Services.GetRequiredService<IJSRuntime>();
    string? stored = await js.InvokeAsync<string?>("localStorage.getItem", CultureState.StorageKey);
    if (!string.IsNullOrWhiteSpace(stored))
    {
        startupCulture = stored;
    }
}
catch (JSException)
{
}

await localizer.LoadAsync(startupCulture);
await host.RunAsync();
