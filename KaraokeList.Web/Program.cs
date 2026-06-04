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
builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddScoped<IKaraokeApiClient, KaraokeApiClient>();
builder.Services.AddHttpClient("KaraokeApi", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("KaraokeApi"));

await builder.Build().RunAsync();
