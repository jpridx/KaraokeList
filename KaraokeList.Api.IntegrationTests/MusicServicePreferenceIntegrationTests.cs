using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class MusicServicePreferenceIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetMusicServicePreference_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/auth/music-service");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetMusicServicePreference_ReturnsNoneForNewUser()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var preference = await client.GetFromJsonAsync<MusicServicePreferenceDto>("/api/auth/music-service");

        Assert.NotNull(preference);
        Assert.Equal(MusicService.None, preference.Service);
    }

    [SkippableFact]
    public async Task UpdateMusicServicePreference_Persists()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var updateResponse = await client.PutAsJsonAsync(
            "/api/auth/music-service",
            new UpdateMusicServicePreferenceRequest { Service = MusicService.Spotify });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var preference = await client.GetFromJsonAsync<MusicServicePreferenceDto>("/api/auth/music-service");
        Assert.NotNull(preference);
        Assert.Equal(MusicService.Spotify, preference.Service);
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"music-service-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
