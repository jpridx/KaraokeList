using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace KaraokeList.Api.IntegrationTests;

internal static class IntegrationAuthHelper
{
    public const string TestPassword = "TestPassw0rd!23";

    public static async Task<string> RegisterAndGetTokenAsync(HttpClient client, string email, string? inviteCode = null)
    {
        var registerRequest = new RegisterRequest
        {
            Name = "Integration Test Singer",
            Email = email,
            Password = TestPassword,
            ConfirmPassword = TestPassword,
            InviteCode = inviteCode
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

    public static async Task<string> LoginAndGetTokenAsync(HttpClient client, string email, string? password = null)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password ?? TestPassword
        });
        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(
            loginResponse.IsSuccessStatusCode,
            $"Login failed ({(int)loginResponse.StatusCode}): {loginBody}");

        var auth = JsonSerializer.Deserialize<AuthResponse>(
            loginBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        return auth.Token;
    }

    public static async Task<HttpClient> CreateMemberClientAsync(KaraokeApiFactory factory)
    {
        var client = factory.CreateClient();
        var email = $"member-{Guid.NewGuid():N}@example.com";
        var token = await RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<(HttpClient Client, string Email)> CreateAdminClientAsync(KaraokeApiFactory factory)
    {
        var registerClient = factory.CreateClient();
        var email = $"admin-{Guid.NewGuid():N}@example.com";
        await RegisterAndGetTokenAsync(registerClient, email);
        await PromoteToAdminAsync(factory, email);

        var token = await LoginAndGetTokenAsync(factory.CreateClient(), email);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, email);
    }

    public static async Task<(string Email, string UserId, HttpClient Client)> CreateUserWithoutSingerAsync(
        KaraokeApiFactory factory)
    {
        var email = $"nosinger-{Guid.NewGuid():N}@example.com";
        var userId = await CreateIdentityUserAsync(factory, email);

        var token = await LoginAndGetTokenAsync(factory.CreateClient(), email);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (email, userId, client);
    }

    public static async Task PromoteToAdminAsync(KaraokeApiFactory factory, string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(KaraokeRoles.Admin))
        {
            var createRole = await roleManager.CreateAsync(new IdentityRole(KaraokeRoles.Admin));
            Assert.True(createRole.Succeeded);
        }

        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);

        if (!await userManager.IsInRoleAsync(user, KaraokeRoles.Admin))
        {
            var addResult = await userManager.AddToRoleAsync(user, KaraokeRoles.Admin);
            Assert.True(addResult.Succeeded);
        }
    }

    private static async Task<string> CreateIdentityUserAsync(KaraokeApiFactory factory, string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email };
        var result = await userManager.CreateAsync(user, TestPassword);
        Assert.True(result.Succeeded, string.Join(" ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }
}
