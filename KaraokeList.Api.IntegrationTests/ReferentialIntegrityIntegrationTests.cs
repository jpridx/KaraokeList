using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class ReferentialIntegrityIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task CreatePerformance_WithoutSong_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (_, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        var response = await client.PostAsJsonAsync("/api/performances", new PerformanceDto
        {
            Venue = venueId,
            PerformedOn = DateTime.Today
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SkippableFact]
    public async Task CreatePerformance_WithUnknownSong_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/performances", new PerformanceDto
        {
            Song = 999_999,
            PerformedOn = DateTime.Today
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SkippableFact]
    public async Task CreatePerformance_WithUnknownVenue_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (songId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        var response = await client.PostAsJsonAsync("/api/performances", new PerformanceDto
        {
            Song = songId,
            Venue = 999_999,
            PerformedOn = DateTime.Today
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SkippableFact]
    public async Task DeleteSong_WithPerformances_ReturnsConflict()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (admin, _) = await IntegrationAuthHelper.CreateAdminClientAsync(factory);
        var (songId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        await PerformanceTestDataHelper.CreatePerformanceAsync(client, songId, venueId);

        var response = await admin.DeleteAsync($"/api/songs/{songId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [SkippableFact]
    public async Task DeleteArtist_ReferencedAsPrimaryArtist_ReturnsConflict()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (admin, _) = await IntegrationAuthHelper.CreateAdminClientAsync(factory);
        var (songId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        var songs = await client.GetFromJsonAsync<List<SongDto>>("/api/songs");
        Assert.NotNull(songs);
        var artistId = Assert.Single(songs, s => s.Id == songId).Artist;
        Assert.NotNull(artistId);

        var response = await admin.DeleteAsync($"/api/artists/{artistId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [SkippableFact]
    public async Task CreateSong_WithUnknownArtist_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/songs", new SongDto
        {
            Title = $"Song {Guid.NewGuid():N}",
            Artist = 999_999
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"integrity-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
