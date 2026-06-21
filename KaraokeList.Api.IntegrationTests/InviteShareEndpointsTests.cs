using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class InviteShareEndpointsTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetInviteShare_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/auth/invite-share");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetInviteShare_WhenInviteNotRequired_ReturnsUnavailable()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var authed = await CreateAuthedClientAsync(factory);
        var share = await authed.GetFromJsonAsync<InviteShareDto>("/api/auth/invite-share");

        Assert.NotNull(share);
        Assert.False(share.CanShare);
        Assert.NotNull(share.UnavailableReason);
    }

    private static async Task<HttpClient> CreateAuthedClientAsync(KaraokeApiFactory factory)
    {
        var client = factory.CreateClient();
        var email = $"invite-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public sealed class InviteRequiredKaraokeApiFactory : KaraokeApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Registration:RequireInviteCode"] = "true",
                ["Security:Registration:InviteCode"] = "integration-invite-code"
            });
        });
    }
}

[CollectionDefinition(Name)]
public sealed class InviteRequiredApiCollection : ICollectionFixture<InviteRequiredKaraokeApiFactory>
{
    public const string Name = nameof(InviteRequiredApiCollection);
}

[Collection(InviteRequiredApiCollection.Name)]
public sealed class InviteRequiredShareEndpointsTests(InviteRequiredKaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetInviteShare_WhenInviteRequired_ReturnsConfiguredCode()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = factory.CreateClient();
        var email = $"invite-required-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(
            client,
            email,
            inviteCode: "integration-invite-code");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var share = await client.GetFromJsonAsync<InviteShareDto>("/api/auth/invite-share");

        Assert.NotNull(share);
        Assert.True(share.CanShare);
        Assert.Equal("integration-invite-code", share.InviteCode);
    }
}
