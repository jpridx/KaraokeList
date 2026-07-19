using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using KaraokeList.Api.Services;
using KaraokeList.Data;
using KaraokeList.Security;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class ExternalAuthEndpointsTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetExternalProviders_ReturnsDisabledWhenNotConfigured()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/auth/external/providers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var providers = await response.Content.ReadFromJsonAsync<ExternalAuthProvidersDto>();
        Assert.NotNull(providers);
        Assert.False(providers.GoogleEnabled);
        Assert.False(providers.MicrosoftEnabled);
        Assert.False(providers.AnyEnabled);
    }

    [SkippableFact]
    public async Task ExchangeExternalAuthCode_WithInvalidCode_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/external/exchange",
            new ExternalAuthExchangeRequest { Code = "invalid-code" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task ExchangeExternalAuthCode_WithValidCode_ReturnsJwt()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var codeStore = scope.ServiceProvider.GetRequiredService<IExternalAuthCodeStore>();

        var email = $"oauth-{Guid.NewGuid():N}@example.com";
        var user = new ApplicationUser { UserName = email, Email = email };
        var createResult = await userManager.CreateAsync(user);
        Assert.True(createResult.Succeeded);

        var code = codeStore.CreateCode(user.Id, rememberMe: false);

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/external/exchange",
            new ExternalAuthExchangeRequest { Code = code });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        Assert.Equal(email, auth.Email);
    }

    [SkippableFact]
    public async Task ProcessExternalLogin_CreatesUserAndSinger_WhenRegistrationOpen()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        using var scope = factory.Services.CreateScope();
        var externalAuth = scope.ServiceProvider.GetRequiredService<IExternalAuthService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"oauth-new-{Guid.NewGuid():N}@example.com";
        var loginInfo = CreateExternalLoginInfo(GoogleDefaults.AuthenticationScheme, email, "Stage Name");

        var result = await externalAuth.ProcessExternalLoginAsync(loginInfo, inviteCode: null);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.User);

        var stored = await userManager.FindByEmailAsync(email);
        Assert.NotNull(stored);
        Assert.True(stored!.SingerId > 0);
        Assert.True(await userManager.HasPasswordAsync(stored) == false);
    }

    [SkippableFact]
    public async Task ProcessExternalLogin_LinksExistingPasswordUser_WhenEmailMatches()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        using var client = factory.CreateClient();
        var email = $"oauth-link-{Guid.NewGuid():N}@example.com";
        await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);

        using var scope = factory.Services.CreateScope();
        var externalAuth = scope.ServiceProvider.GetRequiredService<IExternalAuthService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var loginInfo = CreateExternalLoginInfo(GoogleDefaults.AuthenticationScheme, email, "Linked Singer");
        var result = await externalAuth.ProcessExternalLoginAsync(loginInfo, inviteCode: null);

        Assert.True(result.Succeeded);
        var logins = await userManager.GetLoginsAsync(result.User!);
        Assert.Contains(logins, login => login.LoginProvider == GoogleDefaults.AuthenticationScheme);
    }

    private static ExternalLoginInfo CreateExternalLoginInfo(string provider, string email, string displayName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, displayName),
            new("email_verified", "true")
        };
        var identity = new ClaimsIdentity(claims, authenticationType: provider);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties();
        return new ExternalLoginInfo(principal, provider, $"key-{email}", provider)
        {
            AuthenticationProperties = authProperties
        };
    }
}
