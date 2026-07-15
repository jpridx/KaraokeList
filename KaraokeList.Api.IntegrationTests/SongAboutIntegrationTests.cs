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
    public async Task GetSongAbout_WhenSongExists_ReturnsCatalogFields()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var genreId = await PerformanceTestDataHelper.CreateGenreAsync(client, $"Rock {Guid.NewGuid():N}");

        var artistName = $"Artist {Guid.NewGuid():N}";
        var createArtist = await client.PostAsJsonAsync("/api/artists", new ArtistDto { Name = artistName });
        Assert.Equal(HttpStatusCode.NoContent, createArtist.StatusCode);

        var artists = await client.GetFromJsonAsync<List<ArtistDto>>("/api/artists");
        Assert.NotNull(artists);
        var artistId = Assert.Single(artists, a => a.Name == artistName).Id;

        var songTitle = $"Song {Guid.NewGuid():N}";
        var createSong = await client.PostAsJsonAsync("/api/songs", new SongDto
        {
            Title = songTitle,
            Year = 1994,
            Genre = genreId,
            ArtistCreditDisplay = $"{artistName} feat. Guest",
            Artists =
            [
                new SongArtistDto { ArtistId = artistId, DisplayOrder = 0, Name = artistName }
            ]
        });
        Assert.Equal(HttpStatusCode.Created, createSong.StatusCode);
        var createdSong = await createSong.Content.ReadFromJsonAsync<SongDto>();
        Assert.NotNull(createdSong);

        var about = await client.GetFromJsonAsync<SongAboutDto>($"/api/songs/{createdSong!.Id}/about");
        Assert.NotNull(about);
        Assert.Equal(createdSong.Id, about.SongId);
        Assert.Equal(songTitle, about.Title);
        Assert.Equal($"{artistName} feat. Guest", about.ArtistDisplay);
        Assert.Equal(1994, about.Year);
        Assert.Contains("Rock", about.GenreName!);
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
