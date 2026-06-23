using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class AdminRolesIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetAdminUsers_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var response = await factory.CreateClient().GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetAdminUsers_AsMember_ReturnsForbidden()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var member = await IntegrationAuthHelper.CreateMemberClientAsync(factory);
        var response = await member.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetAdminUsers_AsAdmin_ReturnsUserList()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var (admin, adminEmail) = await IntegrationAuthHelper.CreateAdminClientAsync(factory);
        var users = await admin.GetFromJsonAsync<List<AdminUserDto>>("/api/admin/users");

        Assert.NotNull(users);
        Assert.Contains(users, u => u.Email == adminEmail && u.IsAdmin);
    }

    [SkippableFact]
    public async Task UpdateUser_GrantAdmin_AsAdmin_Succeeds()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var (admin, _) = await IntegrationAuthHelper.CreateAdminClientAsync(factory);
        var memberEmail = $"grant-{Guid.NewGuid():N}@example.com";
        var registerClient = factory.CreateClient();
        await IntegrationAuthHelper.RegisterAndGetTokenAsync(registerClient, memberEmail);

        var users = await admin.GetFromJsonAsync<List<AdminUserDto>>("/api/admin/users");
        Assert.NotNull(users);
        var member = Assert.Single(users, u => u.Email == memberEmail);
        Assert.False(member.IsAdmin);

        var updateResponse = await admin.PutAsJsonAsync(
            $"/api/admin/users/{member.UserId}",
            new UpdateAdminUserRequest
            {
                UserId = member.UserId,
                IsAdmin = true,
                SingerId = member.SingerId
            });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var memberToken = await IntegrationAuthHelper.LoginAndGetTokenAsync(factory.CreateClient(), memberEmail);
        var profileClient = factory.CreateClient();
        profileClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var profile = await profileClient.GetFromJsonAsync<UserProfileDto>("/api/auth/me");

        Assert.NotNull(profile);
        Assert.True(profile.IsAdmin);
    }

    [SkippableFact]
    public async Task LinkSinger_MemberWithExistingSingerId_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var memberWithSinger = await IntegrationAuthHelper.CreateMemberClientAsync(factory);
        var existingSingerId = (await memberWithSinger.GetFromJsonAsync<UserProfileDto>("/api/auth/me"))!.SingerId;
        Assert.NotNull(existingSingerId);

        var (_, _, memberWithoutSinger) = await IntegrationAuthHelper.CreateUserWithoutSingerAsync(factory);
        var response = await memberWithoutSinger.PostAsJsonAsync("/api/auth/link-singer", new LinkSingerRequest
        {
            SingerId = existingSingerId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Contains("Only admins can link to an existing singer", error?.Message);
    }

    [SkippableFact]
    public async Task LinkSinger_MemberWithNewName_CreatesProfile()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var (_, _, client) = await IntegrationAuthHelper.CreateUserWithoutSingerAsync(factory);
        var stageName = $"Stage {Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/auth/link-singer", new LinkSingerRequest
        {
            Name = stageName
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.NotNull(auth.SingerId);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var profile = await client.GetFromJsonAsync<UserProfileDto>("/api/auth/me");
        Assert.NotNull(profile);
        Assert.Equal(auth.SingerId, profile.SingerId);
    }

    [SkippableFact]
    public async Task LinkSinger_AdminWithExistingSingerId_LinksProfile()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var (admin, _) = await IntegrationAuthHelper.CreateAdminClientAsync(factory);
        var singerName = $"Unlinked Singer {Guid.NewGuid():N}";
        var createSinger = await admin.PostAsJsonAsync("/api/singers", new SingerDto { Name = singerName });
        Assert.Equal(HttpStatusCode.NoContent, createSinger.StatusCode);

        var singers = await admin.GetFromJsonAsync<List<SingerDto>>("/api/singers");
        Assert.NotNull(singers);
        var targetSingerId = Assert.Single(singers, s => s.Name == singerName).Id;

        var (adminEmail, _, adminClient) = await IntegrationAuthHelper.CreateUserWithoutSingerAsync(factory);
        await IntegrationAuthHelper.PromoteToAdminAsync(factory, adminEmail);
        var adminToken = await IntegrationAuthHelper.LoginAndGetTokenAsync(factory.CreateClient(), adminEmail);
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await adminClient.PostAsJsonAsync("/api/auth/link-singer", new LinkSingerRequest
        {
            SingerId = targetSingerId
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal(targetSingerId, auth.SingerId);
    }
}
