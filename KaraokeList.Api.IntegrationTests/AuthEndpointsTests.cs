using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
    }
}

internal static class IntegrationAuthHelper
{
    public const string TestPassword = "TestPassw0rd!23";

    public static async Task<string> RegisterAndGetTokenAsync(HttpClient client, string email)
    {
        var registerRequest = new RegisterRequest
        {
            Name = "Integration Test Singer",
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerBody = await registerResponse.Content.ReadAsStringAsync();
        Assert.True(
            registerResponse.IsSuccessStatusCode,
            $"Register failed ({(int)registerResponse.StatusCode}): {registerBody}");

        var auth = JsonSerializer.Deserialize<AuthResponse>(
            registerBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        return auth.Token;
    }
}
