using System.Net;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class SongAboutIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetSongAbout_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/songs/1/about");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetSongAbout_WhenSongMissing_ReturnsNotFound()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var response = await client.GetAsync("/api/songs/999999/about");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(body?.Message);
    }

    [SkippableFact]
    public async Task GetSongAbout_WhenSongExists_ReturnsTitleStub()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (songId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        var songs = await client.GetFromJsonAsync<List<SongDto>>("/api/songs");
        Assert.NotNull(songs);
        var title = Assert.Single(songs, s => s.Id == songId).Title;

        var about = await client.GetFromJsonAsync<SongAboutDto>($"/api/songs/{songId}/about");
        Assert.NotNull(about);
        Assert.Equal(songId, about.SongId);
        Assert.Equal(title, about.Title);
        Assert.Null(about.Enrichment);
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"song-about-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
