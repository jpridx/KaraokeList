using Blazored.LocalStorage;
using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.IdentityModel.Tokens.Jwt;

namespace KaraokeList.Web.Tests.Pages;

public abstract class AuthPageTestContext : BunitTestContext
{
    protected Mock<IKaraokeApiClient> Api { get; } = new();
    private readonly InMemoryLocalStorage localStorage = new();

    protected static string CreateTestToken() =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(expires: DateTime.UtcNow.AddHours(1)));

    protected async Task<string?> GetStoredTokenAsync() =>
        await localStorage.GetItemAsStringAsync(JwtAuthenticationStateProvider.TokenKey);
    protected AuthPageTestContext()
    {
        Api.Setup(client => client.GetMusicServicePreferenceAsync())
            .ReturnsAsync(MusicServicePreferenceResult.Ok(new MusicServicePreferenceDto()));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Api.Object);
        services.AddSingleton<ILocalStorageService>(localStorage);
        services.AddSingleton<ISingerProfileLocalStore>(new SingerProfileLocalStore(localStorage));
        services.AddSingleton<ISingerProfileResolver, SingerProfileResolver>();
        services.AddSingleton<JwtAuthenticationStateProvider>();
        services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
        services.AddSingleton<ApiSlowRequestNotifier>();
    }
}
