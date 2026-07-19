using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Pages;

public sealed class AuthCallbackPageTests : AuthPageTestContext
{
    [Fact]
    public async Task Shows_error_when_query_contains_error()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/auth/callback?error=Provider%20failed");

        var cut = Render<AuthCallback>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Sign-in failed", cut.Markup);
            Assert.Contains("Provider failed", cut.Markup);
        });
    }

    [Fact]
    public async Task Navigates_home_when_exchange_succeeds()
    {
        var token = CreateTestToken();
        Api.Setup(client => client.ExchangeExternalAuthCodeAsync(It.IsAny<ExternalAuthExchangeRequest>()))
            .ReturnsAsync(AuthResult.Ok(new AuthResponse
            {
                Token = token,
                Email = "user@example.com",
                ExpiresUtc = DateTime.UtcNow.AddHours(1)
            }));

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/auth/callback?code=abc123");

        var cut = Render<AuthCallback>();

        cut.WaitForAssertion(() => Assert.Equal("http://localhost/", nav.Uri));

        var storedToken = await GetStoredTokenAsync();
        Assert.Equal(token, storedToken);
    }
}

public sealed class ExternalLoginButtonsTests : BunitTestContext
{
    [Fact]
    public void Renders_provider_links_when_enabled()
    {
        var cut = Render<KaraokeList.Web.Components.ExternalLoginButtons>(parameters => parameters
            .Add(p => p.Providers, new ExternalAuthProvidersDto { GoogleEnabled = true, MicrosoftEnabled = true })
            .Add(p => p.ApiBaseUrl, "http://localhost:5299")
            .Add(p => p.ReturnUrl, "/log")
            .Add(p => p.Invite, "secret"));

        cut.WaitForAssertion(() =>
        {
            var google = cut.Find("a[href*='external/google']");
            var microsoft = cut.Find("a[href*='external/microsoft']");
            Assert.Contains("returnUrl=%2Flog", google.GetAttribute("href"));
            Assert.Contains("invite=secret", google.GetAttribute("href"));
            Assert.Contains("Sign in with Google", google.TextContent);
            Assert.Contains("Sign in with Microsoft", microsoft.TextContent);
        });
    }
}

public sealed class LoginExternalAuthTests : AuthPageTestContext
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton(new HttpClient { BaseAddress = new Uri("http://localhost:5299/") });
    }

    [Fact]
    public void Shows_external_login_buttons_when_providers_enabled()
    {
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto());
        Api.Setup(client => client.GetExternalAuthProvidersAsync())
            .ReturnsAsync(new ExternalAuthProvidersDto { GoogleEnabled = true });

        var cut = Render<Login>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Sign in with Google", cut.Markup);
            Assert.Contains("external/google", cut.Markup);
        });
    }
}
