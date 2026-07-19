using System.Security.Claims;
using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Moq;

namespace KaraokeList.Web.Tests.Services;

public sealed class ThemeServiceTests
{
    [Fact]
    public async Task GetPreferenceAsync_reads_legacy_plain_string_Dark()
    {
        var storage = new InMemoryLocalStorage();
        await storage.SetItemAsStringAsync(ThemeService.StorageKey, "Dark");

        var service = CreateService(storage, authenticated: false);

        var preference = await service.GetPreferenceAsync();

        Assert.Equal(ThemePreference.Dark, preference);
        Assert.Equal("2", await storage.GetItemAsStringAsync(ThemeService.StorageKey));
    }

    [Fact]
    public async Task GetPreferenceAsync_clears_invalid_value_and_defaults_to_System()
    {
        var storage = new InMemoryLocalStorage();
        await storage.SetItemAsStringAsync(ThemeService.StorageKey, "not-a-theme");

        var service = CreateService(storage, authenticated: false);

        var preference = await service.GetPreferenceAsync();

        Assert.Equal(ThemePreference.System, preference);
        Assert.False(await storage.ContainKeyAsync(ThemeService.StorageKey));
    }

    [Fact]
    public async Task GetPreferenceAsync_reads_json_numeric_value()
    {
        var storage = new InMemoryLocalStorage();
        await storage.SetItemAsync(ThemeService.StorageKey, ThemePreference.Dark);

        var service = CreateService(storage, authenticated: false);

        var preference = await service.GetPreferenceAsync();

        Assert.Equal(ThemePreference.Dark, preference);
    }

    [Fact]
    public async Task GetPreferenceAsync_uses_api_when_authenticated()
    {
        var storage = new InMemoryLocalStorage();
        await storage.SetItemAsStringAsync(ThemeService.StorageKey, "Light");

        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetThemePreferenceAsync())
            .ReturnsAsync(ThemePreferenceResult.Ok(new ThemePreferenceDto
            {
                Preference = ThemePreference.Dark
            }));

        var service = CreateService(storage, authenticated: true, api: api.Object);

        var preference = await service.GetPreferenceAsync();

        Assert.Equal(ThemePreference.Dark, preference);
        api.Verify(client => client.GetThemePreferenceAsync(), Times.Once);
    }

    private static ThemeService CreateService(
        InMemoryLocalStorage storage,
        bool authenticated,
        IKaraokeApiClient? api = null)
    {
        var claims = authenticated
            ? new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")], "test"))
            : new ClaimsPrincipal(new ClaimsIdentity());
        var auth = new TestAuthStateProvider(claims);

        var js = new Mock<IJSRuntime>();

        return new ThemeService(
            storage,
            js.Object,
            api ?? Mock.Of<IKaraokeApiClient>(),
            auth);
    }

    private sealed class TestAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(user));
    }
}
