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
        var artistId = Assert.Single(songs, s => s.Id == songId).Artists.First().ArtistId;

        var response = await admin.DeleteAsync($"/api/artists/{artistId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [SkippableFact]
    public async Task CreateSong_WithValidArtist_ReturnsCreatedWithId()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var artistName = $"Artist {Guid.NewGuid():N}";
        var createArtist = await client.PostAsJsonAsync("/api/artists", new ArtistDto { Name = artistName });
        Assert.Equal(HttpStatusCode.NoContent, createArtist.StatusCode);

        var artists = await client.GetFromJsonAsync<List<ArtistLookupDto>>("/api/artists/lookup");
        Assert.NotNull(artists);
        var artistId = Assert.Single(artists, a => a.Name == artistName).Id;

        var songTitle = $"Song {Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/songs", new SongDto
        {
            Title = songTitle,
            Artists =
            [
                new SongArtistDto { ArtistId = artistId, DisplayOrder = 0, Name = artistName }
            ]
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SongDto>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal(songTitle, created.Title);
        Assert.Equal(artistId, created.Artists.First().ArtistId);
    }

    [SkippableFact]
    public async Task CreateSong_WithUnknownArtist_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/songs", new SongDto
        {
            Title = $"Song {Guid.NewGuid():N}",
            Artists =
            [
                new SongArtistDto { ArtistId = 999_999, DisplayOrder = 0, Name = "Missing" }
            ]
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
