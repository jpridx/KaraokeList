using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class AuthEndpointsTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetRegistration_ReturnsOpenFlagsWithoutAuth()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/auth/registration");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var info = await response.Content.ReadFromJsonAsync<RegistrationInfoDto>();
        Assert.NotNull(info);
        Assert.True(info.IsRegistrationOpen);
        Assert.False(info.RequiresInviteCode);
    }

    [SkippableFact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task RegisterAndLogin_ReturnsJwtAndProfile()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        using var client = factory.CreateClient();
        var email = $"test-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);

        Assert.False(string.IsNullOrWhiteSpace(token));

        using var authed = factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var profileResponse = await authed.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profile = await profileResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal(email, profile.Email);
        Assert.True(profile.SingerId > 0);
        Assert.False(profile.IsAdmin);
    }

    [SkippableFact]
    public async Task ChangePassword_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = "old",
            NewPassword = "new",
            ConfirmPassword = "new"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var registerClient = factory.CreateClient();
        var email = $"pwd-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(registerClient, email);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = "wrong-password",
            NewPassword = "NewPassw0rd!99",
            ConfirmPassword = "NewPassw0rd!99"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Contains("incorrect", error?.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task ChangePassword_Succeeds_AndNewPasswordWorksForLogin()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var registerClient = factory.CreateClient();
        var email = $"pwd-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(registerClient, email);

        const string newPassword = "NewPassw0rd!99";
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var changeResponse = await client.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = IntegrationAuthHelper.TestPassword,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        });
        Assert.Equal(HttpStatusCode.NoContent, changeResponse.StatusCode);

        var loginClient = factory.CreateClient();
        var oldLogin = await loginClient.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = IntegrationAuthHelper.TestPassword
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await loginClient.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }
}
