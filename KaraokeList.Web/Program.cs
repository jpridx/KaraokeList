// KaraokeList.Web (WASM UI) — Syncfusion is required here for catalog grids and Log comboboxes.
// KaraokeList.Api has no Syncfusion; do not confuse the two Program.cs files.
using Blazored.LocalStorage;
using KaraokeList.Web;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;

// Key is embedded at build from user secrets (SyncfusionLicenseKey.g.cs). Not loaded from wwwroot.
if (!string.IsNullOrWhiteSpace(SyncfusionLicenseKey.Value))
{
    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(SyncfusionLicenseKey.Value);
}
else
{
    Console.WriteLine(
        "Syncfusion trial banner: set user secrets, rebuild. scripts/set-syncfusion-key.ps1");
}

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is required in wwwroot/appsettings.json");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSyncfusionBlazor();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddScoped<ApiSlowRequestNotifier>();
builder.Services.AddSingleton<AppUpdateNotifier>();
builder.Services.AddScoped<IAppUpdateService, AppUpdateService>();
builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddScoped<SlowApiRequestHandler>();
builder.Services.AddScoped<SafeReadRetryHandler>();
builder.Services.AddScoped<IKaraokeApiClient, KaraokeApiClient>();
builder.Services.AddScoped<ICatalogVersionService, CatalogVersionService>();
builder.Services.AddScoped<ILogPerformanceLocalStore, LogPerformanceLocalStore>();
builder.Services.AddScoped<ISingerProfileLocalStore, SingerProfileLocalStore>();
builder.Services.AddScoped<ISingerProfileResolver, SingerProfileResolver>();
builder.Services.AddScoped<ILogCatalogLoader, LogCatalogLoader>();
builder.Services.AddScoped<IPendingPerformanceSyncService, PendingPerformanceSyncService>();
builder.Services.AddScoped<IMySongsLocalStore, MySongsLocalStore>();
builder.Services.AddScoped<IMySongsLoader, MySongsLoader>();
builder.Services.AddScoped<MySongsScrollRestoreState>();
builder.Services.AddScoped<IScrollRestoreJs, ScrollRestoreJs>();
builder.Services.AddScoped<IMyPerformancesLocalStore, MyPerformancesLocalStore>();
builder.Services.AddScoped<IMyPerformancesLoader, MyPerformancesLoader>();
builder.Services.AddScoped<ITicklerSettingsLocalStore, TicklerSettingsLocalStore>();
builder.Services.AddScoped<ITicklerExclusionsLocalStore, TicklerExclusionsLocalStore>();
builder.Services.AddScoped<ILocalStaleSongsProvider, LocalStaleSongsProvider>();
builder.Services.AddScoped<ICatalogSyncService, CatalogSyncService>();
builder.Services.AddSingleton<IBackgroundWorkScheduler, BackgroundWorkScheduler>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddHttpClient("KaraokeApi", client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
        client.Timeout = TimeSpan.FromMinutes(2);
    })
    .AddHttpMessageHandler<SafeReadRetryHandler>()
    .AddHttpMessageHandler<SlowApiRequestHandler>()
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("KaraokeApi"));

await builder.Build().RunAsync();
